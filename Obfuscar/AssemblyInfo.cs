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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
		private readonly PredicateCollection<string> forceNamespaces = new PredicateCollection<string> ();
		private readonly PredicateCollection<TypeKey> forceTypes = new PredicateCollection<TypeKey> ();
		private readonly PredicateCollection<MethodKey> forceMethods = new PredicateCollection<MethodKey> ();
		private readonly PredicateCollection<FieldKey> forceFields = new PredicateCollection<FieldKey> ();
		private readonly PredicateCollection<PropertyKey> forceProperties = new PredicateCollection<PropertyKey> ();
		private readonly PredicateCollection<EventKey> forceEvents = new PredicateCollection<EventKey> ();
		private readonly PredicateCollection<MethodKey> skipStringHiding = new PredicateCollection<MethodKey> ();
		private readonly List<AssemblyInfo> references = new List<AssemblyInfo> ();
		private readonly List<AssemblyInfo> referencedBy = new List<AssemblyInfo> ();
		private List<TypeReference> unrenamedTypeReferences;
		private List<MemberReference> unrenamedReferences;
		private string filename;
		private AssemblyDefinition definition;
		private string name;
		private string tagname;

		public string TagName {
			get { return tagname; }
			set { tagname = value; }
		}

		private bool exclude = false;

		public bool Exclude {
			get { return exclude; }
			set { exclude = value; }
		}

		bool initialized = false;
		// to create, use FromXml
		private AssemblyInfo (Project project, string tagname)
		{
			this.project = project;
			this.tagname = tagname;
		}

		private static bool AssemblyIsSigned (AssemblyDefinition def)
		{
			return def.Name.PublicKeyToken.Length != 0;
		}

		public static AssemblyInfo FromXml (Project project, XmlReader reader, Variables vars)
		{
			Debug.Assert (reader.NodeType == XmlNodeType.Element && (reader.Name == "Module" || reader.Name == "Assembly"));

			AssemblyInfo info = new AssemblyInfo (project, reader.Name);

			// pull out the file attribute, but don't process anything empty
			string val = Helper.GetAttribute (reader, "file", vars);
			if (val.Length > 0) {
				info.LoadAssembly (val);

				if (AssemblyIsSigned (info.Definition) && project.Settings.KeyFile == null)
					throw new ObfuscarException ("Obfuscating a signed assembly would result in an invalid assembly:  " + info.Name + "; use the KeyFile property to set a key to use");
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
						case "ForceNamespace":
							if (rx != null) {
								info.forceNamespaces.Add (new NamespaceTester (rx));
							} else {
								info.forceNamespaces.Add (new NamespaceTester (name));
							}
							break;
						case "SkipType":
							TypeAffectFlags skipFlags = TypeAffectFlags.SkipNone;

							val = Helper.GetAttribute (reader, "skipMethods", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeAffectFlags.AffectMethod;

							val = Helper.GetAttribute (reader, "skipStringHiding", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeAffectFlags.SkipStringHiding;

							val = Helper.GetAttribute (reader, "skipFields", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeAffectFlags.AffectField;

							val = Helper.GetAttribute (reader, "skipProperties", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeAffectFlags.AffectProperty;

							val = Helper.GetAttribute (reader, "skipEvents", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								skipFlags |= TypeAffectFlags.AffectEvent;

							if (rx != null) {
								info.skipTypes.Add (new TypeTester (rx, skipFlags, attrib, inherits, isStatic, isSerializable));
							} else {
								info.skipTypes.Add (new TypeTester (name, skipFlags, attrib, inherits, isStatic, isSerializable));
							}
							break;
						case "ForceType":
							TypeAffectFlags forceFlags = TypeAffectFlags.SkipNone;

							val = Helper.GetAttribute (reader, "forceMethods", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								forceFlags |= TypeAffectFlags.AffectMethod;

							val = Helper.GetAttribute (reader, "forceFields", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								forceFlags |= TypeAffectFlags.AffectField;

							val = Helper.GetAttribute (reader, "forceProperties", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								forceFlags |= TypeAffectFlags.AffectProperty;

							val = Helper.GetAttribute (reader, "forceEvents", vars);
							if (val.Length > 0 && XmlConvert.ToBoolean (val))
								forceFlags |= TypeAffectFlags.AffectEvent;

							if (rx != null) {
								info.forceTypes.Add (new TypeTester (rx, forceFlags, attrib, inherits, isStatic, isSerializable));
							} else {
								info.forceTypes.Add (new TypeTester (name, forceFlags, attrib, inherits, isStatic, isSerializable));
							}
							break;
						case "SkipMethod":
							if (rx != null) {
								info.skipMethods.Add (new MethodTester (rx, type, attrib, typeattrib, inherits, isStatic));
							} else {
								info.skipMethods.Add (new MethodTester (name, type, attrib, typeattrib, inherits, isStatic));
							}
							break;
						case "ForceMethod":
							if (rx != null) {
								info.forceMethods.Add (new MethodTester (rx, type, attrib, typeattrib, inherits, isStatic));
							} else {
								info.forceMethods.Add (new MethodTester (name, type, attrib, typeattrib, inherits, isStatic));
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
						case "ForceField":
							string decorator1 = Helper.GetAttribute (reader, "decorator", vars);

							if (rx != null) {
								info.forceFields.Add (new FieldTester (rx, type, attrib, typeattrib, inherits, decorator1, isStatic, isSerializable));
							} else {
								info.forceFields.Add (new FieldTester (name, type, attrib, typeattrib, inherits, decorator1, isStatic, isSerializable));
							}
							break;
						case "SkipProperty":
							if (rx != null) {
								info.skipProperties.Add (new PropertyTester (rx, type, attrib, typeattrib));
							} else {
								info.skipProperties.Add (new PropertyTester (name, type, attrib, typeattrib));
							}
							break;
						case "ForceProperty":
							if (rx != null) {
								info.forceProperties.Add (new PropertyTester (rx, type, attrib, typeattrib));
							} else {
								info.forceProperties.Add (new PropertyTester (name, type, attrib, typeattrib));
							}
							break;
						case "SkipEvent":
							if (rx != null) {
								info.skipEvents.Add (new EventTester (rx, type, attrib, typeattrib));
							} else {
								info.skipEvents.Add (new EventTester (name, type, attrib, typeattrib));
							}
							break;
						case "ForceEvent":
							if (rx != null) {
								info.forceEvents.Add (new EventTester (rx, type, attrib, typeattrib));
							} else {
								info.forceEvents.Add (new EventTester (name, type, attrib, typeattrib));
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

		private class Graph
		{
			public List<Node<TypeDefinition>> Root = new List<Node<TypeDefinition>> ();
			public Dictionary<string, Node<TypeDefinition>> _map = new Dictionary<string, Node<TypeDefinition>> ();

			public Graph (IEnumerable<TypeDefinition> items)
			{
				foreach (var item in items) {
					var node = new Node<TypeDefinition> { Item = item };
					Root.Add (node);
					_map.Add (item.FullName, node);
				}

				AddParents (Root, _map);
			}

			private static void AddParents (List<Node<TypeDefinition>> nodes, Dictionary<string, Node<TypeDefinition>> map)
			{
				foreach (var node in nodes) {
					var baseType = node.Item.BaseType;
					if (baseType != null) {
						var parent = SearchNode (baseType, map);
						node.AppendTo (parent);
					}

					if (node.Item.HasInterfaces)
						foreach (var inter in node.Item.Interfaces) {
							var parent = SearchNode (inter, map);
							node.AppendTo (parent);
						}

					var nestedParent = node.Item.DeclaringType;
					if (nestedParent != null) {
						var parent = SearchNode (nestedParent, map);
						node.AppendTo (parent);                        
					}
				}
			}

			private static Node<TypeDefinition> SearchNode (TypeReference baseType, Dictionary<string, Node<TypeDefinition>> map)
			{
				var key = baseType.FullName;
				return map.ContainsKey (key) ? map [key] : null;
			}

			internal IEnumerable<TypeDefinition> GetOrderedList ()
			{
				var result = new List<TypeDefinition> ();
				CleanPool (Root, result);
				return result;
			}

			private void CleanPool (List<Node<TypeDefinition>> pool, List<TypeDefinition> result)
			{
				while (pool.Count > 0) {
					var toRemoved = new List<Node<TypeDefinition>> ();
					foreach (var node in pool) {
						if (node.Parents.Count == 0) {
							toRemoved.Add (node);
							if (result.Contains (node.Item))
								continue;

							result.Add (node.Item);
						}
					}

					foreach (var remove in toRemoved) {
						pool.Remove (remove);
						foreach (var child in remove.Children) {
							if (result.Contains (child.Item))
								continue;

							child.Parents.Remove (remove);
						}
					}
				}
			}
		}

		private IEnumerable<TypeDefinition> _cached;

		public IEnumerable<TypeDefinition> GetAllTypeDefinitions ()
		{
			if (_cached != null) {
				return _cached;
			}

			var result = definition.MainModule.GetAllTypes ();
			var graph = new Graph (result);
			return _cached = graph.GetOrderedList ();
		}

		public void InvalidateCache ()
		{
			_cached = null;
		}

		private IEnumerable<MemberReference> getMemberReferences ()
		{
			HashSet<MemberReference> memberreferences = new HashSet<MemberReference> ();
			foreach (TypeDefinition type in this.GetAllTypeDefinitions()) {
				foreach (MethodDefinition method in type.Methods) {

					foreach (MethodReference memberref in method.Overrides) {
						if (IsOnlyReference (memberref)) {
							memberreferences.Add (memberref);
						}
					}
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
				bool readSymbols = project.Settings.RegenerateDebugInfo && System.IO.File.Exists (System.IO.Path.ChangeExtension (filename, "pdb"));
				try {
					definition = AssemblyDefinition.ReadAssembly (filename, new ReaderParameters {
						ReadingMode = ReadingMode.Deferred,
						ReadSymbols = readSymbols,
						AssemblyResolver = project.Cache
					});
				} catch { // If there's a non-matching pdb next to it, this fails, else just try again
					if (!readSymbols)
						throw;
					definition = AssemblyDefinition.ReadAssembly (filename, new ReaderParameters {
						ReadingMode = ReadingMode.Deferred,
						ReadSymbols = false,
						AssemblyResolver = project.Cache
					});
				}

				project.Cache.RegisterAssembly (definition);

				// IMPORTANT: read again but with full mode.
				try {
					definition = AssemblyDefinition.ReadAssembly (filename, new ReaderParameters {
						ReadingMode = ReadingMode.Immediate,
						ReadSymbols = readSymbols,
						AssemblyResolver = project.Cache
					});
				} catch { // If there's a non-matching pdb next to it, this fails, else just try again
					if (!readSymbols)
						throw;
					definition = AssemblyDefinition.ReadAssembly (filename, new ReaderParameters {
						ReadingMode = ReadingMode.Immediate,
						ReadSymbols = false,
						AssemblyResolver = project.Cache
					});
				}

				name = definition.Name.Name;
			} catch (System.IO.IOException e) {
				throw new ObfuscarException ("Unable to find assembly:  " + filename, e);
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

		private bool ShouldSkip (string ns, InheritMap map)
		{
			return skipNamespaces.IsMatch (ns, map);
		}

		private bool ShouldForce (string ns, InheritMap map)
		{
			return forceNamespaces.IsMatch (ns, map);
		}

		private bool ShouldSkip (TypeKey type, TypeAffectFlags flag, InheritMap map)
		{
			if (ShouldSkip (type.Namespace, map)) {				
				return true;
			}

			foreach (TypeTester typeTester in skipTypes) {
				if ((typeTester.AffectFlags & flag) > 0 && typeTester.Test (type, map))
					return true;
			}

			return false;
		}

		private bool ShouldForce (TypeKey type, TypeAffectFlags flag, InheritMap map)
		{
			if (ShouldForce (type.Namespace, map)) {
				return true;
			}

			foreach (TypeTester typeTester in forceTypes) {
				if ((typeTester.AffectFlags & flag) > 0 && typeTester.Test (type, map))
					return true;
			}

			return false;
		}

		public bool ShouldSkip (TypeKey type, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			var attribute = type.TypeDefinition.MarkedToRename ();
			if (attribute != null) {
				message = "attribute";
				return !attribute.Value;
			}

			if (markedOnly) {
				message = "MarkedOnly option in configuration";
				return true;
			}

			if (forceTypes.IsMatch (type, map)) {
				message = "type rule in configuration";
				return false;
			}

			if (ShouldForce (type.Namespace, map)) {
				message = "namespace rule in configuration";
				return false;
			}

			if (skipTypes.IsMatch (type, map)) {
				message = "type rule in configuration";
				return true;
			}

			if (ShouldSkip (type.Namespace, map)) {
				message = "namespace rule in configuration";
				return true;
			}

			if (type.TypeDefinition.IsTypePublic ()) {
				message = "KeepPublicApi option in configuration";
				return keepPublicApi;
			}

			message = "HidePrivateApi option in configuration";
			return !hidePrivateApi;
		}

		public bool ShouldSkip (MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			if (method.Method.IsRuntime) {
				message = "runtime method";
				return true;
			}

			if (method.Method.IsSpecialName) {
				switch (method.Method.SemanticsAttributes) {
				case MethodSemanticsAttributes.Getter:
				case MethodSemanticsAttributes.Setter:
					message = "skipping properties";
					return !project.Settings.RenameProperties;
				case MethodSemanticsAttributes.AddOn:
				case MethodSemanticsAttributes.RemoveOn:
					message = "skipping events";
					return !project.Settings.RenameEvents;
				default:
					message = "special name";
					return true;
				}
			}

			return ShouldSkipParams (method, map, keepPublicApi, hidePrivateApi, markedOnly, out message);
		}

		public bool ShouldSkipParams (MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			var attribute = method.Method.MarkedToRename ();
			// skip runtime methods
			if (attribute != null) {
				message = "attribute";
				return !attribute.Value;
			}

			var parent = method.DeclaringType.MarkedToRename ();
			if (parent != null) {
				message = "type attribute";
				return !parent.Value;
			}

			if (markedOnly) {
				message = "MarkedOnly option in configuration";
				return true;
			}

			if (ShouldForce (method.TypeKey, TypeAffectFlags.AffectMethod, map)) {
				message = "type rule in configuration";
				return false;
			}

			if (forceMethods.IsMatch (method, map)) {
				message = "method rule in configuration";
				return false;
			}

			if (ShouldSkip (method.TypeKey, TypeAffectFlags.AffectMethod, map)) {
				message = "type rule in configuration";
				return true;
			}

			if (skipMethods.IsMatch (method, map)) {
				message = "method rule in configuration";
				return true;
			}

			if (method.DeclaringType.IsTypePublic () && (method.Method.IsPublic || method.Method.IsFamily)) {
				message = "KeepPublicApi option in configuration";
				return keepPublicApi;
			}

			message = "HidePrivateApi option in configuration";
			return !hidePrivateApi;
		}

		public bool ShouldSkipStringHiding (MethodKey method, InheritMap map, bool hidePrivateApi)
		{
			if (ShouldSkip (method.TypeKey, TypeAffectFlags.SkipStringHiding, map))
				return true;

			return skipStringHiding.IsMatch (method, map);
		}

		public bool ShouldSkip (FieldKey field, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			// skip runtime methods
			if ((field.Field.IsRuntimeSpecialName && field.Field.Name == "value__")) {
				message = "special name";
				return true;
			}

			var attribute = field.Field.MarkedToRename ();
			if (attribute != null) {
				message = "attribute";
				return !attribute.Value;
			}

			var parent = field.DeclaringType.MarkedToRename ();
			if (parent != null) {
				message = "type attribute";
				return !parent.Value;
			}

			if (markedOnly) {
				message = "MarkedOnly option in configuration";
				return true;
			}

			if (ShouldForce (field.TypeKey, TypeAffectFlags.AffectField, map)) {
				message = "type rule in configuration";
				return false;
			}

			if (forceFields.IsMatch (field, map)) {
				message = "field rule in configuration";
				return false;
			}

			if (ShouldSkip (field.TypeKey, TypeAffectFlags.AffectField, map)) {
				message = "type rule in configuration";
				return true;
			}

			if (skipFields.IsMatch (field, map)) {
				message = "field rule in configuration";
				return true;
			}

			if (field.DeclaringType.IsTypePublic () && (field.Field.IsPublic || field.Field.IsFamily)) {
				message = "KeepPublicApi option in configuration";
				return keepPublicApi;
			}

			message = "HidePrivateApi option in configuration";
			return !hidePrivateApi;
		}

		public bool ShouldSkip (PropertyKey prop, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			if (prop.Property.IsRuntimeSpecialName) {
				message = "runtime special name";
				return true;
			}

			var attribute = prop.Property.MarkedToRename ();
			if (attribute != null) {
				message = "attribute";
				return !attribute.Value;
			}

			var parent = prop.DeclaringType.MarkedToRename ();
			if (parent != null) {
				message = "type attribute";
				return !parent.Value;
			}

			if (markedOnly) {
				message = "MarkedOnly option in configuration";
				return true;
			}

			if (ShouldForce (prop.TypeKey, TypeAffectFlags.AffectProperty, map)) {
				message = "type rule in configuration";
				return false;
			}

			if (forceProperties.IsMatch (prop, map)) {
				message = "property rule in configuration";
				return false;
			}

			if (ShouldSkip (prop.TypeKey, TypeAffectFlags.AffectProperty, map)) {
				message = "type rule in configuration";
				return true;
			}

			if (skipProperties.IsMatch (prop, map)) {
				message = "property rule in configuration";
				return true;
			}

			if (prop.DeclaringType.IsTypePublic () && (IsGetterPublic (prop.Property) || IsSetterPublic (prop.Property))) {
				message = "KeepPublicApi option in configuration";
				return keepPublicApi;
			}

			message = "HidePrivateApi option in configuration";
			return !hidePrivateApi;
		}

		public bool ShouldSkip (EventKey evt, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly, out string message)
		{
			// skip runtime special events
			if (evt.Event.IsRuntimeSpecialName) {
				message = "runtime special name";
				return true;
			}

			var attribute = evt.Event.MarkedToRename ();
			// skip runtime methods
			if (attribute != null) {
				message = "attribute";
				return !attribute.Value;
			}

			var parent = evt.DeclaringType.MarkedToRename ();
			if (parent != null) {
				message = "type attribute";
				return !parent.Value;
			}

			if (markedOnly) {
				message = "MarkedOnly option in configuration";
				return true;
			}

			if (ShouldForce (evt.TypeKey, TypeAffectFlags.AffectEvent, map)) {
				message = "type rule in configuration";
				return false;
			}

			if (forceEvents.IsMatch (evt, map)) {
				message = "event rule in configuration";
				return false;
			}

			if (ShouldSkip (evt.TypeKey, TypeAffectFlags.AffectEvent, map)) {
				message = "type rule in configuration";
				return true;
			}

			if (skipEvents.IsMatch (evt, map)) {
				message = "event rule in configuration";
				return true;
			}

			if (evt.DeclaringType.IsTypePublic () && (IsAddPublic (evt.Event) || IsRemovePublic (evt.Event))) {
				message = "KeepPublicApi option in configuration";
				return keepPublicApi;
			}

			message = "HidePrivateApi option in configuration";
			return !hidePrivateApi;
		}

		private bool IsAddPublic (EventDefinition eventDefinition)
		{
			return eventDefinition.AddMethod != null && (eventDefinition.AddMethod.IsPublic || eventDefinition.AddMethod.IsFamily);
		}

		private bool IsRemovePublic (EventDefinition eventDefinition)
		{
			return eventDefinition.RemoveMethod != null && (eventDefinition.RemoveMethod.IsPublic || eventDefinition.RemoveMethod.IsFamily);
		}

		private bool IsGetterPublic (PropertyDefinition propertyDefinition)
		{
			return propertyDefinition.GetMethod != null && (propertyDefinition.GetMethod.IsPublic || propertyDefinition.GetMethod.IsFamily);
		}

		private bool IsSetterPublic (PropertyDefinition propertyDefinition)
		{
			return propertyDefinition.SetMethod != null && (propertyDefinition.SetMethod.IsPublic || propertyDefinition.SetMethod.IsFamily);
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
