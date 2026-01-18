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
using MethodAttributes = System.Reflection.MethodAttributes;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar
{
    class MethodKey : NameParamSig, IComparable<MethodKey>
    {
        readonly int hashCode;
        private readonly IMethod methodAdapter;

        public MethodKey(MutableMethodDefinition method)
            : this(new TypeKey(method?.DeclaringType), method, method)
        {
        }

        public MethodKey(TypeKey typeKey, MutableMethodDefinition method)
            : this(typeKey, method, method)
        {
        }

        public MethodKey(MutableMethodDefinition method, IMethod metadata)
            : this(new TypeKey(method?.DeclaringType), method, metadata ?? method)
        {
        }

        private MethodKey(TypeKey typeKey, MutableMethodDefinition method, IMethod metadata)
            : base(metadata)
        {
            this.TypeKey = typeKey;
            this.Method = method;
            this.methodAdapter = metadata;

            hashCode = CalcHashCode();
        }

        private int CalcHashCode()
        {
            return TypeKey.GetHashCode() ^ base.GetHashCode();
        }

        public MethodAttributes MethodAttributes
        {
            get { return methodAdapter?.Attributes ?? Method.Attributes; }
        }

        public MutableTypeDefinition DeclaringType
        {
            get { return Method?.DeclaringType as MutableTypeDefinition; }
        }

        public TypeKey TypeKey { get; }

        public MutableMethodDefinition Method { get; }

        public IMethod MethodAdapter => methodAdapter;

        public bool Matches(MutableMethodReference member)
        {
            if (member != null)
            {
                if (TypeKey.Matches(member.DeclaringType))
                    return MethodMatch(Method, member);
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
        internal static bool MethodMatch(MutableMethodDefinition candidate, MutableMethodReference method)
        {
            //if (!candidate.IsVirtual)
            //    return false;

            if (candidate.Name != method.Name)
                return false;

            if (!TypeMatch(candidate.ReturnType, method.ReturnType))
                return false;

            if (candidate.Parameters.Count != method.Parameters.Count)
                return false;

            for (int i = 0; i < candidate.Parameters.Count; i++)
                if (!TypeMatch(candidate.Parameters[i].ParameterType, method.Parameters[i].ParameterType))
                    return false;

            return true;
        }

        private static bool TypeMatch(MutableGenericInstanceType a, MutableGenericInstanceType b)
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

        private static bool TypeMatch(MutableTypeReference a, MutableTypeReference b)
        {
            if (a == null || b == null)
                return a == b;

            if (a is MutableGenericParameter || b is MutableGenericParameter)
                return true;

            if (a is MutableGenericInstanceType ga)
            {
                if (b is not MutableGenericInstanceType gb)
                    return false;
                return TypeMatch(ga, gb);
            }
            if (b is MutableGenericInstanceType)
                return false;

            if (a is MutableArrayType aa)
            {
                if (b is not MutableArrayType ab)
                    return false;
                return aa.Rank == ab.Rank && TypeMatch(aa.ElementType, ab.ElementType);
            }
            if (b is MutableArrayType)
                return false;

            if (a is MutableByReferenceType bra)
            {
                if (b is not MutableByReferenceType brb)
                    return false;
                return TypeMatch(bra.ElementType, brb.ElementType);
            }
            if (b is MutableByReferenceType)
                return false;

            if (a is MutablePointerType pa)
            {
                if (b is not MutablePointerType pb)
                    return false;
                return TypeMatch(pa.ElementType, pb.ElementType);
            }
            if (b is MutablePointerType)
                return false;

            return a.GetFullName() == b.GetFullName();
        }
    }
}
