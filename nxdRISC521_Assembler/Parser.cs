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
            { "add", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "addc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "sub", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "subc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "and", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "or", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "not", new MnemonicFormat(1, 1, new TokenType[]{ TokenType.RegName }) },
            { "shra", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "rotr", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "in", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "out", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "cpy", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "swap", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "ld", new MnemonicFormat(3, 2, new TokenType[]{ TokenType.RegName, TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "st", new MnemonicFormat(3, 2, new TokenType[]{ TokenType.RegName, TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "jmp", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jc", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jn", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jv", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jz", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jnc", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jnn", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jnv", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "jnz", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
        };

        private List<List<Token>> tokenStream;

        private List<List<Token>> directives; // Tokens in the .directives section
        private List<List<Token>> constants; // Tokens in the .constants section
        private List<List<Token>> code; // Tokens in the .code section

        // Dictionary of parsed .equ directives
        private Dictionary<string, string> dirDict = new Dictionary<string, string>();
        // Locations in memory where labels point to
        private Dictionary<string, int> labelLocations = new Dictionary<string, int>();

        public Parser(List<List<Token>> tokens)
        {
            tokenStream = tokens;
            code = new List<List<Token>>();
            constants = new List<List<Token>>();
            directives = new List<List<Token>>();
        }

        public List<Operation> Parse()
        {
            FirstPass();
            ParseDirectives();
            return ParseCode();

            // throw new NotImplementedException();
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
            int addr = 0;
            List<ParsedInstruction> firstPassInstrs = new List<ParsedInstruction>();
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
                            ParsedInstruction parsedInstr = new ParsedInstruction(op.Value);
                            for(int i = 0; i < instrFormat.OperandCount; i++)
                            {
                                Token operand = line[i + 1];
                                if((instrFormat.OperandTypes[i] & operand.Type) == operand.Type)
                                {
                                    Operand o = new Operand(operand.Value, EnumConv.TokenToOperandType(operand.Type));
                                    parsedInstr.Operands.Add(o);
                                }
                            }
                            firstPassInstrs.Add(parsedInstr);
                            addr += instrFormat.IWCount;
                        }
                    }
                }
                else if(op.Type == TokenType.Label)
                {
                    labelLocations.Add(op.Value, addr);
                }
            }

            List<Operation> parsedOps = new List<Operation>();

            foreach(ParsedInstruction instr in firstPassInstrs)
            {
                Opcodes opName = EnumConv.StringToOpcode(instr.Name);
                if(Operation.ManipOpcodes.Contains(opName))
                {
                    int Ri = 0;
                    int Rj = 0;
                    // The Ri operand must always be a register, so we'll just
                    // check that and assign Ri easily
                    if (instr.Operands[0].Type != OperandType.Register)
                        throw new ArgumentException($"Invalid Ri operand type {instr.Operands[0].Type} for instruction {instr.Name}.");
                    Ri = int.Parse(instr.Operands[0].Value);
                    // Next we'll assign Rj, which can have a few types
                    // If Rj doesn't exist, it's fine, we can just keep it as 0.
                    // We have NOT which only takes one operand.
                    if (instr.Operands.Count > 1)
                    {
                        if (instr.Operands[1].Type == OperandType.Register || instr.Operands[1].Type == OperandType.Constant)
                        {
                            Rj = int.Parse(instr.Operands[1].Value);
                        }
                        else if (instr.Operands[1].Type == OperandType.Label)
                        {
                            if (dirDict.ContainsKey(instr.Operands[1].Value))
                            {
                                Rj = int.Parse(dirDict[instr.Operands[1].Value]);
                            }
                        }
                    }
                    
                    parsedOps.Add(new ManipulationOperation(Ri, Rj, opName));
                }
                else if(Operation.MemOpcodes.Contains(opName))
                {
                    int Ri = 0;
                    int Rj = 0;
                    int offset = 0;
                    // Ri and Rj must be registers
                    Ri = int.Parse(instr.Operands[0].Value);
                    Rj = int.Parse(instr.Operands[1].Value);
                    // Offset can be a hard-coded or labeled constant
                    if (instr.Operands[2].Type == OperandType.Constant)
                        offset = int.Parse(instr.Operands[2].Value);
                    else if (instr.Operands[2].Type == OperandType.Label)
                        if (dirDict.ContainsKey(instr.Operands[2].Value))
                            offset = int.Parse(dirDict[instr.Operands[2].Value]);

                    parsedOps.Add(new MemoryOperation(Ri, Rj, offset, opName));
                }
                else if(opName == Opcodes.JMP)
                {
                    JumpTypes jmpType = EnumConv.StringToJumpType(instr.Name);
                    int offset = 0;
                    // This assembler will only allow direct AM, so Ri=0 and
                    // offset can be a constant or label
                    if (instr.Operands[0].Type == OperandType.Constant)
                        offset = int.Parse(instr.Operands[0].Value);
                    else if (instr.Operands[0].Type == OperandType.Label)
                        if (labelLocations.ContainsKey(instr.Operands[0].Value))
                            offset = labelLocations[instr.Operands[0].Value];

                    parsedOps.Add(new JumpOperation(0, offset, jmpType));
                }
            }

            return parsedOps;
        }
    }
}
