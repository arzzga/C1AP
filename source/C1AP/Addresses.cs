using System.Collections.Generic;

namespace C1AP
{
    public static class Addresses
    {
        // PSX RAM base offset
        public const uint CacheOffset = 0x80000000;

        // ==================== CRITICAL GAME STATE ====================
        
        /// <summary>
        /// Current level ID - used to detect which level player is in
        /// </summary>
        public const uint CurrentLevelAddress = 0x56710; // 4 bytes

        /// <summary>
        /// Next level ID - used when changing levels
        /// </summary>
        public const uint NextLevelAddress = 0x56714; // 4 bytes

        // ==================== GEM COLLECTION STATE ====================
        
        /// <summary>
        /// Gems collected bitfield
        /// Each bit represents one gem (clear or colored)
        /// Note: CB1 has 25 gems total (one per level, either clear or colored)
        /// </summary>
        public const uint GemsCollectedAddress = 0x61988; // 4 bytes (from global GOOL variables)

        /// <summary>
        /// Alternative gem tracking - per-level breakdown
        /// Referenced at 0x57E6C in checkpoint state
        /// </summary>
        public const uint ZoneCheckpointGemsAddress = 0x579AC; // 304 x 2 bytes (spawn flags list - contains gem state)

        // ==================== GAME PROGRESS ====================
        
        /// <summary>
        /// Game progress counter
        /// Initial: 0x63 (before game start)
        /// Full completion: 0x1F (31+1 = 32, all levels + boss)
        /// </summary>
        public const uint GameProgressAddress = 0x618DC; // 4 bytes

        /// <summary>
        /// Box count / number of boxes broken
        /// Incremented as boxes are destroyed
        /// </summary>
        public const uint BoxCountAddress = 0x61984; // 4 bytes

        // ==================== LEVEL COMPLETION/EXIT FLAGS ====================
        
        /// <summary>
        /// Level exit/completion flags
        /// Bitfield tracking which levels have been beaten
        /// Located in checkpoint state area
        /// </summary>
        public const uint LevelExitFlagsAddress = 0x579AC; // 304 x 2 bytes (part of zone checkpoint state)

        // ==================== PLAYER STATE ====================
        
        /// <summary>
        /// Aku Aku (extra life) pointer
        /// Crash stores pointer to Aku Aku object here
        /// </summary>
        public const uint AkuAkuPointerAddress = 0x618CC; // 4 bytes

        // ==================== ZONE/LEVEL STATE ====================
        
        /// <summary>
        /// Current zone (entry) descriptor
        /// Loaded when entering a new level
        /// </summary>
        public const uint CurrentZoneAddress = 0x57914; // 4 bytes

        /// <summary>
        /// Zone checkpoint state - Player position X
        /// </summary>
        public const uint CheckpointPlayerTransXAddress = 0x57974; // 4 bytes

        /// <summary>
        /// Zone checkpoint state - Player position Y
        /// </summary>
        public const uint CheckpointPlayerTransYAddress = 0x57978; // 4 bytes

        /// <summary>
        /// Zone checkpoint state - Player position Z
        /// </summary>
        public const uint CheckpointPlayerTransZAddress = 0x5797C; // 4 bytes

        // ==================== CRASH BANDICOOT 1 - 25 GEMS MAPPING ====================
        
        /// <summary>
        /// Bitfield mapping of gems to their bit positions in GemsCollectedAddress
        /// Each level (1-25) has either a clear gem or a colored gem
        /// </summary>
        public static Dictionary<string, int> BitOfLocation = new Dictionary<string, int>
        {
            // Island 1 - Levels 1-6
            { "N. Sanity Beach - Clear Gem", 0 },
            { "Jungle Rollers - Clear Gem", 1 },
            { "The Great Gate - Clear Gem", 2 },
            { "Boulders - Clear Gem", 3 },
            { "Upstream - Clear Gem", 4 },
            { "Rolling Stones - Clear Gem", 5 },
            
            // Island 1 Bonus - Levels 7-10
            { "Hog Wild - Clear Gem", 6 },
            { "Native Fortress - Clear Gem", 7 },
            { "Up the Creek - Clear Gem", 8 },
            { "The Lost City - Green Gem", 9 },
            
            // Island 2 - Levels 11-15
            { "Temple Ruins - Clear Gem", 10 },
            { "Road to Nowhere - Clear Gem", 11 },
            { "Boulder Dash - Clear Gem", 12 },
            { "Sunset Vista - Clear Gem", 13 },
            { "Heavy Machinery - Clear Gem", 14 },
            
            // Island 2 Bonus - Levels 16-20
            { "Cortex Power - Clear Gem", 15 },
            { "Generator Room - Orange Gem", 16 },
            { "Toxic Waste - Blue Gem", 17 },
            { "The High Road - Clear Gem", 18 },
            { "Slippery Climb - Red Gem", 19 },
            
            // Island 3 - Levels 21-25
            { "Lights Out - Purple Gem", 20 },
            { "Jaws of Darkness - Clear Gem", 21 },
            { "Castle Machinery - Clear Gem", 22 },
            { "The Lab - Yellow Gem", 23 },
            { "The Great Hall - Clear Gem", 24 },
        };

