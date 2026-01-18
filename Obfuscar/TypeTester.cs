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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar
{
    [Flags]
    public enum TypeAffectFlags
    {
        SkipNone = 0x00,
        AffectEvent = 0x01,
        AffectProperty = 0x02,
        AffectField = 0x04,
        AffectMethod = 0x08,
        AffectString = 0x10
    }

    class TypeTester : IPredicate<TypeKey>
    {
        private readonly string name;
        private readonly Regex nameRx;
        private readonly string attrib;
        private readonly string inherits;
        private readonly bool? isStatic;
        private readonly string decorator;
        private readonly string[] decoratorAll;
        private readonly HashSet<string> decoratorTypeNames = new HashSet<string>();
        private bool decoratorMatchesInitialized;
        private readonly bool?
            isSerializable;

        public TypeAffectFlags AffectFlags { get; }

        public TypeTester(string name)
            : this(name, TypeAffectFlags.SkipNone, string.Empty)
        {
        }

        public TypeTester(string name, TypeAffectFlags skipFlags, string attrib)
        {
            this.name = name;
            this.AffectFlags = skipFlags;
            this.attrib = attrib.ToLower();
        }

        public TypeTester(string name, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable)
            : this(name, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
        }
        
        public TypeTester(string name, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable, string decorator)
            : this(name, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
            this.decorator = decorator;
        }
        
        public TypeTester(string name, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable, string decorator, string[] decoratorAll)
            : this(name, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
            this.decorator = decorator;
            this.decoratorAll = decoratorAll;
        }

        public TypeTester(Regex nameRx, TypeAffectFlags skipFlags, string attrib)
        {
            this.nameRx = nameRx;
            this.AffectFlags = skipFlags;
            this.attrib = attrib.ToLower();
        }

        public TypeTester(Regex nameRx, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable)
            : this(nameRx, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
        }
        
        public TypeTester(Regex nameRx, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable, string decorator)
            : this(nameRx, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
            this.decorator = decorator;
        }
        
        public TypeTester(Regex nameRx, TypeAffectFlags skipFlags, string attrib, string inherits, bool? isStatic,
            bool? isSeriablizable, string decorator, string[] decoratorAll)
            : this(nameRx, skipFlags, attrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSeriablizable;
            this.decorator = decorator;
            this.decoratorAll = decoratorAll;
        }

        public bool Test(TypeKey type, InheritMap map)
        {
            if (!string.IsNullOrEmpty(attrib))
            {
                if (string.Equals(attrib, "public", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!type.Descriptor.IsPublic)
                    {
                        return false;
                    }
                }
                else
                {
                    throw new ObfuscarException(string.Format(
                        "'{0}' is not valid for the 'attrib' value of the SkipType element. Only 'public' is supported by now.",
                        attrib));
                }
            }
            
            if ((!string.IsNullOrEmpty(decorator) || (decoratorAll != null && decoratorAll.Length > 0)) &&
                !decoratorTypeNames.Contains(type.Descriptor.FullName))
            {
                return false;
            }
            
            if (decoratorAll != null && decoratorAll.Length > 0)
            {
                if (!type.Descriptor.HasCustomAttributes)
                {
                    return false;
                }

                var attrNames = type.Descriptor.CustomAttributeTypeFullNames;
                for (int i = 0; i < decoratorAll.Length; i++)
                {
                    if (!attrNames.Contains(decoratorAll[i]))
                        return false;
                }
            }

            // type's regex matches
            if (nameRx != null && !nameRx.IsMatch(type.Fullname))
            {
                return false;
            }

            // type's name matches
            if (!string.IsNullOrEmpty(name) && !Helper.CompareOptionalRegex(type.Fullname, name))
            {
                return false;
            }

            if (isSerializable.HasValue)
            {
                if (isSerializable != type.Descriptor.IsSerializable)
                {
                    return false;
                }
            }

            if (isStatic.HasValue)
            {
                if (isStatic != type.Descriptor.IsStatic)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(inherits) && !map.Inherits(type, inherits))
                return false;

            return true;
        }

        internal void InitializeDecoratorMatches(AssemblyInfo assembly)
        {
            if (decoratorMatchesInitialized)
                return;

            decoratorMatchesInitialized = true;

            if (string.IsNullOrEmpty(decorator) && (decoratorAll == null || decoratorAll.Length == 0))
                return;

            foreach (var type in assembly.GetAllTypes())
            {
                if (MatchesDecorator(type))
                {
                    AddDecoratorType(type);
                }
            }
        }

        private bool MatchesDecorator(IType type)
        {
            if (type == null)
                return false;

            if (!string.IsNullOrEmpty(decorator))
            {
                bool found = type.CustomAttributeTypeFullNames != null &&
                             type.CustomAttributeTypeFullNames.Contains(decorator);
                if (!found &&
                    decorator == "System.Runtime.CompilerServices.CompilerGeneratedAttribute" &&
                    (type.Name?.StartsWith("<") ?? false))
                {
                    found = true;
                }

                if (!found)
                    return false;
            }

            if (decoratorAll != null && decoratorAll.Length > 0)
            {
                if (type.CustomAttributeTypeFullNames == null)
                    return false;

                foreach (string attrName in decoratorAll)
                {
                    if (!type.CustomAttributeTypeFullNames.Contains(attrName))
                        return false;
                }
            }

            return true;
        }

        private void AddDecoratorType(IType type)
        {
            var fullName = type.FullName ?? string.Empty;
            if (fullName.Length == 0)
                return;

            var parts = fullName.Split('/');
            for (int i = parts.Length; i > 0; i--)
            {
                decoratorTypeNames.Add(string.Join("/", parts.Take(i)));
            }
        }
    }
}
