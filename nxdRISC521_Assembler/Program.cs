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

            Parser parser = new Parser(lineTokens);
            List<Operation> parsedOps = parser.Parse();

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
                    string dataLine = addr.ToString("X4") + " : " + iw.ToString("X4") + "; ";
                    dataLine += $"% {op.Name.ToString()} Ri={op.Ri.ToString()} Rj={op.Rj.ToString()} %";
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
