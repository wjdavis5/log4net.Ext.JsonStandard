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
using System.IO;
using log4net.Core;
using log4net.Util;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout
{
    /// <summary>
    /// Utility class to facilitate lambda call layout from the code and <see cref="Members.Member"/> configuration.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public sealed class RawCallLayout : IRawLayout
    {
        #region Static goodies

        /// <summary>
        /// A list of standard conversions to be used by <see cref="Members.Member"/>
        /// </summary>
        public static IEnumerable<RawCallLayout> OverrideCalls { get; set; }

        /// <summary>
        /// cache the proc id;
        /// </summary>
        static int s_processId = System.Diagnostics.Process.GetCurrentProcess().Id;

#if CLIENT_PROFILE
        /// <summary>
        /// Cache the website name
        /// </summary>
        static string s_webappname = null;
#else
        /// <summary>
        /// Cache the website name
        /// </summary>
        private static string s_webappname = "";// System.Web.Hosting.HostingEnvironment.SiteName;
#endif

        /// <summary>
        /// Initialize <see cref="OverrideCalls"/>
        /// </summary>
        static RawCallLayout()
        {
            OverrideCalls = MakeOverrideCalls(OverrideCalls);
        }

        #region FindLayout and friends, static utility methods to take away

        /// <summary>
        /// Find an appropriate <see cref="IRawLayout"/> for the specified conversion name using own defaults or <see cref="ConverterInfo"/>s provided.
        /// </summary>
        /// <param name="name">name of conversion</param>
        /// <param name="converters">converters we can use to work out the conversions</param>
        /// <returns>call found</returns>
        public static IRawLayout FindLayout(string name, ConverterInfo[] converters)
        {
            return FindLayout(name, RawCallLayout.GetCalls(converters));
        }

        /// <summary>
        /// Find an appropriate <see cref="RawCallLayout"/> for the specified conversion name among the <see cref="RawCallLayout"/>s provided.
        /// </summary>
        /// <param name="name">name of conversion</param>
        /// <param name="calls">calls to be searched</param>
        /// <returns>call found</returns>
        public static RawCallLayout FindLayout(string name, IEnumerable<RawCallLayout> calls)
        {
            foreach (var cl in calls)
            {
                if (cl.Name.Equals(name, StringComparison.InvariantCulture)
                        || cl.Name.Length != 1
                        && cl.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    ) return cl;
            }
            return null;
        }

        /// <summary>
        /// Get the standard calls enhanced by converters
        /// </summary>
        /// <param name="converters"></param>
        /// <returns>enhanced standard calls</returns>
        public static IEnumerable<RawCallLayout> GetCalls(ConverterInfo[] converters)
        {
            return RawCallLayout.CombineCalls(OverrideCalls, converters);
        }

        /// <summary>
        /// Combine provided converters (first) and the existing calls
        /// </summary>
        /// <param name="calls">existing calls</param>
        /// <param name="converters">converters to include</param>
        public static IEnumerable<RawCallLayout> CombineCalls(IEnumerable<RawCallLayout> calls, ConverterInfo[] converters)
        {
            return Enumerable.Union(calls, EnumerateConverters(converters));
        }

        private static IEnumerable<RawCallLayout> EnumerateConverters(ConverterInfo[] converters)
        {
            if (converters == null) yield break;

            foreach (var conv in converters) yield return new RawCallLayout(conv);
        }

        /// <summary>
        /// Add the standard (in the opinion of this class) conversions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TODO: (Rant) I'm not entirely happy with this monster. 
        /// I wish s_globalRulesRegistry and friends in <seealso cref="PatternLayout"/>  
        /// would better lend themselves to code reuse.
        /// There could be a simple ConvertorCollection class exposing that functionality 
        /// which I'd re-use to create the following matrix with the already declared convertors.
        /// </para>
        /// </remarks>
        /// <param name="calls">calls to add to</param>
        /// <returns>calls added</returns>
        public static IEnumerable<RawCallLayout> MakeOverrideCalls(IEnumerable<RawCallLayout> calls)
        {
            // these calls should grab the same info as log4net would, but try to get it in raw format so that 
            // value is serialized later by a specific serializer.
            RawCallLayout.AddCalls(ref calls, e => e.TimeStamp.ToUniversalTime().ToString("o"), "utcdate", "utcDate", "UtcDate");
            RawCallLayout.AddCalls(ref calls, e => e.TimeStamp.ToString("o"), "date", "d");
            RawCallLayout.AddCalls(ref calls, e => e.Level.DisplayName, "level", "p");
            RawCallLayout.AddCalls(ref calls, e => e.LoggerName, "logger", "c");
            RawCallLayout.AddCalls(ref calls, e => e.ThreadName, "thread", "t");
            RawCallLayout.AddCalls(ref calls, e => e.RenderedMessage, "message", "raw_event"/*custom*/, "m");
            RawCallLayout.AddCalls(ref calls, e => e.MessageObject, "messageobject"/*custom*/, "mo"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => e.ExceptionObject == null ? null : e.GetExceptionString(), "exception", "e"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => e.ExceptionObject, "exceptionobject"/*custom*/, "eo"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => e.Identity, "identity", "u");
            RawCallLayout.AddCalls(ref calls, e => e.UserName, "username", "w");

            RawCallLayout.AddCalls(ref calls, e => e.GetProperties(), "property", "properties", "mdc", "P", "X");

            RawCallLayout.AddCalls(ref calls, e => e.LookupProperty("NDC"), "ndc", "x");
            RawCallLayout.AddCalls(ref calls, e => e.Domain, "appdomain", "a");

            if (s_webappname == null)
            {
                RawCallLayout.AddCalls(ref calls, e => Environment.CommandLine, "apppath" /*custom*/);
                RawCallLayout.AddCalls(ref calls, e => e.Domain, "appname" /*custom*/);
            }
            else
            {
                RawCallLayout.AddCalls(ref calls, e => e.Domain, "apppath" /*custom*/);
                RawCallLayout.AddCalls(ref calls, e => s_webappname, "appname" /*custom*/);
            }

            RawCallLayout.AddCalls(ref calls, e => e.LocationInformation.ClassName, "type", "class", "C");
            RawCallLayout.AddCalls(ref calls, e => e.LocationInformation.FileName, "file", "F");
            RawCallLayout.AddCalls(ref calls, e => e.LocationInformation.FullInfo, "location", "l");
            RawCallLayout.AddCalls(ref calls, e => e.LocationInformation.LineNumber, "line", "L");
            RawCallLayout.AddCalls(ref calls, e => e.LocationInformation.MethodName, "method", "M");

            RawCallLayout.AddCalls(ref calls, e => (e.TimeStamp - LoggingEvent.StartTime).TotalMilliseconds, "timestamp", "r");
            RawCallLayout.AddCalls(ref calls, e => Environment.NewLine, "newline", "n");
            RawCallLayout.AddCalls(ref calls, e => s_processId, "processid"/*custom*/, "pid"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => Environment.MachineName, "hostname"/*custom*/, "h"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => Environment.CommandLine, "commandline"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => Environment.UserName, "user"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => Environment.UserDomainName, "domain"/*custom*/);
            RawCallLayout.AddCalls(ref calls, e => Environment.WorkingSet, "memory"/*custom*/);

            RawCallLayout.AddCalls(ref calls, e => Environment.WorkingSet, "memory"/*custom*/);

            return calls;
        }

        /// <summary>
        /// Make a union of existing calls and new call provided for each name
        /// </summary>
        /// <param name="calls">existing calls</param>
        /// <param name="layoutCall">lambda call providing value for logging event to be turned into <see cref="IRawLayout"/></param>
        /// <param name="name">valid names for the conversion</param>
        public static void AddCalls(ref IEnumerable<RawCallLayout> calls, RawCallDelegate layoutCall, params string[] name)
        {
            var list = EnumerateCalls(layoutCall, name);

            if (calls == null)
                calls = list;
            else
                calls = Enumerable.Union(calls, list);

            calls = Enumerable.ToArray(calls);
        }

        private static IEnumerable<RawCallLayout> EnumerateCalls(RawCallDelegate layoutCall, string[] names)
        {
            foreach (var n in names) yield return new RawCallLayout(n, layoutCall);
        }

        #endregion

        #endregion

        /// <summary>
        /// The name of a conversion
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Function to retrieve a value from a <see cref="LoggingEvent"/>
        /// </summary>
        RawCallDelegate m_getter;

        /// <summary>
        /// If constructed from such, the original <see cref="ConverterInfo"/>
        /// </summary>
        ConverterInfo m_info;

        /// <summary>
        /// Create a named instance from a (lambda) function
        /// </summary>
        /// <param name="name">conversion name</param>
        /// <param name="getter">function</param>
        public RawCallLayout(string name, RawCallDelegate getter)
        {
            Name = name;
            m_getter = getter;
        }

        /// <summary>
        /// Create a named instance from a <see cref="PatternConverter"/> 
        /// </summary>
        /// <param name="name">conversion name</param>
        /// <param name="converter">pattern converter</param>
        public RawCallLayout(string name, PatternConverter converter)
        {
            Name = name;

            if (converter is IOptionHandler)
                ((IOptionHandler)converter).ActivateOptions();

            m_getter = (e) => Format(converter, e);
        }

        /// <summary>
        /// Create a named instance from a <see cref="PatternConverter"/> 
        /// </summary>
        /// <param name="name">conversion name</param>
        /// <param name="pattern">pattern string</param>
        public RawCallLayout(string name, PatternString pattern)
        {
            Name = name;

            pattern.ActivateOptions();

            m_getter = (e) => pattern.Format();
        }

        /// <summary>
        /// Create an instance from a <see cref="ConverterInfo"/>, instantiating it's <see cref="PatternConverter"/>.
        /// </summary>
        /// <remarks>
        /// Properties["option"] (a <see cref="String"/>) can be used to set an option on the converter instance.
        /// </remarks>
        /// <remarks>
        /// Properties are only supported in log4net 1.2.11 and later.
        /// </remarks>
        /// <param name="info"></param>
        public RawCallLayout(ConverterInfo info)
        {
            Name = info.Name;

            var conv = (PatternConverter)Activator.CreateInstance(info.Type);

#if !LOG4NET_1_2_10_COMPATIBLE
            conv.Properties = info.Properties;

            if (info.Properties.Contains("option"))
                conv.Option = Convert.ToString(info.Properties["option"]);
#endif

            if (conv is IOptionHandler)
                ((IOptionHandler)conv).ActivateOptions();

            m_getter = (e) => Format(conv, e);
            m_info = info;
        }

        /// <summary>
        /// Call the getter
        /// </summary>
        /// <param name="loggingEvent">the event to get a value for/from</param>
        /// <returns>the value gotten</returns>
        public object Format(LoggingEvent loggingEvent)
        {
            return m_getter == null ? null : m_getter(loggingEvent);
        }

        /// <summary>
        /// Helper method to call the <see cref="PatternConverter.Format"/>
        /// </summary>
        /// <param name="conv">converter to call</param>
        /// <param name="loggingEvent">event to render</param>
        /// <returns>value retrieved</returns>
        public static Object Format(PatternConverter conv, LoggingEvent loggingEvent)
        {
            var sw = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            conv.Format(sw, loggingEvent);
            return sw.ToString();
        }

        /// <summary>
        /// This is handy when jamming calls together to avoid duplicates. See the <see cref="CombineCalls" /> method.
        /// </summary>
        /// <param name="obj">to compare to</param>
        /// <returns>same</returns>
        public override bool Equals(object obj)
        {
            var us = obj as RawCallLayout;
            return us != null
                && us.Name == Name
                && us.m_info == m_info
                && us.m_getter.ToString() == m_getter.ToString();
        }

        /// <summary>
        /// The compiler complained...
        /// </summary>
        /// <returns>what the base gives</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
