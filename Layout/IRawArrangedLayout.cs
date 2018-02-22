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
using log4net.Layout.Pattern;

namespace log4net.Layout
{
    /// <summary>
    /// An interface recognized by <see cref="JsonPatternConverter"/> to apply arrangements on it's <see cref="JsonPatternConverter.Fetcher" />
    /// </summary>
    /// <author>Robert Sevcik</author>
    public interface IRawArrangedLayout
    {
        /// <summary>
        /// Serialized members to be arranged
        /// </summary>
        IList<IMember> Members { get; set; }
    }
}
