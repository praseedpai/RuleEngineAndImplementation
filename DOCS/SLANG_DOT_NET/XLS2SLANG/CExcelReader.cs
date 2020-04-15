using System;
using System.Data;
using System.Data.OleDb;
using System.Collections;



namespace XLS2SLANG
{
    /// <summary>
    /// A class to handle the complexity of reading 
    /// the contents of Excel files. Uses oledb provider
    /// for jet to communicate to Excel. Also uses
    /// Oracle DB for validation of Cell contents against
    /// the DB.
    /// 
    /// </summary>
    public class CExcelReader
    {
        private OleDbConnection conn_excel = null;    // Connection to Excel
    
        /// <summary>
        ///  Hash Table for storing Data Tables
        ///  Excel Data is retrieved and stored as a DataTables
        ///  in this HashTable
        /// </summary>
        /// 
        private Hashtable m_Table = new Hashtable();


        ///
        ///
        ///
        ///

        object[] default_alloc;

        /// <summary>
        ///   Close the connection to oracle
        ///   Close the connection to excel
        /// </summary>
        public void Close()
        {

           

            if (conn_excel != null)
            {
                ClearHashTable();
                conn_excel.Close();
                conn_excel.Dispose();
                conn_excel = null;
               
            }

        }

        /// <summary>
        ///  Compare the Key fields Given in ArrayList
        ///  against a data record. This routine will
        ///  Check for invalid data row
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="col"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        /// 
        private bool CompareOne(DataRow rw, DataColumnCollection col, ArrayList arr)
        {
            //////////////////////////////////
            ///
            ///  # of key fields
            ///  
            int Count = arr.Count;

            if (Count == 0)
                return false;
            int index = 0;
            /////////////////////////////////////////
            ///   
            ///   Cycle through all the fields
            ///
            while (index < Count)
            {
                //----------- Retrieve the column name
                String str = (String)arr[index];

                //---------------- retrieve column data
                object obj_rec = rw[str];

                ///////////////////////////////////////////
                ///
                ///   if type does not match
                ///
                if (obj_rec.GetType() != col[str].DataType)
                {
                    return false;
                }
                else if (obj_rec.GetType() == Type.GetType("System.String"))
                {
                    ///////////////////////////////////////////
                    ///  if empty data 
                    ///
                    String rec = (String)obj_rec;
                    if (rec.Trim() == "")
                        return false;

                }

                index++;
            }

            return true;
        }

        /// <summary>
        ///   Check whether spread sheet contains proper header. This routine is written
        ///   to elimate those rows which are considered by excel
        ///   as record and will appear empty as humans. 
        ///   Such rows should be in the tail of the cell.
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        public bool CheckHeader(String sheet, ArrayList arr)
        {
            //////////////////////////////////////////////////////
            ///  Retrieve table associated with sheet
            ///
            DataTable tab = (DataTable)m_Table[sheet + "$"];
            ////////////////////////////////////
            /// Retrieve meta data for the columns
            ///
            DataColumnCollection colls = tab.Columns;

            int fcount = arr.Count;
            int ccount = colls.Count;

            if (fcount <= 0 || ccount <= 0)
                return false;

            String colname = "";
            bool bVal = false;

            for (int j = 0; j < fcount; ++j)
            {

                colname = arr[j].ToString().Trim().ToUpper();

                bVal = false;

                for (int k = 0; k < ccount; ++k)
                {
                    String tcolumn = colls[k].ColumnName.Trim().ToUpper();

                    if (colname == tcolumn)
                    {
                        bVal = true;
                        break;
                    }

                }

                if (!bVal)
                    return false;


            }

            return true;



        }


