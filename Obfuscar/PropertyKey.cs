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
using Mono.Cecil;

namespace Obfuscar
{
    class PropertyKey
    {
        public PropertyKey(TypeKey typeKey, PropertyDefinition prop)
        {
            this.TypeKey = typeKey;
            this.Type = prop.PropertyType.FullName;
            this.Name = prop.Name;
            this.Property = prop;
        }

        public TypeKey TypeKey { get; }

        public string Type { get; }

        public string Name { get; }

        public MethodAttributes GetterMethodAttributes
        {
            get { return Property.GetMethod != null ? Property.GetMethod.Attributes : 0; }
        }

        public TypeDefinition DeclaringType
        {
            get { return (TypeDefinition) Property.DeclaringType; }
        }

        public PropertyDefinition Property { get; }

        public virtual bool Matches(MemberReference member)
        {
            PropertyReference propRef = member as PropertyReference;
            if (propRef != null)
            {
                if (TypeKey.Matches(propRef.DeclaringType))
                    return Type == propRef.PropertyType.FullName && Name == propRef.Name;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            PropertyKey key = obj as PropertyKey;
            if (key == null)
                return false;

            return this == key;
        }

        public static bool operator ==(PropertyKey a, PropertyKey b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.TypeKey == b.TypeKey && a.Type == b.Type && a.Name == b.Name;
        }

        public static bool operator !=(PropertyKey a, PropertyKey b)
        {
            if ((object) a == null)
                return (object) b != null;
            else if ((object) b == null)
                return true;
            else
                return a.TypeKey != b.TypeKey || a.Type != b.Type || a.Name != b.Name;
        }

        public override int GetHashCode()
        {
            return TypeKey.GetHashCode() ^ Type.GetHashCode() ^ Name.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("[{0}]{1} {2}::{3}", TypeKey.Scope, Type, TypeKey.Fullname, Name);
        }
    }
}
