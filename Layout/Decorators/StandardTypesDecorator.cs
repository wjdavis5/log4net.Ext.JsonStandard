using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using log4net.Util;

namespace log4net.Layout.Decorators
{
    /// <summary>
    /// Decorate logged objects - produce standard types to unite different JSON serializers
    /// </summary>
    public class StandardTypesDecorator : IDecorator
    {
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
        /// default constructor - <see cref="TypeMemberName"/> is "@type" and <see cref="SaveType"/> is null
        /// </summary>
        public StandardTypesDecorator()
        {
            SaveType = null;
            TypeMemberName = "@type";
        }

        /// <summary>
        /// decorate logged object
        /// </summary>
        /// <param name="obj">object to decorate</param>
        /// <returns>decorated object</returns>
        public object Decorate(object obj)
        {
            Standardise(ref obj, null);

            return obj;
        }

        /// <summary>
        /// Decoration implementation - turn objects to a standard
        /// </summary>
        /// <param name="obj">object to decorate</param>
        /// <param name="flatdict">used in <see cref="StandardTypesFlatDecorator"/></param>
        /// <param name="path">used in <see cref="StandardTypesFlatDecorator"/></param>
        protected virtual void Standardise(ref object obj, IDictionary flatdict, string path = null)
        {
            object result = null;

            var standardized = StandardNull(obj, ref result) // null gate first, others do not expect nulls
                    || StandardString(obj, ref result)
                    || StandardDictionary(obj, ref result, flatdict, path)
                    || StandardDateTime(obj, ref result)
                    || StandardTimeSpan(obj, ref result)
                    || StandardChars(obj, ref result)
                    || StandardBytes(obj, ref result)
                    || StandardPrimitive(obj, ref result)
                    || StandardEnum(obj, ref result)
                    || StandardGuid(obj, ref result)
                    || StandardUri(obj, ref result)
                    || StandardContext(obj, ref result)
                    || StandardArray(obj, ref result) // goes almost last not to interfere with string, char[], byte[]...
                    || StandardObject(obj, ref result, flatdict, path) // before last resort
                    ;

            if (standardized)
            {
                obj = result;
            }
            else
            {
                // last resort to string
                var str = Convert.ToString(obj);
                StandardString(str, ref obj);
            }
        }

        /// <summary>
        /// Null gate in standardization
        /// </summary>
        /// <param name="obj">null equivalent</param>
        /// <param name="result">null</param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardNull(object obj, ref object result)
        {
            return (obj == null || DBNull.Value.Equals(obj));
        }

