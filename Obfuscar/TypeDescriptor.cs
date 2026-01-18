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
using System.Linq;
using Obfuscar.Helpers;
using Obfuscar.Metadata;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar
{
    class TypeDescriptor
    {
        public TypeDescriptor(ITypeDefinition typeDefinition, string fullName, bool isPublic, bool isSerializable, bool isSealed, bool isAbstract, bool isEnum, IReadOnlyList<string> customAttributeTypeFullNames)
        {
            TypeDefinition = typeDefinition;
            FullName = fullName ?? string.Empty;
            IsPublic = isPublic;
            IsSerializable = isSerializable;
            IsSealed = isSealed;
            IsAbstract = isAbstract;
            IsEnum = isEnum;
            CustomAttributeTypeFullNames =
                customAttributeTypeFullNames ?? Array.Empty<string>();
        }

        public ITypeDefinition TypeDefinition { get; }

        public string FullName { get; }

        public bool IsPublic { get; }

        public bool IsSerializable { get; }

        public bool IsSealed { get; }

        public bool IsAbstract { get; }

        public bool IsEnum { get; }

        public bool IsStatic => IsSealed && IsAbstract;

        public IReadOnlyList<string> CustomAttributeTypeFullNames { get; }

        public bool HasCustomAttributes => CustomAttributeTypeFullNames != null && CustomAttributeTypeFullNames.Count > 0;

        public static TypeDescriptor FromDefinition(ITypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var attributes = type.CustomAttributeTypeFullNames?.ToArray() ?? Array.Empty<string>();
            var isPublic = IsTypePublic(type);

            return new TypeDescriptor(type, type.FullName, isPublic, type.IsSerializable, type.IsSealed, type.IsAbstract, type.IsEnum, attributes);
        }

        public static TypeDescriptor FromType(IType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var attributes = type.CustomAttributeTypeFullNames?.ToArray() ?? Array.Empty<string>();
            var isPublic = IsTypePublic(type);

            return new TypeDescriptor(type as ITypeDefinition, type.FullName, isPublic, type.IsSerializable, type.IsSealed, type.IsAbstract, type.IsEnum, attributes);
        }

        public static TypeDescriptor FromFullName(string fullName)
        {
            return new TypeDescriptor(null, fullName ?? string.Empty, false, false, false, false, false, Array.Empty<string>());
        }

        private static bool IsTypePublic(IType type)
        {
            if (type is not ITypeDefinition typeDef || typeDef.DeclaringType == null)
            {
                return type.IsPublic;
            }

            if (!type.IsPublic)
            {
                return false;
            }

            return IsTypePublic(typeDef.DeclaringType);
        }
    }
}
