using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class GenericDictionaryWeakReferenceTests
    {
        private Obfuscator BuildAndObfuscateAssembly()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericDictionaryWeakReference.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate("AssemblyWithGenericDictionaryWeakReference", string.Empty, xml);
        }

        [Fact]
        public void CheckGenericDictionaryOverloadRunsAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscateAssembly();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, "AssemblyWithGenericDictionaryWeakReference.dll");

            Assembly assembly = Assembly.LoadFile(outputPath);
            Type entryType = assembly.GetType("GenericDictionaryWeakReference.EntryPoint", throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);

            object result = execute.Invoke(null, Array.Empty<object>());
            Assert.Equal(1, (int)result);
        }
    }
}
