using System;
using Cosmos.Assembler.x86.SSE;
using XSharp.Compiler;
using static XSharp.Compiler.XSRegisters;
using CPUx86 = Cosmos.Assembler.x86;
using Label = Cosmos.Assembler.Label;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Div )]
    public class Div : ILOp
    {
        public Div( Cosmos.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute( MethodInfo aMethod, ILOpCode aOpCode )
        {
			var xStackItem = aOpCode.StackPopTypes[0];
            var xStackItemSize = SizeOfType(xStackItem);
            var xStackItem2 = aOpCode.StackPopTypes[0];
            var xStackItem2Size = SizeOfType(xStackItem2);
			if (xStackItemSize == 8)
            {
				// there seem to be an error in MS documentation, there is pushed an int32, but IL shows else
                if (xStackItem2Size != 8)
                {
                    throw new Exception("Cosmos.IL2CPU.x86->IL->Div.cs->Error: Expected a size of 8 for Div!");
                }
                if (TypeIsFloat(xStackItem))
                {// TODO add 0/0 infinity/infinity X/infinity
					// value 1
					new CPUx86.x87.FloatLoad { DestinationReg = CPUx86.RegistersEnum.ESP, Size = 64, DestinationIsIndirect = true, DestinationDisplacement = 8 };
					// value 2
                    new CPUx86.x87.FloatDivide { DestinationReg = CPUx86.RegistersEnum.ESP, DestinationIsIndirect = true, Size = 64 };
					// override value 1
					new CPUx86.x87.FloatStoreAndPop { DestinationReg = CPUx86.RegistersEnum.ESP, Size = 64, DestinationIsIndirect = true, DestinationDisplacement = 8 };
					// pop value 2
					XS.Add(XSRegisters.ESP, 8);
                }
                else
                {
					string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
					string LabelShiftRight = BaseLabel + "ShiftRightLoop";
					string LabelNoLoop = BaseLabel + "NoLoop";
					string LabelEnd = BaseLabel + "End";

					// divisor
					//low
					XS.Set(ESI, ESP, sourceIsIndirect: true);
					//high
					XS.Set(XSRegisters.EDI, XSRegisters.ESP, sourceDisplacement: 4);

					// pop both 8 byte values
					XS.Add(XSRegisters.ESP, 8);

					//dividend
					// low
					XS.Set(EAX, ESP, sourceIsIndirect: true);
					//high
					XS.Set(XSRegisters.EDX, XSRegisters.ESP, sourceDisplacement: 4);

                    XS.Add(XSRegisters.ESP, 8);

					// set flags
					XS.Or(XSRegisters.EDI, XSRegisters.EDI);
					// if high dword of divisor is already zero, we dont need the loop
					XS.Jump(CPUx86.ConditionalTestEnum.Zero, LabelNoLoop);

					// set ecx to zero for counting the shift operations
					XS.Xor(XSRegisters.ECX, XSRegisters.ECX);

					XS.Label(LabelShiftRight);

					// shift divisor 1 bit right
          XS.ShiftRightDouble(ESI, EDI, 1);
					XS.ShiftRight(XSRegisters.EDI, 1);

					// increment shift counter
					XS.Increment(XSRegisters.ECX);

					// set flags
					XS.Or(XSRegisters.EDI, XSRegisters.EDI);
					// loop while high dword of divisor till it is zero
					XS.Jump(CPUx86.ConditionalTestEnum.NotZero, LabelShiftRight);

					// shift the divident now in one step
					// shift divident CL bits right
					XS.ShiftRightDouble(EAX, EDX, CL);
					XS.ShiftRight(XSRegisters.EDX, CL);

					// so we shifted both, so we have near the same relation as original values
					// divide this
					XS.IntegerDivide(XSRegisters.ESI);

					// sign extend
					XS.SignExtendAX(RegisterSize.Int32);

					// save result to stack
					XS.Push(XSRegisters.EDX);
					XS.Push(XSRegisters.EAX);

					//TODO: implement proper derivation correction and overflow detection

					XS.Jump(LabelEnd);

					XS.Label(LabelNoLoop);
					//save high dividend
					XS.Set(XSRegisters.ECX, XSRegisters.EAX);
					XS.Set(XSRegisters.EAX, XSRegisters.EDX);
					// extend that sign is in edx
					XS.SignExtendAX(RegisterSize.Int32);
					// divide high part
					XS.IntegerDivide(XSRegisters.ESI);
					// save high result
					XS.Push(XSRegisters.EAX);
					XS.Set(XSRegisters.EAX, XSRegisters.ECX);
					// divide low part
					XS.Divide(XSRegisters.ESI);
					// save low result
					XS.Push(XSRegisters.EAX);

					XS.Label(LabelEnd);
                }
            }
            else
            {
				if (TypeIsFloat(xStackItem))
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(XSRegisters.ESP, 4);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                    XS.SSE.DivSS(XMM0, XMM1);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                }
                else
                {
                    XS.Pop(XSRegisters.ECX);
                    XS.Pop(XSRegisters.EAX);
					          XS.SignExtendAX(RegisterSize.Int32);
                    XS.IntegerDivide(XSRegisters.ECX);
                    XS.Push(XSRegisters.EAX);
                }
            }
        }
    }
}
