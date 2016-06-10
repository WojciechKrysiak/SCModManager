  
%namespace SCModManager.SCFormat       //names the Namespace of the generated Scanner-class
%visibility public       //visibility of the types "Tokens","ScanBase","Scanner"
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
{EqSign}       {EncounterEq(); return (int)Tokens.Eq;}
{Comment}      {return (int)Tokens.Skip;}


