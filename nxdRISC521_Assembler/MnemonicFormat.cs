using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC_Assembler
{
    struct MnemonicFormat
    {
        public int OperandCount;
        public int IWCount;
        public TokenType[] OperandTypes;

        public MnemonicFormat(int opCount, int iws, TokenType[] opTypes)
        {
            OperandCount = opCount;
            OperandTypes = opTypes;
            IWCount = iws;
        }
    }
}
