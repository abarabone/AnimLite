using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AnimLite.Utility
{


    public static class StringUtility
    {
        public static string JoinString(this IEnumerable<string> src, string delim) => string.Join(delim, src);
        public static string JoinString(this IEnumerable<string> src, char delim) => string.Join(delim, src);
    }





    public struct Wildcard
    {
        public string value;
    }

    public static class WildcardExtension
    {
        public static bool Like(this string input, Wildcard wildcard)
        {
            return Regex.IsMatch(input, wildcard.value);
        }

        public static bool Like(this string input, string wildcard)
        {
            return wildcard.IsWild()
                ? input.Like(wildcard.ToWildcard())
                : input == wildcard
                ;
        }

        public static bool IsWild(this string s) =>
            s.Contains('*')
            ||
            s.Contains('?')
            ||
            s.Contains('#')
            ;

        /// <summary>
        /// 細かい点の注意： # は数字扱い
        /// </summary>
        public static Wildcard ToWildcard(this string s)
        {
            return new Wildcard
            {
                value = toPattern_(s)
            };

            static string toPattern_(string input)
            {
                var q =
                    from c in input
                    select c switch
                    {
                        '*' => ".*",
                        '?' => ".",
                        '#' => @"[\d\#]",
                        _ => Regex.Escape(c.ToString()),
                    };

                return $"^{string.Join("", q)}$";
            }
        }
    }

}
