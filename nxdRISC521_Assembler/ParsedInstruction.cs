using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    class EnumConv
    {
        public static OperandType TokenToOperandType(TokenType t)
        {
            switch(t)
            {
                case TokenType.RegName:
                    return OperandType.Register;
                    break;
                case TokenType.Constant:
                    return OperandType.Constant;
                    break;
                case TokenType.Label:
                    return OperandType.Label;
                    break;
                default:
                    throw new InvalidCastException("Cannot cast TokenType " + t.ToString() + " to OperandType.");
                    break;
            }
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

        public ParsedInstruction(string name)
        {
            Operands = new List<Operand>();
            Name = name;
        }
    }
}
