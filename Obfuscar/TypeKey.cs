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
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar
{
    class TypeKey : IComparable<TypeKey>
    {
        readonly TypeReference typeReference;
        readonly int hashCode;
        readonly TypeDescriptor descriptor;

        public TypeKey(TypeReference type)
        {
            this.typeReference = type;
            this.Scope = type.GetScopeName();
            InitializeFromFullName(type.FullName);

            // Ensure the parsed name matches Cecil's view for consistency.
            // This is important to the Match method.
            GenericInstanceType gi = type as GenericInstanceType;
            type.DeclaringType = type.DeclaringType; // Hack: Update fullname of nested type
            if (this.Fullname != type.ToString() && (gi == null || this.Fullname != gi.ElementType.FullName))
                throw new InvalidOperationException(string.Format("Type names do not match: \"{0}\" != \"{1}\"",
                    this.Fullname, type.ToString()));

            this.hashCode = CalcHashCode();
            this.descriptor = type is TypeDefinition td
                ? TypeDescriptor.FromDefinition(td)
                : TypeDescriptor.FromFullName(this.Fullname);
        }

        public TypeKey(IType type)
        {
            this.typeReference = null;
            this.Scope = type?.Scope ?? string.Empty;
            InitializeFromFullName(type?.FullName ?? string.Empty);
            this.hashCode = CalcHashCode();
            this.descriptor = type != null
                ? TypeDescriptor.FromType(type)
                : TypeDescriptor.FromFullName(this.Fullname);
        }

        public TypeKey(string scope, string ns, string name)
            : this(scope, ns, name, ns + "." + name)
        {
        }

        public TypeKey(string scope, string ns, string name, string fullname)
        {
            this.Scope = scope;
            this.Namespace = ns;
            this.Name = name;
            this.Fullname = fullname;

            this.hashCode = CalcHashCode();
            this.descriptor = TypeDescriptor.FromFullName(fullname);
        }

        // Minimal constructor accepting a full type name. Scope will be empty.
        public TypeKey(string fullName)
        {
            this.Scope = string.Empty;
            InitializeFromFullName(fullName ?? string.Empty);
            this.hashCode = CalcHashCode();
            this.descriptor = TypeDescriptor.FromFullName(this.Fullname);
        }

        private void InitializeFromFullName(string fullName)
        {
            this.Fullname = fullName ?? string.Empty;
            int lastDot = this.Fullname.LastIndexOf('.');
            if (lastDot > 0)
            {
                this.Namespace = this.Fullname.Substring(0, lastDot);
                this.Name = this.Fullname.Substring(lastDot + 1);
            }
            else
            {
                this.Namespace = string.Empty;
                this.Name = this.Fullname;
            }
        }

        private int CalcHashCode()
        {
            return Scope.GetHashCode() ^ Namespace.GetHashCode() ^ Name.GetHashCode() ^ Fullname.GetHashCode();
        }

        public TypeDescriptor Descriptor => descriptor;

        public TypeDefinition TypeDefinition
        {
            get { return descriptor?.TypeDefinition ?? typeReference as TypeDefinition; }
        }

        public string Scope { get; }

        public string Namespace { get; private set; }

        public string Name { get; private set; }

        public string Fullname { get; private set; }

        public bool Matches(TypeReference type)
        {
            // Remove generic type parameters and compare full names
            var instanceType = type as GenericInstanceType;
            if (instanceType == null)
                type.DeclaringType = type.DeclaringType; // Hack: Update full name
            string typefullname = type.ToString();
            if (instanceType != null)
                typefullname = instanceType.ElementType.ToString();
            return typefullname == Fullname;
        }

        public bool Equals(TypeKey other)
        {
            return other != null &&
                   hashCode == other.hashCode &&
                   Scope == other.Scope &&
                   Namespace == other.Namespace &&
                   Name == other.Name &&
                   Fullname == other.Fullname;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeKey ? Equals((TypeKey) obj) : false;
        }

        public static bool operator ==(TypeKey a, TypeKey b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.Equals(b);
        }

        public static bool operator !=(TypeKey a, TypeKey b)
        {
            if ((object) a == null)
                return (object) b != null;
            else if ((object) b == null)
                return true;
            else
                return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}", Scope, Fullname);
        }

        public int CompareTo(TypeKey other)
        {
            // no need to check ns and name...should be in fullname
            int cmp = string.Compare(Scope, other.Scope);
            if (cmp == 0)
                cmp = string.Compare(Fullname, other.Fullname);
            return cmp;
        }
    }
}
