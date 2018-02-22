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
using System.Collections.Generic;
using System.Text;

namespace log4net.Util
{
    /// <summary>
    /// Utility static methods to provide compatibility with System.Linq
    /// </summary>
    /// <author>Robert Sevcik</author>
    public static class Enumerable
    {
        /// <summary>
        /// Equivalent to System.Linq.Enumerable.Cast&lt;T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<T> Cast<T>(IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                yield return (T)item;
            }
        }

        /// <summary>
        /// Equivalent to System.Linq.Enumerable.ToArray&lt;T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            return list.ToArray();
        }

        /// <summary>
        /// A union implementation similar to System.Linq.Enumerable.Union&lt;T>
        /// except that null arguments are allowed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerations"></param>
        /// <returns></returns>
        public static IEnumerable<T> Union<T>(params IEnumerable<T>[] enumerations)
        {
            var set = new Dictionary<T,bool>();

            foreach(var enumer in enumerations)
            {
                if(enumer == null) continue;

                foreach(var item in enumer)
                {
                    if (set.ContainsKey(item)) continue;

                    set.Add(item, true);
                    yield return item;
                }
            }
        }
    }
}
