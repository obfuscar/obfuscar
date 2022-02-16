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
        Obfuscator BuildAndObfuscateAssemblies(string name)
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar, name);

            return TestHelper.BuildAndObfuscate(name, string.Empty, xml);
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.A")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "TestClasses.E")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.A")]
        public void Should_detect_non_generic_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testCodeFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testCodeFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.C<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.D<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.C<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.D<int>")]
        public void Should_detect_generic_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testAssemblyFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testAssemblyFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "System.ComponentModel.INotifyPropertyChanged")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_detect_INotifyPropertyChanged_interface_is_implemented_if_part_of_nested_inherited_interfaces(
            string testAssemblyFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testAssemblyFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeTrue($"Class \"{mainClassName}\" should implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "System.ComponentModel.INotifyPropertyChanged")]
        public void Should_not_detect_INotifyPropertyChanged_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testAssemblyFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testAssemblyFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.B")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.E")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.F")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "TestClasses.B")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "TestClasses.F")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.B")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.E")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.F")]
        public void Should_not_detect_non_generic_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testAssemblyFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testAssemblyFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
                var mainClassImplementsInterface = mainClassType.ImplementsInterface(interfaceName);

                //Assert
                using (new AssertionScope())
                {
                    mainClassImplementsInterface.Should().BeFalse($"Class \"{mainClassName}\" should not implement \"{interfaceName}\"");
                }
            }
        }

        [Theory(Skip = "No support or need to detect generic interfaces currently. Implementation is only used for skipping non generic interfaces.")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.G<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.X", "TestClasses.H<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "TestClasses.G<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Y", "TestClasses.H<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.G<int>")]
        [InlineData("AssemblyWithInheritedInterfaces", "AssemblyWithInheritedInterfaces.dll", "TestClasses.Z", "TestClasses.H<int>")]
        public void Should_not_detect_generic_interface_is_implemented_if_not_part_of_nested_inherited_interfaces(
            string testAssemblyFileNameWithoutExtension, string testAssemblyFileName,
            string mainClassName, string interfaceName)
        {
            //Arrange
            var item = BuildAndObfuscateAssemblies(testAssemblyFileNameWithoutExtension);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, testAssemblyFileName));
            using (_ = AssemblyDefinition.ReadAssembly(Path.Combine(item.Project.Settings.OutPath, testAssemblyFileName)))
            {
                //Act
                var mainClassType = assemblyDefinition.MainModule.GetType(mainClassName);
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
