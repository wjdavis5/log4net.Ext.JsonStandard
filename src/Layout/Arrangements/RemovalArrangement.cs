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
using System.Text.RegularExpressions;
using log4net.Layout.Members;
using log4net.Util;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Layout.Arrangements
{
    /// <summary>
    /// This <see cref="IArrangement"/> will just empty the values; either all or those matching a regex option.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class RemovalArrangement : NoArrangement
    {
        /// <summary>
        /// The regular expression used to match member names for removal. If null, all members shall be removed.
        /// </summary>
        public virtual string NameRegex
        {
            get
            {
                return m_nameRegex == null
                    ? null
                    : m_nameRegex.ToString();
            }
            set
            {
                if (value == null)
                {
                    m_nameRegex = null;
                }
                else
                {
                    m_nameRegex = new Regex(value, RegexOptions.Compiled);
                }
            }
        }

        /// <summary>
        /// Parsed <see cref="NameRegex"/>
        /// </summary>
        private Regex m_nameRegex;

        /// <summary>
        /// Create instance without a <see cref="NameRegex" />
        /// </summary>
        public RemovalArrangement()
            : this(null)
        {
        }

        /// <summary>
        /// Create instance with a <see cref="NameRegex" />
        /// </summary>
        public RemovalArrangement(string nameRegex)
        {
            this.NameRegex = nameRegex;
        }

        /// <summary>
        /// Remove members whose name matches regular expression
        /// </summary>
        /// <param name="members">values to arrange</param>
        /// <param name="converters">ignored</param>
        public override void Arrange(IList<IMember> members, ConverterInfo[] converters)
        {
            var removals = Enumerable.ToArray(GetMembersToRemove(members, m_nameRegex));

            foreach (var v in removals) members.Remove(v);
        }

        /// <summary>
        /// Set the <see cref="NameRegex"/>
        /// </summary>
        /// <param name="value">regular expression</param>
        public override void SetOption(string value)
        {
            NameRegex = value;
        }

        private static IEnumerable<IMember> GetMembersToRemove(IList<IMember> members, Regex nameRegex)
        {
            foreach (var member in members)
            {
                if (nameRegex == null || nameRegex.IsMatch(member.Name))
                    yield return member;
            }
        }
    }
}
