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

namespace log4net.Util.Stamps
{
    /// <summary>
    /// Set a time since unix epoch number property value on the event. 
    /// </summary>
    /// <remarks>
    /// It is seconds by default. This is a double precision value.
    /// It can be multiplied by <see cref="Multiplier"/> and <see cref="Round"/>ed.
    /// If the resulting value can be represented by a long type, long is returned, otherwise double.
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class TimeStamp : Stamp
    {
        /// <summary>
        /// Round the double value to whole units
        /// </summary>
        public bool Round { get; set; }

        /// <summary>
        /// Change unit by multiplying the default seconds. Give 1000000 to get microseconds; 1.0/24/3600 to get days.
        /// </summary>
        public double Multiplier { get; set; }

        /// <summary>
        /// The point of reference (Unix epoch (default), system start or application start)
        /// </summary>
        public AgeReference TimeFrom { get; set; }

        /// <summary>
        /// The point of reference (Unix epoch (default), system start or application start)
        /// </summary>
        public AgeReference TimeTo { get; set; }

        /// <summary>
        /// Create instance by default stamping with Now - Epoch1970
        /// </summary>
        public TimeStamp()
        {
            TimeFrom = AgeReference.Epoch1970;
            TimeTo = AgeReference.Now;
        }

        /// <summary>
        /// Create stamp value - a time value calculated from the props
        /// </summary>
        /// <param name="loggingEvent">event to stamp</param>
        /// <returns>value to set as a stamp</returns>
        protected override object GetValue(Core.LoggingEvent loggingEvent)
        {
            return GetTimeStampValue(TimeFrom, TimeTo, Multiplier, Round);
        }


    }
}
