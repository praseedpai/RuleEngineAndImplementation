///////////////////////////
// XLS to SLANG...
//
// This Command Line Program Converts XLS rule snippets
// into a Legal SLANG Program... 
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace XLS2SLANG
{
    class Program
    {
        /// <summary>
        ///     Entry Point ...
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            string file_name = null;
            bool _diag = false;

            /////////////////////////////////////////////////
            //
            //  if no argument or empty argument ...use
            //  the default worksheet 
            if (args == null || args.Length == 0)
                file_name = "Federal_Rules_Final_Test.xlsx";
            else 
                file_name = args[0];

            if (args.Length == 2 && String.Compare(args[1], "-d") == 0)
            {
                _diag = true;
            }

            ////////////////////////////////////////////////////////////
            //  Check whether the Excel File exists or not ...
            //
            if (!File.Exists(file_name))
            {
                Console.WriteLine("Unable to Locate file  => " + file_name);
                return;
            }
            ///////////////////////////////////////////////////////////////////
            //
            //  Create the Engine.... 
            //
            XlsSlangEngine s = Helper.CreateEngine(file_name);
         
            if (s == null)
            {

                Console.WriteLine("Failed to initialize the engine ");
                return;
            }

            if (!s.Run(_diag))
            {
                Console.WriteLine("Failed While processing ..............");
                return;
            }
            

          


        }
    }
}
