using Npgsql;

namespace Elephanet.Extensions
{
    public static class StringExtensionMethods
    {
        public static string UseParameter(this string text, NpgsqlParameter parameter)
        {
            return text.ReplaceFirst("?", ":" + parameter.ParameterName);
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search, System.StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string ReplaceDotWithUnderscore(this string text)
        {
            text = text.Replace(".", "_");
            return text;
        }

        public static string SurroundWith(this string text, string ends)
        {
            return ends + text + ends;
        }

        public static string SurroundWithSingleQuote(this string text)
        {
            return SurroundWith(text, "'");
        }

        public static string SurroundWithDoubleQuotes(this string text)
        {
            return SurroundWith(text, "\"");
        }
    }
}
