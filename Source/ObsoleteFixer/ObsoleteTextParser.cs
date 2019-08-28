using System;

namespace ObsoleteFixer
{
    internal class ObsoleteTextParser
    {
        /// <summary>
        /// Find the text between `` and after "Replace with"
        /// </summary>
        /// <param name="text"></param>
        /// <returns>null if not found</returns>
        public static string FindReplaceWithValue(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            const string prefix = "Replace with";
            var indexPrefix = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (indexPrefix < 0)
            {
                return null;
            }

            var indexStart = text.IndexOf('`', indexPrefix + prefix.Length);
            if (indexStart < 0)
            {
                return null;
            }

            indexStart++; //skip `
            var indexEnd = text.IndexOf('`', indexStart);
            if (indexEnd < 0)
            {
                return null;
            }

            var length = indexEnd - indexStart;
            return text.Substring(indexStart, length).Trim();
        }
    }
}