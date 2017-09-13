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
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Metadata;

namespace Obfuscar
{
    /// <summary>
    /// Used to identify the signature of a method by its name and parameters.
    /// </summary>
    class NameParamSig : ParamSig, IComparable<NameParamSig>
    {
        readonly int hashCode;

        public NameParamSig(string name, string[] paramTypes)
            : base(paramTypes)
        {
            this.Name = name;

            hashCode = CalcHashCode();
        }

        public NameParamSig(MethodReference method)
            : base(method)
        {
            Name = method.Name;

            hashCode = CalcHashCode();
        }

        public NameParamSig(MethodDefinition method)
            : base(method)
        {
            Name = method.Name;

            hashCode = CalcHashCode();
        }

        private int CalcHashCode()
        {
            return Name.GetHashCode() ^ base.GetHashCode();
        }

        public string Name { get; }

        public bool Equals(NameParamSig other)
        {
            return other != null &&
                   Name == other.Name &&
                   Equals((ParamSig) other);
        }

        public override bool Equals(object obj)
        {
            return obj is NameParamSig ? Equals((NameParamSig) obj) : false;
        }

        public static bool operator ==(NameParamSig a, NameParamSig b)
        {
            if ((object) a == null)
                return (object) b == null;
            else if ((object) b == null)
                return false;
            else
                return a.Equals(b);
        }

        public static bool operator !=(NameParamSig a, NameParamSig b)
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
            return String.Format("{0}[{1}]", Name, ParamTypes.Length);
        }

        public int CompareTo(NameParamSig other)
        {
            int cmp = String.Compare(Name, other.Name);
            if (cmp == 0)
                cmp = CompareTo((ParamSig) other);
            return cmp;
        }
    }
}
