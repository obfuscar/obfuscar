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
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar
{
    class FieldKey
    {
        public FieldKey(IField field)
            : this(new TypeKey(field.DeclaringTypeFullName), field.FieldTypeFullName, field.Name, null, field.Attributes, field)
        {
        }

        public FieldKey(MutableFieldDefinition field)
            : this(new TypeKey(field?.DeclaringType), field?.FieldType?.GetFullName() ?? string.Empty, field?.Name ?? string.Empty, field, field?.Attributes, field)
        {
        }

        public FieldKey(TypeKey typeKey, string type, string name, MutableFieldDefinition fieldDefinition, FieldAttributes? attributes = null, IField fieldAdapter = null)
        {
            this.TypeKey = typeKey;
            this.Type = type;
            this.Name = name;
            this.Field = fieldDefinition;
            this.overriddenAttributes = attributes;
            this.FieldAdapter = fieldAdapter;
        }

        private readonly FieldAttributes? overriddenAttributes;

        public FieldAttributes FieldAttributes
        {
            get
            {
                if (overriddenAttributes.HasValue)
                    return overriddenAttributes.Value;

                if (Field != null)
                    return Field.Attributes;

                if (FieldAdapter != null)
                    return FieldAdapter.Attributes;

                return 0;
            }
        }

        public MutableTypeDefinition DeclaringType
        {
            get { return Field?.DeclaringType as MutableTypeDefinition; }
        }

        public MutableFieldDefinition Field { get; }

        public IField FieldAdapter { get; }

        public TypeKey TypeKey { get; }

        public string Type { get; }

        public string Name { get; }

        public virtual bool Matches(MutableFieldReference member)
        {
            if (member != null)
            {
                if (TypeKey.Matches(member.DeclaringType))
                {
                    if (Field != null)
                        return Type == member.FieldType?.GetFullName() && Name == member.Name;
                    return Type == member.FieldType?.GetFullName() && Name == member.Name;
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
