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
        private static Timer timer;
        //System.Timers.
        public static void Initialize()
        {
            timer = new Timer(150);
            //timer.Elapsed += OnTimedEvent; 
            timer.AutoReset = true; 
            timer.Enabled = true; 
        }
        private static void OnTimedEvent(Object? source, ElapsedEventArgs e)
        {
            Console.WriteLine($"Elapsed at: {e.SignalTime}");
        }
    }
}
