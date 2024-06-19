using ObfuscarTestNet;
using ObfuscarTestNet.Input;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ObfuscarTest
{
    [TestClass]
    public class CollectionExpressionTest
    {
        [TestMethod]
        public void CheckCollectionExpression()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithCollectionArgument.dll'>" +
//                @"  <SkipType name=""*__ReadOnlyArray*"" skipProperties=""true"" skipMethods=""true"" skipFields=""true"" />" +
                @"</Module>" +
//                @"<Module file='$(InPath){2}AssemblyWithCollectionExpression.dll'>" +
                @"  <SkipType name=""*__ReadOnlyArray*"" skipProperties=""true"" skipMethods=""true"" skipFields=""true"" />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var output = TestHelper.BuildAndObfuscate(["AssemblyWithCollectionArgument", "AssemblyWithCollectionExpression"], xml);

            var assembly1 = Assembly.LoadFrom(Path.GetFullPath(Path.Combine(outputPath, "AssemblyWithCollectionArgument.dll")));
            var assembly2 = Assembly.LoadFrom(Path.GetFullPath(Path.Combine(outputPath, "AssemblyWithCollectionExpression.dll")));
            var obfuscatedClass = output.Mapping.ClassMap.First(c => c.Key.Name == "ObfuscarTestNet.Input.AssemblyWithCollectionExpression");
            var obfuscatedClassName = obfuscatedClass.Value.StatusText;
            obfuscatedClassName = obfuscatedClassName[(obfuscatedClassName.IndexOf(']') + 1)..];
            var type = assembly2.GetType(obfuscatedClassName) ?? throw new Exception($"Test class {obfuscatedClassName} not found");
            var instance = Activator.CreateInstance(type);

            var obfuscatedMethodName = obfuscatedClass.Value.Methods.First(m => m.Value.Name.EndsWith("Test[0]")).Value.StatusText;
            type.GetMethod(obfuscatedMethodName)!.Invoke(instance, []);
        }
    }
}