        /// <summary>
        ///   Clean up data in a sheet. This routine is written
        ///   to elimate those rows which are considered by excel
        ///   as record and will appear empty as humans. 
        ///   Such rows should be in the tail of the cell.
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        public bool CleanUpCells(String sheet, ArrayList arr)
        {
            //////////////////////////////////////////////////////
            ///  Retrieve table associated with sheet
            ///
            DataTable tab = (DataTable)m_Table[sheet + "$"];
            ////////////////////////////////////
            /// Retrieve meta data for the columns
            ///
            DataColumnCollection colls = tab.Columns;
            /////////////////////////////////////
            ///
            ///  # of Row 
            ///
            int row_count = tab.Rows.Count;

            if (row_count == 0)
                return false;
            //////////////////////////////////////
            ///  index used for iteration of celss
            ///
            int index = 0;
            /////////////////////////////////////////
            /// 
            ///  
            bool first_time = false;
            int start_rec = -1;

            while (index < row_count)
            {
                DataRow rw = tab.Rows[index];

                if (CompareOne(rw, colls, arr) == false)
                {
                    /////////////////////////////////////////
                    ///  An Empty Record
                    ///

                    if (first_time == false)
                    {
                        start_rec = index;
                        first_time = true;
                    }


                }
                else
                {
                    ///////////////////////////////////////
                    ///  if already an empty record and found
                    ///  a genuine record after that , return
                    ///  failure. Sheet is invalid
                    ///
                    if (first_time == true)
                    {
                        return false;
                    }

                }
                index++;
            }

            ////////////////////////////////
            ///
            ///  IF whitespace rows are found
            ///
            if (start_rec != -1)
            {

                ///////////////////////////////
                ///  # of records to be cleaned up
                ///
                int num_rec = row_count - start_rec;
                int i = 0;

                while (i < num_rec)
                {
                    ///////////////////////////////////
                    /// Iterate the list and delete
                    /// note :- start_rec is not advanced 
                    tab.Rows[start_rec++].Delete();
                    i++;
                }
                //////////////////////////////////////
                /// Accept the changes
                ///
                tab.AcceptChanges();

                if (tab.Rows.Count == 0)
                {
                    CSyntaxErrorLog.AddLine("All the record is invalid because of some missing fields");
                    return false;

                }


            }

            return true;

        }

        /// <summary>
        ///    Open the connections to oracle and Excel 
        /// </summary>
        /// <param name="FileName"></param>
        private void OpenConnections(String ExcelConStr, String OrclConStr)
        {
            try
            {

                if (conn_excel != null)
                {
                    System.Diagnostics.EventLog.WriteEntry("ForeCastLog", "Excel connection already opened");
                }

                conn_excel = new OleDbConnection(ExcelConStr);
                conn_excel.Open();

               

            }
            catch (Exception e)
            {
               // System.Diagnostics.EventLog.WriteEntry("ForeCastLog", e.ToString());
                CSyntaxErrorLog.AddLine(e.ToString());
                Close();
               

            }



        }

        /// <summary>
        ///   Routine for Meta Data collection of Excel sheets
        ///   
        /// </summary>
        /// <returns></returns>
        private DataTable GetTables()
        {
            try
            {

                DataTable schemaTable = conn_excel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                    new object[] { null, null, null, "TABLE" });
                return schemaTable;
            }
            catch (Exception e)
            {
                //System.Diagnostics.EventLog.WriteEntry("ForeCastLog", e.ToString());
                CSyntaxErrorLog.AddLine(e.ToString());

                return null;
            }



        }

