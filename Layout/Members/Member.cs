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
using log4net.Core;
using log4net.Layout.Arrangements;
using log4net.Util;
using log4net.Util.TypeConverters;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Members
{
    /// <summary>
    /// A common value implementation of INamedValue and IRawLayout for simple configuration.
    /// Some commonly used values should be addressed here with sensible output formatting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to easily configure more complex values. This is achieved by 
    /// specifying an <see cref="Option"/>.
    /// </para>
    /// <para>
    /// This class is also used internally to deliver sensible defaults for values.
    /// </para>
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class Member : NoArrangement, IMember, IRawLayout, IOptionHandler
    {
        #region Properties

        /// <summary>
        /// Name of value to be serialized as.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Option used to configure this object in <see cref="ActivateOptions"/>. 
        /// It should help to figure out the <see cref="IRawLayout"/> to use.
        /// Otherwise Option will be used directly as a value.
        /// </summary>
        public object Option { get; set; }

        /// <summary>
        /// Back reference to "this" for simple configuration.
        /// </summary>
        public IRawLayout Layout { get { return this; } }

        /// <summary>
        /// Converters to pass to descendants
        /// </summary>
        public ConverterInfo[] Converters { get; set; }

        #endregion

        /// <summary>
        /// Create an instance
        /// </summary>
        public Member()
        { 
        }

        #region Implementation of IRawLayout

        /// <summary>
        /// If a value cannot be retrieved from the separate NestedLayout object
        /// a set of known vallues would be tried. If that fails too, UndefinedValue is returned.
        /// </summary>
        /// <param name="loggingEvent">the event to get values for/from</param>
        /// <returns>Object retrieved from logging event</returns>
        public virtual object Format(Core.LoggingEvent loggingEvent)
        {
            object obj;

            return GetLayoutValue(loggingEvent, out obj)
                    || GetPropertyValue(loggingEvent, out obj)
                    || GetDefaultValue(loggingEvent, out obj)
                    ? obj
                    : Option ?? Name
                    ;
        }

        #endregion

        #region IOptionHandler implementation

        /// <summary>
        /// When configured by XML or by <see cref="ArrangementConverter"/> in general,
        /// the <see cref="Option"/> is tried to figure out the <see cref="IRawLayout"/> to use.
        /// </summary>
        /// <remarks>
        /// It can be a <see cref="PatternString" />, then the option will be stringified.
        /// It can be a <see cref="string"/>, then <see cref="ArrangementConverter.GetArrangement"/> will be attempted.
        /// It can be an <see cref="IArrangement"/>, then it will be used to arrange a new <see cref="RawArrangedLayout"/>
        /// It can be another <see cref="IMember" />, then if Name was not set yet it will be adopted.
        /// It can be a <see cref="ConverterInfo" />, then a new RawCallLayout will be set up around it.
        /// It can be a <see cref="PatternParser" />, then a new RawCallLayout will be set up around it.
        /// </remarks>
        public void ActivateOptions()
        {
            if (Option == null)
            {
                Option = GetLayout(Name);
                return;
            }

            if (Option is PatternString)
            {
                var ps = (PatternString)Option;
                Option = ps.Format();
            }

            if (Option is string)
            {
                // try to parse an arrangement
                var arrangement = ArrangementConverter.GetArrangement(Option as string, Converters);
                if (arrangement != null) Option = arrangement;
            }

            if (Option is string)
            {
                // try to find a layout
                var layout = GetLayout(Option as string);
                if (layout != null) Option = layout;
            }

            if (Option is ConverterInfo)
            {
                Option = new RawCallLayout((ConverterInfo)Option);
            }

            if (Option is PatternConverter)
            {
                Option = new RawCallLayout(Name, (PatternConverter)Option);
            }

            if (Option is IOptionHandler)
            {
                // this simplifies calls in ArrangementConverter a lot
                ((IOptionHandler)Option).ActivateOptions();
            }

            if (Option is ILayout)
            {
                var layout = (ILayout)Option;
                var playout = Option as PatternLayout;

                if (playout != null && Converters != null)
                    foreach (var conv in Converters)
                        playout.AddConverter(conv);

                Option = new Layout2RawLayoutAdapter((ILayout)Option);
            }
            else if (Option is IMember)
            {
                var optionMember = (IMember)Option;

                if (String.IsNullOrEmpty(Name))
                    Name = optionMember.Name;

                Option = optionMember.Layout;
            }
            else if (Option is IArrangement)
            {
                var optionArrangemet = (IArrangement)Option;

                var l = new RawArrangedLayout();
                optionArrangemet.Arrange(l.Members, Converters);
                Option = l;
            }

            if (Option is IOptionHandler)
            {
                // do it again if object changed
                ((IOptionHandler)Option).ActivateOptions();
            }
        }

        #endregion

        #region IArrangement implementation, NoArrangement override

        /// <summary>
        /// Add this member to the list.
        /// </summary>
        /// <param name="members">Members to be arrangedFetcher</param>
        /// <param name="converters">ignored</param>
        public override void Arrange(IList<IMember> members, ConverterInfo[] converters)
        {
            members.Add(this);
        }

        /// <summary>
        /// Set the <see cref="Option"/>
        /// </summary>
        /// <param name="value">the option</param>
        public override void SetOption(string value)
        {
            Option = value;
        }

        #endregion

        /// <summary>
        /// Find a matching layout for this Member using known values and converters. 
        /// See <see cref="RawCallLayout.FindLayout(string, ConverterInfo[])"/>
        /// </summary>
        /// <param name="name">member name</param>
        /// <returns>layout found</returns>
        protected virtual IRawLayout GetLayout(string name)
        {
            return RawCallLayout.FindLayout(name, Converters);
        }

        /// <summary>
        /// The NestedLayout is tried to provide a value
        /// </summary>
        /// <param name="loggingEvent">the event to get value from</param>
        /// <param name="obj">value found</param>
        /// <returns>success of finding a NestedLayout not null</returns>
        protected virtual bool GetLayoutValue(Core.LoggingEvent loggingEvent, out object obj)
        {
            var layout = Option as IRawLayout;

            try
            {
                if (layout != null)
                {
                    obj = layout.Format(loggingEvent);
                    return true;
                }
                else
                {
                    obj = null;
                    return false;
                }
            }
            catch (Exception x)
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error("Error getting value from NestedLayout", x);
#else
                LogLog.Error(GetType(), "Error getting value from NestedLayout", x);
#endif
                obj = null;
                return false;
            }
        }

        /// <summary>
        /// The NestedLayout is tried to provide a value
        /// </summary>
        /// <param name="loggingEvent">the event to get value from</param>
        /// <param name="obj">value found</param>
        /// <returns>success of finding a NestedLayout not null</returns>
        protected virtual bool GetDefaultValue(Core.LoggingEvent loggingEvent, out object obj)
        {
            obj = Option ?? Name;
            return true;
        }

        /// <summary>
        /// Try to get a property value with exact match
        /// </summary>
        /// <param name="loggingEvent">the event to get value from</param>
        /// <param name="obj">value found</param>
        /// <returns>success of matching a known value</returns>
        protected virtual bool GetPropertyValue(Core.LoggingEvent loggingEvent, out object obj)
        {
            var name = Option as string ?? Name;

            obj = loggingEvent.LookupProperty(name);

            return obj != null;
        }
    }
}
