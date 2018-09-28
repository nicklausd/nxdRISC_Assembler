using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    public enum TokenType
    {
        SectionLabel,
        AsmDirective,
        JmpLabel,
        OpCode,
        RegName,
        Constant,
        ConstLabel,
        ConstType,
        String,
    }

    public class Token
    {
        string Value { get; }
        TokenType Type { get; }

        public Token(string value, TokenType type)
        {
            Value = value;
            Type = type;
        }
    }

    public class Tokenizer
    {
        /// <summary>
        /// Splits a line of assembly code into tokens.
        /// </summary>
        /// <param name="line">The input line.</param>
        /// <returns>A list of tokens generated from the line.</returns>
        public static List<Token> TokenizeLine(string line)
        {
            string strippedLine = line.Trim(); // Strip of unneeded whitespace
            List<Token> tokenList = new List<Token>();

            // Before splitting the line into tokens, strip out comments
            bool containsComment = false;
            if (strippedLine.Contains(';'))
            {
                int semicIndex = -1;
                containsComment = false;
                do
                {
                    semicIndex = strippedLine.IndexOf(';', semicIndex + 1);
                    if (strippedLine.Contains('"'))
                    {
                        int quoteIndex = strippedLine.IndexOf('"');
                        if (strippedLine.Substring(quoteIndex + 1).Contains('"'))
                        {
                            int endQuoteIndex = strippedLine.Substring(quoteIndex + 1).IndexOf('"') + quoteIndex + 1;
                            if (semicIndex > quoteIndex && semicIndex < endQuoteIndex)
                            {
                                // Semicolon contained within string, not a comment
                                // to be cut out.
                                containsComment = false;
                            }
                            else
                            {
                                containsComment = true;
                            }
                        }
                    }
                } while (semicIndex >= 0 || !containsComment);

                if (containsComment)
                {
                    strippedLine = strippedLine.Substring(0, semicIndex);
                }
            }

            // Now that we've stripped out comments, we'll strip out strings
            // and create an array that will get tokenized in as we loop through.

            //if(strippedLine.Contains)

            // First, we'll split the line by spaces to get raw token data
            string[] splitLine = line.Split(' ');

            // Go through each potential raw token
            int i = 0;
            foreach(string raw in splitLine)
            {
                i++; // We'll increment first since we're never indexing using
                     // this variable, it only refers to line numbers
                string rawStripped = raw.Trim();
                //bool containsComment = false;
                // Check if our potential token might include comments
                if(rawStripped.Contains(';'))
                {
                    int semicIndex = rawStripped.IndexOf(';');
                    containsComment = true;
                    // Since we allow string tokens, we need to check if there
                    // are quotation marks surrounding our semicolon to not
                    // accidentally break strings containing semicolons.
                    if(rawStripped.Contains('"'))
                    {
                        int quoteIndex = rawStripped.IndexOf('"');
                        if(rawStripped.Substring(quoteIndex+1).Contains('"'))
                        {
                            int endQuoteIndex = rawStripped.Substring(quoteIndex + 1).IndexOf('"') + quoteIndex + 1;
                            if(semicIndex > quoteIndex && semicIndex < endQuoteIndex)
                            {
                                // Semicolon contained within string, not a comment
                                // to be cut out.
                                containsComment = false;
                            }
                        }
                    }
                    
                    if(containsComment)
                    {
                        rawStripped = rawStripped.Substring(0, semicIndex);
                    }
                }

                // Now that we've stripped any extra potential whitespace and
                // removed comments, proceed to determine token types.
                // First token type we'll look for is section labels.

                List<string> sectLabels = new List<string>()
                {
                    ".directives", ".enddirectives",
                    ".constants", ".endconstants",
                    ".code", ".endcode",
                };

                if(sectLabels.Contains(rawStripped.ToLower()))
                {
                    tokenList.Add(new Token(rawStripped.ToLower(), TokenType.SectionLabel));
                    continue;
                }

                // Next we'll look for ASM directives.
                // The only directive we have right now is .equ
                List<string> asmDirectives = new List<string>()
                {
                    ".equ",
                };

                if(asmDirectives.Contains(rawStripped.ToLower()))
                {
                    tokenList.Add(new Token(rawStripped.ToLower(), TokenType.AsmDirective));
                    continue;
                }

                // After ASM directives we'll check for constants in the
                // .constants section.

                List<string> constTypes = new List<string>()
                {
                    ".word", ".char",
                };

                if (constTypes.Contains(rawStripped.ToLower()))
                {
                    tokenList.Add(new Token(rawStripped.ToLower(), TokenType.ConstType));
                    continue;
                }

                // Look for OpCodes now

                List<string> opCodes = new List<string>()
                {
                    "add", "addc", "sub", "subc",
                    "and", "or", "not", "shra",
                    "rotr"
                };

                if(opCodes.Contains(rawStripped.ToLower()))
                {
                    tokenList.Add(new Token(rawStripped.ToLower(), TokenType.OpCode));
                    continue;
                }

                // Check to see if the token is a constant number. We need to do size
                // checking on constants, as our word is only 14 bits long. This can be
                // done by subtracting 0x3FFF from the number and if the result is
                // greater than zero than the number is out of bounds.

                int outNum = 0;
                if (int.TryParse(rawStripped, out outNum))
                {
                    if(outNum - 0x3FFF <= 0)
                    {
                        tokenList.Add(new Token(outNum.ToString(), TokenType.Constant));
                        continue;
                    }
                    else
                    {
                        // Constant is out of bounds - raise an error
                        throw new TokenizerException("Constant number is out of bounds. Constant "
                            + "numbers are constrained to a 14-bit word and must be a value "
                            + "between 0x0000 and 0x3FFF.", rawStripped);
                    }
                }


                
            }

            return null;
        }
    }

    public class TokenizerException : Exception
    {
        public string RawTokenValue { get; }

        public TokenizerException(string message, string rawToken) : base(message)
        {
            RawTokenValue = rawToken;
        }
    }
}
