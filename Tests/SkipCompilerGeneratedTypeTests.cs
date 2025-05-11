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
/// 
#endregion

using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Obfuscar.Helpers;
using Xunit;

namespace ObfuscarTests
{
    public class SkipCompilerGeneratedTypeTests
    {
        [Fact]
        public void CheckSkipCompilerGeneratedTypesDefault()
        {
            // First, ensure the test assembly exists by copying it to the input folder
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test assembly to the input path
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Read the original assembly to identify compiler-generated types with attributes
            var originalAssembly = AssemblyDefinition.ReadAssembly(testDllPath);
            
            // Find types with compiler-generated attributes
            var typesWithCompilerGeneratedAttrs = new List<TypeDefinition>();
            foreach (var type in originalAssembly.MainModule.GetTypes())
            {
                if (type.HasCompilerGeneratedAttributes())
                {
                    typesWithCompilerGeneratedAttrs.Add(type);
                }
            }
            
            // Ensure we found at least one compiler-generated type
            Assert.True(typesWithCompilerGeneratedAttrs.Count > 0, 
                "Test failed - couldn't find any compiler-generated types in the test assembly");
            
            var output = TestHelper.OutputPath;
            // Configure obfuscation WITHOUT the SkipGenerated flag (default behavior)
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, output, Path.DirectorySeparatorChar);

            // Build the assembly and run obfuscation
            var obfuscator = TestHelper.Obfuscate(xml);
            
            // Test that settings are properly initialized to false by default
            Assert.False(obfuscator.Project.Settings.SkipGenerated,
                "SkipGenerated should be false by default");

            // Read the obfuscated assembly
            string obfuscatedPath = Path.Combine(output, "testmvc6.dll");
            var obfuscatedAssembly = AssemblyDefinition.ReadAssembly(obfuscatedPath);

            // With default behavior (SkipGenerated=false),
            // compiler-generated types should be obfuscated
            // Check if at least some compiler-generated types were obfuscated
            bool someWereObfuscated = false;
            foreach (var originalType in typesWithCompilerGeneratedAttrs)
            {
                // Try to find the original type in the obfuscated assembly
                var obfuscatedType = obfuscatedAssembly.MainModule.GetTypes()
                    .FirstOrDefault(t => t.FullName == originalType.FullName);
                
                // If the type is not found with the same name, it was obfuscated
                if (obfuscatedType == null)
                {
                    someWereObfuscated = true;
                    break;
                }
            }
            
            // By default, compiler-generated types should be processed/obfuscated like other types
            Assert.True(someWereObfuscated, 
                "With default settings, at least some compiler-generated types should be obfuscated");
        }
        
        [Fact]
        public void CheckSkipCompilerGeneratedTypesEnabled()
        {
            // First, ensure the test assembly exists by copying it to the input folder
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test assembly to the input path
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Read the original assembly to identify compiler-generated types with attributes
            var originalAssembly = AssemblyDefinition.ReadAssembly(testDllPath);
            
            // Find types with compiler-generated attributes
            var typesWithCompilerGeneratedAttrs = new List<TypeDefinition>();
            foreach (var type in originalAssembly.MainModule.GetTypes())
            {
                if (type.HasCompilerGeneratedAttributes())
                {
                    typesWithCompilerGeneratedAttrs.Add(type);
                }
            }
            
            // Ensure we found at least one compiler-generated type
            Assert.True(typesWithCompilerGeneratedAttrs.Count > 0, 
                "Test failed - couldn't find any compiler-generated types in the test assembly");
            
            var output = TestHelper.OutputPath;
            // Configure obfuscation to SKIP compiler-generated types
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='SkipGenerated' value='true' />" +
                @"<Module file='$(InPath){2}testmvc6.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, output, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.Obfuscate(xml);
            
            // Test that settings are properly initialized to true
            Assert.True(obfuscator.Project.Settings.SkipGenerated,
                "SkipGenerated should be true when configured");

            // Read the obfuscated assembly
            string obfuscatedPath = Path.Combine(output, "testmvc6.dll");
            var obfuscatedAssembly = AssemblyDefinition.ReadAssembly(obfuscatedPath);
            
            // Check that compiler-generated types were NOT obfuscated (i.e., they were skipped)
            bool allPreserved = true;
            foreach (var originalType in typesWithCompilerGeneratedAttrs)
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
            
            // With SkipGenerated=true, all compiler-generated types should be preserved
            Assert.True(allPreserved,
                "With SkipGenerated=true, compiler-generated types should be preserved with their attributes");
        }
    }
}
