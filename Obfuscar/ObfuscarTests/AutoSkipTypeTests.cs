using Mono.Cecil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObfuscarTests
{
    [TestFixture]
    public class AutoSkipTypeTests
    {
        [Test]
        public void CheckHidePrivateApiFalse()
        {
            string xml = String.Format(
                             @"<?xml version='1.0'?>" +
                             @"<Obfuscator>" +
                             @"<Var name='InPath' value='{0}' />" +
                             @"<Var name='OutPath' value='{1}' />" +
                             @"<Var name='HidePrivateApi' value='false' />" +
                             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
                             @"</Module>" +
                             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

            TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);

            HashSet<string> typesToFind = new HashSet<string>();
            typesToFind.Add("TestClasses.ClassA");

            AssemblyHelper.CheckAssembly("AssemblyWithTypes", 2,
                delegate
                {
                    return true;
                },
                delegate(TypeDefinition typeDef)
                {
                    if (typesToFind.Contains(typeDef.ToString()))
                    {
                        typesToFind.Remove(typeDef.ToString());
                    }
                });
            Assert.IsTrue(typesToFind.Count == 1, "could not find ClassA, which should not have been obfuscated.");
        }

        [Test]
        public void CheckHidePrivateApiTrue()
        {
            string xml = String.Format(
                             @"<?xml version='1.0'?>" +
                             @"<Obfuscator>" +
                             @"<Var name='InPath' value='{0}' />" +
                             @"<Var name='OutPath' value='{1}' />" +
                             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
                             @"</Module>" +
                             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

            TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);

            HashSet<string> typesToFind = new HashSet<string>();
            typesToFind.Add("TestClasses.ClassA");

            AssemblyHelper.CheckAssembly("AssemblyWithTypes", 2,
                delegate
                {
                    return true;
                },
                delegate(TypeDefinition typeDef)
                {
                    if (typesToFind.Contains(typeDef.ToString()))
                    {
                        typesToFind.Remove(typeDef.ToString());
                    }
                });
            Assert.IsTrue(typesToFind.Count == 1, "could find ClassA, which should have been obfuscated.");
        }

        [Test]
        public void CheckKeepPublicApiFalse()
        {
            string xml = String.Format(
                             @"<?xml version='1.0'?>" +
                             @"<Obfuscator>" +
                             @"<Var name='InPath' value='{0}' />" +
                             @"<Var name='OutPath' value='{1}' />" +
                             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
                             @"</Module>" +
                             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

            TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);

            HashSet<string> typesToFind = new HashSet<string>();
            typesToFind.Add("TestClasses.ClassB");

            AssemblyHelper.CheckAssembly("AssemblyWithTypes", 2,
                delegate
                {
                    return true;
                },
                delegate(TypeDefinition typeDef)
                {
                    if (typesToFind.Contains(typeDef.ToString()))
                    {
                        typesToFind.Remove(typeDef.ToString());
                    }
                });
            Assert.IsTrue(typesToFind.Count == 1, "could find ClassB, which should have been obfuscated.");
        }

        [Test]
        public void CheckKeepPublicApiTrue()
        {
            string xml = String.Format(
                             @"<?xml version='1.0'?>" +
                             @"<Obfuscator>" +
                             @"<Var name='InPath' value='{0}' />" +
                             @"<Var name='OutPath' value='{1}' />" +
                             @"<Var name='KeepPublicApi' value='true' />" +
                             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
                             @"</Module>" +
                             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

            TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);

            HashSet<string> typesToFind = new HashSet<string>();
            typesToFind.Add("TestClasses.ClassB");

            AssemblyHelper.CheckAssembly("AssemblyWithTypes", 2,
                delegate
                {
                    return true;
                },
                delegate(TypeDefinition typeDef)
                {
                    if (typesToFind.Contains(typeDef.ToString()))
                    {
                        typesToFind.Remove(typeDef.ToString());
                    }
                });
            Assert.IsTrue(typesToFind.Count == 0, "could not find ClassB, which should not have been obfuscated.");
        }
    }
}
