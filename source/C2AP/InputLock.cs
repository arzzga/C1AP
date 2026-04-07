using Serilog;
using System;
using System.Net;

namespace C2AP
{
    [Flags]
    internal enum InputFlag : int
    {
        None        = 0x0000,
        Select      = 0x0001,
        Start       = 0x0008,
        Up          = 0x0010,
        Right       = 0x0020,
        Down        = 0x0040,
        Left        = 0x0080,
        L2          = 0x0100,
        R2          = 0x0200,
        L1          = 0x0400,
        R1          = 0x0800,
        Triangle    = 0x1000,
        Circle      = 0x2000,
        Cross       = 0x4000,
        Square      = 0x8000
    }

    internal class InputLock
    {
        // static field holding current input flags
        public static InputFlag _inputflag = InputFlag.None;

        // Example usage (commented):
        // InputLock.inputflag = InputFlag.Triangle | InputFlag.Circle;
        // bool hasTriangle = (InputLock.inputflag & InputFlag.Triangle) == InputFlag.Triangle;
        private static CustomHook _inputHook = new CustomHook(["nop"]);
        //private static uint flagAddress;

        public static void Initialize()
        {
            //0x15A38 works, also 0x15A0C, 0x15A44
            _inputHook.InsertHook(0x15A38, 0xf000); //0x15A54
            Log.Error("Inputlock needs to use a different free address");
        }

        private static void RefreshInputHook()
        {
            uint val = (uint)_inputflag << 16;
            _inputHook.ReplaceAsm([
                        $"la $t0, 0x{Addresses.InputsAddress + Addresses.CacheOffset:X}",
                        "lw $t1, 0($t0)",
                        $"la $t2, 0x{val:X}",
                        "or $t1, $t1, $t2",
                        "sw $t1, 0($t0)",
                        ]);
            //Log.Information($"la $t2, 0x{val:X}");
        }
        public static void LockInput(InputFlag flag)
        {
            if ((_inputflag & flag) != 0)
            {
                return;
            }
            _inputflag |= flag;
            RefreshInputHook();
        }

        public static void UnlockInput(InputFlag flag)
        {
            Log.Information($"a1");
            if ( (_inputflag & flag) == 0)
            {
                return;
            }
            Log.Information($"a2");
            _inputflag &= ~flag;
            RefreshInputHook();
        }
    }
}
