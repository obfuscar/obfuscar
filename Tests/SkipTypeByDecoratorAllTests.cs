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
        public void CompareSkipGeneratedWithDecoratorAll()
        {
            // First, ensure the test assembly exists by copying it to the input folder
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test assembly to the input path if it doesn't exist
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Run first test with SkipGenerated=true
            string outputPath1 = Path.Combine(TestHelper.OutputPath, "SkipGenerated");
            Directory.CreateDirectory(outputPath1);
            
            string xmlSkipGenerated = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SkipGenerated' value='true' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath1, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xmlSkipGenerated);

            // Run second test with decoratorAll
            string outputPath2 = Path.Combine(TestHelper.OutputPath, "DecoratorAll");
            Directory.CreateDirectory(outputPath2);
            
            string xmlDecoratorAll = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"<SkipType name='*' decoratorAll='System.Runtime.CompilerServices.CompilerGeneratedAttribute,Microsoft.CodeAnalysis.EmbeddedAttribute' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath2, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xmlDecoratorAll);
            
            // Compare the outputs of both approaches
            var assembly1 = AssemblyDefinition.ReadAssembly(Path.Combine(outputPath1, "testmvc6.dll"));
            var assembly2 = AssemblyDefinition.ReadAssembly(Path.Combine(outputPath2, "testmvc6.dll"));
            
            // Get type names from both assemblies
            var typeNames1 = assembly1.MainModule.GetTypes().Select(t => t.FullName).ToList();
            var typeNames2 = assembly2.MainModule.GetTypes().Select(t => t.FullName).ToList();
            
            // Both approaches should result in the same set of preserved type names
            Assert.Equal(typeNames1.Count, typeNames2.Count);
            
            // Check that both approaches preserved the same types
            foreach (var typeName in typeNames1)
            {
                Assert.Contains(typeName, typeNames2);
            }
        }
    }
}
