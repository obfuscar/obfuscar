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
		private readonly PredicateCollection<string> skipNamespaces = new PredicateCollection<string> ();
		private readonly PredicateCollection<TypeKey> skipTypes = new PredicateCollection<TypeKey> ();
		private readonly PredicateCollection<MethodKey> skipMethods = new PredicateCollection<MethodKey> ();
		private readonly PredicateCollection<FieldKey> skipFields = new PredicateCollection<FieldKey> ();
		private readonly PredicateCollection<PropertyKey> skipProperties = new PredicateCollection<PropertyKey> ();
		private readonly PredicateCollection<EventKey> skipEvents = new PredicateCollection<EventKey> ();
		private readonly PredicateCollection<MethodKey> skipStringHiding = new PredicateCollection<MethodKey> ();
		private readonly List<AssemblyInfo> references = new List<AssemblyInfo> ();
		private readonly List<AssemblyInfo> referencedBy = new List<AssemblyInfo> ();
		private List<TypeReference> unrenamedTypeReferences;
		private List<MemberReference> unrenamedReferences;
		private string filename;
		private AssemblyDefinition definition;
		private string name;
		private bool exclude = false;

		public bool Exclude {
			get { return exclude; }
			set { exclude = value; }
		}

		bool initialized = false;
		// to create, use FromXml
		private AssemblyInfo (Project project)
		{
			this.project = project;
		}

		private static bool AssemblyIsSigned (AssemblyDefinition def)
		{
			return def.Name.PublicKeyToken.Length != 0;
		}

		public static AssemblyInfo FromXml (Project project, XmlReader reader, Variables vars)
		{
			Debug.Assert (reader.NodeType == XmlNodeType.Element && reader.Name == "Module");

			AssemblyInfo info = new AssemblyInfo (project);

			// pull out the file attribute, but don't process anything empty
			string val = Helper.GetAttribute (reader, "file", vars);
			if (val.Length > 0) {
				info.LoadAssembly (val);

				if (AssemblyIsSigned (info.Definition) && project.Settings.KeyFile == null)
					throw new ApplicationException ("Obfuscating a signed assembly would result in an invalid assembly:  " + info.Name + "; use the KeyFile property to set a key to use");
			} else
				throw new InvalidOperationException ("Need valid file attribute.");

			string isExcluded = Helper.GetAttribute (reader, "Exclude", vars);
			if ((isExcluded.Length > 0) && (isExcluded.ToLowerInvariant () == "true")) {
				info.Exclude = true;
			}

			if (!reader.IsEmptyElement) {
				while (reader.Read ()) {
					if (reader.NodeType == XmlNodeType.Element) {
						string name = Helper.GetAttribute (reader, "name", vars);
                    
						string rxStr = Helper.GetAttribute (reader, "rx");
						Regex rx = null;
						if (!string.IsNullOrEmpty (rxStr)) {
							rx = new Regex (rxStr);
						}

						string isStaticStr = Helper.GetAttribute (reader, "static", vars);
						bool? isStatic = null;
						if (!string.IsNullOrEmpty (isStaticStr)) {
							isStatic = XmlConvert.ToBoolean (isStaticStr);
						}

						string isSerializableStr = Helper.GetAttribute (reader, "serializable", vars);
						bool? isSerializable = null;
						if (!string.IsNullOrEmpty (isSerializableStr)) {
							isSerializable = XmlConvert.ToBoolean (isSerializableStr);
						}

						string attrib = Helper.GetAttribute (reader, "attrib", vars);
						string inherits = Helper.GetAttribute (reader, "typeinherits", vars);
						string type = Helper.GetAttribute (reader, "type", vars);
						string typeattrib = Helper.GetAttribute (reader, "typeattrib", vars);

						switch (reader.Name) {
						case "SkipNamespace":
							if (rx != null) {
								info.skipNamespaces.Add (new NamespaceTester (rx));	
							} else {
								info.skipNamespaces.Add (new NamespaceTester (name));
							}
							break;
						case "SkipType":
							TypeSkipFlags skipFlags = TypeSkipFlags.SkipNone;

							val = Helper.GetAttribute (reader, "skipMethods", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeSkipFlags.SkipMethod;

							val = Helper.GetAttribute (reader, "skipStringHiding", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeSkipFlags.SkipStringHiding;

							val = Helper.GetAttribute (reader, "skipFields", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeSkipFlags.SkipField;

							val = Helper.GetAttribute (reader, "skipProperties", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeSkipFlags.SkipProperty;

							val = Helper.GetAttribute (reader, "skipEvents", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeSkipFlags.SkipEvent;
                                
							if (rx != null) {
								info.skipTypes.Add (new TypeTester (rx, skipFlags, attrib, inherits, isStatic, isSerializable));
							} else {
								info.skipTypes.Add (new TypeTester (name, skipFlags, attrib, inherits, isStatic, isSerializable));
							}
							break;
						case "SkipMethod":
							if (rx != null) {
								info.skipMethods.Add (new MethodTester (rx, type, attrib, typeattrib, inherits, isStatic));
							} else {
								info.skipMethods.Add (new MethodTester (name, type, attrib, typeattrib, inherits, isStatic));
							}
							break;
						case "SkipStringHiding":
							if (rx != null) {
								info.skipStringHiding.Add (new MethodTester (rx, type, attrib, typeattrib));
							} else {
								info.skipStringHiding.Add (new MethodTester (name, type, attrib, typeattrib));
							}
							break;
						case "SkipField":
							string decorator = Helper.GetAttribute (reader, "decorator", vars);

							if (rx != null) {
								info.skipFields.Add (new FieldTester (rx, type, attrib, typeattrib, inherits, decorator, isStatic, isSerializable));
							} else {
								info.skipFields.Add (new FieldTester (name, type, attrib, typeattrib, inherits, decorator, isStatic, isSerializable));
							}
							break;
						case "SkipProperty":
							if (rx != null) {
								info.skipProperties.Add (new PropertyTester (rx, type, attrib, typeattrib));
							} else {
								info.skipProperties.Add (new PropertyTester (name, type, attrib, typeattrib));
							}
							break;
						case "SkipEvent":
							if (rx != null) {
								info.skipEvents.Add (new EventTester (rx, type, attrib, typeattrib));
							} else {
								info.skipEvents.Add (new EventTester (name, type, attrib, typeattrib));
							}
							break;
						}
					} else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Module") {
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
		internal void Init ()
		{
			unrenamedReferences = new List<MemberReference> ();
			var items = getMemberReferences ();
			foreach (MemberReference member in items) {
				// FIXME: Figure out why these exist if they are never used.
				// MethodReference mr = member as MethodReference;
				// FieldReference fr = member as FieldReference;
				if (project.Contains (member.DeclaringType))
					unrenamedReferences.Add (member);
			}

			HashSet<TypeReference> typerefs = new HashSet<TypeReference> ();
			foreach (TypeReference type in definition.MainModule.GetTypeReferences()) {
				if (type.FullName == "<Module>")
					continue;

				if (project.Contains (type))
					typerefs.Add (type);
			}

			// Type references in CustomAttributes
			List<CustomAttribute> customattributes = new List<CustomAttribute> ();
			customattributes.AddRange (this.Definition.CustomAttributes);
			foreach (TypeDefinition type in GetAllTypeDefinitions()) {

				customattributes.AddRange (type.CustomAttributes);
				foreach (MethodDefinition methoddef in type.Methods)
					customattributes.AddRange (methoddef.CustomAttributes);
				foreach (FieldDefinition fielddef in type.Fields)
					customattributes.AddRange (fielddef.CustomAttributes);
				foreach (EventDefinition eventdef in type.Events)
					customattributes.AddRange (eventdef.CustomAttributes);
				foreach (PropertyDefinition propertydef in type.Properties)
					customattributes.AddRange (propertydef.CustomAttributes);

				foreach (CustomAttribute customattribute in customattributes) {
					// Check Constructor and named parameter for argument of type "System.Type". i.e. typeof()
					List<CustomAttributeArgument> customattributearguments = new List<CustomAttributeArgument> ();
					customattributearguments.AddRange (customattribute.ConstructorArguments);
					foreach (CustomAttributeNamedArgument namedargument in customattribute.Properties)
						customattributearguments.Add (namedargument.Argument);

					foreach (CustomAttributeArgument ca in customattributearguments) {
						if (ca.Type.FullName == "System.Type")
							typerefs.Add ((TypeReference)ca.Value);
					}
				}
				customattributes.Clear ();
			}

			unrenamedTypeReferences = new List<TypeReference> (typerefs);

			initialized = true;
		}

		public IEnumerable<TypeDefinition> GetAllTypeDefinitions ()
		{
			var result = new List<TypeDefinition> ();
			foreach (TypeDefinition typedef in definition.MainModule.Types)
				GetAllTypeDefinitions (typedef, result);

			return result;
		}

		private void GetAllTypeDefinitions (TypeDefinition type, IList<TypeDefinition> result)
		{
			result.Add (type);
			foreach (var nestedTypeDefition in type.NestedTypes)
				GetAllTypeDefinitions (nestedTypeDefition, result);
		}

		private IEnumerable<MemberReference> getMemberReferences ()
		{
			HashSet<MemberReference> memberreferences = new HashSet<MemberReference> ();
			foreach (TypeDefinition type in this.GetAllTypeDefinitions()) {
				foreach (MethodDefinition method in type.Methods) {
					if (method.Body != null) {
						foreach (Instruction inst in method.Body.Instructions) {
							MemberReference memberref = inst.Operand as MemberReference;
							if (memberref != null) {
								if (IsOnlyReference (memberref) || memberref is FieldReference && !(memberref is FieldDefinition)) {
									// FIXME: Figure out why this exists if it is never used.
									// int c = memberreferences.Count;
									memberreferences.Add (memberref);
								}
							}
						}
					}
				}
			}
			return memberreferences;
		}

		private bool IsOnlyReference (MemberReference memberref)
		{
			if (memberref is MethodReference) {
				if (memberref is MethodDefinition) {
					return false;
				}

				if (memberref is MethodSpecification) {
					if (memberref is GenericInstanceMethod) {
						return true;
					}

					return false;
				}

				return !(memberref is CallSite);
			}

			return false;
		}

		IEnumerable<TypeReference> getTypeReferences ()
		{
			List<TypeReference> typereferences = new List<TypeReference> ();
			foreach (TypeDefinition type in this.GetAllTypeDefinitions()) {
				foreach (MethodDefinition method in type.Methods) {
					if (method.Body != null) {
						foreach (Instruction inst in method.Body.Instructions) {
							TypeReference typeref = inst.Operand as TypeReference;
							if (typeref != null) {
								if (!(typeref is TypeDefinition) && !(typeref is TypeSpecification))
									typereferences.Add (typeref);
							}
						}
					}
				}
			}
			return typereferences;
		}

		private void LoadAssembly (string filename)
		{
			this.filename = filename;

			try {
				definition = AssemblyDefinition.ReadAssembly (filename);
				name = definition.Name.Name;
			} catch (System.IO.IOException e) {
				throw new ApplicationException ("Unable to find assembly:  " + filename, e);
			}
		}

		public string Filename {
			get {
				CheckLoaded ();
				return filename;
			}
		}

		public AssemblyDefinition Definition {
			get {
				CheckLoaded ();
				return definition;
			}
		}

		public string Name {
			get {
				CheckLoaded ();
				return name;
			}
		}

		public List<MemberReference> UnrenamedReferences {
			get {
				CheckInitialized ();
				return unrenamedReferences;
			}
		}

		public List<TypeReference> UnrenamedTypeReferences {
			get {
				CheckInitialized ();
				return unrenamedTypeReferences;
			}
		}

		public List<AssemblyInfo> References {
			get { return references; }
		}

		public List<AssemblyInfo> ReferencedBy {
			get { return referencedBy; }
		}

		public void ForceSkip (MethodKey method)
		{
			skipMethods.Add (new MethodTester (method));
		}

		private bool ShouldSkip (string ns, InheritMap map)
		{
			return skipNamespaces.IsMatch (ns, map);
		}

		public bool ShouldSkip (TypeKey type, TypeSkipFlags flag, InheritMap map, bool hidePrivateApi)
		{
			if (ShouldSkip (type.Namespace, map)) {
				if (!hidePrivateApi) {
					return true;
				}
			}

			foreach (TypeTester typeTester in skipTypes) {
				if ((typeTester.SkipFlags & flag) > 0 && typeTester.Test (type, map))
					return true;
			}

			return false;
		}

		public bool ShouldSkip (TypeKey type, InheritMap map, bool keepPublicApi, bool hidePrivateApi)
		{
			if (skipTypes.IsMatch (type, map))
				return true;

			if (ShouldSkip (type.Namespace, map)) {
				if (type.TypeDefinition.IsPublic) {
					return keepPublicApi;
				}

				if (!hidePrivateApi) {
					return true;
				}
			}

			return false;
		}

		public bool ShouldSkip (MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi)
		{
			if (ShouldSkip (method.TypeKey, TypeSkipFlags.SkipMethod, map, hidePrivateApi))
				return true;

			if (skipMethods.IsMatch (method, map))
				return true;

			return method.ShouldSkip (keepPublicApi, hidePrivateApi);
		}

		public bool ShouldSkipStringHiding (MethodKey method, InheritMap map, bool hidePrivateApi)
		{
			if (ShouldSkip (method.TypeKey, TypeSkipFlags.SkipStringHiding, map, hidePrivateApi))
				return true;

			return skipStringHiding.IsMatch (method, map);
		}

		public bool ShouldSkip (FieldKey field, InheritMap map, bool keepPublicApi, bool hidePrivateApi)
		{
			if (ShouldSkip (field.TypeKey, TypeSkipFlags.SkipField, map, hidePrivateApi))
				return true;

			if (skipFields.IsMatch (field, map))
				return true;

			return field.ShouldSkip (keepPublicApi, hidePrivateApi);
		}

		public bool ShouldSkip (PropertyKey prop, InheritMap map, bool keepPublicApi, bool hidePrivateApi)
		{
			if (ShouldSkip (prop.TypeKey, TypeSkipFlags.SkipProperty, map, hidePrivateApi))
				return true;

			if (skipProperties.IsMatch (prop, map))
				return true;

			return prop.ShouldSkip (keepPublicApi, hidePrivateApi);
		}

		public bool ShouldSkip (EventKey evt, InheritMap map, bool keepPublicApi, bool hidePrivateApi)
		{
			if (ShouldSkip (evt.TypeKey, TypeSkipFlags.SkipEvent, map, hidePrivateApi))
				return true;

			if (skipEvents.IsMatch (evt, map))
				return true;

			return evt.ShouldSkip (keepPublicApi, hidePrivateApi);
		}

		/// <summary>
		/// Makes sure that the assembly definition has been loaded (by <see cref="LoadAssembly"/>).
		/// </summary>
		private void CheckLoaded ()
		{
			if (definition == null)
				throw new InvalidOperationException ("Expected that AssemblyInfo.LoadAssembly would be called before use.");
		}

		/// <summary>
		/// Makes sure that the assembly has been initialized (by <see cref="Init"/>).
		/// </summary>
		private void CheckInitialized ()
		{
			if (!initialized)
				throw new InvalidOperationException ("Expected that AssemblyInfo.Init would be called before use.");
		}

		public override string ToString ()
		{
			return Name;
		}
	}
}
