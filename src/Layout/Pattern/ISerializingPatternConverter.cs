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

using System.Collections.Generic;
using log4net.Layout.Arrangements;
using log4net.Layout.Decorators;
using log4net.ObjectRenderer;
using log4net.Util;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Pattern
{
    /// <summary>
    /// This interface loosely binds <see cref="SerializedLayout"/>
    /// and it's <see cref="SerializedLayout.SerializingConverter"/>
    /// so that arrangement can be passed efficiently if supported.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public interface ISerializingPatternConverter
    {
        /// <summary>
        /// This interface loosely binds <see cref="SerializedLayout"/>
        /// and it's <see cref="SerializedLayout.SerializingConverter"/>
        /// so that arrangement can be passed efficiently if supported.
        /// </summary>
        /// <param name="arrangement">arrangement to organize the serialized members, can be null</param>
        /// <param name="converters">converters to pass to arrangements, can be null</param>
        /// <param name="fetcher">fetches an object from a logging event</param>
        /// <param name="renderer">serializes the object</param>
        /// <param name="decorators">decorates the object before serialization</param>
        void SetUp(IArrangement arrangement, IEnumerable<ConverterInfo> converters, IRawLayout fetcher, IObjectRenderer renderer, IEnumerable<IDecorator> decorators);
    }
}
