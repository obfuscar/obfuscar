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

using MethodAttributes = System.Reflection.MethodAttributes;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Adapters;

namespace Obfuscar
{
    class PropertyKey
    {
        public PropertyKey(IProperty prop)
            : this(new TypeKey(prop.DeclaringTypeFullName), prop.PropertyTypeFullName, prop.Name, null, prop)
        {
        }

        public PropertyKey(TypeKey typeKey, PropertyDefinition prop)
        {
            this.TypeKey = typeKey;
            this.Type = prop.PropertyType.FullName;
            this.Name = prop.Name;
            this.Property = prop;
            this.PropertyAdapter = new CecilPropertyAdapter(prop);
        }

        public PropertyKey(TypeKey typeKey, string type, string name, PropertyDefinition propertyDefinition, IProperty propertyAdapter)
        {
            this.TypeKey = typeKey;
            this.Type = type;
            this.Name = name;
            this.Property = propertyDefinition;
            this.PropertyAdapter = propertyAdapter;
        }

        public TypeKey TypeKey { get; }

        public string Type { get; }

        public string Name { get; }

        public MethodAttributes GetterMethodAttributes
        {
            get
            {
                if (PropertyAdapter != null)
                    return PropertyAdapter.GetterMethodAttributes;

                return Property.GetMethod != null ? (MethodAttributes) Property.GetMethod.Attributes : 0;
            }
        }

        public TypeDefinition DeclaringType
        {
            get { return Property != null ? (TypeDefinition) Property.DeclaringType : null; }
        }

        public PropertyDefinition Property { get; }

        public IProperty PropertyAdapter { get; }

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
            return string.Format("[{0}]{1} {2}::{3}", TypeKey.Scope, Type, TypeKey.Fullname, Name);
        }
    }
}
