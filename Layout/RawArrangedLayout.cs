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
using log4net.Layout.Members;

namespace log4net.Layout
{
    /// <summary>
    /// This <see cref="IRawLayout"/> facilitates arranged members retrieval 
    /// in the form of a <see cref="Dictionary&lt;String,Object>"/>.
    /// </summary>
    /// <remarks>
    /// This is meant to be used as a <see cref="Pattern.JsonPatternConverter.Fetcher"/>
    /// </remarks>
    /// <author>Robert Sevcik</author>
    public class RawArrangedLayout : IRawLayout, IRawArrangedLayout
    {
        /// <summary>
        /// The <see cref="IMember"/>s to be put in a dictionary
        /// </summary>
        public IList<IMember> Members { get; set; }

        /// <summary>
        /// Create instance and set Members to an empty list
        /// </summary>
        public RawArrangedLayout()
        {
            Members = new List<IMember>();
        }

        /// <summary>
        /// Gather the <see cref="Members"/> in a dictionary
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns>dictionary of members</returns>
        public virtual Object Format(LoggingEvent loggingEvent)
        {
            // the dictionary to be serialized in JSON or other

            if (Members.Count == 0)
                return loggingEvent.RenderedMessage;

            if (Members.Count == 1 && Members[0].Name == String.Empty)
                return Members[0].Layout.Format(loggingEvent);

            var dic = new Dictionary<string, object>(Members.Count);

            foreach (var member in Members)
            {
                var value = member.Layout.Format(loggingEvent);

                // ignore nulls
                if (value != null)
                    dic[member.Name] = value;
            }

            return dic;
        }
    }
}
