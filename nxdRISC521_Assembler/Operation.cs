using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC_Assembler
{
    enum Opcodes
    {
        ADD = 0b000000,
        SUB = 0b000001,
        ADDC = 0b000010,
        SUBC = 0b000011,
        NOT = 0b000100,
        AND = 0b000101,
        OR = 0b000110,
        SHRA = 0b000111,
        ROTR = 0b001000,
        LD = 0b001001,
        ST = 0b001010,
        IN = 0b001011,
        OUT = 0b001100,
        CPY = 0b001101,
        SWAP = 0b001110,
        PUSH = 0b001111,
        POP = 0b010000,
        JMP = 0b010001,
        CALL = 0b010010,
        RET = 0b010011,
        ANDC = 0b010100,
        ORC = 0b010101,
    }

    enum JumpTypes
    {
        JMP = 0b0000,
        JC = 0b1000,
        JN = 0b0100,
        JV = 0b0010,
        JZ = 0b0001,
        JNC = 0b0111,
        JNN = 0b1011,
        JNV = 0b1101,
        JNZ = 0b1110,
    }

    abstract class Operation
    {
        public int Ri { get; }
        public int Rj { get; }
        public Opcodes Name { get; }

        protected const int WORD_MAX = 0xFFFF;
        protected const int R_VAL_MAX = 0x1F;

        public static List<Opcodes> ManipOpcodes = new List<Opcodes>()
        {
            Opcodes.ADD, Opcodes.ADDC, Opcodes.SUB, Opcodes.SUBC,
            Opcodes.AND, Opcodes.OR, Opcodes.NOT, Opcodes.SHRA,
            Opcodes.ROTR, Opcodes.IN, Opcodes.OUT, Opcodes.CPY, Opcodes.SWAP,
            Opcodes.ANDC, Opcodes.ORC,
        };

        public static List<Opcodes> MemOpcodes = new List<Opcodes>()
        {
            Opcodes.LD, Opcodes.ST,
        };

        public static List<Opcodes> StackOpcodes = new List<Opcodes>()
        {
            Opcodes.PUSH, Opcodes.POP,
        };

        public static List<Opcodes> MethodOpcodes = new List<Opcodes>()
        {
            Opcodes.CALL, Opcodes.RET,
        };

        /// <summary>
        /// Instantiate a new Operation
        /// </summary>
        /// <param name="ri">Destination/Source1 Register</param>
        /// <param name="rj">Source2 Register</param>
        /// <param name="name">Name of the opcode</param>
        public Operation(int ri, int rj, Opcodes name)
        {
            Ri = ri;
            Rj = rj;
            if(Ri > R_VAL_MAX)
            {
                throw new ArgumentOutOfRangeException("Ri", "Value of Ri is greater than the allocated bit size.");
            }
            else if(Rj > R_VAL_MAX)
            {
                throw new ArgumentOutOfRangeException("Rj", "Value of Rj is greater than the allocated bit size.");
            }
            Name = name;
        }

        /// <summary>
        /// Returns a binary Instruction Word representation of the operation.
        /// </summary>
        /// <returns></returns>
        public abstract int[] GetBinary();
    }

    class ManipulationOperation : Operation
    {
        public ManipulationOperation(int ri, int rj, Opcodes name) : base(ri, rj, name)
        {
        }

        public override int[] GetBinary()
        {
            int iw0 = 0;
            iw0 ^= (int)Name << 10; // Shift opcode into position and XOR into instruction word
            // We'll AND the register values with the max value just in case somehow the
            // values got changed to be out of bounds without throwing an exception
            iw0 ^= (Ri & R_VAL_MAX) << 5; // XOR in Ri value shifted into position
            iw0 ^= (Rj & R_VAL_MAX); // XOR in Rj value

            return new int[1]{ iw0 }; // Manipulation instructions only have IW0
        }
    }

    // MemoryOperation is for LD and ST only; IN, OUT, CPY, and SWAP all
    // follow the same format as ManipulationOperation and will be handled
    // through that class instead.
    class MemoryOperation : Operation
    {
        public int MemoryOffset { get; }

        public MemoryOperation(int ri, int rj, int offset, Opcodes name) : base(ri, rj, name)
        {
            if (offset > WORD_MAX)
                throw new ArgumentOutOfRangeException("MemoryOffset", "Memory offset greater than maximum word size.");
            MemoryOffset = offset;
        }

        public override int[] GetBinary()
        {
            int iw0 = 0; int iw1 = 0;
            iw0 ^= (int)Name << 10;
            iw0 ^= (Ri & R_VAL_MAX) << 5;
            iw0 ^= (Rj & R_VAL_MAX);
            iw1 ^= (MemoryOffset & WORD_MAX);

            return new int[2] { iw0, iw1 };
        }
    }

    class JumpOperation : Operation
    {
        public JumpTypes Condition { get; }
        public int MemoryOffset { get; }

        public JumpOperation(int ri, int offset, JumpTypes cond) : base(ri, 0, Opcodes.JMP)
        {
            if (offset > WORD_MAX)
                throw new ArgumentOutOfRangeException("MemoryOffset", "Memory offset greater than maximum word size.");
            Condition = cond;
            MemoryOffset = offset;
        }

        public override int[] GetBinary()
        {
            int iw0 = 0; int iw1 = 0;
            iw0 ^= (int)Name << 10;
            iw0 ^= (Ri * R_VAL_MAX) << 5;
            iw0 ^= (int)Condition << 1;
            iw1 ^= (MemoryOffset & WORD_MAX);

            return new int[2] { iw0, iw1 };
        }
    }

    class StackOperation : Operation
    {
        public StackOperation(int ri, Opcodes name) : base(ri, 0, name)
        {
        }

        public override int[] GetBinary()
        {
            int iw0 = 0;
            iw0 ^= (int)Name << 10;
            iw0 ^= (Ri & R_VAL_MAX) << 5;

            return new int[1] { iw0 };
        }
    }

    class MethodOperation : Operation
    {
        int MemoryOffest;

        public MethodOperation(int offset, Opcodes name) : base (0, 0, name)
        {
            MemoryOffest = offset & WORD_MAX;
        }

        public override int[] GetBinary()
        {
            int iw0 = (int)Name << 10;

            int[] res = new int[]{ iw0 };

            if(Name == Opcodes.CALL)
            {
                res = new int[2];
                res[0] = iw0; res[1] = MemoryOffest;
            }

            return res;
        }
    }
}
