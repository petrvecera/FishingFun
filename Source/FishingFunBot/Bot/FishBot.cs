﻿using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private int maxFinshingMinutes = 60;
        private Stopwatch totalTimeStopWatch = new Stopwatch();
        private Stopwatch stopwatch = new Stopwatch();
        private Stopwatch lureStopwatch = new Stopwatch();

        public ConsoleKey RodKey { get => rodKey; set => rodKey = value; }
        public ConsoleKey LureKey { get => lureKey; set => lureKey = value; }

        private static Random random = new Random();

        public event EventHandler<FishingEvent> FishingEventHandler;

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

            // Enable total time stopwatch
            totalTimeStopWatch.Start();

            // Equip the rod
            WowProcess.PressKey(rodKey);

            // Enable lure stopwatch
            lureStopwatch.Start();
            Lure.applyLure(rodKey, lureKey);

            while (isEnabled)
            {
                try
                {                    
                    checkLureTimer();
                    logger.Info($"Pressing key {castKey} to Cast.");

                    FishingEventHandler?.Invoke(this, new FishingEvent { Action = FishingAction.Cast });
                    WowProcess.PressKey(castKey);

                    Watch(2000);

                    WaitForBite();
                    checkForStopTimer();
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                    Thread.Sleep(2000);
                }
            }

            logger.Error("Bot has Stopped.");
        }


        public void checkForStopTimer()
        {
            TimeSpan ts = totalTimeStopWatch.Elapsed;
            int elapsedMinutes = ts.Minutes;

            logger.Info($"Elapsed {elapsedMinutes} out of {maxFinshingMinutes}mins.");

            if (elapsedMinutes > maxFinshingMinutes)
            {               
                Stop();
            }

            
        }

        public void checkLureTimer()
        {
            int lureTimer = 10;

            TimeSpan ts = lureStopwatch.Elapsed;
            int elapsedMinutes = ts.Minutes;
            
            logger.Info($"Checking the lure timer.");            

            if (elapsedMinutes > lureTimer)
            {
                Lure.applyLure(rodKey, lureKey);
                lureStopwatch.Restart();
            } else
            {
                logger.Info($"Lure still active for {lureTimer - elapsedMinutes} min");
            }


        }

        public void SetCastKey(ConsoleKey castKey)
        {
            this.castKey = castKey;
        }

        private void Watch(int milliseconds)
        {
            bobberFinder.Reset();
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
            {
                bobberFinder.Find();
            }
            stopwatch.Stop();
        }

        public void Stop()
        {
            isEnabled = false;
            totalTimeStopWatch.Reset();
            logger.Error("Bot is Stopping...");
        }

        private void WaitForBite()
        {
            bobberFinder.Reset();

            var bobberPosition = FindBobber();
            if (bobberPosition == Point.Empty)
            {
                return;
            }

            this.biteWatcher.Reset(bobberPosition);

            logger.Info("Bobber start position: " + bobberPosition);

            var timedTask = new TimedAction((a) => { logger.Info("Fishing timed out!"); }, 25 * 1000, 25);

            // Wait for the bobber to move
            while (isEnabled)
            {
                var currentBobberPosition = FindBobber();
                if (currentBobberPosition == Point.Empty || currentBobberPosition.X == 0) { return; }

                if (this.biteWatcher.IsBite(currentBobberPosition))
                {
                    Loot(bobberPosition);
                    PressTenMinKey();
                    return;
                }

                if (!timedTask.ExecuteIfDue()) { return; }
            }
        }

        private DateTime StartTime = DateTime.Now;


        private void PressTenMinKey()
        {
            if ((DateTime.Now - StartTime).TotalMinutes > 10 && tenMinKey.Count > 0)
            {
                StartTime = DateTime.Now;
                logger.Info($"Pressing key {tenMinKey} to run a macro.");

                FishingEventHandler?.Invoke(this, new FishingEvent { Action = FishingAction.Cast });

                foreach (var key in tenMinKey)
                {
                    WowProcess.PressKey(key);
                }
            }
        }

        private void Loot(Point bobberPosition)
        {
            Sleep(900 + random.Next(0, 225));
            logger.Info($"Right clicking mouse to Loot.");
            WowProcess.RightClickMouse(logger, bobberPosition);
            Sleep(1000 + random.Next(0, 125));
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

        private Point FindBobber()
        {
            var timer = new TimedAction((a) => { logger.Info("Waited seconds for target: " + a.ElapsedSecs); }, 1000, 5);

            while (true)
            {
                var target = this.bobberFinder.Find();
                if (target != Point.Empty || !timer.ExecuteIfDue()) { return target; }
            }
        }
    }
}