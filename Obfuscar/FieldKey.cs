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

using FieldAttributes = System.Reflection.FieldAttributes;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar
{
    class FieldKey
    {
        public FieldKey(Obfuscar.Metadata.Abstractions.IField field)
            : this(new TypeKey(field.DeclaringTypeFullName), field.FieldTypeFullName, field.Name, null, field.Attributes)
        {
        }

        public FieldKey(FieldDefinition field)
            : this(new TypeKey((TypeDefinition) field.DeclaringType), field.FieldType.FullName, field.Name, field)
        {
        }

        public FieldKey(TypeKey typeKey, string type, string name, FieldDefinition fieldDefinition, FieldAttributes? attributes = null)
        {
            this.TypeKey = typeKey;
            this.Type = type;
            this.Name = name;
            this.Field = fieldDefinition;
            this.overriddenAttributes = attributes;
        }

        private readonly FieldAttributes? overriddenAttributes;

        public FieldAttributes FieldAttributes
        {
            get
            {
                if (overriddenAttributes.HasValue)
                    return overriddenAttributes.Value;

                return Field != null ? (FieldAttributes) Field.Attributes : 0;
            }
        }

        public TypeDefinition DeclaringType
        {
            get { return Field != null ? (TypeDefinition) Field.DeclaringType : null; }
        }

        public FieldDefinition Field { get; }

        public TypeKey TypeKey { get; }

        public string Type { get; }

        public string Name { get; }

        public virtual bool Matches(MemberReference member)
        {
            FieldReference fieldRef = member as FieldReference;
            if (fieldRef != null)
            {
                if (TypeKey.Matches(fieldRef.DeclaringType))
                {
                    if (Field != null)
                        return Type == fieldRef.FieldType.FullName && Name == fieldRef.Name;
                    // If we don't have a FieldDefinition (from IField), match by name and type string
                    return Type == fieldRef.FieldType.FullName && Name == fieldRef.Name;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            FieldKey key = obj as FieldKey;
            if (key == null)
                return false;

            return this == key;
        }

        public static bool operator ==(FieldKey a, FieldKey b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.TypeKey == b.TypeKey && a.Type == b.Type && a.Name == b.Name;
        }

        public static bool operator !=(FieldKey a, FieldKey b)
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
