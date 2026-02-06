using System;
using System.IO;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class QueryablePropertyAccessorTests
    {
        private const string AssemblyName = "AssemblyWithQueryablePropertyAccessor";
        private const string EntryTypeName = "TestClasses.QueryablePropertyAccessorEntryPoint";

        private static int InvokeExecute(string assemblyPath)
        {
            var assembly = Assembly.LoadFile(assemblyPath);
            var entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            var execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            return (int)execute.Invoke(null, Array.Empty<object>());
        }

        private static Obfuscator BuildAndObfuscate(bool skipQueryableProperty = false)
        {
            string skipPropertyXml = skipQueryableProperty
                ? @"<SkipProperty type='TestClasses.QueryablePropertyAccessorModel' name='Name' />"
                : string.Empty;

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='SkipGenerated' value='true' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll'>" +
                @"{3}" +
                @"</Module>" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar,
                skipPropertyXml);

            return TestHelper.BuildAndObfuscate(AssemblyName, string.Empty, xml);
        }

        private static MethodDefinition FindMethodByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
            {
                if (method.Name == name)
                    return method;
            }

            Assert.Fail(string.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        private static PropertyDefinition FindPropertyByName(TypeDefinition typeDef, string name)
        {
            foreach (PropertyDefinition property in typeDef.Properties)
            {
                if (property.Name == name)
                    return property;
            }

            Assert.Fail(string.Format("Expected to find property: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckQueryablePropertyAccessorBaseline()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");

            int output = InvokeExecute(inputPath);
            Assert.Equal(0, output);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorAfterObfuscationReproducesIssue579()
        {
            Obfuscator item = BuildAndObfuscate();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeExecute(outputPath));
            var argumentException = Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Contains("not a property accessor", argumentException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorRunsWhenPropertySkippedViaConfig()
        {
            Obfuscator item = BuildAndObfuscate(skipQueryableProperty: true);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int output = InvokeExecute(outputPath);
            Assert.Equal(0, output);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorIsSkippedViaConfig()
        {
            Obfuscator item = BuildAndObfuscate(skipQueryableProperty: true);
            ObfuscationMap map = item.Mapping;

            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            AssemblyDefinition input = AssemblyDefinition.ReadAssembly(inputPath);
            TypeDefinition modelType = input.MainModule.GetType("TestClasses.QueryablePropertyAccessorModel");
            PropertyDefinition property = FindPropertyByName(modelType, "Name");
            MethodDefinition getter = FindMethodByName(modelType, "get_Name");
            MethodDefinition setter = FindMethodByName(modelType, "set_Name");

            ObfuscatedThing propertyEntry = map.GetProperty(new PropertyKey(new TypeKey(modelType), property));
            ObfuscatedThing getterEntry = map.GetMethod(new MethodKey(getter));
            ObfuscatedThing setterEntry = map.GetMethod(new MethodKey(setter));

            Assert.Equal(ObfuscationStatus.Skipped, propertyEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, getterEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, setterEntry.Status);
        }
    }
}
