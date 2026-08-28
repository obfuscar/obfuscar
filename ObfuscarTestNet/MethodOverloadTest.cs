using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ObfuscarTestNet
{
    [TestClass]
    public class MethodOverloadTest
    {
        [TestMethod]
        [Ignore]
        public void CanCallOverloadedMethods()
        {
            // Arrange
            string library = "AssemblyWithGenericMethodOverloads";
            string caller = "AssemblyWithGenericMethodOverloadsCaller";
            string[] assemblyNames = [library, caller];
            string outputPath = TestHelper.GenerateOutputPath();
            var xml =
                $"""
                <?xml version='1.0'?>
                <Obfuscator>
                    <Var name='InPath' value='{TestHelper.InputPath}' />
                    <Var name='OutPath' value='{outputPath}' />
                    <Var name="AbortOnInconsistentState" value="false" />
                    <Var name='KeepPublicApi' value='false' />
                    <Module file='$(InPath){Path.DirectorySeparatorChar}{assemblyNames[0]}.dll'/>
                    <Module file='$(InPath){Path.DirectorySeparatorChar}{assemblyNames[1]}.dll'/>
                </Obfuscator>
                """;

            // Act
            TestHelper.BuildAndObfuscate(assemblyNames, xml);

            // Assert
            Assembly.LoadFrom(Path.GetFullPath(Path.Combine(outputPath, $"{library}.dll")));
            var callerAssembly = Assembly.LoadFrom(Path.GetFullPath(Path.Combine(outputPath, $"{caller}.dll")));
            var callerClass = callerAssembly.ExportedTypes.Single();
            var methods = callerClass.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                Assert.IsEmpty(method.GetParameters());
                try
                {
                    method.Invoke(null, null);
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException is MissingMethodException)
                    {
                        Assert.Fail("Caller can't call the library due to mismatch in obfuscation mappings.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected exception.");
                    }
                }
            }
        }
    }
}
