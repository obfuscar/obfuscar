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
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace Obfuscar
{
	interface IMapWriter
	{
		void WriteMap( ObfuscationMap map );
	}

	class TextMapWriter : IMapWriter, IDisposable
	{
		private readonly TextWriter writer;

		public TextMapWriter( TextWriter writer )
		{
			this.writer = writer;
		}

		public void WriteMap( ObfuscationMap map )
		{
			writer.WriteLine( "Renamed Types:" );

			foreach ( ObfuscatedClass classInfo in map.ClassMap.Values )
			{
				// print the ones we didn't skip first
				if ( classInfo.Status == ObfuscationStatus.Renamed )
					DumpClass( classInfo );
			}

			writer.WriteLine( );
			writer.WriteLine( "Skipped Types:" );

			foreach ( ObfuscatedClass classInfo in map.ClassMap.Values )
			{
				// now print the stuff we skipped
				if ( classInfo.Status == ObfuscationStatus.Skipped )
					DumpClass( classInfo );
			}

			writer.WriteLine( );
			writer.WriteLine( "Renamed Resources:" );
			writer.WriteLine( );

			foreach ( ObfuscatedThing info in map.Resources )
			{
				if ( info.Status == ObfuscationStatus.Renamed )
					writer.WriteLine( "{0} -> {1}", info.Name, info.StatusText );
			}

			writer.WriteLine( );
			writer.WriteLine( "Skipped Resources:" );
			writer.WriteLine( );

			foreach ( ObfuscatedThing info in map.Resources )
			{
				if ( info.Status == ObfuscationStatus.Skipped )
					writer.WriteLine( "{0} ({1})", info.Name, info.StatusText );
			}
		}

		private void DumpClass( ObfuscatedClass classInfo )
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
					DumpMethod( method.Key, method.Value );
					numRenamed++;
				}
			}

			// add a blank line to separate renamed from skipped...it's pretty.
			if ( numRenamed < classInfo.Methods.Count )
				writer.WriteLine( );

			foreach ( KeyValuePair<MethodKey, ObfuscatedThing> method in classInfo.Methods )
			{
				if ( method.Value.Status == ObfuscationStatus.Skipped )
					DumpMethod( method.Key, method.Value );
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

			// add a blank line to separate props...it's pretty.
			if ( classInfo.Properties.Count > 0 )
				writer.WriteLine( );

			numRenamed = 0;
			foreach ( KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties )
			{
				if ( field.Value.Status == ObfuscationStatus.Renamed )
				{
					DumpProperty( writer, field.Key, field.Value );
					numRenamed++;
				}
			}

			// add a blank line to separate renamed from skipped...it's pretty.
			if ( numRenamed < classInfo.Properties.Count )
				writer.WriteLine( );

			foreach ( KeyValuePair<PropertyKey, ObfuscatedThing> field in classInfo.Properties )
			{
				if ( field.Value.Status == ObfuscationStatus.Skipped )
					DumpProperty( writer, field.Key, field.Value );
			}

			// add a blank line to separate events...it's pretty.
			if ( classInfo.Events.Count > 0 )
				writer.WriteLine( );

			numRenamed = 0;
			foreach ( KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events )
			{
				if ( field.Value.Status == ObfuscationStatus.Renamed )
				{
					DumpEvent( writer, field.Key, field.Value );
					numRenamed++;
				}
			}

			// add a blank line to separate renamed from skipped...it's pretty.
			if ( numRenamed < classInfo.Events.Count )
				writer.WriteLine( );

			foreach ( KeyValuePair<EventKey, ObfuscatedThing> field in classInfo.Events )
			{
				if ( field.Value.Status == ObfuscationStatus.Skipped )
					DumpEvent( writer, field.Key, field.Value );
			}

			writer.WriteLine( "}" );
		}

		private void DumpMethod( MethodKey key, ObfuscatedThing info )
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

		private void DumpProperty( TextWriter writer, PropertyKey key, ObfuscatedThing info )
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

		private void DumpEvent( TextWriter writer, EventKey key, ObfuscatedThing info )
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

		public void Dispose( )
		{
			writer.Close( );
		}
	}

	class XmlMapWriter : IMapWriter, IDisposable
	{
		private readonly XmlWriter writer;

		public XmlMapWriter( TextWriter writer )
		{
			this.writer = new XmlTextWriter( writer );
		}

		public void WriteMap( ObfuscationMap map )
		{
		}

		public void Dispose( )
		{
			writer.Close( );
		}
	}
}
