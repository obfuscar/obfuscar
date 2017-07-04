#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Obfuscar
{
    static class NameMaker
    {
        static string uniqueChars;
        static int numUniqueChars;
        const string defaultChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        const string unicodeChars = "\u00A0\u1680" +
                                    "\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u200B\u2010\u2011\u2012\u2013\u2014\u2015" +
                                    "\u2022\u2024\u2025\u2027\u2028\u2029\u202A\u202B\u202C\u202D\u202E\u202F" +
                                    "\u2032\u2035\u2033\u2036\u203E" +
                                    "\u2047\u2048\u2049\u204A\u204B\u204C\u204D\u204E\u204F\u2050\u2051\u2052\u2053\u2054\u2055\u2056\u2057\u2058\u2059" +
                                    "\u205A\u205B\u205C\u205D\u205E\u205F\u2060" +
                                    "\u2061\u2062\u2063\u2064\u206A\u206B\u206C\u206D\u206E\u206F" +
                                    "\u3000";

        private static readonly string koreanChars;

        static NameMaker()
        {
            string lUnicode = unicodeChars;
            for (int i = 0; i < lUnicode.Length; i++)
            {
                for (int j = i + 1; j < lUnicode.Length; j++)
                {
                    System.Diagnostics.Debug.Assert(lUnicode[i] != lUnicode[j], "Duplicate Char");
                }
            }

            UseUnicodeChars = false;

            // Fill the char array used for renaming with characters
            // from Hangul (Korean) unicode character set.
            var chars = new List<char>(128);
            var rnd = new Random();
            var startPoint = rnd.Next(0xAC00, 0xD5D0);
            for (int i = startPoint; i < startPoint + 128; i++)
                chars.Add((char) i);

            ShuffleArray(chars, rnd);
            koreanChars = new string(chars.ToArray());

            UseKoreanChars = false;
        }

        private static void ShuffleArray<T>(IList<T> list, Random rnd)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool UseUnicodeChars
        {
            get { return uniqueChars == unicodeChars; }
            set
            {
                if (value)
                    uniqueChars = unicodeChars;
                else
                    uniqueChars = defaultChars;

                numUniqueChars = uniqueChars.Length;
            }
        }

        public static bool UseKoreanChars
        {
            get { return unicodeChars == koreanChars; }
            set
            {
                if (value)
                    uniqueChars = koreanChars;
                else
                    uniqueChars = defaultChars;

                numUniqueChars = uniqueChars.Length;
            }
        }

        public static string UniqueName(int index)
        {
            return UniqueName(index, null);
        }

        public static string UniqueName(int index, string sep)
        {
            // optimization for simple case
            if (index < numUniqueChars)
                return uniqueChars[index].ToString();

            Stack<char> stack = new Stack<char>();

            do
            {
                stack.Push(uniqueChars[index % numUniqueChars]);
                if (index < numUniqueChars)
                    break;
                index /= numUniqueChars;
            } while (true);

            StringBuilder builder = new StringBuilder();
            builder.Append(stack.Pop());
            while (stack.Count > 0)
            {
                if (sep != null)
                    builder.Append(sep);
                builder.Append(stack.Pop());
            }

            return builder.ToString();
        }

        public static string UniqueNestedTypeName(int index)
        {
            return UniqueName(index, null);
        }

        public static string UniqueTypeName(int index)
        {
            return UniqueName(index % numUniqueChars, ".");
        }

        public static string UniqueNamespace(int index)
        {
            return UniqueName(index / numUniqueChars, ".");
        }
    }
}
