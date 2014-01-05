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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ILSpy.BamlDecompiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.Security;
using Mono.Security.Cryptography;
using Ricciolo.StylesExplorer.MarkupReflection;

namespace Obfuscar
{
	[SuppressMessage ("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification = "Reviewed. Suppression is OK here.")]
	public class Obfuscator
	{
		public bool hideStrings {
			get { return project.Settings.HideStrings; }
		}

		public event Action<string> Log;

		private void LogOutput (string output)
		{
			if (Log != null) {
				Log (output);
			} else {
				Console.Write (output);
			}
		}

		private Project project;
		private ObfuscationMap map = new ObfuscationMap ();
		// Unique names for type and members
		private int uniqueTypeNameIndex;
		private int uniqueMemberNameIndex;

		/// <summary>
		/// Creates an obfuscator initialized from a project file.
		/// </summary>
		/// <param name="projfile">Path to project file.</param>
		[SuppressMessage ("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification = "Reviewed. Suppression is OK here.")]
		public Obfuscator (string projfile)
		{
			// open XmlTextReader over xml string stream
			XmlReaderSettings settings = GetReaderSettings ();

			try {
				using (XmlReader reader = XmlReader.Create (File.OpenRead (projfile), settings)) {
					LoadFromReader (reader, Path.GetDirectoryName (projfile));
				}
			} catch (IOException e) {
				throw new ApplicationException ("Unable to read specified project file:  " + projfile, e);
			}
		}

		/// <summary>
		/// Creates an obfuscator initialized from a project file.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public Obfuscator (XmlReader reader)
		{
			LoadFromReader (reader, null);
		}

		public void RunRules ()
		{
			// The SemanticAttributes of MethodDefinitions have to be loaded before any fields,properties or events are removed
			this.LoadMethodSemantics ();
			
			LogOutput ("Renaming:  fields...");
			this.RenameFields ();
			
			LogOutput ("parameters...");
			this.RenameParams ();
			
			LogOutput ("properties...");
			this.RenameProperties ();
			
			LogOutput ("events...");
			this.RenameEvents ();
			
			LogOutput ("methods...");
			this.RenameMethods ();
			
			LogOutput ("types...");
			this.RenameTypes ();
			
			if (this.hideStrings) {
				LogOutput ("hiding strings...\n");
				this.HideStrings ();
			}
			
			LogOutput ("Done.\n");
			
			LogOutput ("Saving assemblies...");
			this.SaveAssemblies ();
			LogOutput ("Done.\n");
			
			LogOutput ("Writing log file...");
			this.SaveMapping ();
			LogOutput ("Done.\n");
		}

		public static Obfuscator CreateFromXml (string xml)
		{
			// open XmlTextReader over xml string stream
			XmlReaderSettings settings = GetReaderSettings ();

			using (XmlReader reader = XmlReader.Create (new StringReader (xml), settings)) {
				return new Obfuscator (reader);
			}
		}

		private static XmlReaderSettings GetReaderSettings ()
		{
			var settings = new XmlReaderSettings {
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
				XmlResolver = null,
				DtdProcessing = DtdProcessing.Parse
			};
			return settings;
		}

		internal Project Project {
			get { return project; }
		}

		private void LoadFromReader (XmlReader reader, string projectFileDirectory)
		{
			project = Project.FromXml (reader, projectFileDirectory);

			// make sure everything looks good
			project.CheckSettings ();
			if (project.Settings.UseUnicodeNames)
				NameMaker.UseUnicodeChars = true;
			if (project.Settings.UseKoreanNames)
				NameMaker.UseKoreanChars = true;

			LogOutput ("Loading assemblies...");
			LogOutput ("Extra framework folders: ");
			foreach (var lExtraPath in project.ExtraPaths?? new string[0])
				LogOutput (lExtraPath + ", ");

			project.LoadAssemblies ();
		}

		/// <summary>
		/// Saves changes made to assemblies to the output path.
		/// </summary>
		public void SaveAssemblies ()
		{
			string outPath = project.Settings.OutPath;

			//copy excluded assemblies
			foreach (AssemblyInfo copyInfo in project.CopyAssemblyList) {
				string outName = Path.Combine (outPath, Path.GetFileName (copyInfo.Filename));
				copyInfo.Definition.Write (outName);
			}

			// save the modified assemblies
			foreach (AssemblyInfo info in project) {
				string outName = Path.Combine (outPath, Path.GetFileName (info.Filename));
				if (project.Settings.RegenerateDebugInfo)
					info.Definition.Write (outName, new WriterParameters { SymbolWriterProvider = new Mono.Cecil.Pdb.PdbWriterProvider () });
				else
					info.Definition.Write (outName);

				if (info.Definition.Name.HasPublicKey) {
					if (project.KeyContainerName != null) {
						MsNetSigner.SignAssemblyFromKeyContainer (outName, project.KeyContainerName);
					} else {
						var sn = new StrongName (project.KeyValue);
						sn.Sign (outName);
					}
				}

				// TODO: review whether this is still needed 
				// SignAssembly (info, outName);
			}

			TypeNameCache.nameCache.Clear ();
		}

		private void SignAssembly (AssemblyInfo info, string outName)
		{
			// Re-sign assembly
			if (!info.Definition.Name.HasPublicKey)
				return;

			if (project.Settings.KeyFile == null)
				return;

			string keyfilepath = project.Settings.KeyFile;

			if (string.CompareOrdinal (keyfilepath, "auto") == 0) {
				keyfilepath = null;
				foreach (CustomAttribute ca in info.Definition.CustomAttributes) {
					if (ca.Constructor.DeclaringType.FullName ==
					    typeof(System.Reflection.AssemblyKeyFileAttribute).FullName) {
						keyfilepath = (string)ca.ConstructorArguments [0].Value;
					}
				}

				if (keyfilepath == null) {
					throw new ApplicationException (string.Format ("KeyFile='auto', but assembly '{0}' contains no AssemblyKeyFileAttribute", info.Filename));
				}
			}

			if (!Path.IsPathRooted (keyfilepath)) {
				foreach (string checkpath in new[] { ".", Path.GetDirectoryName(info.Filename) }) {
					string newpath = Path.Combine (checkpath, keyfilepath);
					if (File.Exists (newpath)) {
						keyfilepath = newpath;
						break;
					}
				}
			}

			try {
				var sn = new StrongName (CryptoConvert.FromCapiKeyBlob (File.ReadAllBytes (keyfilepath)));
				sn.Sign (outName);
			} catch (Exception ex) {
				throw new ApplicationException (
					string.Format ("Failed to sign '{1}' with key file \"{0}\"", keyfilepath, info.Filename), ex);
			}
		}

		/// <summary>
		/// Saves the name mapping to the output path.
		/// </summary>
		public void SaveMapping ()
		{
			string filename = project.Settings.XmlMapping ?
				"Mapping.xml" : "Mapping.txt";

			string logPath = Path.Combine (project.Settings.OutPath, filename);
			if (!String.IsNullOrEmpty (project.Settings.LogFilePath))
				logPath = project.Settings.LogFilePath;

			string lPath = Path.GetDirectoryName (logPath);
			if (!String.IsNullOrEmpty (lPath) && !Directory.Exists (lPath))
				Directory.CreateDirectory (lPath);

			using (TextWriter file = File.CreateText (logPath))
				SaveMapping (file);
		}

		/// <summary>
		/// Saves the name mapping to a text writer.
		/// </summary>
		public void SaveMapping (TextWriter writer)
		{
			IMapWriter mapWriter = project.Settings.XmlMapping ?
				new XmlMapWriter (writer) : (IMapWriter)new TextMapWriter (writer);

			mapWriter.WriteMap (map);
		}

		/// <summary>
		/// Returns the obfuscation map for the project.
		/// </summary>
		internal ObfuscationMap Mapping {
			get { return map; }
		}

		/// <summary>
		/// Calls the SemanticsAttributes-getter for all methods
		/// </summary>
		public void LoadMethodSemantics ()
		{
			foreach (AssemblyInfo info in project) {
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {   
					foreach (MethodDefinition method in type.Methods) {
						method.SemanticsAttributes.ToString ();
					}
				}
			}
		}

		/// <summary>
		/// Renames fields in the project.
		/// </summary>
		public void RenameFields ()
		{
			var nameGroups = new Dictionary<string, NameGroup> ();
			foreach (var info in project) {
				// loop through the types
				foreach (var type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					var typeKey = new TypeKey (type);

					if (!ShouldRename (type, true)) {
						continue;
					}

					nameGroups.Clear ();

					// rename field, grouping according to signature
					foreach (FieldDefinition field in type.Fields) {
						string sig = field.FieldType.FullName;
						var fieldKey = new FieldKey (typeKey, sig, field.Name, field);
						NameGroup nameGroup = GetNameGroup (nameGroups, sig);
						if (field.IsRuntimeSpecialName && field.Name == "value__") {
							map.UpdateField (fieldKey, ObfuscationStatus.Skipped, "filtered");
							nameGroup.Add (fieldKey.Name);
							continue;
						} 
                        
						if (project.Settings.KeepPublicApi && field.DeclaringType.IsPublic && field.IsPublic) {
							if (type.ObfuscationMarked () != true) {
								map.UpdateField (fieldKey, ObfuscationStatus.Skipped, "public field");
								nameGroup.Add (fieldKey.Name);
								continue;
							}
						} 

						// skip filtered fields
						if (info.ShouldSkip (fieldKey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi)) {
							if (type.ObfuscationMarked () != true) {
								map.UpdateField (fieldKey, ObfuscationStatus.Skipped, "filtered");
								nameGroup.Add (fieldKey.Name);
								continue;
							}
						} 

						string newName;
						if (project.Settings.ReuseNames) {
							newName = nameGroup.GetNext ();
						} else {
							newName = NameMaker.UniqueName (uniqueMemberNameIndex++);
						}

						RenameField (info, fieldKey, field, newName);
						nameGroup.Add (newName);
					}
				}
			}
		}

		private void RenameField (AssemblyInfo info, FieldKey fieldKey, FieldDefinition field, string newName)
		{
			// find references, rename them, then rename the field itself
			foreach (AssemblyInfo reference in info.ReferencedBy) {
				for (int i = 0; i < reference.UnrenamedReferences.Count;) {
					FieldReference member = reference.UnrenamedReferences [i] as FieldReference;
					if (member != null) {
						if (fieldKey.Matches (member)) {
							member.Name = newName;
							reference.UnrenamedReferences.RemoveAt (i);

							// since we removed one, continue without the increment
							continue;
						}
					}

					i++;
				}
			}

			field.Name = newName;
			map.UpdateField (fieldKey, ObfuscationStatus.Renamed, newName);
		}

		/// <summary>
		/// Renames constructor, method, and generic parameters.
		/// </summary>
		public void RenameParams ()
		{
			foreach (AssemblyInfo info in project) {
				// loop through the types
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					if (ShouldRename (type, true)) {
						// rename the method parameters
						foreach (MethodDefinition method in type.Methods)
							RenameParams (method, info);

						// rename the class parameters
						if (info.ShouldSkip (new TypeKey (type), Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi) && type.ObfuscationMarked () != true)
							continue;
                            
						int index = 0;
						foreach (GenericParameter param in type.GenericParameters)
							param.Name = NameMaker.UniqueName (index++);						
					}
				}
			}
		}

		private void RenameParams (MethodDefinition method, AssemblyInfo info)
		{
			MethodKey methodkey = new MethodKey (method);
			if (!info.ShouldSkip (methodkey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi)) {
				foreach (ParameterDefinition param in method.Parameters) {
					if (param.CustomAttributes.Count == 0) {
						param.Name = null;
					}
				}

				int index = 0;
				foreach (GenericParameter param in method.GenericParameters) {
					if (param.CustomAttributes.Count == 0) {
						param.Name = NameMaker.UniqueName (index++);
					}
				}
			}
		}

		private bool ShouldRename (TypeDefinition type, bool member = false)
		{
			var marked = type.ObfuscationMarked ();
			if (marked != null) {
				return marked.Value;
			}

			return !project.Settings.MarkedOnly;
		}

		/// <summary>
		/// Renames types and resources in the project.
		/// </summary>
		public void RenameTypes ()
		{
			var typerenamemap = new Dictionary<string, string> (); // For patching the parameters of typeof(xx) attribute constructors
			foreach (AssemblyInfo info in project) {
				AssemblyDefinition library = info.Definition;

				// make a list of the resources that can be renamed
				List<Resource> resources = new List<Resource> (library.MainModule.Resources.Count);
				foreach (Resource res in library.MainModule.Resources) {
					resources.Add (res);
				}

				var xamlFiles = GetXamlDocuments (library);

				// Save the original names of all types because parent (declaring) types of nested types may be already renamed.
				// The names are used for the mappings file.
				Dictionary<TypeDefinition, TypeKey> unrenamedTypeKeys = new Dictionary<TypeDefinition, TypeKey> ();
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					unrenamedTypeKeys.Add (type, new TypeKey (type));
				}

				// loop through the types
				int typeIndex = 0;
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					TypeKey oldTypeKey = new TypeKey (type);
					TypeKey unrenamedTypeKey = unrenamedTypeKeys [type];
					string fullName = type.FullName;

					if (!ShouldRename (type)) {
						map.UpdateType (oldTypeKey, ObfuscationStatus.Skipped, "marked");

						// go through the list of resources, remove ones that would be renamed
						for (int i = 0; i < resources.Count;) {
							Resource res = resources [i];
							string resName = res.Name;

							if (Path.GetFileNameWithoutExtension (resName) == fullName) {
								resources.RemoveAt (i);
								map.AddResource (resName, ObfuscationStatus.Skipped, "marked");
							} else {
								i++;
							}
						}

						continue;
					}

					var namesInXaml = NamesInXaml (xamlFiles);
					if (info.ShouldSkip (unrenamedTypeKey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi) || project.Settings.KeepPublicApi && type.IsPublic || namesInXaml.Contains (type.FullName)) {
						if (type.ObfuscationMarked () != true) {
							map.UpdateType (oldTypeKey, ObfuscationStatus.Skipped, "filtered");

							// go through the list of resources, remove ones that would be renamed
							for (int i = 0; i < resources.Count;) {
								Resource res = resources [i];
								string resName = res.Name;
								if (Path.GetFileNameWithoutExtension (resName) == fullName) {
									resources.RemoveAt (i);
									map.AddResource (resName, ObfuscationStatus.Skipped, "filtered");
								} else {
									i++;
								}
							}

							continue;
						}
					}

					string name;
					string ns;
					if (project.Settings.ReuseNames) {
						name = NameMaker.UniqueTypeName (typeIndex);
						ns = NameMaker.UniqueNamespace (typeIndex);
					} else {
						name = NameMaker.UniqueName (uniqueTypeNameIndex);
						ns = NameMaker.UniqueNamespace (uniqueTypeNameIndex);
						uniqueTypeNameIndex++;
					}

					if (type.GenericParameters.Count > 0) {
						name += '`' + type.GenericParameters.Count.ToString ();
					}

					if (type.DeclaringType != null) {
						// Nested types do not have namespaces
						ns = "";
					}

					TypeKey newTypeKey = new TypeKey (info.Name, ns, name);
					typeIndex++;

					// go through the list of renamed types and try to rename resources
					for (int i = 0; i < resources.Count;) {
						Resource res = resources [i];
						string resName = res.Name;

						if (Path.GetFileNameWithoutExtension (resName) == fullName) {
							// If one of the type's methods return a ResourceManager and contains a string with the full type name,
							// we replace the type string with the obfuscated one.
							// This is for the Visual Studio generated resource designer code.
							foreach (MethodDefinition method in type.Methods) {
								if (method.ReturnType.FullName != "System.Resources.ResourceManager") {
									continue;
								}

								for (int j = 0; j < method.Body.Instructions.Count; j++) {
									Instruction instruction = method.Body.Instructions [j];
									if (instruction.OpCode == OpCodes.Ldstr &&
									    (string)instruction.Operand == fullName) {
										instruction.Operand = newTypeKey.Fullname;
									}
								}
							}

							string suffix = resName.Substring (fullName.Length);
							string newName = newTypeKey.Fullname + suffix;
							res.Name = newName;
							resources.RemoveAt (i);
							map.AddResource (resName, ObfuscationStatus.Renamed, newName);
						} else {
							i++;
						}
					}

					RenameType (info, type, oldTypeKey, newTypeKey, unrenamedTypeKey);
					typerenamemap.Add (unrenamedTypeKey.Fullname.Replace ('/', '+'), type.FullName.Replace ('/', '+'));
				}

				foreach (Resource res in resources) {
					map.AddResource (res.Name, ObfuscationStatus.Skipped, "no clear new name");
				}
			}
		}

		private HashSet<string> NamesInXaml (List<XDocument> xamlFiles)
		{
			var result = new HashSet<string> ();
			if (xamlFiles.Count == 0)
				return result;

			foreach (var doc in xamlFiles)
				foreach (XElement child in doc.Descendants()) {
					var classAttribute = child.Attributes ().FirstOrDefault (node => node.Name.LocalName == "Class");
					if (classAttribute == null)
						continue;

					result.Add (classAttribute.Value);
				}

			return result;
		}

		private List<XDocument> GetXamlDocuments (AssemblyDefinition library)
		{
			var result = new List<XDocument> ();
			foreach (Resource res in library.MainModule.Resources) {
				var embed = res as EmbeddedResource;
				if (embed == null)
					continue;

				Stream s = embed.GetResourceStream ();
				s.Position = 0;
				ResourceReader reader;
				try {
					reader = new ResourceReader (s);
				} catch (ArgumentException) {
					continue;
				}

				foreach (DictionaryEntry entry in reader.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString())) {
					if (entry.Key.ToString ().EndsWith (".baml", StringComparison.OrdinalIgnoreCase)) {
						Stream stream;
						if (entry.Value is Stream)
							stream = (Stream)entry.Value;
						else if (entry.Value is byte[])
							stream = new MemoryStream ((byte[])entry.Value);
						else
							continue;

						using (var bamlReader = new XmlBamlReader (stream, new CecilTypeResolver (project.InheritMap.Cache, library)))
							result.Add (XDocument.Load (bamlReader));
					}
				}
			}

			return result;
		}

		private void RenameType (AssemblyInfo info, TypeDefinition type, TypeKey oldTypeKey, TypeKey newTypeKey, TypeKey unrenamedTypeKey)
		{
			// find references, rename them, then rename the type itself
			foreach (AssemblyInfo reference in info.ReferencedBy) {
				for (int i = 0; i < reference.UnrenamedTypeReferences.Count;) {
					TypeReference refType = reference.UnrenamedTypeReferences [i];

					// check whether the referencing module references this type...if so,
					// rename the reference
					if (oldTypeKey.Matches (refType)) {
						refType.Namespace = newTypeKey.Namespace;
						refType.Name = newTypeKey.Name;

						reference.UnrenamedTypeReferences.RemoveAt (i);

						// since we removed one, continue without the increment
						continue;
					}

					i++;
				}
			}

			type.Namespace = newTypeKey.Namespace;
			type.Name = newTypeKey.Name;
			map.UpdateType (unrenamedTypeKey, ObfuscationStatus.Renamed, string.Format ("[{0}]{1}", newTypeKey.Scope, type));
		}

		private string GetObfuscatedTypeName (string typeString, IDictionary<string, string> typeRenameMap)
		{
			string[] typeparts = typeString.Split (new[] { ',' });
			if (typeparts.Length > 0) { // be paranoid
				string typename = typeparts [0].Trim ();
				string obfuscatedtypename;
				if (typeRenameMap.TryGetValue (typename, out obfuscatedtypename)) {
					string newtypename = obfuscatedtypename;
					for (int n = 1; n < typeparts.Length; n++) {
						newtypename += ',' + typeparts [n];
					}

					return newtypename;
				}
			}

			return typeString;
		}

		private Dictionary<ParamSig, NameGroup> GetSigNames (Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames, TypeKey typeKey)
		{
			Dictionary<ParamSig, NameGroup> sigNames;
			if (!baseSigNames.TryGetValue (typeKey, out sigNames)) {
				sigNames = new Dictionary<ParamSig, NameGroup> ();
				baseSigNames [typeKey] = sigNames;
			}

			return sigNames;
		}

		private NameGroup GetNameGroup (Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames, TypeKey typeKey, ParamSig sig)
		{
			return GetNameGroup (GetSigNames (baseSigNames, typeKey), sig);
		}

		private NameGroup GetNameGroup<KeyType> (Dictionary<KeyType, NameGroup> sigNames, KeyType sig)
		{
			NameGroup nameGroup;
			if (!sigNames.TryGetValue (sig, out nameGroup)) {
				nameGroup = new NameGroup ();
				sigNames [sig] = nameGroup;
			}

			return nameGroup;
		}

		public void RenameProperties ()
		{
			// do nothing if it was requested not to rename
			if (!project.Settings.RenameProperties) {
				return;
			}

			foreach (AssemblyInfo info in project) {
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					TypeKey typeKey = new TypeKey (type);

					if (!ShouldRename (type, true)) {
						continue;
					}

					int index = 0;
					List<PropertyDefinition> propsToDrop = new List<PropertyDefinition> ();
					foreach (PropertyDefinition prop in type.Properties) {
						PropertyKey propKey = new PropertyKey (typeKey, prop);
						ObfuscatedThing m = map.GetProperty (propKey);

						// skip runtime special properties
						if (prop.IsRuntimeSpecialName) {
							m.Update (ObfuscationStatus.Skipped, "runtime special");
							continue;
						}

						var hasPublicProperty = (prop.GetMethod != null && (prop.GetMethod.IsPublic || prop.GetMethod.IsFamily)) || (prop.SetMethod != null && (prop.SetMethod.IsPublic || prop.SetMethod.IsFamily));
						if (project.Settings.KeepPublicApi && prop.DeclaringType.IsPublic && hasPublicProperty) {
							if (type.ObfuscationMarked () != true) {
								m.Update (ObfuscationStatus.Skipped, "public property");
								continue;
							}
						}

						// skip filtered props
						if (info.ShouldSkip (propKey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi)) {
							if (type.ObfuscationMarked () != true) {
								m.Update (ObfuscationStatus.Skipped, "filtered");

								// make sure get/set get skipped too
								if (prop.GetMethod != null) {
									info.ForceSkip (new MethodKey (prop.GetMethod));
								}

								if (prop.SetMethod != null) {
									info.ForceSkip (new MethodKey (prop.SetMethod));
								}

								continue;
							}							
						}
							
						if (type.BaseType != null && type.BaseType.Name.EndsWith ("Attribute") && prop.SetMethod != null && (prop.SetMethod.Attributes & MethodAttributes.Public) != 0) {
							// do not rename properties of custom attribute types which have a public setter method
							m.Update (ObfuscationStatus.Skipped, "public setter of a custom attribute");
							// no problem when the getter or setter methods are renamed by RenameMethods()
						} else if (prop.CustomAttributes.Count > 0) {
							// If a property has custom attributes we don't remove the property but rename it instead.
							string newName;
							if (project.Settings.ReuseNames)
								newName = NameMaker.UniqueName (index++);
							else
								newName = NameMaker.UniqueName (uniqueMemberNameIndex++);
							RenameProperty (info, propKey, prop, newName);
						} else {
							// add to to collection for removal
							propsToDrop.Add (prop);
						}
					}

					foreach (PropertyDefinition prop in propsToDrop) {
						PropertyKey propKey = new PropertyKey (typeKey, prop);
						ObfuscatedThing m = map.GetProperty (propKey);
						m.Update (ObfuscationStatus.Renamed, "dropped");
						type.Properties.Remove (prop);
					}
				}
			}
		}

