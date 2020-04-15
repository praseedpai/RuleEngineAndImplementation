using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace XLS2SLANG
{
    public class Helper
    {
        public static XlsSlangEngine CreateEngine(string filename)
        {

            String ConnStr = "Provider=Microsoft.ACE.OLEDB.12.0;";
            ConnStr = ConnStr + "Data Source=" + filename + ";Extended Properties=\"Excel 12.0 Xml;MaxScanRows=0;HDR=YES;IMEX=1\"";
            CExcelReader rd = new CExcelReader(ConnStr, null);

            XlsSlangEngine s = new XlsSlangEngine(rd);
            return s;

        }
    }
        /// <summary>
        ///    
        /// </summary>
        public class CSyntaxErrorLog
        {

            /// <summary>
            ///   instance variables
            /// </summary>
            static int ErrorCount = 0;
            static ArrayList lst = new ArrayList();
            /// <summary>
            ///    Ctor
            /// </summary>
            static CSyntaxErrorLog()
            {

            }


            public static void Cleanup()
            {
                lst.Clear();
                ErrorCount = 0;
            }
            /// <summary>
            ///    Add a Line from script
            /// </summary>
            /// <param name="str"></param>

            public static void AddLine(String str)
            {
                lst.Add(str.Substring(0));
                ErrorCount++;

            }

            /// <summary>
            ///    Get Logged data as a String 
            /// </summary>
            /// <returns></returns>
            public static String GetLog()
            {

                String str = "Syntax Error" + "\r\n";
                str += "--------------------------------------\r\n";

                int xt = lst.Count;

                if (xt == 0)
                {
                    str += "NIL" + "\r\n";

                }
                else
                {

                    for (int i = 0; i < xt; ++i)
                    {
                        str = str + lst[i].ToString() + "\r\n";
                    }
                }
                str += "--------------------------------------\r\n";
                return str;
            }
        }

    }

