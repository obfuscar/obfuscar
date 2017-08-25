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
using System.Text.RegularExpressions;
using Mono.Cecil;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Obfuscar
{
    class Node<T>
    {
        public List<Node<T>> Parents = new List<Node<T>>();
        public List<Node<T>> Children = new List<Node<T>>();
        public T Item;

        public void AppendTo(Node<T> parent)
        {
            if (parent == null)
                return;

            parent.Children.Add(this);
            Parents.Add(parent);
        }
    }

    static class Helper
    {
        public static void Noop()
        {
        }

        /// <summary>
        /// Builds a name for a parameter type that can be used for comparing parameters.  See 
        /// <see cref="GetTypeName"/> for details.
        /// </summary>
        public static string GetParameterTypeName(ParameterReference param)
        {
            return TypeNameCache.GetTypeName(param.ParameterType);
        }

        public static string GetAttribute(XElement reader, string name)
        {
            string result;
            var val = reader.Attribute(name);
            if (val == null)
                result = String.Empty;
            else
                result = val.Value.Trim();

            return result;
        }

        public static string GetAttribute(XElement reader, string name, Variables vars)
        {
            return vars.Replace(GetAttribute(reader, name));
        }

        private static bool MatchWithWildCards(string test, int testIdx, string pattern, int patIdx)
        {
            while (true)
            {
                if (testIdx >= test.Length && patIdx >= pattern.Length)
                    return true;
                if (patIdx >= pattern.Length) // text has still characters but there is no pattern left.
                    return false;
                if (pattern[patIdx] != '*')
                {
                    if (testIdx >= test.Length || pattern[patIdx] != '?' && pattern[patIdx] != test[testIdx])
                        return false;
                    testIdx++;
                }
                else
                {
                    while (!MatchWithWildCards(test, testIdx, pattern, patIdx + 1))
                    {
                        if (test.Length <= testIdx++)
                            return false;
                    }
                }
                patIdx++;
            }
        }

        public static bool MatchWithWildCards(string test, string pattern)
        {
            return MatchWithWildCards(test, 0, pattern, 0);
        }

        public static bool CompareOptionalRegex(string test, string pattern)
        {
            if (pattern.StartsWith("^"))
                return Regex.IsMatch(test, pattern);
            else
                return MatchWithWildCards(test, pattern);
        }

        public static object GetAttributePropertyByName(CustomAttribute attr, string name)
        {
            foreach (CustomAttributeNamedArgument property in attr.Properties)
            {
                if (property.Name == name)
                    return property.Argument.Value;
            }
            return null;
        }
    }
}
