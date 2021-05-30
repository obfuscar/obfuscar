
using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTest
{
    public class SkipMethodWithDynamicParametersTest
    {
        [Fact]
        public void SkipMethodWithDynamicParametersTest1()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}SkipMethodWithDynamicParameterTest1.dll'>" +
                @"<SkipMethod type='SkipMethodWithDynamicParameterTest.Class1' name='Method1' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("SkipMethodWithDynamicParameterTest", "1", xml);

            string[] expected = new string[]
            {
                "Method1"
            };

            string[] notExpected = new string[]
            {
            };

            AssemblyHelper.CheckAssembly(Path.Combine(outputPath, "SkipMethodWithDynamicParameterTest1.dll"), 1, expected,
                notExpected,
                delegate (TypeDefinition typeDef) { return !typeDef.IsInterface; },
                CheckType);
        }

        void CheckType(TypeDefinition typeDef)
        {
            Assembly assm = Assembly.LoadFile(Path.GetFullPath(typeDef.Module.FileName));
            Type type = assm.GetType(typeDef.FullName);

            object obj = Activator.CreateInstance(type);

            object result = type.InvokeMember("Method1",
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, obj, new object[]{"dyn"});
            Assert.IsType<string>(result); // "Method1 returns a string.");

            Assert.Equal("Method1 result", result); // "Method1 is expected to return a specific string.");
        }
    }
}
