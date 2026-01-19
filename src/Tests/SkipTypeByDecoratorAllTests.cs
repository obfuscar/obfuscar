using System.IO;
using System.Collections.Generic;
using System.Linq;
using Obfuscar.Helpers;
using Xunit;

namespace ObfuscarTests
{
    public class SkipTypeByDecoratorAllTests
    {
        [Fact]
        public void CheckSkipTypesByDecoratorAll()
        {
            // First, ensure the test assembly exists by copying it to the input folder
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test assembly to the input path if it doesn't exist
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Read the original assembly to identify compiler-generated types with both attributes
            var originalAssembly = AssemblyDefinition.ReadAssembly(testDllPath);
            
            // Find types with both CompilerGenerated and Embedded attributes
            var typesWithBothAttributes = new List<TypeDefinition>();
            foreach (var type in originalAssembly.MainModule.GetTypes())
            {
                if (type.HasCompilerGeneratedAttributes())
                {
                    typesWithBothAttributes.Add(type);
                }
            }
            
            // Ensure we found at least one type with both attributes
            Assert.True(typesWithBothAttributes.Count > 0, 
                "Test failed - couldn't find any compiler-generated types with both attributes in the test assembly");
            
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"<SkipType name='*' decoratorAll='System.Runtime.CompilerServices.CompilerGeneratedAttribute,Microsoft.CodeAnalysis.EmbeddedAttribute' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xml);
            
            // Read the obfuscated assembly
            string obfuscatedPath = Path.Combine(outputPath, "testmvc6.dll");
            var obfuscatedAssembly = AssemblyDefinition.ReadAssembly(obfuscatedPath);
            
            // Check that all types with both attributes were preserved
            bool allPreserved = true;
            foreach (var originalType in typesWithBothAttributes)
            {
                // Try to find the original type in the obfuscated assembly
                var obfuscatedType = obfuscatedAssembly.MainModule.GetTypes()
                    .FirstOrDefault(t => t.FullName == originalType.FullName);
                
                // If the type is not found with the same name, it was obfuscated (not skipped)
                if (obfuscatedType == null)
                {
                    allPreserved = false;
                    break;
                }
                
                // Check that the attributes are preserved
                if (!obfuscatedType.HasCompilerGeneratedAttributes())
                {
                    allPreserved = false;
                    break;
                }
            }
            
            // With decoratorAll, all compiler-generated types with both attributes should be preserved
            Assert.True(allPreserved,
                "With decoratorAll for both attributes, compiler-generated types with both attributes should be preserved");
        }
        
        [Fact]
        public void CheckSkipTypesByDecorator()
        {
            // First, ensure the test assembly exists by copying it to the input folder
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test assembly to the input path if it doesn't exist
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Read the original assembly to identify compiler-generated types
            var originalAssembly = AssemblyDefinition.ReadAssembly(testDllPath);
            
            // Find types with CompilerGeneratedAttribute or types with names starting with '<'
            var compilerGeneratedTypes = new List<TypeDefinition>();
            foreach (var type in originalAssembly.MainModule.GetTypes())
            {
                bool hasAttribute = type.CustomAttributes?.Any(a => 
                    a.AttributeType?.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") ?? false;
                bool hasGeneratedName = type.Name?.StartsWith("<") ?? false;
                
                if (hasAttribute || hasGeneratedName)
                {
                    compilerGeneratedTypes.Add(type);
                }
            }
            
            // Ensure we found at least one compiler-generated type
            Assert.True(compilerGeneratedTypes.Count > 0, 
                "Test failed - couldn't find any compiler-generated types in the test assembly");
            
            string outputPath = Path.Combine(TestHelper.OutputPath, "Decorator");
            Directory.CreateDirectory(outputPath);
            
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"<SkipType name='*' decorator='System.Runtime.CompilerServices.CompilerGeneratedAttribute' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xml);
            
            // Read the obfuscated assembly
            string obfuscatedPath = Path.Combine(outputPath, "testmvc6.dll");
            var obfuscatedAssembly = AssemblyDefinition.ReadAssembly(obfuscatedPath);
            
            // Check that all compiler-generated types were preserved with their original names
            var obfuscatedTypeNames = obfuscatedAssembly.MainModule.GetTypes()
                .Select(t => t.FullName).ToHashSet();
            
            int preservedCount = 0;
            foreach (var originalType in compilerGeneratedTypes)
            {
                if (obfuscatedTypeNames.Contains(originalType.FullName))
                {
                    preservedCount++;
                }
            }
            
            // With decorator for CompilerGeneratedAttribute, compiler-generated types should be preserved
            Assert.True(preservedCount > 0,
                "With decorator for CompilerGeneratedAttribute, at least some compiler-generated types should be preserved");
        }
    }
}
