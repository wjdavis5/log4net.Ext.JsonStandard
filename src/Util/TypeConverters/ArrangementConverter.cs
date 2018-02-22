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
using System.Text.RegularExpressions;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Arrangements;
using log4net.Layout.Members;

#if LOG4NET_1_2_10_COMPATIBLE
using ConverterInfo = log4net.Layout.PatternLayout.ConverterInfo;
#endif

namespace log4net.Util.TypeConverters
{

    /// <summary>
    /// Supports conversion from string or <see cref="PatternString"/> to <see cref="IArrangement"/> type.
    /// </summary>
    /// <author>Robert Sevcik</author>
    public class ArrangementConverter : IConvertFrom
    {
        #region Static goodies

        /// <summary>
        /// This is just a hack to grab converters from PatternLayout
        /// </summary>
        [ThreadStatic]
        static ConverterContext s_converter_context;

        delegate IArrangement Call(string option);

        static ConverterContext GetConverterContext()
        {
            var c = s_converter_context;
            if (c == null) c = s_converter_context = new ConverterContext();
            return c;
        }

        class ConverterContext
        {
            readonly Stack<ConverterInfo[]> stack = new Stack<ConverterInfo[]>();

            public IArrangement Call(Call call, ConverterInfo[] converters, string option)
            {
                stack.Push(converters);
                try
                {
                    return call(option);
                }
                finally
                {
                    stack.Pop();
                }
            }

            public ConverterInfo[] Get()
            {
                if (stack.Count != 0)
                    lock (stack)
                        if (stack.Count != 0)
                            return stack.Peek();

                return null;
            }
        }

        /// <summary>
        /// Convert string option into an arrangement using <see cref="ConverterRegistry.GetConvertFrom"/> 
        /// </summary>
        /// <param name="option">pattern, see <seealso cref="ConvertFrom"/> for more info on formatting</param>
        /// <param name="converters">converters to consider, can be null</param>
        /// <returns>the arrangement instance</returns>
        public static IArrangement GetArrangement(string option, ConverterInfo[] converters)
        {
            var arrangement = GetConverterContext().Call(GetArrangementInternal, converters, option);
            return arrangement;
        }

        /// <summary>
        /// Convert string option into an arrangement using <see cref="ConverterRegistry.GetConvertFrom"/> 
        /// </summary>
        /// <param name="option">pattern, see <seealso cref="ConvertFrom"/> for more info on formatting</param>
        /// <returns>the arrangement instance</returns>
        protected static IArrangement GetArrangementInternal(string option)
        {
            var arrangement = OptionConverter.ConvertStringTo(typeof(IArrangement), option) as IArrangement;
            return arrangement;
        }

        /// <summary>
        /// Initialize the environment: register Arrangement Type Converters 
        /// with the <see cref="ConverterRegistry"/>
        /// </summary>
        public static void Init()
        {
            ConverterRegistry.AddConverter(typeof(IArrangement), new ArrangementConverter());
            ConverterRegistry.AddConverter(typeof(DefaultArrangement), new ArrangementConverter());
            ConverterRegistry.AddConverter(typeof(RemovalArrangement), new ArrangementConverter());
            ConverterRegistry.AddConverter(typeof(IMember), new ArrangementConverter());
        }

        #region regex parsers
        static Regex s_setMatcher = new Regex(@"(?<Member>(?:(?:\\\\|\\;)|[^;])+)(;|$)", RegexOptions.Compiled);
        static Regex s_singleMatcher = new Regex(@"^(?<Name>(?:(?:\\\\|\\=|\\:|\\!|\\%|\\\|)|[^=:!%|])+)(?:(?<Op>[=:!|])(?<Value>.*$)|(?<Op>%)(?<Value>(?:\\\\|\\:|[^:])+)(?::(?<Option>.*)$|$)|$)", RegexOptions.Compiled);
        static Regex s_semiClean = new Regex(@"\\\\|\\;", RegexOptions.Compiled);
        static Regex s_nameClean = new Regex(@"\\\\|\\=|\\:|\\!|\\%|\\\|", RegexOptions.Compiled);
        static Regex s_brackClean = new Regex(@"\\\\|\\\(|\\\)|[()]", RegexOptions.Compiled);
        static Regex s_coverUp = new Regex(@"[\;=:!%|()]", RegexOptions.Compiled);
        #endregion

