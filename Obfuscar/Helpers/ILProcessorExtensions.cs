using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuscar.Helpers
{
	static class ILProcessorExtensions
	{
		public static Instruction ReplaceAndFixReferences(this ILProcessor processor, Instruction oldInstruction, Instruction newInstruction)
		{
			newInstruction.Offset = oldInstruction.Offset;
			processor.Replace(oldInstruction, newInstruction);
			foreach (Instruction bodyInstruction in processor.Body.Instructions)
			{
				ReplaceOperandInstruction(bodyInstruction, oldInstruction, newInstruction);
			}

			foreach (ExceptionHandler exceptionHandler in processor.Body.ExceptionHandlers)
			{
				ReplaceInstruction(
					() => exceptionHandler.FilterStart,
					instruction => exceptionHandler.FilterStart = instruction,
					oldInstruction,
					newInstruction);
				ReplaceInstruction(
					() => exceptionHandler.HandlerStart,
					instruction => exceptionHandler.HandlerStart = instruction,
					oldInstruction,
					newInstruction);
				ReplaceInstruction(
					() => exceptionHandler.HandlerEnd,
					instruction => exceptionHandler.HandlerEnd = instruction,
					oldInstruction,
					newInstruction);
				ReplaceInstruction(
					() => exceptionHandler.TryStart,
					instruction => exceptionHandler.TryStart = instruction,
					oldInstruction,
					newInstruction);
				ReplaceInstruction(
					() => exceptionHandler.TryEnd,
					instruction => exceptionHandler.TryEnd = instruction,
					oldInstruction,
					newInstruction);
			}

			return newInstruction;
		}

		private static void ReplaceInstruction(
			Func<Instruction> getInstruction,
			Action<Instruction> setInstruction,
			Instruction oldInstruction,
			Instruction newInstruction)
		{
			if (getInstruction() == oldInstruction) {
				setInstruction(newInstruction);
			}
		}

		private static void ReplaceOperandInstruction(Instruction bodyInstruction, Instruction oldInstruction, Instruction newInstruction)
		{
			if (bodyInstruction == null)
				return;

			if (bodyInstruction.Operand == oldInstruction) {
				bodyInstruction.Operand = newInstruction;
			}
		}
	}
}
