using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace log4net.Layout.Decorators
{
    /// <summary>
    /// Decorate logged objects - produce standard types to unite different JSON serializers + flatten dictionaries / objects
    /// </summary>
    public class StandardTypesFlatDecorator : StandardTypesDecorator
    {
        /// <summary>
        /// Override base by flattening the directory structure using dot-path member names
        /// </summary>
        /// <param name="obj">dictionary</param>
        /// <param name="result">standardized dictionary</param>
        /// <param name="flatdict">flat dictionary built recursively</param>
        /// <param name="path">recursive name in flat dictionary</param>
        /// <returns>true if it's all fine and done - obj was an IDictionary</returns>
        protected override bool StandardDictionary(object obj, ref object result, IDictionary flatdict, string path = null)
        {
            var dict = obj as IDictionary;

            if (dict != null)
            {
                if (flatdict == null)
                {
                    flatdict = new Dictionary<string, object>(dict.Count);
                    result = flatdict;
                }
                else
                {
                    result = null;
                }

                FlattenDictionary(dict, flatdict, path);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Copy a recursively nested dictionary into a flat dictionary.
        /// </summary>
        /// <param name="dict">source hierarchical dictionary</param>
        /// <param name="flatdict">target flat dictionary</param>
        /// <param name="path">nesting path</param>
        /// <remarks>
        /// 
        /// * member keys/names are stringified
        /// * nested <see cref="IDictionary"/> member values are flattened with a "parent.child" notation
        /// * non-primitive values are stringified
        /// 
        /// </remarks>
        protected virtual void FlattenDictionary(IDictionary dict, IDictionary flatdict, string path = null)
        {
            if (flatdict == null) throw new ArgumentNullException("flatdict");
            if (dict == null) throw new ArgumentNullException("dict");

            foreach (DictionaryEntry entry in dict)
            {
                var name = path == null
                            ? Convert.ToString(entry.Key)
                            : String.Format("{0}.{1}", path, entry.Key)
                            ;

                if (entry.Value == null || DBNull.Value.Equals(entry.Value))
                {
                    // ignore nulls
                }
                else if (entry.Value is IDictionary)
                {
                    FlattenDictionary((IDictionary)entry.Value, flatdict, name);
                }
                else
                {
                    var obj = entry.Value;

                    Standardise(ref obj, flatdict, name);

                    if (obj != null)
                        flatdict[name] = obj;
                }
            }
        }

    }
}
