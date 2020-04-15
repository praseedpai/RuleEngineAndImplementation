using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SLANG_DOT_NET;
using System.Data;
using System.Collections;

namespace XLS2SLANG
{
    /// <summary>
    ///    The following program will convert XLS rule
    ///    snippets to legal slang program
    /// </summary>
    public class XlsSlangEngine
    {
        /// <summary>
        ///   Global Symbol Table...
        ///   This is extracted from the VariableMappings tab..
        ///   of the rule spreadsheet...
        /// </summary>
        Dictionary<String , SYMBOL_INFO > glb_sym;
        /// <summary>
        ///   Excel Helper , This isolates Excel related
        ///   routines from the Engine propert....
        /// </summary>
        CExcelReader m_reader = null;
        /// <summary>
        ///    Whether all the rules so far were legal...
        /// </summary>
        Boolean rule_compile_flag = true;

        /// <summary>
        ///    Dictionary to maintain dependency chain...
        ///    depend_dict = {};
	    ///    depend_dict["a"] = ["R11","R12","R13"];
	    ///    depend_dict["b"] = ["R12","R13"];
	    ///    depend_dict["c"] = [];
	    ///    depend_dict["Citizenship"] = [ "R14" ];
	    ///    depend_dict["Insurance"] = ["R15"];
	    /// </summary>
        Dictionary<String, List<String>> dependency = new Dictionary<string,List<string> >();

        /// <summary>
        ///    Creation of Pseudo Symbol Table for storing
        ///    Rule result evaluation as a Symbolic Variable..
        ///   
        ///   
        ///    invoke_dict["R11"] = "R11(a)";
	    ///    invoke_dict["R12"] = "R12(b,a)";
	    ///    invoke_dict["R13"] = "R13(a,b)";
	    ///    invoke_dict["R14"] = "R14(Citizenship)";
        ///    invoke_dict["R15"] = "R15(Insurance)";
        ///    
        ///    {R11,R12,R13,R14,R15 } are pre-defined variables in the symbol
        ///    table while evaluating the Eligibility.
        ///    
        ///    These value of variables have to be retrieved from the truth_table
        ///    maintained by the engine after evaluating the contents of invoke_dict
        ///    
        ///    TRUTH_DICT["R11"] = eval(INVOKE_DICT["R11"]);
        ///    
        /// </summary>
        Dictionary<String, SYMBOL_INFO> rule_sym = new Dictionary<string,SYMBOL_INFO>();

        /// <summary>
        ///    Module Handle for the rule snippets compilation, If SLANG engine can 
        ///    parse it , a legal TModule instance will be assigned to it...
        ///    
        ///    _rule_snippets - stores the compiled code for rules
        ///    
        /// </summary>
        TModule _rule_snippets = null;

        /// <summary>
        ///      Module Handle for the rule snippets compilation, If SLANG engine can 
        ///      parse it , a legal TModule instance will be assigned to it...
        ///      
        ///      _eligible_rules - stores the compiled code for the eligibility rules
        /// </summary>
        TModule _eligible_rules = null;

        /// <summary>
        ///     CTOR
        ///     
        /// </summary>
        /// <param name="reader"></param>

