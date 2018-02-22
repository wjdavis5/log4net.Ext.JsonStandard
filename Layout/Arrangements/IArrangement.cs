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
using log4net.Layout.Members;
using log4net.Util;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Arrangements
{

    /// <summary>
    /// Used by <see cref="SerializedLayout"/>, this interface allows the organization of the members to be serialized.
    /// It may be used to simply add or remove members or to do any kinf of magic on the list.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public interface IArrangement
    {
        /// <summary>
        /// Organize the <see cref="IMember"/>s to be serialized
        /// </summary>
        /// <param name="members">Members to be arranged</param>
        /// <param name="converters">inherited converters, can be null</param>
        void Arrange(IList<IMember> members, ConverterInfo[] converters);
        
        /// <summary>
        /// All arrangements can take an option which is handy for XML configuration and to simplify set up.
        /// </summary>
        /// <param name="value">The option specific to the arrangement implementation</param>
        void SetOption(string value);
    }
}
