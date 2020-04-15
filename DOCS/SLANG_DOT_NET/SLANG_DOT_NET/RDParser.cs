////////////////////////////////////////////////////////
//
//  This software is released as per the clauses of MIT License
//
// 
//  The MIT License
//
//  Copyright (c) 2010, Praseed Pai K.T.
//                      http://praseedp.blogspot.com
//                      praseedp@yahoo.com  
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SLANG_DOT_NET
{
    /// <summary>
    /// 
    /// </summary>
    public class RDParser : Lexer
    {

        /// <summary>
        ///    The Final outcome of the parser is a group of 
        ///    functions.
        /// </summary>
        protected TModuleBuilder prog = null;
        /// <summary>
        ///    
        /// </summary>
        /// <param name="str"></param>

        public RDParser(String str)
            : base(str)
        {
            prog = new TModuleBuilder(); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Exp BExpr(ProcedureBuilder pb)
        {
            TOKEN l_token;
            Exp RetValue = LExpr(pb);
            while (Current_Token == TOKEN.TOK_AND || Current_Token == TOKEN.TOK_OR)
            {
                l_token = Current_Token;
                Current_Token = GetNext();
                Exp e2 = LExpr(pb);
                RetValue = new LogicalExp(l_token, RetValue, e2);

            }
            return RetValue;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>

        public Exp LExpr(ProcedureBuilder pb)
        {
            TOKEN l_token;
            Exp RetValue = Expr(pb);
            while (Current_Token == TOKEN.TOK_GT ||
                    Current_Token == TOKEN.TOK_LT ||
                    Current_Token == TOKEN.TOK_GTE ||
                    Current_Token == TOKEN.TOK_LTE ||
                    Current_Token == TOKEN.TOK_NEQ ||
                    Current_Token == TOKEN.TOK_EQ)
            {
                l_token = Current_Token;
                Current_Token = GetNext();
                Exp e2 = Expr(pb);
                RELATION_OPERATOR relop = GetRelOp(l_token);
                RetValue = new RelationExp(relop, RetValue, e2);


            }
            return RetValue;

        }

        /// <summary>
        ///    <Expr>  ::=  <Term> | <Term> { + | - } <Expr>
        ///    
        /// </summary>
        /// <returns></returns>
        public Exp Expr(ProcedureBuilder ctx)
        {
            TOKEN l_token;
            Exp RetValue = Term(ctx);
            while (Current_Token == TOKEN.TOK_PLUS || Current_Token == TOKEN.TOK_SUB)
            {
                l_token = Current_Token;
                Current_Token = GetToken();
                Exp e1 = Expr(ctx);

                if (l_token == TOKEN.TOK_PLUS)
                    RetValue = new BinaryPlus(RetValue, e1);
                else
                    RetValue = new BinaryMinus(RetValue, e1);
            }

            return RetValue;

        }
        /// <summary>
        /// <Term> ::=  <Factor> | <Factor>  {*|/} <Term>
        /// </summary>
        public Exp Term(ProcedureBuilder ctx)
        {
            TOKEN l_token;
            Exp RetValue = Factor(ctx);

            while (Current_Token == TOKEN.TOK_MUL || Current_Token == TOKEN.TOK_DIV)
            {
                l_token = Current_Token;
                Current_Token = GetToken();


                Exp e1 = Term(ctx);
                if (l_token == TOKEN.TOK_MUL)
                    RetValue = new Mul(RetValue, e1);
                else
                    RetValue = new Div(RetValue, e1);

            }

            return RetValue;
        }

        /// <summary>
        ///     <Factor>::=  <number> | ( <expr> ) | {+|-} <factor>
        ///           <variable> | TRUE | FALSE
        /// </summary>
        public Exp Factor(ProcedureBuilder ctx)
        {
            TOKEN l_token;
            Exp RetValue = null;



            if (Current_Token == TOKEN.TOK_NUMERIC)
            {

                RetValue = new NumericConstant(GetNumber());
                Current_Token = GetToken();

            }
            else if (Current_Token == TOKEN.TOK_STRING)
            {
                RetValue = new StringLiteral(last_str);
                Current_Token = GetToken();
            }
            else if (Current_Token == TOKEN.TOK_BOOL_FALSE ||
                      Current_Token == TOKEN.TOK_BOOL_TRUE)
            {
                RetValue = new BooleanConstant(
                    Current_Token == TOKEN.TOK_BOOL_TRUE ? true : false);
                Current_Token = GetToken();
            }
            else if (Current_Token == TOKEN.TOK_OPAREN)
            {

                Current_Token = GetToken();

                RetValue = BExpr(ctx);  // Recurse

                if (Current_Token != TOKEN.TOK_CPAREN)
                {
                    Console.WriteLine("Missing Closing Parenthesis\n");
                    throw new CParserException(-100,"Missing Closing Parenthesis\n",SaveIndex());

                }
                Current_Token = GetToken();
            }

            else if (Current_Token == TOKEN.TOK_PLUS || Current_Token == TOKEN.TOK_SUB)
            {
                l_token = Current_Token;
                Current_Token = GetToken();
                RetValue = Factor(ctx);
                if (l_token == TOKEN.TOK_PLUS)
                    RetValue = new UnaryPlus(RetValue);
                else
                    RetValue = new UnaryMinus(RetValue);
            
            }
            else if (Current_Token == TOKEN.TOK_NOT)
            {
                l_token = Current_Token;
                Current_Token = GetToken();
                RetValue = Factor(ctx);

                RetValue = new LogicalNot(RetValue);
            }
            else if (Current_Token == TOKEN.TOK_UNQUOTED_STRING)
            {
                String str = base.last_str;


                if (!prog.IsFunction(str))
                {
                    //
                    // if it is not a function..it ought to 
                    // be a variable...
                    SYMBOL_INFO inf = ctx.GetSymbol(str);

                    if (inf == null)
                        throw new CParserException(-100,"Undefined symbol "+str,SaveIndex());

                    if (inf.Type != TYPE_INFO.TYPE_ARRAY &&
                         inf.Type != TYPE_INFO.TYPE_MAP)
                    {

                        GetNext();
                        return new Variable(inf);
                    }

                    if (inf.Type == TYPE_INFO.TYPE_ARRAY)
                    {
                        GetNext();
                        if (Current_Token != TOKEN.TOK_OSUBSCRIPT)
                        {
                            CSyntaxErrorLog.AddLine("[ expected");
                            CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                            throw new CParserException(-100, "[ expected", SaveIndex());
                        }
                        GetNext();
                        Exp index = BExpr(ctx);
                        if (Current_Token != TOKEN.TOK_CSUBSCRIPT)
                        {
                            CSyntaxErrorLog.AddLine("] expected");
                            CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                            throw new CParserException(-100, "] expected", SaveIndex());
                        }

                        GetNext();
                        return new IndexedExp(new IndexedVariable(inf,index,null));
                    }
                    else
                    {
                        GetNext();

                        if (Current_Token != TOKEN.TOK_OSUBSCRIPT)
                        {
                            CSyntaxErrorLog.AddLine("[ expected");
                            CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                            throw new CParserException(-100, "[ expected", SaveIndex());
                        }
                        GetNext();
                        Exp index = BExpr(ctx);

                        if (Current_Token != TOKEN.TOK_CSUBSCRIPT)
                        {
                            CSyntaxErrorLog.AddLine("] expected");
                            CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                            throw new CParserException(-100, "] expected", SaveIndex());
                        }

                        GetNext();
                        return new HashExp(new HashedVariable(inf,index,null));
                    }


                }

                //
                // P can be null , if we are parsing a
                // recursive function call
                //
                Procedure p = prog.GetProc(str);
                // It is a Function Call
                // Parse the function invocation
                //
                Exp ptr = ParseCallProc(ctx, p);
                GetNext();
                return ptr;
            }





            else
            {

                //Console.WriteLine("Illegal Token");
                throw new CParserException(-100,"Illegal Token",SaveIndex());
            }


            return RetValue;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pb"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public Exp ParseCallProc(ProcedureBuilder pb, Procedure p)
        {
            GetNext();

            if (Current_Token != TOKEN.TOK_OPAREN)
            {
                throw new CParserException(-1,"Opening Parenthesis expected",SaveIndex());
            }

            GetNext();

            ArrayList actualparams = new ArrayList();

            if (Current_Token != TOKEN.TOK_CPAREN)
            {



                while (true)
                {
                    // Evaluate Each Expression in the 
                    // parameter list and populate actualparams
                    // list
                    Exp exp = BExpr(pb);
                    // do type analysis
                    exp.TypeCheck(pb.Context);
                    // if , there are more parameters
                    if (Current_Token == TOKEN.TOK_COMMA)
                    {
                        actualparams.Add(exp);
                        GetNext();
                        continue;
                    }


                    if (Current_Token != TOKEN.TOK_CPAREN)
                    {
                        throw new CParserException(-100,"Expected paranthesis",SaveIndex());
                    }

                    else
                    {
                        // Add the last parameters
                        actualparams.Add(exp);
                        break;

                    }
                }

            }
           
            // if p is null , that means it is a 
            // recursive call. Being a one pass 
            // compiler , we need to wait till 
            // the parse process to be over to
            // resolve the Procedure.
            //
            //
            if (p != null)
                return new CallExp(p, actualparams);
            else
                return new CallExp(pb.Name, 
                                   true,  // recurse !
                                   actualparams);

            

        }


        /// <summary>
        ///   The new Parser entry point
        /// </summary>
        /// <returns></returns>
        public TModule DoParse()
        {
            try
            {
                GetNext();   // Get The First Valid Token

                if (Current_Token != TOKEN.TOK_FUNCTION)
                {
                    throw new CParserException(-100, "Function Keyword expected ", SaveIndex());
                }

                return ParseFunctions();
            }
            catch (CParserException e)
            {
             // Console.WriteLine(e.ToString() + "@" + "=>");
                Console.WriteLine(e.GetErrorString() + " @ " + base.GetLineNo(e.GetLexicalOffset()));
                Console.WriteLine("==>" + base.GetCurrentLine(e.GetLexicalOffset()));
                return null;
            }
            catch (Exception e)
            {
                // Console.WriteLine("Parse Error -------");
                //  Console.WriteLine(e.ToString());
                return null;
            }
        }
       
        /// <summary>
        ///    While There are more functions to parse
        /// </summary>
        /// <returns></returns>
        public TModule ParseFunctions()
        {
            bool error_state = false;
            while (Current_Token == TOKEN.TOK_FUNCTION)
            {
                ProcedureBuilder b = ParseFunction();
                Procedure s = b.GetProcedure();

              //  if (s == null)
              //  {
              //      Console.WriteLine("Error While Parsing Functions");
             //       return null;
            //    }
                if (s != null)
                {
                    if (prog.IsCompiledFunction(s.Name))
                    {
                        Console.WriteLine("Warning : Duplicate function ....! " + s.Name);
                        throw new CParserException(-1, "Duplicate Function..", SaveIndex());
                    }

                    prog.Add(s);
                    GetNext();
                }
                else
                {
                    error_state = true;
                    while (GetNext() != TOKEN.TOK_FUNCTION)
                    {
                        if (Current_Token == TOKEN.ILLEGAL_TOKEN)
                        {
                            throw new CParserException(-100, "Compilation Error  ", SaveIndex());
                        }
                    }



                }
               
            }

            if (Current_Token != TOKEN.TOK_NULL)
            {

                throw new CParserException(-100, "Failed to compile the whole program ", SaveIndex());
            }
            if (Current_Token == TOKEN.ILLEGAL_TOKEN)
            {

                throw new CParserException(-100, "Illegal token , terminating compilation ", SaveIndex());
            }
            if (error_state == true)
            {
                throw new CParserException(-100, "Compilation Error  ", SaveIndex());
            }

            //
            //  Convert the builder into a program
            //
            return prog.GetProgram();
        }

        /// <summary>
        ///    Parse A Single Function.
        /// </summary>
        /// <returns></returns>
        protected ProcedureBuilder ParseFunction()
        {
            //
            // Create a Procedure builder Object
            //
            ProcedureBuilder p = new ProcedureBuilder("", new COMPILATION_CONTEXT());
            if (Current_Token != TOKEN.TOK_FUNCTION)
                throw new CParserException(-1,"FUNCTION expected " , SaveIndex());


            GetNext();
            // return type of the Procedure ought to be 
            // Boolean , Numeric or String 
            if (!(Current_Token == TOKEN.TOK_VAR_BOOL ||
                Current_Token == TOKEN.TOK_VAR_NUMBER ||
                Current_Token == TOKEN.TOK_VAR_STRING ||
                Current_Token == TOKEN.TOK_ARRAY ||
                Current_Token == TOKEN.TOK_MAP))
            {
                throw new CParserException(-1, "A Legal data type expected ", SaveIndex());
                

            }

            if (Current_Token == TOKEN.TOK_MAP ||
                 Current_Token == TOKEN.TOK_ARRAY)
            {
                throw new CParserException(-1, "Array / Map not supported as return value", SaveIndex());

            }
            else
            {
                //-------- Assign the return type
                p.TYPE = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                    TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                    TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;
            }

            // Parse the name of the Function call
            GetNext();
            if (Current_Token != TOKEN.TOK_UNQUOTED_STRING)
                throw new CParserException(-1, "Function name expected ", SaveIndex());
                
            p.Name = this.last_str; // assign the name

            // ---------- Opening parenthesis for 
            // the start of <paramlist>
            GetNext();
            if (Current_Token != TOKEN.TOK_OPAREN)
            {
                throw new CParserException(-1, "Opening  Parenthesis expected", SaveIndex());
            }

            //---- Parse the Formal Parameter list
            FormalParameters(p);



            if (Current_Token != TOKEN.TOK_CPAREN)
            {
                throw new CParserException(-1, "Closing Parenthesis expected", SaveIndex());
           
            }

            GetNext();

            // --------- Parse the Function code
            ArrayList lst = StatementList(p);

            if (Current_Token != TOKEN.TOK_END)
            {
                throw new CParserException(-1,"END expected",SaveIndex());
            }

            // Accumulate all statements to 
            // Procedure builder
            //
            foreach (Stmt s in lst)
            {
                p.AddStatement(s);

            }
            return p;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected SYMBOL_INFO ParseArray()
        {
            return null;
        }

        protected SYMBOL_INFO ParseMap()
        {
            return null;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="pb"></param>
        protected void FormalParameters(ProcedureBuilder pb)
        {

            if (Current_Token != TOKEN.TOK_OPAREN)
                throw new  CParserException(-1,"Opening Paranthesis expected" , SaveIndex());
            GetNext();

            ArrayList lst_types = new ArrayList();

            while (Current_Token == TOKEN.TOK_VAR_BOOL ||
                Current_Token == TOKEN.TOK_VAR_NUMBER ||
                Current_Token == TOKEN.TOK_VAR_STRING || 
                Current_Token == TOKEN.TOK_ARRAY ||
                Current_Token == TOKEN.TOK_MAP)
            {

                if (Current_Token == TOKEN.TOK_MAP ||
                     Current_Token == TOKEN.TOK_ARRAY)
                {

                    throw new CParserException(-1, "Array/Map not accepted as parameters", SaveIndex());
                }
                SYMBOL_INFO inf = new SYMBOL_INFO();

                inf.Type = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                    TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                    TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;





                GetNext();
                if (Current_Token != TOKEN.TOK_UNQUOTED_STRING)
                {
                    throw new CParserException(-1,"Variable Name expected",SaveIndex());
                }

                inf.SymbolName = this.last_str;
                lst_types.Add(inf.Type);
                pb.AddFormals(inf);
                pb.AddLocal(inf);


                GetNext();

                if (Current_Token != TOKEN.TOK_COMMA)
                {
                    break;
                }
                GetNext();
            }

            prog.AddFunctionProtoType(pb.Name, pb.TYPE, lst_types);
            return;


        }

       

        /// <summary>
        ///  The Grammar is 
        ///  
        ///  <stmts> :=  { stmt }+
        ///  {stmt}  :=  <vardeclstmt> | 
        ///              <printstmt>|<assignmentstmt>|
        ///              <ifstmt>| <whilestmt> |
        ///              <printlinestmt>  |
        ///              <returnstmt>
        ///                            
        ///
        ///   <vardeclstmt> ::=  <type>  var_name;
        ///   <printstmt> := PRINT <expr>;
        ///   <assignmentstmt>:= <variable> = value;
        ///   <ifstmt>::= IF  <expr> THEN <stmts> [ ELSE  <stmts> ] ENDIF
        ///   <whilestmt>::=  WHILE  <expr> <stmts> WEND
        ///   <returnstmt>:= Return <expr>
        ///    <type> := NUMERIC | STRING | BOOLEAN
        ///    
        ///    <expr> ::=  <BExpr>
        ///    <BExpr> ::= <LExpr> LOGIC_OP <BExpr>
        ///    <LExpr> ::= <RExpr> REL_OP   <LExpr>
        ///    <RExpr> ::= <Term> ADD_OP <RExpr>
        ///    <Term>::= <Factor>  MUL_OP <Term>
        ///    <Factor>  ::= <Numeric>  |  
        ///                  <String> | 
        ///                  TRUE | 
        ///                  FALSE | 
        ///                  <variable> | 
        ///                  ‘(‘ <expr> ‘)’ |
        ///                  {+|-|!} <Factor>  
        ///     
        ///     <LOGIC_OP> := '&&'  | ‘||’
        ///     <REL_OP> := '>' |' < '|' >=' |' <=' |' <>' |' =='
        ///     <MUL_OP> :=  '*' |' /'
        ///     <ADD_OP>  :=  '+' |' -'
        /// </summary>
        /// <returns></returns>
        protected ArrayList StatementList(ProcedureBuilder ctx)
        {
            ArrayList arr = new ArrayList();
            //
            //  StatementList Parses a block and a block can 
            //  end with
            //      ELSE
            //      ENDIF
            //      WEND  ( End of While )
            //      END   ( end of Function )
            //
            while (
                    (Current_Token != TOKEN.TOK_ELSE) &&
                    (Current_Token != TOKEN.TOK_ENDIF) &&
                    (Current_Token != TOKEN.TOK_WEND) &&
                    (Current_Token != TOKEN.TOK_END)
              )
            {
                Stmt temp = Statement(ctx);
                if (temp != null)
                {
                    arr.Add(temp);
                }
            }
            return arr;
        }

        /// <summary>
        ///    This Routine Queries Statement Type 
        ///    to take the appropriate Branch...
        ///    Currently , only Print and PrintLine statement
        ///    are supported..
        ///    if a line does not start with Print or PrintLine ..
        ///    an exception is thrown
        /// </summary>
        /// <returns></returns>
        private Stmt Statement(ProcedureBuilder ctx)
        {
            Stmt retval = null;
            switch (Current_Token)
            {
                case TOKEN.TOK_VAR_STRING:
                case TOKEN.TOK_VAR_NUMBER:
                case TOKEN.TOK_VAR_BOOL:
                    retval = ParseVariableDeclStatement(ctx);
                    GetNext();
                    return retval;
                case TOKEN.TOK_ARRAY:
                    retval = ParseArrayDeclStatement(ctx);
                    GetNext();
                    return retval;
                case TOKEN.TOK_MAP:
                    retval = ParseMapDeclStatement(ctx);
                    GetNext();
                    return retval;
                case TOKEN.TOK_PRINT:
                    retval = ParsePrintStatement(ctx);
                    GetNext();
                    break;
                case TOKEN.TOK_PRINTLN:
                    retval = ParsePrintLNStatement(ctx);
                    GetNext();
                    break;
                case TOKEN.TOK_UNQUOTED_STRING:
                    {
                        if (!prog.IsFunction(base.last_str))
                        {
                            retval = ParseAssignmentStatement(ctx);
                            GetNext();
                            return retval;
                        }
                        else
                        {
                            Exp ex = BExpr(ctx);
                            retval = new ExpStmt(ex);
                            GetNext();
                            return retval;



                        }
                    }
                   

                case TOKEN.TOK_IF:
                    retval = ParseIfStatement(ctx);
                    GetNext();
                    return retval;

                case TOKEN.TOK_WHILE:
                    retval = ParseWhileStatement(ctx);
                    GetNext();
                    return retval;
                case TOKEN.TOK_RETURN:
                    retval = ParseReturnStatement(ctx);
                    GetNext();
                    return retval; 
                
  
                default:
                    throw new CParserException(-1,"Invalid statement",SaveIndex());

            }
            return retval;
        }
        /// <summary>
        ///    Parse the Print Staement .. The grammar is 
        ///    PRINT <expr> ;
        ///    Once you are in this subroutine , we are expecting 
        ///    a valid expression ( which will be compiled ) and a
        ///    semi collon to terminate the line..
        ///    Once Parse Process is successful , we create a PrintStatement
        ///    Object..
        /// </summary>
        /// <returns></returns>
        private Stmt ParsePrintStatement(ProcedureBuilder ctx)
        {
            GetNext();
            Exp a = BExpr(ctx);
            //
            // Do the type analysis ...
            //
            a.TypeCheck(ctx.Context); 

            if (Current_Token != TOKEN.TOK_SEMI)
            {
                throw new CParserException(-1,"; is expected",SaveIndex());
            }
            return new PrintStatement(a);
        }
        /// <summary>
        ///    Parse the PrintLine Staement .. The grammar is 
        ///    PRINTLINE <expr> ;
        ///    Once you are in this subroutine , we are expecting 
        ///    a valid expression ( which will be compiled ) and a
        ///    semi collon to terminate the line..
        ///    Once Parse Process is successful , we create a PrintLineStatement
        ///    Object..
        /// </summary>
        /// <returns></returns>
        private Stmt ParsePrintLNStatement(ProcedureBuilder ctx)
        {
            GetNext();
            Exp a = BExpr(ctx);
            a.TypeCheck(ctx.Context); 
            if (Current_Token != TOKEN.TOK_SEMI)
            {
                throw new CParserException(-1,"; is expected",SaveIndex());
            }
            return new PrintLineStatement(a);
        }

        /// <summary>
        ///    Parse Variable declaration statement
        /// </summary>
        /// <param name="type"></param>

        public Stmt ParseVariableDeclStatement(ProcedureBuilder ctx)
        {
            
            //--- Save the Data type 
            TOKEN tok = Current_Token;

            // --- Skip to the next token , the token ought 
            // to be a Variable name ( UnQouted String )
            GetNext();

            if (Current_Token == TOKEN.TOK_UNQUOTED_STRING)
            {
                SYMBOL_INFO symb = new SYMBOL_INFO();
                symb.SymbolName = base.last_str;
                symb.Type = (tok == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (tok == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;

                //---------- Skip to Expect the SemiColon
                
                GetNext();



                if (Current_Token == TOKEN.TOK_SEMI)
                {
                    // ----------- Add to the Symbol Table
                    // for type analysis 
                    ctx.TABLE.Add(symb); 

                    // --------- return the Object of type
                    // --------- VariableDeclStatement
                    // This will just store the Variable name
                    // to be looked up in the above table
                    return new VariableDeclStatement(symb);
                }
                else
                {
                    CSyntaxErrorLog.AddLine("; expected");
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, ", or ; expected", SaveIndex());
                }
            }
            else
            {

                CSyntaxErrorLog.AddLine("invalid variable declaration");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, ", or ; expected", SaveIndex());
            }








        }
        /// <summary>
        ///    Parse the Assignment Statement 
        ///    <variable> = <expr>
        /// </summary>
        /// <param name="pb"></param>
        /// <returns></returns>
        public Stmt ParseAssignmentStatement(ProcedureBuilder ctx)
        {

            //
            // Retrieve the variable and look it up in 
            // the symbol table ..if not found throw exception
            //
            string variable = base.last_str;
            SYMBOL_INFO s = ctx.TABLE.Get(variable);
            Exp index_expression = null; 

            if (s == null)
            {
                CSyntaxErrorLog.AddLine("Variable not found  " + last_str);
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, "Variable not found", SaveIndex());

            }


            if (s.Type == TYPE_INFO.TYPE_ARRAY || s.Type == TYPE_INFO.TYPE_MAP)
            {
                GetNext();

                if (Current_Token != TOKEN.TOK_OSUBSCRIPT)
                {
                    CSyntaxErrorLog.AddLine("[ expected " );
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, "[ expected ", SaveIndex());

                }

                GetNext();
                Exp a = BExpr(ctx);
                //
                // Do the type analysis ...
                //
                TYPE_INFO tp = a.TypeCheck(ctx.Context);

                if (s.Type == TYPE_INFO.TYPE_MAP)
                {
                    if (tp != s.m_info.key)
                    {
                        CSyntaxErrorLog.AddLine("Type mismatch in key ");
                        CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                        throw new CParserException(-100, "Type mismatch in key ", SaveIndex());
                    }
                }
                else
                {
                    if (tp != s.a_info.tf)
                    {
                        CSyntaxErrorLog.AddLine("Type mismatch in index ");
                        CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                        throw new CParserException(-100, "Type mismatch in index ", SaveIndex());
                    }

                }

                index_expression = a;

                if (Current_Token != TOKEN.TOK_CSUBSCRIPT)
                {
                    throw new CParserException(-100,"] is expected",SaveIndex());
                }

               

            }
            
            //------------ The next token ought to be an assignment
            // expression....



            GetNext();

            if (Current_Token != TOKEN.TOK_ASSIGN)
            {

                CSyntaxErrorLog.AddLine("= expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, "= expected", SaveIndex());

            }

            //-------- Skip the token to start the expression
            // parsing on the RHS
            GetNext();
            Exp exp = BExpr(ctx);

            //------------ Do the type analysis ...
            if (s.Type == TYPE_INFO.TYPE_BOOL || s.Type == TYPE_INFO.TYPE_NUMERIC ||
                s.Type == TYPE_INFO.TYPE_STRING)
            {
                if (exp.TypeCheck(ctx.Context) != s.Type)
                {
                    throw new CParserException(-1,"Type mismatch in assignment",SaveIndex());

                }
            }
            else
            {
                exp.TypeCheck(ctx.Context);
                

            }

            // -------------- End of statement ( ; ) is expected
            if (Current_Token != TOKEN.TOK_SEMI)
            {
                CSyntaxErrorLog.AddLine("; expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " ; expected", SaveIndex());

            }
            // return an instance of AssignmentStatement node..
            //   s => Symbol info associated with variable
            //   exp => to evaluated and assigned to symbol_info
            if (index_expression == null)
                return new AssignmentStatement(s, exp);
            else
                return new AssignmentStatement(s, index_expression, exp);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pb"></param>
        /// <returns></returns>
        public Stmt ParseIfStatement(ProcedureBuilder pb)
        {
            GetNext();
            ArrayList true_part = null;
            ArrayList false_part = null;
            Exp exp = BExpr(pb);  // Evaluate Expression


            if (pb.TypeCheck(exp) != TYPE_INFO.TYPE_BOOL)
            {
                throw new CParserException(-1,"Expects a boolean expression",SaveIndex());

            }


            if (Current_Token != TOKEN.TOK_THEN)
            {
                CSyntaxErrorLog.AddLine(" Then Expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, "Then Expected", SaveIndex());

            }

            GetNext();

            true_part = StatementList(pb);

            if (Current_Token == TOKEN.TOK_ENDIF)
            {
                return new IfStatement(exp, true_part, false_part);
            }


            if (Current_Token != TOKEN.TOK_ELSE)
            {

                throw new CParserException(-1,"ELSE expected",SaveIndex());
            }

            GetNext();
            false_part = StatementList(pb);

            if (Current_Token != TOKEN.TOK_ENDIF)
            {
                throw new CParserException(-1,"ENDIF EXPECTED",SaveIndex());

            }

            return new IfStatement(exp, true_part, false_part);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pb"></param>
        /// <returns></returns>
        public Stmt ParseWhileStatement(ProcedureBuilder pb)
        {

            GetNext();

            Exp exp = BExpr(pb);
            if (pb.TypeCheck(exp) != TYPE_INFO.TYPE_BOOL)
            {
                throw new CParserException(-100,"Expects a boolean expression",SaveIndex());

            }

            ArrayList body = StatementList(pb);
            if ((Current_Token != TOKEN.TOK_WEND))
            {
                CSyntaxErrorLog.AddLine("Wend Expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, "Wend Expected", SaveIndex());

            }


            return new WhileStatement(exp, body);

        }
        /// <summary>
        ///   
        /// </summary>
        /// <param name="pb"></param>
        /// <returns></returns>
        public Stmt ParseReturnStatement(ProcedureBuilder pb)
        {

            GetNext();
            Exp exp = BExpr(pb);
            if (Current_Token != TOKEN.TOK_SEMI)
            {
                CSyntaxErrorLog.AddLine("   ; expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " ; expected", -1);

            }

            if (pb.TypeCheck(exp) != pb.TYPE)
            {
                throw new CParserException(-100, " Type mismatch in return value", -1);
            }
            
            return new ReturnStatement(exp);

        }
        /// <summary>
        ///    Convert a token to Relational Operator
        /// </summary>
        /// <param name="tok"></param>
        /// <returns></returns>
        private RELATION_OPERATOR GetRelOp(TOKEN tok)
        {
            if (tok == TOKEN.TOK_EQ)
                return RELATION_OPERATOR.TOK_EQ;
            else if (tok == TOKEN.TOK_NEQ)
                return RELATION_OPERATOR.TOK_NEQ;
            else if (tok == TOKEN.TOK_GT)
                return RELATION_OPERATOR.TOK_GT;
            else if (tok == TOKEN.TOK_GTE)
                return RELATION_OPERATOR.TOK_GTE;
            else if (tok == TOKEN.TOK_LT)
                return RELATION_OPERATOR.TOK_LT;
            else
                return RELATION_OPERATOR.TOK_LTE;


        }

        public Stmt ParseArrayDeclStatement(ProcedureBuilder pb)
        {
            ARRAY_INFO t_info = new ARRAY_INFO();

            
            GetNext();

            if (Current_Token != TOKEN.TOK_LT)
            {
                CSyntaxErrorLog.AddLine("< expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " < expected", -1);
            }

            GetNext();

            if (!(Current_Token == TOKEN.TOK_VAR_BOOL ||
               Current_Token == TOKEN.TOK_VAR_NUMBER ||
               Current_Token == TOKEN.TOK_VAR_STRING))
            {

                CSyntaxErrorLog.AddLine(" expects a data type ");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a data type", -1);

            }
            TOKEN tok = Current_Token;

            t_info.tf = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;

            GetNext();

            if (Current_Token != TOKEN.TOK_COMMA)
            {
                CSyntaxErrorLog.AddLine(" expects a ,");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a ,", -1);

            }
            GetNext();

           

            if (Current_Token != TOKEN.TOK_NUMERIC)
            {
                CSyntaxErrorLog.AddLine(" expects a array bound");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a array bound", -1);

            }

            double limits = base.GetNumber();

            GetNext();

            if (Current_Token != TOKEN.TOK_GT)
            {
                CSyntaxErrorLog.AddLine(" expects a >");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a >", -1);

            }

            
            // --- Skip to the next token , the token ought 
            // to be a Variable name ( UnQouted String )
            GetNext();

            if (Current_Token == TOKEN.TOK_UNQUOTED_STRING)
            {
                SYMBOL_INFO symb = new SYMBOL_INFO();
                symb.SymbolName = base.last_str;

                if (pb.TABLE.Get(base.last_str) != null )
                {
                    CSyntaxErrorLog.AddLine(" duplicate symbol");
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, " duplicate symbol", -1);
                }

                symb.Type = TYPE_INFO.TYPE_ARRAY;
                symb.a_info = new ARRAY_INFO();
                symb.a_info.tf = (tok == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (tok == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;
                symb.a_info.bound = (int)limits;
                Type basetype = null;

                basetype = ( tok == TOKEN.TOK_VAR_BOOL ) ?
                    basetype = typeof(Boolean) :
                    (tok == TOKEN.TOK_VAR_NUMBER ) ?
                    typeof(Double) : typeof(String);

                symb.a_info.data = Array.CreateInstance(basetype, (int)limits);

                //---------- Skip to Expect the SemiColon

                GetNext();

                if (Current_Token == TOKEN.TOK_SEMI)
                {
                    // ----------- Add to the Symbol Table
                    // for type analysis 
                    pb.TABLE.Add(symb);

                    // --------- return the Object of type
                    // --------- VariableDeclStatement
                    // This will just store the Variable name
                    // to be looked up in the above table
                    return new VariableDeclStatement(symb);
                }
                else
                {
                    CSyntaxErrorLog.AddLine("; expected");
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, ", or ; expected", SaveIndex());
                }


            }
          
            return null;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pb"></param>
        /// <returns></returns>
        public Stmt ParseMapDeclStatement(ProcedureBuilder pb)
        {
            MAP_INFO t_info = new MAP_INFO();


            GetNext();

            if (Current_Token != TOKEN.TOK_LT)
            {
                CSyntaxErrorLog.AddLine("< expected");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " < expected", -1);
            }

            GetNext();

            if (!(Current_Token == TOKEN.TOK_VAR_BOOL ||
               Current_Token == TOKEN.TOK_VAR_NUMBER ||
               Current_Token == TOKEN.TOK_VAR_STRING))
            {

                CSyntaxErrorLog.AddLine(" expects a data type ");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a data type", -1);

            }
            TOKEN tok = Current_Token;

            t_info.key = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;

            GetNext();

            if (Current_Token != TOKEN.TOK_COMMA)
            {
                CSyntaxErrorLog.AddLine(" expects a ,");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a ,", -1);

            }
            GetNext();

            if (!(Current_Token == TOKEN.TOK_VAR_BOOL ||
              Current_Token == TOKEN.TOK_VAR_NUMBER ||
              Current_Token == TOKEN.TOK_VAR_STRING))
            {

                CSyntaxErrorLog.AddLine(" expects a data type ");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a data type", -1);

            }
            TOKEN tok1 = Current_Token;

            t_info.Value = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;

           

            GetNext();

            if (Current_Token != TOKEN.TOK_GT)
            {
                CSyntaxErrorLog.AddLine(" expects a >");
                CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                throw new CParserException(-100, " expects a >", -1);

            }


            // --- Skip to the next token , the token ought 
            // to be a Variable name ( UnQouted String )
            GetNext();

            if (Current_Token == TOKEN.TOK_UNQUOTED_STRING)
            {
                SYMBOL_INFO symb = new SYMBOL_INFO();
                symb.SymbolName = base.last_str;

                if (pb.TABLE.Get(base.last_str) != null)
                {
                    CSyntaxErrorLog.AddLine(" duplicate symbol");
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, " duplicate symbol", -1);
                }

                symb.Type = TYPE_INFO.TYPE_MAP;
                symb.m_info = new MAP_INFO();
                symb.m_info.key = t_info.key ;
                symb.m_info.Value = t_info.Value;

                Type basetype = null;

               

                symb.m_info.data = new Hashtable();

                //---------- Skip to Expect the SemiColon

                GetNext();

                if (Current_Token == TOKEN.TOK_SEMI)
                {
                    // ----------- Add to the Symbol Table
                    // for type analysis 
                    pb.TABLE.Add(symb);

                    // --------- return the Object of type
                    // --------- VariableDeclStatement
                    // This will just store the Variable name
                    // to be looked up in the above table
                    return new VariableDeclStatement(symb);
                }
                else
                {
                    CSyntaxErrorLog.AddLine("; expected");
                    CSyntaxErrorLog.AddLine(GetCurrentLine(SaveIndex()));
                    throw new CParserException(-100, ", or ; expected", SaveIndex());
                }


            }

            return null;

        }

    }
}

