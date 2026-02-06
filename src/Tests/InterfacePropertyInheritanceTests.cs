using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class InterfacePropertyInheritanceTests
    {
        private string output;

        private Obfuscator BuildAndObfuscateAssemblies()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithInterfacePropertyContract.dll' />" +
                @"<Module file='$(InPath){2}AssemblyWithInterfacePropertyImpl.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate(
                new[] { "AssemblyWithInterfacePropertyContract", "AssemblyWithInterfacePropertyImpl" },
                xml);
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
        public void CheckPublicInterfacePropertyMappingAcrossAssemblies()
        {
            Obfuscator item = BuildAndObfuscateAssemblies();
            ObfuscationMap map = item.Mapping;

            AssemblyDefinition contract = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithInterfacePropertyContract.dll"));
            AssemblyDefinition implementation = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithInterfacePropertyImpl.dll"));

            TypeDefinition interfaceType = contract.MainModule.GetType("TestLib.A");
            PropertyDefinition interfaceProperty = FindPropertyByName(interfaceType, "Property");
            MethodDefinition interfaceMethod = FindMethodByName(interfaceType, "Method");

            TypeDefinition implementationType = implementation.MainModule.GetType("TestClasses.C");
            PropertyDefinition implementationProperty = FindPropertyByName(implementationType, "Property");
            MethodDefinition implementationMethod = FindMethodByName(implementationType, "Method");

            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetProperty(new PropertyKey(new TypeKey(interfaceType), interfaceProperty)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(interfaceMethod)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetProperty(new PropertyKey(new TypeKey(implementationType), implementationProperty)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(implementationMethod)).Status);
        }

        [Fact]
        public void CheckPublicInterfacePropertyRunsAfterObfuscationAcrossAssemblies()
        {
            Obfuscator item = BuildAndObfuscateAssemblies();
            output = item.Project.Settings.OutPath;

            string implementationPath = Path.Combine(output, "AssemblyWithInterfacePropertyImpl.dll");
            Assembly implementationAssembly = Assembly.LoadFile(implementationPath);

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

                Type entryType = implementationAssembly.GetType("TestClasses.InterfacePropertyEntryPoint");
                MethodInfo executeMethod = entryType.GetMethod("ExecuteThroughPublicInterface", BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(executeMethod);

                object result = executeMethod.Invoke(null, Array.Empty<object>());
                Assert.Equal(0, (int)result);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(output, args.Name.Split(',')[0] + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        }
    }
}
