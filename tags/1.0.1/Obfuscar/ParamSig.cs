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
	/// Used to identify the signature of a method by its parameters.
	/// </summary>
	class ParamSig : IComparable<ParamSig>
	{
		readonly string[] paramTypes;
		readonly int hashCode;

		public ParamSig( ParamSig sig )
		{
			paramTypes = (string[]) sig.paramTypes.Clone( );
			
			hashCode = CalcHashCode( );
		}

		public ParamSig( MethodReference method )
		{
			paramTypes = new string[method.Parameters.Count];

			int i = 0;
			foreach ( ParameterDefinition param in method.Parameters )
				paramTypes[i++] = Helper.GetParameterTypeName( param );

			hashCode = CalcHashCode( );
		}

		public ParamSig( MethodDefinition method )
		{
			paramTypes = new string[method.Parameters.Count];

			int i = 0;
			foreach ( ParameterDefinition param in method.Parameters )
				paramTypes[i++] = Helper.GetParameterTypeName( param );

			hashCode = CalcHashCode( );
		}

		private int CalcHashCode( )
		{
			int hashCode = 0;
			for ( int i = 0; i < paramTypes.Length; i++ )
				hashCode ^= paramTypes[i].GetHashCode( );
			return hashCode;
		}

		public int Count
		{
			get { return paramTypes.Length; }
		}

		public string this[int index]
		{
			get { return paramTypes[index]; }
		}

		public string[] ParamTypes
		{
			get { return paramTypes; }
		}

		public bool Equals( ParamSig other )
		{
			return other != null &&
				hashCode == other.hashCode && 
				ListHelper.ListsEqual( paramTypes, other.paramTypes );
		}

		public override bool Equals( object obj )
		{
			return obj is ParamSig ? Equals( (ParamSig) obj ) : false;
		}

		public static bool operator ==( ParamSig a, ParamSig b )
		{
			if ( (object) a == null )
				return (object) b == null;
			else if ( (object) b == null )
				return false;
			else
				return a.Equals( b );
		}

		public static bool operator !=( ParamSig a, ParamSig b )
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
			return String.Format( "[{0}]", paramTypes.Length );
		}

		public int CompareTo( ParamSig other )
		{
			if ( paramTypes.Length < other.paramTypes.Length )
				return -1;
			else if ( paramTypes.Length > other.paramTypes.Length )
				return 1;
			else
			{
				for ( int i = 0; i < paramTypes.Length; i++ )
				{
					int cmp = String.Compare( paramTypes[i], other.paramTypes[i] );
					if ( cmp != 0 )
						return cmp;
				}

				return 0;
			}
		}
	}
}
