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

using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Obfuscar
{
    class FieldTester : IPredicate<FieldKey>
    {
        private readonly string name;
        private readonly Regex nameRx;
        private readonly string type;
        private readonly string attrib;
        private readonly string typeAttrib;
        private readonly string inherits;
        private readonly bool? isStatic;
        private readonly bool? isSerializable;
        private readonly string decorator;

        public FieldTester(string name, string type, string attrib, string typeAttrib, string inherits,
            string decorator, bool? isStatic, bool? isSerializable)
        {
            this.name = name;
            this.type = type;
            this.attrib = attrib;
            this.typeAttrib = typeAttrib;
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSerializable;
            this.decorator = decorator;
        }

        public FieldTester(Regex nameRx, string type, string attrib, string typeAttrib, string inherits,
            string decorator, bool? isStatic, bool? isSerializable)
        {
            this.nameRx = nameRx;
            this.type = type;
            this.attrib = attrib;
            this.typeAttrib = typeAttrib;
            this.inherits = inherits;
            this.isStatic = isStatic;
            this.isSerializable = isSerializable;
            this.decorator = decorator;
        }

        public bool Test(FieldKey field, InheritMap map)
        {
            if (!string.IsNullOrEmpty(decorator))
            {
                var match = false;

                foreach (var typeField in field.TypeKey.TypeDefinition.Fields)
                {
                    if (typeField.Name != field.Name)
                    {
                        continue;
                    }

                    if (!typeField.HasCustomAttributes)
                    {
                        return false;
                    }

                    foreach (var customAttr in typeField.CustomAttributes)
                    {
                        if (customAttr.AttributeType.FullName == decorator)
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (!match)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(type) && !Helper.CompareOptionalRegex(field.TypeKey.Fullname, type))
            {
                return false;
            }

            // It's not very clean to use CheckMethodVisibility() from MethodTester. But we don't want duplicate code either.
            if (MethodTester.CheckMemberVisibility(attrib, typeAttrib, (MethodAttributes) field.FieldAttributes,
                field.DeclaringType))
            {
                return false;
            }

            if (nameRx != null && !nameRx.IsMatch(field.Name))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(name) && !Helper.CompareOptionalRegex(field.Name, name))
            {
                return false;
            }

            if (isStatic.HasValue)
            {
                bool fieldIsStatic = (field.FieldAttributes & FieldAttributes.Static) == FieldAttributes.Static;

                if (isStatic.Value != fieldIsStatic)
                {
                    return false;
                }
            }

            if (isSerializable.HasValue)
            {
                if (isSerializable.Value && ((field.FieldAttributes & FieldAttributes.NotSerialized) ==
                                             FieldAttributes.NotSerialized))
                {
                    return false;
                }

                bool fieldIsPublic = (field.FieldAttributes & FieldAttributes.Public) == FieldAttributes.Public;
                bool parentIsSerializable = field.DeclaringType.IsSerializable;

                if (isSerializable != (fieldIsPublic && parentIsSerializable))
                {
                    return false;
                }
            }

            // finally does method's type inherit?
            if (!string.IsNullOrEmpty(inherits))
            {
                if (!map.Inherits(field.DeclaringType, inherits))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
