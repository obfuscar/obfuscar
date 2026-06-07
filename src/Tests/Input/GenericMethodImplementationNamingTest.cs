using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace GenericMethodImplementationNamingTest
{
	[Obfuscation(ApplyToMembers = false)]
	public interface Interface1
	{
		//  Task Method1(string section, IDictionary<string, string> data);

		Task Method1<T1, T2>(T1 data1, T2 data2);
	}

	[Obfuscation(ApplyToMembers = false)]
	public class Example_1 : Interface1
	{
		public async Task Method1(string section, IDictionary<string, string> data)
		{
		}

		// after obfuscation the generic method implementation should have same name as interface's method
		public async Task Method1<T1, T2>(T1 data1, T2 data2)
		{
		}
	}

	[Obfuscation(ApplyToMembers = false)]
	public class Example_2 : Interface1
	{
		public async Task Method1(string section, IDictionary<string, string> data)
		{
		}

		// after obfuscation the generic method implementation should have same name as interface's method
		async Task Interface1.Method1<T1, T2>(T1 data1, T2 data2)
		{
			
		}
	}

	// no need to obfuscate
	[Obfuscation]
	public class UsageOfExampleClasses
	{
		public void CallGenericMethod1()
		{
			new Example_1().Method1(2, "test");
			(new Example_2() as Interface1).Method1(2, "test");
		}
	}
}