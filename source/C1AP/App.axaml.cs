using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
using Archipelago.Core.GameClients;
using Archipelago.Core.Models;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Location = Archipelago.Core.Models.Location;
using Timer = System.Timers.Timer;

namespace C1AP;

public partial class App : Application
{
    public static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    public static List<ILocation> GameLocations { get; set; }
    public static Dictionary<string, object> SlotData { get; private set; } = new();
    private static readonly object _lockObject = new object();
    private static bool _hasSubmittedGoal { get; set; }
    private static bool _useQuietHints { get; set; }

    /// <summary>
    /// Crash Bandicoot 1 game state tracker
    /// </summary>
    private class CrashState
    {
        // 25 gems total (one per level, either clear or colored)
        public uint GemsCollected;
        public byte[] GemLocations = new byte[4]; // 32 bits for 25 gems (need 4 bytes)
    }

    private static CrashState crashState = new CrashState();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Start();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Context
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = Context
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void Start()
    {
        Context = new MainWindowViewModel("0.6.5");
        Context.ClientVersion = "v0.1.0";
        Context.ConnectClicked += Context_ConnectClicked;
        Context.CommandReceived += (e, a) =>
        {
            if (string.IsNullOrWhiteSpace(a.Command)) return;
            Client?.SendMessage(a.Command);
            HandleCommand(a.Command);
        };
        Context.ConnectButtonEnabled = true;
        _hasSubmittedGoal = false;
        _useQuietHints = true;

        Log.Logger.Information("Crash Bandicoot 1 Archipelago Client");
        Log.Logger.Information("This client requires Crash Bandicoot 1 (NTSC-U or PAL) on DuckStation");
    }

    private void HandleCommand(string command)
    {
        switch (command)
        {
            case "syncGameState":
                Log.Logger.Information("Syncing game state with Archipelago server...");
                SyncGameState();
                UpdateCrashState();
                Log.Logger.Information("Sync complete.");
                break;
            case "useQuietHints":
                Log.Logger.Information("Hints for found locations will not be displayed. Type 'useVerboseHints' to show them.");
                _useQuietHints = true;
                break;
            case "useVerboseHints":
                Log.Logger.Information("Hints for found locations will be displayed. Type 'useQuietHints' to hide them.");
                _useQuietHints = false;
                break;
            case "itemstate":
                if (Client.ItemState == null) break;
                List<Item> items = Client.ItemState.ReceivedItems.OfType<Item>().ToList();
                foreach (Item item in items)
                {
                    Log.Logger.Information($"{item.Name}");
                }
                break;
            case "locationstate":
                if (Client.LocationState == null) break;
                List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
                foreach (Location location in locations)
                {
                    Log.Logger.Information($"{location.Name}");
                }
                break;
        }
    }

    private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
    {
        // Cleanup previous connection if exists
        if (Client != null)
        {
            Client.CancelMonitors();
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ItemReceived -= ItemReceived;
            Client.MessageReceived -= Client_MessageReceived;
            Client.LocationCompleted -= Client_LocationCompleted;
            Client.CurrentSession.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
        }

        // Connect to DuckStation emulator
        DuckstationClient? client = null;
        try
        {
            client = new DuckstationClient();
        }
        catch (ArgumentException ex)
        {
            Log.Logger.Warning("DuckStation not running. Please open DuckStation and launch Crash Bandicoot 1 before connecting!");
            return;
        }

        var DuckstationConnected = client.Connect();
        if (!DuckstationConnected)
        {
            Log.Logger.Warning("Could not connect to DuckStation. Please ensure DuckStation is running and Crash 1 is loaded.");
            return;
        }

        // Initialize Archipelago client
        Client = new ArchipelagoClient(client);
        Client.ShouldSaveStateOnItemReceived = false;

        Memory.GlobalOffset = Memory.GetDuckstationOffset();

        // Initialize game state monitors
        BaseHooks.Initialize();

        // Wire up event handlers
        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;
        Client.LocationCompleted += Client_LocationCompleted;
        Client.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemReceived += ItemReceived;
        Client.EnableLocationsCondition = () => IsInGame();

        // Connect to Archipelago server
        await Client.Connect(e.Host, "Crash 1", "");
        if (!Client.IsConnected)
        {
            Log.Logger.Error("Failed to connect to Archipelago server. Please check your host address.");
            return;
        }

        // Build location list and start monitoring
        GameLocations = BuildLocationList();
        Client.MonitorLocations(GameLocations);

        // Login to slot
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
        
        Log.Logger.Information("Connected to Archipelago successfully!");
    }

    /// <summary>
    /// Build the list of all 25 gem locations for monitoring
    /// </summary>
    private static List<ILocation> BuildLocationList()
    {
        List<ILocation> locations = new List<ILocation>();

        foreach (var kvp in Addresses.LocationIdInApWorld)
        {
            string locationName = kvp.Key;
            int locationId = kvp.Value;
            int bitPosition = Addresses.BitOfLocation[locationName];

            locations.Add(new Location
            {
                Name = locationName,
                Id = locationId,
                Address = Addresses.GemsCollectedAddress,
                AddressBit = bitPosition
            });
        }

        return locations;
    }

