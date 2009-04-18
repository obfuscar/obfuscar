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

namespace Obfuscar
{
	class TypeKey : IComparable<TypeKey>
	{
		readonly string scope;
		readonly string ns;
		readonly string name;
		readonly string fullname;

		readonly int hashCode;

		public TypeKey( TypeReference type )
		{
			this.scope = Helper.GetScopeName( type );

			this.name = type.Name;
			TypeReference declaringType = type;
			// Build path to nested type
			while ( declaringType.DeclaringType != null )
			{
				declaringType = declaringType.DeclaringType;
				this.name = declaringType.Name + "/" + name;
			}
			this.ns = declaringType.Namespace;

			this.fullname = !string.IsNullOrEmpty( this.ns ) ? this.ns + "." + name : name;

			// Our name should be the same as the Cecil's name. This is important to the Match method.
			if ( this.fullname != type.ToString( ) )
				throw new InvalidOperationException( string.Format( "Type names do not match: \"{0}\" != \"{1}\"", this.fullname, type.ToString( ) ) );

			this.hashCode = CalcHashCode( );
		}

		public TypeKey( string scope, string ns, string name )
			: this ( scope, ns, name, ns + "." + name )
		{
		}

		public TypeKey( string scope, string ns, string name, string fullname )
		{
			this.scope = scope;
			this.ns = ns;
			this.name = name;
			this.fullname = fullname;

			this.hashCode = CalcHashCode( );
		}

		private int CalcHashCode( )
		{
			return scope.GetHashCode( ) ^ ns.GetHashCode( ) ^ name.GetHashCode( ) ^ fullname.GetHashCode( );
		}

		public string Scope
		{
			get { return scope; }
		}

		public string Namespace
		{
			get { return ns; }
		}

		public string Name
		{
			get { return name; }
		}

		public string Fullname
		{
			get { return fullname; }
		}

		public bool Matches( TypeReference type )
		{
			// Remove generic type parameters and compare full names
			string typefullname = type.ToString( );
			if ( typefullname.EndsWith( ">" ) )
			{
				int pos = typefullname.LastIndexOf( '<' );
				if ( pos < 0 )
					throw new InvalidOperationException( string.Format( "Type \"{0}\" has malformed generic type parameter.", typefullname ) );
				typefullname = typefullname.Substring( 0, pos );
			}

			return typefullname == fullname;
		}

		public bool Equals( TypeKey other )
		{
			return other != null &&
				hashCode == other.hashCode &&
				scope == other.scope &&
				ns == other.ns &&
				name == other.name &&
				fullname == other.fullname;
		}

		public override bool Equals( object obj )
		{
			return obj is TypeKey ? Equals( (TypeKey) obj ) : false;
		}

		public static bool operator ==( TypeKey a, TypeKey b )
		{
			if ( (object) a == null )
				return (object) b == null;
			else if ( (object) b == null )
				return false;
			else
				return a.Equals( b );
		}

		public static bool operator !=( TypeKey a, TypeKey b )
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
			return String.Format( "[{0}]{1}", scope, fullname );
		}

		public int CompareTo( TypeKey other )
		{
			// no need to check ns and name...should be in fullname
			int cmp = String.Compare( scope, other.scope );
			if ( cmp == 0 )
				cmp = String.Compare( fullname, other.fullname );
			return cmp;
		}
	}
}
