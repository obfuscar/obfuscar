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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;

using Mono.Cecil;
using Mono.Security.Cryptography;
using System.Security.Cryptography;

namespace Obfuscar
{
	class Project : IEnumerable<AssemblyInfo>
	{
		private readonly List<AssemblyInfo> assemblyList = new List<AssemblyInfo>( );
		private readonly Dictionary<string, AssemblyInfo> assemblyMap = new Dictionary<string, AssemblyInfo>( );

		private readonly Variables vars = new Variables( );

		InheritMap inheritMap;
		Settings settings;
		private RSA keyvalue;

		// don't create.  call FromXml.
		private Project( )
		{
		}

		public RSA KeyValue
		{
			get
			{
				if (keyvalue != null)
					return keyvalue;
				if (vars.GetValue("KeyFile", null) == null)
					return null;
				try
				{
					keyvalue = CryptoConvert.FromCapiKeyBlob(File.ReadAllBytes(vars.GetValue("KeyFile", null)));
				}
				catch (Exception ex)
				{
					throw new ApplicationException(String.Format("Failure loading key file \"{0}\"", vars.GetValue("KeyFile", null)), ex);
				}
				return keyvalue;
			}
		}

		public static Project FromXml( XmlReader reader )
		{
			Project project = new Project( );

			while ( reader.Read( ) )
			{
				if ( reader.NodeType == XmlNodeType.Element )
				{
					switch ( reader.Name )
					{
						case "Var":
						{
							string name = Helper.GetAttribute( reader, "name" );
							if ( name.Length > 0 )
							{
								string value = Helper.GetAttribute( reader, "value" );
								if ( value.Length > 0 )
									project.vars.Add( name, value );
								else
									project.vars.Remove( name );
							}
							break;
						}
						case "Module":
							AssemblyInfo info = AssemblyInfo.FromXml( project, reader, project.vars );
							project.assemblyList.Add( info );
							project.assemblyMap[info.Name] = info;
							break;
					}
				}
			}

			return project;
		}

		/// <summary>
		/// Looks through the settings, trys to make sure everything looks ok.
		/// </summary>
		public void CheckSettings( )
		{
			if ( !Directory.Exists( Settings.InPath ) )
				throw new ApplicationException( "Path specified by InPath variable must exist:" + Settings.InPath );

			if ( !Directory.Exists( Settings.OutPath ) )
			{
				try
				{
					Directory.CreateDirectory( Settings.OutPath );
				}
				catch ( IOException e )
				{
					throw new ApplicationException( "Could not create path specified by OutPath:  " + Settings.OutPath, e );
				}
			}
		}

		public InheritMap InheritMap
		{
			get { return inheritMap; }
		}

		public Settings Settings
		{
			get
			{
				if ( settings == null )
					settings = new Settings( vars );

				return settings;
			}
		}

		public void LoadAssemblies( )
		{
			// make everything fully load
			foreach ( AssemblyInfo info in assemblyList )
				info.Definition.MainModule.FullLoad( );

			// build reference tree
			foreach ( AssemblyInfo info in assemblyList )
			{
				// add self reference...makes things easier later, when
				// we need to go through the member references
				info.ReferencedBy.Add( info );

				// try to get each assembly referenced by this one.  if it's in
				// the map (and therefore in the project), set up the mappings
				foreach ( AssemblyNameReference nameRef in info.Definition.MainModule.AssemblyReferences )
				{
					AssemblyInfo reference;
					if ( assemblyMap.TryGetValue( nameRef.Name, out reference ) )
					{
						info.References.Add( reference );
						reference.ReferencedBy.Add( info );
					}
				}
			}

			// make each assembly's list of member refs
			foreach ( AssemblyInfo info in assemblyList )
				info.Init( );

			// build inheritance map
			inheritMap = new InheritMap( this );
		}

		/// <summary>
		/// Returns whether the project contains a given type.
		/// </summary>
		public bool Contains( TypeReference type )
		{
			string name = Helper.GetScopeName( type );

			return assemblyMap.ContainsKey( name );
		}

		/// <summary>
		/// Returns whether the project contains a given type.
		/// </summary>
		public bool Contains( TypeKey type )
		{
			return assemblyMap.ContainsKey( type.Scope );
		}

		public TypeDefinition GetTypeDefinition( TypeReference type )
		{
			if ( type == null )
				return null;

			TypeDefinition typeDef = type as TypeDefinition;
			if ( typeDef == null )
			{
				string name = Helper.GetScopeName( type );

				AssemblyInfo info;
				if ( assemblyMap.TryGetValue( name, out info ) )
				{
					string fullName = type.Namespace + "." + type.Name;
					typeDef = info.Definition.MainModule.Types[fullName];
				}
			}

			return typeDef;
		}

		IEnumerator IEnumerable.GetEnumerator( )
		{
			return assemblyList.GetEnumerator( );
		}

		public IEnumerator<AssemblyInfo> GetEnumerator( )
		{
			return assemblyList.GetEnumerator( );
		}
	}
}
