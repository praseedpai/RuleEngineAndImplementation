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
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;


namespace SLANG_DOT_NET
{
    /// <summary>
    ///    A bunch of statement is called a Compilation
    ///    unit at this point of time... STEP 5
    ///    In future , a Collection of Procedures will be
    ///    called a Compilation unit
    ///    
    ///    Added in the STEP 5
    /// </summary>
    public abstract class CompilationUnit
    {
        //public abstract SYMBOL_INFO Execute(RUNTIME_CONTEXT cont);
        //Extended with Formal Parameter list given to Main
        //Addition in STEP 7
        public abstract SYMBOL_INFO Execute(RUNTIME_CONTEXT cont, ArrayList actuals); 
        
        public abstract bool Compile(DNET_EXECUTABLE_GENERATION_CONTEXT cont);

        public abstract SYMBOL_INFO GenerateJS(RUNTIME_CONTEXT cont, ArrayList actuals); 
    }

    /// <summary>
    ///    Abstract base class for Procedure
    ///    All the statements in a Program ( Compilation unit )
    ///    will be compiled into a PROC 
    /// </summary>
    public abstract class PROC
    {
        //
        //public abstract SYMBOL_INFO Execute(RUNTIME_CONTEXT cont);
        // The above stuff is extended with Formal parameter list
        // addition in STEP 7
        public abstract SYMBOL_INFO Execute(RUNTIME_CONTEXT cont, ArrayList formals);
        
        public abstract bool Compile(DNET_EXECUTABLE_GENERATION_CONTEXT cont);

        public abstract SYMBOL_INFO GenerateJS(RUNTIME_CONTEXT cont, ArrayList formals);

    }

    /// <summary>
	///     A CodeModule is a Compilation Unit ..
    ///     At this point of time ..it is just a bunch
    ///     of statements... 
	/// </summary>
    public class TModule : CompilationUnit
    {
        /// <summary>
        ///    A Program is a collection of Procedures...
        ///    Now , we support only global function...
        /// </summary>
        private ArrayList m_procs=null;
        /// <summary>
        ///    List of Compiled Procedures....
        ///    At this point of time..only one procedure
        ///    will be there....
        /// </summary>
        private ArrayList compiled_procs = null;
        /// <summary>
        ///    class to generate IL executable... 
        /// </summary>

        private ExeGenerator _exe = null;

        public ArrayList GetProcs()
        {
            return m_procs;
        }

        /// <summary>
        ///    Ctor for the Program ...
        /// </summary>
        /// <param name="procedures"></param>

        public TModule(ArrayList procs)
        {
            m_procs = procs;
            
        }

        /// <summary>
        ///      
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CreateExecutable(string name)
        {
            //
            // Create an instance of Exe Generator
            // ExeGenerator takes a TModule and 
            // exe name as the Parameter...
            _exe = new ExeGenerator(this,name);
            // Compile The module...
            Compile(null);
            // Save the Executable...
            _exe.Save();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cont"></param>
        /// <returns></returns>
        public override bool Compile(DNET_EXECUTABLE_GENERATION_CONTEXT cont)
        {
            compiled_procs = new ArrayList();
            foreach (Procedure p in m_procs)
            {
                DNET_EXECUTABLE_GENERATION_CONTEXT con = new DNET_EXECUTABLE_GENERATION_CONTEXT(this,p, _exe.type_bulder);
                compiled_procs.Add(con);
                p.Compile(con);

            }
            return true;

        }

        public override SYMBOL_INFO Execute(RUNTIME_CONTEXT cont,ArrayList actuals )
        {
            Procedure p = Find("Main");

            if (p != null)
            {

                return p.Execute(cont,actuals);
            }

            return null;

        }

        public MethodBuilder _get_entry_point(string _funcname)
        {
            foreach (DNET_EXECUTABLE_GENERATION_CONTEXT u in compiled_procs)
            {
                if (u.MethodName.Equals(_funcname))
                {
                    return u.MethodHandle;
                }

            }

            return null;


        }

        public Procedure Find(string str)
        {
            foreach (Procedure p in m_procs)
            {
                string pname = p.Name;

                if (pname.ToUpper().CompareTo(str.ToUpper()) == 0)
                    return p;

            }

            return null;

        }

        public override SYMBOL_INFO GenerateJS(RUNTIME_CONTEXT cont, ArrayList formals)
        {
            //Procedure p = Find("Main");

            Console.Write("//--- invoke the main method ... \r\n\r\n\r\n");
            Console.Write("//---- Generated JavaScript from SLANG Script\r\n");

            //if (p != null)
           // {

             //   p.GenerateJS(cont, formals);
           // }

            foreach (Procedure p in m_procs)
            {
                p.GenerateJS(cont, formals);

            }

          
            Console.Write("//----End  Generated JavaScript \r\n");
            return null;
            
        }


    }

    /// <summary>
    ///     A Procedure which returns an Exit Code...
    ///     It defaults to 0 in this step...!
    /// </summary>
    public class Procedure : PROC
    {
        /// <summary>
        ///    Procedure name ..which defaults to Main 
        ///    in the type MainClass
        /// </summary>
        public string m_name;
        /// <summary>
        ///    Formal parameters...
        /// </summary>
        public ArrayList m_formals = null;
        /// <summary>
        ///     List of statements which comprises the Procedure
        /// </summary>
        public ArrayList m_statements = null;
        /// <summary>
        ///     Local variables
        /// </summary>
        public SymbolTable m_locals = null;
        /// <summary>
        ///        return_value.... a hard coded zero at this
        ///        point of time..
        /// </summary>
        public SYMBOL_INFO return_value = null;
        /// <summary>
        ///       TYPE_INFO => TYPE_NUMERIC
        /// </summary>
        public TYPE_INFO _type = TYPE_INFO.TYPE_ILLEGAL;

