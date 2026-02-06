using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class AnonymousTypePropertyTests
    {
        private const string AssemblyName = "AssemblyWithAnonymousTypeProperties";
        private const string EntryTypeName = "TestClasses.AnonymousTypePropertyEntryPoint";

        private static int InvokeExecute(string assemblyPath)
        {
            Assembly assembly = Assembly.LoadFile(assemblyPath);
            Type entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            return (int)execute.Invoke(null, Array.Empty<object>());
        }

        private static Obfuscator BuildAndObfuscate(bool skipGenerated)
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Var name='SkipGenerated' value='{3}' />" +
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar,
                skipGenerated ? "true" : "false");

            return TestHelper.BuildAndObfuscate(AssemblyName, string.Empty, xml);
        }

        [Fact]
        public void CheckAnonymousTypePropertyCountBaseline()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");

            int propertyCount = InvokeExecute(inputPath);
            Assert.Equal(2, propertyCount);
        }

        [Fact]
        public void CheckAnonymousTypePropertyCountAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: false);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int propertyCount = InvokeExecute(outputPath);
            Assert.Equal(0, propertyCount);
        }

        [Fact]
        public void CheckAnonymousTypePropertyCountAfterObfuscationWithSkipGenerated()
        {
            Obfuscator item = BuildAndObfuscate(skipGenerated: true);
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int propertyCount = InvokeExecute(outputPath);
            Assert.Equal(2, propertyCount);
        }
    }
}
