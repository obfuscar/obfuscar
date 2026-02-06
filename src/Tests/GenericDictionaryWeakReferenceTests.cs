using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class GenericDictionaryWeakReferenceTests
    {
        private Obfuscator BuildAndObfuscateAssembly(bool hideStrings)
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='{3}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericDictionaryWeakReference.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar, hideStrings ? "true" : "false");

            return TestHelper.BuildAndObfuscate("AssemblyWithGenericDictionaryWeakReference", string.Empty, xml);
        }

        [Fact]
        public void CheckGenericDictionaryOverloadRunsAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscateAssembly(hideStrings: false);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, "AssemblyWithGenericDictionaryWeakReference.dll");

            Assembly assembly = Assembly.LoadFile(outputPath);
            Type entryType = assembly.GetType("GenericDictionaryWeakReference.EntryPoint", throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);

            object result = execute.Invoke(null, Array.Empty<object>());
            Assert.Equal(1, (int)result);
        }

        [Fact]
        public void CheckGenericDictionaryOverloadRunsAfterObfuscationWithHideStrings()
        {
            Obfuscator item = BuildAndObfuscateAssembly(hideStrings: true);
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
