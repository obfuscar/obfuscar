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
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuscar
{
	class AssemblyInfo
	{
		private readonly Project project;

		private readonly PredicateCollection<TypeKey> skipTypes = new PredicateCollection<TypeKey>( );
		private readonly PredicateCollection<MethodKey> skipMethods = new PredicateCollection<MethodKey>( );
		private readonly PredicateCollection<FieldKey> skipFields = new PredicateCollection<FieldKey>( );
		private readonly PredicateCollection<PropertyKey> skipProperties = new PredicateCollection<PropertyKey>( );
		private readonly PredicateCollection<EventKey> skipEvents = new PredicateCollection<EventKey>( );

		private readonly List<AssemblyInfo> references = new List<AssemblyInfo>( );
		private readonly List<AssemblyInfo> referencedBy = new List<AssemblyInfo>( );

		private List<TypeReference> unrenamedTypeReferences;
		private List<MemberReference> unrenamedReferences;

		private string filename;

		private AssemblyDefinition definition;
		private string name;

		bool initialized = false;

		// to create, use FromXml
		private AssemblyInfo( Project project )
		{
			this.project = project;
		}

		public static AssemblyInfo FromXml( Project project, XmlReader reader, Variables vars )
		{
			Debug.Assert( reader.NodeType == XmlNodeType.Element && reader.Name == "Module" );

			AssemblyInfo info = new AssemblyInfo( project );

			// pull out the file attribute, but don't process anything empty
			string val = Helper.GetAttribute( reader, "file", vars );
			if ( val.Length > 0 )
				info.LoadAssembly( val );
			else
				throw new InvalidOperationException( "Need valid file attribute." );

			if ( !reader.IsEmptyElement )
			{
				while ( reader.Read( ) )
				{
					if ( reader.NodeType == XmlNodeType.Element )
					{
						switch ( reader.Name )
						{
							case "SkipType":
								{
									val = Helper.GetAttribute( reader, "name", vars );
									if ( val.Length > 0 )
										info.skipTypes.Add( new TypeTester( val ) );
								}
								break;
							case "SkipMethod":
								{
									val = Helper.GetAttribute( reader, "name", vars );
									string type = Helper.GetAttribute( reader, "type", vars );
									string attrib = Helper.GetAttribute( reader, "attrib", vars );

									if ( val.Length > 0 )
										info.skipMethods.Add( new MethodTester( val, type, attrib ) );
									else
									{
										val = Helper.GetAttribute( reader, "rx" );
										if ( val.Length > 0 )
											info.skipMethods.Add( new MethodTester( new Regex( val ), type, attrib ) );
									}
								}
								break;
							case "SkipField":
								{
									val = Helper.GetAttribute( reader, "name", vars );
									string type = Helper.GetAttribute( reader, "type", vars );
									string attrib = Helper.GetAttribute( reader, "attrib", vars );

									if ( val.Length > 0 )
										info.skipFields.Add( new FieldTester( val, type, attrib ) );
									else
									{
										val = Helper.GetAttribute( reader, "rx" );
										if ( val.Length > 0 )
											info.skipFields.Add( new FieldTester( new Regex( val ), type, attrib ) );
									}
								}
								break;
							case "SkipProperty":
								{
									val = Helper.GetAttribute( reader, "name", vars );
									string type = Helper.GetAttribute( reader, "type", vars );
									string attrib = Helper.GetAttribute( reader, "attrib", vars );

									if ( val.Length > 0 )
										info.skipProperties.Add( new PropertyTester( val, type, attrib ) );
									else
									{
										val = Helper.GetAttribute( reader, "rx" );
										if ( val.Length > 0 )
											info.skipProperties.Add( new PropertyTester( new Regex( val ), type, attrib ) );
									}
								}
								break;
							case "SkipEvent":
								{
									val = Helper.GetAttribute( reader, "name", vars );
									string type = Helper.GetAttribute( reader, "type", vars );
									string attrib = Helper.GetAttribute( reader, "attrib", vars );

									if ( val.Length > 0 )
										info.skipEvents.Add( new EventTester( val, type, attrib ) );
									else
									{
										val = Helper.GetAttribute( reader, "rx" );
										if ( val.Length > 0 )
											info.skipEvents.Add( new EventTester( new Regex( val ), type, attrib ) );
									}
								}
								break;
						}
					}
					else if ( reader.NodeType == XmlNodeType.EndElement && reader.Name == "Module" )
					{
						// hit end of module element...stop reading
						break;
					}
				}
			}

			return info;
		}

		/// <summary>
		/// Called by project to finish initializing the assembly.
		/// </summary>
		internal void Init( )
		{
			unrenamedReferences = new List<MemberReference>( );
			foreach ( MemberReference member in definition.MainModule.MemberReferences )
			{
				if ( project.Contains( member.DeclaringType ) )
					unrenamedReferences.Add( member );
			}

			unrenamedTypeReferences = new List<TypeReference>( );
			foreach ( TypeReference type in definition.MainModule.TypeReferences )
			{
				if ( type.FullName == "<Module>" )
					continue;

				if ( project.Contains( type ) )
					unrenamedTypeReferences.Add( type );
			}

			initialized = true;
		}

		private void LoadAssembly( string filename )
		{
			this.filename = filename;

			try
			{
				definition = AssemblyFactory.GetAssembly( filename );
				name = definition.Name.Name;
			}
			catch ( System.IO.FileNotFoundException e )
			{
				throw new ApplicationException( "Unable to find assembly:  " + filename, e );
			}
		}

		public string Filename
		{
			get
			{
				CheckLoaded( );
				return filename;
			}
		}

		public AssemblyDefinition Definition
		{
			get
			{
				CheckLoaded( );
				return definition;
			}
		}

		public string Name
		{
			get
			{
				CheckLoaded( );
				return name;
			}
		}

		public List<MemberReference> UnrenamedReferences
		{
			get
			{
				CheckInitialized( );
				return unrenamedReferences;
			}
		}

		public List<TypeReference> UnrenamedTypeReferences
		{
			get
			{
				CheckInitialized( );
				return unrenamedTypeReferences;
			}
		}

		public List<AssemblyInfo> References
		{
			get { return references; }
		}

		public List<AssemblyInfo> ReferencedBy
		{
			get { return referencedBy; }
		}

		public void ForceSkip( MethodKey method )
		{
			skipMethods.Add( new MethodTester( method ) );
		}

		public bool ShouldSkip( TypeKey type )
		{
			return skipTypes.IsMatch( type );
		}

		public bool ShouldSkip( MethodKey method )
		{
			return skipMethods.IsMatch( method );
		}

		public bool ShouldSkip( FieldKey field )
		{
			return skipFields.IsMatch( field );
		}

		public bool ShouldSkip( PropertyKey prop )
		{
			return skipProperties.IsMatch( prop );
		}

		public bool ShouldSkip( EventKey evt )
		{
			return skipEvents.IsMatch( evt );
		}

		/// <summary>
		/// Makes sure that the assembly definition has been loaded (by <see cref="LoadAssembly"/>).
		/// </summary>
		private void CheckLoaded( )
		{
			if ( definition == null )
				throw new InvalidOperationException( "Expected that AssemblyInfo.LoadAssembly would be called before use." );
		}

		/// <summary>
		/// Makes sure that the assembly has been initialized (by <see cref="Init"/>).
		/// </summary>
		private void CheckInitialized( )
		{
			if ( !initialized )
				throw new InvalidOperationException( "Expected that AssemblyInfo.Init would be called before use." );
		}

		public override string ToString( )
		{
			return Name;
		}
	}
}
