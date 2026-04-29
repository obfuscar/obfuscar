using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LeXtudio.Metadata.Mutable;
using Xunit;

public class ExceptionHandlerTests
{
    [Fact]
    public void WriteAndRead_ExceptionHandler_Preserved()
    {
        var asmName = new MutableAssemblyNameDefinition("EHTest", new Version(1,0,0,0));
        var asm = MutableAssemblyDefinition.CreateAssembly(asmName, "EHTest.dll", MutableModuleKind.Dll);
        var module = asm.MainModule;
        module.InitializeTypeSystem();

        var type = new MutableTypeDefinition("EHTest", "C", TypeAttributes.Public, null);
        module.RegisterType(type);

        var method = new MutableMethodDefinition("M", MethodAttributes.Public | MethodAttributes.Static, null);
        method.DeclaringType = type;
        type.Methods.Add(method);

        var il = method.GetILProcessor();

        // Build simple try/catch structure:
        var instr0 = il.Create(MutableOpCodes.Nop);
        il.Append(instr0);

        var instr1 = il.Create(MutableOpCodes.Nop); // nop (inside try)
        il.Append(instr1);

        var leaveInstr = il.Create(MutableOpCodes.Leave); // use leave opcode; branch target handled by operand
        il.Append(leaveInstr);
        var tryEnd = il.Create(MutableOpCodes.Nop); // marker for try end
        il.Append(tryEnd);
        var handlerStart = il.Create(MutableOpCodes.Nop);
        il.Append(handlerStart);
        var popInstr = il.Create(MutableOpCodes.Pop); // pop
        il.Append(popInstr);
        var handlerEnd = il.Create(MutableOpCodes.Nop);
        il.Append(handlerEnd);
        var ret = il.Create(MutableOpCodes.Ret); // ret
        il.Append(ret);

        // Create a catch handler for System.Exception
        var handler = new MutableExceptionHandler
        {
            HandlerType = MutableExceptionHandlerType.Catch,
            TryStart = instr0,
            TryEnd = tryEnd,
            HandlerStart = handlerStart,
            HandlerEnd = handlerEnd,
            CatchType = module.ImportReference(typeof(Exception))
        };

        method.Body.ExceptionHandlers.Add(handler);

        var temp = Path.GetTempFileName();
        try
        {
            asm.Write(temp);

            var read = MutableAssemblyDefinition.ReadAssembly(temp);
            var rt = read.MainModule.GetType("EHTest", "C");
            var rm = rt.Methods.First(m => m.Name == "M");

            Assert.NotNull(rm.Body);
            Assert.Single(rm.Body.ExceptionHandlers);
            var reh = rm.Body.ExceptionHandlers.First();
            Assert.True(reh.TryEnd.Offset > reh.TryStart.Offset);
            Assert.True(reh.HandlerEnd.Offset > reh.HandlerStart.Offset);
        }
        finally
        {
            try { File.Delete(temp); } catch { }
        }
    }
}
