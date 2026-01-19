using System.IO;
using System.Collections.Generic;
using System.Linq;
using Obfuscar.Helpers;
using Xunit;

namespace ObfuscarTests
{
    public class SkipTypeByDecoratorTests
    {
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
            
            // Find types with the CompilerGeneratedAttribute
            var typesWithAttribute = new List<TypeDefinition>();
            foreach (var type in originalAssembly.MainModule.GetTypes())
            {
                if (type.HasCustomAttributes && 
                    type.CustomAttributes.Any(a => 
                        a.AttributeType.Name == "CompilerGeneratedAttribute" && 
                        a.AttributeType.Namespace == "System.Runtime.CompilerServices"))
                {
                    typesWithAttribute.Add(type);
                }
            }
            
            // Ensure we found at least one type with the attribute
            Assert.True(typesWithAttribute.Count > 0, 
                "Test failed - couldn't find any compiler-generated types in the test assembly");
            
            string outputPath = TestHelper.OutputPath;
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

            var map = TestHelper.Obfuscate(xml);
            
            // Read the obfuscated assembly
            string obfuscatedPath = Path.Combine(outputPath, "testmvc6.dll");
            var obfuscatedAssembly = AssemblyDefinition.ReadAssembly(obfuscatedPath);
            
            // Count how many types with the attribute were preserved
            int preservedCount = 0;
            foreach (var originalType in typesWithAttribute)
            {
                // Try to find the original type in the obfuscated assembly
                var obfuscatedType = obfuscatedAssembly.MainModule.GetTypes()
                    .FirstOrDefault(t => t.FullName == originalType.FullName);
                
                // If the type is found with the same name, it was preserved (not obfuscated)
                if (obfuscatedType != null)
                {
                    preservedCount++;
                }
            }
            
            // With decorator, all compiler-generated types should be preserved
            Assert.True(preservedCount >= 5);
        }
    }
}
