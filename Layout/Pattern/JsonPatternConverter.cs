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
using log4net.Layout.Arrangements;
using log4net.Layout.Members;
using log4net.ObjectRenderer;
using log4net.Util;
using log4net.Util.TypeConverters;
using log4net.Util.Serializer;
using log4net.Layout.Decorators;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Pattern
{
    /// <summary>
    /// Render the <see cref="Member"/>s using <see cref="Renderer"/> or using <see cref="RendererMap.FindAndRender(object, TextWriter)" />.
    /// Log4net properties can set (likely through <see cref="ConverterInfo"/>) to provide custom 
    /// <see cref="Renderer"/>, <see cref="Fetcher"/> and to pass <see cref="IArrangement" /> 
    /// and <see cref="ConverterInfo"/>s from hosting layout.
    /// Option can be set to introduce an additional <see cref="IArrangement" />
    /// Use it in a custom <see cref="PatternLayout.ConversionPattern" /> like this: "%serialize{DEFAULT;PID:processid}"
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class JsonPatternConverter : PatternLayoutConverter, IObjectRenderer, IOptionHandler, ISerializingPatternConverter
    {
        #region Properties

        /// <summary>
        /// How to render the members is decided here. By default it is a <see cref="JsonObjectRenderer.Default"/>
        /// </summary>
        public IObjectRenderer Renderer { get; set; }

        /// <summary>
        /// What to render is decided here. By default it is a <see cref="RawArrangedLayout"/> 
        /// and it's members can be arranged - see <see cref="ActivateOptions"/>
        /// </summary>
        public IRawLayout Fetcher { get; set; }

        /// <summary>
        /// members can be modified by decorators before they are serialized
        /// </summary>
        /// <remarks>
        /// This is used to unite different JSON serializers for instance in <see cref="StandardTypesDecorator"/>
        /// </remarks>
        public IDecorator[] Decorators { get; set; }

        #endregion

        /// <summary>
        /// Create instance with a default <see cref="Fetcher" /> and <see cref="Renderer" />
        /// </summary>
        public JsonPatternConverter()
        {
#if !LOG4NET_1_2_10_COMPATIBLE
            this.Properties = new PropertiesDictionary();
#endif

            // we're not going to bother, user decides where the exception will go.
            this.IgnoresException = false;
        }

        #region PatternLayoutConverter override implementation

        /// <summary>
        /// Render an object which will most likely be a <see cref="LoggingEvent"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="state"></param>
        protected override void Convert(TextWriter writer, object state)
        {
            if (state != null && state is LoggingEvent)
            {
                Convert(writer, (LoggingEvent)state);
            }
            else
            {
                RenderObject(null, state, writer);
            }
        }



        /// <summary>
        /// Render what comes from the  <see cref="Fetcher" /> using  <see cref="Renderer" /> or default renderer.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
        /// <param name="loggingEvent">The <see cref="LoggingEvent" /> on which the pattern converter should be executed.</param>
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            var obj = Fetcher.Format(loggingEvent);
            var map = loggingEvent.Repository.RendererMap;
            RenderObject(map, obj, writer);
        }

        #endregion

        #region IObjectRenderer implementation

        /// <summary>
        /// Render <paramref name="obj"/> into the <paramref name="writer"/>
        /// </summary>
        /// <param name="map">from <see cref="LoggingEvent.Repository"/></param>
        /// <param name="obj">value to be rendered</param>
        /// <param name="writer">writer to write obj to</param>
        public void RenderObject(RendererMap map, object obj, TextWriter writer)
        {
            foreach (var decorator in Decorators)
                obj = decorator.Decorate(obj);

            var renderer = Renderer
                ?? (map == null ? null : map.Get(obj))
                ?? JsonObjectRenderer.Default ?? map.DefaultRenderer;

            renderer.RenderObject(map, obj, writer);
        }

        #endregion

        #region IOptionHandler implementation

        /// <summary>
        /// Activate the options that were previously set with calls to properties.
        /// <see cref="Renderer"/> of type <see cref="IObjectRenderer"/> is taken from Properties["renderer"] if present.
        /// <see cref="Fetcher"/> of type <see cref="IRawLayout"/> is taken from Properties["fetcher"] if present
        /// <see cref="IArrangement"/> is taken from Properties["arrangement"] and from <i>option</i>.
        /// Converters to be used in arrangements are taken from Properties["converters"], an array of <see cref="ConverterInfo"/>.
        /// Members are arranged using <see cref="SetUp"/>
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
        /// Please note that properties are only supported with log4net 1.2.11 and above.
        /// </para>
        /// </remarks>
        public virtual void ActivateOptions()
        {
#if LOG4NET_1_2_10_COMPATIBLE
            var renderer = null as IObjectRenderer;
            var fetcher = null as IRawLayout;
            var decorators = null as IEnumerable<IDecorator>;
            var converters = null as ConverterInfo[];
            var arrangement = null as IArrangement;
#else
            var renderer = Properties["renderer"] as IObjectRenderer;
            var fetcher = Properties["fetcher"] as IRawLayout;
            var decorators = Properties["decorators"] as IEnumerable<IDecorator>;
            var converters = Properties["converters"] as IEnumerable<ConverterInfo>;
            var arrangement = Properties["arrangement"] as IArrangement;
#endif

            SetUp(arrangement, converters, fetcher, renderer, decorators);

            if (!String.IsNullOrEmpty(Option))
            {
                var convertersArray = converters == null ? null : Enumerable.ToArray(converters);
                var optarrangement = ArrangementConverter.GetArrangement(Option, convertersArray);
                Arrange(optarrangement, converters);
            }
        }

        /// <summary>
        /// Arrange <see cref="Fetcher"/>'s members if possible, if it is an <see cref="IRawArrangedLayout"/>.
        /// </summary>
        /// <param name="arrangement">arangement to use, can be null</param>
        /// <param name="converters">converters to consider, can be null</param>
        /// <param name="fetcher">fetches an object from a logging event</param>
        /// <param name="renderer">serializes the object</param>
        /// <param name="decorators">decorates the object before serialization</param>
        public virtual void SetUp(IArrangement arrangement, IEnumerable<ConverterInfo> converters, IRawLayout fetcher, IObjectRenderer renderer, IEnumerable<IDecorator> decorators)
        {
            var decoratorsArray = decorators == null ? null : Enumerable.ToArray(decorators);
            Fetcher = fetcher ?? Fetcher ?? CreateFetcher();
            Decorators = decoratorsArray ?? Decorators ?? CreateDecorators();
            Renderer = renderer ?? Renderer ?? CreateRenderer();
            Arrange(arrangement, converters);
        }

        /// <summary>
        /// Arrange members
        /// </summary>
        /// <param name="arrangement">by arrangement</param>
        /// <param name="converters">with converters</param>
        protected virtual void Arrange(IArrangement arrangement, IEnumerable<ConverterInfo> converters)
        {
            var convertersArray = converters == null ? null : Enumerable.ToArray(converters);

            var arrangedFetcher = Fetcher as IRawArrangedLayout;

            if (arrangedFetcher != null && arrangement != null)
            {
                arrangement.Arrange(arrangedFetcher.Members, convertersArray);
            }
        }


        /// <summary>
        /// Give us our default <see cref="Fetcher"/>
        /// </summary>
        /// <returns>fetcher</returns>
        protected virtual IRawLayout CreateFetcher()
        {
            return new RawArrangedLayout();
        }

        /// <summary>
        /// Give us our default <see cref="Decorators"/>
        /// </summary>
        /// <returns>fetcher</returns>
        protected virtual IDecorator[] CreateDecorators()
        {
            return new IDecorator[0];
        }

        /// <summary>
        /// Give us our default <see cref="Renderer"/>
        /// </summary>
        /// <returns>renderer</returns>
        protected virtual IObjectRenderer CreateRenderer()
        {
            return JsonObjectRenderer.Default;
        }

        #endregion
    }
}
