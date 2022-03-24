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
using System.Diagnostics;
using Mono.Cecil;

namespace Obfuscar
{
    class MethodKey : NameParamSig, IComparable<MethodKey>
    {
        readonly int hashCode;

        public MethodKey(MethodDefinition method) : this(new TypeKey((TypeDefinition) method.DeclaringType), method)
        {
        }

        public MethodKey(TypeKey typeKey, MethodDefinition method)
            : base(method)
        {
            this.TypeKey = typeKey;
            this.Method = method;

            hashCode = CalcHashCode();
        }

        private int CalcHashCode()
        {
            return TypeKey.GetHashCode() ^ base.GetHashCode();
        }

        public MethodAttributes MethodAttributes
        {
            get { return Method.Attributes; }
        }

        public TypeDefinition DeclaringType
        {
            get { return (TypeDefinition) Method.DeclaringType; }
        }

        public TypeKey TypeKey { get; }

        public MethodDefinition Method { get; }

        public bool Matches(MemberReference member)
        {
            MethodReference methodRef = member as MethodReference;
            if (methodRef != null)
            {
                if (TypeKey.Matches(methodRef.DeclaringType))
                    return MethodMatch(Method, methodRef);
            }

            return false;
        }

        public bool Equals(MethodKey other)
        {
            return other != null &&
                   hashCode == other.hashCode &&
                   (TypeKey == null ? other.TypeKey == null : TypeKey == other.TypeKey) &&
                   Equals((NameParamSig) other);
        }

        public override bool Equals(object obj)
        {
            return obj is MethodKey ? Equals((MethodKey) obj) : false;
        }

        public static bool operator ==(MethodKey a, MethodKey b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.Equals(b);
        }

        public static bool operator !=(MethodKey a, MethodKey b)
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
            return string.Format("{0}::{1}", TypeKey, base.ToString());
        }

        public int CompareTo(MethodKey other)
        {
            int cmp = CompareTo((NameParamSig) other);
            if (cmp == 0)
                cmp = TypeKey.CompareTo(other.TypeKey);
            return cmp;
        }

        // taken from https://github.com/mono/mono/blob/master/mcs/tools/linker/Mono.Linker.Steps/TypeMapStep.cs
        internal static bool MethodMatch(MethodDefinition candidate, MethodReference method)
        {
            if (candidate == null && method == null)
            {
                return true;
            }

            if (candidate == null || method == null)
            {
                return false;
            }

            //if (!candidate.IsVirtual)
            //    return false;

            if (candidate.Parameters.Count != method.Parameters.Count)
                return false;

            var expandedName = $"{candidate.DeclaringType.FullName}.{candidate.Name}";
            if (candidate.Name != method.Name && expandedName != ToGeneralGenericMemberName(method.Name))
                return false;

            if (!TypeMatch(candidate.ReturnType, method.ReturnType))
                return false;

            for (int i = 0; i < candidate.Parameters.Count; i++)
                if (!TypeMatch(candidate.Parameters[i].ParameterType, method.Parameters[i].ParameterType))
                    return false;

            return true;
        }

        // Convert member names from Type<T>.Member to Type`1.Member form
        internal static string ToGeneralGenericMemberName(string memberName)
        {
            int iEnd;
            if ((iEnd = memberName.LastIndexOf(">.")) < 0)
                return memberName;
            int iStart = memberName.IndexOf("<");
            int genericArgumentCount = 1;
            int level = 0;
            for (int i = iStart + 1; i < iEnd; i++)
            {
                switch (memberName[i])
                {
                    case '<':
                        level++;
                        break;
                    case '>':
                        level--;
                        break;
                    case ',':
                        //if next generic type within first generic type listing
                        if (level == 0)
                            genericArgumentCount++;

                        break;
                }
            }
            Debug.Assert(level == 0);
            return memberName = memberName.Substring(0, iStart) + $"`{genericArgumentCount}" + memberName.Substring(iEnd + 1);
        }

        private static bool TypeMatch(IModifierType a, IModifierType b)
        {
            if (!TypeMatch(a.ModifierType, b.ModifierType))
                return false;

            return TypeMatch(a.ElementType, b.ElementType);
        }

        private static bool TypeMatch(TypeSpecification a, TypeSpecification b)
        {
            if (a is GenericInstanceType)
                return TypeMatch((GenericInstanceType) a, (GenericInstanceType) b);

            if (a is IModifierType)
                return TypeMatch((IModifierType) a, (IModifierType) b);

            return TypeMatch(a.ElementType, b.ElementType);
        }

        private static bool TypeMatch(GenericInstanceType a, GenericInstanceType b)
        {
            if (!TypeMatch(a.ElementType, b.ElementType))
                return false;

            if (a.GenericArguments.Count != b.GenericArguments.Count)
                return false;

            if (a.GenericArguments.Count == 0)
                return true;

            for (int i = 0; i < a.GenericArguments.Count; i++)
                if (!TypeMatch(a.GenericArguments[i], b.GenericArguments[i]))
                    return false;

            return true;
        }

        private static bool TypeMatch(TypeReference a, TypeReference b)
        {
            if (a is GenericParameter)
                return true;

            if (a is TypeSpecification || b is TypeSpecification)
            {
                if (a.GetType() != b.GetType())
                    return false;

                return TypeMatch((TypeSpecification) a, (TypeSpecification) b);
            }

            return a.FullName == b.FullName;
        }
    }
}
