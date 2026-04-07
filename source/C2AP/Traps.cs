using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace C2AP
{
    internal class Traps
    {
        public enum TrapType
        {
            BigCrash,
            SmallCrash,
            NoLives,
            JetpackControls,
        }
        private static Timer? trapRefresh = null;
        private const int tickRate = 100;
        private static int trapDuration;

        private static uint crashAddress;
        private const int defaultCrashSize = 0x1000;
        private static int storedLives;
        private static double bigCrashSizeMult = 1.5;
        private static double smallCrashSizeMult = 0.2;

        private static CustomHook jetpackControlsHook = new CustomHook([//"nop"]);
            $"la $t0, 0x{Addresses.InputsAddress + Addresses.CacheOffset:X}",
            "lw $t1, 0($t0)",
            "lw $t2, 0($t0)",
            
            //prepare $t1 for masking
            "srl $t1, $t1, 0x10",
            "nor $t1, $t1, $zero",

            //"unpress" the controls that will be affected
            "lui $t3, 0x6c50",//0x6250",
            "nor $t2, $t2, $t3",

            
            $"andi $t3, $t1, 0x{InputFlag.Circle:X}", //capture the value of circle
            "sll $t3, $t3, 0x9", //shift it to the "down" button
            "or $t2, $t2, $t3", //apply

            $"andi $t3, $t1, 0x{InputFlag.Cross:X}", //capture the value of cross
            "sll $t3, $t3, 0x6", //shift it to the "up" button
            "or $t2, $t2, $t3", //apply
            
            $"andi $t3, $t1, 0x{InputFlag.R1:X}",
            "sll $t3, $t3, 0x9", //shift it to the "up" button
            "or $t2, $t2, $t3", //apply

            $"andi $t3, $t1, 0x{InputFlag.L1:X}",
            "sll $t3, $t3, 0xc", //shift it to the "down" button
            "or $t2, $t2, $t3", //apply

            $"andi $t3, $t1, 0x{InputFlag.Up:X}",
            "sll $t3, $t3, 0x19", //shift it to the "circle" button
            "or $t2, $t2, $t3", //apply

            $"andi $t3, $t1, 0x{InputFlag.Down:X}",
            "sll $t3, $t3, 0x18", //shift it to the "cross" button
            "or $t2, $t2, $t3", //apply

            //"nop",

            //now compliment and exit
            "nor $t2, $t2, $zero",
            "sw $t2, 0($t0)"
            ]);
        
        private static List<Trap> activeTraps = new List<Trap>();
        private class Trap
        {
            public TrapType type;
            public int duration;
            public Trap(TrapType type)
            {
                this.type = type;
                this.duration = trapDuration;
            }
        }

        public static void Initialize()
        {
            if (trapRefresh == null)
            {
                trapRefresh = new Timer(tickRate);
                trapRefresh.Elapsed += (s, ev) =>
                {
                    ApplyTraps();
                };
            }

            App.Client.Options.TryGetValue("trap_duration", out var trapDurationSeconds);
            if (trapDurationSeconds != null)
            {
                Log.Logger.Debug($"option: {trapDurationSeconds}");
            }
            else
            {
                Log.Logger.Debug($"option: null");
                return;
            }
            App.Client.Options.TryGetValue("small_crash_size", out var smallCrashSize);
            if (smallCrashSize != null)
            {
                Log.Logger.Debug($"option: {smallCrashSize}");
            }
            else
            {
                Log.Logger.Debug($"option: null");
                return;
            }
            App.Client.Options.TryGetValue("big_crash_size", out var bigCrashSize);
            if (bigCrashSize != null)
            {
                Log.Logger.Debug($"option: {bigCrashSize}");
            }
            else
            {
                Log.Logger.Debug($"option: null");
                return;
            }
            smallCrashSizeMult = Convert.ToInt32(smallCrashSize.ToString()) / 100.0;
            bigCrashSizeMult = Convert.ToInt32(bigCrashSize.ToString()) / 100.0;
            trapDuration = Convert.ToInt32(trapDurationSeconds.ToString()) * 1000 / tickRate;
            //trapDuration = 100 * 1000 / tickRate;
            Log.Logger.Information("Initialized traps");
        }

        public static void AddTrap(TrapType type)
        {
            if (trapRefresh == null) return;
            if (type == TrapType.BigCrash || type == TrapType.SmallCrash)
            {
                activeTraps.RemoveAll(t => t.type == TrapType.BigCrash || t.type == TrapType.SmallCrash);
            }
            else
            {
                //refresh duration if trap already exists
                foreach (Trap trap in activeTraps)
                {
                    if (trap.type == type)
                    {
                        trap.duration = trapDuration;
                        return;
                    }
                }
            }

            activeTraps.Add(new Trap(type));
            trapRefresh.AutoReset = true;
            trapRefresh.Enabled = true;
        }
        private static void ApplyTraps()
        {
            if (activeTraps.Count == 0)
            {
                if (trapRefresh == null) return;
                trapRefresh.AutoReset = false;
                trapRefresh.Enabled = false;
                return;
            }
            crashAddress = CrashObject.FindObjectAddress(0, 0);
            //if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            //{
            //    return;
            //}
            foreach (Trap trap in activeTraps.ToList())
            {
                trap.duration--;
                Log.Logger.Debug($"applying trap {trap.type} with duration {trap.duration}");
                if (trap.duration <= 0)
                {
                    switch (trap.type)
                    {
                        case TrapType.BigCrash:
                        case TrapType.SmallCrash:
                            ResetCrashSize();
                            break;
                        case TrapType.NoLives:
                            ResetNoLives();
                            break;
                        case TrapType.JetpackControls:
                            ResetJetpackControls();
                            break;
                    }
                    activeTraps.Remove(trap);
                }
                else
                {
                    switch (trap.type)
                    {
                        case TrapType.BigCrash:
                            ApplyCrashSize(bigCrashSizeMult);
                            break;
                        case TrapType.SmallCrash:
                            ApplyCrashSize(smallCrashSizeMult);
                            break;
                        case TrapType.NoLives:
                            ApplyNoLives();
                            break;
                        case TrapType.JetpackControls:
                            ApplyJetpackControls();
                            break;
                    }
                }
                
            }
        }
        private static void ApplyCrashSize(double sizeMult)
        {
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            {
                return;
            }
            int newSize = (int)(defaultCrashSize * sizeMult);
            if (Memory.ReadInt(crashAddress + 0x78) != newSize)
            {
                Memory.Write(crashAddress + 0x78, newSize);
                Memory.Write(crashAddress + 0x7C, newSize);
                Memory.Write(crashAddress + 0x80, newSize);
            }
        }
        private static void ResetCrashSize()
        {
            ApplyCrashSize(1);
        }
        private static void ApplyNoLives()
        {
            int lives = Memory.ReadInt(Addresses.LivesGlobalAddress);
            if ((lives > 0 && lives < 4) || storedLives == 0) // written this way to prevent gameover from giving the player many extra lives
            {
                storedLives += lives;
                Memory.Write(Addresses.LivesGlobalAddress, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    Memory.Write(crashAddress + Addresses.LivesOffset, 0);
                }
            }
        }
        private static void ResetNoLives()
        {
            if (storedLives != 0)
            {
                Memory.Write(Addresses.LivesGlobalAddress, storedLives);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    Memory.Write(crashAddress + Addresses.LivesOffset, storedLives);
                }
                storedLives = 0;
            }
        }
        private static void ApplyJetpackControls()
        {
            if (jetpackControlsHook._freeAddress != 0)
            {
                return;
            }
            if (BaseHooks.ApItemsHook == null) return;
            jetpackControlsHook.InsertHook(0x15A38, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + 0x4);
        }

        private static void ResetJetpackControls()
        {
            ApplyCrashSize(1);
            jetpackControlsHook.RemoveHook();
        }
    }
}
