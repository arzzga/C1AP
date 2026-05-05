using Archipelago.Core.Models;
using Serilog;
using System;
using System.Collections.Generic;

namespace C1AP
{
    /// <summary>
    /// Helper class for building location lists for CB1
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Builds the complete list of locations for Crash Bandicoot 1
        /// Each of the 25 levels has one gem (clear or colored)
        /// </summary>
        public static List<ILocation> BuildLocationList()
        {
            List<ILocation> locations = new List<ILocation>();

            foreach (var locationEntry in Addresses.LocationIdInApWorld)
            {
                string locationName = locationEntry.Key;
                int locationId = locationEntry.Value;

                if (Addresses.BitOfLocation.TryGetValue(locationName, out int bitPosition))
                {
                    // Create location with address and bit position for memory tracking
                    Location location = new Location()
                    {
                        Id = locationId,
                        Name = locationName,
                        Address = Addresses.GemsCollectedAddress,
                        AddressBit = bitPosition
                    };

                    locations.Add(location);
                    Log.Logger.Debug($"Added location: {locationName} (ID: {locationId}, Bit: {bitPosition})");
                }
                else
                {
                    Log.Logger.Warning($"Could not find bit position for location: {locationName}");
                }
            }

            Log.Logger.Information($"Built location list with {locations.Count} locations");
            return locations;
        }

        /// <summary>
        /// Checks if the player is currently in a game level (not in menu)
        /// </summary>
        public static bool IsInGame()
        {
            try
            {
                int levelId = BaseHooks.GetCurrentLevelId();
                // Level ID 0x00 is typically the menu, other IDs are actual game levels
                return levelId > 0 && levelId < 0x20;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning($"Error checking if in game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the human-readable level name from the level ID
        /// </summary>
        public static string GetLevelName(int levelId)
        {
            if (Addresses.LevelIdToName.TryGetValue(levelId, out string levelName))
            {
                return levelName;
            }
            return $"Unknown Level (0x{levelId:X2})";
        }
    }
}
