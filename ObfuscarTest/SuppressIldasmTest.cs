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

using System.IO;
using System.Linq;
using Mono.Cecil;
using Obfuscar;
using Xunit;
using System.Runtime.CompilerServices;

namespace ObfuscarTest
{
    public class SuppressIldasmTests
    {
        Obfuscator BuildAndObfuscateAssemblies(string name)
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='SuppressIldasm' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar, name);

            return TestHelper.BuildAndObfuscate(name, string.Empty, xml);
        }

        [Fact]
        public void CheckSuppressIldasm()
        {
            Obfuscator item = BuildAndObfuscateAssemblies("AssemblyWithInterfaces");
            ObfuscationMap map = item.Mapping;

            string assmName = "AssemblyWithInterfaces.dll";

            // We do not expect input assembly to have special attribute
            using (AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName)))
            {
                CustomAttribute found = inAssmDef.CustomAttributes.FirstOrDefault(existing =>
                    existing.Constructor.DeclaringType.Name == nameof(SuppressIldasmAttribute));
                Assert.Null(found);

            }

            // the output assembly must have specific attribute
            using (AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                    Path.Combine(item.Project.Settings.OutPath, assmName)))
            {
                CustomAttribute found = outAssmDef.CustomAttributes.FirstOrDefault(existing =>
                    existing.Constructor.DeclaringType.Name == nameof(SuppressIldasmAttribute));
                Assert.NotNull(found);
            }
        }
    }
}
