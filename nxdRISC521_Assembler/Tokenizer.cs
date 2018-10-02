using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace nxdRISC521_Assembler
{
    public enum TokenType
    {
        SectionLabel,
        AsmDirective,
        Label,
        OpCode,
        RegName,
        Constant,
        ConstType,
        String,
    }

    public class Token
    {
        public string Value { get; }
        public TokenType Type { get; }

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
            // Let's also remove any commas from the token, since they denote
            // a separation between operands and aren't needed
            strippedLine = strippedLine.Replace(',', ' ').Trim();
            List<Token> tokenList = new List<Token>();

            // Before splitting the line into tokens, strip out comments
            bool containsComment = false;
            if (strippedLine.Contains(';'))
            {
                // We'll worry about string handling a different time
                /*int semicIndex = -1;
                containsComment = true;
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
                } while (semicIndex >= 0 || !containsComment);*/

                int semicIndex = strippedLine.IndexOf(';');

                strippedLine = strippedLine.Substring(0, semicIndex);
            }

            // Now that we've stripped out comments, we'll strip out strings
            // and create an array that will get tokenized in as we loop through.
            // We'll replace all instances of a string with $x$ where x is the
            // index in the strings list of the string, and then during tokenization
            // we'll check those values and throw the string back in as a token.
            // Stripping out strings is untested and ultimately unneeded for the class
            // but is a worthwhile extension to make for a fun side project.

            Regex strRegex = new Regex("\\$[0-9]+\\$");
            List<string> tokenStrings = new List<string>();

            while(strippedLine.Contains('"'))
            {
                int quoteIndex = strippedLine.IndexOf('"');
                if(strippedLine.Substring(quoteIndex + 1).Contains('"'))
                {
                    int endQuoteIndex = strippedLine.Substring(quoteIndex + 1).IndexOf('"');
                    string tokenString = strippedLine.Substring(quoteIndex + 1, endQuoteIndex - quoteIndex - 1);
                    tokenStrings.Add(tokenString);
                    strippedLine = strippedLine.Remove(quoteIndex, 1 + endQuoteIndex - quoteIndex);
                    strippedLine.Insert(quoteIndex, $"${tokenStrings.Count - 1}$");
                }
            }

            // First, we'll split the line by spaces to get raw token data
            string[] splitLine = strippedLine.Split(' ');

            if (strippedLine.Length == 0 || splitLine.Length == 0)
                return null;

            Regex registerRegex = new Regex("(r|R)[0-9]+");

            // Go through each potential raw token
            foreach(string raw in splitLine)
            {
                string rawStripped = raw.Trim();

                if (rawStripped.Length == 0) continue;

                // Now that we've stripped any extra potential whitespace and
                // removed comments, proceed to determine token types.

                // Before any other types are removed, we want to check for our
                // strings that we pulled out earlier.

                if(strRegex.IsMatch(rawStripped))
                {
                    string indexStr = rawStripped.Replace('$', ' ').Trim();
                    int index = 0;
                    if(int.TryParse(indexStr, out index))
                    {
                        tokenList.Add(new Token(tokenStrings[index], TokenType.String));
                        continue;
                    }
                    else
                    {
                        throw new TokenizerException("Token contains invalid character: '$'.", rawStripped);
                    }
                }

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
                else if (rawStripped.Length > 2 && rawStripped.ToLower().Substring(0, 2) == "0x")
                {
                    string hexString = rawStripped.ToLower().Substring(2);
                    if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out outNum))
                    {
                        if (outNum - 0x3FFF <= 0)
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
                    else
                    {
                        throw new TokenizerException("Invalid constant hex value. Hex values may only "
                            + "contain digits of 0-9 and A-F.", rawStripped);
                    }
                }

                Match registerMatch = registerRegex.Match(rawStripped);
                // Check if token is a register reference
                if(registerMatch.Success)
                {
                    tokenList.Add(new Token(registerMatch.Value, TokenType.RegName));
                    continue;
                }

                // For now, we'll leave other token types out and implement them as needed.
            }

            return tokenList;
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
