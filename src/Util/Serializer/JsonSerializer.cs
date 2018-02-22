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
using System.Text;
using log4net.ObjectRenderer;
using System.Collections.Generic;
using System.Reflection;

namespace log4net.Util.Serializer
{
    /// <summary>
    /// A simpleton implementation of a JSON serializer to supplement 
    /// System.Web.Script.Serialization.JavaScriptSerializer of NET35
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class JsonSerializer : ISerializer
    {
        #region statics

        /// <summary>
        /// Default JSON escaped characters
        /// </summary>
        public static readonly IDictionary<char, string> DefaultEscapedChars = new Dictionary<char, string>()
            {
                {'"',"\\\""},
                {'\\',"\\\\"},
                {'\b',"\\b"},
                {'\f',"\\f"},
                {'\n',"\\n"},
                {'\r',"\\r"},
                {'\t',"\\t"}, 
                // forward slash could be escaped instead of <> to allow javascript embedding
                //{'/',"\\/"},
                // but builtin serializer does it like this instead:
                {'<',"\\u003c"},
                {'>',"\\u003e"},      
                {'&',"\\u0026"},             
            };

        /// <summary>
        /// Which serializer will be used by default?
        /// </summary>
        /// <remarks>
        /// Creating JsonBuiltinSerializer here
        /// </remarks>
        public static ISerializer DefaultSerializer = new JsonSerializer();

        #endregion

        /// <summary>
        /// JSON escaped characters
        /// </summary>
        public IDictionary<char, string> EscapedChars { get; set; }

        /// <summary>
        /// preserve object type in serialization. true => always, false => never, null => only if class is publicly visible
        /// </summary>
        public bool? SaveType { get; set; }

        /// <summary>
        /// Call ToString and save the string
        /// </summary>
        public bool? Stringify { get; set; }

        /// <summary>
        /// if <see cref="SaveType"/> then this is the name it will be saved as
        /// </summary>
        public string TypeMemberName { get; set; }

        /// <summary>
        /// if <see cref="Stringify"/> then this is the name it will be saved as
        /// </summary>
        public string StringMemberName { get; set; }

        /// <summary>
        /// Construct instance - take <see cref="EscapedChars"/> from <see cref="DefaultEscapedChars"/>,
        /// <see cref="SaveType"/> is false, <see cref="TypeMemberName"/> is "__type".
        /// </summary>
        public JsonSerializer()
        {
            EscapedChars = new Dictionary<char, string>(DefaultEscapedChars);
            SaveType = false;
            Stringify = false;
            TypeMemberName = "__type";
            StringMemberName = "String";
        }

        /// <summary>
        /// Serialize <paramref name="obj"/> to a JSON string
        /// </summary>
		/// <param name="obj">object to serialize</param>
		/// <param name="map">log4net renderer map</param>
        /// <returns>JSON string</returns>
        public object Serialize(object obj, RendererMap map)
        {
            var sb = new StringBuilder();

            Serialize(obj, sb, map);

            return sb.ToString();
        }

        /// <summary>
        /// Serialize any object into a string builder
        /// </summary>
        /// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="map">log4net renderer map</param>
        protected virtual void Serialize(object obj, StringBuilder sb, RendererMap map)
        {
            var serialized = SerializeNull(obj, sb) // null gate first, others do not expect nulls
                    || SerializeDictionary(obj as IDictionary, sb, map)
                    || SerializeString(obj as string, sb)
                    || SerializeChars(obj as char[], sb)
                    || SerializeBytes(obj as byte[], sb)
                    || SerializeDateTime(obj, sb)
                    || SerializeTimeSpan(obj, sb)
                    || SerializePrimitive(obj, sb)
                    || SerializeEnum(obj, sb)
                    || SerializeGuid(obj, sb)
                    || SerializeUri(obj as Uri, sb)
                    || SerializeArray(obj as IEnumerable, sb, map) // goes almost last not to interfere with string, char[], byte[]...
                    || SerializeObject(obj, sb, map) // before last resort
                    ;

            if (!serialized)
                SerializeString(Convert.ToString(obj), sb); // last resort
        }

        /// <summary>
        /// Serialize null into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeNull(object obj, StringBuilder sb)
        {
            if (obj != null && !DBNull.Value.Equals(obj)) return false;
            sb.Append("null");
            return true;
        }

        /// <summary>
        /// Serialize date and time into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeDateTime(object obj, StringBuilder sb)
        {
            if (!(obj is DateTime)) return false;
            SerializeString(((DateTime)obj).ToString("o"), sb);
            return true;
        }

        /// <summary>
        /// Serialize time span into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeTimeSpan(object obj, StringBuilder sb)
        {
            if (!(obj is TimeSpan)) return false;
            SerializePrimitive(((TimeSpan)obj).TotalSeconds, sb);
            return true;
        }

        /// <summary>
        /// Serialize int's, byte's, char's, bools and friends into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializePrimitive(object obj, StringBuilder sb)
        {
            if (obj == null) return false;

            var t = obj.GetType();

            switch (t.FullName)
            {
                case "System.Double":
                case "System.Float":
                    sb.AppendFormat("{0:r}", obj);
                    break;
                case "System.Char":
                    SerializeChars(new char[] { (char)obj }, sb);
                    break;
                case "System.Byte":
                    SerializeBytes(new byte[] { (byte)obj }, sb);
                    break;
                case "System.Decimal":
                    sb.Append(obj);
                    break;
                case "System.Boolean":
                    sb.Append(true.Equals(obj) ? "true" : "false");
                    break;
                default:
                    if (!t.IsPrimitive)
                        return false;
                    else
                        sb.Append(obj);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Serialize a dictionary into a string builder
        /// </summary>
        /// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="map">log4net renderer map</param>
        protected virtual bool SerializeDictionary(IDictionary obj, StringBuilder sb, RendererMap map)
        {
            if (obj == null) return false;

            sb.Append("{");

            bool first = true;

            foreach (DictionaryEntry entry in (IDictionary)obj)
            {
                if (first)
                    first = false;
                else
                    sb.Append(",");

                sb.AppendFormat(@"""{0}"":", entry.Key);

                Serialize(entry.Value, sb, map);
            }

            sb.Append("}");

            return true;
        }

        /// <summary>
        /// Serialize enumerables into a string builder
        /// </summary>
        /// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="map">log4net renderer map</param>
        protected virtual bool SerializeArray(IEnumerable obj, StringBuilder sb, RendererMap map)
        {
            if (obj == null) return false;

            sb.Append("[");

            bool first = true;

            foreach (var item in obj)
            {
                if (first)
                    first = false;
                else
                    sb.Append(",");

                Serialize(item, sb, map);
            }

            sb.Append("]");

            return true;
        }

        /// <summary>
        /// Serialize an object (last resort) into a string builder
        /// </summary>
        /// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="map">log4net renderer map</param>
        protected virtual bool SerializeObject(Object obj, StringBuilder sb, RendererMap map)
        {
            if (obj == null) return false;

            var customSerializer = map == null ? null : map.Get(obj) as ISerializer;

            if (customSerializer == null)
            {
                var dict = ObjToDict(obj, SaveType, TypeMemberName, Stringify, StringMemberName);
                SerializeDictionary(dict, sb, map);
            }
            else
            {
                var json = customSerializer.Serialize(obj, map);
                sb.Append(json);
            }

            return true;
        }

        /// <summary>
        /// Serialize escaped string into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeBytes(byte[] obj, StringBuilder sb)
        {
            if (obj == null) return false;

            var str = Encoding.UTF8.GetString(obj);
            SerializeString(str, sb);

            return true;
        }

        /// <summary>
        /// Serialize escaped string into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeChars(char[] obj, StringBuilder sb)
        {
            if (obj == null) return false;

            var str = new string(obj);
            SerializeString(str, sb);

            return true;
        }

        /// <summary>
        /// Serialize escaped string into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeString(object obj, StringBuilder sb)
        {
            if (obj == null) return false;

            var str = Convert.ToString(obj);

            sb.Append(@"""");

            foreach (var c in str)
            {

                string cstring;

                if (EscapedChars.TryGetValue(c, out cstring))
                    sb.Append(cstring);
                else if (c < 32 || c > 126)
                    // c<32 nonprintable
                    // c=127 nonptintable
                    // c>127 encoding specific
                    sb.AppendFormat("\\u{0:X4}", (int)c);
                else
                    sb.Append(c);
            }

            sb.Append(@"""");

            return true;
        }

        /// <summary>
        /// Serialize URI into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeUri(Uri obj, StringBuilder sb)
        {
            if (obj == null) return false;

            var str = Convert.ToString(obj);

            return SerializeString(str, sb);
        }

        /// <summary>
        /// Serialize enum into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeGuid(object obj, StringBuilder sb)
        {
            if (obj == null || !(obj is Guid)) return false;

            var str = Convert.ToString(obj);

            return SerializeString(str, sb);
        }

        /// <summary>
        /// Serialize enum into a string builder
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sb"></param>
        protected virtual bool SerializeEnum(object obj, StringBuilder sb)
        {
            if (obj == null) return false;
            if (!obj.GetType().IsEnum) return false;

            var str = Convert.ToString(obj);

            return SerializeString(str, sb);
        }

        /// <summary>
        /// Convert objects fields and props into a dictionary
        /// </summary>
        /// <param name="obj">object to be turned into a dictionary</param>
        /// <param name="saveType">preserve the type of the object? null => only when publicly visible</param>
        /// <param name="typeMemberName">where to preserve the type</param>
        /// <param name="stringify">call ToString() and save it</param>
        /// <param name="stringMemberName">where to preserve the string</param>
        /// <returns>dictionary of props and fields</returns>
        public static IDictionary ObjToDict(object obj, bool? saveType, string typeMemberName, bool? stringify, string stringMemberName)
        {
            if (obj == null) return null;

            var flags = BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.GetField
                        | BindingFlags.GetProperty
                        ;

            var type = obj.GetType();
            var props = type.GetProperties(flags);
            var flds = type.GetFields(flags);
            var dict = new Dictionary<string, object>(props.Length + flds.Length + 1);

            foreach (var fld in flds)
            {
                dict[fld.Name] = fld.GetValue(obj);
            }

            foreach (var prop in props)
            {
                dict[prop.Name] = prop.GetValue(obj, null);
            }

            if (true.Equals(saveType) || (saveType == null && type.IsVisible))
                dict[typeMemberName] = type.FullName;

            if (true.Equals(stringify))
                dict[stringMemberName] = Convert.ToString(obj);

            return dict;
        }
    }


}
