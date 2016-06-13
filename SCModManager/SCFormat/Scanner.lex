  
%namespace SCModManager.SCFormat       //names the Namespace of the generated Scanner-class
%visibility internal       //visibility of the types "Tokens","ScanBase","Scanner"
%scannertype Scanner     //names the Scannerclass to "Scanner"
%scanbasetype ScanBase  //names the ScanBaseclass to "ScanBase"
%tokentype Tokens        //names the Tokenenumeration to "Tokens"

Comment #.*$
// : should be a field reference
Identifier [A-Za-z][A-Za-z0-9_.:]*
Variable @[A-Za-z][A-Za-z0-9_.:]*
String \".*\"
Number -?[0-9]+\.?[0-9]*
Percentage [0-9]+\%
OpenBrace \{
CloseBrace \}
EqSign =
LeSign <
GrSign >
LEqSign <=
GEqSign >=
NEqSign !=

 
%% //Rules Section
%{ //user-code that will be executed before getting the next token
%}

{Identifier}   {yylval = MakeIdentifier(yytext);return (int)Tokens.ID;}
{Variable}     {yylval = MakeVariable(yytext);return (int)Tokens.Var;}
{String}       {yylval = MakeString(yytext);return (int)Tokens.Str;}
{Number}       {yylval = MakeNumber(yytext);return (int)Tokens.Num;}
{Percentage}   {yylval = MakePercentage(yytext);return (int)Tokens.Perc;}
{OpenBrace}    {return (int)Tokens.OBr;}
{CloseBrace}   {return (int)Tokens.CBr;}
{EqSign}       {yylval = MakeToken(Tokens.Eq); return (int)Tokens.Eq;}
{LeSign}       {yylval = MakeToken(Tokens.Le); return (int)Tokens.Le;}
{GrSign}       {yylval = MakeToken(Tokens.Gr); return (int)Tokens.Gr;}
{LEqSign}      {yylval = MakeToken(Tokens.LEq); return (int)Tokens.LEq;}
{GEqSign}      {yylval = MakeToken(Tokens.GEq); return (int)Tokens.GEq;}
{NEqSign}      {yylval = MakeToken(Tokens.NEq); return (int)Tokens.NEq;}
{Comment}      {return (int)Tokens.Skip;}


