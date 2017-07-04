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
using Mono.Cecil;
using Obfuscar.Helpers;

namespace Obfuscar
{
    static class TypeNameCache
    {
        public static Dictionary<TypeReference, string> nameCache = new Dictionary<TypeReference, string>();

        /// <summary>
        /// Recursively builds a type name.
        /// </summary>
        /// <param name="builder">Builder the type name will be added to.</param>
        /// <param name="type">Type whose name is to be built.</param>
        static void BuildTypeName(StringBuilder builder, TypeReference type)
        {
            GenericParameter genParam = type as GenericParameter;
            if (genParam != null)
            {
                builder.AppendFormat("!{0}", genParam.Position);
            }
            else
            {
                GenericInstanceType genType = type as GenericInstanceType;
                if (genType != null)
                {
                    builder.AppendFormat("[{2}]{0}.{1}<", genType.Namespace, genType.Name, type.GetScopeName());
                    for (int i = 0; i < genType.GenericArguments.Count; i++)
                    {
                        TypeReference argType = genType.GenericArguments[i];

                        if (i > 0)
                            builder.Append(',');

                        BuildTypeName(builder, argType);
                    }
                    builder.Append(">");
                }
                else
                {
                    ArrayType arrType = type as ArrayType;
                    if (arrType != null)
                    {
                        BuildTypeName(builder, arrType.ElementType);
                        builder.Append("[]");
                    }
                    else
                        builder.Append(String.Format("[{1}]{0}", type.FullName, type.GetScopeName()));
                }
            }
        }

        /// <summary>
        /// Builds a name for a type that can be used for comparing types.  Any generic parameters
        /// are replaced with their placeholder names instead of actual names (e.g., changes T to !0).
        /// </summary>
        public static string GetTypeName(TypeReference type)
        {
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
    }
}
