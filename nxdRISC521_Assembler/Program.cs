using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nxdRISC_Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("======nxd6495 RISC521 Assembler======");
            Console.WriteLine("Select an option:");
            Console.WriteLine("   1. Assemble a single file");
            Console.WriteLine("   2. Assemble 5 task files");
            string option = Console.ReadLine();
            // List<Operation> parsedOps = new List<Operation>();
            switch(option)
            {
                case "1":
                    AssembleSingleFile();
                    break;
                case "2":
                    AssembleMultiTasks();
                    break;
            }

            //Console.WriteLine("Enter a filename to assemble:");
            //string fName = Console.ReadLine();
            //string[] lines = File.ReadAllLines(fName);

            //// We'll start by tokenizing every line

            //List<List<Token>> lineTokens = new List<List<Token>>();
            //int lineNum = 0;

            //foreach(string line in lines)
            //{
            //    lineTokens.Add(Tokenizer.TokenizeLine(line, lineNum));
            //    lineNum++;
            //}

            //Parser parser = new Parser(lineTokens);
            //List<Operation> parsedOps = parser.Parse();
            //int kbIRQLoc = parser.GetIRQLocation("KB_IRQ");

            //if(kbIRQLoc > 0)
            //{
            //    Console.WriteLine("Writing RAM MIF file...");
            //    StringBuilder ramMIFOutput = new StringBuilder();
            //    ramMIFOutput.AppendLine("WIDTH = 16;");
            //    ramMIFOutput.AppendLine("DEPTH = 4096;");
            //    ramMIFOutput.AppendLine("ADDRESS_RADIX = HEX;");
            //    ramMIFOutput.AppendLine("DATA_RADIX = HEX;");
            //    ramMIFOutput.AppendLine("CONTENT BEGIN");
            //    ramMIFOutput.AppendLine($"0FF0 : {kbIRQLoc.ToString("X4")};");
            //    ramMIFOutput.AppendLine("END;");
            //    File.WriteAllText("nxd_ram_main.mif", ramMIFOutput.ToString());
            //    Console.WriteLine("RAM MIF file generated");
            //}

            //Console.WriteLine("Writing ROM MIF file...");

            //StringBuilder mifOutput = new StringBuilder();
            //mifOutput.AppendLine("WIDTH = 16;");
            //mifOutput.AppendLine("DEPTH = 4096;");
            //mifOutput.AppendLine("ADDRESS_RADIX = HEX;");
            //mifOutput.AppendLine("DATA_RADIX = HEX;");

            //mifOutput.AppendLine("CONTENT BEGIN");

            //int addr = 0;
            //foreach(Operation op in parsedOps)
            //{
            //    int[] iws = op.GetBinary(); // Get the instruction words of the operation
            //    Console.Write("Successfully parsed " + op.Name.ToString() + " Ri=" +
            //        op.Ri.ToString() + " Rj=" + op.Rj.ToString() + " to hex value: ");
            //    foreach(int iw in iws)
            //    {
            //        Console.Write(iw.ToString("X4"));
            //        string dataLine = addr.ToString("X4") + " : " + iw.ToString("X4") + "; ";
            //        dataLine += $"% {op.Name.ToString()} Ri={op.Ri.ToString()} Rj={op.Rj.ToString()} %";
            //        mifOutput.AppendLine(dataLine);
            //        addr++;
            //    }
            //    Console.WriteLine();
            //}

            //mifOutput.AppendLine("END;");

            //File.WriteAllText(fName.Substring(0, fName.Length - 4) + ".mif", mifOutput.ToString());

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        private static void AssembleSingleFile()
        {
            Console.WriteLine("Enter a filename to assemble:");
            string fName = Console.ReadLine();
            Parser parser = CreateParserFromFile(fName);
            List<Operation> parsedOps = parser.Parse();
            int kbIRQLoc = parser.GetIRQLocation("KB_IRQ");

            if (kbIRQLoc >= 0)
            {
                Console.WriteLine("Writing RAM MIF file...");
                StringBuilder ramMIFOutput = new StringBuilder();
                ramMIFOutput.AppendLine("WIDTH = 16;");
                ramMIFOutput.AppendLine("DEPTH = 4096;");
                ramMIFOutput.AppendLine("ADDRESS_RADIX = HEX;");
                ramMIFOutput.AppendLine("DATA_RADIX = HEX;");
                ramMIFOutput.AppendLine("CONTENT BEGIN");
                ramMIFOutput.AppendLine($"0FF0 : {kbIRQLoc.ToString("X4")};");
                ramMIFOutput.AppendLine("END;");
                File.WriteAllText("nxd_ram_main.mif", ramMIFOutput.ToString());
                Console.WriteLine("RAM MIF file generated");
            }

            Console.WriteLine("Writing ROM MIF file...");

            StringBuilder mifOutput = new StringBuilder();
            mifOutput.AppendLine("WIDTH = 16;");
            mifOutput.AppendLine("DEPTH = 4096;");
            mifOutput.AppendLine("ADDRESS_RADIX = HEX;");
            mifOutput.AppendLine("DATA_RADIX = HEX;");

            mifOutput.AppendLine("CONTENT BEGIN");

            int addr = 0;
            foreach (Operation op in parsedOps)
            {
                int[] iws = op.GetBinary(); // Get the instruction words of the operation
                Console.Write("Successfully parsed " + op.Name.ToString() + " Ri=" +
                    op.Ri.ToString() + " Rj=" + op.Rj.ToString() + " to hex value: ");
                foreach (int iw in iws)
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

            // Console.WriteLine("Press any key to continue...");
            // Console.ReadLine();
        }

        private static void AssembleMultiTasks()
        {
            List<List<Operation>> parsedTasks = new List<List<Operation>>();
            int kbIRQLoc = -1;
            for(int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Enter filename for task {i} (starts at 0x{i}00):");
                string fName = Console.ReadLine();
                Parser taskParser = CreateParserFromFile(fName);
                parsedTasks.Add(taskParser.Parse(i * 0x100));
                int kbIRQLocTask = taskParser.GetIRQLocation("KB_IRQ");
                if(kbIRQLoc >= 0 && kbIRQLocTask >= 0)
                {
                    Console.WriteLine("ERROR: Keyboard IRQ discovered in multiple tasks.");
                    Console.ReadLine();
                    return;
                }
                else if(kbIRQLoc < 0 && kbIRQLocTask >= 0)
                {
                    kbIRQLoc = kbIRQLocTask;
                }
            }

            if (kbIRQLoc >= 0)
            {
                Console.WriteLine("Writing RAM MIF file...");
                StringBuilder ramMIFOutput = new StringBuilder();
                ramMIFOutput.AppendLine("WIDTH = 16;");
                ramMIFOutput.AppendLine("DEPTH = 4096;");
                ramMIFOutput.AppendLine("ADDRESS_RADIX = HEX;");
                ramMIFOutput.AppendLine("DATA_RADIX = HEX;");
                ramMIFOutput.AppendLine("CONTENT BEGIN");
                ramMIFOutput.AppendLine($"0FF0 : {kbIRQLoc.ToString("X4")};");
                ramMIFOutput.AppendLine("END;");
                File.WriteAllText("nxd_ram_main.mif", ramMIFOutput.ToString());
                Console.WriteLine("RAM MIF file generated");
            }

            Console.WriteLine("Writing ROM MIF file...");

            StringBuilder mifOutput = new StringBuilder();
            mifOutput.AppendLine("WIDTH = 16;");
            mifOutput.AppendLine("DEPTH = 4096;");
            mifOutput.AppendLine("ADDRESS_RADIX = HEX;");
            mifOutput.AppendLine("DATA_RADIX = HEX;");

            mifOutput.AppendLine("CONTENT BEGIN");

            for(int i = 0; i < 5; i++)
            {
                int addr = i * 0x100;
                foreach (Operation op in parsedTasks[i])
                {
                    int[] iws = op.GetBinary(); // Get the instruction words of the operation
                    Console.Write("Successfully parsed " + op.Name.ToString() + " Ri=" +
                        op.Ri.ToString() + " Rj=" + op.Rj.ToString() + " to hex value: ");
                    foreach (int iw in iws)
                    {
                        Console.Write(iw.ToString("X4"));
                        string dataLine = addr.ToString("X4") + " : " + iw.ToString("X4") + "; ";
                        dataLine += $"% {op.Name.ToString()} Ri={op.Ri.ToString()} Rj={op.Rj.ToString()} %";
                        mifOutput.AppendLine(dataLine);
                        addr++;
                    }
                    Console.WriteLine();
                }
            }

            mifOutput.AppendLine("END;");

            File.WriteAllText("nxd_ram_main_TASKS.mif", mifOutput.ToString());

            // Console.WriteLine("Press any key to continue...");
            // Console.ReadLine();
        }

        private static Parser CreateParserFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

            List<List<Token>> lineTokens = new List<List<Token>>();
            int lineNum = 0;

            foreach (string line in lines)
            {
                lineTokens.Add(Tokenizer.TokenizeLine(line, lineNum));
                lineNum++;
            }

            Parser parser = new Parser(lineTokens);
            return parser;
        }
    }
}
