using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Terminator
{
    internal static class ParseUtils
    {
        public static double? ParseDouble(string source)
        {
            if (source == null)
            {
                return null;
            }

            double result;
            if (Double.TryParse(source, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return null;
        }

        public static double? ParseDouble(this NameValueCollection source, string optionName)
        {
            return ParseDouble(source[optionName]);
        }

        public static string[] ParseValues(this NameValueCollection source, string key, char separator)
        {
            return source[key]
                .Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
        }
    }
}