		private void RenameProperty (AssemblyInfo info, PropertyKey propertyKey, PropertyDefinition property, string newName)
		{
			// find references, rename them, then rename the property itself
			foreach (AssemblyInfo reference in info.ReferencedBy) {
				for (int i = 0; i < reference.UnrenamedReferences.Count;) {
					PropertyReference member = reference.UnrenamedReferences [i] as PropertyReference;
					if (member != null) {
						if (propertyKey.Matches (member)) {
							member.Name = newName;
							reference.UnrenamedReferences.RemoveAt (i);

							// since we removed one, continue without the increment
							continue;
						}
					}

					i++;
				}
			}

			property.Name = newName;
			map.UpdateProperty (propertyKey, ObfuscationStatus.Renamed, newName);
		}

		public void RenameEvents ()
		{
			// do nothing if it was requested not to rename
			if (!project.Settings.RenameEvents) {
				return;
			}

			foreach (AssemblyInfo info in project) {
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					TypeKey typeKey = new TypeKey (type);
					if (!ShouldRename (type, true)) {
						continue;
					}

					List<EventDefinition> evtsToDrop = new List<EventDefinition> ();
					foreach (EventDefinition evt in type.Events) {
						EventKey evtKey = new EventKey (typeKey, evt);
						ObfuscatedThing m = map.GetEvent (evtKey);

						// skip runtime special events
						if (evt.IsRuntimeSpecialName) {
							m.Update (ObfuscationStatus.Skipped, "runtime special");
							continue;
						}

						var hasPublicEvent = evt.AddMethod.IsPublic || evt.AddMethod.IsFamily || evt.RemoveMethod.IsPublic || evt.RemoveMethod.IsFamily;
						if (project.Settings.KeepPublicApi && evt.DeclaringType.IsPublic && hasPublicEvent) {
							if (type.ObfuscationMarked () != true) {
								m.Update (ObfuscationStatus.Skipped, "public event");
								continue;
							}
						}

						// skip filtered events
						if (info.ShouldSkip (evtKey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi)) {
							if (type.ObfuscationMarked () != true) {
								m.Update (ObfuscationStatus.Skipped, "filtered");

								// make sure add/remove get skipped too
								info.ForceSkip (new MethodKey (evt.AddMethod));
								info.ForceSkip (new MethodKey (evt.RemoveMethod));

								continue;
							}
						}

						// add to to collection for removal
						evtsToDrop.Add (evt);
					}

					foreach (EventDefinition evt in evtsToDrop) {
						EventKey evtKey = new EventKey (typeKey, evt);
						ObfuscatedThing m = map.GetEvent (evtKey);

						m.Update (ObfuscationStatus.Renamed, "dropped");
						type.Events.Remove (evt);
					}
				}
			}
		}

		public void RenameMethods ()
		{
			var baseSigNames = new Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> ();
			foreach (AssemblyInfo info in project) {
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					TypeKey typeKey = new TypeKey (type);

					Dictionary<ParamSig, NameGroup> sigNames = GetSigNames (baseSigNames, typeKey);

					// first pass.  mark grouped virtual methods to be renamed, and mark some things
					// to be skipped as neccessary
					foreach (MethodDefinition method in type.Methods) {
						string skiprename = null;
						if (!ShouldRename (type, true)) {
							skiprename = "ObfuscateAttribute found on type.";
						}

						MethodKey methodKey = new MethodKey (typeKey, method);
						ObfuscatedThing m = map.GetMethod (methodKey);

						// skip runtime methods
						if (method.IsRuntime) {
							skiprename = "runtime method";
						}

						if (project.Settings.KeepPublicApi && method.DeclaringType.IsPublic && (method.IsPublic || method.IsFamily)) {
							if (type.ObfuscationMarked () != true)
								skiprename = "public method";
						}

						// skip filtered methods
						if (info.ShouldSkip (methodKey, Project.InheritMap, Project.Settings.KeepPublicApi, Project.Settings.HidePrivateApi)) {
							if (type.ObfuscationMarked () != true)
								skiprename = "filtered";
						}

						// update status for skipped non-virtual methods immediately...status for
						// skipped virtual methods gets updated in RenameVirtualMethod
						if (!method.IsVirtual) {
							if (skiprename != null) {
								m.Update (ObfuscationStatus.Skipped, skiprename);
							}

							continue;
						}

						if (method.IsSpecialName) {
							switch (method.SemanticsAttributes) {
							case MethodSemanticsAttributes.Getter:
							case MethodSemanticsAttributes.Setter:
								if (!project.Settings.RenameProperties) {
									skiprename = "skipping properties";
								}

								break;
							case MethodSemanticsAttributes.AddOn:
							case MethodSemanticsAttributes.RemoveOn:
								if (!project.Settings.RenameEvents) {
									skiprename = "skipping events";
								}

								break;
							default:
								skiprename = "virtual and special name";
								break;
							}
						}

						// if we need to skip the method or we don't yet have a name planned for a method, rename it
						if ((skiprename != null && m.Status != ObfuscationStatus.Skipped) ||
						    m.Status == ObfuscationStatus.Unknown) {
							RenameVirtualMethod (info, baseSigNames, sigNames, methodKey, method, skiprename);
						}
					}

					// update name groups, so new names don't step on inherited ones
					foreach (TypeKey baseType in project.InheritMap.GetBaseTypes(typeKey)) {
						Dictionary<ParamSig, NameGroup> baseNames = GetSigNames (baseSigNames, baseType);
						foreach (KeyValuePair<ParamSig, NameGroup> pair in baseNames) {
							NameGroup nameGroup = GetNameGroup (sigNames, pair.Key);
							nameGroup.AddAll (pair.Value);
						}
					}
				}
                
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>") {
						continue;
					}

					TypeKey typeKey = new TypeKey (type);
					Dictionary<ParamSig, NameGroup> sigNames = GetSigNames (baseSigNames, typeKey);

					// second pass...marked virtuals and anything not skipped get renamed
					foreach (MethodDefinition method in type.Methods) {
						MethodKey methodKey = new MethodKey (typeKey, method);
						ObfuscatedThing m = map.GetMethod (methodKey);

						// if we already decided to skip it, leave it alone
						if (m.Status == ObfuscationStatus.Skipped) {
							continue;
						}

						if (method.IsSpecialName) {
							switch (method.SemanticsAttributes) {
							case MethodSemanticsAttributes.Getter:
							case MethodSemanticsAttributes.Setter:
								if (project.Settings.RenameProperties) {
									RenameMethod (info, sigNames, methodKey, method);
									method.SemanticsAttributes = 0;
								} else
									m.Update (ObfuscationStatus.Skipped, "skipping properties");
								break;
							case MethodSemanticsAttributes.AddOn:
							case MethodSemanticsAttributes.RemoveOn:
								if (project.Settings.RenameEvents) {
									RenameMethod (info, sigNames, methodKey, method);
									method.SemanticsAttributes = 0;
								} else
									m.Update (ObfuscationStatus.Skipped, "skipping events");
								break;
							default:
								m.Update (ObfuscationStatus.Skipped, "special name");
								break;
							}
						} else {
							RenameMethod (info, sigNames, methodKey, method);
						}
					}
				}
			}
		}

		private void RenameVirtualMethod (AssemblyInfo info, Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
		                                  Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method, string skipRename)
		{
			// if method is in a group, look for group key
			MethodGroup group = project.InheritMap.GetMethodGroup (methodKey);
			if (group == null) {
				if (skipRename != null) {
					map.UpdateMethod (methodKey, ObfuscationStatus.Skipped, skipRename);
				}

				return;
			}

			string groupName = @group.Name;
			if (groupName == null) {
				// group is not yet named

				// counts are grouping according to signature
				ParamSig sig = new ParamSig (method);

				// get name groups for classes in the group
				NameGroup[] nameGroups = GetNameGroups (baseSigNames, @group.Methods, sig);

				if (@group.External)
					skipRename = "external base class or interface";
				if (skipRename != null) {
					// for an external group, we can't rename.  just use the method 
					// name as group name
					groupName = method.Name;
				} else {
					// for an internal group, get next unused name
					groupName = NameGroup.GetNext (nameGroups);
				}

				@group.Name = groupName;

				// set up methods to be renamed
				foreach (MethodKey m in @group.Methods)
					if (skipRename == null)
						map.UpdateMethod (m, ObfuscationStatus.WillRename, groupName);
					else
						map.UpdateMethod (m, ObfuscationStatus.Skipped, skipRename);

				// make sure the classes' name groups are updated
				for (int i = 0; i < nameGroups.Length; i++)
					nameGroups [i].Add (groupName);
			} else if (skipRename != null) {
				// group is named, so we need to un-name it

				Debug.Assert (!@group.External,
					"Group's external flag should have been handled when the group was created, " +
					"and all methods in the group should already be marked skipped.");

				// counts are grouping according to signature
				ParamSig sig = new ParamSig (method);

				// get name groups for classes in the group
				NameGroup[] nameGroups = GetNameGroups (baseSigNames, @group.Methods, sig);

				// make sure to remove the old group name from the classes' name groups
				for (int i = 0; i < nameGroups.Length; i++)
					nameGroups [i].Remove (groupName);

				// since this method has to be skipped, we need to use the method 
				// name as new group name
				groupName = method.Name;
				@group.Name = groupName;

				// set up methods to be renamed
				foreach (MethodKey m in @group.Methods)
					map.UpdateMethod (m, ObfuscationStatus.Skipped, skipRename);

				// make sure the classes' name groups are updated
				for (int i = 0; i < nameGroups.Length; i++)
					nameGroups [i].Add (groupName);
			} else {
				ObfuscatedThing m = map.GetMethod (methodKey);
				Debug.Assert (m.Status == ObfuscationStatus.Skipped ||
				((m.Status == ObfuscationStatus.WillRename || m.Status == ObfuscationStatus.Renamed) &&
				m.StatusText == groupName),
					"If the method isn't skipped, and the group already has a name...method should have one too.");
			}
		}

		NameGroup[] GetNameGroups (Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
		                           IEnumerable<MethodKey> methodKeys, ParamSig sig)
		{
			// build unique set of classes in group
			HashSet<TypeKey> typeKeys = new HashSet<TypeKey> ();
			foreach (MethodKey methodKey in methodKeys)
				typeKeys.Add (methodKey.TypeKey);

			// build list of namegroups
			NameGroup[] nameGroups = new NameGroup[typeKeys.Count];

			int i = 0;
			foreach (TypeKey typeKey in typeKeys) {
				NameGroup nameGroup = GetNameGroup (baseSigNames, typeKey, sig);

				nameGroups [i++] = nameGroup;
			}

			return nameGroups;
		}

		string GetNewMethodName (Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method)
		{
			ObfuscatedThing t = map.GetMethod (methodKey);

			// if it already has a name, return it
			if (t.Status == ObfuscationStatus.Renamed ||
			    t.Status == ObfuscationStatus.WillRename)
				return t.StatusText;

			// don't mess with methods we decided to skip
			if (t.Status == ObfuscationStatus.Skipped)
				return null;

			// got a new name for the method
			t.Status = ObfuscationStatus.WillRename;
			t.StatusText = GetNewName (sigNames, method);
			return t.StatusText;
		}

		private string GetNewName (Dictionary<ParamSig, NameGroup> sigNames, MethodDefinition method)
		{
			// counts are grouping according to signature
			ParamSig sig = new ParamSig (method);

			NameGroup nameGroup = GetNameGroup (sigNames, sig);

			string newName = nameGroup.GetNext ();

			// make sure the name groups is updated
			nameGroup.Add (newName);
			return newName;
		}

		void RenameMethod (AssemblyInfo info, Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method)
		{
			string newName = GetNewMethodName (sigNames, methodKey, method);

			RenameMethod (info, methodKey, method, newName);
		}

		void RenameMethod (AssemblyInfo info, MethodKey methodKey, MethodDefinition method, string newName)
		{
			// find references, rename them, then rename the method itself
			var references = new List<AssemblyInfo> ();
			references.AddRange (info.ReferencedBy);
			if (!references.Contains (info)) {
				references.Add (info);
			}

			foreach (AssemblyInfo reference in references) {
				for (int i = 0; i < reference.UnrenamedReferences.Count;) {
					MethodReference member = reference.UnrenamedReferences [i] as MethodReference;
					if (member != null) {
						if (methodKey.Matches (member)) {
							var generic = member as GenericInstanceMethod;
							if (generic == null) {
								member.Name = newName;
							} else {
								generic.ElementMethod.Name = newName;
							}

							reference.UnrenamedReferences.RemoveAt (i);

							// since we removed one, continue without the increment
							continue;
						}
					}

					i++;
				}
			}

			method.Name = newName;

			map.UpdateMethod (methodKey, ObfuscationStatus.Renamed, newName);
		}

		public void HideStrings ()
		{
			foreach (AssemblyInfo info in project) {
				AssemblyDefinition library = info.Definition;

				Dictionary<string, MethodDefinition> methodByString = new Dictionary<string, MethodDefinition> ();

				int nameIndex = 0;

				// We get the most used type references
				TypeReference systemObjectTypeReference = library.MainModule.Import (typeof(Object));
				TypeReference systemVoidTypeReference = library.MainModule.Import (typeof(void));
				TypeReference systemStringTypeReference = library.MainModule.Import (typeof(String));
				TypeReference systemValueTypeTypeReference = library.MainModule.Import (typeof(ValueType));
				TypeReference systemByteTypeReference = library.MainModule.Import (typeof(byte));
				TypeReference systemIntTypeReference = library.MainModule.Import (typeof(int));

				// New static class with a method for each unique string we substitute.
				TypeDefinition newtype = new TypeDefinition ("<PrivateImplementationDetails>{" + Guid.NewGuid ().ToString ().ToUpper () + "}", null, TypeAttributes.BeforeFieldInit | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, systemObjectTypeReference);

				// Array of bytes receiving the obfuscated strings in UTF8 format.
				List<byte> databytes = new List<byte> ();

				// Add struct for constant byte array data
				TypeDefinition structType = new TypeDefinition ("\0", "", TypeAttributes.ExplicitLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate, systemValueTypeTypeReference);
				structType.PackingSize = 1;
				newtype.NestedTypes.Add (structType);

				// Add field with constant string data
				FieldDefinition dataConstantField = new FieldDefinition ("\0", FieldAttributes.HasFieldRVA | FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly, structType);
				newtype.Fields.Add (dataConstantField);

				// Add data field where constructor copies the data to
				FieldDefinition dataField = new FieldDefinition ("\0\0", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly, new ArrayType (systemByteTypeReference));
				newtype.Fields.Add (dataField);

				// Add string array of deobfuscated strings
				FieldDefinition stringArrayField = new FieldDefinition ("\0\0\0", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly, new ArrayType (systemStringTypeReference));
				newtype.Fields.Add (stringArrayField);

				// Add method to extract a string from the byte array. It is called by the indiviual string getter methods we add later to the class.
				MethodDefinition stringGetterMethodDefinition = new MethodDefinition ("\0", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, systemStringTypeReference);
				stringGetterMethodDefinition.Parameters.Add (new ParameterDefinition (systemIntTypeReference));
				stringGetterMethodDefinition.Parameters.Add (new ParameterDefinition (systemIntTypeReference));
				stringGetterMethodDefinition.Parameters.Add (new ParameterDefinition (systemIntTypeReference));
				stringGetterMethodDefinition.Body.Variables.Add (new VariableDefinition (systemStringTypeReference));
				ILProcessor worker3 = stringGetterMethodDefinition.Body.GetILProcessor ();

				worker3.Emit (OpCodes.Call, library.MainModule.Import (typeof(System.Text.Encoding).GetProperty ("UTF8").GetGetMethod ()));
				worker3.Emit (OpCodes.Ldsfld, dataField);
				worker3.Emit (OpCodes.Ldarg_1);
				worker3.Emit (OpCodes.Ldarg_2);
				worker3.Emit (OpCodes.Callvirt, library.MainModule.Import (typeof(System.Text.Encoding).GetMethod ("GetString", new[] {
					typeof(byte[]),
					typeof(int),
					typeof(int)
				})));
				worker3.Emit (OpCodes.Stloc_0);

				worker3.Emit (OpCodes.Ldsfld, stringArrayField);
				worker3.Emit (OpCodes.Ldarg_0);
				worker3.Emit (OpCodes.Ldloc_0);
				worker3.Emit (OpCodes.Stelem_Ref);

				worker3.Emit (OpCodes.Ldloc_0);
				worker3.Emit (OpCodes.Ret);
				newtype.Methods.Add (stringGetterMethodDefinition);

				int stringIndex = 0;

				// Look for all string load operations and replace them with calls to indiviual methods in our new class
				foreach (TypeDefinition type in info.GetAllTypeDefinitions()) {
					if (type.FullName == "<Module>")
						continue;

					// FIXME: Figure out why this exists if it is never used.
					// TypeKey typeKey = new TypeKey(type);
					if (ShouldRename (type, true)) {
						foreach (MethodDefinition method in type.Methods) {
							if (!info.ShouldSkipStringHiding (new MethodKey (method), Project.InheritMap, Project.Settings.HidePrivateApi) && method.Body != null) {
								for (int i = 0; i < method.Body.Instructions.Count; i++) {
									Instruction instruction = method.Body.Instructions [i];
									if (instruction.OpCode == OpCodes.Ldstr) {
										string str = (string)instruction.Operand;
										MethodDefinition individualStringMethodDefinition = null;
										if (!methodByString.TryGetValue (str, out individualStringMethodDefinition)) {
											string methodName = NameMaker.UniqueName (nameIndex++);

											// Add the string to the data array
											byte[] stringBytes = Encoding.UTF8.GetBytes (str);
											int start = databytes.Count;
											databytes.AddRange (stringBytes);
											int count = databytes.Count - start;

											// Add a method for this string to our new class
											individualStringMethodDefinition = new MethodDefinition (methodName, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, systemStringTypeReference);
											individualStringMethodDefinition.Body = new MethodBody (individualStringMethodDefinition);
											ILProcessor worker4 = individualStringMethodDefinition.Body.GetILProcessor ();

											worker4.Emit (OpCodes.Ldsfld, stringArrayField);
											worker4.Emit (OpCodes.Ldc_I4, stringIndex);
											worker4.Emit (OpCodes.Ldelem_Ref);
											worker4.Emit (OpCodes.Dup);
											Instruction label20 = worker4.Create (OpCodes.Brtrue_S, stringGetterMethodDefinition.Body.Instructions [0]);
											worker4.Append (label20);
											worker4.Emit (OpCodes.Pop);
											worker4.Emit (OpCodes.Ldc_I4, stringIndex);
											worker4.Emit (OpCodes.Ldc_I4, start);
											worker4.Emit (OpCodes.Ldc_I4, count);
											worker4.Emit (OpCodes.Call, stringGetterMethodDefinition);


											label20.Operand = worker4.Create (OpCodes.Ret);
											worker4.Append ((Instruction)label20.Operand);

											newtype.Methods.Add (individualStringMethodDefinition);
											methodByString.Add (str, individualStringMethodDefinition);

											stringIndex++;
										}
										// Replace Ldstr with Call
										ILProcessor worker = method.Body.GetILProcessor ();
										Instruction newinstruction = worker.Create (OpCodes.Call, individualStringMethodDefinition);
										worker.Replace (instruction, newinstruction);
									}
								}
							}
						}
					}
				}

				// Now that we know the total size of the byte array, we can update the struct size and store it in the constant field
				structType.ClassSize = databytes.Count;
				for (int i = 0; i < databytes.Count; i++)
					databytes [i] = (byte)(databytes [i] ^ (byte)i ^ 0xAA);
				dataConstantField.InitialValue = databytes.ToArray ();

				// Add static constructor which initializes the dataField from the constant data field
				MethodDefinition ctorMethodDefinition = new MethodDefinition (".cctor", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, systemVoidTypeReference);
				newtype.Methods.Add (ctorMethodDefinition);
				ctorMethodDefinition.Body = new MethodBody (ctorMethodDefinition);
				ctorMethodDefinition.Body.Variables.Add (new VariableDefinition (systemIntTypeReference));

				ILProcessor worker2 = ctorMethodDefinition.Body.GetILProcessor ();
				worker2.Emit (OpCodes.Ldc_I4, stringIndex);
				worker2.Emit (OpCodes.Newarr, systemStringTypeReference);
				worker2.Emit (OpCodes.Stsfld, stringArrayField);


				worker2.Emit (OpCodes.Ldc_I4, databytes.Count);
				worker2.Emit (OpCodes.Newarr, systemByteTypeReference);
				worker2.Emit (OpCodes.Dup);
				worker2.Emit (OpCodes.Ldtoken, dataConstantField);
				worker2.Emit (OpCodes.Call, library.MainModule.Import (typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod ("InitializeArray")));
				worker2.Emit (OpCodes.Stsfld, dataField);

				worker2.Emit (OpCodes.Ldc_I4_0);
				worker2.Emit (OpCodes.Stloc_0);

				Instruction backlabel1 = worker2.Create (OpCodes.Br_S, ctorMethodDefinition.Body.Instructions [0]);
				worker2.Append (backlabel1);
				Instruction label2 = worker2.Create (OpCodes.Ldsfld, dataField);
				worker2.Append (label2);
				worker2.Emit (OpCodes.Ldloc_0);
				worker2.Emit (OpCodes.Ldsfld, dataField);
				worker2.Emit (OpCodes.Ldloc_0);
				worker2.Emit (OpCodes.Ldelem_U1);
				worker2.Emit (OpCodes.Ldloc_0);
				worker2.Emit (OpCodes.Xor);
				worker2.Emit (OpCodes.Ldc_I4, 0xAA);
				worker2.Emit (OpCodes.Xor);
				worker2.Emit (OpCodes.Conv_U1);
				worker2.Emit (OpCodes.Stelem_I1);
				worker2.Emit (OpCodes.Ldloc_0);
				worker2.Emit (OpCodes.Ldc_I4_1);
				worker2.Emit (OpCodes.Add);
				worker2.Emit (OpCodes.Stloc_0);
				backlabel1.Operand = worker2.Create (OpCodes.Ldloc_0);
				worker2.Append ((Instruction)backlabel1.Operand);
				worker2.Emit (OpCodes.Ldsfld, dataField);
				worker2.Emit (OpCodes.Ldlen);
				worker2.Emit (OpCodes.Conv_I4);
				worker2.Emit (OpCodes.Clt);
				worker2.Emit (OpCodes.Brtrue, label2);
				worker2.Emit (OpCodes.Ret);


				library.MainModule.Types.Add (newtype);
			}
		}

		public static class MsNetSigner
		{
			[System.Runtime.InteropServices.DllImport ("mscoree.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
			private static extern bool StrongNameSignatureGeneration (
				[/*In, */System.Runtime.InteropServices.MarshalAs (System.Runtime.InteropServices.UnmanagedType.LPWStr)]string wzFilePath,
				[/*In, */System.Runtime.InteropServices.MarshalAs (System.Runtime.InteropServices.UnmanagedType.LPWStr)]string wzKeyContainer,
				/*[In]*/byte[] pbKeyBlob,
				/*[In]*/uint cbKeyBlob,
				/*[In]*/IntPtr ppbSignatureBlob, // not supported, always pass 0.
				[System.Runtime.InteropServices.Out]out uint pcbSignatureBlob
			);

			public static void SignAssemblyFromKeyContainer (string assemblyname, string keyname)
			{
				uint dummy;
				if (!StrongNameSignatureGeneration (assemblyname, keyname, null, 0, IntPtr.Zero, out dummy))
					throw new Exception ("Unable to sign assembly using key from key container - " + keyname);
			}
		}
	}
}
