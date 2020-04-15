using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using SLANG_DOT_NET;

namespace XLS2SLANG
{
    /// <summary>
    ///    An API for parsing Snippets...
    ///    By injecting External Symbols ..not part
    ///    of the source module...
    ///  
    ///    ----------------------------------------------------------------------------
    ///    | PROGRAMNAME	          |                       RULETEXT                 |
    ///    ----------------------------------------------------------------------------
    ///      ELIGIBLE_PROGRAM_ONE     |	return  (( R11  == TRUE ) || (R12 == TRUE  ) );
    ///    -----------------------------------------------------------------------------
    ///      ELIGIBLE_PROGRAM_TWO     |
    ///                               |     IF ( R11 == FALSE ) THEN
    ///                               |          return TRUE;
    ///                               |
    ///                               |     ENDIF
    ///                               |
    ///                               |
    ///                               |     return FALSE;
    ///
    /// </summary>
    public class CSnippetParser : RDParser
    {
        /// <summary>
        ///    All the rule names will be an element in this dictionary as SYMBOL
        ///       { Symbolname = rulename , Type = BOOLEAN }
        /// </summary>
        Dictionary<string, SYMBOL_INFO> _rules = new Dictionary<string, SYMBOL_INFO>();

        /// <summary>
        ///   Parameters ...to be used for the program...
        ///   
        /// </summary>
        Dictionary<string,SYMBOL_INFO > parameters = new Dictionary<string,SYMBOL_INFO>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        public CSnippetParser(String str,Dictionary<string,SYMBOL_INFO> m_rules ) :
            base(str)
        {
            _rules = m_rules;
        }

        public Dictionary<string, SYMBOL_INFO> GetLocals()
        {
            return parameters;
        }

        /// <summary>
        ///   The new Parser entry point
        /// </summary>
        /// <returns></returns>
        public TModule  ParseText()
        {
            try
            {
                if (_rules == null || _rules.Count == 0)
                    return null;

                GetNext();   // Get The First Valid Token
                return ParseFunctions2();
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
        public TModule ParseFunctions2()
        {
            bool error_state = false;
            while (Current_Token == TOKEN.TOK_FUNCTION)
            {
                ProcedureBuilder b = ParseFunction2();
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


            if (error_state == true)
            {
                throw new CParserException(-100, "Compilation Error  ", SaveIndex());
            }

            //
            //  Convert the builder into a program
            //
            return prog.GetProgram();
        }

        //////////////////////////////////////
        //
        //
        //
        SYMBOL_INFO CollectSymbols(string str)
        {
           
                SYMBOL_INFO st = new SYMBOL_INFO();
                st.SymbolName = str;
                st.Type = TYPE_INFO.TYPE_BOOL;
                st.bol_val = false;
                parameters.Add(str, st);
                return null;
        }
        /// <summary>
        ///    Parse A Single Function.
        /// </summary>
        /// <returns></returns>
        ProcedureBuilder ParseFunction2()
        {

            COMPILATION_CONTEXT cnt = new COMPILATION_CONTEXT();

            foreach (KeyValuePair<string, SYMBOL_INFO> temp in _rules)
            {
                cnt.TABLE.Add(temp.Value);
            }
            //
            // Create a Procedure builder Object
            //    We are passing the method "CollectSymbols" functions as parameters
            //    All the Symbol Table Lookup in the ProcedureBuilder will be 
            //    captured by CollectSymbols
            //
            ProcedureBuilder p = new ProcedureBuilder("", cnt,CollectSymbols);
            if (Current_Token != TOKEN.TOK_FUNCTION)
                throw new CParserException(-1, "FUNCTION expected ", SaveIndex());


            GetNext();
            // return type of the Procedure ought to be 
            // Boolean , Numeric or String 
            if (!(Current_Token == TOKEN.TOK_VAR_BOOL ||
                Current_Token == TOKEN.TOK_VAR_NUMBER ||
                Current_Token == TOKEN.TOK_VAR_STRING))
            {
                throw new CParserException(-1, "A Legal data type expected ", SaveIndex());


            }

            //-------- Assign the return type
            p.TYPE = (Current_Token == TOKEN.TOK_VAR_BOOL) ?
                TYPE_INFO.TYPE_BOOL : (Current_Token == TOKEN.TOK_VAR_NUMBER) ?
                TYPE_INFO.TYPE_NUMERIC : TYPE_INFO.TYPE_STRING;

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
                throw new CParserException(-1, "END expected", SaveIndex());
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
    }
}
