using System.IO;
using Mono.Cecil;
using Obfuscar;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;
using Obfuscar.Helpers;

namespace ObfuscarTest
{
    public class ImplementsInterfaceTests
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

        Obfuscator BuildAndLoad(string csFileName)
        {
            var xml = CreateTestConfiguration(csFileName);
            TestHelper.BuildAssembly(csFileName, string.Empty);
            return Obfuscator.CreateFromXml(xml);
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "TestClasses.A")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "TestClasses.A")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "TestClasses.H")]
        public void Should_detect_non_generic_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "TestClasses.C<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "TestClasses.E<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "TestClasses.C<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "TestClasses.E<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "TestClasses.J<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "TestClasses.L<int>")]
        public void Should_detect_generic_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_detect_INotifyPropertyChanged_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_not_detect_INotifyPropertyChanged_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "TestClasses.B")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "TestClasses.B")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "TestClasses.I")]
        public void Should_not_detect_non_generic_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.ImplementsINotifyPropertyChanged", "TestClasses.D<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.BaseImplementsINotifyPropertyChanged", "TestClasses.D<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "TestClasses.NotImplementsINotifyPropertyChanged", "TestClasses.K<int>")]
        public void Should_not_detect_generic_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var _ = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }
    }
}
