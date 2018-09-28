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

            // We'll start by splitting the input file into the different sections

            // Section 1: Assembler Directives
            string[] directives = SplitDirectives(fName, lines);
            if(directives != null)
            {
                // We've got some directives to process; do that here.
            }

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
