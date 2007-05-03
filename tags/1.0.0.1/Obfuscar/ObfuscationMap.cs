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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Obfuscar
{
	enum ObfuscationStatus
	{
		Unknown,
		WillRename,
		Renamed,
		Skipped
	}

	class ObfuscatedThing
	{
		private readonly string name;

		public ObfuscatedThing( string name )
		{
			this.name = name;
		}

		public string Name
		{
			get { return name; }
		}

		public void Update( ObfuscationStatus status, string statusText )
		{
			this.Status = status;
			this.StatusText = statusText;
		}

		public ObfuscationStatus Status = ObfuscationStatus.Unknown;
		public string StatusText;
	}

	class ObfuscatedClass : ObfuscatedThing
	{
		public ObfuscatedClass( string name )
			: base( name )
		{
		}

		public Dictionary<MethodKey, ObfuscatedThing> Methods = new Dictionary<MethodKey,ObfuscatedThing>( );
		public Dictionary<FieldKey, ObfuscatedThing> Fields = new Dictionary<FieldKey, ObfuscatedThing>( );
	}

	class ObfuscationMap
	{
		readonly Dictionary<TypeKey, ObfuscatedClass> classMap = new Dictionary<TypeKey, ObfuscatedClass>( );
		readonly List<ObfuscatedThing> resources = new List<ObfuscatedThing>( );

		public ObfuscatedClass GetClass( TypeKey key )
		{
			ObfuscatedClass c;

			if ( !classMap.TryGetValue( key, out c ) )
			{
				c = new ObfuscatedClass( key.ToString( ) );
				classMap[key] = c;
			}

			return c;
		}

		public ObfuscatedThing GetField( FieldKey key )
		{
			ObfuscatedClass c = GetClass( key.TypeKey );
			
			ObfuscatedThing t;
			if ( !c.Fields.TryGetValue( key, out t ) )
			{
				t = new ObfuscatedThing( key.ToString( ) );
				c.Fields[key] = t;
			}

			return t;
		}

		public ObfuscatedThing GetMethod( MethodKey key )
		{
			ObfuscatedClass c = GetClass( key.TypeKey );

			ObfuscatedThing t;
			if ( !c.Methods.TryGetValue( key, out t ) )
			{
				t = new ObfuscatedThing( key.ToString( ) );
				c.Methods[key] = t;
			}

			return t;
		}

		public void UpdateType( TypeKey key, ObfuscationStatus status, string text )
		{
			ObfuscatedClass c = GetClass( key );

			c.Update( status, text );
		}

		public void UpdateField( FieldKey key, ObfuscationStatus status, string text )
		{
			ObfuscatedThing f = GetField( key );

			f.Update( status, text );
		}

		public void UpdateMethod( MethodKey key, ObfuscationStatus status, string text )
		{
			ObfuscatedThing m = GetMethod( key );

			m.Update( status, text );
		}

		public void AddResource( string name, ObfuscationStatus status, string text )
		{
			ObfuscatedThing r = new ObfuscatedThing( name );

			r.Update( status, text );

			resources.Add( r );
		}

		public void DumpMap( TextWriter writer )
		{
			writer.WriteLine( "Renamed Types:" );

			foreach ( ObfuscatedClass classInfo in classMap.Values )
			{
				// print the ones we didn't skip first
				if ( classInfo.Status == ObfuscationStatus.Renamed )
					DumpClass( writer, classInfo );
			}

			writer.WriteLine( );
			writer.WriteLine( "Skipped Types:" );

			foreach ( ObfuscatedClass classInfo in classMap.Values )
			{
				// now print the stuff we skipped
				if ( classInfo.Status == ObfuscationStatus.Skipped )
					DumpClass( writer, classInfo );
			}

			writer.WriteLine( );
			writer.WriteLine( "Renamed Resources:" );
			writer.WriteLine( );

			foreach ( ObfuscatedThing info in resources )
			{
				if ( info.Status == ObfuscationStatus.Renamed )
					writer.WriteLine( "{0} -> {1}", info.Name, info.StatusText );
			}

			writer.WriteLine( );
			writer.WriteLine( "Skipped Resources:" );
			writer.WriteLine( );

			foreach ( ObfuscatedThing info in resources )
			{
				if ( info.Status == ObfuscationStatus.Skipped )
					writer.WriteLine( "{0} ({1})", info.Name, info.StatusText );
			}
		}

		private void DumpClass( TextWriter writer, ObfuscatedClass classInfo )
		{
			writer.WriteLine( );
			if ( classInfo.Status == ObfuscationStatus.Renamed )
				writer.WriteLine( "{0} -> {1}", classInfo.Name, classInfo.StatusText );
			else
			{
				Debug.Assert( classInfo.Status == ObfuscationStatus.Skipped,
					"Status is expected to be either Renamed or Skipped." );
				writer.WriteLine( "{0} skipped:  {1}", classInfo.Name, classInfo.StatusText );
			}
			writer.WriteLine( "{" );

			int numRenamed = 0;
			foreach ( KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods )
			{
				if ( method.Value.Status == ObfuscationStatus.Renamed )
				{
					DumpMethod( writer, method.Key, method.Value );
					numRenamed++;
				}
			}

			// add a blank line to separate renamed from skipped...it's pretty.
			if ( numRenamed < classInfo.Methods.Count )
				writer.WriteLine( );

			foreach ( KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods )
			{
				if ( method.Value.Status == ObfuscationStatus.Skipped )
					DumpMethod( writer, method.Key, method.Value );
			}

			// add a blank line to separate methods from field...it's pretty.
			if ( classInfo.Methods.Count > 0 && classInfo.Fields.Count > 0 )
				writer.WriteLine( );

			numRenamed = 0;
			foreach ( KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields )
			{
				if ( field.Value.Status == ObfuscationStatus.Renamed )
				{
					DumpField( writer, field.Key, field.Value );
					numRenamed++;
				}
			}

			// add a blank line to separate renamed from skipped...it's pretty.
			if ( numRenamed < classInfo.Fields.Count )
				writer.WriteLine( );

			foreach ( KeyValuePair<FieldKey, ObfuscatedThing> field in classInfo.Fields )
			{
				if ( field.Value.Status == ObfuscationStatus.Skipped )
					DumpField( writer, field.Key, field.Value );
			}

			writer.WriteLine( "}" );
		}

		private void DumpMethod( TextWriter writer, MethodKey key, ObfuscatedThing info )
		{
			writer.Write( "\t{0}(", info.Name );
			for ( int i = 0; i < key.Count; i++ )
			{
				if ( i > 0 )
					writer.Write( ", " );
				else
					writer.Write( " " );

				writer.Write( key.ParamTypes[i] );
			}

			if ( info.Status == ObfuscationStatus.Renamed )
				writer.WriteLine( " ) -> {0}", info.StatusText );
			else
			{
				Debug.Assert( info.Status == ObfuscationStatus.Skipped,
					"Status is expected to be either Renamed or Skipped." );

				writer.WriteLine( " ) skipped:  {0}", info.StatusText );
			}
		}

		private void DumpField( TextWriter writer, FieldKey key, ObfuscatedThing info )
		{
			if ( info.Status == ObfuscationStatus.Renamed )
				writer.WriteLine( "\t{0} {1} -> {2}", key.Type, info.Name, info.StatusText );
			else
			{
				Debug.Assert( info.Status == ObfuscationStatus.Skipped,
					"Status is expected to be either Renamed or Skipped." );

				writer.WriteLine( "\t{0} {1} skipped:  {2}", key.Type, info.Name, info.StatusText );
			}
		}
	}
}
