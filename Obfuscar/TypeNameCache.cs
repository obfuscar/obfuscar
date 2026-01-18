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

using System.Collections.Generic;
using System.Text;
using Obfuscar.Metadata.Mutable;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar
{
    static class TypeNameCache
    {
        public static Dictionary<MutableTypeReference, string> nameCache = new Dictionary<MutableTypeReference, string>();

        /// <summary>
        /// Recursively builds a type name.
        /// </summary>
        /// <param name="builder">Builder the type name will be added to.</param>
        /// <param name="type">Type whose name is to be built.</param>
        static void BuildTypeName(StringBuilder builder, MutableTypeReference type)
        {
            MutableGenericParameter genParam = type as MutableGenericParameter;
            if (genParam != null)
            {
                builder.AppendFormat("!{0}", genParam.Position);
            }
            else
            {
                MutableGenericInstanceType genType = type as MutableGenericInstanceType;
                if (genType != null)
                {
                    builder.AppendFormat("[{2}]{0}.{1}<", genType.Namespace, genType.Name, type.GetScopeName());
                    for (int i = 0; i < genType.GenericArguments.Count; i++)
                    {
                        MutableTypeReference argType = genType.GenericArguments[i];

                        if (i > 0)
                            builder.Append(',');

                        BuildTypeName(builder, argType);
                    }
                    builder.Append(">");
                }
                else
                {
                    MutableArrayType arrType = type as MutableArrayType;
                    if (arrType != null)
                    {
                        BuildTypeName(builder, arrType.ElementType);
                        builder.Append("[]");
                    }
                    else
                        builder.Append(string.Format("[{1}]{0}", type.GetFullName(), type.GetScopeName()));
                }
            }
        }

        /// <summary>
        /// Builds a name for a type that can be used for comparing types.  Any generic parameters
        /// are replaced with their placeholder names instead of actual names (e.g., changes T to !0).
        /// </summary>
        public static string GetTypeName(MutableTypeReference type)
        {
            if (type == null)
                return string.Empty;

            lock (nameCache)
            {
                string name;
                if (!nameCache.TryGetValue(type, out name))
                {
                    StringBuilder builder = new StringBuilder();
                    BuildTypeName(builder, type);
                    name = builder.ToString();

                    nameCache[type] = name;
                }
                return name;
            }
        }

        // String-based cache for abstraction layer types
        private static Dictionary<string, string> stringNameCache = new Dictionary<string, string>();

        /// <summary>
        /// Gets a comparable type name from an IType abstraction.
        /// For simple types, returns the full name with scope prefix.
        /// </summary>
        public static string GetTypeName(IType type)
        {
            if (type == null)
                return string.Empty;

            string key = $"{type.Scope}|{type.FullName}";
            lock (stringNameCache)
            {
                if (!stringNameCache.TryGetValue(key, out string name))
                {
                    // For abstraction layer, we use the full name with scope
                    // Generic handling is done at the adapter level
                    name = $"[{type.Scope}]{type.FullName}";
                    stringNameCache[key] = name;
                }
                return name;
            }
        }

        /// <summary>
        /// Gets a comparable type name from a parameter type full name.
        /// </summary>
        public static string GetTypeName(string parameterTypeFullName, string scope)
        {
            if (string.IsNullOrEmpty(parameterTypeFullName))
                return string.Empty;

            string key = $"{scope}|{parameterTypeFullName}";
            lock (stringNameCache)
            {
                if (!stringNameCache.TryGetValue(key, out string name))
                {
                    name = $"[{scope}]{parameterTypeFullName}";
                    stringNameCache[key] = name;
                }
                return name;
            }
        }
    }
}
