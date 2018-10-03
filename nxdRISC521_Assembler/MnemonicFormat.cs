using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    struct MnemonicFormat
    {
        public int OperandCount;
        public TokenType[] OperandTypes;

        public MnemonicFormat(int opCount, TokenType[] opTypes)
        {
            OperandCount = opCount;
            OperandTypes = opTypes;
        }
    }
}