        #endregion

        #region Implementation of IConvertFrom

        /// <summary>
        /// Can the source type be converted to the type supported by this object
        /// </summary>
        /// <param name="sourceType">the type to convert</param>
        /// <returns>true if the conversion is possible</returns>
        /// <remarks>
        /// <para>
        /// Returns <c>true</c> if the <paramref name="sourceType"/> is
        /// the <see cref="String"/> type.
        /// </para>
        /// </remarks>
        public bool CanConvertFrom(Type sourceType)
        {
            return (sourceType == typeof(string)) || (sourceType == typeof(PatternString));
        }

        /// <summary>
        /// Overrides the ConvertFrom method of IConvertFrom.
        /// </summary>
        /// <param name="source">the object to convert to an <see cref="IArrangement"/></param>
        /// <returns>the arrangement</returns>
        /// <remarks>
        /// <para>
        /// "MemberName" => add member of name "MemberName"
        /// "MemberName:message" => add member named "MemberName" with the value of conversion of name "message"
        /// "MemberName=message\;exception" => add member named "MemberName" with the value of {message="...",exception="..."}
        /// "MemberName%date:dddd" => add member named "MemberName" with the value of PatternLayout for "%date{dddd}"
        /// "MemberName|%message%n" => add member named "MemberName" with the value of PatternLayout for "%message%n"
        /// "DEFAULT" => add member default members
        /// "DEFAULT!nxlog" => add member default members suitable for nxlog
        /// "CLEAR" => remove all members
        /// "REMOVE:^ex.*n$" remove member whose name matches regex "^ex.*n$"
        /// "DEFAULT!nxlog;Host=Name:hostname\;ProcessId\;Memory\;TimeStamp" => composite configuration
        /// </para>
        /// </remarks>
        /// <exception cref="ConversionNotSupportedException">
        /// The <paramref name="source"/> object cannot be converted to the
        /// target type. To check for this condition use the <see cref="CanConvertFrom"/>
        /// method.
        /// </exception>
        public object ConvertFrom(object source)
        {
            if (source == null) return null;

            var ps = source as PatternString;
            var str = ps == null ? source as string : ps.Format();

            if (str == null)
                str = "UnknownObject|" + s_coverUp.Replace(Convert.ToString(source), @"\$0");

            try
            {
                var arrangement = ParseArrangementSet(str);
                return arrangement;
            }
            catch (Exception x)
            {
#if LOG4NET_1_2_10_COMPATIBLE
                LogLog.Error(String.Format("Unable to parse '{0}'", source), x);
#else
                LogLog.Error(GetType(), String.Format("Unable to parse '{0}'", source), x);
#endif
                return null;
            }

        }

        #endregion