        /// <summary>
        ///    Ctor
        /// </summary>
        /// <param name="ExcelFile"></param>
        public CExcelReader(String ExcelConStr, String OrclConStr)
        {
            OpenConnections(ExcelConStr, OrclConStr);
            FillHashTable();
            int xt = 0;
            double one_slice = (100.0 / 24.0) / 100;
            default_alloc = new object[24];
            for (xt = 0; xt < 24; ++xt)
            {
                default_alloc[xt] = one_slice;
            }
        }
        /// <summary>
        ///   Fill the Data into Hash table
        /// </summary>
        private void FillHashTable()
        {
            ///////////////////////////////////////
            ///
            ///  Get the name of the Work sheets
            ///  
            String[] Sheets = GetWorkSheets();
            ////////////////////////////////////////////
            ///
            ///  Cycle through the work sheets
            ///
            for (int i = 0; i < Sheets.Length; ++i)
            {
                DataSet st = GetData(Sheets[i]);
                DataTable t = st.Tables[0];
                m_Table.Add(Sheets[i], t);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void ClearHashTable()
        {

            String[] Sheets = GetWorkSheets();

            if (Sheets == null)
                return;

            for (int i = 0; i < Sheets.Length; ++i)
            {
                DataTable t = (DataTable)m_Table[Sheets[i]];
                t.Clear();
                t.Dispose();
            }

            m_Table.Clear();




        }

        /// <summary>
        ///    Get the Data set corresponding to Work sheet name
        /// </summary>
        /// <param name="SheetName"></param>
        /// <returns></returns>
        public DataSet GetData(String SheetName)
        {
            try
            {
                String Query = "SELECT * FROM [" + SheetName + "];";
                OleDbDataAdapter myCommand = new OleDbDataAdapter(Query, conn_excel);
                DataSet myDataSet = new DataSet();
                myCommand.Fill(myDataSet, "ExcelInfo");
                return myDataSet;
            }
            catch (Exception e)
            {
                return null;
            }

        }
        /// <summary>
        ///   Retrieve the names of the work sheets
        /// </summary>
        /// <returns></returns>
        public String[] GetWorkSheets()
        {
            DataTable x = GetTables();
            if (x == null)
                return null;
            int num_worksheets = x.Rows.Count;
            String[] ret_value = new String[num_worksheets];
            for (int i = 0; i < num_worksheets; ++i)
                ret_value[i] = x.Rows[i]["Table_Name"].ToString();
            return ret_value;
        }
        /////////////////////////////////////////////
        ///
        /// Get the Values for the range 
        ///
        public object[] GetColumnValues(String SheetName, String CellName,
            int rec1, int rec2)
        {
            DataTable t = (DataTable)m_Table[SheetName + "$"];
            object[] x = new object[(rec2 - rec1) + 1];
            int index = 0;

            double xt = 0.0;

            for (int j = rec1; j <= rec2; ++j)
            {

                x[index] = t.Rows[j][CellName];
                xt = xt + Convert.ToDouble(x[index]);
                index++;
            }

            xt = Math.Round(xt, 3);

            if (xt == 0.0)
            {
                return default_alloc;

            }
            else
            {
                return x;
            }

        }
        /// <summary>
        ///   
        /// </summary>
        /// <param name="SheetName"></param>
        /// <returns></returns>

        public bool CheckHourly()
        {

            String SheetName = "HOURLY_DISTRIBUTION";

            String[] days;

            days = new String[7];

            days[0] = "SUNDAY";
            days[1] = "MONDAY";
            days[2] = "TUESDAY";
            days[3] = "WEDNESDAY";
            days[4] = "THURSDAY";
            days[5] = "FRIDAY";
            days[6] = "SATURDAY";


            DataTable tab = GetDataTable(SheetName);

            int rec_count = tab.Rows.Count;
            int index = 0;

            int jt = 0;
            while (index < rec_count)
            {
                DataRow row = tab.Rows[index];
                int ctr = Convert.ToInt16(row["HOUR"]);
                String hr = Convert.ToString(row["DAY"]).Trim().ToUpper();
                if (hr != days[jt % 7] || ctr != index % 24)
                {
                    CSyntaxErrorLog.AddLine("HOURLY_DISTRIBUTION worksheet invalid at row " + Convert.ToString(index + 1));
                    return false;
                }

                index++;

                if (index % 24 == 0)
                    jt++;



            }

            return true;


        }
        /// <summary>
        ///    
        /// </summary>
        /// <param name="SheetName"></param>
        /// <returns></returns>
        public double RecCount(String SheetName)
        {
            DataTable t = GetDataTable(SheetName);
            return t.Rows.Count;
        }
        /// <summary>
        ///    Take the Sum of the Column
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="CellName"></param>
        /// <param name="rec1"></param>
        /// <param name="rec2"></param>
        /// <returns></returns>
        public double SumColumn(String SheetName, String CellName,
            int rec1, int rec2, int ScaleFactor)
        {
            DataTable t = (DataTable)m_Table[SheetName + "$"];
            double dtr = 0;
            int RoundFactor = 3;
            for (int j = rec1; j <= rec2; ++j)
            {


                dtr = dtr + Convert.ToDouble(t.Rows[j][CellName]) * ScaleFactor;

            }

            return Math.Round(dtr, RoundFactor);

        }
        /// <summary>
        ///    Sum Excel data
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="CellName_Start"></param>
        /// <param name="CellName_End"></param>
        /// <returns></returns>
        public double SumRange(String SheetName, String CellName_Start
            , String CellName_End, int ScaleFactor)
        {
            String Column_Start = "";
            String Column_End = "";
            int index = 0;
            int alph = 0;
            int alph1 = 0;

            if (char.IsLetter(CellName_Start[index])
                && char.IsLetter(CellName_End[index]))
            {
                alph = CellName_Start[index] - 'A';
                alph1 = CellName_End[index] - 'A';

                if (alph != alph1)
                {
                    String exception_str = "";
                    exception_str += "Cell Name mismatch" + "\r\n";
                    CSyntaxErrorLog.AddLine(exception_str);
                    throw new Exception( exception_str);

                }

            }
            else
            {

                String exception_str = "";
                exception_str += "Invalid Cell reference" + "\r\n";
                CSyntaxErrorLog.AddLine(exception_str);
                throw new Exception(exception_str);

            }

            Column_Start = CellName_Start.Substring(1);
            Column_End = CellName_End.Substring(1);

            if (!(char.IsDigit(Column_Start[0]) || char.IsDigit(Column_End[0])))
            {
                String exception_str = "";
                exception_str += "Invalid Cell reference" + "\r\n";
                CSyntaxErrorLog.AddLine(exception_str);
                throw new Exception(exception_str);
            }

            int rec_num = Convert.ToInt32(Column_Start) - 2;
            int rec_num1 = Convert.ToInt32(Column_End) - 2;

            String ColumnName = ((DataTable)m_Table[SheetName + "$"]).Columns[alph].ToString();

            return SumColumn(SheetName, ColumnName, rec_num, rec_num1, ScaleFactor);


        }
        ////////////////////////
        ///

        private String ConvertCellToColumn(String SheetName, String Cell)
        {
            String Column = "";
            int index = 0;
            int alph = 0;

            if (char.IsLetter(Cell[index]))
            {
                alph = Cell[index] - 'A';

            }

            Column = Cell.Substring(1);

            if (!char.IsDigit(Column[0]))
            {
                String exception_str = "";
                exception_str += "Invalid Cell reference" + "\r\n";
                CSyntaxErrorLog.AddLine(exception_str);
                throw new Exception( exception_str);

            }

            int rec_num = Convert.ToInt32(Column) - 2;
            String name = ((DataTable)m_Table[SheetName + "$"]).Columns[alph].ToString();
            DataTable t = ((DataTable)m_Table[SheetName + "$"]);
            return t.Rows[rec_num][name].ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="CellName"></param>
        /// <returns></returns>
        public String GetCellValue(String sheetName, String CellName)
        {
            return ConvertCellToColumn(sheetName, CellName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="column"></param>
        /// <param name="rec_num"></param>
        /// <returns></returns>
        public String GetColumnValue(String sheet, String column, int rec_num)
        {
            String name;
            try
            {
                name = ((DataTable)m_Table[sheet + "$"]).Columns[column].ToString();

            }
            catch (Exception)
            {
                CSyntaxErrorLog.AddLine("Column name contains space " + column + " " + sheet);
                throw new Exception("Column name contains space");


            }



            DataTable t = ((DataTable)m_Table[sheet + "$"]);


            Type st = t.Rows[rec_num][name].GetType();
            object obj = (t.Rows[rec_num][name]);

            if (st == Type.GetType("System.DBNull"))
            {
                CSyntaxErrorLog.AddLine("NULL VALUE FOUND IN " + sheet + " " + column);
                CSyntaxErrorLog.AddLine("Entry Line " + Convert.ToString(rec_num + 1));
                return "";
            }
            return obj.ToString();

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public DataTable GetDataTable(String sheet)
        {
            DataTable t = ((DataTable)m_Table[sheet + "$"]);
            return t;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
            //public void DeleteData(string sql)
            //{
            //    try
            //    {
            //        OracleCommand tmpcommand = new OracleCommand(sql, conn_oracle);
            //        tmpcommand.CommandTimeout = 90;
            //        tmpcommand.ExecuteNonQuery();
            //    }
            //    catch (Exception e)
            //    {
            //        CSyntaxErrorLog.AddLine(e.ToString());
            //        Close();
            //        throw new CParserException(-100, "Error Executing Query", -1);
            //    }

            //}
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="Sql"></param>
        ///// <returns></returns>
        //public DataSet GetDataSet(string Sql)
        //{
        //    try
        //    {

        //        OracleCommand tmpcommand = new OracleCommand(Sql, conn_oracle);

        //        tmpcommand.CommandTimeout = 90;
        //        DataSet ds = new DataSet();
        //        OracleDataAdapter da = new OracleDataAdapter(tmpcommand);

        //        da.Fill(ds);

        //        return ds;
        //    }
        //    catch (Exception e)
        //    {
        //        System.Diagnostics.EventLog.WriteEntry("ForeCastLog", e.ToString());
        //        CSyntaxErrorLog.AddLine(e.ToString());
        //        Close();
        //        throw new CParserException(-100, e.ToString(), -1);
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="CellName"></param>
        /// <param name="TableName"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        /// 

        //public bool LookupInDB(String SheetName, String CellName, String TableName, String ColumnName)
        //{
        //    String Clm = ConvertCellToColumn(SheetName, CellName);
        //    String Sql = "Select " + ColumnName + " from " + TableName;
        //    Sql += " Where UPPER(" + ColumnName + ") = '" + Clm.Trim().ToUpper() + "'";
        //    DataSet rset = GetDataSet(Sql);
        //    bool front = rset.Tables[0].Rows.Count > 0;
        //    rset.Clear();
        //    rset.Dispose();
        //    return front;
        //}


        /// <summary>
        ///      Case Sensitive
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="CellName"></param>
        /// <param name="TableName"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        //public bool LookupInDBCaseSensitive(String SheetName, String CellName, String TableName, String ColumnName)
        //{
        //    String Clm = ConvertCellToColumn(SheetName, CellName);
        //    String Sql = "Select " + ColumnName + " from " + TableName;
        //    Sql += " Where " + ColumnName + " = '" + Clm.Trim() + "'";
        //    DataSet rset = GetDataSet(Sql);
        //    bool front = rset.Tables[0].Rows.Count > 0;
        //    rset.Clear();
        //    rset.Dispose();
        //    return front;
        //}
        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>


        //public bool ScanCurrency(String currency)
        //{

        //    String country;
        //    String segment;


        //    //////////////////////////////////////////
        //    ///
        //    ///  Retrieve the Country and the segment from the spread sheet
        //    ///
        //    country = GetCellValue("CONTROL", "B2");
        //    segment = GetCellValue("CONTROL", "B3");

        //    country = country.Trim();
        //    segment = segment.Trim();

        //    String Sql = "Select DISTINCT  Currency_code from OB_HOURLY_REVENUE ";
        //    Sql += "Where " + "UPPER(country_code) = '" + country.ToUpper() + "' AND UPPER(EI_SEGMENT_CODE) = '" + segment.ToUpper() + "'";
        //    Sql += " AND UPPER(Currency_code) = '" + currency.Trim().ToUpper() + "'";

        //    DataTable rset = GetDataSet(Sql).Tables[0];
        //    int rs = rset.Rows.Count;

        //    rset.Dispose();

        //    return rs > 0;

        //}


        ///// <summary>
        /////    
        ///// </summary>
        ///// <param name="SheetName"></param>
        ///// <param name="CellName"></param>
        ///// <param name="TableName"></param>
        ///// <param name="ColumnName"></param>
        ///// <returns></returns>


        //public bool ScanBrand()
        //{

        //    String SheetName;
        //    String CellName;
        //    String ColumnName;

        //    SheetName = "OB_REVENUE_UNITS";
        //    CellName = "BRAND_ID";
        //    ColumnName = "BRAND_ID";


        //    String Sql = "Select BRAND_ID  from BRAND";
        //    DataTable rset = GetDataSet(Sql).Tables[0];
        //    DataColumnCollection col = rset.Columns;
        //    DataTable st = GetDataTable(SheetName);
        //    int iter = st.Rows.Count;
        //    int index = 0;
        //    while (index < iter)
        //    {


        //        object ars = st.Rows[index][CellName];
        //        Type ars_type = ars.GetType();
        //        int rs = 0;

        //        bool nFound = false;

        //        //	object desc = st.Rows[index]["BRAND_LOB"];

        //        if (ars.GetType() != Type.GetType("System.Int32") &&
        //             ars.GetType() != Type.GetType("System.Double"))
        //        {
        //            return false;
        //        }

        //        //	if ( desc.GetType() != Type.GetType("System.String") )
        //        //	{
        //        //                return false;  
        //        // 
        //        //	}

        //        //	String desc_str = desc.ToString().Trim().ToUpper();
        //        int br_value = Convert.ToInt32(ars);

        //        if (br_value == -1)
        //        {
        //            index++;
        //            continue;
        //        }




        //        while (rs < rset.Rows.Count)
        //        {
        //            object rw = rset.Rows[rs][ColumnName];
        //            if (rw.GetType() != Type.GetType("System.Double") &&
        //                 rw.GetType() != Type.GetType("System.Int32") &&
        //                 rw.GetType() != Type.GetType("System.Int16") &&
        //                 rw.GetType() != Type.GetType("System.Decimal"))
        //                return false;

        //            if (ars_type == Type.GetType("System.String"))
        //            {
        //                String a = Convert.ToString(ars);
        //                String b = Convert.ToString(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }



        //            }
        //            else if (ars_type == Type.GetType("System.Double"))
        //            {
        //                Double a = Convert.ToDouble(ars);
        //                Double b = Convert.ToDouble(rw);



        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }
        //            else if (ars_type == Type.GetType("System.Int32"))
        //            {
        //                Int32 a = Convert.ToInt32(ars);
        //                Int32 b = Convert.ToInt32(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }
        //            else if (ars_type == Type.GetType("System.DateTime"))
        //            {
        //                DateTime a = Convert.ToDateTime(ars);
        //                DateTime b = Convert.ToDateTime(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }
        //            else if (ars_type == Type.GetType("System.Decimal"))
        //            {

        //                Decimal a = Convert.ToDecimal(ars);
        //                Decimal b = Convert.ToDecimal(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }


        //            }



        //            rs++;

        //        }

        //        if (nFound == false)
        //            return false;
        //        index++;

        //    }

        //    return true;

        //}







        ///// <summary>
        /////    Scan the DB by sending excel data
        ///// </summary>
        ///// <param name="SheetName"></param>
        ///// <param name="CellName"></param>
        ///// <param name="TableName"></param>
        ///// <param name="ColumnName"></param>
        ///// <returns></returns>

        //public bool ScanBySendingExcelData(String SheetName, String CellName, String TableName, String ColumnName)
        //{

        //    DataTable st = GetDataTable(SheetName);
        //    int iter = st.Rows.Count;
        //    int index = 0;


        //    String Sql;
        //    while (index < iter)
        //    {
        //        object ars = st.Rows[index][CellName];
        //        Type ars_type = ars.GetType();
        //        Sql = "Select " + ColumnName + " from " + TableName;
        //        Sql += " Where UPPER(" + ColumnName + ")=";



        //        if (ars_type == Type.GetType("System.String"))
        //        {
        //            String a = Convert.ToString(ars);
        //            Sql = Sql + "'" + a.ToUpper() + "'";

        //        }
        //        else if (ars_type == Type.GetType("System.Double"))
        //        {
        //            Double a = Convert.ToDouble(ars);
        //            Sql = Sql + Convert.ToString(a);

        //        }
        //        else if (ars_type == Type.GetType("System.Int32"))
        //        {
        //            Int32 a = Convert.ToInt32(ars);
        //            Sql = Sql + Convert.ToString(a);

        //        }
        //        else if (ars_type == Type.GetType("System.DateTime"))
        //        {
        //            DateTime a = Convert.ToDateTime(ars);
        //            Sql = Sql + "'" + Convert.ToString(a) + "'";

        //        }
        //        else if (ars_type == Type.GetType("System.Decimal"))
        //        {

        //            Decimal a = Convert.ToDecimal(ars);
        //            Sql = Sql + Convert.ToString(a);


        //        }


        //        DataTable rset = GetDataSet(Sql).Tables[0];

        //        if (!(rset.Rows.Count > 0))
        //            return false;

        //        index++;

        //    }

        //    return true;

        //}



        /// <summary>
        /// 
        /// </summary>
        /// <param name="SheetName"></param>
        /// <param name="CellName"></param>
        /// <param name="TableName"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        //public bool ScanInDB(String SheetName, String CellName, String TableName, String ColumnName)
        //{
        //    String Sql = "Select " + ColumnName + " from " + TableName;
        //    DataTable rset = GetDataSet(Sql).Tables[0];
        //    DataColumnCollection col = rset.Columns;
        //    DataTable st = GetDataTable(SheetName);
        //    int iter = st.Rows.Count;
        //    int index = 0;
        //    while (index < iter)
        //    {
        //        object ars = st.Rows[index][CellName];
        //        Type ars_type = ars.GetType();
        //        int rs = 0;

        //        bool nFound = false;

        //        while (rs < rset.Rows.Count)
        //        {
        //            object rw = rset.Rows[rs][ColumnName];
        //            if (rw.GetType() != ars_type)
        //                return false;

        //            if (ars_type == Type.GetType("System.String"))
        //            {
        //                String a = Convert.ToString(ars).ToUpper();
        //                String b = Convert.ToString(rw).ToUpper();

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }



        //            }
        //            else if (ars_type == Type.GetType("System.Double"))
        //            {
        //                Double a = Convert.ToDouble(ars);
        //                Double b = Convert.ToDouble(rw);



        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }
        //            else if (ars_type == Type.GetType("System.Int32"))
        //            {
        //                Int32 a = Convert.ToInt32(ars);
        //                Int32 b = Convert.ToInt32(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }
        //            else if (ars_type == Type.GetType("System.Decimal"))
        //            {

        //                Decimal a = Convert.ToDecimal(ars);
        //                Decimal b = Convert.ToDecimal(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }


        //            }
        //            else if (ars_type == Type.GetType("System.DateTime"))
        //            {
        //                DateTime a = Convert.ToDateTime(ars);
        //                DateTime b = Convert.ToDateTime(rw);

        //                if (a == b)
        //                {
        //                    nFound = true;
        //                    break;
        //                }

        //            }


        //            rs++;

        //        }

        //        if (nFound == false)
        //            return false;
        //        index++;

        //    }

        //    return true;

        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="rw"></param>
        /// <param name="arr"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool CompareRecord(
            DataTable t,
            DataRow rw,
            ArrayList arr,
            int index)
        {


            DataColumnCollection colls = t.Columns;
            int st = t.Rows.Count;

            int i = 0;
            Stack pstack = new Stack();

            while (i < st)
            {
                if (i == index)
                {
                    i++;
                    continue;
                }
                DataRow rw1 = t.Rows[i];

                int sn = arr.Count;
                int j = 0;
                bool equal = true;

                while (j < sn)
                {
                    String s = (String)arr[j];
                    if (colls[s].DataType == Type.GetType("System.DateTime") &&
                        (rw[s].GetType() == rw1[s].GetType() && rw[s].GetType() != Type.GetType("System.DBNull")))
                    {
                        DateTime r = Convert.ToDateTime(rw[s]);
                        DateTime r1 = Convert.ToDateTime(rw1[s]);

                        equal = equal && (r == r1);


                    }
                    else if (colls[s].DataType == Type.GetType("System.Double") &&
                        (rw[s].GetType() == rw1[s].GetType() && rw[s].GetType() != Type.GetType("System.DBNull")))
                    {
                        System.Double r = Convert.ToDouble(rw[s]);
                        Double r1 = Convert.ToDouble(rw1[s]);
                        equal = equal && (r == r1);

                    }
                    else if (colls[s].DataType == Type.GetType("System.Int32") &&
                        (rw[s].GetType() == rw1[s].GetType() && rw[s].GetType() != Type.GetType("System.DBNull")))
                    {
                        Int32 r = Convert.ToInt32(rw[s]);
                        Int32 r1 = Convert.ToInt32(rw1[s]);
                        equal = equal && (r == r1);

                    }
                    else if (colls[s].DataType == Type.GetType("System.String") &&
                        (rw[s].GetType() == rw1[s].GetType() && rw[s].GetType() != Type.GetType("System.DBNull")))
                    {
                        String r = Convert.ToString(rw[s]).ToUpper();
                        String r1 = Convert.ToString(rw1[s]).ToUpper();
                        equal = equal && (r == r1);

                    }
                    else if (colls[s].DataType == Type.GetType("System.String") &&
                        (rw[s].GetType() == rw1[s].GetType() && rw[s].GetType() != Type.GetType("System.DBNull")))
                    {
                        Decimal r = Convert.ToDecimal(rw[s]);
                        Decimal r1 = Convert.ToDecimal(rw1[s]);
                        equal = equal && (r == r1);

                    }
                    else
                    {
                        String expression = "";
                        expression = "Invalid Type reference at row" + Convert.ToString(i);
                        CSyntaxErrorLog.AddLine(expression);
                        throw new Exception(expression);

                    }


                    if (!equal)
                        break;

                    j++;

                }

                if (equal)
                    return false;
                i++;


            }
            return true;
        }

        /// <summary>
        ///    Check for Duplicate in a Sheet
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        public bool CheckDuplicate(String sheet, ArrayList arr)
        {
            int nCount = arr.Count;

            if (nCount == 0)
                return false;

            DataTable t = GetDataTable(sheet);
            DataColumnCollection cols = t.Columns;


            try
            {
                int nItems = t.Rows.Count;
                int index = 0;

                while (index < nItems)
                {
                    DataRow row = t.Rows[index];
                    if (CompareRecord(t, row, arr, index) == false)
                        return false;
                    index++;
                }

                return true;
            }
            catch (Exception e)
            {

                CSyntaxErrorLog.AddLine(e.ToString());
                throw e;


            }
        }

        /// <summary>
        ///    Check for Null
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="arr"></param>
        /// <returns></returns>
        public bool HasNull(String sheet, ArrayList arr)
        {

            int nCount = arr.Count;

            if (nCount == 0)
                return false;

            DataTable t = GetDataTable(sheet);
            DataColumnCollection cols = t.Columns;


            try
            {
                int nItems = t.Rows.Count;
                int index = 0;

                while (index < nItems)
                {
                    DataRow row = t.Rows[index];

                    for (int k = 0; k < arr.Count; ++k)
                    {

                        if (row[arr[k].ToString()].GetType() == Type.GetType("System.DBNull"))
                        {
                            CSyntaxErrorLog.AddLine("Null or No data in Column = " + arr[k]);
                            return true;
                        }

                    }

                    index++;
                }
                return false;
            }
            catch (Exception e)
            {

                CSyntaxErrorLog.AddLine(e.ToString());
                throw e;


            }
        }



        /// <summary>
        ///    Find minimum and maximum date
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public DateTime[] GetMinMaxDate(String sheet, String column)
        {
            String name = ((DataTable)m_Table[sheet + "$"]).Columns[column].ToString();
            DataTable t = ((DataTable)m_Table[sheet + "$"]);

            int num_records = t.Rows.Count; // Get number of the rows  

            int rec_num = 0;

            DateTime dtmin = DateTime.Now;
            DateTime dtmax = DateTime.Now;



            while (rec_num < num_records)
            {
                object temp = t.Rows[rec_num][name];
                Type st = temp.GetType();

                if (st != Type.GetType("System.DateTime"))
                {
                    return null;
                }

                if (rec_num == 0)
                {
                    dtmin = dtmax = Convert.ToDateTime(temp);
                }

                DateTime ts = Convert.ToDateTime(temp);

                if (dtmin > ts)
                    dtmin = ts;
                else if (ts > dtmax)
                    dtmax = ts;

                rec_num++;
            }

            DateTime[] retval = new DateTime[2];

            retval[0] = dtmin.AddSeconds(0);
            retval[1] = dtmax.AddSeconds(0);
            return retval;

        }



    }

}
