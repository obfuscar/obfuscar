using System;
using System.IO;
using Obfuscar;
using Obfuscar.Metadata.Mutable;
using Xunit;

namespace ObfuscarTests
{
    public class AssemblyResolverTests
    {
        [Fact]
        public void ResolverFindsAssemblyInSearchPath()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(names: ["AssemblyA", "AssemblyB"]);

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"  <Var name='InPath' value='{0}' />" +
                @"  <Var name='OutPath' value='{1}' />" +
                @"  <Module file='{2}' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.Combine(TestHelper.InputPath, "AssemblyB.dll"));

            var obfuscator = TestHelper.Obfuscate(xml);
            var resolved = obfuscator.Project.Cache.Resolve(
                new MutableAssemblyNameReference("AssemblyA", new Version(0, 0, 0, 0)));

            Assert.NotNull(resolved);
            Assert.Equal("AssemblyA", resolved.Name.Name);
        }
    }
}
