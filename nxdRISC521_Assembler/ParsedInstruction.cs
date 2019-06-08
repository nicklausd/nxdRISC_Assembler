using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    class EnumConv
    {
        private static Dictionary<string, Opcodes> strToOp = new Dictionary<string, Opcodes>()
        {
            { "add", Opcodes.ADD },
            { "addc", Opcodes.ADDC },
            { "sub", Opcodes.SUB },
            { "subc", Opcodes.SUBC },
            { "and", Opcodes.AND },
            { "or", Opcodes.OR },
            { "not", Opcodes.NOT },
            { "shra", Opcodes.SHRA },
            { "rotr", Opcodes.ROTR },
            { "ld", Opcodes.LD },
            { "st", Opcodes.ST },
            { "in", Opcodes.IN },
            { "out", Opcodes.OUT },
            { "cpy", Opcodes.CPY },
            { "swap", Opcodes.SWAP },
            { "jmp", Opcodes.JMP },
            { "jc", Opcodes.JMP },
            { "jn", Opcodes.JMP },
            { "jv", Opcodes.JMP },
            { "jz", Opcodes.JMP },
            { "jnc", Opcodes.JMP },
            { "jnn", Opcodes.JMP },
            { "jnv", Opcodes.JMP },
            { "jnz", Opcodes.JMP },
            { "push", Opcodes.PUSH },
            { "pop", Opcodes.POP },
            { "call", Opcodes.CALL },
            { "ret", Opcodes.RET },
            { "orc", Opcodes.ORC },
            { "andc", Opcodes.ANDC },
        };

        private static Dictionary<string, JumpTypes> strToJmp = new Dictionary<string, JumpTypes>()
        {
            { "jmp", JumpTypes.JMP },
            { "jc", JumpTypes.JC },
            { "jn", JumpTypes.JN },
            { "jv", JumpTypes.JV },
            { "jz", JumpTypes.JZ },
            { "jnc", JumpTypes.JNC },
            { "jnn", JumpTypes.JNN },
            { "jnv", JumpTypes.JNV },
            { "jnz", JumpTypes.JNZ },
        };

        public static OperandType TokenToOperandType(TokenType t)
        {
            switch(t)
            {
                case TokenType.RegName:
                    return OperandType.Register;
                case TokenType.Constant:
                    return OperandType.Constant;
                case TokenType.Label:
                    return OperandType.Label;
                default:
                    throw new InvalidCastException("Cannot cast TokenType " + t.ToString() + " to OperandType.");
            }
        }

        public static Opcodes StringToOpcode(string s)
        {
            if (strToOp.ContainsKey(s))
                return strToOp[s];
            else
                throw new InvalidCastException("Cannot cast " + s + " to enum Opcodes.");
        }

        public static JumpTypes StringToJumpType(string s)
        {
            if (strToJmp.ContainsKey(s))
                return strToJmp[s];
            else
                throw new InvalidCastException("Cannot cast " + s + " to enum JumpTypes.");
        }
    }

    enum OperandType
    {
        Register,
        Constant,
        Label,
    }

    struct Operand
    {
        public string Value;
        public OperandType Type;

        public Operand(string value, OperandType opType)
        {
            Value = value;
            Type = opType;
        }
    }

    class ParsedInstruction
    {
        public List<Operand> Operands { get; }
        public string Name { get; }
        public int LineNumber { get; }

        public ParsedInstruction(string name, int lineNumber)
        {
            Operands = new List<Operand>();
            Name = name;
            LineNumber = lineNumber;
        }
    }
}
