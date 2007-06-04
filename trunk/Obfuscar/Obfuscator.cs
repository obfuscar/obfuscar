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
	class Obfuscator
	{
		Project project;

		ObfuscationMap map = new ObfuscationMap( );

		/// <summary>
		/// Creates an obfuscator initialized from a project file.
		/// </summary>
		/// <param name="projfile">Path to project file.</param>
		public Obfuscator( string projfile )
		{
			// open XmlTextReader over xml string stream
			XmlReaderSettings settings = GetReaderSettings( );

			try
			{
				using ( XmlReader reader = XmlTextReader.Create( System.IO.File.OpenRead( projfile ), settings ) )
					LoadFromReader( reader );
			}
			catch ( System.IO.IOException e )
			{
				throw new ApplicationException( "Unable to read specified project file:  " + projfile, e );
			}
		}

		/// <summary>
		/// Creates an obfuscator initialized from a project file.
		/// </summary>
		/// <param name="projfile">Reader for project file.</param>
		public Obfuscator( XmlReader reader )
		{
			LoadFromReader( reader );
		}

		public static Obfuscator CreateFromXml( string xml )
		{
			// open XmlTextReader over xml string stream
			XmlReaderSettings settings = GetReaderSettings( );

			using ( XmlReader reader = XmlTextReader.Create( new System.IO.StringReader( xml ), settings ) )
				return new Obfuscar.Obfuscator( reader );
		}

		static XmlReaderSettings GetReaderSettings( )
		{
			XmlReaderSettings settings = new XmlReaderSettings( );
			settings.IgnoreProcessingInstructions = true;
			settings.IgnoreWhitespace = true;
			settings.XmlResolver = null;
			settings.ProhibitDtd = false;
			return settings;
		}

		void LoadFromReader( XmlReader reader )
		{
			project = Project.FromXml( reader );

			// make sure everything looks good
			project.CheckSettings( );

			Console.Write( "Loading assemblies..." );
			project.LoadAssemblies( );
		}

		/// <summary>
		/// Saves changes made to assemblies to the output path.
		/// </summary>
		public void SaveAssemblies( )
		{
			string outPath = project.Settings.OutPath;

			// save the modified assemblies
			foreach ( AssemblyInfo info in project )
			{
				string outName = System.IO.Path.Combine( outPath,
					System.IO.Path.GetFileName( info.Filename ) );

				AssemblyFactory.SaveAssembly( info.Definition, outName );
			}
		}

		/// <summary>
		/// Saves the name mapping to the output path.
		/// </summary>
		public void SaveMapping( )
		{
			string filename = project.Settings.XmlMapping?
				"Mapping.xml" : "Mapping.txt";

			string logPath = System.IO.Path.Combine( project.Settings.OutPath, filename );

			using ( System.IO.TextWriter file = System.IO.File.CreateText( logPath ) )
				SaveMapping( file );
		}

		/// <summary>
		/// Saves the name mapping to a text writer.
		/// </summary>
		public void SaveMapping( System.IO.TextWriter writer )
		{
			IMapWriter mapWriter = project.Settings.XmlMapping ?
				(IMapWriter) new XmlMapWriter( writer ) : (IMapWriter) new TextMapWriter( writer );

			mapWriter.WriteMap( map );
		}

		/// <summary>
		/// Returns the obfuscation map for the project.
		/// </summary>
		public ObfuscationMap Mapping
		{
			get { return map; }
		}

		/// <summary>
		/// Renames fields in the project.
		/// </summary>
		public void RenameFields( )
		{
			Dictionary<string, NameGroup> nameGroups = new Dictionary<string, NameGroup>( );

			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				// loop through the types
				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey typeKey = new TypeKey( type );

					if ( ShouldRename( type ) )
					{
						nameGroups.Clear( );

						// rename field, grouping according to signature

						foreach ( FieldDefinition field in type.Fields )
						{
							string sig = field.FieldType.FullName;
							FieldKey fieldKey = new FieldKey( typeKey, sig, field.Name );

							NameGroup nameGroup = GetNameGroup( nameGroups, sig );

							// skip filtered fields
							if ( info.ShouldSkip( fieldKey ) )
							{
								map.UpdateField( fieldKey, ObfuscationStatus.Skipped, "filtered" );
								nameGroup.Add( fieldKey.Name );
							}
							else
							{
								string newName = nameGroup.GetNext( );

								RenameField( info, fieldKey, field, newName );

								nameGroup.Add( newName );
							}
						}
					}
				}
			}
		}

		void RenameField( AssemblyInfo info, FieldKey fieldKey, FieldDefinition field, string newName )
		{
			// find references, rename them, then rename the field itself

			foreach ( AssemblyInfo reference in info.ReferencedBy )
			{
				for ( int i = 0; i < reference.UnrenamedReferences.Count; )
				{
					FieldReference member = reference.UnrenamedReferences[i] as FieldReference;
					if ( member != null )
					{
						if ( fieldKey.Matches( member ) )
						{
							member.Name = newName;
							reference.UnrenamedReferences.RemoveAt( i );

							// since we removed one, continue without the increment
							continue;
						}
					}

					i++;
				}
			}

			field.Name = newName;

			map.UpdateField( fieldKey, ObfuscationStatus.Renamed, newName );
		}

		/// <summary>
		/// Renames constructor, method, and generic parameters.
		/// </summary>
		public void RenameParams( )
		{
			int index;

			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				// loop through the types
				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					if ( ShouldRename( type ) )
					{
						// rename the constructor parameters
						foreach ( MethodDefinition method in type.Constructors )
							RenameParams( method );

						// rename the method parameters
						foreach ( MethodDefinition method in type.Methods )
							RenameParams( method );

						// rename the class parameters
						index = 0;
						foreach ( GenericParameter param in type.GenericParameters )
							param.Name = NameMaker.UniqueName( index++ );
					}
				}
			}
		}

		void RenameParams( MethodDefinition method )
		{
			int index = 0;
			foreach ( ParameterReference param in method.Parameters )
				param.Name = NameMaker.UniqueName( index++ );

			index = 0;
			foreach ( GenericParameter param in method.GenericParameters )
				param.Name = NameMaker.UniqueName( index++ );
		}

		bool ShouldRename( TypeDefinition type )
		{
			const string ctor = "System.Void Obfuscar.ObfuscateAttribute::.ctor()";

			bool should = !project.Settings.MarkedOnly;

			foreach ( CustomAttribute attr in type.CustomAttributes )
			{
				if ( attr.Constructor.ToString( ) == ctor )
				{
					// determine the result from the property, default to true if missing
					object obj = attr.Properties["ShouldObfuscate"];
					if ( obj != null )
						should = (bool) obj;
					else
						should = true;

					break;
				}
			}

			return should;
		}

		/// <summary>
		/// Renames types and resources in the project.
		/// </summary>
		public void RenameTypes( )
		{
			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				// make a list of the resources that can be renamed
				List<Resource> resources = new List<Resource>( library.MainModule.Resources.Count );
				foreach ( Resource res in library.MainModule.Resources )
					resources.Add( res );

				// loop through the types
				int typeIndex = 0;
				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey oldTypeKey = new TypeKey( type );
					string fullName = type.FullName;

					if ( ShouldRename( type ) )
					{
						if ( !info.ShouldSkip( oldTypeKey ) )
						{
							TypeKey newTypeKey = new TypeKey( info.Name,
								NameMaker.UniqueNamespace( typeIndex ), NameMaker.UniqueTypeName( typeIndex ) );
							typeIndex++;

							// go through the list of renamed types and try to rename resources
							for ( int i = 0; i < resources.Count; )
							{
								Resource res = resources[i];
								string resName = res.Name;

								if ( resName.StartsWith( fullName + "." ) )
								{
									string suffix = resName.Substring( fullName.Length );
									string newName = newTypeKey.Fullname + suffix;

									res.Name = newName;
									resources.RemoveAt( i );

									map.AddResource( resName, ObfuscationStatus.Renamed, newName );
								}
								else
									i++;
							}

							RenameType( info, type, oldTypeKey, newTypeKey );
						}
						else
						{
							map.UpdateType( oldTypeKey, ObfuscationStatus.Skipped, "filtered" );

							// go through the list of resources, remove ones that would be renamed
							for ( int i = 0; i < resources.Count; )
							{
								Resource res = resources[i];
								string resName = res.Name;

								if ( resName.StartsWith( fullName + "." ) )
								{
									resources.RemoveAt( i );
									map.AddResource( resName, ObfuscationStatus.Skipped, "filtered" );
								}
								else
									i++;
							}
						}
					}
					else
					{
						map.UpdateType( oldTypeKey, ObfuscationStatus.Skipped, "marked" );

						// go through the list of resources, remove ones that would be renamed
						for ( int i = 0; i < resources.Count; )
						{
							Resource res = resources[i];
							string resName = res.Name;

							if ( resName.StartsWith( fullName + "." ) )
							{
								resources.RemoveAt( i );
								map.AddResource( resName, ObfuscationStatus.Skipped, "marked" );
							}
							else
								i++;
						}
					}
				}

				foreach ( Resource res in resources )
					map.AddResource( res.Name, ObfuscationStatus.Skipped, "no clear new name" );
			}
		}

		void RenameType( AssemblyInfo info, TypeDefinition type, TypeKey oldTypeKey, TypeKey newTypeKey )
		{
			// find references, rename them, then rename the type itself

			foreach ( AssemblyInfo reference in info.ReferencedBy )
			{
				for ( int i = 0; i < reference.UnrenamedTypeReferences.Count; )
				{
					TypeReference refType = reference.UnrenamedTypeReferences[i];

					// check whether the referencing module references this type...if so,
					// rename the reference
					if ( oldTypeKey.Matches( refType ) )
					{
						refType.Namespace = newTypeKey.Namespace;
						refType.Name = newTypeKey.Name;

						reference.UnrenamedTypeReferences.RemoveAt( i );

						// since we removed one, continue without the increment
						continue;
					}

					i++;
				}
			}

			type.Namespace = newTypeKey.Namespace;
			type.Name = newTypeKey.Name;

			map.UpdateType( oldTypeKey, ObfuscationStatus.Renamed, newTypeKey.ToString( ) );
		}

		Dictionary<ParamSig, NameGroup> GetSigNames( Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
			TypeKey typeKey )
		{
			Dictionary<ParamSig, NameGroup> sigNames;
			if ( !baseSigNames.TryGetValue( typeKey, out sigNames ) )
			{
				sigNames = new Dictionary<ParamSig, NameGroup>( );
				baseSigNames[typeKey] = sigNames;
			}
			return sigNames;
		}

		NameGroup GetNameGroup( Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
			TypeKey typeKey, ParamSig sig )
		{
			return GetNameGroup( GetSigNames( baseSigNames, typeKey ), sig );
		}


		NameGroup GetNameGroup<KeyType>( Dictionary<KeyType, NameGroup> sigNames, KeyType sig )
		{
			NameGroup nameGroup;
			if ( !sigNames.TryGetValue( sig, out nameGroup ) )
			{
				nameGroup = new NameGroup( );
				sigNames[sig] = nameGroup;
			}
			return nameGroup;
		}

		public void RenameProperties( )
		{
			// do nothing if it was requested not to rename
			if ( !project.Settings.RenameProperties )
				return;

			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey typeKey = new TypeKey( type );

					if ( ShouldRename( type ) )
					{
						List<PropertyDefinition> propsToDrop = new List<PropertyDefinition>( );
						foreach ( PropertyDefinition prop in type.Properties )
						{
							PropertyKey propKey = new PropertyKey( typeKey, prop );
							ObfuscatedThing m = map.GetProperty( propKey );

							// skip runtime special properties
							if ( prop.IsRuntimeSpecialName )
							{
								m.Update( ObfuscationStatus.Skipped, "runtime special" );
								continue;
							}

							// skip filtered props
							if ( info.ShouldSkip( propKey ) )
							{
								m.Update( ObfuscationStatus.Skipped, "filtered" );

								// make sure get/set get skipped too
								if ( prop.GetMethod != null )
									info.ForceSkip( new MethodKey( prop.GetMethod ) );
								if ( prop.SetMethod != null )
									info.ForceSkip( new MethodKey( prop.SetMethod ) );

								continue;
							}

							// add to to collection for removal
							propsToDrop.Add( prop );
						}

						foreach ( PropertyDefinition prop in propsToDrop )
						{
							PropertyKey propKey = new PropertyKey( typeKey, prop );
							ObfuscatedThing m = map.GetProperty( propKey );

							m.Update( ObfuscationStatus.Renamed, "dropped" );
							type.Properties.Remove( prop );
						}
					}
				}
			}
		}

		public void RenameEvents( )
		{
			// do nothing if it was requested not to rename
			if ( !project.Settings.RenameEvents )
				return;

			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey typeKey = new TypeKey( type );

					if ( ShouldRename( type ) )
					{
						List<EventDefinition> evtsToDrop = new List<EventDefinition>( );
						foreach ( EventDefinition evt in type.Events )
						{
							EventKey evtKey = new EventKey( typeKey, evt );
							ObfuscatedThing m = map.GetEvent( evtKey );

							// skip runtime special events
							if ( evt.IsRuntimeSpecialName )
							{
								m.Update( ObfuscationStatus.Skipped, "runtime special" );
								continue;
							}

							// skip filtered events
							if ( info.ShouldSkip( evtKey ) )
							{
								m.Update( ObfuscationStatus.Skipped, "filtered" );

								// make sure add/remove get skipped too
								info.ForceSkip( new MethodKey( evt.AddMethod ) );
								info.ForceSkip( new MethodKey( evt.RemoveMethod ) );

								continue;
							}

							// add to to collection for removal
							evtsToDrop.Add( evt );
						}

						foreach ( EventDefinition evt in evtsToDrop )
						{
							EventKey evtKey = new EventKey( typeKey, evt );
							ObfuscatedThing m = map.GetEvent( evtKey );

							m.Update( ObfuscationStatus.Renamed, "dropped" );
							type.Events.Remove( evt );
						}
					}
				}
			}
		}

		public void RenameMethods( )
		{
			Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames = 
				new Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>>( );

			foreach ( AssemblyInfo info in project )
			{
				AssemblyDefinition library = info.Definition;

				foreach ( TypeDefinition type in library.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey typeKey = new TypeKey( type );

					if ( ShouldRename( type ) )
					{
						Dictionary<ParamSig, NameGroup> sigNames = GetSigNames( baseSigNames, typeKey );

						// first pass.  mark grouped virtual methods to be renamed, and mark some things
						// to be skipped as neccessary
						foreach ( MethodDefinition method in type.Methods )
						{
							MethodKey methodKey = new MethodKey( typeKey, method );
							ObfuscatedThing m = map.GetMethod( methodKey );

							// skip runtime methods
							if ( method.IsRuntime )
							{
								m.Update( ObfuscationStatus.Skipped, "runtime method" );
								continue;
							}

							// skip filtered methods
							if ( info.ShouldSkip( methodKey ) )
							{
								m.Update( ObfuscationStatus.Skipped, "filtered" );
								continue;
							}

							if ( !method.IsVirtual )
								continue;

							// if we already have a name planned for a method, leave it alone
							if ( m.Status == ObfuscationStatus.WillRename )
								continue;

							if ( method.IsSpecialName )
							{
								switch ( method.SemanticsAttributes )
								{
									case MethodSemanticsAttributes.Getter:
									case MethodSemanticsAttributes.Setter:
										if ( project.Settings.RenameProperties )
											RenameVirtualMethod( info, baseSigNames, sigNames, methodKey, method );
										else
											m.Update( ObfuscationStatus.Skipped, "skipping properties" );
										break;
									case MethodSemanticsAttributes.AddOn:
									case MethodSemanticsAttributes.RemoveOn:
										if ( project.Settings.RenameEvents )
											RenameVirtualMethod( info, baseSigNames, sigNames, methodKey, method );
										else
											m.Update( ObfuscationStatus.Skipped, "skipping events" );
										break;
									default:
										m.Update( ObfuscationStatus.Skipped, "virtual and special name" );
										break;
								}
							}
							else
								RenameVirtualMethod( info, baseSigNames, sigNames, methodKey, method );
						}

						// update name groups, so new names don't step on inherited ones
						foreach ( TypeKey baseType in project.InheritMap.GetBaseTypes( typeKey ) )
						{
							Dictionary<ParamSig, NameGroup> baseNames = GetSigNames( baseSigNames, baseType );
							foreach ( KeyValuePair<ParamSig, NameGroup> pair in baseNames )
							{
								NameGroup nameGroup = GetNameGroup( sigNames, pair.Key );
								nameGroup.AddAll( pair.Value );
							}
						}

						// second pass...marked virtuals and anything not skipped get renamed
						foreach ( MethodDefinition method in type.Methods )
						{
							MethodKey methodKey = new MethodKey( typeKey, method );
							ObfuscatedThing m = map.GetMethod( methodKey );

							// if we already decided to skip it, leave it alone
							if ( m.Status == ObfuscationStatus.Skipped )
								continue;

							if ( method.IsSpecialName )
							{
								switch ( method.SemanticsAttributes )
								{
									case MethodSemanticsAttributes.Getter:
									case MethodSemanticsAttributes.Setter:
										if ( project.Settings.RenameProperties )
										{
											RenameMethod( info, sigNames, methodKey, method );
											method.SemanticsAttributes = 0;
										}
										else
											m.Update( ObfuscationStatus.Skipped, "skipping properties" );
										break;
									case MethodSemanticsAttributes.AddOn:
									case MethodSemanticsAttributes.RemoveOn:
										if ( project.Settings.RenameEvents )
										{
											RenameMethod( info, sigNames, methodKey, method );
											method.SemanticsAttributes = 0;
										}
										else
											m.Update( ObfuscationStatus.Skipped, "skipping events" );
										break;
									default:
										m.Update( ObfuscationStatus.Skipped, "special name" );
										break;
								}
							}
							else
								RenameMethod( info, sigNames, methodKey, method );
						}
					}
				}
			}
		}

		void RenameVirtualMethod( AssemblyInfo info, Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
			Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method )
		{
			// if method is in a group, look for group key
			MethodGroup group = project.InheritMap.GetMethodGroup( methodKey );
			if ( group != null )
			{
				string groupName = group.Name;
				if ( groupName == null )
				{
					// group is not yet named

					// counts are grouping according to signature
					ParamSig sig = new ParamSig( method );

					// get name groups for classes in the group
					NameGroup[] nameGroups = GetNameGroups( baseSigNames, group.Methods, sig );

					if ( group.External )
					{
						// for an external group, we can't rename.  just use the method 
						// name as group name
						groupName = method.Name;
					}
					else
					{
						// for an internal group, get next unused name
						groupName = NameGroup.GetNext( nameGroups );
					}

					group.Name = groupName;

					// set up methods to be renamed
					foreach ( MethodKey m in group.Methods )
						map.UpdateMethod( m, ObfuscationStatus.WillRename, groupName );

					// make sure the classes' name groups are updated
					for ( int i = 0; i < nameGroups.Length; i ++ )
						nameGroups[i].Add( groupName );
				}
				else
				{
					ObfuscatedThing m = map.GetMethod( methodKey );
					Debug.Assert( 
						( m.Status == ObfuscationStatus.WillRename || m.Status == ObfuscationStatus.Renamed ) &&
						m.StatusText == groupName,
						"If the group is already has a name...method should have one too." );
				}
			}
		}

		NameGroup[] GetNameGroups( Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
			IEnumerable<MethodKey> methodKeys, ParamSig sig )
		{
			// build unique set of classes in group
			C5.HashSet<TypeKey> typeKeys = new C5.HashSet<TypeKey>( );
			foreach ( MethodKey methodKey in methodKeys )
				typeKeys.Add( methodKey.TypeKey );

			// build list of namegroups
			NameGroup[] nameGroups = new NameGroup[typeKeys.Count];

			int i = 0;
			foreach ( TypeKey typeKey in typeKeys )
			{
				NameGroup nameGroup = GetNameGroup( baseSigNames, typeKey, sig );

				nameGroups[i++] = nameGroup;
			}

			return nameGroups;
		}

		string GetNewMethodName( Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method )
		{
			ObfuscatedThing t = map.GetMethod( methodKey );

			// if it already has a name, return it
			if ( t.Status == ObfuscationStatus.Renamed ||
				t.Status == ObfuscationStatus.WillRename )
				return t.StatusText;

			// don't mess with methods we decided to skip
			if ( t.Status == ObfuscationStatus.Skipped )
				return null;

			// counts are grouping according to signature
			ParamSig sig = new ParamSig( method );

			NameGroup nameGroup = GetNameGroup( sigNames, sig );

			string newName = nameGroup.GetNext( );

			// got a new name for the method
			t.Status = ObfuscationStatus.WillRename;
			t.StatusText = newName;

			// make sure the name groups is updated
			nameGroup.Add( newName );

			return newName;
		}

		void RenameMethod( AssemblyInfo info, Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method )
		{
			string newName = GetNewMethodName( sigNames, methodKey, method );

			RenameMethod( info, methodKey, method, newName );
		}

		void RenameMethod( AssemblyInfo info, MethodKey methodKey, MethodDefinition method, string newName )
		{
			// find references, rename them, then rename the method itself
			foreach ( AssemblyInfo reference in info.ReferencedBy )
			{
				for ( int i = 0; i < reference.UnrenamedReferences.Count; )
				{
					MethodReference member = reference.UnrenamedReferences[i] as MethodReference;
					if ( member != null )
					{
						if ( methodKey.Matches( member ) )
						{
							member.Name = newName;
							reference.UnrenamedReferences.RemoveAt( i );

							// since we removed one, continue without the increment
							continue;
						}
					}

					i++;
				}
			}

			method.Name = newName;

			map.UpdateMethod( methodKey, ObfuscationStatus.Renamed, newName );
		}
	}
}
