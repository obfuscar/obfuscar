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

        [Fact]
        public void ResolverFallsBackToRuntimeAssembly()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(names: ["AssemblyA"]);

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"  <Var name='InPath' value='{0}' />" +
                @"  <Var name='OutPath' value='{1}' />" +
                @"  <Module file='{2}' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.Combine(TestHelper.InputPath, "AssemblyA.dll"));

            var obfuscator = Obfuscator.CreateFromXml(xml);
            var resolved = obfuscator.Project.Cache.Resolve(
                new MutableAssemblyNameReference("System.Runtime", new Version(0, 0, 0, 0)));

            Assert.NotNull(resolved);

            var runtimePath = typeof(object).Assembly.Location;
            if (!string.IsNullOrEmpty(runtimePath))
                Assert.Equal(runtimePath, resolved.Modules[0].FileName);
        }

        [Fact]
        public void ResolverPrefersFirstSearchPath()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(names: ["AssemblyA", "AssemblyB"]);

            var searchRoot = TestHelper.OutputPath;
            var firstPath = Path.Combine(searchRoot, "net8.0");
            var secondPath = Path.Combine(searchRoot, "netstandard2.0");
            Directory.CreateDirectory(firstPath);
            Directory.CreateDirectory(secondPath);

            var assemblyA = Path.Combine(TestHelper.InputPath, "AssemblyA.dll");
            File.Copy(assemblyA, Path.Combine(secondPath, "AssemblyA.dll"), true);
            File.Copy(assemblyA, Path.Combine(firstPath, "AssemblyA.dll"), true);

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"  <Var name='InPath' value='{0}' />" +
                @"  <Var name='OutPath' value='{1}' />" +
                @"  <AssemblySearchPath path='{2}' />" +
                @"  <AssemblySearchPath path='{3}' />" +
                @"  <Module file='{4}' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                firstPath,
                secondPath,
                Path.Combine(TestHelper.InputPath, "AssemblyB.dll"));

            var obfuscator = Obfuscator.CreateFromXml(xml);
            var resolved = obfuscator.Project.Cache.Resolve(
                new MutableAssemblyNameReference("AssemblyA", new Version(0, 0, 0, 0)));

            Assert.NotNull(resolved);
            Assert.Equal(Path.Combine(firstPath, "AssemblyA.dll"), resolved.Modules[0].FileName);
        }

        [Fact]
        public void ResolverUsesNetCorePackReferencesWhenPresent()
        {
            if (!TryGetWritablePacksRoot(out var packsRoot))
                return;

            const string targetVersion = "99.9";
            var versionDir = Path.Combine(packsRoot, "Microsoft.NETCore.App.Ref", "99.9.0");
            var refDir = Path.Combine(versionDir, "ref", "net99.9");
            Directory.CreateDirectory(refDir);

            try
            {
                TestHelper.CleanInput();

                File.WriteAllText(Path.Combine(TestHelper.InputPath, "AssemblyNetCore.cs"),
                    "[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(\".NETCoreApp,Version=v99.9\", FrameworkDisplayName = \".NET 99.9\")]" +
                    "namespace AssemblyNetCore { public class Entry { } }");
                TestHelper.BuildAssembly("AssemblyNetCore", useNetFramework: false);

                File.WriteAllText(Path.Combine(TestHelper.InputPath, "PackLib.cs"),
                    "namespace PackLib { public class Entry { } }");
                TestHelper.BuildAssembly("PackLib", useNetFramework: false);

                var packLibSource = Path.Combine(TestHelper.InputPath, "PackLib.dll");
                var packLibDest = Path.Combine(refDir, "PackLib.dll");
                File.Copy(packLibSource, packLibDest, true);
                File.Delete(packLibSource);

                string xml = string.Format(
                    @"<?xml version='1.0'?>" +
                    @"<Obfuscator>" +
                    @"  <Var name='InPath' value='{0}' />" +
                    @"  <Var name='OutPath' value='{1}' />" +
                    @"  <Module file='{2}' />" +
                    @"</Obfuscator>",
                    TestHelper.InputPath,
                    TestHelper.OutputPath,
                    Path.Combine(TestHelper.InputPath, "AssemblyNetCore.dll"));

                var obfuscator = Obfuscator.CreateFromXml(xml);
                var resolved = obfuscator.Project.Cache.Resolve(
                    new MutableAssemblyNameReference("PackLib", new Version(0, 0, 0, 0)));

                Assert.NotNull(resolved);
                Assert.Equal(packLibDest, resolved.Modules[0].FileName);
            }
            finally
            {
                try
                {
                    Directory.Delete(versionDir, true);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }

        private static bool TryGetWritablePacksRoot(out string packsRoot)
        {
            packsRoot = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "packs")
                : "/usr/local/share/dotnet/packs";

            try
            {
                Directory.CreateDirectory(packsRoot);
                return true;
            }
            catch
            {
                packsRoot = null;
                return false;
            }
        }
    }
}
