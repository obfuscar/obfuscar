using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class LambdaCapturedReferenceTests
    {
        private const string AssemblyName = "AssemblyWithLambdaCapturedReference";
        private const string EntryTypeName = "TestClasses.LambdaCapturedReferenceEntryPoint";

        private static int InvokeExecute(string assemblyPath)
        {
            Assembly assembly = Assembly.LoadFile(assemblyPath);
            Type entryType = assembly.GetType(EntryTypeName, throwOnError: true);
            MethodInfo execute = entryType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            return (int)execute.Invoke(null, Array.Empty<object>());
        }

        private static Obfuscator BuildAndObfuscate()
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
                @"<Module file='$(InPath){2}" + AssemblyName + @".dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                TestHelper.OutputPath,
                Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate(AssemblyName, string.Empty, xml);
        }

        [Fact]
        public void CheckLambdaCapturedReferenceBaseline()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName);
            string inputPath = Path.Combine(TestHelper.InputPath, AssemblyName + ".dll");

            int output = InvokeExecute(inputPath);
            Assert.Equal(15, output);
        }

        [Fact]
        public void CheckLambdaCapturedReferenceAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscate();
            string outputPath = Path.Combine(item.Project.Settings.OutPath, AssemblyName + ".dll");

            int output = InvokeExecute(outputPath);
            Assert.Equal(15, output);
        }
    }
}
