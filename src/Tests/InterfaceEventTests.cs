using System;
using System.IO;
using System.Reflection.Emit;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class InterfaceEventTests
    {
        private string output;

        private Obfuscator BuildAndObfuscateAssemblies()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='RenameEvents' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithInterfaceEventContract.dll' />" +
                @"<Module file='$(InPath){2}AssemblyWithInterfaceEventImpl.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate(
                new[] { "AssemblyWithInterfaceEventContract", "AssemblyWithInterfaceEventImpl" },
                xml);
        }

        private static MethodDefinition FindMethodByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.Fail(string.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        private static EventDefinition FindEventByName(TypeDefinition typeDef, string name)
        {
            foreach (EventDefinition evt in typeDef.Events)
                if (evt.Name == name)
                    return evt;

            Assert.Fail(string.Format("Expected to find event: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckPublicInterfaceEventMappings()
        {
            Obfuscator item = BuildAndObfuscateAssemblies();
            ObfuscationMap map = item.Mapping;

            AssemblyDefinition contract = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithInterfaceEventContract.dll"));
            AssemblyDefinition implementation = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithInterfaceEventImpl.dll"));

            TypeDefinition interfaceType = contract.MainModule.GetType("TestLib.ITest");
            MethodDefinition interfaceAdd = FindMethodByName(interfaceType, "add_TestEvent");
            MethodDefinition interfaceRemove = FindMethodByName(interfaceType, "remove_TestEvent");

            TypeDefinition implementationType = implementation.MainModule.GetType("TestClasses.EventImplementation");
            EventDefinition implementationEvent = FindEventByName(implementationType, "TestEvent");
            MethodDefinition implementationAdd = FindMethodByName(implementationType, "add_TestEvent");
            MethodDefinition implementationRemove = FindMethodByName(implementationType, "remove_TestEvent");

            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(interfaceAdd)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(interfaceRemove)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetEvent(new EventKey(new TypeKey(implementationType), implementationEvent)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(implementationAdd)).Status);
            Assert.Equal(
                ObfuscationStatus.Skipped,
                map.GetMethod(new MethodKey(implementationRemove)).Status);
        }

        [Fact]
        public void CheckPublicInterfaceEventRunsAfterObfuscation()
        {
            Obfuscator item = BuildAndObfuscateAssemblies();
            string outputPath = item.Project.Settings.OutPath;
            output = outputPath;

            string implementationPath = Path.Combine(outputPath, "AssemblyWithInterfaceEventImpl.dll");
            Assembly implementationAssembly = Assembly.LoadFile(implementationPath);

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

                Type entryType = implementationAssembly.GetType("TestClasses.EventEntryPoint");
                MethodInfo executeMethod = entryType.GetMethod("ExecuteThroughInterface", BindingFlags.Public | BindingFlags.Static);
                AssertAssemblyTokensResolve(implementationAssembly);
                AssertMethodTokensResolve(executeMethod);

                object result = executeMethod.Invoke(null, Array.Empty<object>());
                Assert.Equal(1, (int)result);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(output, args.Name.Split(',')[0] + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        }

        private static void AssertMethodTokensResolve(MethodInfo method)
        {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null)
                return;

            int i = 0;
            while (i < il.Length)
            {
                OpCode opCode;
                byte first = il[i++];
                if (first == 0xFE)
                {
                    byte second = il[i++];
                    opCode = MultiByteOpCodes[second];
                }
                else
                {
                    opCode = SingleByteOpCodes[first];
                }

                if (opCode.OperandType == OperandType.InlineMethod ||
                    opCode.OperandType == OperandType.InlineField ||
                    opCode.OperandType == OperandType.InlineType ||
                    opCode.OperandType == OperandType.InlineTok)
                {
                    int token = BitConverter.ToInt32(il, i);
                    try
                    {
                        method.Module.ResolveMember(token, method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments());
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Invalid metadata token 0x{token:X8} in {method.DeclaringType?.FullName}::{method.Name} at IL offset {i - opCode.Size} for opcode {opCode}: {ex.Message}");
                    }
                }

                i += GetOperandSize(opCode, il, i);
            }
        }

        private static void AssertAssemblyTokensResolve(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    AssertMethodTokensResolve(method);
                }
            }
        }

        private static int GetOperandSize(OpCode opCode, byte[] il, int operandOffset)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(il, operandOffset);
                    return 4 + (count * 4);
                default:
                    return 0;
            }
        }

        private static readonly OpCode[] SingleByteOpCodes = BuildSingleByteOpCodes();
        private static readonly OpCode[] MultiByteOpCodes = BuildMultiByteOpCodes();

        private static OpCode[] BuildSingleByteOpCodes()
        {
            var opCodes = new OpCode[0x100];
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(OpCode))
                    continue;
                var opCode = (OpCode)field.GetValue(null);
                if (opCode.Size == 1)
                    opCodes[(byte)opCode.Value] = opCode;
            }
            return opCodes;
        }

        private static OpCode[] BuildMultiByteOpCodes()
        {
            var opCodes = new OpCode[0x100];
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(OpCode))
                    continue;
                var opCode = (OpCode)field.GetValue(null);
                if (opCode.Size == 2 && ((opCode.Value >> 8) & 0xFF) == 0xFE)
                    opCodes[opCode.Value & 0xFF] = opCode;
            }
            return opCodes;
        }
    }
}
