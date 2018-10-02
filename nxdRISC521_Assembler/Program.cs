using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("======nxd6495 RISC521 Assembler======");
            Console.WriteLine("Enter a filename to assemble:");
            string fName = Console.ReadLine();
            string[] lines = File.ReadAllLines(fName);

            // We'll start by tokenizing every line

            List<List<Token>> lineTokens = new List<List<Token>>();

            foreach(string line in lines)
            {
                lineTokens.Add(Tokenizer.TokenizeLine(line));
            }

            List<Operation> parsedOps = new List<Operation>(); // Parsed list of code
            Dictionary<string, Opcodes> opcodeDict = new Dictionary<string, Opcodes>()
            {
                { "and", Opcodes.AND },
                { "or", Opcodes.OR },
                { "add", Opcodes.ADD },
                { "sub", Opcodes.SUB },
                { "addc", Opcodes.ADDC },
                { "subc", Opcodes.SUBC },
                { "shra", Opcodes.SHRA },
                { "rotr", Opcodes.ROTR },
            };

            foreach(List<Token> tLine in lineTokens)
            {
                // For now we're only going to bother parsing manipulation instructions
                // We can worry about code sections later, and we can worry about other
                // instruction types later. As long as manipulation instructions are
                // able to be parsed and assembled properly, we are good.
                if (tLine == null) continue;
                Token op = tLine[0];
                if(op.Type == TokenType.OpCode)
                {
                    if(op.Value == "not")
                    {
                        // For NOT, make sure the line contains only one other token and that
                        // the token is a register
                        if(tLine.Count == 2 && tLine[1].Type == TokenType.RegName)
                        {
                            parsedOps.Add(new ManipulationOperation(int.Parse(tLine[1].Value.Substring(1)), 0, Opcodes.NOT));
                        }
                    }
                    else if(op.Value == "add" || op.Value == "sub" || op.Value == "and" || op.Value == "or")
                    {
                        // Two-operand ops where both operands are registers
                        if(tLine.Count == 3 && tLine[1].Type == TokenType.RegName && tLine[2].Type == TokenType.RegName)
                        {
                            parsedOps.Add(new ManipulationOperation(int.Parse(tLine[1].Value.Substring(1)), int.Parse(tLine[2].Value.Substring(1)), opcodeDict[op.Value]));
                        }
                    }
                    else if(op.Value == "addc" || op.Value == "subc" || op.Value == "shra" || op.Value == "rotr")
                    {
                        if (tLine.Count == 3 && tLine[1].Type == TokenType.RegName && tLine[2].Type == TokenType.Constant)
                        {
                            parsedOps.Add(new ManipulationOperation(int.Parse(tLine[1].Value.Substring(1)), int.Parse(tLine[2].Value), opcodeDict[op.Value]));
                        }
                    }
                }
            }

            StringBuilder mifOutput = new StringBuilder();
            mifOutput.AppendLine("WIDTH = 14;");
            mifOutput.AppendLine("DEPTH = 1024;");
            mifOutput.AppendLine("ADDRESS_RADIX = HEX;");
            mifOutput.AppendLine("DATA_RADIX = HEX;");

            mifOutput.AppendLine("CONTENT BEGIN");

            int addr = 0;
            foreach(Operation op in parsedOps)
            {
                int[] iws = op.GetBinary(); // Get the instruction words of the operation
                Console.Write("Successfully parsed " + op.Name.ToString() + " Ri=" +
                    op.Ri.ToString() + " Rj=" + op.Rj.ToString() + " to hex value: ");
                foreach(int iw in iws)
                {
                    Console.Write(iw.ToString("X4"));
                    string dataLine = addr.ToString("X4") + " : " + iw.ToString("X4") + ";";
                    mifOutput.AppendLine(dataLine);
                    addr++;
                }
                Console.WriteLine();
            }

            mifOutput.AppendLine("END;");

            File.WriteAllText(fName.Substring(0, fName.Length - 4) + ".mif", mifOutput.ToString());

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }
    }
}
