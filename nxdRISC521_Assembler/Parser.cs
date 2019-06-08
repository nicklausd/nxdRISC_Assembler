using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    class ParserException : Exception
    {
        public int LineNumber { get; }
        public int TokenPosition { get; }

        public ParserException(string message, int lineNum, int tokenPos) : base(message)
        {
            LineNumber = lineNum;
            TokenPosition = tokenPos;
        }
    }

    class Parser
    {
        private enum SectionType
        {
            Dir, Consts, Code, None
        }

        private struct ParsedPragma
        {
            public List<ParsedInstruction> ParsedInstructions;
            public int LinesParsed;
            public int NewAddrOffset;
        }

        private Dictionary<string, MnemonicFormat> mnemonicFormat = new Dictionary<string, MnemonicFormat>()
        {
            { "add", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "addc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "sub", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "subc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "and", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "andc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
            { "or", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.RegName }) },
            { "orc", new MnemonicFormat(2, 1, new TokenType[]{ TokenType.RegName, TokenType.Constant | TokenType.Label }) },
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
            { "push", new MnemonicFormat(1, 1, new TokenType[]{ TokenType.RegName }) },
            { "pop", new MnemonicFormat(1, 1, new TokenType[]{ TokenType.RegName }) },
            { "call", new MnemonicFormat(1, 2, new TokenType[]{ TokenType.Constant | TokenType.Label }) },
            { "ret", new MnemonicFormat(0, 1, new TokenType[]{ }) }
        };

        private List<List<Token>> tokenStream;

        private List<List<Token>> directives; // Tokens in the .directives section
        private List<List<Token>> constants; // Tokens in the .constants section
        private List<List<Token>> code; // Tokens in the .code section

        // Dictionary of parsed .equ directives
        private Dictionary<string, string> dirDict = new Dictionary<string, string>();
        // Locations in memory where labels point to
        private Dictionary<string, int> labelLocations = new Dictionary<string, int>();

        private int forLoopNum = 0;

        public Parser(List<List<Token>> tokens)
        {
            tokenStream = tokens;
            code = new List<List<Token>>();
            constants = new List<List<Token>>();
            directives = new List<List<Token>>();
        }

        public List<Operation> Parse(int startAddr = 0)
        {
            forLoopNum = 0;
            FirstPass();
            ParseDirectives();
            return ParseCode(startAddr);

            // throw new NotImplementedException();
        }

        public int GetIRQLocation(string irqLabel)
        {
            if (labelLocations.ContainsKey(irqLabel))
                return labelLocations[irqLabel];

            return -1;
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
                        throw new ParserException($"ERROR: Invalid token on line {label.LineNumber}. " 
                            + "Expected a section label.", label.LineNumber, 0);
                    if (label.Value == ".directives")
                        section = SectionType.Dir;
                    else if (label.Value == ".constants")
                        section = SectionType.Consts;
                    else if (label.Value == ".code")
                        section = SectionType.Code;
                    else
                        throw new ParserException($"ERROR: Invalid section label on line {label.LineNumber}. ", label.LineNumber, 0);
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
                        else
                        {
                            throw new ParserException($"ERROR: Invalid section label inside .code section on line {label.LineNumber}.\n"
                                + $"Label seen: \"{label.Value}\". Expected .endcode.", label.LineNumber, 0);
                        }
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
                        else
                            throw new ParserException($"ERROR: Invalid section label inside .directives section on line {label.LineNumber}.\n"
                                + $"Label seen: \"{label.Value}\". Expected .enddirectives.", label.LineNumber, 0);
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
                        else
                            throw new ParserException($"ERROR: Invalid section label inside .constants section on line {label.LineNumber}.\n"
                                + $"Label seen: \"{label.Value}\". Expected .endconstants.", label.LineNumber, 0);
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
                        throw new ParserException($"ERROR: Unsupported directive type {dirType.Value} on line "
                            + $"{dirType.LineNumber}.", dirType.LineNumber, 0);
                    }
                }
                else
                {
                    throw new ParserException($"ERROR: Invalid token on line {dirType.LineNumber}. Expected an assembly "
                        + $"directive, received {dirType.Type}.", dirType.LineNumber, 0);
                }
            }
        }

        private void ParseConstants()
        {
            // TODO: Generate a uint16 array corresponding to the data and return it back to the
            // main function to turn into a binary format however it pleases. Also generate a memory
            // map from this so that code referencing these constants can actually get the location.

            // NOTE: This was not previously completed because the nxdRISC arch is a Harvard arch
            // and separates code and data between a ROM and RAM. Constants couldn't be stored in
            // the ROM since instructions cannot retrieve data from the ROM, and initializing the
            // RAM to some file was counterintuitive. The design has since been changed so that
            // the RAM module used can be initialized with some binary file, meaning a constants
            // section is viable.
        }

        private List<Operation> ParseCode(int startAddr = 0)
        {
            int addr = startAddr;
            List<ParsedInstruction> firstPassInstrs = new List<ParsedInstruction>();
            for(int ln = 0; ln < code.Count; ln++)
            {
                List<Token> line = code[ln];
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
                            ParsedInstruction parsedInstr = new ParsedInstruction(op.Value, op.LineNumber);
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
                        else
                        {
                            throw new ParserException($"ERROR: Expected {instrFormat.OperandCount} parameters on line "
                                + $"{op.LineNumber} for instruction {op.Value}. {line.Count - 1} parameters found.", op.LineNumber, line.Count);
                        }
                    }
                    else
                    {
                        throw new ParserException($"ERROR: Unknown mnemonic {op.Value} on line {op.LineNumber}.", op.LineNumber, 0);
                    }
                }
                else if(op.Type == TokenType.Label)
                {
                    labelLocations.Add(op.Value, addr);
                }
                else if(op.Type == TokenType.Pragma)
                {
                    // We'll hardcode this for now but come back and make it general later
                    if(op.Value == "for" && line.Count == 5)
                    {
                        ParsedInstruction subInstr = new ParsedInstruction("sub", 0);
                        Token loopReg = line[1];
                        if(loopReg.Type != TokenType.RegName)
                        {
                            // Throw an error
                            throw new Exception("FOR loop token type mismatch - first argument not a register.");
                        }
                        // Subtract the register from itself to clear it
                        subInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        subInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        addr++; // SUB is one word long

                        ParsedInstruction addcInstr = new ParsedInstruction("addc", 0);
                        Token startVal = line[2];
                        if(startVal.Type != TokenType.Label && startVal.Type != TokenType.Constant)
                        {
                            if (startVal.Type == TokenType.RegName)
                            {
                                addcInstr = new ParsedInstruction("add", 0);
                            }
                            else
                            {
                                throw new Exception("FOR loop token type mismatch - second argument not a constant or label.");
                            }
                        }
                        addcInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        addcInstr.Operands.Add(new Operand(startVal.Value, EnumConv.TokenToOperandType(startVal.Type)));
                        addr++;

                        firstPassInstrs.Add(subInstr);
                        firstPassInstrs.Add(addcInstr);

                        int currentLoopNum = forLoopNum;
                        labelLocations.Add($"@@@FOR_LOOP_NUM_{currentLoopNum}", addr);
                        forLoopNum++;
                        List<List<Token>> restOfLines = new List<List<Token>>();
                        restOfLines.AddRange(code.Skip(ln + 1));
                        ParsedPragma parsedFor = ParseFor(restOfLines, addr, currentLoopNum);

                        firstPassInstrs.AddRange(parsedFor.ParsedInstructions);
                        addr = parsedFor.NewAddrOffset;
                        ln += parsedFor.LinesParsed;

                        Operand regOp = new Operand(line[1].Value, EnumConv.TokenToOperandType(line[1].Type));
                        Operand startOp = new Operand(line[2].Value, EnumConv.TokenToOperandType(line[2].Type));
                        Operand loopOp = new Operand(line[3].Value, EnumConv.TokenToOperandType(line[3].Type));
                        Operand incOp = new Operand(line[4].Value, EnumConv.TokenToOperandType(line[4].Type));

                        List<ParsedInstruction> forInstrs = GenerateForCode(regOp, loopOp, incOp, currentLoopNum);
                        firstPassInstrs.AddRange(forInstrs);
                        addr += 10;

                        labelLocations.Add($"@@@END_FOR_LOOP_NUM_{currentLoopNum}", addr);
                    }
                    else if(op.Value != "for")
                    {
                        throw new ParserException($"ERROR: Unsupported pragma {op.Value} on line {op.LineNumber}.", op.LineNumber, 0);
                    }
                    else
                    {
                        throw new ParserException($"ERROR: Invalid number of arguments for FOR loop on line " +
                            $"{op.LineNumber}. Expected 4 arguments, received {line.Count - 1}.", op.LineNumber, 0);
                    }
                }
                else
                {
                    throw new ParserException($"ERROR: Unsupported token type {op.Type} on line {op.LineNumber}.", op.LineNumber, 0);
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
                        throw new ParserException($"ERROR: Invalid Ri operand type {instr.Operands[0].Type} for instruction {instr.Name}.",
                            instr.LineNumber, 1);
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
                            else
                            {
                                throw new ParserException($"ERROR: Invalid label {instr.Operands[1].Value} on line {instr.LineNumber}.", instr.LineNumber, 2);
                            }
                        }
                        else
                        {
                            throw new ParserException($"ERROR: Invalid Rj operand type {instr.Operands[1].Type} for instruction {instr.Name}.",
                                instr.LineNumber, 2);
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
                    {
                        if (dirDict.ContainsKey(instr.Operands[2].Value))
                            offset = int.Parse(dirDict[instr.Operands[2].Value]);
                        else
                            throw new ParserException($"ERROR: Invalid label {instr.Operands[2].Value} on line {instr.LineNumber}.", instr.LineNumber, 3);
                    }
                    else
                        throw new ParserException($"ERROR: Invalid operand type {instr.Operands[2].Type} for instruction {instr.Name} on line {instr.LineNumber}.",
                            instr.LineNumber, 3);

                    parsedOps.Add(new MemoryOperation(Ri, Rj, offset, opName));
                }
                else if (Operation.MethodOpcodes.Contains(opName))
                {
                    int offset = 0;
                    if (instr.Operands.Count > 0)
                    {
                        if (instr.Operands[0].Type == OperandType.Constant)
                            offset = int.Parse(instr.Operands[0].Value);
                        else if (instr.Operands[0].Type == OperandType.Label)
                        {
                            if (labelLocations.ContainsKey(instr.Operands[0].Value))
                                offset = labelLocations[instr.Operands[0].Value];
                            else
                                throw new ParserException($"ERROR: Invalid label {instr.Operands[0].Value} on line {instr.LineNumber}.", instr.LineNumber, 1);
                        }
                        else
                            throw new ParserException($"ERROR: Invalid operand type {instr.Operands[0].Type} for instruction {instr.Name} on line {instr.LineNumber}.",
                                instr.LineNumber, 1);
                    }

                    parsedOps.Add(new MethodOperation(offset, opName));
                }
                else if(Operation.StackOpcodes.Contains(opName))
                {
                    int Ri = int.Parse(instr.Operands[0].Value);
                    parsedOps.Add(new StackOperation(Ri, opName));
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
                    {
                        if (labelLocations.ContainsKey(instr.Operands[0].Value))
                            offset = labelLocations[instr.Operands[0].Value];
                        else
                            throw new ParserException($"ERROR: Invalid label {instr.Operands[0].Value} on line {instr.LineNumber}.", instr.LineNumber, 1);
                    }
                    else
                        throw new ParserException($"ERROR: Invalid operand type {instr.Operands[0].Type} for instruction {instr.Name} on line {instr.LineNumber}.",
                                instr.LineNumber, 1);

                    parsedOps.Add(new JumpOperation(0, offset, jmpType));
                }
            }

            return parsedOps;
        }

        private ParsedPragma ParseFor(List<List<Token>> lines, int startAddr, int currentLoopNum)
        {
            ParsedPragma result = new ParsedPragma();

            List<ParsedInstruction> firstPassInstrs = new List<ParsedInstruction>();
            int addr = startAddr;

            for(int ln = 0; ln < lines.Count; ln++)
            {
                List<Token> line = lines[ln];
                Token op = line[0];
                if (op.Type == TokenType.OpCode)
                {
                    if (mnemonicFormat.ContainsKey(op.Value))
                    {
                        MnemonicFormat instrFormat = mnemonicFormat[op.Value];
                        // Check if the number of operands is correct, remembering to
                        // account for the fact that the first token in the line was our
                        // opcode token
                        if (line.Count == instrFormat.OperandCount + 1)
                        {
                            ParsedInstruction parsedInstr = new ParsedInstruction(op.Value, op.LineNumber);
                            for (int i = 0; i < instrFormat.OperandCount; i++)
                            {
                                Token operand = line[i + 1];
                                if ((instrFormat.OperandTypes[i] & operand.Type) == operand.Type)
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
                else if (op.Type == TokenType.Label)
                {
                    labelLocations.Add(op.Value, addr);
                }
                else if (op.Type == TokenType.Pragma)
                {
                    // We'll hardcode this for now but come back and make it general later
                    if (op.Value == "for" && line.Count == 5)
                    {
                        ParsedInstruction subInstr = new ParsedInstruction("sub", 0);
                        Token loopReg = line[1];
                        if (loopReg.Type != TokenType.RegName)
                        {
                            // Throw an error
                            throw new Exception("FOR loop token type mismatch - first argument not a register.");
                        }
                        // Subtract the register from itself to clear it
                        subInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        subInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        addr++; // SUB is one word long

                        ParsedInstruction addcInstr = new ParsedInstruction("addc", 0);
                        Token startVal = line[2];
                        if (startVal.Type != TokenType.Label && startVal.Type != TokenType.Constant)
                        {
                            if (startVal.Type == TokenType.RegName)
                            {
                                addcInstr = new ParsedInstruction("add", 0);
                            }
                            else
                            {
                                throw new Exception("FOR loop token type mismatch - second argument not a constant or label.");
                            }
                        }
                        addcInstr.Operands.Add(new Operand(loopReg.Value, OperandType.Register));
                        addcInstr.Operands.Add(new Operand(startVal.Value, EnumConv.TokenToOperandType(startVal.Type)));
                        addr++;

                        firstPassInstrs.Add(subInstr);
                        firstPassInstrs.Add(addcInstr);

                        int thisLoopNum = forLoopNum;
                        labelLocations.Add($"@@@FOR_LOOP_NUM_{thisLoopNum}", addr);
                        forLoopNum++;
                        List<List<Token>> restOfLines = new List<List<Token>>();
                        restOfLines.AddRange(lines.Skip(ln + 1));
                        ParsedPragma parsedFor = ParseFor(restOfLines, addr, thisLoopNum);

                        firstPassInstrs.AddRange(parsedFor.ParsedInstructions);
                        addr = parsedFor.NewAddrOffset;
                        ln += parsedFor.LinesParsed;

                        Operand regOp = new Operand(line[1].Value, EnumConv.TokenToOperandType(line[1].Type));
                        Operand startOp = new Operand(line[2].Value, EnumConv.TokenToOperandType(line[2].Type));
                        Operand loopOp = new Operand(line[3].Value, EnumConv.TokenToOperandType(line[3].Type));
                        Operand incOp = new Operand(line[4].Value, EnumConv.TokenToOperandType(line[4].Type));

                        List<ParsedInstruction> forInstrs = GenerateForCode(regOp, loopOp, incOp, thisLoopNum);
                        firstPassInstrs.AddRange(forInstrs);
                        addr += 10;

                        labelLocations.Add($"@@@END_FOR_LOOP_NUM_{thisLoopNum}", addr);
                    }
                    else if (op.Value == "endfor")
                    {
                        result.ParsedInstructions = firstPassInstrs;
                        result.NewAddrOffset = addr;
                        result.LinesParsed = ln + 1;
                        return result;
                    }
                }
            }

            throw new Exception("Error: endfor not detected to match start of FOR loop");

            // throw new NotImplementedException();
        }

        private List<ParsedInstruction> GenerateForCode(Operand reg, Operand loopVal, Operand inc, int loopNum)
        {
            ParsedInstruction addcInstr = new ParsedInstruction("addc", 0);
            ParsedInstruction pushInstr = new ParsedInstruction("push", 0);
            ParsedInstruction subcInstr = new ParsedInstruction("subc", 0);
            ParsedInstruction popInstr = new ParsedInstruction("pop", 0);
            ParsedInstruction jzInstr = new ParsedInstruction("jz", 0);
            ParsedInstruction jnnInstr = new ParsedInstruction("jnn", 0);
            ParsedInstruction jmpInstr = new ParsedInstruction("jmp", 0);

            if(loopVal.Type != OperandType.Constant && loopVal.Type != OperandType.Label)
            {
                if (loopVal.Type == OperandType.Register)
                {
                    subcInstr = new ParsedInstruction("sub", 0);
                }
                else
                {
                    throw new Exception("FOR loop operand type mismatch - max value not a const or label.");
                }
            }

            if (inc.Type != OperandType.Constant && inc.Type != OperandType.Label)
            {
                throw new Exception("FOR loop operand type mismatch - increment value not a const or label.");
            }

            addcInstr.Operands.Add(reg);
            addcInstr.Operands.Add(inc);

            pushInstr.Operands.Add(reg);

            subcInstr.Operands.Add(reg);
            subcInstr.Operands.Add(loopVal);

            popInstr.Operands.Add(reg);

            jzInstr.Operands.Add(new Operand($"@@@END_FOR_LOOP_NUM_{loopNum}", OperandType.Label));
            jnnInstr.Operands.Add(new Operand($"@@@END_FOR_LOOP_NUM_{loopNum}", OperandType.Label));
            jmpInstr.Operands.Add(new Operand($"@@@FOR_LOOP_NUM_{loopNum}", OperandType.Label));

            List<ParsedInstruction> forInstrs = new List<ParsedInstruction>();
            forInstrs.Add(addcInstr);
            forInstrs.Add(pushInstr);
            forInstrs.Add(subcInstr);
            forInstrs.Add(popInstr);
            forInstrs.Add(jzInstr);
            forInstrs.Add(jnnInstr);
            forInstrs.Add(jmpInstr);

            return forInstrs;

            // throw new NotImplementedException();
        }
    }
}