        public ArrayList  GetStmts() {
            return m_statements;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formals"></param>
        /// <param name="stats"></param>
        /// <param name="locals"></param>
        /// <param name="type"></param>

        public Procedure(string name,
                         ArrayList formals,
                         ArrayList stats, 
                         SymbolTable locals, 
                         TYPE_INFO type)
        {
            m_name = name;
            //
            // The value is only supplied for STEP 7
            m_formals = formals;
            m_statements = stats;
            m_locals = locals;
            _type = type;
        }
        /// <summary>
        /// 
        /// </summary>
        public TYPE_INFO TYPE
        {

            get
            {
                return _type;

            }

        }
        /// <summary>
        ///     STEP 7 
        /// </summary>
        public ArrayList FORMALS
        {
            get
            {
                return m_formals;
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            set
            {

                Name = value;
            }

            get
            {
                return m_name;
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SYMBOL_INFO ReturnValue()
        {
            return return_value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cont"></param>
        /// <returns></returns>
        public TYPE_INFO TypeCheck(COMPILATION_CONTEXT cont)
        {

            return _type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cont"></param>
        /// <returns></returns>
       public override bool Compile( DNET_EXECUTABLE_GENERATION_CONTEXT cont )
		{
			
			if ( m_formals != null ) 
			{

				
                int i=0;

				foreach( SYMBOL_INFO b in m_formals ) 
				{

					System.Type type = (b.Type == TYPE_INFO.TYPE_BOOL ) ?
						typeof(bool) : (b.Type == TYPE_INFO.TYPE_NUMERIC ) ?
						typeof(double) : typeof(string);
					int s = cont.DeclareLocal(type);
					b.loc_position = s;
					cont.TABLE.Add(b); 
					cont.CodeOutput.Emit(OpCodes.Ldarg,i);   
					cont.CodeOutput.Emit(OpCodes.Stloc,cont.GetLocal(s));
                    i++;
				}

			}


            foreach (Stmt e1 in m_statements) 
			{
				e1.Compile(cont); 
			}

			cont.CodeOutput.Emit(OpCodes.Ret);
			return true;
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cont"></param>
		/// <param name="actuals"></param>
		/// <returns></returns>
		public override SYMBOL_INFO  Execute(RUNTIME_CONTEXT cont,ArrayList actuals  ) 
		{
			ArrayList vars = new ArrayList();
			int i=0;

			FRAME ft = new FRAME(); 

			if ( m_formals != null && actuals !=null ) 
			{

				i=0;
				foreach( SYMBOL_INFO b in m_formals ) 
				{
                    
					SYMBOL_INFO inf = actuals[i] as SYMBOL_INFO;
					inf.SymbolName = b.SymbolName;
                	cont.TABLE.Add(inf); 
					i++;
				}

			}

            foreach (Stmt e1 in m_statements) 
			{
				return_value = e1.Execute(cont); 

				if ( return_value != null )
					  return return_value;

	 		}

			return null;
			
     	}

        public override SYMBOL_INFO GenerateJS(RUNTIME_CONTEXT cont, ArrayList formals)
        {
            ArrayList vars = new ArrayList();
            int i = 0;

            FRAME ft = new FRAME();

            Console.Write("function " +m_name + "( " );

          

            if (m_formals != null)
            {
              
                i = 0;
                foreach (SYMBOL_INFO b in m_formals)
                {

                 //   SYMBOL_INFO inf = m_formals[i] as SYMBOL_INFO;
                 //   inf.SymbolName = b.SymbolName;
                  if ( i == m_formals.Count -1 )   
                    Console.Write(b.SymbolName +"");
                  else
                      Console.Write(b.SymbolName + ","); 
                 //   cont.TABLE.Add(inf);
                    i++;
                }

            }

            Console.Write(" ) {\r\n");

            ///////////////////////
            //
            // The flag given below detects whether there
            // is a return value at the top most level
            //  (also called topmost block )

            bool return_found = false;

            ////////////////////////////
            // Iterate through the statements  list and 
            // Generate Code....
            //
            foreach (Stmt e1 in m_statements)
            {
                ///////////////////////////
                //
                // Found a return value , set the flag to 
                // true. 
                //
                if (e1 is ReturnStatement)
                    return_found = true;

               e1.GenerateJS(cont);

            }

            if (return_found == false)
            {
                Console.Write("\r\n//---- Translator installed return\r\n ");
                if (this.TYPE == TYPE_INFO.TYPE_BOOL)
                    Console.Write("return false; \r\n");
                else if (this.TYPE == TYPE_INFO.TYPE_NUMERIC)
                    Console.Write("return 0;\r\n");
                else if (this.TYPE == TYPE_INFO.TYPE_STRING)
                    Console.Write("return \"\";\r\n");

            }


            Console.Write("\r\n } \r\n");
            return null;
        }

        public Boolean AnalyzeTree()
        {

          

            return true;


        }
	}
    }

