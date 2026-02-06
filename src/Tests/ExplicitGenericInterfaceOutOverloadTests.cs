using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class ExplicitGenericInterfaceOutOverloadTests
    {
        private static Obfuscator BuildAndObfuscateAssembly()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithExplicitGenericInterfaceOutOverloads.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate("AssemblyWithExplicitGenericInterfaceOutOverloads", string.Empty, xml);
        }

        [Fact]
        public void CheckExplicitGenericInterfaceOutOverloadsRunAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscateAssembly();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, "AssemblyWithExplicitGenericInterfaceOutOverloads.dll");

            Assembly assembly = Assembly.LoadFile(outputPath);
            Type entryType = assembly.GetType("Issue546.EntryPoint", throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);

            object result = execute.Invoke(null, Array.Empty<object>());
            Assert.Equal(4, (int)result);
        }

        [Fact]
        public void CheckExplicitGenericInterfaceOutOverloadsGetDistinctNames()
        {
            Obfuscator item = BuildAndObfuscateAssembly();
            ObfuscationMap map = item.Mapping;

            string inputPath = Path.Combine(TestHelper.InputPath, "AssemblyWithExplicitGenericInterfaceOutOverloads.dll");
            AssemblyDefinition input = AssemblyDefinition.ReadAssembly(inputPath);
            TypeDefinition implementationType = input.MainModule.GetType("Issue546.Implementation");

            MethodDefinition method3 = FindMethodByNameFragment(implementationType, "Method3");
            MethodDefinition method4 = FindMethodByNameFragment(implementationType, "Method4");

            ObfuscatedThing method3Entry = map.GetMethod(new MethodKey(method3));
            ObfuscatedThing method4Entry = map.GetMethod(new MethodKey(method4));

            Assert.Equal(ObfuscationStatus.Renamed, method3Entry.Status);
            Assert.Equal(ObfuscationStatus.Renamed, method4Entry.Status);
            Assert.NotEqual(method3Entry.StatusText, method4Entry.StatusText);
        }

        private static MethodDefinition FindMethodByNameFragment(TypeDefinition typeDef, string fragment)
        {
            foreach (MethodDefinition method in typeDef.Methods)
            {
                if (method.Name.Contains(fragment, StringComparison.Ordinal))
                    return method;
            }

            Assert.Fail($"Expected to find method containing: {fragment}");
            return null; // never here
        }
    }
}
