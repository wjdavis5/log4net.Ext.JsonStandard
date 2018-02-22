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

using System.IO;

using log4net.Util.Serializer;

namespace log4net.ObjectRenderer
{
    /// <summary>
    /// This is the default inmplementation of ISerializer used by JsonLayout. 
    /// It uses the .net35 System.Web.Script.Serialization.JavaScriptSerializer in turn.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class JsonObjectRenderer : IObjectRenderer
    {
        /// <summary>
        /// Factory of the serializer implementation
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// The bare minimal default serializer - static cache
        /// </summary>
        public static IObjectRenderer Default = new JsonObjectRenderer();

        /// <summary>
        /// Write the object value as Json string into the writer using the serializer
        /// </summary>
        /// <param name="rendererMap">The map used to lookup renderers</param>
        /// <param name="obj">Object to be serialized</param>
        /// <param name="writer">Will receive the serialized data of obj</param>
        public virtual void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
        {
            var serializer = Serializer ?? JsonSerializer.DefaultSerializer;
            var data = serializer.Serialize(obj, rendererMap);
            writer.WriteLine(data);
        }

    }


}