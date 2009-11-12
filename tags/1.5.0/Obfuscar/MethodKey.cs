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
	class MethodKey : NameParamSig, IComparable<MethodKey>
	{
		readonly TypeKey typeKey;
		readonly int hashCode;
		readonly MethodAttributes methodAttributes;

		public MethodKey( MethodReference method )
			: base( method )
		{
			this.typeKey = new TypeKey( method.DeclaringType );

			hashCode = CalcHashCode( );
		}

		public MethodKey( TypeKey typeKey, string name, string[] paramTypes, MethodAttributes methodAttributes )
			: base( name, paramTypes )
		{
			this.typeKey = typeKey;
			this.methodAttributes = methodAttributes;

			hashCode = CalcHashCode( );
		}

		public MethodKey( TypeKey typeKey, MethodDefinition method )
			: base( method )
		{
			this.typeKey = typeKey;

			hashCode = CalcHashCode( );

			methodAttributes = method.Attributes;
		}

		private int CalcHashCode( )
		{
			return typeKey.GetHashCode( ) ^ base.GetHashCode( );
		}

		public MethodAttributes MethodAttributes
		{
			get { return methodAttributes; }
		}

		public TypeKey TypeKey
		{
			get { return typeKey; }
		}

		public override bool Matches( MemberReference member )
		{
			MethodReference methodRef = member as MethodReference;
			if ( methodRef != null )
			{
				if ( typeKey.Matches( methodRef.DeclaringType ) )
					return base.Matches( member );
			}

			return false;
		}

		public bool Equals( MethodKey other )
		{
			return other != null &&
				hashCode == other.hashCode && 
				( typeKey == null ? other.typeKey == null : typeKey == other.typeKey ) &&
				Equals( (NameParamSig) other );
		}

		public override bool Equals( object obj )
		{
			return obj is MethodKey ? Equals( (MethodKey) obj ) : false;
		}

		public static bool operator ==( MethodKey a, MethodKey b )
		{
			if ( (object) a == null )
				return (object) b == null;
			else if ( (object) b == null )
				return false;
			else
				return a.Equals( b );
		}

		public static bool operator !=( MethodKey a, MethodKey b )
		{
			if ( (object) a == null )
				return (object) b != null;
			else if ( (object) b == null )
				return true;
			else
				return !a.Equals( b );
		}

		public override int GetHashCode( )
		{
			return hashCode;
		}

		public override string ToString( )
		{
			return String.Format( "{0}::{1}", typeKey, base.ToString( ) );
		}

		public int CompareTo( MethodKey other )
		{
			int cmp = CompareTo( (NameParamSig) other );
			if ( cmp == 0 )
				cmp = typeKey.CompareTo( other.typeKey );
			return cmp;
		}
	}
}