        /// <summary>
        /// Parse a single member arrangement
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public IArrangement ParseArangement(string str)
        {
            var match = s_singleMatcher.Match(str);

            var name = match.Groups["Name"].Value;
            var op = match.Groups["Op"].Value;
            var value = match.Groups["Value"].Value;
            var option = match.Groups["Option"].Value;
            var convs = GetConverterContext().Get();

            if (!match.Success)
            {
                name = String.Empty;
                op = "|";
                value = str;
            }

            name = s_nameClean.Replace(name, Clean);

            if (op == String.Empty)
            {
                if (value == String.Empty)
                {
                    op = ":";
                    value = name;
                }
                else
                {
                    op = "|";
                }
            }

            IArrangement ar;

            switch (op)
            {
                case "!":
                    // custom arrangements, removals, defaults or swaps
                    switch (name)
                    {
                        case "CLEAR":
                        case "REMOVE":
                            // removals
                            ar = new RemovalArrangement(value);
                            break;
                        case "DEFAULT":
                            // defaults
                            ar = new DefaultArrangement(value);
                            break;
                        default:
                            // custom arrangements
                            var type = Type.GetType(name, false);
                            if (type == null) throw new Exception(String.Format("Arrangement type not found: {0}", name));
                            var arrangement = Activator.CreateInstance(type) as IArrangement;
                            if (arrangement == null) throw new Exception(String.Format("Arrangement type is not IArrangement: {0}", type));
                            arrangement.SetOption(value);
                            ar = arrangement;
                            break;
                    }
                    break;
                case ":":
                    // just rename members
                    var cons =
                    ar = new Member()
                    {
                        Name = name,
                        Converters = convs,
                        Option = new Member()
                        {
                            Name = value,
                            Converters = convs
                        }
                    };
                    break;
                case "|":
                    // run a nested pattern layout
                    value = s_brackClean.Replace(value, Brackets);
                    value = s_nameClean.Replace(value, Clean);
                    var pl = new PatternLayout(value);
                    if (convs != null) foreach (var conv in convs) pl.AddConverter(conv);
                    ar = new Member()
                    {
                        Name = name,
                        Converters = convs,
                        Option = pl
                    };
                    break;
                case "%":
                    // run a nested pattern layout
                    var pl2 = new PatternLayout(string.Format("%{0}{{{1}}}", value, option));
                    if (convs != null) foreach (var conv in convs) pl2.AddConverter(conv);
                    ar = new Member()
                    {
                        Name = name,
                        Converters = convs,
                        Option = pl2
                    };
                    break;
                case "=":
                    // a member with an option
                    ar = new Member()
                    {
                        Name = name,
                        Converters = convs,
                        Option = value
                    };
                    break;
                default:
                    throw new Exception(String.Format("Unknown arrangement: '{0}{1}'", name, op));
            }

            if (ar is IOptionHandler)
                ((IOptionHandler)ar).ActivateOptions();

            return ar;
        }

        /// <summary>
        /// Parse a composite arrangement
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public IArrangement ParseArrangementSet(string option)
        {
            if (string.IsNullOrEmpty(option)) return null;

            var set = new MultipleArrangement();
            int lengthMatched = 0;

            foreach (Match match in s_setMatcher.Matches(option))
            {
                var member = match.Groups["Member"].Value;
                lengthMatched += match.Length;

                if (string.IsNullOrEmpty(member)) continue;

                // shortcuts
                switch (member)
                {
                    case "DEFAULT":
                    case "default":
                        member = "DEFAULT!default";
                        break;
                    case "nxlog":
                        member = "DEFAULT!nxlog";
                        break;
                    case "CLEAR":
                        member = "REMOVE";
                        break;
                }

                var cleanMember = s_semiClean.Replace(member, Clean);

                var arrangement = ParseArangement(cleanMember);

                set.AddArrangement(arrangement);
            }

            if (lengthMatched != option.Length) throw new Exception(String.Format("Unable to parse option: {0}", option));

            return set.Arrangements.Count == 0
                ? null
                : set.Arrangements.Count == 1
                ? set.Arrangements[0]
                : set;
        }

        /// <summary>
        /// Unescape matched escaped characters
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        protected string Clean(Match match)
        {
            switch (match.Value)
            {
                case @"\\": return @"\";
                case @"\;": return @";";
                case @"\=": return @"=";
                case @"\:": return @":";
                case @"\!": return @"!";
                case @"\%": return @"%";
                case @"\|": return @"|";
                case @"\(": return @"(";
                case @"\)": return @")";
                default: throw new Exception(String.Format("Not sure how to clean '{0}'", match.Value));
            }
        }
        /// <summary>
        /// Unescape matched escaped brackets
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        protected string Brackets(Match match)
        {
            switch (match.Value)
            {
                case @"\\": return @"\";
                case @"\(": return @"(";
                case @"\)": return @")";
                case @"(": return @"{";
                case @")": return @"}";
                default: throw new Exception(String.Format("Not sure how to bracket '{0}'", match.Value));
            }
        }

    }

}
