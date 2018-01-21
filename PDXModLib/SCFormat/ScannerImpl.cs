using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.SCFormat
{
    public partial class Scanner
    {
        SCValue MakeIdentifier(string yytext)
        {
            return new SCIdentifier(yytext);
        }

        SCValue MakeVariable(string yytext)
        {
            return new SCVariable(yytext.TrimStart('@'));
        }

        SCValue MakeString(string yytext)
        {
            return new SCString(yytext.TrimStart('"').TrimEnd('"'));
        }

        SCValue MakeNumber(string yytext)
        {
            return new SCNumber(yytext);
        }

        SCValue MakePercentage(string yytext)
        {
            return new SCPercent(yytext);
        }

        SCValue MakeToken(Tokens token)
        {
            return new SCToken(token);
        }
    }
}
