using System;
using System.IO;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
	public class CustomAttributeWithArgTests
	{
		public CustomAttributeWithArgTests()
		{
			var xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
							 @"<Var name='KeepPublicApi' value='false' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath){2}AssemblyWithCustomAttrTypeArg.dll' />" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

			TestHelper.BuildAndObfuscate ("AssemblyWithCustomAttrTypeArg", String.Empty, xml);
		}

		[Fact]
		public void Check_for_null ()
		{
			var assmDef = AssemblyDefinition.ReadAssembly (
				                             Path.Combine (TestHelper.OutputPath, "AssemblyWithCustomAttrTypeArg.dll"));

			Assert.Equal (3, assmDef.MainModule.Types.Count);
		}
	}
}
