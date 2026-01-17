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
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using ICSharpCode.BamlDecompiler.Baml;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Obfuscar.Helpers;

namespace Obfuscar
{
    public class Obfuscator
    {
        // Unique names for type and members
        private int _uniqueTypeNameIndex;

        private int _uniqueMemberNameIndex;

        /// <summary>
        /// Creates an obfuscator initialized from a project file.
        /// </summary>
        /// <param name="projfile">Path to project file.</param>
        public Obfuscator(string projfile)
        {
            Mapping = new ObfuscationMap();

            try
            {
                var document = XDocument.Load(projfile);
                LoadFromReader(document, Path.GetDirectoryName(projfile));
            }
            catch (IOException e)
            {
                throw new ObfuscarException("Unable to read specified project file:  " + projfile, e);
            }
            catch (System.Xml.XmlException e)
            {
                throw new ObfuscarException($"{projfile} is not a valid XML file", e);
            }
        }

        /// <summary>
        /// Creates an obfuscator initialized from a project file.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private Obfuscator(XDocument reader)
        {
            Mapping = new ObfuscationMap();
            LoadFromReader(reader, null);
        }

        public void RunRules()
        {
            // DEBUG: Log properties at start
            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log",
                $"RunRules: AssemblyList.Count={Project.AssemblyList.Count}\n");
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log",
                    $"RunRules: Checking assembly {info.Name}, Types.Count={info.Definition.MainModule.Types.Count}\n");
                foreach (var t in info.Definition.MainModule.Types)
                {
                    System.IO.File.AppendAllText("/tmp/obfuscar_debug.log",
                        $"StartOfRules: Type={t.FullName}, PropertiesCount={t.Properties.Count}\n");
                }
            }
            
            // The SemanticAttributes of MethodDefinitions have to be loaded before any fields,properties or events are removed
            LoadMethodSemantics();

            LoggerService.Logger.LogInformation("Hiding strings...\n");
            HideStrings();

            LoggerService.Logger.LogInformation("Renaming:  fields...");
            RenameFields();

            LoggerService.Logger.LogInformation("Parameters...");
            RenameParams();

            LoggerService.Logger.LogInformation("Properties...");
            RenameProperties();

            LoggerService.Logger.LogInformation("Events...");
            RenameEvents();

            LoggerService.Logger.LogInformation("Methods...");
            RenameMethods();

            LoggerService.Logger.LogInformation("Types...");
            RenameTypes();

            PostProcessing();

            LoggerService.Logger.LogInformation("Done.\n");

            LoggerService.Logger.LogInformation("Saving assemblies...");
            SaveAssemblies();
            LoggerService.Logger.LogInformation("Done.\n");

            LoggerService.Logger.LogInformation("Writing log file...");
            SaveMapping();
            LoggerService.Logger.LogInformation("Done.\n");
        }

        public static Obfuscator CreateFromXml(string xml)
        {
            var document = XDocument.Load(new StringReader(xml));
            {
                return new Obfuscator(document);
            }
        }

        internal Project Project { get; set; }

        private void LoadFromReader(XDocument reader, string projectFileDirectory)
        {
            Project = Project.FromXml(reader, projectFileDirectory);

            // make sure everything looks good
            Project.CheckSettings();
            NameMaker.DetermineChars(Project.Settings);

            LoggerService.Logger.LogInformation("Loading assemblies...");
            LoggerService.Logger.LogInformation("Extra framework folders: ");
            foreach (var lExtraPath in Project.ExtraPaths ?? new string[0])
                LoggerService.Logger.LogInformation(lExtraPath + ", ");

            Project.LoadAssemblies();
        }

        /// <summary>
        /// Saves changes made to assemblies to the output path.
        /// </summary>
        public void SaveAssemblies(bool throwException = true)
        {
            string outPath = Project.Settings.OutPath;
            LoggerService.Logger.LogDebug("Saving assemblies to output path: {0}", outPath);

            //copy excluded assemblies
            using (var writer = Metadata.AssemblyWriterFactory.CreateWriter())
            {
                foreach (AssemblyInfo copyInfo in Project.CopyAssemblyList)
                {
                    var fileName = Path.GetFileName(copyInfo.FileName);
                    // ReSharper disable once InvocationIsSkipped
                    Debug.Assert(fileName != null, "fileName != null");
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string outName = Path.Combine(outPath, fileName);
                    LoggerService.Logger.LogDebug("Copying excluded assembly: {0} to {1}", copyInfo.Name, outName);
                    writer.Write(copyInfo.Definition, outName);
                }

                // Cecil does not properly update the name cache, so force that:
                foreach (AssemblyInfo info in Project.AssemblyList)
                {
                    var types = info.Definition.MainModule.Types;
                    // DEBUG: Log type properties before saving
                    foreach (var t in types)
                    {
                        if (t.Properties.Count > 0)
                        {
                            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log",
                                $"BeforeSave: Type={t.FullName}, PropertiesCount={t.Properties.Count}\n");
                        }
                    }
                    for (int i = 0; i < types.Count; i++)
                        types[i] = types[i];
                }

                // save the modified assemblies
                foreach (AssemblyInfo info in Project.AssemblyList)
                {
                    var fileName = Path.GetFileName(info.FileName);
                    try
                    {
                        // ReSharper disable once InvocationIsSkipped
                        Debug.Assert(fileName != null, "fileName != null");
                        // ReSharper disable once AssignNullToNotNullAttribute
                        string outName = Path.Combine(outPath, fileName);
                        var parameters = new WriterParameters();
                        if (Project.Settings.RegenerateDebugInfo)
                        {
                            LoggerService.Logger.LogDebug("Regenerating debug info for assembly: {0}", info.Name);
                            if (IsOnWindows)
                            {
                                parameters.SymbolWriterProvider = new Mono.Cecil.Cil.PortablePdbWriterProvider();
                            }
                            else
                            {
                                parameters.SymbolWriterProvider = new Mono.Cecil.Pdb.PdbWriterProvider();
                            }
                        }

                        if (info.Definition.Name.HasPublicKey)
                        {
                            LoggerService.Logger.LogDebug("Assembly {0} is signed, processing key information", info.Name);
                            // source assembly was signed.
                            if (Project.KeyPair != null)
                            {
                                // config file contains key file.
                                string keyFile = Project.KeyPair;
                                if (string.Equals(keyFile, "auto", StringComparison.OrdinalIgnoreCase))
                                {
                                    LoggerService.Logger.LogDebug("Using 'auto' key mode for assembly: {0}", info.Name);
                                    // if key file is "auto", resolve key file from assembly's attribute.
                                    var attribute = info.Definition.CustomAttributes
                                        .FirstOrDefault(item => item.AttributeType.FullName == "System.Reflection.AssemblyKeyFileAttribute");
                                    if (attribute != null && attribute?.ConstructorArguments.Count == 1)
                                    {
                                        fileName = attribute.ConstructorArguments[0].Value.ToString();
                                        if (!File.Exists(fileName))
                                        {
                                            // assume relative path.
                                            keyFile = Path.Combine(Project.Settings.InPath, fileName);
                                        }
                                        else
                                        {
                                           keyFile = fileName;
                                        }
                                        LoggerService.Logger.LogDebug("Auto-resolved key file for assembly {0}: {1}", info.Name, keyFile);
                                    }
                                }

                                if (!File.Exists(keyFile))
                                {
                                    // Try look for the key file in the project's input path if reasonable
                                    var candidate = Path.Combine(Project.Settings.InPath ?? ".", keyFile);
                                    if (File.Exists(candidate))
                                    {
                                        keyFile = candidate;
                                    }
                                    else
                                    {
                                        throw new ObfuscarException($"Cannot locate key file: {keyFile}");
                                    }
                                }

                                var keyPair = File.ReadAllBytes(keyFile);
                                // Try to detect whether the provided key pair contains a private key.
                                bool hasPrivateKey = false;
                                try
                                {
                                    using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider())
                                    {
                                        try
                                        {
                                            rsa.ImportCspBlob(keyPair);
                                            // Try to export private parameters - will throw if no private key
                                            var priv = rsa.ExportParameters(true);
                                            hasPrivateKey = true;
                                        }
                                        catch
                                        {
                                            hasPrivateKey = false;
                                        }
                                    }
                                }
                                catch
                                {
                                    // If we can't use RSACryptoServiceProvider on this platform, fall back to conservative behavior:
                                    // treat the blob as containing a private key if it's large (> 300 bytes).
                                    hasPrivateKey = keyPair.Length > 300;
                                }

                                if (hasPrivateKey)
                                {
                                    try
                                    {
                                        LoggerService.Logger.LogDebug("Attempting to sign assembly {0} with key file: {1}", info.Name, keyFile);
                                        parameters.StrongNameKeyBlob = keyPair;
                                        writer.Write(info.Definition, outName, parameters);
                                        info.OutputFileName = outName;
                                    }
                                    catch (Exception ex)
                                    {
                                        LoggerService.Logger.LogDebug("Strong-name signing failed for {0}, using delay signing. Error: {1}", 
                                            info.Name, ex.Message);
                                        parameters.StrongNameKeyBlob = null;
                                        if (info.Definition.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned))
                                        {
                                            info.Definition.MainModule.Attributes ^= ModuleAttributes.StrongNameSigned;
                                        }

                                        // delay sign.
                                        info.Definition.Name.PublicKey = keyPair;
                                        writer.Write(info.Definition, outName, parameters);
                                        info.OutputFileName = outName;
                                    }
                                }
                                else
                                {
                                    // Public-key-only SNK: perform delay signing (do not set StrongNameKeyBlob)
                                    LoggerService.Logger.LogDebug("Provided key file appears to be public-only; performing delay-sign for {0}", info.Name);
                                    info.Definition.Name.PublicKey = keyPair;
                                    writer.Write(info.Definition, outName, parameters);
                                    info.OutputFileName = outName;
                                }
                            }
                            else if (Project.KeyValue != null)
                            {
                                // config file contains key container name.
                                LoggerService.Logger.LogDebug("Signing assembly {0} using key container: {1}", 
                                    info.Name, Project.KeyContainerName);
                                writer.Write(info.Definition, outName, parameters);
                                MsNetSigner.SignAssemblyFromKeyContainer(outName, Project.KeyContainerName);
                            }
                            else if (!info.Definition.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned))
                            {
                                // When an assembly is "delay signed" and no KeyFile or KeyContainer properties were provided,
                                // keep the obfuscated assembly "delay signed" too.
                                LoggerService.Logger.LogDebug("Assembly {0} is delay-signed and will remain delay-signed", info.Name);
                                writer.Write(info.Definition, outName, parameters);
                                info.OutputFileName = outName;
                            }
                            else
                            {
                                throw new ObfuscarException($"Obfuscating a signed assembly would result in an invalid assembly:  {info.Name}; use the KeyFile or KeyContainer property to set a key to use");
                            }
                        }
                        else
                        {
                            LoggerService.Logger.LogDebug("Saving unsigned assembly: {0} to {1}", info.Name, outName);
                            writer.Write(info.Definition, outName, parameters);
                            info.OutputFileName = outName;
                        }
                    }
                    catch (Exception e)
                    {
                        if (throwException)
                        {
                            throw;
                        }

                        LoggerService.Logger.LogInformation(string.Format("\nFailed to save {0}", fileName));
                        LoggerService.Logger.LogInformation(string.Format("\n{0}: {1}", e.GetType().Name, e.Message));
                    }
                }
            }
        }

        private bool IsOnWindows {
            get {
                // https://stackoverflow.com/a/38795621/11182
                string windir = Environment.GetEnvironmentVariable("windir");
                return !string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir);
            }
        }

        private void LogMappings(string name)
        {
            foreach (var tuple in Mapping.FindClasses(name))
            {
                LoggerService.Logger.LogInformation(string.Format("\n{0} => {1}", tuple.Item1.Fullname, tuple.Item2));
            }
        }

        /// <summary>
        /// Saves the name mapping to the output path.
        /// </summary>
        private void SaveMapping()
        {
            string filename = Project.Settings.XmlMapping ? "Mapping.xml" : "Mapping.txt";

            string logPath = Path.Combine(Project.Settings.OutPath, filename);
            if (!string.IsNullOrEmpty(Project.Settings.LogFilePath))
                logPath = Project.Settings.LogFilePath;

            string lPath = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(lPath) && !Directory.Exists(lPath))
                Directory.CreateDirectory(lPath);

            using (TextWriter file = File.CreateText(logPath))
                SaveMapping(file);
        }

        /// <summary>
        /// Saves the name mapping to a text writer.
        /// </summary>
        private void SaveMapping(TextWriter writer)
        {
            IMapWriter mapWriter = Project.Settings.XmlMapping
                ? new XmlMapWriter(writer)
                : (IMapWriter) new TextMapWriter(writer);

            mapWriter.WriteMap(Mapping);
        }

        /// <summary>
        /// Returns the obfuscation map for the project.
        /// </summary>
        internal ObfuscationMap Mapping { get; private set; }

        /// <summary>
        /// Calls the SemanticsAttributes-getter for all methods
        /// </summary>
        private void LoadMethodSemantics()
        {
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    foreach (MethodDefinition method in type.Methods)
                    {
                        // ReSharper disable once UnusedVariable
                        var value = method.SemanticsAttributes.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Renames fields in the project.
        /// </summary>
        public void RenameFields()
        {
            if (!Project.Settings.RenameFields)
            {
                return;
            }

            foreach (var info in Project.AssemblyList)
            {
                // loop through the types
                foreach (var type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    var typeKey = new TypeKey(type);

                    var nameGroups = new Dictionary<string, NameGroup>();

                    // rename field, grouping according to signature
                    foreach (FieldDefinition field in type.Fields)
                    {
                        ProcessField(field, typeKey, nameGroups, info);
                    }
                }
            }
        }

        private void ProcessField(FieldDefinition field, TypeKey typeKey, Dictionary<string, NameGroup> nameGroups,
            AssemblyInfo info)
        {
            string sig = field.FieldType.FullName;
            var fieldKey = new FieldKey(typeKey, sig, field.Name, field);
            NameGroup nameGroup = GetNameGroup(nameGroups, sig);

            // skip filtered fields
            string skip;
            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                $"ProcessField: {typeKey.Fullname}.{field.Name}, Scope={typeKey.Scope}, FieldKey={fieldKey}, HidePrivateApi={Project.Settings.HidePrivateApi}\n");
            if (info.ShouldSkip(fieldKey, Project.InheritMap, Project.Settings.KeepPublicApi,
                Project.Settings.HidePrivateApi,
                Project.Settings.MarkedOnly, out skip))
            {
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                    $"  -> Skipped: {skip}\n");
                LoggerService.Logger.LogDebug("Field {0} in type {1} skipped: {2}", field.Name, typeKey, skip);
                Mapping.UpdateField(fieldKey, ObfuscationStatus.Skipped, skip);
                nameGroup.Add(fieldKey.Name);
                return;
            }

            var newName = Project.Settings.ReuseNames
                ? nameGroup.GetNext()
                : NameMaker.UniqueName(_uniqueMemberNameIndex++);

            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                $"  -> Renamed to: {newName}\n");
            LoggerService.Logger.LogDebug("Renaming field {0} in type {1} to {2}", field.Name, typeKey, newName);
            RenameField(info, fieldKey, field, newName);
            nameGroup.Add(newName);
        }

        private void RenameField(AssemblyInfo info, FieldKey fieldKey, FieldDefinition field, string newName)
        {
            // find references, rename them, then rename the field itself
            foreach (AssemblyInfo reference in info.ReferencedBy)
            {
                for (int i = 0; i < reference.UnrenamedReferences.Count;)
                {
                    FieldReference member = reference.UnrenamedReferences[i] as FieldReference;
                    if (member != null)
                    {
                        // DEBUG: Log field reference matching
                        System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                            $"  Checking FieldRef: {member.DeclaringType?.FullName}.{member.Name}, Type={member.FieldType?.FullName} vs Key Type={fieldKey.Type}, Name={fieldKey.Name}\n");
                        
                        if (fieldKey.Matches(member))
                        {
                            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                $"    -> MATCHED! Renaming to {newName}\n");
                            member.Name = newName;
                            reference.UnrenamedReferences.RemoveAt(i);

                            // since we removed one, continue without the increment
                            continue;
                        }
                    }

                    i++;
                }
            }

            field.Name = newName;
            Mapping.UpdateField(fieldKey, ObfuscationStatus.Renamed, newName);
        }

        /// <summary>
        /// Renames constructor, method, and generic parameters.
        /// </summary>
        public void RenameParams()
        {
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                // loop through the types
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    // rename the method parameters
                    foreach (MethodDefinition method in type.Methods)
                        RenameParams(method, info);

                    string skip;
                    // rename the class parameters
                    if (info.ShouldSkip(new TypeKey(type), Project.InheritMap, Project.Settings.KeepPublicApi,
                        Project.Settings.HidePrivateApi, Project.Settings.MarkedOnly, 
                        Project.Settings.SkipGenerated, out skip))
                        continue;

                    int index = 0;
                    foreach (GenericParameter param in type.GenericParameters)
                        param.Name = NameMaker.UniqueName(index++);
                }
            }
        }

        private void RenameParams(MethodDefinition method, AssemblyInfo info)
        {
            MethodKey methodkey = new MethodKey(method);
            string skip;
            if (info.ShouldSkipParams(methodkey, Project.InheritMap, Project.Settings.KeepPublicApi,
                Project.Settings.HidePrivateApi, Project.Settings.MarkedOnly, out skip))
                return;

            foreach (ParameterDefinition param in method.Parameters)
                if (param.CustomAttributes.Count == 0)
                    param.Name = null;

            int index = 0;
            foreach (GenericParameter param in method.GenericParameters)
                if (param.CustomAttributes.Count == 0)
                    param.Name = NameMaker.UniqueName(index++);
        }

        /// <summary>
        /// Renames types and resources in the project.
        /// </summary>
        public void RenameTypes()
        {
            //var typerenamemap = new Dictionary<string, string> (); // For patching the parameters of typeof(xx) attribute constructors
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                AssemblyDefinition library = info.Definition;

                // make a list of the resources that can be renamed
                List<Resource> resources = new List<Resource>(library.MainModule.Resources.Count);
                resources.AddRange(library.MainModule.Resources);

                var xamlFiles = GetXamlDocuments(library, Project.Settings.AnalyzeXaml);
                var namesInXaml = NamesInXaml(xamlFiles);

                // Save the original names of all types because parent (declaring) types of nested types may be already renamed.
                // The names are used for the mappings file.
                Dictionary<TypeDefinition, TypeKey> unrenamedTypeKeys =
                    info.GetAllTypeDefinitions().ToDictionary(type => type, type => new TypeKey(type));

                // loop through the types
                int typeIndex = 0;
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                        continue;

                    if (type.FullName.IndexOf("<PrivateImplementationDetails>{", StringComparison.Ordinal) >= 0)
                        continue;

                    TypeKey oldTypeKey = new TypeKey(type);
                    TypeKey unrenamedTypeKey = unrenamedTypeKeys[type];
                    string fullName = type.FullName;

                    string skip;
                    if (info.ShouldSkip(unrenamedTypeKey, Project.InheritMap, Project.Settings.KeepPublicApi,
                        Project.Settings.HidePrivateApi, Project.Settings.MarkedOnly,
                        Project.Settings.SkipGenerated, out skip))
                    {
                        Mapping.UpdateType(oldTypeKey, ObfuscationStatus.Skipped, skip);

                        // go through the list of resources, remove ones that would be renamed
                        for (int i = 0; i < resources.Count;)
                        {
                            Resource res = resources[i];
                            string resName = res.Name;
                            if (Path.GetFileNameWithoutExtension(resName) == fullName)
                            {
                                resources.RemoveAt(i);
                                Mapping.AddResource(resName, ObfuscationStatus.Skipped, skip);
                            }
                            else
                            {
                                i++;
                            }
                        }

                        continue;
                    }

                    if (namesInXaml.Contains(type.FullName))
                    {
                        Mapping.UpdateType(oldTypeKey, ObfuscationStatus.Skipped, "filtered by BAML");

                        // go through the list of resources, remove ones that would be renamed
                        for (int i = 0; i < resources.Count;)
                        {
                            Resource res = resources[i];
                            string resName = res.Name;
                            if (Path.GetFileNameWithoutExtension(resName) == fullName)
                            {
                                resources.RemoveAt(i);
                                Mapping.AddResource(resName, ObfuscationStatus.Skipped, "filtered by BAML");
                            }
                            else
                            {
                                i++;
                            }
                        }

                        continue;
                    }

                    string name;
                    string ns;
                    if (type.IsNested)
                    {
                        ns = "";
                        name = NameMaker.UniqueNestedTypeName(type.DeclaringType.NestedTypes.IndexOf(type));
                    }
                    else
                    {
                        if (Project.Settings.ReuseNames)
                        {
                            name = NameMaker.UniqueTypeName(typeIndex);
                            ns = NameMaker.UniqueNamespace(typeIndex);
                        }
                        else
                        {
                            name = NameMaker.UniqueName(_uniqueTypeNameIndex);
                            ns = NameMaker.UniqueNamespace(_uniqueTypeNameIndex);
                            _uniqueTypeNameIndex++;
                        }
                    }

                    if (type.GenericParameters.Count > 0)
                        name += '`' + type.GenericParameters.Count.ToString();

                    if (type.DeclaringType != null)
                        ns = ""; // Nested types do not have namespaces

                    TypeKey newTypeKey = new TypeKey(info.Name, ns, name);
                    typeIndex++;

                    FixResouceManager(resources, type, fullName, newTypeKey);

                    RenameType(info, type, oldTypeKey, newTypeKey, unrenamedTypeKey);
                }

                foreach (Resource res in resources)
                    Mapping.AddResource(res.Name, ObfuscationStatus.Skipped, "no clear new name");

                info.InvalidateCache();
            }
        }

        private void FixResouceManager(List<Resource> resources, TypeDefinition type, string fullName,
            TypeKey newTypeKey)
        {
            if (!type.IsResourcesType())
                return;

            // go through the list of renamed types and try to rename resources
            for (int i = 0; i < resources.Count;)
            {
                Resource res = resources[i];
                string resName = res.Name;

                if (Path.GetFileNameWithoutExtension(resName) == fullName)
                {
                    // If one of the type's methods return a ResourceManager and contains a string with the full type name,
                    // we replace the type string with the obfuscated one.
                    // This is for the Visual Studio generated resource designer code.
                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (method.ReturnType.FullName != "System.Resources.ResourceManager")
                            continue;

                        foreach (Instruction instruction in method.Body.Instructions)
                        {
                            if (instruction.OpCode == OpCodes.Ldstr && (string) instruction.Operand == fullName)
                                instruction.Operand = newTypeKey.Fullname;
                        }
                    }

                    // ReSharper disable once InvocationIsSkipped
                    Debug.Assert(fullName != null, "fullName != null");
                    // ReSharper disable once PossibleNullReferenceException
                    string suffix = resName.Substring(fullName.Length);
                    string newName = newTypeKey.Fullname + suffix;
                    res.Name = newName;
                    resources.RemoveAt(i);
                    Mapping.AddResource(resName, ObfuscationStatus.Renamed, newName);
                }
                else
                {
                    i++;
                }
            }
        }

        private HashSet<string> NamesInXaml(List<BamlDocument> xamlFiles)
        {
            var result = new HashSet<string>();
            if (xamlFiles.Count == 0)
                return result;

            foreach (var doc in xamlFiles)
            {
                foreach (BamlRecord child in doc)
                {
                    var classAttribute = child as TypeInfoRecord;
                    if (classAttribute == null)
                        continue;

                    result.Add(classAttribute.TypeFullName);
                }
            }

            return result;
        }

        private List<BamlDocument> GetXamlDocuments(AssemblyDefinition library, bool analyzeXaml)
        {
            var result = new List<BamlDocument>();
            if (!analyzeXaml)
            {
                return result;
            }

            foreach (Resource res in library.MainModule.Resources)
            {
                var embed = res as EmbeddedResource;
                if (embed == null)
                    continue;

                Stream s = embed.GetResourceStream();
                s.Position = 0;
                ResourceReader reader;
                try
                {
                    reader = new ResourceReader(s);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                foreach (DictionaryEntry entry in reader.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString()))
                {
                    if (entry.Key.ToString().EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                    {
                        Stream stream;
                        var value = entry.Value as Stream;
                        if (value != null)
                            stream = value;
                        else if (entry.Value is byte[])
                            stream = new MemoryStream((byte[]) entry.Value);
                        else
                            continue;

                        try
                        {
                            result.Add(BamlReader.ReadDocument(stream, CancellationToken.None));
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }
                }
            }

            return result;
        }

        private void RenameType(AssemblyInfo info, TypeDefinition type, TypeKey oldTypeKey, TypeKey newTypeKey,
            TypeKey unrenamedTypeKey)
        {
            LoggerService.Logger.LogDebug("Renaming type {0} to {1}", oldTypeKey, newTypeKey);
            
            // find references, rename them, then rename the type itself
            foreach (AssemblyInfo reference in info.ReferencedBy)
            {
                for (int i = 0; i < reference.UnrenamedTypeReferences.Count;)
                {
                    TypeReference refType = reference.UnrenamedTypeReferences[i];

                    // check whether the referencing module references this type...if so,
                    // rename the reference
                    if (oldTypeKey.Matches(refType))
                    {
                        LoggerService.Logger.LogDebug("Found reference to {0} in {1}, updating", oldTypeKey, reference.Name);
                        refType.GetElementType().Namespace = newTypeKey.Namespace;
                        refType.GetElementType().Name = newTypeKey.Name;

                        reference.UnrenamedTypeReferences.RemoveAt(i);

                        // since we removed one, continue without the increment
                        continue;
                    }

                    i++;
                }
            }

            type.Namespace = newTypeKey.Namespace;
            type.Name = newTypeKey.Name;
            Mapping.UpdateType(unrenamedTypeKey, ObfuscationStatus.Renamed,
                string.Format("[{0}]{1}", newTypeKey.Scope, type));
        }

        private Dictionary<ParamSig, NameGroup> GetSigNames(
            Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames, TypeKey typeKey)
        {
            Dictionary<ParamSig, NameGroup> sigNames;
            if (!baseSigNames.TryGetValue(typeKey, out sigNames))
            {
                sigNames = new Dictionary<ParamSig, NameGroup>();
                baseSigNames[typeKey] = sigNames;
            }

            return sigNames;
        }

        private NameGroup GetNameGroup(Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
            TypeKey typeKey, ParamSig sig)
        {
            return GetNameGroup(GetSigNames(baseSigNames, typeKey), sig);
        }

        private NameGroup GetNameGroup<TKeyType>(Dictionary<TKeyType, NameGroup> sigNames, TKeyType sig)
        {
            NameGroup nameGroup;
            if (!sigNames.TryGetValue(sig, out nameGroup))
            {
                nameGroup = new NameGroup();
                sigNames[sig] = nameGroup;
            }

            return nameGroup;
        }

        public void RenameProperties()
        {
            // do nothing if it was requested not to rename
            if (!Project.Settings.RenameProperties)
            {
                return;
            }

            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    TypeKey typeKey = new TypeKey(type);

                    int index = 0;
                    List<PropertyDefinition> propsToDrop = new List<PropertyDefinition>();
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (PropertyDefinition prop in type.Properties)
                    {
                        index = ProcessProperty(typeKey, prop, info, type, index, propsToDrop);
                    }

                    foreach (PropertyDefinition prop in propsToDrop)
                    {
                        PropertyKey propKey = new PropertyKey(typeKey, prop);
                        ObfuscatedThing m = Mapping.GetProperty(propKey);
                        m.Update(ObfuscationStatus.Renamed, "dropped");
                        type.Properties.Remove(prop);
                    }
                }
            }
        }

        private int ProcessProperty(TypeKey typeKey, PropertyDefinition prop, AssemblyInfo info, TypeDefinition type,
            int index,
            List<PropertyDefinition> propsToDrop)
        {
            PropertyKey propKey = new PropertyKey(typeKey, prop);
            ObfuscatedThing m = Mapping.GetProperty(propKey);

            string skip;
            // skip filtered props
            if (info.ShouldSkip(propKey, Project.InheritMap, Project.Settings.KeepPublicApi,
                Project.Settings.HidePrivateApi,
                Project.Settings.MarkedOnly, out skip))
            {
                LoggerService.Logger.LogDebug("Property {0} in type {1} skipped: {2}", prop.Name, typeKey, skip);
                m.Update(ObfuscationStatus.Skipped, skip);

                // make sure get/set get skipped too
                if (prop.GetMethod != null)
                {
                    ForceSkip(prop.GetMethod, "skip by property");
                }

                if (prop.SetMethod != null)
                {
                    ForceSkip(prop.SetMethod, "skip by property");
                }

                return index;
            }

            if (type.BaseType != null && type.BaseType.Name.EndsWith("Attribute") && prop.SetMethod != null &&
                (prop.SetMethod.Attributes & MethodAttributes.Public) != 0)
            {
                // do not rename properties of custom attribute types which have a public setter method
                LoggerService.Logger.LogDebug("Property {0} in attribute type {1} skipped: public setter of a custom attribute", prop.Name, typeKey);
                m.Update(ObfuscationStatus.Skipped, "public setter of a custom attribute");
                // no problem when the getter or setter methods are renamed by RenameMethods()
            }
            else
            {
                bool keepForMarkedOnly = Project.Settings.MarkedOnly &&
                    (prop.MarkedToRename() == true || prop.DeclaringType.MarkedToRename() == true);

                if (prop.CustomAttributes.Count > 0 || Project.Settings.KeepProperties || keepForMarkedOnly)
                {
                    // If a property has custom attributes we don't remove the property but rename it instead.
                    // Does the same if KeepProperties is set to true or marked-only should keep it.
                    var newName = NameMaker.UniqueName(Project.Settings.ReuseNames ? index++ : _uniqueMemberNameIndex++);
                    LoggerService.Logger.LogDebug("Renaming property {0} in type {1} to {2} (has {3} custom attributes, KeepProperties={4})",
                        prop.Name, typeKey, newName, prop.CustomAttributes.Count, Project.Settings.KeepProperties);
                    RenameProperty(info, propKey, prop, newName);
                }
                else
                {
                    // add to to collection for removal
                    LoggerService.Logger.LogDebug("Property {0} in type {1} will be dropped", prop.Name, typeKey);
                    propsToDrop.Add(prop);
                }
            }
            return index;
        }

        private void RenameProperty(AssemblyInfo info, PropertyKey propertyKey, PropertyDefinition property,
            string newName)
        {
            // find references, rename them, then rename the property itself
            foreach (AssemblyInfo reference in info.ReferencedBy)
            {
                for (int i = 0; i < reference.UnrenamedReferences.Count;)
                {
                    PropertyReference member = reference.UnrenamedReferences[i] as PropertyReference;
                    if (member != null)
                    {
                        if (propertyKey.Matches(member))
                        {
                            member.Name = newName;
                            reference.UnrenamedReferences.RemoveAt(i);

                            // since we removed one, continue without the increment
                            continue;
                        }
                    }

                    i++;
                }
            }

            property.Name = newName;
            Mapping.UpdateProperty(propertyKey, ObfuscationStatus.Renamed, newName);
        }

        public void RenameEvents()
        {
            // do nothing if it was requested not to rename
            if (!Project.Settings.RenameEvents)
            {
                return;
            }

            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    TypeKey typeKey = new TypeKey(type);
                    List<EventDefinition> evtsToDrop = new List<EventDefinition>();
                    foreach (EventDefinition evt in type.Events)
                    {
                        ProcessEvent(typeKey, evt, info, evtsToDrop);
                    }

                    foreach (EventDefinition evt in evtsToDrop)
                    {
                        EventKey evtKey = new EventKey(typeKey, evt);
                        ObfuscatedThing m = Mapping.GetEvent(evtKey);

                        m.Update(ObfuscationStatus.Renamed, "dropped");
                        type.Events.Remove(evt);
                    }
                }
            }
        }

        private void ProcessEvent(TypeKey typeKey, EventDefinition evt, AssemblyInfo info,
            List<EventDefinition> evtsToDrop)
        {
            EventKey evtKey = new EventKey(typeKey, evt);
            ObfuscatedThing m = Mapping.GetEvent(evtKey);

            string skip;
            // skip filtered events
            if (info.ShouldSkip(evtKey, Project.InheritMap, Project.Settings.KeepPublicApi,
                Project.Settings.HidePrivateApi,
                Project.Settings.MarkedOnly, out skip))
            {
                m.Update(ObfuscationStatus.Skipped, skip);

                // make sure add/remove get skipped too
                ForceSkip(evt.AddMethod, "skip by event");
                ForceSkip(evt.RemoveMethod, "skip by event");
                return;
            }

            // add to to collection for removal
            evtsToDrop.Add(evt);
        }

        private void ForceSkip(MethodDefinition method, string skip)
        {
            if (method == null)
                return;

            var mk = new MethodKey(method);
            var delete = Mapping.GetMethod(mk);
            if (delete != null)
            {
                delete.Status = ObfuscationStatus.Skipped;
                delete.StatusText = skip;
                // Do not physically remove methods here; mapping status is used by RenameMethods
            }
        }

        public void RenameMethods()
        {
            var baseSigNames = new Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>>();
            // Do not remove methods marked as Skipped here; rename phase will consult mapping statuses
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    TypeKey typeKey = new TypeKey(type);

                    Dictionary<ParamSig, NameGroup> sigNames = GetSigNames(baseSigNames, typeKey);

                    // first pass.  mark grouped virtual methods to be renamed, and mark some things
                    // to be skipped as neccessary
                    foreach (MethodDefinition method in type.Methods)
                    {
                        ProcessMethod(typeKey, method, info, baseSigNames);
                    }

                    // update name groups, so new names don't step on inherited ones
                    foreach (TypeKey baseType in Project.InheritMap.GetBaseTypes(typeKey))
                    {
                        Dictionary<ParamSig, NameGroup> baseNames = GetSigNames(baseSigNames, baseType);
                        foreach (KeyValuePair<ParamSig, NameGroup> pair in baseNames)
                        {
                            NameGroup nameGroup = GetNameGroup(sigNames, pair.Key);
                            nameGroup.AddAll(pair.Value);
                        }
                    }
                }

                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    TypeKey typeKey = new TypeKey(type);
                    Dictionary<ParamSig, NameGroup> sigNames = GetSigNames(baseSigNames, typeKey);

                    // second pass...marked virtuals and anything not skipped get renamed
                    foreach (MethodDefinition method in type.Methods)
                    {
                        MethodKey methodKey = new MethodKey(typeKey, method);
                        ObfuscatedThing m = Mapping.GetMethod(methodKey);

                        // if we already decided to skip it, leave it alone
                        if (m.Status == ObfuscationStatus.Skipped)
                        {
                            continue;
                        }

                        if (method.IsSpecialName)
                        {
                            switch (method.SemanticsAttributes)
                            {
                                case MethodSemanticsAttributes.Getter:
                                case MethodSemanticsAttributes.Setter:
                                    if (Project.Settings.RenameProperties)
                                    {
                                        RenameMethod(info, sigNames, methodKey, method);
                                        method.SemanticsAttributes = 0;
                                    }
                                    break;
                                case MethodSemanticsAttributes.AddOn:
                                case MethodSemanticsAttributes.RemoveOn:
                                    if (Project.Settings.RenameEvents)
                                    {
                                        RenameMethod(info, sigNames, methodKey, method);
                                        method.SemanticsAttributes = 0;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            RenameMethod(info, sigNames, methodKey, method);
                        }
                    }
                }
            }
        }

        private void ProcessMethod(TypeKey typeKey, MethodDefinition method, AssemblyInfo info,
            Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames)
        {
            MethodKey methodKey = new MethodKey(typeKey, method);
            ObfuscatedThing m = Mapping.GetMethod(methodKey);

            if (m.Status == ObfuscationStatus.Skipped)
            {
                // IMPORTANT: shortcut for event and property methods.
                return;
            }

            // skip filtered methods
            string skiprename;
            var toDo = info.ShouldSkip(methodKey, Project.InheritMap, Project.Settings.KeepPublicApi,
                Project.Settings.HidePrivateApi, Project.Settings.MarkedOnly, out skiprename);
            if (!toDo)
                skiprename = null;
            // update status for skipped non-virtual methods immediately...status for
            // skipped virtual methods gets updated in RenameVirtualMethod
            if (!method.IsVirtual)
            {
                if (skiprename != null)
                {
                    m.Update(ObfuscationStatus.Skipped, skiprename);
                }

                return;
            }

            // if we need to skip the method or we don't yet have a name planned for a method, rename it
            if ((skiprename != null && m.Status != ObfuscationStatus.Skipped) ||
                m.Status == ObfuscationStatus.Unknown)
            {
                RenameVirtualMethod(baseSigNames, methodKey, method, skiprename);
            }
        }

        private void RenameVirtualMethod(Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
            MethodKey methodKey, MethodDefinition method, string skipRename)
        {
            // if method is in a group, look for group key
            MethodGroup group = Project.InheritMap.GetMethodGroup(methodKey);
            if (group == null)
            {
                LoggerService.Logger.LogDebug("Method {0} has no group", methodKey);
                if (skipRename != null)
                {
                    Mapping.UpdateMethod(methodKey, ObfuscationStatus.Skipped, skipRename);
                }

                return;
            }

            LoggerService.Logger.LogDebug("Method {0} is in group with {1} methods, external: {2}", 
                methodKey, group.Methods.Count, group.External);

            string groupName = @group.Name;
            if (groupName == null)
            {
                // group is not yet named

                // counts are grouping according to signature
                ParamSig sig = new ParamSig(method);

                // get name groups for classes in the group
                NameGroup[] nameGroups = GetNameGroups(baseSigNames, @group.Methods, sig);

                if (@group.External)
                {
                    skipRename = "external base class or interface";
                    LoggerService.Logger.LogDebug("Group for method {0} is external, will be skipped", methodKey);
                }

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (skipRename != null)
                {
                    // for an external group, we can't rename.  just use the method
                    // name as group name
                    groupName = method.Name;
                    LoggerService.Logger.LogDebug("Using method name as group name: {0}", groupName);
                }
                else
                {
                    // for an internal group, get next unused name
                    groupName = NameGroup.GetNext(nameGroups);
                    LoggerService.Logger.LogDebug("Generated new name for method group: {0}", groupName);
                }

                @group.Name = groupName;

                // set up methods to be renamed
                foreach (MethodKey m in @group.Methods)
                    if (skipRename == null)
                        Mapping.UpdateMethod(m, ObfuscationStatus.WillRename, groupName);
                    else
                        Mapping.UpdateMethod(m, ObfuscationStatus.Skipped, skipRename);

                // make sure the classes' name groups are updated
                foreach (NameGroup t in nameGroups)
                {
                    t.Add(groupName);
                }
            }
            else if (skipRename != null)
            {
                // ReSharper disable once InvocationIsSkipped
                Debug.Assert(!@group.External,
                    "Group's external flag should have been handled when the group was created, " +
                    "and all methods in the group should already be marked skipped.");
                Mapping.UpdateMethod(methodKey, ObfuscationStatus.Skipped, skipRename);

                LoggerService.Logger.LogDebug("Group {0} is already named but method {1} needs to be skipped", 
                    groupName, methodKey);

                var message =
                    new StringBuilder(
                            "Inconsistent virtual method obfuscation state detected. Abort. Please review the following methods,")
                        .AppendLine();
                foreach (var item in @group.Methods)
                {
                    var state = Mapping.GetMethod(item);
                    message.AppendFormat("{0}->{1}:{2}", item, state.Status, state.StatusText).AppendLine();
                }

                throw new ObfuscarException(message.ToString());
            }
            else
            {
                LoggerService.Logger.LogDebug("Using existing name for method group: {0}", groupName);
                // ReSharper disable once RedundantAssignment
                ObfuscatedThing m = Mapping.GetMethod(methodKey);
                // ReSharper disable once InvocationIsSkipped
                Debug.Assert(m.Status == ObfuscationStatus.Skipped ||
                             ((m.Status == ObfuscationStatus.WillRename || m.Status == ObfuscationStatus.Renamed) &&
                              m.StatusText == groupName),
                    "If the method isn't skipped, and the group already has a name...method should have one too.");
            }
        }

        NameGroup[] GetNameGroups(Dictionary<TypeKey, Dictionary<ParamSig, NameGroup>> baseSigNames,
            IEnumerable<MethodKey> methodKeys, ParamSig sig)
        {
            // build unique set of classes in group
            HashSet<TypeKey> typeKeys = new HashSet<TypeKey>();
            foreach (MethodKey methodKey in methodKeys)
                typeKeys.Add(methodKey.TypeKey);

            HashSet<TypeKey> parentTypes = new HashSet<TypeKey>();
            foreach (TypeKey type in typeKeys)
                InheritMap.GetBaseTypes(Project, parentTypes, type.TypeDefinition);

            typeKeys.UnionWith(parentTypes);

            // build list of namegroups
            NameGroup[] nameGroups = new NameGroup[typeKeys.Count];

            int i = 0;
            foreach (TypeKey typeKey in typeKeys)
            {
                NameGroup nameGroup = GetNameGroup(baseSigNames, typeKey, sig);

                nameGroups[i++] = nameGroup;
            }

            return nameGroups;
        }

        string GetNewMethodName(Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey, MethodDefinition method)
        {
            ObfuscatedThing t = Mapping.GetMethod(methodKey);

            // if it already has a name, return it
            if (t.Status == ObfuscationStatus.Renamed ||
                t.Status == ObfuscationStatus.WillRename)
                return t.StatusText;

            // don't mess with methods we decided to skip
            if (t.Status == ObfuscationStatus.Skipped)
                return null;

            // got a new name for the method
            t.Status = ObfuscationStatus.WillRename;
            t.StatusText = GetNewName(sigNames, method);
            return t.StatusText;
        }

        private string GetNewName(Dictionary<ParamSig, NameGroup> sigNames, MethodDefinition method)
        {
            // counts are grouping according to signature
            ParamSig sig = new ParamSig(method);

            NameGroup nameGroup = GetNameGroup(sigNames, sig);

            string newName = nameGroup.GetNext();

            // make sure the name groups is updated
            nameGroup.Add(newName);
            return newName;
        }

        void RenameMethod(AssemblyInfo info, Dictionary<ParamSig, NameGroup> sigNames, MethodKey methodKey,
            MethodDefinition method)
        {
            string newName = GetNewMethodName(sigNames, methodKey, method);

            RenameMethod(info, methodKey, method, newName);
        }

        void RenameMethod(AssemblyInfo info, MethodKey methodKey, MethodDefinition method, string newName)
        {
            // find references, rename them, then rename the method itself
            var references = new List<AssemblyInfo>();
            references.AddRange(info.ReferencedBy);
            if (!references.Contains(info))
            {
                references.Add(info);
            }

            var generics = new List<GenericInstanceMethod>();

            foreach (AssemblyInfo reference in references)
            {
                for (int i = 0; i < reference.UnrenamedReferences.Count;)
                {
                    MethodReference member = reference.UnrenamedReferences[i] as MethodReference;
                    if (member != null)
                    {
                        if (methodKey.Matches(member))
                        {
                            var generic = member as GenericInstanceMethod;
                            if (generic == null)
                            {
                                member.Name = newName;
                            }
                            else
                            {
                                generics.Add(generic);
                            }

                            reference.UnrenamedReferences.RemoveAt(i);

                            // since we removed one, continue without the increment
                            continue;
                        }
                    }

                    i++;
                }
            }

            foreach (var generic in generics)
            {
                generic.ElementMethod.Name = newName;
            }

            method.Name = newName;

            Mapping.UpdateMethod(methodKey, ObfuscationStatus.Renamed, newName);
        }

        /// <summary>
        /// Encoded strings using an auto-generated class.
        /// </summary>
        internal void HideStrings()
        {
            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", "HideStrings() called\n");
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"Processing assembly {info.Definition.Name}\n");
                AssemblyDefinition library = info.Definition;
                StringSqueeze container = new StringSqueeze(library);

                // Look for all string load operations and replace them with calls to indiviual methods in our new class
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                    {
                        continue;
                    }

                    // FIXME: Figure out why this exists if it is never used.
                    // TypeKey typeKey = new TypeKey(type);
                    foreach (MethodDefinition method in type.Methods)
                    {
                        container.ProcessStrings(method, info, Project);
                    }
                }

                container.Squeeze();
            }
        }

        public void PostProcessing()
        {
            foreach (AssemblyInfo info in Project.AssemblyList)
            {
                info.Definition.CleanAttributes();
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                        continue;

                    type.CleanAttributes();

                    foreach (var field in type.Fields)
                    {
                        field.CleanAttributes();
                    }

                    foreach (var property in type.Properties)
                    {
                        property.CleanAttributes();
                    }

                    foreach (var eventItem in type.Events)
                    {
                        eventItem.CleanAttributes();
                    }

                    // first pass.  mark grouped virtual methods to be renamed, and mark some things
                    // to be skipped as neccessary
                    foreach (MethodDefinition method in type.Methods)
                    {
                        method.CleanAttributes();
                        // if (method.HasBody && Project.Settings.Optimize)
                        //     method.Body.Optimize();
                    }
                }

                if (!Project.Settings.SuppressIldasm)
                    continue;

                var module = info.Definition.MainModule;
                var attribute = new TypeReference("System.Runtime.CompilerServices", "SuppressIldasmAttribute", module,
                    module.TypeSystem.CoreLibrary).Resolve();
                if (attribute == null || attribute.Module != module.TypeSystem.CoreLibrary)
                    return;

                CustomAttribute found = module.CustomAttributes.FirstOrDefault(existing =>
                    existing.Constructor.DeclaringType.FullName == attribute.FullName);

                //Only add if it's not there already
                if (found != null)
                    continue;

                //Add one
                var add = module.ImportReference(attribute.GetConstructors().FirstOrDefault(item => !item.HasParameters));
                MethodReference constructor = module.ImportReference(add);
                CustomAttribute attr = new CustomAttribute(constructor);
                module.CustomAttributes.Add(attr);
                module.Assembly.CustomAttributes.Add(attr);
            }
        }

        private class StringSqueeze
        {
            /// <summary>
            /// Store the class to generate so we can generate it later on.
            /// </summary>
            private class StringSqueezeData
            {
                public TypeDefinition NewType { get; set; }

                public TypeDefinition StructType { get; set; }

                public FieldDefinition DataConstantField { get; set; }

                public FieldDefinition DataField { get; set; }

                public FieldDefinition StringArrayField { get; set; }

                public MethodDefinition StringGetterMethodDefinition { get; set; }

                public int NameIndex { get; set; }

                public int StringIndex { get; set; }

                // Array of bytes receiving the obfuscated strings in UTF8 format.
                public List<byte> DataBytes { get; } = new List<byte>();
            }

            private TypeReference SystemStringTypeReference { get; set; }

            private TypeReference SystemVoidTypeReference { get; set; }

            private TypeReference SystemByteTypeReference { get; set; }

            private TypeReference SystemIntTypeReference { get; set; }

            private TypeReference SystemObjectTypeReference { get; set; }

            private TypeReference SystemValueTypeTypeReference { get; set; }

            private MethodReference InitializeArrayMethod { get; set; }

            private TypeDefinition EncodingTypeDefinition { get; set; }

            private readonly List<StringSqueezeData> newDatas = new List<StringSqueezeData>();

            private StringSqueezeData mostRecentData;

            private readonly Dictionary<string, MethodDefinition> _methodByString =
                new Dictionary<string, MethodDefinition>();

            private readonly AssemblyDefinition _library;
            private bool _initialized;
            private bool _disabled;

            public StringSqueeze(AssemblyDefinition library)
            {
                _library = library;
            }

            private void Initialize()
            {
                if (_initialized)
                    return;

                _initialized = true;
                var library = _library;

                // We get the most used type references
                SystemVoidTypeReference = library.MainModule.TypeSystem.Void;
                SystemStringTypeReference = library.MainModule.TypeSystem.String;
                SystemByteTypeReference = library.MainModule.TypeSystem.Byte;
                SystemIntTypeReference = library.MainModule.TypeSystem.Int32;
                SystemObjectTypeReference = library.MainModule.TypeSystem.Object;
                SystemValueTypeTypeReference = new TypeReference("System", "ValueType", library.MainModule,
                    library.MainModule.TypeSystem.CoreLibrary);

                EncodingTypeDefinition = new TypeReference("System.Text", "Encoding", library.MainModule,
                    library.MainModule.TypeSystem.CoreLibrary).Resolve();
                if (EncodingTypeDefinition == null)
                {
                    _disabled = true;
                    return;
                }

                // IMPORTANT: this runtime helpers resolution must be after encoding resolution. 
                var runtimeHelpers = new TypeReference("System.Runtime.CompilerServices", "RuntimeHelpers",
                    library.MainModule, library.MainModule.TypeSystem.CoreLibrary).Resolve();
                InitializeArrayMethod = library.MainModule.ImportReference(
                    runtimeHelpers.Methods.FirstOrDefault(method => method.Name == "InitializeArray"));
            }

            private StringSqueezeData GetNewType()
            {
                StringSqueezeData data;

                if (mostRecentData != null && mostRecentData.StringIndex < 65_000 /* maximum number of methods per class allowed by the CLR */)
                {
                    data = mostRecentData;
                }
                else
                {
                    var library = _library;

                    var encodingGetUtf8Method =
                        library.MainModule.ImportReference(EncodingTypeDefinition.Methods.FirstOrDefault(method => method.Name == "get_UTF8"));
                    var encodingGetStringMethod = library.MainModule.ImportReference(EncodingTypeDefinition.Methods.FirstOrDefault(method =>
                        method.FullName ==
                        "System.String System.Text.Encoding::GetString(System.Byte[],System.Int32,System.Int32)"));

                    // New static class with a method for each unique string we substitute.
                    string guid = Guid.NewGuid().ToString().ToUpper();

                    TypeDefinition newType = new TypeDefinition(
                        "<PrivateImplementationDetails>{" + guid + "}",
                        Guid.NewGuid().ToString().ToUpper(),
                        TypeAttributes.BeforeFieldInit | TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
                        TypeAttributes.BeforeFieldInit, SystemObjectTypeReference);

                    // Add struct for constant byte array data
                    TypeDefinition structType = new TypeDefinition("1{" + guid + "}", "2",
                        TypeAttributes.ExplicitLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed |
                        TypeAttributes.NestedPrivate, SystemValueTypeTypeReference);
                    structType.PackingSize = 1;
                    newType.NestedTypes.Add(structType);

                    // Add field with constant string data
                    FieldDefinition dataConstantField = new FieldDefinition("3",
                        FieldAttributes.HasFieldRVA | FieldAttributes.Private | FieldAttributes.Static |
                        FieldAttributes.Assembly, structType);
                    newType.Fields.Add(dataConstantField);

                    // Add data field where constructor copies the data to
                    FieldDefinition dataField = new FieldDefinition("4",
                        FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
                        new ArrayType(SystemByteTypeReference));
                    newType.Fields.Add(dataField);

                    // Add string array of deobfuscated strings
                    FieldDefinition stringArrayField = new FieldDefinition("5",
                        FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
                        new ArrayType(SystemStringTypeReference));
                    newType.Fields.Add(stringArrayField);

                    // Add method to extract a string from the byte array. It is called by the indiviual string getter methods we add later to the class.
                    MethodDefinition stringGetterMethodDefinition = new MethodDefinition("6",
                        MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
                        SystemStringTypeReference);
                    stringGetterMethodDefinition.Parameters.Add(new ParameterDefinition(SystemIntTypeReference));
                    stringGetterMethodDefinition.Parameters.Add(new ParameterDefinition(SystemIntTypeReference));
                    stringGetterMethodDefinition.Parameters.Add(new ParameterDefinition(SystemIntTypeReference));
                    stringGetterMethodDefinition.Body.Variables.Add(new VariableDefinition(SystemStringTypeReference));
                    ILProcessor worker3 = stringGetterMethodDefinition.Body.GetILProcessor();

                    worker3.Emit(OpCodes.Call, encodingGetUtf8Method);
                    worker3.Emit(OpCodes.Ldsfld, dataField);
                    worker3.Emit(OpCodes.Ldarg_1);
                    worker3.Emit(OpCodes.Ldarg_2);
                    worker3.Emit(OpCodes.Callvirt, encodingGetStringMethod);
                    worker3.Emit(OpCodes.Stloc_0);

                    worker3.Emit(OpCodes.Ldsfld, stringArrayField);
                    worker3.Emit(OpCodes.Ldarg_0);
                    worker3.Emit(OpCodes.Ldloc_0);
                    worker3.Emit(OpCodes.Stelem_Ref);

                    worker3.Emit(OpCodes.Ldloc_0);
                    worker3.Emit(OpCodes.Ret);
                    newType.Methods.Add(stringGetterMethodDefinition);

                    data = new StringSqueezeData()
                            { NewType = newType
                            , DataConstantField = dataConstantField
                            , DataField = dataField
                            , StringArrayField = stringArrayField
                            , StringGetterMethodDefinition = stringGetterMethodDefinition
                            , StructType = structType
                            };

                    newDatas.Add(data);

                    mostRecentData = data;
                }

                return data;
            }

            public void Squeeze()
            {
                if (!_initialized)
                    return;

                if (_disabled)
                    return;

                foreach (StringSqueezeData data in newDatas)
                {
                    // Now that we know the total size of the byte array, we can update the struct size and store it in the constant field
                    data.StructType.ClassSize = data.DataBytes.Count;
                    for (int i = 0; i < data.DataBytes.Count; i++)
                        data.DataBytes[i] = (byte) (data.DataBytes[i] ^ (byte) i ^ 0xAA);
                    data.DataConstantField.InitialValue = data.DataBytes.ToArray();

                    // Add static constructor which initializes the dataField from the constant data field
                    MethodDefinition ctorMethodDefinition = new MethodDefinition(".cctor",
                        MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, SystemVoidTypeReference);
                    data.NewType.Methods.Add(ctorMethodDefinition);
                    ctorMethodDefinition.Body = new MethodBody(ctorMethodDefinition);
                    ctorMethodDefinition.Body.Variables.Add(new VariableDefinition(SystemIntTypeReference));

                    ILProcessor worker2 = ctorMethodDefinition.Body.GetILProcessor();
                    worker2.Emit(OpCodes.Ldc_I4, data.StringIndex);
                    worker2.Emit(OpCodes.Newarr, SystemStringTypeReference);
                    worker2.Emit(OpCodes.Stsfld, data.StringArrayField);


                    worker2.Emit(OpCodes.Ldc_I4, data.DataBytes.Count);
                    worker2.Emit(OpCodes.Newarr, SystemByteTypeReference);
                    worker2.Emit(OpCodes.Dup);
                    worker2.Emit(OpCodes.Ldtoken, data.DataConstantField);
                    worker2.Emit(OpCodes.Call, InitializeArrayMethod);
                    worker2.Emit(OpCodes.Stsfld, data.DataField);

                    worker2.Emit(OpCodes.Ldc_I4_0);
                    worker2.Emit(OpCodes.Stloc_0);

                    Instruction backlabel1 = worker2.Create(OpCodes.Br_S, ctorMethodDefinition.Body.Instructions[0]);
                    worker2.Append(backlabel1);
                    Instruction label2 = worker2.Create(OpCodes.Ldsfld, data.DataField);
                    worker2.Append(label2);
                    worker2.Emit(OpCodes.Ldloc_0);
                    worker2.Emit(OpCodes.Ldsfld, data.DataField);
                    worker2.Emit(OpCodes.Ldloc_0);
                    worker2.Emit(OpCodes.Ldelem_U1);
                    worker2.Emit(OpCodes.Ldloc_0);
                    worker2.Emit(OpCodes.Xor);
                    worker2.Emit(OpCodes.Ldc_I4, 0xAA);
                    worker2.Emit(OpCodes.Xor);
                    worker2.Emit(OpCodes.Conv_U1);
                    worker2.Emit(OpCodes.Stelem_I1);
                    worker2.Emit(OpCodes.Ldloc_0);
                    worker2.Emit(OpCodes.Ldc_I4_1);
                    worker2.Emit(OpCodes.Add);
                    worker2.Emit(OpCodes.Stloc_0);
                    backlabel1.Operand = worker2.Create(OpCodes.Ldloc_0);
                    worker2.Append((Instruction) backlabel1.Operand);
                    worker2.Emit(OpCodes.Ldsfld, data.DataField);
                    worker2.Emit(OpCodes.Ldlen);
                    worker2.Emit(OpCodes.Conv_I4);
                    worker2.Emit(OpCodes.Clt);
                    worker2.Emit(OpCodes.Brtrue, label2);
                    worker2.Emit(OpCodes.Ret);

                    _library.MainModule.Types.Add(data.NewType);
                }
            }

            public void ProcessStrings(MethodDefinition method, AssemblyInfo info, Project project)
            {
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"ProcessStrings: {method.FullName}\n");
                if (info.ShouldSkipStringHiding(new MethodKey(method), project.InheritMap,
                        project.Settings.HideStrings) || method.Body == null)
                {
                    System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"  Skipping: Body={method.Body != null}\n");
                    return;
                }

                Initialize();

                if (_disabled)
                    return;

                LoggerService.Logger.LogDebug("Processing strings in method {0}", method.FullName);
                
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"  Body.Variables.Count={method.Body.Variables.Count}, Instructions.Count={method.Body.Instructions.Count}\n");

                // Unroll short form instructions so they can be auto-fixed by Cecil
                // automatically when instructions are inserted/replaced
                // method.Body.SimplifyMacros();
                ILProcessor worker = method.Body.GetILProcessor();

                // DEBUG: Count ldstr instructions
                int ldstrCount = 0;
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode == OpCodes.Ldstr)
                    {
                        ldstrCount++;
                        System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"  Found ldstr operand type={instr.Operand?.GetType()?.Name ?? "null"}, value={instr.Operand}\n");
                    }
                }
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", $"  Instructions={method.Body.Instructions.Count}, ldstr={ldstrCount}\n");

                //
                // Make a dictionary of all instructions to replace and their replacement.
                //
                Dictionary <Instruction, LdStrInstructionReplacement> oldToNewStringInstructions = new Dictionary<Instruction, LdStrInstructionReplacement>();

                for (int index = 0; index < method.Body.Instructions.Count; index++)
                {
                    Instruction instruction = method.Body.Instructions[index];

                    if (instruction.OpCode == OpCodes.Ldstr)
                    {
                        string str = (string)instruction.Operand;
                        MethodDefinition individualStringMethodDefinition;
                        if (!_methodByString.TryGetValue(str, out individualStringMethodDefinition))
                        {
                            StringSqueezeData data = GetNewType();

                            string methodName = NameMaker.UniqueName(data.NameIndex++);

                            // Add the string to the data array
                            byte[] stringBytes = Encoding.UTF8.GetBytes(str);
                            int start = data.DataBytes.Count;
                            data.DataBytes.AddRange(stringBytes);
                            int count = data.DataBytes.Count - start;

                            LoggerService.Logger.LogDebug("Creating new hidden string method for '{0}' with name {1}", 
                                str.Length > 30 ? str.Substring(0, 30) + "..." : str, methodName);

                            // Add a method for this string to our new class
                            individualStringMethodDefinition = new MethodDefinition(
                                methodName,
                                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                                SystemStringTypeReference);
                            individualStringMethodDefinition.Body = new MethodBody(individualStringMethodDefinition);
                            ILProcessor worker4 = individualStringMethodDefinition.Body.GetILProcessor();

                            worker4.Emit(OpCodes.Ldsfld, data.StringArrayField);
                            worker4.Emit(OpCodes.Ldc_I4, data.StringIndex);
                            worker4.Emit(OpCodes.Ldelem_Ref);
                            worker4.Emit(OpCodes.Dup);
                            Instruction label20 = worker4.Create(
                                OpCodes.Brtrue_S,
                                data.StringGetterMethodDefinition.Body.Instructions[0]);
                            worker4.Append(label20);
                            worker4.Emit(OpCodes.Pop);
                            worker4.Emit(OpCodes.Ldc_I4, data.StringIndex);
                            worker4.Emit(OpCodes.Ldc_I4, start);
                            worker4.Emit(OpCodes.Ldc_I4, count);
                            worker4.Emit(OpCodes.Call, data.StringGetterMethodDefinition);

                            label20.Operand = worker4.Create(OpCodes.Ret);
                            worker4.Append((Instruction) label20.Operand);

                            data.NewType.Methods.Add(individualStringMethodDefinition);
                            _methodByString.Add(str, individualStringMethodDefinition);

                            mostRecentData.StringIndex++;
                        }

                        // Replace Ldstr with Call
                        Instruction newinstruction = worker.Create(OpCodes.Call, individualStringMethodDefinition);
                        oldToNewStringInstructions.Add(instruction, new LdStrInstructionReplacement(index, newinstruction));
                    }
                }

                int replacementCount = oldToNewStringInstructions.Count;
                if (replacementCount > 0)
                {
                    LoggerService.Logger.LogDebug("Replacing {0} string constants in method {1}", replacementCount, method.FullName);
                    worker.ReplaceAndFixReferences(method.Body, oldToNewStringInstructions);
                }

                // Optimize method back
                if (project.Settings.Optimize)
                {
                    try
                    {
                        // Debug: Check ThisParameter
                        if (method.HasThis)
                        {
                            try
                            {
                                var thisPar = method.Body.ThisParameter;
                                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                    $"  ThisParameter: {(thisPar == null ? "NULL" : thisPar.ParameterType?.FullName)}\n");
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                    $"  ThisParameter THREW: {ex.GetType().Name}: {ex.Message}\n");
                            }
                        }
                        
                        // Debug: Check for problematic operands before Optimize
                        System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                            $"  Pre-optimize: Method.HasThis={method.HasThis}, Parameters.Count={method.Parameters.Count}\n");
                        foreach (var instr in method.Body.Instructions)
                        {
                            // Log all ldarg-family instructions
                            if (instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg ||
                                instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_S ||
                                instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0 ||
                                instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_1 ||
                                instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_2 ||
                                instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_3)
                            {
                                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                    $"  Instr@{instr.Offset}: {instr.OpCode} operand={instr.Operand?.GetType()?.Name ?? "null"}\n");
                            }
                            
                            if (instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineBrTarget ||
                                instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.ShortInlineBrTarget)
                            {
                                if (instr.Operand != null && !(instr.Operand is Mono.Cecil.Cil.Instruction))
                                {
                                    System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                        $"  ERROR: {instr.OpCode} has operand of type {instr.Operand?.GetType()?.Name}, expected Instruction\n");
                                }
                            }
                            else if (instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.ShortInlineVar ||
                                     instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineVar)
                            {
                                if (!(instr.Operand is Mono.Cecil.Cil.VariableDefinition))
                                {
                                    System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                        $"  ERROR: {instr.OpCode} has operand of type {instr.Operand?.GetType()?.Name ?? "null"}, expected VariableDefinition\n");
                                }
                            }
                            else if (instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.ShortInlineArg ||
                                     instr.OpCode.OperandType == Mono.Cecil.Cil.OperandType.InlineArg)
                            {
                                if (!(instr.Operand is Mono.Cecil.ParameterDefinition))
                                {
                                    System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                        $"  ERROR: {instr.OpCode} has operand of type {instr.Operand?.GetType()?.Name ?? "null"}, expected ParameterDefinition\n");
                                }
                            }
                            // Check for null operands on Ldarg/Ldloc after SimplifyMacros style expansion
                            if (instr.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg && instr.Operand == null)
                            {
                                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                                    $"  ERROR: {instr.OpCode} at offset {instr.Offset} has NULL operand\n");
                            }
                        }
                        // method.Body.Optimize();
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                            $"  OPTIMIZE FAILED for {method.FullName}: {ex.GetType().Name}: {ex.Message}\n");
                        throw;
                    }
                }
            }
        }

        private static class MsNetSigner
        {
            [System.Runtime.InteropServices.DllImport("mscoree.dll",
                CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
            private static extern bool StrongNameSignatureGeneration(
                [ /*In, */System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
                string wzFilePath,
                [ /*In, */System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
                string wzKeyContainer,
                /*[In]*/byte[] pbKeyBlob,
                /*[In]*/uint cbKeyBlob,
                /*[In]*/IntPtr ppbSignatureBlob, // not supported, always pass 0.
                [System.Runtime.InteropServices.Out] out uint pcbSignatureBlob
            );

            public static void SignAssemblyFromKeyContainer(string assemblyname, string keyname)
            {
                uint dummy;
                if (!StrongNameSignatureGeneration(assemblyname, keyname, null, 0, IntPtr.Zero, out dummy))
                    throw new ObfuscarException("Unable to sign assembly using key from key container - " + keyname);
            }
        }
    }
}
