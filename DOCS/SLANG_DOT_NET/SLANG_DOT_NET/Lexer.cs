﻿////////////////////////////////////////////////////////
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
    ///  Type info enumerations 
    /// </summary>
    public enum TYPE_INFO
    {
        TYPE_ILLEGAL = -1, // NOT A TYPE
        TYPE_NUMERIC,      // IEEE Double precision floating point 
        TYPE_BOOL,         // Boolean Data type
        TYPE_STRING ,      // String data type 
        TYPE_ARRAY ,
        TYPE_MAP 
        
    }

    /// <summary>
    /// 
    /// </summary>
    public class ARRAY_INFO
    {
        public TYPE_INFO tf;
        public int bound;
        public Array data;
    }
    /// <summary>
    /// 
    /// </summary>
    public class MAP_INFO
    {
        public TYPE_INFO key;
        public TYPE_INFO Value;
        public int bound;
        public Hashtable data;
    }

    /// <summary>
    /// 
    /// </summary>
    public class TYPE_DB
    {

    }
    /// <summary>
    /// 
    /// </summary>
    public class CLASS_DB
    {

    }



    /// <summary>
    ///    Symbol Table entry for variable
    ///    using Attributes , one can optimize the
    ///    storage by simulating C/C++ union.
    /// </summary>
    public class SYMBOL_INFO
    {
        public String SymbolName;   // Symbol Name
        public TYPE_INFO Type;      // Data type
        public String str_val;      // memory to hold string 
        public double dbl_val;      // memory to hold double
        public bool bol_val;      // memory to hold boolean
        public ARRAY_INFO a_info;
        public MAP_INFO m_info;
        public TYPE_DB m_struct;
        public CLASS_DB m_class;
        
        //
        // Added in STEP 5 to store offset 
        // in the TypeBuilder.BuildLocal table
        // Only used by the compiler..interpreter
        // just ignores it..!
        public int loc_position = 0;
    }

    public enum TOKEN
    {
        ILLEGAL_TOKEN = -1, // Not a Token
        TOK_PLUS = 1, // '+'
        TOK_MUL, // '*'
        TOK_DIV, // '/'
        TOK_SUB, // '-'
        TOK_OPAREN, // '('
        TOK_CPAREN, // ')'
               
        TOK_NULL, // End of string
        TOK_PRINT, // Print Statement
        TOK_PRINTLN, // PrintLine
        TOK_UNQUOTED_STRING, // Variable name , Function name etc
        TOK_SEMI , // ; 

        //---------- Addition in Step 4

        TOK_VAR_NUMBER,        // NUMBER data type
        TOK_VAR_STRING,        // STRING data type 
        TOK_VAR_BOOL,          // Bool data type
        TOK_NUMERIC,            // [0-9]+ 
        TOK_COMMENT ,      // Comment Token ( presently not used )   
        TOK_BOOL_TRUE,         // Boolean TRUE
        TOK_BOOL_FALSE  ,   // Boolean FALSE
        TOK_STRING,         // String Literal
        TOK_ASSIGN ,        // Assignment Symbol =  

        // ----------- Added in Step 6 
        // for relational operator support

        TOK_EQ,                // '=='
        TOK_NEQ,               // '<>'
        TOK_GT,                // '>'
        TOK_GTE,               // '>='
        TOK_LT,                // '<'
        TOK_LTE,               // '<='
        TOK_AND,               // '&&'
        TOK_OR,                // '||'
        TOK_NOT,               // '!'

        //------------ Added in Step 6 for 
        // Control structures support

        TOK_IF,                // IF 
        TOK_THEN,              // Then  
        TOK_ELSE,              // Else Statement
        TOK_ENDIF,             // Endif Statement
        TOK_WHILE,             // WHILE
        TOK_WEND,              // Wend Statement

        // ----------- Added in step 7 for 
        // supporting Functions

        
		TOK_FUNCTION,          // FUNCTION 
        TOK_END,               // END keyword
		TOK_RETURN,            // Return keyword
        TOK_COMMA,              // useful in paramlist

        //------- For JS Generation

        TOK_ARRAY,
        TOK_MAP ,
        TOK_OSUBSCRIPT,
        TOK_CSUBSCRIPT
       


    }
    /// <summary>
    ///    Addition of Relational Operators in
    ///    Step 6
    /// </summary>
    public enum RELATION_OPERATOR
    {
        TOK_EQ,                // '=='
        TOK_NEQ,               // '<>'
        TOK_GT,                // '>'
        TOK_GTE,               // '>='
        TOK_LT,                // '<'
        TOK_LTE,               // '<='

    }

    /// <summary>
    ///     Keyword Table Entry
    /// </summary>
    /// 
    public struct ValueTable
    {
        public TOKEN tok;          // Token id
        public String Value;       // Token string  
        public ValueTable(TOKEN tok, String Value)
        {
            this.tok = tok;
            this.Value = Value;

        }
    }
    /// <summary>
    ///     The Lexical Analyzer
    /// </summary>
    public class Lexer
    {

        /// <summary>
        ///    Items which are static of nature
        /// </summary>
        String IExpr;            // Expression string
        protected ValueTable[] keyword; // Keyword Table
        int length;           // Length of the string 
        double number;           // Last grabbed number from the stream

        /// <summary>
        ///  Items which are dependent on state
        ///  index can be changed by GetNext,a Loop or IF statement
        /// </summary>
        int index;           // index into a character  

        /// <summary>
        ///        last_str := Token assoicated with 
        /// </summary>

        public String last_str; // Last grabbed String

        /// <summary>
        ///    Current Token and Last Grabbed Token
        /// </summary>
        protected TOKEN Current_Token;  // Current Token
        protected TOKEN Last_Token;     // Penultimate token




        /// <summary>
        ///    Get Next Token from the stream and return to the caller
        /// </summary>
        /// <returns></returns>

        protected TOKEN GetNext()
        {
            Last_Token = Current_Token;
            Current_Token = GetToken();
            return Current_Token;
        }
        /// <summary>
        ///   Save the Current Lexer index
        /// </summary>
        /// <returns></returns>
        public int SaveIndex()
        {
            return index;
        }

        public int GetLineNo(int pindex)
        {
            int tindex = pindex;
            int nCount = 0;
            if (pindex >= length)
            {
                tindex = length - 1;
            }
            while (tindex > 0)
            {
                if (IExpr[tindex] == '\n')
                    nCount++;

                tindex--;
            }

            return nCount;

        }

        /// <summary>
        ///     Get Line Correswhere Error Occured
        /// </summary>
        /// <param name="pindex"></param>
        /// <returns></returns>
        public String GetCurrentLine(int pindex)
        {
            if (pindex == -1)
                return "";
            int tindex = pindex;
            if (pindex >= length)
            {
                tindex = length - 1;
            }
            while (tindex > 0 && IExpr[tindex] != '\n')
                tindex--;

            
            if (IExpr[tindex] == '\n')
                tindex++;

            String CurrentLine = "";

            while (tindex < length && (IExpr[tindex] != '\n'))
            {
                CurrentLine = CurrentLine + IExpr[tindex];
                tindex++;
            }

            return CurrentLine + "\n";

        }
        /// <summary>
        ///    
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>

        public String GetPreviousLine(int pindex)
        {

            int tindex = pindex;
            while (tindex > 0 && IExpr[tindex] != '\n')
                tindex--;

            if (IExpr[tindex] == '\n')
                tindex--;
            else
                return "";

            while (tindex > 0 && IExpr[tindex] != '\n')
                tindex--;


            if (IExpr[tindex] == '\n')
                tindex--;


            String CurrentLine = "";

            while (tindex < length && (IExpr[tindex] != '\n'))
            {
                CurrentLine = CurrentLine + IExpr[tindex];
                tindex++;
            }

            return CurrentLine + "\n";



        }
        /// <summary>
        ///    Restore Index . Only after a GetNext , contents
        ///    of the state variables will be reliable
        /// </summary>
        /// <param name="m_index"></param>
        public void RestoreIndex(int m_index)
        {
            index = m_index;

        }

        /////////////////////////////////////////////
        //
        // Ctor
        //
        //   
        public Lexer(String Expr)
        {
            IExpr = Expr;
            length = IExpr.Length;
            index = 0;
            ////////////////////////////////////////////////////
            // Fill the Keywords
            //
            //
            keyword = new ValueTable[18];

            keyword[0] = new ValueTable(TOKEN.TOK_BOOL_FALSE, "FALSE");
            keyword[1] = new ValueTable(TOKEN.TOK_BOOL_TRUE, "TRUE");
            keyword[2] = new ValueTable(TOKEN.TOK_VAR_STRING, "STRING");
            keyword[3] = new ValueTable(TOKEN.TOK_VAR_BOOL, "BOOLEAN");
            keyword[4] = new ValueTable(TOKEN.TOK_VAR_NUMBER, "NUMERIC");
            keyword[5] = new ValueTable(TOKEN.TOK_PRINT, "PRINT");
            keyword[6] = new ValueTable(TOKEN.TOK_PRINTLN, "PRINTLINE");

            // -------------- Added in the step 6
            // -------------- To support control structures

            keyword[7] = new ValueTable(TOKEN.TOK_IF, "IF");
            keyword[8] = new ValueTable(TOKEN.TOK_WHILE, "WHILE");
            keyword[9] = new ValueTable(TOKEN.TOK_WEND, "WEND");
            keyword[10] = new ValueTable(TOKEN.TOK_ELSE, "ELSE");
            keyword[11] = new ValueTable(TOKEN.TOK_ENDIF, "ENDIF");
            keyword[12] = new ValueTable(TOKEN.TOK_THEN, "THEN");

            // ----------- Step 7

            keyword[13] = new ValueTable(TOKEN.TOK_END, "END");
            keyword[14] = new ValueTable(TOKEN.TOK_FUNCTION, "FUNCTION");
            keyword[15] = new ValueTable(TOKEN.TOK_RETURN, "RETURN"); 
            
            //--------------- For JS generation

            keyword[16] = new ValueTable(TOKEN.TOK_ARRAY, "ARRAY");
            keyword[17] = new ValueTable(TOKEN.TOK_MAP, "MAP");














        }
        /// <summary>
        ///      Extract string from the stream
        /// </summary>
        /// <returns></returns>
        private String ExtractString()
        {
            String ret_value = "";
            while (index < IExpr.Length &&
                (char.IsLetterOrDigit(IExpr[index]) || IExpr[index] == '_'))
            {
                ret_value = ret_value + IExpr[index];
                index++;
            }
            return ret_value;
        }

        /// <summary>
        ///    Skip to the End of Line
        /// </summary>
        public void SkipToEoln()
        {
           // while (index < length && (( IExpr[index] != '\r' && IExpr[index] !='\n')))
           //     index++;

            while (index < length)
            {
                if (IExpr[index] == '\r' || IExpr[index] == '\n')
                                  break;
                index++;
            }

            if (index == length)
                return;

            if (IExpr[index] == '\n')
            {
                index++;
                return;
            }

            if (IExpr[index + 1] == '\n')
            {
                index += 2;
                return;
            }
            index++;
            return;
        }
        /////////////////////////////////////////////////////
        // Grab the next token from the stream
        //
        //    
        //
        //
        public TOKEN GetToken()
        {

            TOKEN tok=TOKEN.ILLEGAL_TOKEN ;

        re_start:
            tok = TOKEN.ILLEGAL_TOKEN;

            ////////////////////////////////////////////////////////////
            //
            // Skip  the white space 
            //  

            while (index < length &&
                (IExpr[index] == ' ' || IExpr[index] == '\t'))
                index++;
            //////////////////////////////////////////////
            //
            //   End of string ? return NULL;
            //

            if (index == length)
                return TOKEN.TOK_NULL;

            /////////////////////////////////////////////////  
            //
            //
            // 
            switch (IExpr[index])
            {
                case '\n':
                    index++;
                    goto re_start;
                case '\r':
                    index++;
                    goto re_start;
                case '+':
                    tok = TOKEN.TOK_PLUS;
                    index++;
                    break;
                case '-':
                    tok = TOKEN.TOK_SUB;
                    index++;
                    break;
                case '*':
                    tok = TOKEN.TOK_MUL;
                    index++;
                    break;
                case ',':
                    // -------- Addition in step 7
                    tok = TOKEN.TOK_COMMA;
                    index++;
                    break;
                case '(':
                    tok = TOKEN.TOK_OPAREN;
                    index++;
                    break;
                case '[':
                    tok = TOKEN.TOK_OSUBSCRIPT;
                    index++;
                    break;
                case ']':
                    tok = TOKEN.TOK_CSUBSCRIPT;
                    index++;
                    break;
                case ';':
                    tok = TOKEN.TOK_SEMI;
                    index++;
                    break;
                case ')':
                    tok = TOKEN.TOK_CPAREN;
                    index++;
                    break;
                case '!':
                    tok = TOKEN.TOK_NOT;
                    index++;
                    break;
                case '>':
                    if (IExpr[index + 1] == '=')
                    {
                        tok = TOKEN.TOK_GTE;
                        index += 2;
                    }
                    else
                    {
                        tok = TOKEN.TOK_GT;
                        index++;
                    }
                    break;
                case '<':
                    if (IExpr[index + 1] == '=')
                    {
                        tok = TOKEN.TOK_LTE;
                        index += 2;
                    }
                    else if (IExpr[index + 1] == '>')
                    {
                        tok = TOKEN.TOK_NEQ;
                        index += 2;
                    }
                    else
                    {
                        tok = TOKEN.TOK_LT;
                        index++;
                    }
                    break;
                case '=':
                    if (IExpr[index + 1] == '=')
                    {
                        tok = TOKEN.TOK_EQ;
                        index += 2;
                    }
                    else
                    {
                        tok = TOKEN.TOK_ASSIGN;
                        index++;
                    }
                    break;
                case '&':
                    if (IExpr[index + 1] == '&')
                    {
                        tok = TOKEN.TOK_AND;
                        index += 2;
                    }
                    else
                    {
                        tok = TOKEN.ILLEGAL_TOKEN;
                        index++;
                    }
                    break;
                case '|':
                    if (IExpr[index + 1] == '|')
                    {
                        tok = TOKEN.TOK_OR;
                        index += 2;
                    }
                    else
                    {
                        tok = TOKEN.ILLEGAL_TOKEN;
                        index++;
                    }
                    break;
                case '/':

                    if (IExpr[index + 1] == '/')
                    {
                        SkipToEoln();
                        goto re_start;
                    }
                    else
                    {
                        tok = TOKEN.TOK_DIV;
                        index++;
                    }
                    break;
                case '"':
                    String x = "";
                    index++;
                    while (index < length && IExpr[index] != '"')
                    {
                        x = x + IExpr[index];
                        index++;
                    }

                    if (index == length)
                    {
                        tok = TOKEN.ILLEGAL_TOKEN;
                        return tok;
                    }
                    else
                    {
                        index++;
                        last_str = x;
                        tok = TOKEN.TOK_STRING;
                        return tok;
                    }
                  




                default:
                    if (char.IsDigit(IExpr[index]))
                    {

                        String str = "";

                        while (index < length && (IExpr[index] == '0' ||
                            IExpr[index] == '1' ||
                            IExpr[index] == '2' ||
                            IExpr[index] == '3' ||
                            IExpr[index] == '4' ||
                            IExpr[index] == '5' ||
                            IExpr[index] == '6' ||
                            IExpr[index] == '7' ||
                            IExpr[index] == '8' ||
                            IExpr[index] == '9'))
                        {
                            str += Convert.ToString(IExpr[index]);
                            index++;
                        }

                        if (IExpr[index] == '.')
                        {
                            str = str + ".";
                            index++;
                            while (index < length && (IExpr[index] == '0' ||
                                IExpr[index] == '1' ||
                                IExpr[index] == '2' ||
                                IExpr[index] == '3' ||
                                IExpr[index] == '4' ||
                                IExpr[index] == '5' ||
                                IExpr[index] == '6' ||
                                IExpr[index] == '7' ||
                                IExpr[index] == '8' ||
                                IExpr[index] == '9'))
                            {
                                str += Convert.ToString(IExpr[index]);
                                index++;
                            }

                        }





                        number = Convert.ToDouble(str);
                        tok = TOKEN.TOK_NUMERIC;


                    }
                    else if (char.IsLetter(IExpr[index]))
                    {

                        String tem = Convert.ToString(IExpr[index]);
                        index++;
                        while (index < length && (char.IsLetterOrDigit(IExpr[index]) ||
                            IExpr[index] == '_'))
                        {
                            tem += IExpr[index];
                            index++;
                        }

                        tem = tem.ToUpper();

                        for (int i = 0; i < keyword.Length; ++i)
                        {
                            if (keyword[i].Value.CompareTo(tem) == 0)
                                return keyword[i].tok;

                        }


                        this.last_str = tem;



                        return TOKEN.TOK_UNQUOTED_STRING;



                    }
                    else
                    {
                        return TOKEN.ILLEGAL_TOKEN;
                    }
                    break;
            }

            return tok;

        }

        /// <summary>
        ///  Return the last grabbed number from the steam
        /// </summary>
        /// <returns></returns>
        public double GetNumber()
        {
            return number;

        }

    }
}
