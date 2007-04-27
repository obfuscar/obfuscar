#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>
#endregion

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuscar
{
	class CallMap
	{
		private readonly Project project;

		// list of methods that call each method
		private readonly Dictionary<MethodKey, C5.HashSet<MethodKey>> calledBy = new Dictionary<MethodKey, C5.HashSet<MethodKey>>( );

		// list of methods that accessed each field
		private readonly Dictionary<FieldKey, C5.HashSet<MethodKey>> usedBy = new Dictionary<FieldKey, C5.HashSet<MethodKey>>( );

		private CallMapVisitor visitor;

		public CallMap( Project project )
		{
			this.project = project;

			visitor = new CallMapVisitor( this );

			foreach ( AssemblyInfo info in project )
			{
				foreach ( TypeDefinition type in info.Definition.MainModule.Types )
				{
					if ( type.FullName == "<Module>" )
						continue;

					TypeKey typeKey = new TypeKey( type );

					foreach ( MethodDefinition method in type.Constructors )
						HandleMethod( typeKey, method );
					foreach ( MethodDefinition method in type.Methods )
						HandleMethod( typeKey, method );
				}
			}
		}

		void HandleMethod( TypeKey typeKey, MethodDefinition method )
		{
			if ( method.HasBody )
				method.Body.Instructions.Accept( visitor );
		}

		void HandleFieldOperand( MethodKey methodKey, FieldReference reference )
		{
			if ( !project.Contains( reference.DeclaringType ) )
				return;

			FieldKey fieldKey = new FieldKey( reference );

			C5.HashSet<MethodKey> usedByList;
			if ( !usedBy.TryGetValue( fieldKey, out usedByList ) )
			{
				usedByList = new C5.HashSet<MethodKey>( );
				usedBy[fieldKey] = usedByList;
			}

			usedByList.Add( methodKey );
		}

		void HandleMethodOperand( MethodKey methodKey, MethodReference reference )
		{
			if ( !project.Contains( reference.DeclaringType ) )
				return;

			MethodKey refKey = new MethodKey( reference );

			C5.HashSet<MethodKey> calledByList;
			if ( !calledBy.TryGetValue( refKey, out calledByList ) )
			{
				calledByList = new C5.HashSet<MethodKey>( );
				calledBy[refKey] = calledByList;
			}

			calledByList.Add( methodKey );
		}

		void HandleTypeOperand( MethodKey methodKey, TypeReference typeOp )
		{
			// not sure what to do...
		}

		class CallMapVisitor : ICodeVisitor
		{
			private readonly CallMap callMap;

			public CallMapVisitor( CallMap callMap )
			{
				this.callMap = callMap;
			}

			public void VisitInstructionCollection( InstructionCollection instructions )
			{
				MethodKey methodKey = new MethodKey( instructions.Container.Method );

				foreach ( Instruction instruction in instructions )
				{
					switch ( instruction.OpCode.OperandType )
					{
						case OperandType.InlineField:
							callMap.HandleFieldOperand( methodKey, (FieldReference) instruction.Operand );
							break;
						case OperandType.InlineMethod:
							callMap.HandleMethodOperand( methodKey, (MethodReference) instruction.Operand );
							break;
						case OperandType.InlineType:
							callMap.HandleTypeOperand( methodKey, (TypeReference) instruction.Operand );
							break;
						case OperandType.InlineTok:
							if ( instruction.Operand is TypeReference )
								callMap.HandleTypeOperand( methodKey, (TypeReference) instruction.Operand );
							else if ( instruction.Operand is FieldReference )
								callMap.HandleFieldOperand( methodKey, (FieldReference) instruction.Operand );
							else if ( instruction.Operand is MethodReference )
								callMap.HandleMethodOperand( methodKey, (MethodReference) instruction.Operand );
							break;
						default:
							break;
					}
				}
			}

			#region Unused overrides

			public void TerminateMethodBody( MethodBody body )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitExceptionHandler( ExceptionHandler eh )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitExceptionHandlerCollection( ExceptionHandlerCollection seh )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitInstruction( Instruction instr )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitMethodBody( MethodBody body )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitScope( Scope scope )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitScopeCollection( ScopeCollection scopes )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitVariableDefinition( VariableDefinition var )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			public void VisitVariableDefinitionCollection( VariableDefinitionCollection variables )
			{
				throw new Exception( "The method or operation is not implemented." );
			}

			#endregion
		}
	}
}
