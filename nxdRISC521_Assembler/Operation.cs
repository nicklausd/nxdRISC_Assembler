using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    enum Opcodes
    {
        ADD = 0b0101,
        ADDC = 0b0111,
        SUB = 0b0110,
        SUBC = 0b1000,
        AND = 0b1010,
        OR = 0b1011,
        NOT = 0b1001,
        SHRA = 0b1100,
        ROTR = 0b1101,
        LD = 0b0000,
        ST = 0b0001,
        IN = 0b1110,
        OUT = 0b1111,
        CPY = 0b0010,
        SWAP = 0b0011,
        JMP = 0b0100,
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

        protected const int WORD_MAX = 0x3FFF;
        protected const int R_VAL_MAX = 0x1F;

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

    class MemoryOperation : Operation
    {
        public MemoryOperation(int ri, int rj, Opcodes name) : base(ri, rj, name)
        {

        }

        public override int[] GetBinary()
        {


            throw new NotImplementedException();
        }
    }

    class JumpOperation : Operation
    {
        public JumpOperation(int ri, int rj) : base(ri, rj, Opcodes.JMP)
        {

        }

        public override int[] GetBinary()
        {
            throw new NotImplementedException();
        }
    }
}
