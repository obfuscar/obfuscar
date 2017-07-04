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
using Obfuscar.Helpers;

namespace Obfuscar
{
    class MethodTester : IPredicate<MethodKey>
    {
        private readonly MethodKey key;
        private readonly string name;
        private readonly Regex nameRx;
        private readonly string type;
        private readonly string attrib;
        private readonly string typeAttrib;
        private readonly string inherits;
        private readonly bool? isStatic;

        public MethodTester(MethodKey key)
        {
            this.key = key;
        }

        public MethodTester(string name, string type, string attrib, string typeAttrib)
        {
            this.name = name;
            this.type = type;
            this.attrib = attrib;
            this.typeAttrib = typeAttrib;
        }

        public MethodTester(Regex nameRx, string type, string attrib, string typeAttrib)
        {
            this.nameRx = nameRx;
            this.type = type;
            this.attrib = attrib;
            this.typeAttrib = typeAttrib;
        }

        public MethodTester(string name, string type, string attrib, string typeAttrib, string inherits, bool? isStatic)
            : this(name, type, attrib, typeAttrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
        }

        public MethodTester(Regex nameRx, string type, string attrib, string typeAttrib, string inherits,
            bool? isStatic)
            : this(nameRx, type, attrib, typeAttrib)
        {
            this.inherits = inherits;
            this.isStatic = isStatic;
        }

        public bool Test(MethodKey method, InheritMap map)
        {
            if (key != null)
                return method == key;

            // method name matches type regex?
            if (!String.IsNullOrEmpty(type) && !Helper.CompareOptionalRegex(method.TypeKey.Fullname, type))
            {
                return false;
            }

            // method visibility matches
            if (CheckMemberVisibility(this.attrib, typeAttrib, method.MethodAttributes, method.DeclaringType))
            {
                return false;
            }

            // method's name matches
            if (nameRx != null && !nameRx.IsMatch(method.Name))
            {
                return false;
            }

            // method's name matches
            if (!string.IsNullOrEmpty(name) && !Helper.CompareOptionalRegex(method.Name, name))
            {
                return false;
            }

            // check is method's static flag matches.
            if (isStatic.HasValue)
            {
                bool methodIsStatic = (method.MethodAttributes & MethodAttributes.Static) == MethodAttributes.Static;

                if (isStatic != methodIsStatic)
                {
                    return false;
                }
            }

            // finally does method's type inherit?
            if (!string.IsNullOrEmpty(inherits))
            {
                if (!map.Inherits(method.DeclaringType, inherits))
                {
                    return false;
                }
            }

            return true;
        }

        static public bool CheckMemberVisibility(string attribute, string typeAttribute,
            MethodAttributes methodAttributes, TypeDefinition declaringType)
        {
            if (!string.IsNullOrEmpty(typeAttribute))
            {
                if (string.Equals(typeAttribute, "public", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!declaringType.IsTypePublic())
                        return false;
                }
                else
                    throw new ObfuscarException(string.Format(
                        "'{0}' is not valid for the 'typeattrib' value of skip elements. Only 'public' is supported by now.",
                        typeAttribute));
            }

            if (!string.IsNullOrEmpty(attribute))
            {
                MethodAttributes accessmask = (methodAttributes & MethodAttributes.MemberAccessMask);
                if (string.Equals(attribute, "public", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (accessmask != MethodAttributes.Public)
                        return true;
                }
                else if (string.Equals(attribute, "protected", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!(accessmask == MethodAttributes.Public || accessmask == MethodAttributes.Family ||
                          accessmask == MethodAttributes.FamORAssem))
                        return true;
                }
                else
                    throw new ObfuscarException(string.Format(
                        "'{0}' is not valid for the 'attrib' value of skip elements. Only 'public' and 'protected' are supported by now.",
                        attribute));

                // attrib value given, but the member is not public/protected. We signal that the Skip* rule should be ignored. The member is obfuscated in any case.
                return false;
            }

            // No attrib value given: The Skip* rule is processed normally.
            return false;
        }
    }
}
