using Archipelago.Core.GameClients;
using Serilog;
using System;

namespace C1AP
{
    /// <summary>
    /// Base hooks for monitoring Crash Bandicoot 1 game state
    /// Detects when gems are collected and level progress
    /// </summary>
    public static class BaseHooks
    {
        private static uint _lastGemsCollected = 0;

        public static void Initialize()
        {
            Log.Logger.Information("BaseHooks initialized for CB1");
        }

        /// <summary>
        /// Checks game memory for changes in gem collection state
        /// Should be called regularly to detect location checks
        /// </summary>
        public static void MonitorGemCollection()
        {
            try
            {
                uint currentGems = Memory.ReadUInt(Addresses.GemsCollectedAddress);

                // Check if any new gems have been collected since last check
                if (currentGems != _lastGemsCollected)
                {
                    uint changedBits = currentGems ^ _lastGemsCollected;
                    
                    for (int i = 0; i < 25; i++)
                    {
                        // Check if bit i changed from 0 to 1 (gem collected)
                        if ((changedBits & (1u << i)) != 0 && (currentGems & (1u << i)) != 0)
                        {
                            OnGemCollected(i);
                        }
                    }

                    _lastGemsCollected = currentGems;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning($"Error monitoring gem collection: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when a gem is collected
        /// </summary>
        private static void OnGemCollected(int bitPosition)
        {
            Log.Logger.Debug($"Gem collected at bit position {bitPosition}");
        }

        /// <summary>
        /// Gets the current level ID from game memory
        /// </summary>
        public static int GetCurrentLevelId()
        {
            try
            {
                return (int)Memory.ReadUInt(Addresses.CurrentLevelAddress);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning($"Error reading current level: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Gets the game progress value
        /// </summary>
        public static int GetGameProgress()
        {
            try
            {
                return (int)Memory.ReadUInt(Addresses.GameProgressAddress);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning($"Error reading game progress: {ex.Message}");
                return -1;
            }
        }
    }
}
