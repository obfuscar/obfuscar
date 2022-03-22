using System;
using System.Linq;
using System.Reflection;
using System.Web.UI.HtmlControls;
using FluentAssertions;
using Mono.Cecil;
using Obfuscar;
using Xunit;

namespace ObfuscarTest
{
    public class MethodKeyToGeneralGenericMemberNameTests
    {
        [Theory]
        [InlineData(
            "System.Void ObfuscarTest.TestClass.TestMethod",
            "System.Void ObfuscarTest.TestClass.TestMethod")]
        [InlineData(
            "System.Void ObfuscarTest.TestClass<int, int, int>.GenericMethod",
            "System.Void ObfuscarTest.TestClass`3.GenericMethod")]
        [InlineData(
            "System.Void ObfuscarTest.TestClass<int,int,int>.GenericMethod",
            "System.Void ObfuscarTest.TestClass`3.GenericMethod")]
        [InlineData(
            "System.Void ObfuscarTest.TestClass<int, int, int, GenericType<int, int, int>, GenericType<int, int, int>>.GenericMethod",
            "System.Void ObfuscarTest.TestClass`5.GenericMethod")]
        [InlineData(
            "System.Void ObfuscarTest.TestClass<int,int,int,GenericType<int,int,int>, GenericType<int,int,int>>.GenericMethod",
            "System.Void ObfuscarTest.TestClass`5.GenericMethod")]
        public void Should_convert_general_generic_member_name_to_generic_count_member_name(string methodName, string expectedResult)
        {
            //Act
            var result = MethodKey.ToGeneralGenericMemberName(methodName);

            //Assert
            result.Should().Be(expectedResult);
        }
    }
}
