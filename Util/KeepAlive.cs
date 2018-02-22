#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;

namespace log4net.Util
{
    /// <summary>
    /// Keep appenders and logging busy with occasional "Alive" notice
    /// </summary>
    /// <remarks>
    /// <para>
    /// This may serve to:
    /// 
    /// * force roll over of files even with little logging,
    /// * maintain and track application instances health
    /// 
    /// </para>
    /// <para>
    /// A single thread does the time scheduling and calling. 
    /// It is implemented as a static singleton. 
    /// Appenders can be <see cref="Manage"/>d and <see cref="Release"/>d.
    /// When at least one appender is managed, thread executes. Otherwise it stops.
    /// </para>
    /// <para>
    /// </para>
    /// It's made to be thread safe. 
    /// The code locks <see cref="m_control_locker"/> to synchronize, wait and pulse.
    /// Additionally the code locks the <see cref="m_calls_locker"/> for any operation with appenders.
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public sealed class KeepAlive : IDisposable
    {
        /// <summary>
        /// The only single static instance of this class
        /// </summary>
        public static readonly KeepAlive Instance = new KeepAlive();

        /// <summary>
        /// Let a callback be called by KeepAlive regularly;
        /// </summary>
        /// <param name="alivecall">callback to be called</param>
        /// <param name="interval">how often</param>
        public void Manage(AliveCall alivecall, int interval)
        {
            lock (m_control_locker)
            {
                lock (m_calls_locker)
                {
                    m_calls[alivecall] = MakeConfig(alivecall, interval);
                }

                // ensure we have the right repo if config changed
                m_rep = LogManager.GetRepository(typeof(KeepAlive).Assembly);

                if (!m_stop) Start();

                Monitor.PulseAll(m_control_locker);
            }
        }

        /// <summary>
        /// Stop managing an appender
        /// </summary>
        /// <param name="alivecall">callback to be released</param>
        public void Release(AliveCall alivecall)
        {
            lock (m_control_locker)
            {
                lock (m_calls_locker)
                {
                    m_calls.Remove(alivecall);
                }

                Monitor.PulseAll(m_control_locker);
            }
        }

        /// <summary>
        /// Used to lock operations on this (Start, Stop, Manage, Release)
        /// </summary>
        readonly object m_control_locker = new object();

        /// <summary>
        /// Used to lock operations on <see cref="m_calls"/>
        /// </summary>
        readonly object m_calls_locker = new object();


        /// <summary>
        /// Internal appender and config store
        /// </summary>
        readonly IDictionary<AliveCall, Config> m_calls = new Dictionary<AliveCall, Config>();

        /// <summary>
        /// Repository used for custom <see cref="LoggingEvent"/>
        /// </summary>
        ILoggerRepository m_rep;

        /// <summary>
        /// The Alive thread
        /// </summary>
        Thread m_thread;

        /// <summary>
        /// flag indication that <see cref="Run"/> should terminate.
        /// </summary>
        bool m_stop;

        /// <summary>
        /// Initiate the <see cref="Run"/> loop
        /// </summary>
        private void Start()
        {
            lock (m_control_locker)
            {
                if (m_thread == null || !m_thread.IsAlive)
                {
                    m_thread = new Thread(Run);
                    m_thread.Name = String.Format("{0}-{1}", typeof(KeepAlive).Name, m_thread.ManagedThreadId);
                    m_thread.Priority = ThreadPriority.Lowest;
                    m_thread.IsBackground = true;
                    m_thread.Start();
                }
            }
        }

        /// <summary>
        /// This is fatal.
        /// </summary>
        public void Stop()
        {
            lock (m_control_locker)
            {
                m_stop = true;

                Monitor.PulseAll(m_control_locker);
            }

            if (m_thread != null && m_thread.IsAlive)
            {
                m_thread.Priority = ThreadPriority.AboveNormal;

                if (!m_thread.Join(1000))
                    m_thread.Abort();
            }
        }

        /// <summary>
        /// Loop as long as not <see cref="m_stop"/>
        /// </summary>
        private void Run()
        {
            var maxSnooze = TimeSpan.FromMilliseconds(500);

            try
            {
                lock (m_control_locker)
                {
                    while (!m_stop)
                    {
                        while (!m_stop && m_calls.Count == 0)
                        {
                            // suspend activity if there's nothing to do
                            Monitor.Wait(m_control_locker, maxSnooze);
                            Monitor.PulseAll(m_control_locker);
                        }

                        if (m_stop) break;

                        var snooze = ExecuteSchedule(maxSnooze);

                        Monitor.Wait(m_control_locker, snooze);
                        Monitor.PulseAll(m_control_locker);
                    }
                }
            }
            catch (ThreadAbortException tax)
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error("Alive.Run() aborted.", tax);
#else
                LogLog.Error(GetType(), "Alive.Run() aborted.", tax);
#endif
            }
            catch (Exception x)
            {
                m_stop = true;

#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error("Alive.Run() failed.", x);
#else
                LogLog.Error(GetType(), "Alive.Run() failed.", x);
#endif
            }
            finally
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Warn("Alive.Run() finished.");
#else
                LogLog.Warn(GetType(), "Alive.Run() finished.");
#endif
            }
        }

        private TimeSpan ExecuteSchedule(TimeSpan maxSnooze)
        {
            var now = DateTime.UtcNow;
            var waketime = now + maxSnooze;
            var scheduledConfigs = new List<Config>();

            lock (m_calls_locker)
            {
                foreach (var config in m_calls.Values)
                {
                    if (now.AddMilliseconds(10) >= config.Schedule)
                    {
                        scheduledConfigs.Add(config);

                        // round to a multiple of interval with an offset
                        var wake_ms = (now - s_refdt).TotalMilliseconds + config.Interval + 10;
                        wake_ms -= wake_ms % config.Interval;
                        wake_ms = Math.Round(wake_ms) + config.Offset;

                        // set new schedule
                        config.Schedule = s_refdt.AddMilliseconds(wake_ms);
                    }

                    if (waketime > config.Schedule) waketime = config.Schedule;
                }
            }

            // execute scheduled actions
            foreach (var config in scheduledConfigs) MakeCall(config);

            var snooze = waketime - DateTime.UtcNow;
            if (snooze < TimeSpan.Zero) return TimeSpan.Zero;
            if (snooze > maxSnooze) return maxSnooze;
            return snooze;
        }

        private void MakeCall(Config config)
        {
            try
            {
                config.Call();
            }
            catch (Exception x)
            {
                if (!config.ExceptionLogged)
                {
                    config.ExceptionLogged = true;

#if LOG4NET_1_2_10_COMPATIBLE
                    LogLog.Error("Exception in Alive.MakeCall(). Further exceptions will not be logged for this call.", x);
#else
                    LogLog.Error(GetType(), "Exception in Alive.MakeCall(). Further exceptions will not be logged for this call.", x);
#endif
                }
            }
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        Config MakeConfig(AliveCall alivecall, int interval)
        {
            var config = new Config()
            {
                Interval = interval,
                Schedule = DateTime.Today,
                Call = alivecall
            };

            if (config.Interval <= 0)
                config.Interval = 60000;

            while (config.Offset == 0)
                config.Offset = new Random((int)(DateTime.Now.Ticks % int.MaxValue)).Next(config.Interval);

            return config;
        }

        /// <summary>
        /// Per-appender config structure
        /// </summary>
        private class Config
        {
            /// <summary>
            /// Interval to keep alive with
            /// </summary>
            public int Interval;

            /// <summary>
            /// Offset to the interval. 
            /// </summary>
            /// <remarks>
            /// Randomize the logging time so that if many apps log come together, they don't compete too much.
            /// Still keep the regular interval.
            /// </remarks>
            public int Offset;

            /// <summary>
            /// Next run schedule
            /// </summary>
            public DateTime Schedule;

            /// <summary>
            /// This call has been problematic. This helps reduce verbosity in case of permanent failure.
            /// </summary>
            public bool ExceptionLogged;

            /// <summary>
            /// The AliveCall delegate to be called
            /// </summary>
            public AliveCall Call;
        }

        /// <summary>
        /// Alive call action delegate
        /// </summary>
        public delegate void AliveCall();

        private static readonly DateTime s_refdt = DateTime.ParseExact("1970", "yyyy", CultureInfo.InvariantCulture);
    }
}
