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
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using log4net.Core;

namespace log4net.Util.Stamps
{
    /// <summary>
    /// The class providing standard stamps
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class Stamp : IStamp
    {
        /// <summary>
        /// Property name to set
        /// </summary>
        public virtual String Name { get; set; }

        /// <summary>
        /// A universal stamp with name "stamp"
        /// </summary>
        public Stamp()
        {
            Name = "stamp";
        }

        /// <summary>
        /// Stamp the event.
        /// </summary>
        /// <param name="loggingEvent">event to stamp</param>
        public virtual void StampEvent(Core.LoggingEvent loggingEvent)
        {
            var value = GetValue(loggingEvent);
            value = GetSanitizedValue(loggingEvent, value);
            SetStamp(loggingEvent, Name, value);
        }

        /// <summary>
        /// Store the stamp value in a property of the logging event
        /// </summary>
        /// <param name="loggingEvent">event to stamp</param>
        /// <param name="name">name of the stamp</param>
        /// <param name="value">stamp value</param>
        protected virtual void SetStamp(Core.LoggingEvent loggingEvent, string name, Object value)
        {
            loggingEvent.Properties[name] = value;
        }

        /// <summary>
        /// Create stamp value
        /// </summary>
        /// <param name="loggingEvent">event to stamp</param>
        /// <returns>value to set as a stamp</returns>
        protected virtual Object GetValue(Core.LoggingEvent loggingEvent)
        {
            var tSys = GetTimeStampValue(AgeReference.Epoch1970, AgeReference.SystemStart, 0, false);
            var tApp = GetTimeStampValue(AgeReference.SystemStart, AgeReference.ApplicationStart, 0, false);
            var tNow = GetTimeStampValue(AgeReference.ApplicationStart, AgeReference.Now, 0, false);
            var seq = GetSequence();
            var pid = GetProcessId();

            return String.Format(
                "{0};{1};{2};{3};{4};{5}"
                , Environment.MachineName
                , tSys
                , tApp
                , tNow
                , pid
                , seq
                );
        }

        /// <summary>
        /// Make sure the value will not change later
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Object GetSanitizedValue(Core.LoggingEvent loggingEvent, Object value)
        {
            if (value == null) return null;

            if (value is IFixingRequired)
                value = ((IFixingRequired)value).GetFixedObject();

            if (value == null) return null;

            if (!(value is string || value.GetType().IsPrimitive))
                value = loggingEvent.Repository.RendererMap.FindAndRender(value);

            return value;
        }

        #region Statics

        /// <summary>
        /// lock root
        /// </summary>
        static protected readonly Object s_sync_root = new Object();

        /// <summary>
        /// System start reference time against unix epoch in seconds
        /// </summary>
        static double s_ref_sys_time;

        /// <summary>
        /// Application start reference time against unix epoch in seconds
        /// </summary>
        static double s_ref_app_time;

        /// <summary>
        /// cache the proc id;
        /// </summary>
        static int s_processId;

        /// <summary>
        /// cache the sequence id;
        /// </summary>
        static long s_sequenceId = 0;

        /// <summary>
        /// Call <see cref="Init"/>
        /// </summary>
        static Stamp()
        {
            Init();
        }

        /// <summary>
        /// Initialize internal epoch time reference cache, thread safe
        /// </summary>
        public static void Init()
        {
            // init only if not done yet, thread safe

            if (s_ref_sys_time == 0)
                lock (s_sync_root)
                {
                    // Check if someone else was faster
                    if (s_ref_sys_time != 0) return;

                    // First we work with the NOW now, to get precise system start time

                    var now = DateTime.UtcNow;
                    var uptime = GetSystemUpTime();
                    var epoch = DateTime.ParseExact("1970", "yyyy", CultureInfo.InvariantCulture);
                    var espan = now - epoch;

                    s_ref_app_time = ConvertTimeSpanToSeconds(espan);
                    s_ref_sys_time = s_ref_app_time - uptime;

                    // Then try get the actual application start time. The 'NOW now' was possibly the time of the first logging event

                    try
                    {
                        espan = Process.GetCurrentProcess().StartTime.ToUniversalTime() - epoch;
                        s_ref_app_time = ConvertTimeSpanToSeconds(espan);
                    }
                    catch (Exception x)
                    {
#if LOG4NET_1_2_10_COMPATIBLE
                        LogLog.Warn("PropertyTimeStamp.Init() - getting exact app start time failed, but we should be fine with approximate time.", x);
#else
                        LogLog.Warn(typeof(TimeStamp), "PropertyTimeStamp.Init() - getting exact app start time failed, but we should be fine with approximate time.", x);
#endif
                    }

                    try
                    {
                        s_processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    }
                    catch (Exception x)
                    {
#if LOG4NET_1_2_10_COMPATIBLE
                        LogLog.Warn("PropertyTimeStamp.Init() - getting process id failed, leaving -1.", x);
#else
                        LogLog.Warn(typeof(TimeStamp), "PropertyTimeStamp.Init() - getting process id failed, leaving -1.", x);
#endif
                        s_processId = -1;
                    }
                }
        }

        /// <summary>
        /// Get the cached process ID
        /// </summary>
        /// <returns>process id</returns>
        public static int GetProcessId()
        {
            return s_processId;
        }

        /// <summary>
        /// Get a statically incremented number in a thread safe manner
        /// </summary>
        /// <returns>sequence number</returns>
        public static long GetSequence()
        {
            return Interlocked.Increment(ref s_sequenceId);
        }

        /// <summary>
        /// Set a statically incremented number in a thread safe manner
        /// </summary>
        /// <returns>sequence number</returns>
        public static long SetSequence(long value)
        {
            return Interlocked.Exchange(ref s_sequenceId, value);
        }

        /// <summary>
        /// Utility method returns current time in seconds since system start using the <see cref="Stopwatch.GetTimestamp"/>
        /// </summary>
        /// <returns>seconds</returns>
        public static double GetSystemUpTime()
        {
            var ticks = Stopwatch.GetTimestamp();
            var seconds = ConvertStopwatchTicksToSeconds(ticks);
            return seconds;
        }

        /// <summary>
        /// Utility method converts ticks given by <see cref="Stopwatch"/> to seconds
        /// </summary>
        /// <param name="ticks">Stopwatch ticks</param>
        /// <returns>seconds</returns>
        public static double ConvertStopwatchTicksToSeconds(long ticks)
        {
            return ((double)ticks) / Stopwatch.Frequency;
        }

        /// <summary>
        /// Utility method converts ticks given by <see cref="TimeSpan"/> to seconds
        /// </summary>
        /// <param name="span">TimeSpan</param>
        /// <returns>seconds</returns>
        public static double ConvertTimeSpanToSeconds(TimeSpan span)
        {
            return ((double)span.Ticks) / TimeSpan.TicksPerSecond;
        }

        /// <summary>
        /// Get the epoch time requested in seconds
        /// </summary>
        /// <param name="ageRef"></param>
        /// <returns>seconds since epoch 1970</returns>
        public static double GetEpochTime(AgeReference ageRef)
        {
            switch (ageRef)
            {
                case AgeReference.Now: 
                    var uptime = GetSystemUpTime();
                    return s_ref_sys_time + uptime;
                case AgeReference.Epoch1970: return 0;
                case AgeReference.SystemStart: return s_ref_sys_time;
                case AgeReference.ApplicationStart: return s_ref_app_time;
                default:
#if LOG4NET_1_2_10_COMPATIBLE
                    LogLog.Error(String.Format("AgeReference not implemented: {0}", ageRef));
#else
                    LogLog.Error(typeof(Stamp), String.Format("AgeReference not implemented: {0}", ageRef));
#endif
                    return long.MinValue;
            }
        }

        /// <summary>
        /// Get the time elapsed between tfrom and tto adjusted by multiplier and potentially rounded
        /// </summary>
        /// <param name="tfrom">start of time span definition</param>
        /// <param name="tto">end of timespan definition</param>
        /// <param name="multiplier">give 1000000 to get microseconds; 1.0/24/3600 to get days</param>
        /// <param name="round">Round to a whole number</param>
        /// <returns>adjusted time value</returns>
        public static double GetTimeStampValue(AgeReference tfrom, AgeReference tto, double multiplier, bool round)
        {
            var timeFrom = GetEpochTime(tfrom);
            var timeTo = GetEpochTime(tto);
            var value = AdjustTimeValue(timeTo - timeFrom, multiplier, round);
            return value;
        }

        /// <summary>
        /// Adjust time value - Multiply and Round
        /// </summary>
        /// <param name="value">epoch time value</param>
        /// <param name="multiplier"> 1,000,000 to get microseconds</param>
        /// <param name="round">Round to a whole number</param>
        /// <returns>adjusted value</returns>
        public static double AdjustTimeValue(double value, double multiplier, bool round)
        {
            if (multiplier != 0 && multiplier != 1)
            {
                value = value * multiplier;
            }

            if (round)
            {
                value = Math.Round(value);
            }

            return value;
        }
        #endregion Statics

    }
}
