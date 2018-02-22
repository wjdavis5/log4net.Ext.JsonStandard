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
using System.Collections;
using log4net.Core;
using log4net.Layout.Arrangements;
using log4net.Layout.Members;
using log4net.Layout.Pattern;
using log4net.Util;
using log4net.Util.TypeConverters;
using System.Collections.Generic;
using log4net.Layout.Decorators;
using log4net.ObjectRenderer;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout
{
    /// <summary>
    /// Enable an external serializer (JSON) to participate in PaternLayout 
    /// with variable member configuration using <see cref="IArrangement"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The goal of this class is to serialize a <see cref="log4net.Core.LoggingEvent"/> 
    /// as a string. The results depend on the <i>Members</i> organized by <see cref="IArrangement"/>s.
    /// </para>
    /// <para>
    /// Custom <i>renderer</i> and <i>fetcher</i> can be provided if the default 
    /// <see cref="JsonPatternConverter"/> is used or another implementation 
    /// follows this convention:
    /// 
    /// * log4net property: renderer, type <see cref="log4net.ObjectRenderer.IObjectRenderer" />
    /// * log4net property: fetcher, type <see cref="log4net.Layout.IRawLayout" />
    /// </para>
    /// <para>
    /// Collected <i>arrangements</i> and <i>converters</i> are also passed as properties and used in 
    /// <see cref="JsonPatternConverter"/>:
    /// 
    /// * log4net property: arrangement, type <see cref="log4net.Layout.Arrangements.IArrangement" />
    /// * log4net property: converters, type <see cref="ConverterInfo" />[]
    /// </para>
    /// <para>
    /// This class is not concerned with how the data is rendered. It only provides a configuration shortcut
    /// to organize members into structures suitable for JSON serialization. Serialization is then performed
    /// by a PatternConverter, the <see cref="JsonPatternConverter"/> by default.
    /// </para>
    /// </remarks>
    /// <example>
    /// You can use a default configuration. Note that default default default is used only when no other arrangements exist.
    /// 
    /// * to use the default default members:
    /// 
    ///         <default />
    ///       - it is equivalent to leaving Layout without any arrangements. In that case the defaults are implied.
    /// 
    /// * to use the default members suitable for nxlog:
    ///     
    ///         <default value="nxlog" />
    /// 
    /// </example>
    /// <example>
    /// You can use member configurations:
    /// 
    /// * to use a default before any custom members: 
    /// 
    ///         <default />
    ///         <member value="ProcessID" />
    ///         <member value="AppName:appdomain" />
    /// 
    /// </example>
    /// <example>
    /// You can use the pattern configuration to allow simple configurations of complex requirements:
    /// 
    /// * to add a member with custom name:
    /// 
    ///         <arrangement value="MyOwnMember:appdomain" />
    /// * to render members using <see cref="PatternLayout"/>:
    /// 
    ///         <arrangement value="Day|It is %date{dddd} today" />
    /// * to add nested members (note the \;):
    /// 
    ///         <arrangement value="Host=Name:hostname\;ProcessId\;Memory\;timestamp" />
    /// * to add any custom arrangement:
    /// 
    ///         <arrangement value="log4net.Layout.Arrangements.RemovalArrangement!" />
    /// * to add any custom arrangement with an option:
    /// 
    ///         <arrangement value="log4net.Layout.Arrangements.RemovalArrangement!Message" />
    /// * to run a <see cref="PatternLayout"/> converter with an option (useful more in conversionPattern):
    /// 
    ///         <arrangement value="Month%date:MMM" />
    /// * to add a default arrangement:
    /// 
    ///         <arrangement value="DEFAULT!nxlog" />
    /// * to add remove all members:
    /// 
    ///         <arrangement value="CLEAR" />
    /// * to add remove specific members matching Regex pattern:
    /// 
    ///         <arrangement value="REMOVE!Source.*" />
    /// 
    /// </example>
    /// <example>
    /// You can remove members from default:
    /// 
    ///         <default />
    ///         <remove value="message" />
    ///         <arrangement value="data:message" />
    /// 
    /// </example>
    /// <example>
    /// You can also use the <see cref="PatternLayout.ConversionPattern"/> configurations:
    /// 
    /// * to use the default members suitable for nxlog, username and hostname:
    /// 
    ///         <conversionPattern value="DEFAULT!nxlog;UserName;HostName" />
    ///         
    /// * to use the <see cref="PatternLayout"/> style
    /// 
    ///         <conversionPattern value="%d ... %serialize ..." />
    /// </example>
    /// <author>Robert Sevcik</author>
    public class SerializedLayout : PatternLayout
    {
        #region Static defaults and initialization

        /// <summary>
        /// This is the default serializing pattern converter name.
        /// Destination: <see cref="SerializerName"/>
        /// </summary>
        public static string DefaultSerializerName = "serialize";

        /// <summary>
        /// Static constructor to initialize the environment - calls <see cref="ArrangementConverter.Init"/>.
        /// </summary>
        static SerializedLayout()
        {
            ArrangementConverter.Init();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The name to use for the serializing conversion pattern
        /// </summary>
        public String SerializerName { get; set; }

        /// <summary>
        /// The serializer used to <see cref="Format"/> the <see cref="LoggingEvent"/>
        /// </summary>
        public PatternConverter SerializingConverter { get; set; }

        #endregion

        #region Internal fields

        /// <summary>
        /// Keep the collected arrangements here, pass them to the serializing pattern converter
        /// </summary>
        protected readonly MultipleArrangement m_arrangement = new MultipleArrangement();

        /// <summary>
        /// decorators to pass to the serializing pattern converter
        /// </summary>
        protected readonly List<IDecorator> m_decorators = new List<IDecorator>();

        /// <summary>
        /// renderer to pass to the serializing pattern converter
        /// </summary>
        protected IObjectRenderer m_renderer = null;

        /// <summary>
        /// fetcher to pass to the serializing pattern converter
        /// </summary>
        protected IRawLayout m_fetcher = null;

        /// <summary>
        /// FIXME: Who knows why the parrent class calls ActivateOptions() from constructor?
        /// It seems unnecessary and causes issues here. We use this field to 
        /// suspend the call to ActivateOptions() from constructor
        /// </summary>
        private bool m_constructed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an JsonLayout with empty <i>Members</i>, no <i>Style</i>, and default <i>serializer</i>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default just produces an empty JSON object string.
        /// </para>
        /// <para>
        /// As per the <see cref="log4net.Core.IOptionHandler"/> contract the <see cref="ActivateOptions"/>
        /// method must be called after the properties on this object have been
        /// configured.
        /// </para>
        /// </remarks>
        public SerializedLayout()
            : base(String.Empty)
        {
            // exception can be rendered so we do not ignore exceptions
            // note: when this was true (default) AppenderSkeleton.RenderLoggingEvent 
            // would add the exception, which is invalid in JSON context
            IgnoresException = false;

            SerializerName = DefaultSerializerName;

            // now we can allow ActivateOptions()
            m_constructed = true;
        }

        #endregion

        #region Implementation of IOptionHandler, override of PatternLayout

        /// <summary>
        /// Activate the options that were previously set with calls to properties.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This allows an object to defer activation of its options until all
        /// options have been set. This is required for components which have
        /// related options that remain ambiguous until all are set.
        /// </para>
        /// <para>
        /// If a component implements this interface then this method must be called
        /// after its properties have been set before the component can be used.
        /// </para>
        /// <para>
        /// The strange constructor call to this method is suspended using 
        /// <see cref="m_constructed"/>.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            if (!m_constructed) return;

            // pass control to parent in case we do not get a serializer :o[
            base.ActivateOptions();

            // just to get those converters
            var parser = CreatePatternParser(String.Empty);

#if LOG4NET_1_2_10_COMPATIBLE
            int convord = 0;
            var converters = new ConverterInfo[parser.PatternConverters.Count];
            foreach (DictionaryEntry entry in parser.PatternConverters)
            {
                converters[convord++] = new ConverterInfo()
                {
                    Name = Convert.ToString(entry.Key),
                    Type = (Type)entry.Value
                };
            }
#else
            // Extract discovered converters
            var converters = Enumerable.ToArray(
                                Enumerable.Cast<ConverterInfo>(
                                    parser.PatternConverters.Values
                                )
                             );
#endif

            var arrangement = new MultipleArrangement();

            if (m_arrangement.Arrangements.Count != 0)
                arrangement.AddArrangement(m_arrangement);

            var patternArrangement = ArrangementConverter.GetArrangement(ConversionPattern, converters);
            if (patternArrangement != null)
                arrangement.AddArrangement(patternArrangement);

            if (arrangement.Arrangements.Count == 0)
            {
                // cater for bare defaults
                arrangement.AddArrangement(new DefaultArrangement());
            }

            var serconv = SerializingConverter;

            if (serconv == null)
            {
                var name = SerializerName ?? DefaultSerializerName;
                var info = (parser.PatternConverters.ContainsKey(name)
                                ? parser.PatternConverters[name] as ConverterInfo
                                : null
                            ) ?? CreateSerializingConverterInfo(name, typeof(JsonPatternConverter));

                SerializingConverter = serconv = CreateSerializingConverter(info);
            }

            if (serconv != null)
                SetUpSerializingConverter(serconv, converters, arrangement, m_fetcher, m_renderer, m_decorators.ToArray());
        }

        #endregion

        #region Override of PatternLayout

        /// <summary>
        /// Produces a formatted string as specified by the SerializingConverter.
        /// </summary>
        /// <param name="loggingEvent">the event being logged</param>
        /// <param name="writer">The TextWriter to write the formatted event to</param>
        /// <remarks>
        /// If SerializingConverter is not set, we default to base implementation.
        /// </remarks>
        public override void Format(System.IO.TextWriter writer, LoggingEvent loggingEvent)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (loggingEvent == null)
            {
                throw new ArgumentNullException("loggingEvent");
            }

            if (SerializingConverter == null)
                base.Format(writer, loggingEvent);
            else
                SerializingConverter.Format(writer, loggingEvent);
        }

        #endregion

        #region Internal methods


        /// <summary>
        /// Fetch our own <see cref="PatternConverter"/> SerializingConverter.
        /// </summary>
        /// <param name="info">description of the PatternConverter</param>
        /// <returns>pattern converter set up</returns>
        /// <remarks>
        /// <para>
        /// Please note that properties are only supported with log4net 1.2.11 and above.
        /// </para>
        /// </remarks>
        protected virtual PatternConverter CreateSerializingConverter(ConverterInfo info)
        {
            var conv = info.Type == null ? null : Activator.CreateInstance(info.Type) as PatternConverter;
            if (conv == null) conv = new JsonPatternConverter();

#if !LOG4NET_1_2_10_COMPATIBLE
            conv.Properties = info.Properties;
#endif

            return conv;
        }

        /// <summary>
        /// Add <see cref="PatternConverter.Properties"/> or make use of <see cref="ISerializingPatternConverter.SetUp"/>, 
        /// call <see cref="IOptionHandler.ActivateOptions"/> 
        /// </summary>
        /// <param name="conv">serializer to be set up, see also <seealso cref="ISerializingPatternConverter"/></param>
        /// <param name="converters">converters to be used collected from parent class</param>
        /// <param name="arrangement">arrangement to be used collected from parent class</param>
        /// <param name="fetcher">fetcher to use</param>
        /// <param name="renderer">renderer to use</param>
        /// <param name="decorators">decorators to use</param>
        /// <remarks>
        /// <para>
        /// Please note that properties are only supported with log4net 1.2.11 and above.
        /// </para>
        /// </remarks>
        protected virtual void SetUpSerializingConverter(PatternConverter conv, ConverterInfo[] converters, IArrangement arrangement, IRawLayout fetcher, IObjectRenderer renderer, IDecorator[] decorators)
        {
            var serializedConv = conv as ISerializingPatternConverter;

            if (serializedConv != null)
            {
                serializedConv.SetUp(arrangement, converters, fetcher, renderer, decorators);
            }
#if !LOG4NET_1_2_10_COMPATIBLE
            else
            {
                LogLog.Warn(GetType(), String.Format("Converter is not a ISerializingPatternConverter: {0}. Passing fetcher, renderer, decorators, arrangement and converters as properties.", conv));
                conv.Properties["arrangement"] = arrangement;
                conv.Properties["converters"] = converters;
                conv.Properties["fetcher"] = fetcher;
                conv.Properties["renderer"] = renderer;
                conv.Properties["decorators"] = decorators;
            }
#else
            else
            {
                LogLog.Error(String.Format("Converter is not a ISerializingPatternConverter: {0}. Since converter properties are not supported before 1.2.11, no fetcher, renderer, decorators or arrangements can be passed. You can still use the converter option with PatternLayout.", conv));
            }
#endif

            IOptionHandler optionHandler = conv as IOptionHandler;
            if (optionHandler != null)
            {
                optionHandler.ActivateOptions();
            }
        }

        /// <summary>
        /// Instantiate our own SerializingConverter info
        /// </summary>
        /// <remarks>
        /// <see cref="SerializerName"/> property
        /// </remarks>
        /// <returns>the info created</returns>
        /// <exception cref="InvalidOperationException">for invalid types see <see cref="PatternConverter"/> abstract class.</exception>
        protected virtual ConverterInfo CreateSerializingConverterInfo(string name, Type type)
        {
            return new ConverterInfo() { Name = name, Type = type };
        }

        #endregion

        #region Configuration methods

        /// <summary>
        /// Add an arbitrary <see cref="IArrangement"/>. 
        /// This method will be most useful for XML configuration.
        /// </summary>
        /// <param name="value">the arrangement</param>
        public virtual void AddArrangement(IArrangement value)
        {
            m_arrangement.AddArrangement(value);
        }

        /// <summary>
        /// Add an <see cref="DefaultArrangement"/> that can be plain pattern string.
        /// This method will be most useful for XML configuration.
        /// </summary>
        /// <param name="value">the arrangement</param>
        public virtual void AddDefault(string value)
        {
            value = "DEFAULT!" + value;
            var arrangement = log4net.Util.TypeConverters.ArrangementConverter.GetArrangement(value, new ConverterInfo[0]);
            m_arrangement.AddArrangement(arrangement);
        }

        /// <summary>
        /// Add a single <see cref="Member"/> that can be plain pattern string. 
        /// Note that <see cref="Member"/> implements <see cref="IArrangement"/> as well.
        /// This method will be most useful for XML configuration.
        /// </summary>
        /// <param name="value">the member</param>
        public virtual void AddMember(string value)
        {
            var arrangement = log4net.Util.TypeConverters.ArrangementConverter.GetArrangement(value, new ConverterInfo[0]);
            m_arrangement.AddArrangement(arrangement);
        }

        /// <summary>
        /// With <see cref="RemovalArrangement"/> remove all or 
        /// <seealso cref="System.Text.RegularExpressions.Regex"/> specific members.
        /// This method will be most useful for XML configuration.
        /// </summary>
        /// <param name="value">the removal</param>
        public virtual void AddRemove(string value)
        {
            value = "REMOVE!" + value;
            var arrangement = log4net.Util.TypeConverters.ArrangementConverter.GetArrangement(value, new ConverterInfo[0]);
            m_arrangement.AddArrangement(arrangement);
        }

        /// <summary>
        /// Add renderer to be passed to serializing pattern converter
        /// </summary>
        /// <param name="value">renderer</param>
        /// <remarks>
        /// This method will be most useful for XML configuration.
        /// </remarks>
        public virtual void AddRenderer(IObjectRenderer value)
        {
            m_renderer = value;
        }

        /// <summary>
        /// Add fetcher to be passed to serializing pattern converter
        /// </summary>
        /// <param name="value">fetcher</param>
        /// <remarks>
        /// This method will be most useful for XML configuration.
        /// </remarks>
        public virtual void AddFetcher(IRawLayout value)
        {
            m_fetcher = value;
        }

        /// <summary>
        /// Add decorators to be passed to serializing pattern converter
        /// </summary>
        /// <param name="value">one decorator</param>
        /// <remarks>
        /// This method will be most useful for XML configuration.
        /// </remarks>
        public virtual void AddDecorator(IDecorator value)
        {
            m_decorators.Add(value);
        }

        #endregion

    }
}
