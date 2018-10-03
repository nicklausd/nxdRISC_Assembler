using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    class Parser
    {
        private enum SectionType
        {
            Dir, Consts, Code, None
        }

        private Dictionary<string, MnemonicFormat> mnemonicFormat = new Dictionary<string, MnemonicFormat>()
        {
            { "add", new MnemonicFormat(2, new TokenType[]{ TokenType.RegName, TokenType.RegName | TokenType.Label }) },
            { "addc", new MnemonicFormat(2, new TokenType[]{ TokenType.RegName, TokenType.Constant }) },
            { "sub", new MnemonicFormat(2, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "subc", new MnemonicFormat(2, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },

        };

        private List<List<Token>> tokenStream;

        private List<List<Token>> directives; // Tokens in the .directives section
        private List<List<Token>> constants; // Tokens in the .constants section
        private List<List<Token>> code; // Tokens in the .code section

        // Dictionary of parsed .equ directives
        private Dictionary<string, string> dirDict = new Dictionary<string, string>();

        public Parser(List<List<Token>> tokens)
        {
            tokenStream = tokens;
        }

        public List<Operation> Parse()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a first pass through the list of tokens, splitting it into
        /// the different sections.
        /// </summary>
        private void FirstPass()
        {
            SectionType section = SectionType.None;
            foreach(List<Token> line in tokenStream)
            {
                if (line == null || line.Count == 0) continue;
                if (section == SectionType.None)
                {
                    Token label = line[0];
                    if (label.Type != TokenType.SectionLabel)
                        continue; // TODO: Change to a ParserException
                    if (label.Value == ".directives")
                        section = SectionType.Dir;
                    else if (label.Value == ".constants")
                        section = SectionType.Consts;
                    else if (label.Value == ".code")
                        section = SectionType.Code;
                    else
                        continue; // TODO: Change to a ParserException
                }
                else if(section == SectionType.Code)
                {
                    Token label = line[0];
                    if(label.Type == TokenType.SectionLabel)
                    {
                        if(label.Value == ".endcode")
                        {
                            section = SectionType.None;
                            continue;
                        }
                        else continue; // TODO: Change to a ParserException
                    }
                    else
                    {
                        code.Add(line);
                    }
                }
                else if(section == SectionType.Dir)
                {
                    Token label = line[0];
                    if (label.Type == TokenType.SectionLabel)
                    {
                        if (label.Value == ".enddirectives")
                        {
                            section = SectionType.None;
                            continue;
                        }
                        else continue; // TODO: Change to a ParserException
                    }
                    else
                    {
                        directives.Add(line);
                    }
                }
                else if(section == SectionType.Consts)
                {
                    Token label = line[0];
                    if (label.Type == TokenType.SectionLabel)
                    {
                        if (label.Value == ".endconstants")
                        {
                            section = SectionType.None;
                            continue;
                        }
                        else continue; // TODO: Change to a ParserException
                    }
                    else
                    {
                        constants.Add(line);
                    }
                }
            }
        }

        private void ParseDirectives()
        {
            foreach(List<Token> line in directives)
            {
                Token dirType = line[0];
                if(dirType.Type == TokenType.AsmDirective)
                {
                    if(dirType.Value == ".equ")
                    {
                        // .equ directive requires a label and a value, so we'll
                        // look ahead two tokens in the line
                        if(line.Count == 3)
                        {
                            Token label = line[1];
                            Token val = line[2];
                            if(label.Type == TokenType.Label && val.Type == TokenType.Constant)
                            {
                                dirDict.Add(label.Value, val.Value);
                            }
                        }
                    }
                    else
                    {
                        // TODO: Throw an exception
                    }
                }
                else
                {
                    // Throw an exception
                }
            }
        }

        private void ParseConstants()
        {
            // Figure out how to do this since our design is Harvard memory structure.
            // TODO: As a side project, try out making a von Neumann design too, where
            // this is a bit easier to handle.
        }

        private List<Operation> ParseCode()
        {
            foreach(List<Token> line in code)
            {
                Token op = line[0];
                if(op.Type == TokenType.OpCode)
                {
                    if(mnemonicFormat.ContainsKey(op.Value))
                    {
                        MnemonicFormat instrFormat = mnemonicFormat[op.Value];
                        // Check if the number of operands is correct, remembering to
                        // account for the fact that the first token in the line was our
                        // opcode token
                        if(line.Count == instrFormat.OperandCount + 1)
                        {
                            for(int i = 0; i < instrFormat.OperandCount; i++)
                            {
                                Token operand = line[i + 1];
                                if((instrFormat.OperandTypes[i] & operand.Type) == operand.Type)
                                {

                                }
                            }
                        }
                    }
                }
            }

            throw new NotImplementedException();
        }
    }
}
