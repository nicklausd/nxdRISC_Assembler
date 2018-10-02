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
                foreach(int iw in iws)
                {
                    string dataLine = addr.ToString("X4") + " : " + iw.ToString("X4") + ";";
                    mifOutput.AppendLine(dataLine);
                    addr++;
                }
            }

            mifOutput.AppendLine("END;");

            File.WriteAllText(fName.Substring(0, fName.Length - 4) + ".mif", mifOutput.ToString());

            Console.ReadLine();
        }

        public static string[] SplitDirectives(string fName, string[] lines)
        {
            // We'll follow the syntax of .directives to start the section and
            // .enddirectives to end the section.
            // We'll do a simple linear search over every line. It's not particularly
            // efficient but it will do for now.
            int directivesStart = -1;
            int directivesEnd = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                // For now, we will assume that our assembly won't contain any strings
                // or that when we need strings we won't have any semicolons in them,
                // so we'll treat anything after a semicolon as a comment
                string[] splitComments = lines[i].Split(';');
                if (splitComments[0] == "")
                    continue;
                // Strip leading and trailing whitespace, and force line to lower since we
                // don't particularly care about character casing.
                string stripped = splitComments[0].Trim().ToLower();
                if (stripped == ".directives")
                {
                    if (directivesStart == -1)
                        directivesStart = i;
                    else
                    {
                        // User should not declare more than one directives section.
                        ConsoleColor defaultColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("ERROR:");
                        Console.ForegroundColor = defaultColor;
                        Console.WriteLine(" Cannot declare more than one directive section.");
                        Console.WriteLine("Error in " + fName + ": Line " + i.ToString());
                        Console.ReadLine();
                        throw new Exception("Directives Exception");
                    }
                }
                if (stripped == ".enddirectives")
                {
                    if (directivesEnd == -1)
                        directivesEnd = i;
                    else
                    {
                        // User should not declare more than one directives section.
                        ConsoleColor defaultColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("ERROR:");
                        Console.ForegroundColor = defaultColor;
                        Console.WriteLine(" .enddirectives already declared.");
                        Console.WriteLine("Error in " + fName + ": Line " + i.ToString());
                        Console.ReadLine();
                        throw new Exception("Directives Exception");
                    }
                }
            }

            if (directivesStart != -1 && directivesEnd != -1)
            {
                if (directivesEnd - 1 == directivesStart)
                    return null;
                string[] directivesOut = new string[directivesEnd - directivesStart - 1];
                for (int i = directivesStart + 1, j = 0; i < directivesEnd; i++, j++)
                {
                    directivesOut[j] = lines[i];
                }
                return directivesOut;
            }
            else if (directivesStart == -1 && directivesEnd != -1)
            {
                // User should declare both start and end of directives
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR:");
                Console.ForegroundColor = defaultColor;
                Console.WriteLine(" .enddirectives declared without a matching .directives statement.");
                Console.WriteLine("Error in " + fName + ": Line " + directivesEnd.ToString());
                Console.ReadLine();
                throw new Exception("Directives Exception");
            }
            else if (directivesStart != -1 && directivesEnd == -1)
            {
                // User should declare both start and end of directives
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR:");
                Console.ForegroundColor = defaultColor;
                Console.WriteLine(" .directives declared without a matching .enddirectives statement.");
                Console.WriteLine("Error in " + fName + ": Line " + directivesStart.ToString());
                Console.ReadLine();
                throw new Exception("Directives Exception");
            }
            // Something is probably wrong if we get here, but we'll return null for now.
            return null;
        }

        public static string[] SplitCode(string fName, string[] lines)
        {

            return null;
        }
    }
}
