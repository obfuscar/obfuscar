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
using System.IO;
using System.Linq;
using Obfuscar.Helpers;
using Xunit;

namespace ObfuscarTests
{
    public class NetCoreTests
    {
        [Fact]
        public void CheckFolderDetection()
        {
            // This test verifies the GetNetCoreDirectories method correctly identifies .NET Core assemblies
            string outputPath = TestHelper.OutputPath;
            string testDllPath = Path.Combine(TestHelper.InputPath, "testmvc6.dll");
            
            // Copy the test DLL to input path if it doesn't exist
            if (!File.Exists(testDllPath))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "testmvc6.dll"),
                    testDllPath, true);
            }
            
            // Read the test assembly
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(testDllPath);
            
            // Create a custom attribute to simulate .NET Core
            var targetFrameworkAttribute = new CustomAttribute(
                assembly.MainModule.ImportReference(
                    typeof(System.Runtime.Versioning.TargetFrameworkAttribute).GetConstructor(
                        new Type[] { typeof(string) })));
            
            // Set the constructor argument to indicate .NET Core
            targetFrameworkAttribute.ConstructorArguments.Add(
                new CustomAttributeArgument(
                    assembly.MainModule.TypeSystem.String,
                    ".NETCoreApp,Version=6.0"
                ));
            
            // Add the attribute to the assembly
            assembly.CustomAttributes.Add(targetFrameworkAttribute);
            
            // Test the GetNetCoreDirectories method
            var netCoreDirs = assembly.GetNetCoreDirectories().ToList();
            
            // Verify the result - ensure at least one valid path exists
            Assert.NotEmpty(netCoreDirs);
            Assert.True(netCoreDirs.Any(dir => dir.Contains("Microsoft.NETCore.App.Ref") &&
                                               dir.Contains("6.0.") &&
                                               dir.Contains("net6.0")),
                        "No valid directory found in netCoreDirs.");
        }
    }
}
