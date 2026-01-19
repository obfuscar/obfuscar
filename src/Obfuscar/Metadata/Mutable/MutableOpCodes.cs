namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Standard IL opcodes.
    /// This replaces the legacy Cecil OpCodes.
    /// </summary>
    public static class MutableOpCodes
    {
        // Load constants
        public static readonly MutableOpCode Nop = new MutableOpCode("nop", 0x00, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Break = new MutableOpCode("break", 0x01, MutableOperandType.InlineNone, MutableFlowControl.Break);
        public static readonly MutableOpCode Ldarg_0 = new MutableOpCode("ldarg.0", 0x02, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarg_1 = new MutableOpCode("ldarg.1", 0x03, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarg_2 = new MutableOpCode("ldarg.2", 0x04, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarg_3 = new MutableOpCode("ldarg.3", 0x05, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc_0 = new MutableOpCode("ldloc.0", 0x06, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc_1 = new MutableOpCode("ldloc.1", 0x07, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc_2 = new MutableOpCode("ldloc.2", 0x08, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc_3 = new MutableOpCode("ldloc.3", 0x09, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc_0 = new MutableOpCode("stloc.0", 0x0A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc_1 = new MutableOpCode("stloc.1", 0x0B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc_2 = new MutableOpCode("stloc.2", 0x0C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc_3 = new MutableOpCode("stloc.3", 0x0D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarg_S = new MutableOpCode("ldarg.s", 0x0E, MutableOperandType.ShortInlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarga_S = new MutableOpCode("ldarga.s", 0x0F, MutableOperandType.ShortInlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Starg_S = new MutableOpCode("starg.s", 0x10, MutableOperandType.ShortInlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc_S = new MutableOpCode("ldloc.s", 0x11, MutableOperandType.ShortInlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloca_S = new MutableOpCode("ldloca.s", 0x12, MutableOperandType.ShortInlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc_S = new MutableOpCode("stloc.s", 0x13, MutableOperandType.ShortInlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldnull = new MutableOpCode("ldnull", 0x14, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_M1 = new MutableOpCode("ldc.i4.m1", 0x15, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_0 = new MutableOpCode("ldc.i4.0", 0x16, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_1 = new MutableOpCode("ldc.i4.1", 0x17, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_2 = new MutableOpCode("ldc.i4.2", 0x18, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_3 = new MutableOpCode("ldc.i4.3", 0x19, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_4 = new MutableOpCode("ldc.i4.4", 0x1A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_5 = new MutableOpCode("ldc.i4.5", 0x1B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_6 = new MutableOpCode("ldc.i4.6", 0x1C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_7 = new MutableOpCode("ldc.i4.7", 0x1D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_8 = new MutableOpCode("ldc.i4.8", 0x1E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4_S = new MutableOpCode("ldc.i4.s", 0x1F, MutableOperandType.ShortInlineI, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I4 = new MutableOpCode("ldc.i4", 0x20, MutableOperandType.InlineI, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_I8 = new MutableOpCode("ldc.i8", 0x21, MutableOperandType.InlineI8, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_R4 = new MutableOpCode("ldc.r4", 0x22, MutableOperandType.ShortInlineR, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldc_R8 = new MutableOpCode("ldc.r8", 0x23, MutableOperandType.InlineR, MutableFlowControl.Next);
        public static readonly MutableOpCode Dup = new MutableOpCode("dup", 0x25, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Pop = new MutableOpCode("pop", 0x26, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Jmp = new MutableOpCode("jmp", 0x27, MutableOperandType.InlineMethod, MutableFlowControl.Call);
        public static readonly MutableOpCode Call = new MutableOpCode("call", 0x28, MutableOperandType.InlineMethod, MutableFlowControl.Call);
        public static readonly MutableOpCode Calli = new MutableOpCode("calli", 0x29, MutableOperandType.InlineSig, MutableFlowControl.Call);
        public static readonly MutableOpCode Ret = new MutableOpCode("ret", 0x2A, MutableOperandType.InlineNone, MutableFlowControl.Return);
        public static readonly MutableOpCode Br_S = new MutableOpCode("br.s", 0x2B, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Branch);
        public static readonly MutableOpCode Brfalse_S = new MutableOpCode("brfalse.s", 0x2C, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Brtrue_S = new MutableOpCode("brtrue.s", 0x2D, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Beq_S = new MutableOpCode("beq.s", 0x2E, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bge_S = new MutableOpCode("bge.s", 0x2F, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bgt_S = new MutableOpCode("bgt.s", 0x30, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Ble_S = new MutableOpCode("ble.s", 0x31, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Blt_S = new MutableOpCode("blt.s", 0x32, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bne_Un_S = new MutableOpCode("bne.un.s", 0x33, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bge_Un_S = new MutableOpCode("bge.un.s", 0x34, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bgt_Un_S = new MutableOpCode("bgt.un.s", 0x35, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Ble_Un_S = new MutableOpCode("ble.un.s", 0x36, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Blt_Un_S = new MutableOpCode("blt.un.s", 0x37, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Br = new MutableOpCode("br", 0x38, MutableOperandType.InlineBrTarget, MutableFlowControl.Branch);
        public static readonly MutableOpCode Brfalse = new MutableOpCode("brfalse", 0x39, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Brtrue = new MutableOpCode("brtrue", 0x3A, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Beq = new MutableOpCode("beq", 0x3B, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bge = new MutableOpCode("bge", 0x3C, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bgt = new MutableOpCode("bgt", 0x3D, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Ble = new MutableOpCode("ble", 0x3E, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Blt = new MutableOpCode("blt", 0x3F, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bne_Un = new MutableOpCode("bne.un", 0x40, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bge_Un = new MutableOpCode("bge.un", 0x41, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Bgt_Un = new MutableOpCode("bgt.un", 0x42, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Ble_Un = new MutableOpCode("ble.un", 0x43, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Blt_Un = new MutableOpCode("blt.un", 0x44, MutableOperandType.InlineBrTarget, MutableFlowControl.Cond_Branch);
        public static readonly MutableOpCode Switch = new MutableOpCode("switch", 0x45, MutableOperandType.InlineSwitch, MutableFlowControl.Cond_Branch);
        
        // Load/store indirect
        public static readonly MutableOpCode Ldind_I1 = new MutableOpCode("ldind.i1", 0x46, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_U1 = new MutableOpCode("ldind.u1", 0x47, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_I2 = new MutableOpCode("ldind.i2", 0x48, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_U2 = new MutableOpCode("ldind.u2", 0x49, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_I4 = new MutableOpCode("ldind.i4", 0x4A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_U4 = new MutableOpCode("ldind.u4", 0x4B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_I8 = new MutableOpCode("ldind.i8", 0x4C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_I = new MutableOpCode("ldind.i", 0x4D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_R4 = new MutableOpCode("ldind.r4", 0x4E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_R8 = new MutableOpCode("ldind.r8", 0x4F, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldind_Ref = new MutableOpCode("ldind.ref", 0x50, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_Ref = new MutableOpCode("stind.ref", 0x51, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_I1 = new MutableOpCode("stind.i1", 0x52, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_I2 = new MutableOpCode("stind.i2", 0x53, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_I4 = new MutableOpCode("stind.i4", 0x54, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_I8 = new MutableOpCode("stind.i8", 0x55, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_R4 = new MutableOpCode("stind.r4", 0x56, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stind_R8 = new MutableOpCode("stind.r8", 0x57, MutableOperandType.InlineNone, MutableFlowControl.Next);
        
        // Arithmetic
        public static readonly MutableOpCode Add = new MutableOpCode("add", 0x58, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Sub = new MutableOpCode("sub", 0x59, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Mul = new MutableOpCode("mul", 0x5A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Div = new MutableOpCode("div", 0x5B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Div_Un = new MutableOpCode("div.un", 0x5C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Rem = new MutableOpCode("rem", 0x5D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Rem_Un = new MutableOpCode("rem.un", 0x5E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode And = new MutableOpCode("and", 0x5F, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Or = new MutableOpCode("or", 0x60, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Xor = new MutableOpCode("xor", 0x61, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Shl = new MutableOpCode("shl", 0x62, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Shr = new MutableOpCode("shr", 0x63, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Shr_Un = new MutableOpCode("shr.un", 0x64, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Neg = new MutableOpCode("neg", 0x65, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Not = new MutableOpCode("not", 0x66, MutableOperandType.InlineNone, MutableFlowControl.Next);
        
        // Conversions
        public static readonly MutableOpCode Conv_I1 = new MutableOpCode("conv.i1", 0x67, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_I2 = new MutableOpCode("conv.i2", 0x68, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_I4 = new MutableOpCode("conv.i4", 0x69, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_I8 = new MutableOpCode("conv.i8", 0x6A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_R4 = new MutableOpCode("conv.r4", 0x6B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_R8 = new MutableOpCode("conv.r8", 0x6C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_U4 = new MutableOpCode("conv.u4", 0x6D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_U8 = new MutableOpCode("conv.u8", 0x6E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        
        // Object model
        public static readonly MutableOpCode Callvirt = new MutableOpCode("callvirt", 0x6F, MutableOperandType.InlineMethod, MutableFlowControl.Call);
        public static readonly MutableOpCode Cpobj = new MutableOpCode("cpobj", 0x70, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldobj = new MutableOpCode("ldobj", 0x71, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldstr = new MutableOpCode("ldstr", 0x72, MutableOperandType.InlineString, MutableFlowControl.Next);
        public static readonly MutableOpCode Newobj = new MutableOpCode("newobj", 0x73, MutableOperandType.InlineMethod, MutableFlowControl.Call);
        public static readonly MutableOpCode Castclass = new MutableOpCode("castclass", 0x74, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Isinst = new MutableOpCode("isinst", 0x75, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_R_Un = new MutableOpCode("conv.r.un", 0x76, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Unbox = new MutableOpCode("unbox", 0x79, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Throw = new MutableOpCode("throw", 0x7A, MutableOperandType.InlineNone, MutableFlowControl.Throw);
        public static readonly MutableOpCode Ldfld = new MutableOpCode("ldfld", 0x7B, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldflda = new MutableOpCode("ldflda", 0x7C, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Stfld = new MutableOpCode("stfld", 0x7D, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldsfld = new MutableOpCode("ldsfld", 0x7E, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldsflda = new MutableOpCode("ldsflda", 0x7F, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Stsfld = new MutableOpCode("stsfld", 0x80, MutableOperandType.InlineField, MutableFlowControl.Next);
        public static readonly MutableOpCode Stobj = new MutableOpCode("stobj", 0x81, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I1_Un = new MutableOpCode("conv.ovf.i1.un", 0x82, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I2_Un = new MutableOpCode("conv.ovf.i2.un", 0x83, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I4_Un = new MutableOpCode("conv.ovf.i4.un", 0x84, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I8_Un = new MutableOpCode("conv.ovf.i8.un", 0x85, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U1_Un = new MutableOpCode("conv.ovf.u1.un", 0x86, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U2_Un = new MutableOpCode("conv.ovf.u2.un", 0x87, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U4_Un = new MutableOpCode("conv.ovf.u4.un", 0x88, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U8_Un = new MutableOpCode("conv.ovf.u8.un", 0x89, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I_Un = new MutableOpCode("conv.ovf.i.un", 0x8A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U_Un = new MutableOpCode("conv.ovf.u.un", 0x8B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Box = new MutableOpCode("box", 0x8C, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Newarr = new MutableOpCode("newarr", 0x8D, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldlen = new MutableOpCode("ldlen", 0x8E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelema = new MutableOpCode("ldelema", 0x8F, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_I1 = new MutableOpCode("ldelem.i1", 0x90, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_U1 = new MutableOpCode("ldelem.u1", 0x91, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_I2 = new MutableOpCode("ldelem.i2", 0x92, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_U2 = new MutableOpCode("ldelem.u2", 0x93, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_I4 = new MutableOpCode("ldelem.i4", 0x94, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_U4 = new MutableOpCode("ldelem.u4", 0x95, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_I8 = new MutableOpCode("ldelem.i8", 0x96, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_I = new MutableOpCode("ldelem.i", 0x97, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_R4 = new MutableOpCode("ldelem.r4", 0x98, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_R8 = new MutableOpCode("ldelem.r8", 0x99, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem_Ref = new MutableOpCode("ldelem.ref", 0x9A, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_I = new MutableOpCode("stelem.i", 0x9B, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_I1 = new MutableOpCode("stelem.i1", 0x9C, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_I2 = new MutableOpCode("stelem.i2", 0x9D, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_I4 = new MutableOpCode("stelem.i4", 0x9E, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_I8 = new MutableOpCode("stelem.i8", 0x9F, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_R4 = new MutableOpCode("stelem.r4", 0xA0, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_R8 = new MutableOpCode("stelem.r8", 0xA1, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem_Ref = new MutableOpCode("stelem.ref", 0xA2, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldelem = new MutableOpCode("ldelem", 0xA3, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Stelem = new MutableOpCode("stelem", 0xA4, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Unbox_Any = new MutableOpCode("unbox.any", 0xA5, MutableOperandType.InlineType, MutableFlowControl.Next);
        
        // More conversions
        public static readonly MutableOpCode Conv_Ovf_I1 = new MutableOpCode("conv.ovf.i1", 0xB3, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U1 = new MutableOpCode("conv.ovf.u1", 0xB4, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I2 = new MutableOpCode("conv.ovf.i2", 0xB5, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U2 = new MutableOpCode("conv.ovf.u2", 0xB6, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I4 = new MutableOpCode("conv.ovf.i4", 0xB7, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U4 = new MutableOpCode("conv.ovf.u4", 0xB8, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I8 = new MutableOpCode("conv.ovf.i8", 0xB9, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U8 = new MutableOpCode("conv.ovf.u8", 0xBA, MutableOperandType.InlineNone, MutableFlowControl.Next);
        
        // More object model
        public static readonly MutableOpCode Refanyval = new MutableOpCode("refanyval", 0xC2, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ckfinite = new MutableOpCode("ckfinite", 0xC3, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Mkrefany = new MutableOpCode("mkrefany", 0xC6, MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldtoken = new MutableOpCode("ldtoken", 0xD0, MutableOperandType.InlineTok, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_U2 = new MutableOpCode("conv.u2", 0xD1, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_U1 = new MutableOpCode("conv.u1", 0xD2, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_I = new MutableOpCode("conv.i", 0xD3, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_I = new MutableOpCode("conv.ovf.i", 0xD4, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_Ovf_U = new MutableOpCode("conv.ovf.u", 0xD5, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Add_Ovf = new MutableOpCode("add.ovf", 0xD6, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Add_Ovf_Un = new MutableOpCode("add.ovf.un", 0xD7, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Mul_Ovf = new MutableOpCode("mul.ovf", 0xD8, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Mul_Ovf_Un = new MutableOpCode("mul.ovf.un", 0xD9, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Sub_Ovf = new MutableOpCode("sub.ovf", 0xDA, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Sub_Ovf_Un = new MutableOpCode("sub.ovf.un", 0xDB, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Endfinally = new MutableOpCode("endfinally", 0xDC, MutableOperandType.InlineNone, MutableFlowControl.Return);
        public static readonly MutableOpCode Leave = new MutableOpCode("leave", 0xDD, MutableOperandType.InlineBrTarget, MutableFlowControl.Branch);
        public static readonly MutableOpCode Leave_S = new MutableOpCode("leave.s", 0xDE, MutableOperandType.ShortInlineBrTarget, MutableFlowControl.Branch);
        public static readonly MutableOpCode Stind_I = new MutableOpCode("stind.i", 0xDF, MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Conv_U = new MutableOpCode("conv.u", 0xE0, MutableOperandType.InlineNone, MutableFlowControl.Next);
        
        // Two-byte opcodes (prefix 0xFE)
        public static readonly MutableOpCode Arglist = new MutableOpCode("arglist", unchecked((short)0xFE00), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ceq = new MutableOpCode("ceq", unchecked((short)0xFE01), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Cgt = new MutableOpCode("cgt", unchecked((short)0xFE02), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Cgt_Un = new MutableOpCode("cgt.un", unchecked((short)0xFE03), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Clt = new MutableOpCode("clt", unchecked((short)0xFE04), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Clt_Un = new MutableOpCode("clt.un", unchecked((short)0xFE05), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldftn = new MutableOpCode("ldftn", unchecked((short)0xFE06), MutableOperandType.InlineMethod, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldvirtftn = new MutableOpCode("ldvirtftn", unchecked((short)0xFE07), MutableOperandType.InlineMethod, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarg = new MutableOpCode("ldarg", unchecked((short)0xFE09), MutableOperandType.InlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldarga = new MutableOpCode("ldarga", unchecked((short)0xFE0A), MutableOperandType.InlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Starg = new MutableOpCode("starg", unchecked((short)0xFE0B), MutableOperandType.InlineArg, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloc = new MutableOpCode("ldloc", unchecked((short)0xFE0C), MutableOperandType.InlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Ldloca = new MutableOpCode("ldloca", unchecked((short)0xFE0D), MutableOperandType.InlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Stloc = new MutableOpCode("stloc", unchecked((short)0xFE0E), MutableOperandType.InlineVar, MutableFlowControl.Next);
        public static readonly MutableOpCode Localloc = new MutableOpCode("localloc", unchecked((short)0xFE0F), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Endfilter = new MutableOpCode("endfilter", unchecked((short)0xFE11), MutableOperandType.InlineNone, MutableFlowControl.Return);
        public static readonly MutableOpCode Unaligned = new MutableOpCode("unaligned.", unchecked((short)0xFE12), MutableOperandType.ShortInlineI, MutableFlowControl.Meta);
        public static readonly MutableOpCode Volatile = new MutableOpCode("volatile.", unchecked((short)0xFE13), MutableOperandType.InlineNone, MutableFlowControl.Meta);
        public static readonly MutableOpCode Tail = new MutableOpCode("tail.", unchecked((short)0xFE14), MutableOperandType.InlineNone, MutableFlowControl.Meta);
        public static readonly MutableOpCode Initobj = new MutableOpCode("initobj", unchecked((short)0xFE15), MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Constrained = new MutableOpCode("constrained.", unchecked((short)0xFE16), MutableOperandType.InlineType, MutableFlowControl.Meta);
        public static readonly MutableOpCode Cpblk = new MutableOpCode("cpblk", unchecked((short)0xFE17), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Initblk = new MutableOpCode("initblk", unchecked((short)0xFE18), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode No = new MutableOpCode("no.", unchecked((short)0xFE19), MutableOperandType.ShortInlineI, MutableFlowControl.Meta);
        public static readonly MutableOpCode Rethrow = new MutableOpCode("rethrow", unchecked((short)0xFE1A), MutableOperandType.InlineNone, MutableFlowControl.Throw);
        public static readonly MutableOpCode Sizeof = new MutableOpCode("sizeof", unchecked((short)0xFE1C), MutableOperandType.InlineType, MutableFlowControl.Next);
        public static readonly MutableOpCode Refanytype = new MutableOpCode("refanytype", unchecked((short)0xFE1D), MutableOperandType.InlineNone, MutableFlowControl.Next);
        public static readonly MutableOpCode Readonly = new MutableOpCode("readonly.", unchecked((short)0xFE1E), MutableOperandType.InlineNone, MutableFlowControl.Meta);
    }
}
