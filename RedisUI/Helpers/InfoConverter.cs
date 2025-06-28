using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisUI.Helpers
{
    public static class InfoConverter
    {
        public static Dictionary<string, string> ToInfo(this string input)
        {
            string[] rows = input.Split(new[] { "\r\n" }, StringSplitOptions.None);

            var attributeMap = rows
                .Where(row => !string.IsNullOrEmpty(row) && row.Contains(':'))
                .Select(row => row.Split(':'))
                .Where(keyValue => keyValue.Length == 2)
                .ToDictionary(keyValue => keyValue[0], keyValue => keyValue[1]);

            return attributeMap;
        }
    }
}
