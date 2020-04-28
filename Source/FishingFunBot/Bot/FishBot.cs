using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace FishingFun
{
    public class FishingBot
    {
        public static ILog logger = LogManager.GetLogger("Fishbot");

        private ConsoleKey castKey;
        private ConsoleKey rodKey;
        private ConsoleKey lureKey;        
        private List<ConsoleKey> tenMinKey;
        private IBobberFinder bobberFinder;
        private IBiteWatcher biteWatcher;
        private bool isEnabled;        
        private Stopwatch totalTimeStopWatch = new Stopwatch();
        private Stopwatch stopwatch = new Stopwatch();
        private Stopwatch lureStopwatch = new Stopwatch();

        public ConsoleKey RodKey { get => rodKey; set => rodKey = value; }
        public ConsoleKey LureKey { get => lureKey; set => lureKey = value; }

        private static Random random = new Random();

        public event EventHandler<FishingEvent> FishingEventHandler;

        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);


        public FishingBot(IBobberFinder bobberFinder, IBiteWatcher biteWatcher, ConsoleKey castKey, List<ConsoleKey> tenMinKey)
        {
            this.bobberFinder = bobberFinder;
            this.biteWatcher = biteWatcher;
            this.castKey = castKey;
            this.tenMinKey = tenMinKey;

            this.rodKey = ConsoleKey.D2;
            this.lureKey = ConsoleKey.D3;            

            logger.Info("FishBot Created.");

            FishingEventHandler += (s, e) => { };
        }

        public void Start()
        {
            biteWatcher.FishingEventHandler = (e) => FishingEventHandler?.Invoke(this, e);

            isEnabled = true;
     

            while (isEnabled)
            {
                WowProcess.PressKey(ConsoleKey.Spacebar);
                Thread.Sleep(120000 + random.Next(0, 60000));
            }

            logger.Error("Bot has Stopped.");
        }


        public void SetCastKey(ConsoleKey castKey)
        {
            this.castKey = castKey;
        }


        public void Stop()
        {
            isEnabled = false;
            totalTimeStopWatch.Reset();
            logger.Error("Bot is Stopping...");
        }


        public static void Sleep(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds < ms)
            {
                FlushBuffers();
                //System.Windows.Application.Current?.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate { }));
                Thread.Sleep(100);
            }
        }

        public static void FlushBuffers()
        {
            ILog log = LogManager.GetLogger("Fishbot");
            var logger = log.Logger as Logger;
            if (logger != null)
            {
                foreach (IAppender appender in logger.Appenders)
                {
                    var buffered = appender as BufferingAppenderSkeleton;
                    if (buffered != null)
                    {
                        buffered.Flush();
                    }
                }
            }
        }

    }
}