        /// <summary>
        /// Besides regular strings, convert <see cref="StringBuilder"/>s and <see cref="System.IO.StringWriter"/>s to string too.
        /// </summary>
        /// <param name="obj">Stringy object</param>
        /// <param name="result">string</param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardString(object obj, ref object result)
        {
            var str = obj as String;
            if (str != null)
            {
                result = str;
                return true;
            }

            var sf = obj as SystemStringFormat;
            if (sf != null)
            {
                result = sf.ToString();
                return true;
            }

            var sb = obj as StringBuilder;
            if (sb != null)
            {
                result = sb.ToString();
                return true;
            }

            var sw = obj as System.IO.StringWriter;
            if (sw != null)
            {
                result = sw.ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Turn DateTime into ISO-8601 formatted string
        /// </summary>
        /// <param name="obj"><see cref="DateTime"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardDateTime(object obj, ref object result)
        {
            if (obj is DateTime)
            {
                result = ((DateTime)obj).ToString("o");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Turn timespans into a number of seconds
        /// </summary>
        /// <param name="obj"><see cref="TimeSpan"/></param>
        /// <param name="result"><see cref="Double"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardTimeSpan(object obj, ref object result)
        {
            if (obj is TimeSpan)
            {
                result = ((TimeSpan)obj).TotalSeconds;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Turn bytes or byte arrays into UTF8 encoded string
        /// </summary>
        /// <param name="obj"><see cref="Byte"/> or <see cref="T:byte[]"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardBytes(object obj, ref object result)
        {
            if (obj is byte[])
            {
                result = Encoding.UTF8.GetString((byte[])obj);
                return true;
            }

            if (obj is byte)
            {
                result = Encoding.UTF8.GetString(new byte[] { (byte)obj });
                return true;
            }

            var ms = obj as System.IO.MemoryStream;
            if (ms != null)
            {
                result = Encoding.UTF8.GetString(ms.GetBuffer());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Turn chars or char arrays into a string
        /// </summary>
        /// <param name="obj"><see cref="Char"/> or <see cref="T:char[]"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardChars(object obj, ref object result)
        {
            if (obj is char[])
            {
                result = new string((char[])obj);
                return true;
            }

            if (obj is char)
            {
                result = ((char)obj).ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Accept primitives
        /// </summary>
        /// <param name="obj">primitive type value</param>
        /// <param name="result">same</param>
        /// <returns>true if it's all done - it's a primitive or decimal</returns>
        protected virtual bool StandardPrimitive(object obj, ref object result)
        {
            var t = obj.GetType();

            if (t.IsPrimitive || obj is decimal)
            {
                result = obj;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stringify GUIDs
        /// </summary>
        /// <param name="obj"><see cref="Guid"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardGuid(object obj, ref object result)
        {
            if (obj == null || !(obj is Guid)) return false;

            result = Convert.ToString(obj);

            return true;
        }

        /// <summary>
        /// Stringify enums with value names
        /// </summary>
        /// <param name="obj"><see cref="Enum"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardEnum(object obj, ref object result)
        {
            if (obj == null) return false;
            if (!obj.GetType().IsEnum) return false;

            result = Convert.ToString(obj);
            return true;
        }

        /// <summary>
        /// Stringify URI
        /// </summary>
        /// <param name="obj"><see cref="Uri"/></param>
        /// <param name="result"><see cref="String"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardUri(object obj, ref object result)
        {
            var uri = obj as Uri;

            if (uri == null) return false;

            var str = Convert.ToString(uri);

            return true;
        }

        /// <summary>
        /// Stringify thread context
        /// </summary>
        /// <param name="obj">ThreadContextStack</param>
        /// <param name="result">string</param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardContext(object obj, ref object result)
        {
            var threadCtx = obj as log4net.Util.ThreadContextStack;

            if (threadCtx != null)
            {
                result = threadCtx.ToString();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Copy and traverse dictionary and turn non-standard members into standard ones.
        /// </summary>
        /// <param name="obj"><see cref="IDictionary"/></param>
        /// <param name="result">standardized <see cref="Dictionary{String,Object}"/></param>
        /// <param name="flatdict">ignored here</param>
        /// <param name="path">ignored here</param>
        /// <returns>true if it's all done - obj was an IDictionary</returns>
        protected virtual bool StandardDictionary(object obj, ref object result, IDictionary flatdict, string path = null)
        {
            var dict = obj as IDictionary;

            if (dict != null)
            {
                var standardDict = new Dictionary<string, object>(dict.Count);

                foreach (DictionaryEntry entry in dict)
                {
                    var objmember = entry.Value;
                    Standardise(ref objmember, null, null);
                    standardDict[Convert.ToString(entry.Key)] = objmember;
                }

                result = standardDict;
                return true;
            }

            result = obj;
            return obj is IDictionary;
        }

        /// <summary>
        /// Accept enumerables
        /// </summary>
        /// <param name="obj"><see cref="IEnumerable"/></param>
        /// <param name="result"><see cref="IEnumerable"/></param>
        /// <returns>true if it's all done - obj was an IEnumerable</returns>
        protected virtual bool StandardArray(object obj, ref object result)
        {
            var en = obj as IEnumerable;
            if (en != null)
            {
                result = obj;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Turn objects into dictionaries
        /// </summary>
        /// <param name="obj">object</param>
        /// <param name="result">standard dictionary</param>
        /// <param name="flatdict">used in <see cref="StandardTypesFlatDecorator"/></param>
        /// <param name="path">used in <see cref="StandardTypesFlatDecorator"/></param>
        /// <returns>true if it's all done</returns>
        protected virtual bool StandardObject(object obj, ref object result, IDictionary flatdict, string path = null)
        {
            return StandardDictionary(log4net.Util.Serializer.JsonSerializer.ObjToDict(obj, SaveType, TypeMemberName, Stringify, StringMemberName), ref result, flatdict, path);
        }



    }
}
