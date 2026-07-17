using Obfuscar;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ObfuscarTests
{
    public class EnumerableParameterIsSetToNullTests
    {
        [Fact]
        public async Task CheckEnumerableParameterIsNotSetToNull()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}EnumerableParameterIsSetToNullTest.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "EnumerableParameterIsSetToNullTest",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string outputAssemblyPath = Path.Combine(outputPath, "EnumerableParameterIsSetToNullTest.dll");
            Assembly assembly = Assembly.LoadFile(outputAssemblyPath);

            Type derivedType = assembly.GetType("EnumerableParameterIsSetToNullTest.EnumerableParameterIsSetToNullExample_ChildClass", throwOnError: true);

            var instance = Activator.CreateInstance(derivedType);

            MethodInfo method = derivedType.GetMethod("PublicMethodAsync");

            bool? result = null;

            var exception = await Record.ExceptionAsync(async () =>
            {
                result = await (Task<bool>)method.Invoke(instance, new object[] { });
            });

            Assert.Null(exception);
            Assert.True(result);
        }
    }
}
