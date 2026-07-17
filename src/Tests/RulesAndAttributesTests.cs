using System.Linq;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class RulesAndAttributesTests
    {
        private const string AssemblyName = "AssemblyWithRules";

        [Fact]
        public void CheckSuppressIldasmAdded()
        {
            // Verify SuppressIldasmAttribute is added to the output module
            // when SuppressIldasm is not explicitly disabled.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);

            string outPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");
            var outAssm = AssemblyDefinition.ReadAssembly(outPath);

            bool hasSuppressIldasm = outAssm.MainModule.CustomAttributes
                .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.SuppressIldasmAttribute");

            Assert.True(hasSuppressIldasm,
                "SuppressIldasmAttribute should be present in the output module when SuppressIldasm is not disabled.");
        }

        [Fact]
        public void CheckObfuscationAttributesCleaned()
        {
            // Verify that [Obfuscation] and [Obfuscate] attributes are removed from output.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);

            string outPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");
            var outAssm = AssemblyDefinition.ReadAssembly(outPath);

            // Check no types have ObfuscationAttribute or ObfuscateAttribute
            var obfuscationAttrNames = new[]
            {
                "System.Reflection.ObfuscationAttribute",
                "Obfuscar.ObfuscateAttribute"
            };

            foreach (var type in outAssm.MainModule.Types)
            {
                foreach (var attr in type.CustomAttributes)
                {
                    Assert.False(obfuscationAttrNames.Contains(attr.AttributeType.FullName),
                        $"Type '{type.FullName}' still has {attr.AttributeType.FullName} in output.");
                }

                foreach (var method in type.Methods)
                {
                    foreach (var attr in method.CustomAttributes)
                    {
                        Assert.False(obfuscationAttrNames.Contains(attr.AttributeType.FullName),
                            $"Method '{method.FullName}' still has {attr.AttributeType.FullName} in output.");
                    }
                }

                foreach (var field in type.Fields)
                {
                    foreach (var attr in field.CustomAttributes)
                    {
                        Assert.False(obfuscationAttrNames.Contains(attr.AttributeType.FullName),
                            $"Field '{field.FullName}' still has {attr.AttributeType.FullName} in output.");
                    }
                }
            }
        }

        [Fact]
        public void CheckForceMethodOverridesHidePrivateApi()
        {
            // Verify that ForceMethod renames a private method that would
            // otherwise be skipped by HidePrivateApi.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll'>" +
                @"  <ForceMethod name=""PrivateMethod"" type=""TestClasses.ForceMethodPrivateTarget"" />" +
                @"  <ForceMethod name=""InternalMethod"" type=""TestClasses.ForceMethodPrivateTarget"" />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);
            ObfuscationMap map = item.Mapping;

            string assmName = AssemblyName + ".dll";
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            var type = inAssmDef.MainModule.GetType("TestClasses.ForceMethodPrivateTarget");
            var privateMethod = FindByName(type, "PrivateMethod");
            var internalMethod = FindByName(type, "InternalMethod");

            var privateEntry = map.GetMethod(new MethodKey(privateMethod));
            var internalEntry = map.GetMethod(new MethodKey(internalMethod));

            Assert.True(
                privateEntry.Status == ObfuscationStatus.Renamed ||
                privateEntry.Status == ObfuscationStatus.WillRename,
                $"ForceMethod should rename 'PrivateMethod' hidden by HidePrivateApi, " +
                $"but got status: {privateEntry.Status} ({privateEntry.StatusText})");

            Assert.True(
                internalEntry.Status == ObfuscationStatus.Renamed ||
                internalEntry.Status == ObfuscationStatus.WillRename,
                $"ForceMethod should rename 'InternalMethod' hidden by HidePrivateApi, " +
                $"but got status: {internalEntry.Status} ({internalEntry.StatusText})");
        }

        [Fact]
        public void CheckSkipSpecialNamePreservesGetterSetter()
        {
            // Verify that SkipSpecialName=true prevents property getter/setter
            // and event add/remove methods from being renamed.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='RenameProperties' value='true' />" +
                @"<Var name='RenameEvents' value='true' />" +
                @"<Var name='SkipSpecialName' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);
            ObfuscationMap map = item.Mapping;

            string assmName = AssemblyName + ".dll";
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));
            var type = inAssmDef.MainModule.GetType("TestClasses.SpecialNameTarget");

            // Property getters/setters
            var getPropA = FindByName(type, "get_PropA");
            var setPropA = FindByName(type, "set_PropA");
            var getPropB = FindByName(type, "get_PropB");
            var setPropB = FindByName(type, "set_PropB");

            // Event add/remove
            var addEvent = FindByName(type, "add_MyEvent");
            var removeEvent = FindByName(type, "remove_MyEvent");

            var entries = new (MethodDefinition method, string label)[]
            {
                (getPropA, "get_PropA"),
                (setPropA, "set_PropA"),
                (getPropB, "get_PropB"),
                (setPropB, "set_PropB"),
                (addEvent, "add_MyEvent"),
                (removeEvent, "remove_MyEvent"),
            };

            foreach (var (method, label) in entries)
            {
                var entry = map.GetMethod(new MethodKey(method));
                Assert.True(
                    entry.Status == ObfuscationStatus.Skipped,
                    $"'{label}' should be skipped when SkipSpecialName=true, " +
                    $"but got status: {entry.Status} ({entry.StatusText})");
            }
        }

        [Fact]
        public void CheckMappingPopulated()
        {
            // Verify obfuscation map is populated with type/method entries.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);

            ObfuscationMap map = item.Mapping;
            Assert.NotNull(map);

            // The map should contain at least one renamed type (RenamedByAttribute has [Obfuscation(Exclude=false)])
            bool hasRenamedEntry = false;
            foreach (var kvp in map.ClassMap)
            {
                if (kvp.Value.Status == ObfuscationStatus.Renamed ||
                    kvp.Value.Status == ObfuscationStatus.WillRename)
                {
                    hasRenamedEntry = true;
                    break;
                }
            }
            Assert.True(hasRenamedEntry,
                "Mapping should contain at least one renamed entry.");
        }

        [Fact]
        public void CheckXmlMappingSetting()
        {
            // Verify XmlMapping setting is parsed correctly from config.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='XmlMapping' value='true' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                AssemblyName, string.Empty, xml, useNetFramework: false);

            Assert.True(item.Project.Settings.XmlMapping,
                "XmlMapping setting should be true when configured.");
        }

        private static MethodDefinition FindByName(TypeDefinition typeDef, string name)
        {
            foreach (var method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.Fail($"Expected to find method: {name}");
            return null;
        }
    }
}