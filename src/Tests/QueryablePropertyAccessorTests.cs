using System;
using System.Collections.Generic;
using System.IO;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class QueryablePropertyAccessorTests
    {
        private const string AssemblyName = "AssemblyWithQueryablePropertyAccessor";
        private const string EntryTypeName = "TestClasses.QueryablePropertyAccessorEntryPoint";
        private const string CompilerGeneratedAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

        private static int InvokeExecute(string assemblyPath)
        {
            var assembly = Assembly.LoadFile(assemblyPath);
            var entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            var execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            return (int)execute.Invoke(null, Array.Empty<object>());
        }

        private static Obfuscator BuildAndObfuscate(bool skipGenerated = true, bool skipSpecialName = false)
        {
            string skipGeneratedValue = skipGenerated ? "true" : "false";
            string skipSpecialNameValue = skipSpecialName ? "true" : "false";

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='SkipGenerated' value='{3}' />" +
                @"<Var name='SkipSpecialName' value='{4}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar,
                skipGeneratedValue,
                skipSpecialNameValue);

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

        private static bool HasCompilerGeneratedAttribute(IEnumerable<CustomAttribute> attributes)
        {
            foreach (CustomAttribute attribute in attributes)
            {
                if (attribute.AttributeTypeName == CompilerGeneratedAttribute)
                    return true;
            }

            return false;
        }

        private static FieldDefinition FindCompilerGeneratedField(TypeDefinition typeDef)
        {
            foreach (FieldDefinition field in typeDef.Fields)
            {
                if (HasCompilerGeneratedAttribute(field.CustomAttributes))
                    return field;
            }

            Assert.Fail("Expected to find a compiler-generated field.");
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
        public void CheckQueryablePropertyAccessorAfterObfuscationReproducesIssue579WithoutSkipGenerated()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: false);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeExecute(outputPath));
            var argumentException = Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Contains("not a property accessor", argumentException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorRunsAfterObfuscationWithSkipGenerated()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: true);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int output = InvokeExecute(outputPath);
            Assert.Equal(0, output);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorCompilerGeneratedMembersAreSkippedWithSkipGenerated()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: true);
            ObfuscationMap map = item.Mapping;

            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            AssemblyDefinition input = AssemblyDefinition.ReadAssembly(inputPath);
            TypeDefinition modelType = input.MainModule.GetType("TestClasses.QueryablePropertyAccessorModel");
            PropertyDefinition property = FindPropertyByName(modelType, "Name");
            MethodDefinition getter = FindMethodByName(modelType, "get_Name");
            MethodDefinition setter = FindMethodByName(modelType, "set_Name");
            FieldDefinition backingField = FindCompilerGeneratedField(modelType);

            Assert.True(HasCompilerGeneratedAttribute(getter.CustomAttributes), "Expected getter to be compiler-generated.");
            Assert.True(HasCompilerGeneratedAttribute(setter.CustomAttributes), "Expected setter to be compiler-generated.");

            ObfuscatedThing propertyEntry = map.GetProperty(new PropertyKey(new TypeKey(modelType), property));
            ObfuscatedThing getterEntry = map.GetMethod(new MethodKey(getter));
            ObfuscatedThing setterEntry = map.GetMethod(new MethodKey(setter));
            ObfuscatedThing backingFieldEntry = map.GetField(new FieldKey(backingField));

            Assert.Equal(ObfuscationStatus.Skipped, propertyEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, getterEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, setterEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, backingFieldEntry.Status);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorSpecialNameMethodsAreSkippedWithoutSkipGenerated()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: false, skipSpecialName: true);
            ObfuscationMap map = item.Mapping;

            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");
            AssemblyDefinition input = AssemblyDefinition.ReadAssembly(inputPath);
            TypeDefinition modelType = input.MainModule.GetType("TestClasses.QueryablePropertyAccessorModel");
            MethodDefinition getter = FindMethodByName(modelType, "get_Name");
            MethodDefinition setter = FindMethodByName(modelType, "set_Name");

            Assert.True(getter.IsSpecialName, "Expected getter to be special-name.");
            Assert.True(setter.IsSpecialName, "Expected setter to be special-name.");
            Assert.True(HasCompilerGeneratedAttribute(getter.CustomAttributes), "Expected getter to be compiler-generated.");
            Assert.True(HasCompilerGeneratedAttribute(setter.CustomAttributes), "Expected setter to be compiler-generated.");

            ObfuscatedThing getterEntry = map.GetMethod(new MethodKey(getter));
            ObfuscatedThing setterEntry = map.GetMethod(new MethodKey(setter));

            Assert.Equal(ObfuscationStatus.Skipped, getterEntry.Status);
            Assert.Equal(ObfuscationStatus.Skipped, setterEntry.Status);
        }

        [Fact]
        public void CheckQueryablePropertyAccessorRunsAfterObfuscationWithSkipSpecialName()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: false, skipSpecialName: true);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int output = InvokeExecute(outputPath);
            Assert.Equal(0, output);
        }
    }
}
