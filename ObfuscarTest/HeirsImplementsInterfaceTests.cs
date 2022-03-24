using System.IO;
using Mono.Cecil;
using Obfuscar;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;
using Obfuscar.Helpers;

namespace ObfuscarTest
{
    public class HeirsImplementsInterfaceTests
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
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.A")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.A")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.H")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.H")]
        public void Should_detect_non_generic_interface_is_implemented_if_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.C<int>")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.E<int>")]
        public void Should_detect_generic_interface_is_implemented_if_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_detect_INotifyPropertyChanged_interface_is_implemented_if_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_not_detect_INotifyPropertyChanged_interface_is_implemented_if_not_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.B")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.B")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.I")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.I")]
        public void Should_not_detect_non_generic_interface_is_implemented_if_not_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.D<int>")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassImplementsINotifyPropertyChanged", "TestClasses.D<int>")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.K<int>")]
        [InlineData("AssemblyWithHeirsInheritedInterfaces", "TestClasses.HeirToNonAbstractBaseClassNotImplementsINotifyPropertyChanged", "TestClasses.K<int>")]
        public void Should_not_detect_generic_interface_is_implemented_if_not_part_of_heirs_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndLoad(testCodeFileNameWithoutExtension);

            using (var inputAssemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, $"{testCodeFileNameWithoutExtension}.dll")))
            {
                //Act
                var mainClassType = inputAssemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassHeirsImplementsInterface = mainClassType.HeirsImplementsInterface(interfaceName, item.Project.AssemblyList);

                //Assert
                using (new AssertionScope())
                {
                    mainClassHeirsImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }
    }
}
