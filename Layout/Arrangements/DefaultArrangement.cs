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
using log4net.Layout.Members;
using log4net.Util.TypeConverters;
using log4net.Util;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Arrangements
{
    /// <summary>
    /// This <see cref="IArrangement"/> will put together few most obvious values as defaults.
    /// These <see cref="ConfigDefaults"/> are the options recognized by <see cref="ArrangementConverter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If no other arrangement is set for the <see cref="SerializedLayout"/> it will add a default default by default.
    /// </para>
    /// <para>
    /// It is used by <see cref="SerializedLayout.AddDefault"/> to allow simple xml configuration 
    /// &lt;default value="nxlog" /&gt; or simply &lt;default /&gt;.
    /// </para>
    /// <para>
    /// It is used by <see cref="ArrangementConverter" /> to represent "DEFAULT:nxlog" or simply "DEFAULT" 
    /// in the serialize conversion pattern option.
    /// </para>
    /// <para>
    /// The arrangement is actually done by the base <see cref="OptionArrangement"/> implementation.
    /// </para>
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class DefaultArrangement : OptionArrangement
    {
        /// <summary>
        /// This is the default <see cref="Default"/> containing "default" :o)
        /// </summary>
        public const string DefaultDefaultDefault = "default";

        #region Static defaults

        /// <summary>
        /// A dictionary of default options which are recognized by <see cref="ArrangementConverter"/>
        /// </summary>
        public static IDictionary<string, string> ConfigDefaults
                = new Dictionary<string, string>()
                    {
                        {"default",
                            "date;"
                            + "level;"
                            + "appname;"
                            + "logger;"
                            + "thread;"   
                            + "ndc|%ndc;"  
                            + "message;"
                            + "exception;"                         
                        },
                        {"nxlog",
                            "EventTime:date;"
                            + "Severity:level;"
                            + "SourceName:appname;"
                            + "Logger;"
                            + "Thread;"  
                            + "NDC|%ndc;"  
                            + "Message;"
                            + "Exception;"                                                               
                        }
                    };

        #endregion

        #region Properties

        /// <summary>
        /// Default option of <see cref="Config"/>
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Default values configuration
        /// </summary>
        public IDictionary<string, string> Config { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Create an instance with <see cref="Default"/> = "default".
        /// Copy static <see cref="ConfigDefaults"/> dictionary to <see cref="Config"/>
        /// </summary>
        public DefaultArrangement()
            : this(null)
        {
        }

        /// <summary>
        /// Create an instance with specific <see cref="Default"/>.
        /// Copy static <see cref="ConfigDefaults"/> dictionary to <see cref="Config"/>
        /// </summary>
        public DefaultArrangement(string def)
        {
            Default = String.IsNullOrEmpty(def) ? DefaultDefaultDefault : def;
            Config = new Dictionary<string, string>(ConfigDefaults);
        }

        #endregion

        #region Implementation of IArrangement, OptionArrangement overrides

        /// <summary>
        /// This implementation will pick a <see cref="Default"/> from <see cref="Config"/>
        /// and call the base <see cref="OptionArrangement"/> implementation on that
        /// </summary>
        /// <exception cref="Exception">When the <see cref="Default"/> is not found in <see cref="Config"/></exception>
        /// <param name="members">Members to be arranged</param>
        /// <param name="converters">Converter infos to pass to child arrangements</param>
        public override void Arrange(IList<IMember> members, ConverterInfo[] converters)
        {
            var config = Config;
            string def = Default;
            string arrangement = null;

            if (config == null || config.Count == 0)
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error(String.Format("No defaults are available in this.Config. Default requested: '{0}'", def));
#else
                LogLog.Error(GetType(), String.Format("No defaults are available in this.Config. Default requested: '{0}'", def));
#endif
                return;
            }

            if (def != null && def != DefaultDefaultDefault && !Config.TryGetValue(def, out arrangement))
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error(String.Format("Defaults not found for: '{0}'. Default defaults will be used.", def));
#else
                LogLog.Error(GetType(), String.Format("Defaults not found for: '{0}'. Default defaults will be used.", def));
#endif
                def = DefaultDefaultDefault;
            }

            if (arrangement == null && !Config.TryGetValue(def, out arrangement))
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error(String.Format("Defaults not found for: '{0}'. First available defaults will be used.", def));
#else
                LogLog.Error(GetType(), String.Format("Defaults not found for: '{0}'. First available defaults will be used.", def));
#endif
                foreach (var kvp in config)
                {
                    arrangement = kvp.Value;
                    break;
                }
            }

            // update base option
            base.SetOption(arrangement);

            // actuall arrangement is done by the base implementation
            base.Arrange(members, converters);
        }

        /// <summary>
        /// Chose the <see cref="Default"/> of <see cref="Config"/>
        /// </summary>
        /// <remarks>
        /// base.SetOption(value) is called from <see cref="Arrange"/>
        /// </remarks>
        /// <param name="value">Config dictionary key</param>
        public override void SetOption(string value)
        {
            Default = value;
        }

        #endregion

    }
}
