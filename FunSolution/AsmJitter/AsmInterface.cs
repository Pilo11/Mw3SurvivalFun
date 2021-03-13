using AsmJitter.Model;
using AsmJitter.Model.Instruction;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AsmJitter
{
    public static class AsmInterface
    {

        public static Code Nop(this Code code)
        {
            code.Instructions.Add(new Nop());
            return code;
        }

        public static Code Push(this Code code, Register register)
        {
            code.Instructions.Add(new Push(register));
            return code;
        }

        public static Code Pop(this Code code, Register register)
        {
            code.Instructions.Add(new Pop(register));
            return code;
        }

        public static Code Ret(this Code code)
        {
            code.Instructions.Add(new Ret());
            return code;
        }

        public static Code Mov(this Code code, RegisterOperand targetRegister, Register subtrahendRegister)
        {
            code.Instructions.Add(new MovRegisterToRegister(targetRegister, subtrahendRegister));
            return code;
        }

        public static Code Mov(this Code code, Register targetRegister, AbstractConst constant)
        {
            code.Instructions.Add(new MovConstantToRegister(targetRegister, constant));
            return code;
        }

        public static Code Mov(this Code code, RegisterMemory targetRegister, AbstractConst constant)
        {
            code.Instructions.Add(new MovConstantToRegisterMemory(targetRegister, constant));
            return code;
        }

        public static Code Movss(this Code code, Register targetRegister, FourBytesConst addressToConstant)
        {
            code.Instructions.Add(new MovssConstantMemoryToRegister(targetRegister, addressToConstant));
            return code;
        }

        public static Code Movss(this Code code, Register targetRegister, RegisterMemory originRegister)
        {
            code.Instructions.Add(new MovssRegisterMemoryToRegister(targetRegister, originRegister));
            return code;
        }

        public static Code Sub(this Code code, RegisterOperand targetRegister, Register subtrahendRegister)
        {
            code.Instructions.Add(new Sub(targetRegister, subtrahendRegister));
            return code;
        }

        public static Code Cmp(this Code code, RegisterOperand register, AbstractConst constant)
        {
            code.Instructions.Add(new Cmp(register, constant));
            return code;
        }

        public static Code Comiss(this Code code, Register firstRegister, Register secondRegister)
        {
            code.Instructions.Add(new Comiss(firstRegister, secondRegister));
            return code;
        }

        public static Code Fld(this Code code, RegisterOperand targetRegister)
        {
            code.Instructions.Add(new Fld(targetRegister));
            return code;
        }

        public static Code Fstp(this Code code, RegisterOperand targetRegister)
        {
            code.Instructions.Add(new Fstp(targetRegister));
            return code;
        }

        public static Code Jb(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jb(target));
            return code;
        }

        public static Code Jb(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jb(target));
            return code;
        }

        public static Code Jnb(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jnb(target));
            return code;
        }

        public static Code Jnb(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jnb(target));
            return code;
        }

        public static Code Je(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Je(target));
            return code;
        }

        public static Code Je(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Je(target));
            return code;
        }

        public static Code Jne(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jne(target));
            return code;
        }

        public static Code Jne(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jne(target));
            return code;
        }        

        public static Code Jl(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jl(target));
            return code;
        }

        public static Code Jl(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jl(target));
            return code;
        }

        public static Code Jnl(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jnl(target));
            return code;
        }

        public static Code Jnl(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jnl(target));
            return code;
        }

        public static Code Jle(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jle(target));
            return code;
        }

        public static Code Jle(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jle(target));
            return code;
        }

        public static Code Jnle(this Code code, Label label)
        {
            var target = new JumpTarget(label);
            code.Instructions.Add(new Jnle(target));
            return code;
        }

        public static Code Jnle(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Jnle(target));
            return code;
        }

        public static Code Call(this Code code, FourBytesConst targetAddress)
        {
            var target = new JumpTarget(targetAddress);
            code.Instructions.Add(new Call(target));
            return code;
        }

        public static Code Label(this Code code, Label label)
        {
            if (code.Instructions.Contains(label))
            {
                throw new ArgumentException($"You cannot define the label: {label.Name} twice.");
            }
            code.Instructions.Add(label);
            label.NextInstruction = code.Instructions.LastOrDefault();
            return code;
        }

    }
}
