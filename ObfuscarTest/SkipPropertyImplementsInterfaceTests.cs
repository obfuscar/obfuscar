using System.IO;
using Mono.Cecil;
using Obfuscar;
using Xunit;
using FluentAssertions.Execution;
using ObfuscarTest.FluentAssertions.Mono.Cecil;

namespace ObfuscarTest
{
    public class SkipPropertyImplementsInterfaceTests
    {
        private string CreateTestConfiguration(string csFileName) =>
            string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar, csFileName);

        Obfuscator BuildAndObfuscateAssemblies(string csFileName)
        {
            var xml = CreateTestConfiguration(csFileName);
            return TestHelper.BuildAndObfuscate(csFileName, string.Empty, xml);
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "PublicProperty")]
        public void Should_skip_public_property_when_INotifyPropertyChanged_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, params string[] propertyNames)
        {
            //Arrange

            //Act
            var item = BuildAndObfuscateAssemblies(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);

                //Assert
                using (new AssertionScope())
                {
                    foreach (var propertyName in propertyNames)
                    {
                        mainClassType.Should()
                            .HavePropertyWithName(propertyName).And
                            .HaveObfuscationStatus(item.Mapping, ObfuscationStatus.Skipped);
                    }
                }
            }
        }

        [Theory(Skip = "Why is non public properties skipped for classes with heirs implementing INotifyPropertyChanged interface. Properties are detected public due to getter and/or setter IsFamily.")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "ProtectedProperty", "InternalProperty", "PrivateProperty")]
        public void Should_not_skip_non_public_property_when_INotifyPropertyChanged_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, params string[] propertyNames)
        {
            //Arrange

            //Act
            var item = BuildAndObfuscateAssemblies(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);

                //Assert
                using (new AssertionScope())
                {
                    foreach (var propertyName in propertyNames)
                    {
                        mainClassType.Should()
                            .HavePropertyWithName(propertyName).And
                            .NotHaveObfuscationStatus(item.Mapping, ObfuscationStatus.Skipped);
                    }
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "PublicProperty")]
        public void Should_not_skip_public_property_when_INotifyPropertyChanged_interface_is_not_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, params string[] propertyNames)
        {
            //Arrange

            //Act
            var item = BuildAndObfuscateAssemblies(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);

                //Assert
                using (new AssertionScope())
                {
                    foreach (var propertyName in propertyNames)
                    {
                        mainClassType.Should()
                            .HavePropertyWithName(propertyName).And
                            .NotHaveObfuscationStatus(item.Mapping, ObfuscationStatus.Skipped);
                    }
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "ProtectedProperty", "InternalProperty", "PrivateProperty")]
        public void Should_not_skip_non_public_property_when_INotifyPropertyChanged_interface_is_not_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, params string[] propertyNames)
        {
            //Arrange

            //Act
            var item = BuildAndObfuscateAssemblies(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);

                //Assert
                using (new AssertionScope())
                {
                    foreach (var propertyName in propertyNames)
                    {
                        mainClassType.Should()
                            .HavePropertyWithName(propertyName).And
                            .NotHaveObfuscationStatus(item.Mapping, ObfuscationStatus.Skipped);
                    }
                }
            }
        }
    }
}