    /// <summary>
    /// Check if player is currently in a playable level (not menu/loading)
    /// </summary>
    private static bool IsInGame()
    {
        try
        {
            byte levelId = Memory.ReadByte(Addresses.CurrentLevelAddress);
            // Level IDs 0x01-0x19 are playable levels
            // 0x00 is menu, 0x1A+ are special/unused
            return levelId >= 0x01 && levelId <= 0x19;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Called when a location is completed by the player
    /// </summary>
    private void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
    {
        UpdateCrashState();
        CheckGoalCondition();
    }

    /// <summary>
    /// Called when checked locations are updated (synced with server)
    /// </summary>
    private static void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
    {
        CheckGoalCondition();
    }

    /// <summary>
    /// Sync current game state from Archipelago server to emulator memory
    /// </summary>
    public static void SyncGameState()
    {
        if (Client.LocationState == null) return;
        if (Client.ItemState == null) return;

        // Get all completed locations from the server
        List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
        foreach (Location location in locations)
        {
            if (location.Address == 0 || location.AddressBit == 0) continue;

            // Mark gem as collected in our state tracker
            if (location.Address == Addresses.GemsCollectedAddress)
            {
                int byteIndex = location.AddressBit / 8;
                int bitIndex = location.AddressBit % 8;
                crashState.GemLocations[byteIndex] |= (byte)(1 << bitIndex);
            }
        }

        // Count total gems received from Archipelago
        List<Item> items = Client.ItemState.ReceivedItems.ToList();
        uint gemCount = 0;
        foreach (Item item in items)
        {
            if (item.Name == "Gem")
            {
                gemCount++;
            }
        }
        crashState.GemsCollected = gemCount;
    }

    /// <summary>
    /// Update emulator memory with current crash state
    /// </summary>
    public static void UpdateCrashState()
    {
        // Write gem collection bitfield to emulator memory
        Memory.WriteByteArray(Addresses.GemsCollectedAddress, crashState.GemLocations);
    }

    /// <summary>
    /// Called when an item is received from another player or Archipelago
    /// </summary>
    private async void ItemReceived(object? o, ItemReceivedEventArgs args)
    {
        Log.Logger.Debug($"Item Received: {JsonConvert.SerializeObject(args.Item)}");

        switch (args.Item.Name)
        {
            case "Gem":
                crashState.GemsCollected++;
                break;
            case "Life":
                // Add a life to Crash
                try
                {
                    // Find Crash object and increment lives
                    // This is a simplified approach - may need adjustment based on actual CB1 structure
                    Log.Logger.Information("Received an extra life!");
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning($"Could not give life: {ex.Message}");
                }
                break;
            case "Wumpa Fruit":
                // Add Wumpa fruit to inventory
                try
                {
                    Log.Logger.Information("Received Wumpa Fruit!");
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning($"Could not give Wumpa Fruit: {ex.Message}");
                }
                break;
        }

        UpdateCrashState();
    }

    /// <summary>
    /// Check if the goal condition is met and send goal completion to server
    /// </summary>
    private static void CheckGoalCondition()
    {
        if (Client.LocationState == null) return;
        if (Client.ItemState == null) return;
        if (_hasSubmittedGoal) return;

        try
        {
            // Goal: Collect all 25 gems
            List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
            
            if (locations.Count >= 25)
            {
                Log.Logger.Information("All gems collected! Goal achieved!");
                Timer sendGoal = new Timer();
                sendGoal.Interval = 10;
                sendGoal.AutoReset = false;
                sendGoal.Elapsed += (s, ev) =>
                {
                    Client.SendGoalCompletion();
                };
                sendGoal.Enabled = true;
                _hasSubmittedGoal = true;
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning($"Error checking goal condition: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when connected to Archipelago server
    /// </summary>
    private static void OnConnected(object sender, EventArgs args)
    {
        int currentSlot = Client.CurrentSession.ConnectionInfo.Slot;
        Log.Logger.Information("Connected to Archipelago");
        Log.Logger.Information($"Playing Crash Bandicoot 1 as {Client.CurrentSession.Players.GetPlayerName(currentSlot)}");

        // Get slot data if available
        var slotDataTask = Client.CurrentSession.DataStorage.GetSlotDataAsync(currentSlot);
        slotDataTask.Wait();
        SlotData = slotDataTask.Result;

        // Request hints
        Client?.SendMessage("!hint");

        // Sync current game state with server
        SyncGameState();
        UpdateCrashState();
    }

    /// <summary>
    /// Called when disconnected from Archipelago server
    /// </summary>
    private static void OnDisconnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
        _hasSubmittedGoal = false;
        _useQuietHints = true;
    }

    /// <summary>
    /// Log received messages from server
    /// </summary>
    private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        // Filter hint messages based on quiet hints setting
        if (e.Message.Parts.Any(x => x.Text == "[Hint]: ") && (!_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)")))
        {
            LogMessage(e.Message);
        }
        else if (!e.Message.Parts.Any(x => x.Text == "[Hint]: ") || !_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)"))
        {
            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
        }
    }

    /// <summary>
    /// Log a message from the server
    /// </summary>
    private static void LogMessage(LogMessage message)
    {
        var newMessage = message.Parts.Select(x => x.Text);
        var messageText = string.Join("", newMessage);
        Log.Logger.Information(messageText);
    }
}