        /// <summary>
        /// Location ID mapping for Archipelago world
        /// Maps gem location names to unique Archipelago location IDs
        /// </summary>
        public static Dictionary<string, int> LocationIdInApWorld = new Dictionary<string, int>
        {
            // Island 1
            { "N. Sanity Beach - Clear Gem", 1 },
            { "Jungle Rollers - Clear Gem", 2 },
            { "The Great Gate - Clear Gem", 3 },
            { "Boulders - Clear Gem", 4 },
            { "Upstream - Clear Gem", 5 },
            { "Rolling Stones - Clear Gem", 6 },
            
            // Island 1 Bonus
            { "Hog Wild - Clear Gem", 7 },
            { "Native Fortress - Clear Gem", 8 },
            { "Up the Creek - Clear Gem", 9 },
            { "The Lost City - Green Gem", 10 },
            
            // Island 2
            { "Temple Ruins - Clear Gem", 11 },
            { "Road to Nowhere - Clear Gem", 12 },
            { "Boulder Dash - Clear Gem", 13 },
            { "Sunset Vista - Clear Gem", 14 },
            { "Heavy Machinery - Clear Gem", 15 },
            
            // Island 2 Bonus
            { "Cortex Power - Clear Gem", 16 },
            { "Generator Room - Orange Gem", 17 },
            { "Toxic Waste - Blue Gem", 18 },
            { "The High Road - Clear Gem", 19 },
            { "Slippery Climb - Red Gem", 20 },
            
            // Island 3
            { "Lights Out - Purple Gem", 21 },
            { "Jaws of Darkness - Clear Gem", 22 },
            { "Castle Machinery - Clear Gem", 23 },
            { "The Lab - Yellow Gem", 24 },
            { "The Great Hall - Clear Gem", 25 },
        };

        /// <summary>
        /// Level ID to name mapping
        /// Used to identify which level player is currently in
        /// </summary>
        public static Dictionary<int, string> LevelIdToName = new Dictionary<int, string>
        {
            // Island 1
            { 0x01, "N. Sanity Beach" },
            { 0x02, "Jungle Rollers" },
            { 0x03, "The Great Gate" },
            { 0x04, "Boulders" },
            { 0x05, "Upstream" },
            { 0x06, "Rolling Stones" },
            
            // Island 1 Bonus
            { 0x07, "Hog Wild" },
            { 0x08, "Native Fortress" },
            { 0x09, "Up the Creek" },
            { 0x0A, "The Lost City" },
            
            // Island 2
            { 0x0B, "Temple Ruins" },
            { 0x0C, "Road to Nowhere" },
            { 0x0D, "Boulder Dash" },
            { 0x0E, "Sunset Vista" },
            { 0x0F, "Heavy Machinery" },
            
            // Island 2 Bonus
            { 0x10, "Cortex Power" },
            { 0x11, "Generator Room" },
            { 0x12, "Toxic Waste" },
            { 0x13, "The High Road" },
            { 0x14, "Slippery Climb" },
            
            // Island 3
            { 0x15, "Lights Out" },
            { 0x16, "Jaws of Darkness" },
            { 0x17, "Castle Machinery" },
            { 0x18, "The Lab" },
            { 0x19, "The Great Hall" },
            
            // Menu/Special
            { 0x00, "Main Menu" },
            { 0x1A, "Boss Level" },
        };

        /// <summary>
        /// Gem type by location name
        /// Determines whether a location contains a clear gem or colored gem
        /// </summary>
        public static Dictionary<string, string> GemTypeByLocation = new Dictionary<string, string>
        {
            { "N. Sanity Beach - Clear Gem", "Clear" },
            { "Jungle Rollers - Clear Gem", "Clear" },
            { "The Great Gate - Clear Gem", "Clear" },
            { "Boulders - Clear Gem", "Clear" },
            { "Upstream - Clear Gem", "Clear" },
            { "Rolling Stones - Clear Gem", "Clear" },
            { "Hog Wild - Clear Gem", "Clear" },
            { "Native Fortress - Clear Gem", "Clear" },
            { "Up the Creek - Clear Gem", "Clear" },
            { "The Lost City - Green Gem", "Green" },
            { "Temple Ruins - Clear Gem", "Clear" },
            { "Road to Nowhere - Clear Gem", "Clear" },
            { "Boulder Dash - Clear Gem", "Clear" },
            { "Sunset Vista - Clear Gem", "Clear" },
            { "Heavy Machinery - Clear Gem", "Clear" },
            { "Cortex Power - Clear Gem", "Clear" },
            { "Generator Room - Orange Gem", "Orange" },
            { "Toxic Waste - Blue Gem", "Blue" },
            { "The High Road - Clear Gem", "Clear" },
            { "Slippery Climb - Red Gem", "Red" },
            { "Lights Out - Purple Gem", "Purple" },
            { "Jaws of Darkness - Clear Gem", "Clear" },
            { "Castle Machinery - Clear Gem", "Clear" },
            { "The Lab - Yellow Gem", "Yellow" },
            { "The Great Hall - Clear Gem", "Clear" },
        };
    }
}
