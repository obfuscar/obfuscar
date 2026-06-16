using Obfuscar;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ObfuscarTestNet
{
    [TestClass]
    public class MethodGroupingTest
    {
        [TestMethod]
        public void CheckGenericMethodGrouping()
        {
            // Arrange
            const string assemblyName = "AssemblyWithGenericsHierarchy";
            const string assemblyDll = $"{assemblyName}.dll";
            var outputPath = TestHelper.GenerateOutputPath();
            var xml = $"""
                       <?xml version='1.0'?>
                       <Obfuscator>
                           <Var name='InPath' value='{TestHelper.InputPath}' />
                           <Var name='OutPath' value='{outputPath}' />
                           <Var name="AbortOnInconsistentState" value="false" />
                           <Var name='KeepPublicApi' value='false' />
                           <Module file='$(InPath){Path.DirectorySeparatorChar}{assemblyDll}'/>
                       </Obfuscator>
                       """;

            // Act
            var output = TestHelper.BuildAndObfuscate(assemblyName, xml);
            var assembly = Assembly.LoadFrom(Path.GetFullPath(Path.Combine(outputPath, assemblyDll)));

            // Assert

            // all methods should have been skipped
            Assert.IsTrue(output.Mapping.ClassMap.SelectMany(t => t.Value.Methods.Values).All(t => t.Status == ObfuscationStatus.Skipped));
            Assert.IsTrue(assembly.DefinedTypes.SelectMany(t => t.DeclaredMethods).All(t => t.Name == "Method"));
        }
    }
}
