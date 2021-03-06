using System;
using XSharp.Compiler;
using CPUx86 = Cosmos.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Converts the unsigned integer value on top of the evaluation stack to float32 or float64.
    /// </summary>
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Conv_R_Un )]
    public class Conv_R_Un : ILOp
    {
        public Conv_R_Un( Cosmos.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute( MethodInfo aMethod, ILOpCode aOpCode )
        {
            var xValue = aOpCode.StackPopTypes[0];
            var xValueIsFloat = TypeIsFloat(xValue);
            var xValueSize = SizeOfType(xValue);
            if (xValueSize > 8)
            {
                //EmitNotImplementedException( Assembler, aServiceProvider, "Size '" + xSize.Size + "' not supported (add)", aCurrentLabel, aCurrentMethodInfo, aCurrentOffset, aNextLabel );
                throw new NotImplementedException();
            }
			//TODO if on stack a float it is first truncated, http://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.conv_r_un.aspx
            if (!xValueIsFloat)
            {
                switch (xValueSize)
                {
                    case 1:
                    case 2:
                    case 4:
                        new CPUx86.Mov { SourceReg = CPUx86.RegistersEnum.ESP, DestinationReg = CPUx86.RegistersEnum.EAX, SourceIsIndirect = true };
                        XS.SSE.ConvertSI2SS(XSRegisters.XMM0, XSRegisters.EAX);
                        XS.SSE.MoveSS(XSRegisters.ESP, XSRegisters.XMM0, destinationIsIndirect: true);
                        break;
                    case 8:
                    //XS.Add(XSRegisters.ESP, 4);
                    //break;
                    default:
                        //EmitNotImplementedException( Assembler, GetServiceProvider(), "Conv_I: SourceSize " + xSource + " not supported!", mCurLabel, mMethodInformation, mCurOffset, mNextLabel );
                        throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