        public XlsSlangEngine(CExcelReader reader)
        {
            m_reader = reader;
           
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public  bool Run(bool diag)
        {
            ///////////////////////////////////////////////////
            //
            // Run the engine on Rule Snippets.... This step will
            // generate legal  SLANG script 
            //
            String res = null;
            if ((res = EmitRuleSnippets(true)) == null)
            {
                Console.WriteLine("Failed to Generate SLANG program from Rule Snippets\r\n");
                return false;
            }

            /////////////////////////
            // If we are in the diagnostic mode , generate the SLANG output as a
            // comment....
            //
            if (diag == true)
            {
                Console.WriteLine("/*\r\n"+ res +"\r\n*/");
            }
            ///////////////////////////////////////////////////
            //
            //  Process PROGRAM_TO_RULE mappings
            //

            string sl = EmitProgramToRuleMappings("PROGRAM_TO_RULE$", "");

            if (sl == null)
            {

                Console.WriteLine("Failed to compile PROGRAM_TO_RULE mapping snippets ");
                return false;
            }
            /////////////////////////
            // If we are in the diagnostic mode , generate the SLANG output as a
            // comment....
            //
            if (diag == true)
            {
                Console.WriteLine("/*\r\n" + sl + "\r\n***************/\r\n");
            }

            /////////////////////////////////
            // get into the Code genaration phase ...!
            //
            //
            if (!EmitPrologue())
            {
                Console.WriteLine("Error Generating Prolog Code \r\n");
                return false;
            }


            if (!GenerateJSRuleSnippets(res))
            {
                Console.WriteLine("Error Generating JavaScript  Code from Rule snippets... \r\n");
                return false;
            }

            if (!GenerateJSRuleEligible(sl))
            {
                Console.WriteLine("Error Generating JavaScript  Code from Program_To_Rule snippets... \r\n");
                return false;
            }

            if (!EmitEpilogue())
            {

                Console.WriteLine("Error Generating JavaScript  Code from Program_To_Rule snippets... \r\n");
                return false;
            }


            Console.WriteLine("//-------------------------------------- Successfull --------------\r\n");
            return true;

        }
        /// <summary>
        ///    Generate the JavaScript for the Rule snippets...
        /// </summary>
        /// <param name="slang_text"></param>
        /// <returns></returns>
        public bool GenerateJSRuleSnippets(string slang_text)
        {

            //---------------- Creates the Parser Object
            // With Program text as argument 
            RDParser pars = null;
            pars = new RDParser(slang_text);
           
            _rule_snippets = pars.DoParse();

            if (_rule_snippets == null)
            {
                Console.WriteLine("Parse Process Failed while processing rule snippets");
                return false;
            }
            //
            //  Now that Parse is Successul...
            //  Generate JavaScript
            //
            RUNTIME_CONTEXT f = new RUNTIME_CONTEXT(_rule_snippets);
            SYMBOL_INFO fp = _rule_snippets.GenerateJS(f, null);
            return true;
        }
        /// <summary>
        ///    Generate the JavaScript for the Eligibility rules...
        /// </summary>
        /// <param name="slang_text"></param>
        /// <returns></returns>
        public bool GenerateJSRuleEligible(string slang_text)
        {

            //---------------- Creates the Parser Object
            // With Program text as argument 
            RDParser pars = null;
            pars = new RDParser(slang_text);

            _eligible_rules = pars.DoParse();

            if (_eligible_rules == null)
            {
                Console.WriteLine("Parse Process Failed while processing Eligibility rules");
                return false;
            }
            //
            //  Now that Parse is Successul...
            //  Generate JavaScript
            //
            RUNTIME_CONTEXT f = new RUNTIME_CONTEXT(_eligible_rules);
            SYMBOL_INFO fp = _eligible_rules.GenerateJS(f, null);
            return true;
        }
        /// <summary>
        ///     Emit Rules into a Legal SLANG program text....
        ///     
        /// </summary>
        /// <param name="partial_code"></param>
        /// <returns></returns>
        public String  EmitRuleSnippets(bool partial_code = false)
        {
            /////////////////////////////////////////////
            // Create the Global Symbol Table from
            // the Variablemappings tab
            glb_sym = RetrieveGlobalSymbols();

            if (glb_sym == null || glb_sym.Count == 0)
            {
                Console.WriteLine("Failed to generate Symbol Table from VariableMappings tab\r\n");
                return null;
            }

            ////////////////////////////////////////////////////////
            // Visit the Functions tab and Verbatim generate
            // code for the src code over there...

            String slang_text = EmitFunctions();

            if (slang_text == null || slang_text.Length == 0)
            {
                Console.WriteLine("Error while processing FUNCTIONS tab ");
                return null;
            }

            /////////////////////////////////////////////////
            //
            // Retrieve all the tabs from the Worksheets...
            //
            String[] er = m_reader.GetWorkSheets();

            if (er == null || er.Length == 0 )
            {
                rule_compile_flag = false;
                Console.WriteLine("Failed to retrieve Worksheets from the spreadsheet");

            }

            foreach (String s in er)
            {
                if (s != "VariableMappings$" && s != "FUNCTIONS$" 
                    && s != "PROGRAM_TO_RULE$" )
                {
                    slang_text = EmitRuleSnippets(s, slang_text);

                    if (slang_text == null)
                    {
                        Console.WriteLine("Error While processing .... " + s);


                    }
                }
            }

           


            if (  rule_compile_flag )
                return slang_text;

            return null;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="slang_text"></param>
        /// <returns></returns>
        public string EmitProgramToRuleMappings(string p, string slang_text)
        {
            /////////////////////////////////////////////
            //
            // Retrieve the data from Excel Reader...
            DataSet ds = m_reader.GetData(p);
            if (ds == null)
                return null;

            ////////////////////////////////////////
            //
            // Get the Worksheet data...
            //
            DataTable dt = ds.Tables[0];

            Dictionary<String, SYMBOL_INFO> ret = new Dictionary<string, SYMBOL_INFO>();

            if (dt == null)
                return null;

            int Count = dt.Rows.Count;

            slang_text += "//----------- Program To Rule mapping \r\n\r\n";

            int i = 0;
            while (i < Count)
            {
                DataRow dr = dt.Rows[i];

                try
                {
                    if (dr["RULETEXT"] == null || dr["PROGRAMNAME"] == null)
                        return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("The Cell Column name should be PROGRAMNAME , RULETEXT in "+ p);
                    return null;
                }

                String FunctionName = Convert.ToString(dr["PROGRAMNAME"]);
                String FunctionBody = Convert.ToString(dr["RULETEXT"]);

                List<String> rst = SplitLines(FunctionBody);

                string new_prog = "";

                foreach (String s in rst)
                {
                    new_prog += s + "\r\n";
                }

                string program_text = ManufactureFunction(FunctionName, new_prog);
                i++;

                ///////////////////////////////
                // CSnippetParser is a subclass of RDParser the purpose is to parse
                // PROGRAM_TO_RULE snippets...
                //
                //

                CSnippetParser par = new CSnippetParser(program_text, rule_sym);
                TModule mod = par.ParseText();

                if (mod == null)
                {
                    Console.WriteLine("Error in Parsing " + FunctionName + " in " + "PROGRAM_TO_RULE \r\n");
                    rule_compile_flag = false;

                }
                else
                {

                    Dictionary<string, SYMBOL_INFO> inf = par.GetLocals();
                    program_text = ManufactureFunctionWithParams(inf, FunctionName, new_prog);
                    RDParser par2 = new RDParser(program_text);

                    mod = par2.DoParse();

                    if (mod == null)
                    {
                        Console.WriteLine("Error in Parsing " + FunctionName + " in " + "PROGRAM_TO_RULE \r\n");
                        rule_compile_flag = false;

                    }
                    else
                    {
                        slang_text += "\r\n" + program_text + "\r\n";
                    }


                }
               
             }
            return slang_text;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inf"></param>
        /// <param name="FunctionName"></param>
        /// <param name="FunctionBody"></param>
        /// <returns></returns>
        private string ManufactureFunctionWithParams(Dictionary<string, SYMBOL_INFO> inf, string FunctionName, string FunctionBody)
        {
            string func_text = "FUNCTION BOOLEAN " + FunctionName + " ( ";
            int nCount = inf.Count;
            int i = 0;
            foreach (KeyValuePair<string, SYMBOL_INFO> kt in inf)
            {

                if (i == nCount - 1)
                    func_text += "BOOLEAN " + kt.Key;
                else
                    func_text += "BOOLEAN " + kt.Key + ",";
                i++;
            }
            func_text += " )\r\n";
            func_text += FunctionBody + "\r\n";
            func_text += "END\r\n";
            return func_text;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="FunctionBody"></param>
        /// <returns></returns>
        String ManufactureFunction(string FunctionName, String FunctionBody)
        {
           
            string func_text = " FUNCTION BOOLEAN " + FunctionName + " ( ) \r\n";
            func_text += FunctionBody + "\r\n";
            func_text += "END\r\n";
            return func_text;
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Cell"></param>
        /// <returns></returns>
        TYPE_INFO ConvertCellValueToType( string Cell ) {

            Cell = Cell.Trim().ToUpper();

            if (Cell.CompareTo("BOOLEAN") == 0 )
                return TYPE_INFO.TYPE_BOOL;
            else if ( Cell.CompareTo("NUMERIC") == 0 )
                return TYPE_INFO.TYPE_NUMERIC;
            else if ( Cell.CompareTo("STRING") == 0 )
                return TYPE_INFO.TYPE_STRING;

            return TYPE_INFO.TYPE_ILLEGAL;


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        void SetDefaultValue(SYMBOL_INFO sm,String cellvalue )
        {
           
                cellvalue = cellvalue.Trim().ToUpper();

                if (sm.Type == TYPE_INFO.TYPE_BOOL)
                {
                    if (cellvalue.CompareTo("TRUE") == 0)
                    {
                        sm.bol_val = true;
                    }
                    else
                        sm.bol_val = false;


                }
                else if (sm.Type == TYPE_INFO.TYPE_NUMERIC)
                {

                    sm.dbl_val = Convert.ToDouble(cellvalue);

                }
                else if (sm.Type == TYPE_INFO.TYPE_STRING)
                {

                    sm.str_val = cellvalue;
                }
          
           


        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Dictionary<String, SYMBOL_INFO> RetrieveGlobalSymbols()
        {
            /////////////////////////////////////////////
            //
            // Retrieve the data from Excel Reader...
            DataSet ds = m_reader.GetData("VariableMappings$");
            if (ds == null)
                return null;

            ////////////////////////////////////////
            //
            // Get the Worksheet data...
            //
            DataTable dt = ds.Tables[0];

            Dictionary<String, SYMBOL_INFO> ret = new Dictionary<string, SYMBOL_INFO>();

            if (dt == null)
                return null;

            int Count = dt.Rows.Count;

            int i = 0;
            while (i < Count)
            {
                DataRow dr = dt.Rows[i];

                SYMBOL_INFO n = new SYMBOL_INFO();
                n.SymbolName = Convert.ToString(dr["VariableName"]);
                i++;
                if (n.SymbolName == null ||
                     n.SymbolName == "")
                    continue;
                n.Type = ConvertCellValueToType((String)dr["Type"]);

                if (n.Type == TYPE_INFO.TYPE_ILLEGAL)
                {

                    Console.WriteLine("The Column " + n.SymbolName + "Contains " + "illegal type in VariableMappings\r\n");
                    rule_compile_flag = false;
                }

                SetDefaultValue(n,(String) dr[3]);


                ret.Add(n.SymbolName.Trim(), n);
                dependency.Add(n.SymbolName.Trim(), new List<string>());

               

            }

            if (rule_compile_flag == false)
                return null;

            return ret;

        }
        /// <summary>
        ///   
        /// </summary>
        /// <returns></returns>

        String  EmitFunctions()
        {
            DataSet ds = m_reader.GetData("FUNCTIONS$");

            if (ds == null)
            {
                return null;
            }


            DataTable dt = ds.Tables[0];

         

            if (dt == null)
                return null;

            int Count = dt.Rows.Count;

            int i = 0;

            String rs = "//------- Function spit from Functions Tab \r\n\r\n";

            while (i < Count)
            {
                DataRow dr = dt.Rows[i];


               
                String FunctionBody = Convert.ToString(dr[1]);
                i++;
                if (FunctionBody == null ||
                    FunctionBody == "")
                    continue;

                rs += "// --- " + dr[0] + "\r\n";

                List<String> rst = SplitLines(FunctionBody);

               

                foreach (String s in rst)
                {
                    rs += "//-------- " + s + "\r\n";
                }

                //----------- Generate the Rule Prolog
                //-----------
                //----------- 

                String Splitbody = "";

                foreach (String st in rst)
                {
                    Splitbody += st + "\r\n";
                }



                RDParser par = new RDParser(rs + Splitbody);

                TModule module = par.DoParse();

                if (module == null)
                {

                    Console.WriteLine("//Failed to Compile Rule " +dr[0]);
                    Console.WriteLine("//=====================================");
                    rule_compile_flag = false;
                }
                else
                {

                    rs += Splitbody + "\r\n\r\n\r\n";
                }



            }



            if (rule_compile_flag == false)
                return null;

            return rs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ruletab"></param>
        /// <param name="compiled_so_far"></param>
        /// <returns></returns>
        String EmitRuleSnippets(String ruletab,String compiled_so_far)
        {
            DataSet ds = m_reader.GetData(ruletab);

            if (ds == null)
                return null;

            DataTable dt = ds.Tables[0];

            if (dt == null)
                return null;

            int Count = dt.Rows.Count;

            

            int i = 0;

            String rs = "//------- Rule for Programs " + ruletab +"\r\n\r\n";
            String FuncText2 = ""; 
            while (i < Count)
            {
                DataRow dr = dt.Rows[i];



                String RuleFunction  = Convert.ToString(dr["Rule Name"]);
                i++;
                if (RuleFunction == null ||
                    RuleFunction == "")
                    continue;

                FuncText2 = RuleFunction;

                String RuleBody = Convert.ToString(dr[1]);
                if (RuleBody == null ||
                    RuleBody == "")
                    continue;


                rs += "// --- " + RuleFunction + "\r\n";

                List<String> rst = SplitLines(RuleBody);

              

                foreach (String s in rst)
                {
                    rs += "//-------- " + s + "\r\n";
                }

                 //----------- Generate the Rule Prolog
                //-----------
                //----------- 

                String Splitbody = "";

                foreach (String st in rst)
                {
                    Splitbody += st + "\r\n";
                }

                String FuncText = GenerateRuleProlog(RuleFunction, Splitbody);


                if (FuncText == null)
                {
                    Console.WriteLine("Failed to Compile Rule " + FuncText2 + " in " + ruletab);
                    Console.WriteLine("=====================================");
                    rule_compile_flag = false;
                }

                RDParser par = new RDParser(compiled_so_far +"\r\n" + rs + "\r\n" + FuncText);

                  TModule module = par.DoParse();

                  if (module == null)
                  {

                      Console.WriteLine("Failed to Compile Rule " + FuncText2 + " in " + ruletab);
                      Console.WriteLine("=====================================");
                      rule_compile_flag = false;
                  }
                  else
                  {
                      rs += FuncText + "\r\n\r\n\r\n";
                  }
            }

            
         

            if (rule_compile_flag == false)
                return null;

            return compiled_so_far + rs;


        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        List<String> SplitLines(String str )
        {
            List<String> ret = new List<String>();

            int Length = str.Length;
            int i = 0;
            while (i < Length)
            {
                String curr_line = "";

                while (i < Length)
                {
                    curr_line += str[i];
                    
                    if (str[i] == '\n')
                    {
                        i++;           
                        break;
                    }
                    i++;
                }

                ret.Add(curr_line);

            }
            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RuleName"></param>
        /// <param name="RuleBody"></param>
        /// <returns></returns>
        String GenerateRuleProlog(String RuleName,String RuleBody)
        {
            String[] rstr = RuleName.Split("(,)".ToCharArray());
            String FunctionBody = "//---------- Function Generated \r\n";

            try
            {
              

                ////////////////////////////
                //
                // Add the rule to Symbolic Variable..
                //
                SYMBOL_INFO st = new SYMBOL_INFO();
                st.SymbolName = rstr[0].Trim();
                st.Type = TYPE_INFO.TYPE_BOOL;
                st.bol_val = false;
                rule_sym.Add(st.SymbolName, st);

               

                FunctionBody += "FUNCTION BOOLEAN " + rstr[0] + "( ";
                int i = 1;
                while (i < rstr.Length - 1)
                {
                    if (rstr[i].Trim() == "")
                        break;
                    FunctionBody += FindType(rstr[i]) + "  " + rstr[i];
                    FunctionBody += (i == rstr.Length - 2) ? ")" : ",";
                    List<string> str = dependency[rstr[i].Trim()];
                    str.Add(rstr[0].Trim());
                    i++;
                }
                FunctionBody += "\r\n";

                FunctionBody += RuleBody + "\r\n";
                FunctionBody += "END \r\n\r\n";


                return FunctionBody;
            }
            catch (Exception  e)
            {
                return null;

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        String FindType(String var)
        {
            if (!glb_sym.ContainsKey(var.Trim()))
            {
                return " ";
            }

            SYMBOL_INFO sym = glb_sym[var.Trim()];

            if (sym == null)
                return " ";

            if (sym.Type == TYPE_INFO.TYPE_BOOL)
                return "BOOLEAN  ";
            else if (sym.Type == TYPE_INFO.TYPE_NUMERIC )
                return "NUMERIC ";
            else if (sym.Type == TYPE_INFO.TYPE_STRING )
                return "STRING ";

            return "  ";
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool EmitPrologue()
        {
            Console.WriteLine("//-------------------------Emit the prolog \r\n");
            return true;
                
        }
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sym"></param>
        /// <returns></returns>
        public string GetTypedValue(SYMBOL_INFO sym)
        {
            if (sym.Type == TYPE_INFO.TYPE_BOOL)
            {
                return sym.bol_val ? "true" : "false";
            }
            else if  (sym.Type == TYPE_INFO.TYPE_STRING)
            {
                return "'"+sym.str_val+"'";
            }

            else  if (sym.Type == TYPE_INFO.TYPE_NUMERIC)
            {
                return sym.dbl_val.ToString() ;   //.str_val;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool EmitEpilogue()
        {
            Console.WriteLine("//-------------------------Emit the epilog \r\n");
            GenerateEngineCtor();
            GenerateRuleDictionary();
            GenerateRuleInvocation();
            GenerateProgramRuleInvocation();
            GenerateDependencies();
            GenerateTruthDictionary();
            GenerateAPICalls();
            GenerateEvaluateKernel();
          //  GenerateTestProgram01();
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        private void GenerateAPICalls()
        {
            StringBuilder br = new StringBuilder();

            //----------------------------- SetCurrentEnvironment API
            br.Append("RuleEvaluator.prototype.SetCurrentEnvironment = function ( prule_dict ) \r\n");
            br.Append("{\r\n");
            br.Append("\tfor (var cat in prule_dict ) \r\n\tthis.rule_dict[cat] = prule_dict[cat];");
            br.Append("\r\n\tthis.EvaluateAll();\r\nthis.eval_array=[];\r\n}\r\n");
            //----------------------- GetCurrentEnvironment API
            br.Append("RuleEvaluator.prototype.GetCurrentEnvironment = function ( )\r\n");
            br.Append("{\r\n");
            br.Append("\treturn this.rule_dict; \r\n } \r\n");
            //--------------------- Change Fact API
            br.Append("RuleEvaluator.prototype.ChangeFact = function (key,value) {\r\n");
            br.Append("\tthis.rule_dict[key]=value;\r\n\tthis.eval_array.push(key);\r\n}\r\n");
            //--------------------- ResetEvaluationContext API
            br.Append("RuleEvaluator.prototype.ResetEvaluationContext = function() {\r\n");
            br.Append("\tthis.eval_array = [];\r\n");
            br.Append("}\r\n\r\n");
            //--------------------- EvaluateAll API
            br.Append("RuleEvaluator.prototype.EvaluateAll = function( ) \r\n\r\n");
            br.Append("{\r\n");
            br.Append("\treturn this.Evaluate(this.rule_dict,null);\r\n}\r\n\r\n");
            //--------------------- EvaluateDelta API
            br.Append("RuleEvaluator.prototype.EvaluateDelta = function() {\r\n");
            br.Append("\treturn this.Evaluate(this.rule_dict,this.eval_array);\r\n");
            br.Append("}\r\n\r\n");
            
            Console.WriteLine(br);


        }
        /// <summary>
        /// 
        /// </summary>
        private void GenerateEngineCtor()
        {
            StringBuilder br = new StringBuilder();
            br.Append("\r\n\r\n");
            br.Append("function RuleEvaluator() {\r\n");
            br.Append("\tthis.rule_dict = this.RetrieveRuleDictionary();\r\n");
            br.Append("\tthis.truth_dict = this.RetrieveTruthDictionary();\r\n");
            br.Append("\tthis.eval_array = [];\r\n");
            br.Append("\tthis.invoke_dict = this.RetrieveInvocationDictionary();\r\n");
            br.Append("\tthis.invoke_program_dict = this.RetrieveInvokeProgramDictionary();\r\n");
            br.Append("\tthis.depend_dict = this.RetrieveDependencyDictionary();\r\n}\r\n");
            br.Append("\r\n");
           Console.WriteLine(br);
        }


        /// <summary>
        /// 
        /// </summary>
        private void GenerateRuleDictionary()
        {
            StringBuilder br = new StringBuilder();
            br.Append("RuleEvaluator.prototype.RetrieveRuleDictionary= function() {\r\n");
            br.Append("\t\tvar rule_dict = {};\r\n ");
            foreach (var str in glb_sym.Keys)
            {

                br.Append("\t\trule_dict[" + "\"" + str + "\"]=" + GetTypedValue(glb_sym[str]) + ";\r\n");
            }
            br.Append("\r\n");
            br.Append("\treturn rule_dict;\r\n}\r\n");
            Console.WriteLine(br);
        }
        /// <summary>
        /// 
        /// </summary>
        private void GenerateEvaluateKernel()
        {
           

            //-------------------- Generate Rule Engine class....

            StringBuilder br = new StringBuilder();

             br.Append("RuleEvaluator.prototype.ProgramEvaluation = function()\t\n");
             br.Append("{\r\n");
             foreach (string v in rule_sym.Keys)
                 br.Append("var " + v.ToUpper() + " = this.truth_dict[" + "\"" + v + "\"];\r\n");
 
            br.Append(" var program_truth = {}; \r\n");
            br.Append("for (var cat in this.invoke_program_dict ) \r\n");
            br.Append("      program_truth[cat]=eval(this.invoke_program_dict[cat]);   \r\n");               
            br.Append("   return program_truth; \r\n");
            br.Append(" } \r\n");
            //---------------------- Rest of the Rule Engine class....
            br.Append("\r\n\r\n");
            br.Append("//-------------------------\r\n");
            br.Append("//-------------------------\r\n");
            br.Append("RuleEvaluator.prototype.Evaluate = function(prule_dict,param_array) {\r\n");
            br.Append("if (prule_dict == null || prule_dict.length == 0)\r\n");
            br.Append("                return null;                     \r\n");    
            foreach (string v in dependency.Keys)
                br.Append("var " + v.ToUpper() + " = prule_dict[" + "\"" + v + "\"];\r\n");

            br.Append("//--------------------------- End data extraction\r\n");

            
           br.Append(" if ( param_array == null || param_array.length == 0) \r\n");
           br.Append("{\r\n");
           br.Append(" for (var cat in this.invoke_dict ) \r\n");
           br.Append("      this.truth_dict[cat] = eval(this.invoke_dict[cat]); \r\n");
           br.Append("    return this.ProgramEvaluation(); \r\n");
           br.Append("}\r\n");
   
           br.Append("for ( var index = 0; index < param_array.length; ++index) {\r\n");
           br.Append("         var str = param_array[index]; \r\n");
           br.Append("         var arr = this.depend_dict[str]; \r\n");
           br.Append("  if ( arr == null || arr.length == 0 ) \r\n");
           br.Append("           continue;  \r\n");
           br.Append(" for (var index2 = 0; index2 < arr.length; ++index2) \r\n");
           br.Append("   this.truth_dict[arr[index2]] = eval(this.invoke_dict[arr[index2]]); \r\n");
           br.Append("}\r\n");    
           br.Append("    return this.ProgramEvaluation(); \r\n");
           br.Append("}\r\n");
           Console.WriteLine(br); 


        }

       
        /// <summary>
        /// 
        /// 
        /// </summary>
        private void GenerateTruthDictionary()
        {
            StringBuilder br = new StringBuilder();
            br.Append("RuleEvaluator.prototype.RetrieveTruthDictionary = function() {\r\n");
            br.Append("\tvar truth_dict = {}; \r\n");

            foreach (string v in rule_sym.Keys)
                br.Append("\ttruth_dict[" + "\"" + v + "\"]=false;\r\n");
            br.Append("\treturn truth_dict;\r\n\r\n");
            br.Append("}\r\n");
            Console.WriteLine(br);

            return;

        }

      
        /// <summary>
        /// 
        /// </summary>
        public void GenerateRuleInvocation()
        {
           
            StringBuilder br = new StringBuilder();
            br.Append("RuleEvaluator.prototype.RetrieveInvocationDictionary= function() {\r\n");
            br.Append("\tvar invoke_dict = {}; \r\n");
            foreach (var rs in rule_sym.Keys)
            {

                br.Append("\tinvoke_dict[\"" + rs + "\"]=\"" + ConcatFormals(this._rule_snippets,rs) +"\";\r\n");
            }
            br.Append("\r\n");
            br.Append("\treturn invoke_dict;\r\n}\r\n");
            Console.WriteLine(br);

        }
        /// <summary>
        /// 
        /// </summary>
        public void GenerateProgramRuleInvocation()
        {
            StringBuilder br = new StringBuilder();

            br.Append("RuleEvaluator.prototype.RetrieveInvokeProgramDictionary = function() {\r\n");
            ArrayList elist = _eligible_rules.GetProcs();

            br.Append("\tvar invoke_program_dict = {};\r\n ");
            foreach (Procedure proc in elist )
            {

                br.Append("\tinvoke_program_dict[\"" + proc.Name + "\"]=\"" + ConcatFormals(this._eligible_rules, proc.Name) + "\";\r\n");
            }

            br.Append("\treturn invoke_program_dict;\r\n}\r\n");
            Console.WriteLine(br);

        }
        /// <summary>
        /// 
        /// </summary>
        public void GenerateTestProgram01()
        {
            StringBuilder br = new StringBuilder();
            br.Append("console.log(\"============================================================\");\r\n");
            br.Append("var rule = new RuleEvaluator();\r\n");
            br.Append("console.log(\"//============Populating Rule Dictionary\");\r\n");
            br.Append("rule.SetCurrentEnvironment(rule.RetrieveRuleDictionary());\r\n");
            br.Append("var program_dict = rule.EvaluateAll();\r\n");
            br.Append("for( var el in program_dict )\r\n");
            br.Append(" console.log(\"Elligibility for \" + el + \"= \" + program_dict[el]);\r\n");
            br.Append("console.log(//==============Reset Evaluation Context,Change Fact a = -1\r\n);\r\n");
            br.Append("rule.ResetEvaluationContext();\r\n");
            br.Append("rule.ChangeFact(\"a\",-1); \r\n");
            br.Append("program_dict = rule.EvaluateDelta();\r\n");
            br.Append("for( var el in program_dict )\r\n");
            br.Append(" console.log(\"Elligibility for \" + el + \"= \" + program_dict[el]);\r\n ");
            Console.WriteLine(br); 
            
            /*
console.log("==============Reset Evaluation Context,Change Citizenship to Asian\r\n");
rule.ResetEvaluationContext();
rule.ChangeFact("Citizenship",'Asian'); 
program_dict = rule.EvaluateDelta();

for( var el in program_dict )
  console.log("Elligibility for " + el + "= " + program_dict[el]);

console.log("==============Reset Evaluation Context,Change a = 2\r\n");

rule.ResetEvaluationContext();
rule.ChangeFact("a",2); 
program_dict = rule.EvaluateDelta();

 for( var el in program_dict )
  console.log("Elligibility for " + el + "= " + program_dict[el]);
            */
        }
        /// <summary>
        /// 
        /// </summary>
        public void GenerateDependencies()
        {
                    //depend_dict = {};

                    //depend_dict["a"] = ["R11","R12","R13"];
                    //depend_dict["b"] = ["R12","R13"];
                    //depend_dict["c"] = [];
                    //depend_dict["Citizenship"] = [ "R14" ];
                    //depend_dict["Insurance"] = ["R15"];

            ArrayList elist = _eligible_rules.GetProcs();
            StringBuilder br = new StringBuilder();
            br.Append("RuleEvaluator.prototype.RetrieveDependencyDictionary = function(){\r\n");
            br.Append("\tvar depend_dict = {};\r\n ");

            
            foreach (string rst in dependency.Keys)
            {
                List<string> rs = dependency[rst];
                br.Append("\tdepend_dict[\"" + rst + "\"]=[");
                int i = 0;

                foreach (string temp in rs)
                {
                    br.Append("\"" + temp + "\"");

                    if (i < rs.Count - 1)
                        br.Append(",");
                    i++;
                    

                }

                br.Append("];\r\n");

                
            }

            br.Append("\r\n");
            br.Append("\treturn depend_dict; \r\n}\r\n");
            Console.WriteLine(br);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private string ConcatFormals(TModule mod , string func)
        {

            Procedure p = mod.Find(func);
            ArrayList arr = p.FORMALS;

            string fstr = func + "(";
            int i=0;


            foreach (SYMBOL_INFO smb in arr)
            {
                if (i < arr.Count - 1)
                    fstr += smb.SymbolName + ",";
                else
                    fstr += smb.SymbolName;
                i++;

            }
            fstr += ")";
            return fstr;

        }




    }
}
