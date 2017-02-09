using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;

namespace GCS_WPF_2
{
    class DBHelper
    {
        GCS_DB_MODEL model1;

        private static string Connection = @"Data Source=GCS_DB.db; Version = 3;";
        SQLiteConnection conn = new SQLiteConnection(Connection);
        SQLiteCommand cmd = new SQLiteCommand();

        private bool executed = false;

        public DBHelper()
        {
            OpenConnection();
            DeleteAllData();
            string Query = "CREATE TABLE IF NOT EXISTS GCS_DB(ID INTEGER PRIMARY KEY AUTOINCREMENT, "
                + "ALTITUDE TEXT, "
                + "YAW TEXT, "
                + "PITCH TEXT, "
                + "ROLL TEXT, "
                + "LAT TEXT, "
                + "LNG TEXT, "
                + "TIME TEXT)";
            SQLiteCommand cmd = new SQLiteCommand(Query, conn);
            cmd.ExecuteNonQuery();
        }
        public void OpenConnection()
        {
            conn.Open();
        }

        public void InsertData(string alt, string yaw, string pitch, string roll, string lat, string lng, string time)
        {
            //string Query = "INSERT INTO GCS_DB (ALTITUDE, YAW, PITCH, ROLL, LAT, LNG, TIME) " +
            //    "values ('" + alt + "','" + yaw + "','" + pitch + "','" + roll + 
            //    "','" + lat + "','" + lng + "','" + time + "')";
            SQLiteCommand create = new SQLiteCommand(conn);
                    create.CommandText = "INSERT INTO GCS_DB (ALTITUDE, YAW, PITCH, ROLL, LAT, LNG, TIME) " +
                "values ('" + alt + "','" + yaw + "','" + pitch + "','" + roll +
                "','" + lat + "','" + lng + "','" + time + "')";
                    create.Prepare();
                    try
                    {
                        create.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        Debug.Write(ex.Message);
                    }
        }

        public void GetData()
        {
            string Query = "SELECT * FROM GCS_DB";
            conn.Open();
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            SQLiteDataReader sdr = create.ExecuteReader();
            while (sdr.Read())
            {
                Debug.Write(sdr.GetString(1) + ",");
                Debug.Write(sdr.GetString(2) + ",");
                Debug.Write(sdr.GetString(3) + ",");
                Debug.Write(sdr.GetString(4) + ",");
                Debug.Write(sdr.GetString(5) + ",");
                Debug.WriteLine(sdr.GetString(6));
            }
        }

        public int GetLastID(string namaTabel)
        {
            //cmd.CommandText = "SELECT COUNT(*) FROM GCS_DB";
            //cmd.CommandType = CommandType.Text;
            //int RowCount = 0;

            //RowCount = Convert.ToInt32(cmd.ExecuteScalar());
            //return RowCount;
            string Query = "SELECT COUNT(*) FROM "+namaTabel;
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            int total = Convert.ToInt32(create.ExecuteScalar());
            return total;
        }

        public string GetLat(string namaTabel, int ID)
        {
            string Lat="";
            string Query = "SELECT * FROM "+namaTabel+" WHERE ID = " + ID;
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            SQLiteDataReader sdr = create.ExecuteReader();
            while (sdr.Read())
            {
                Lat = sdr.GetString(5);
            }
            return Lat;
        }
        public string GetLng(string namaTabel, int ID)
        {
            string Lng = "";
            string Query = "SELECT * FROM "+namaTabel+" WHERE ID = " + ID;
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            SQLiteDataReader sdr = create.ExecuteReader();
            while (sdr.Read())
            {
                Lng = sdr.GetString(6);
            }
            return Lng;
        }

        public GCS_DB_MODEL GetDataModel(string NamaTabel)
        {
            string Query = "SELECT * FROM "+NamaTabel;
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            SQLiteDataReader sdr = create.ExecuteReader();
            while (sdr.Read())
            {
                //model1 = new GCS_DB_MODEL(sdr.GetString(1), sdr.GetString(2), sdr.GetString(3),
                //    sdr.GetString(4), sdr.GetString(5), sdr.GetString(6)) ;
                model1 = new GCS_DB_MODEL();
                model1.Alt = sdr.GetString(1);
                model1.Yaw = sdr.GetString(2);
                model1.Pitch = sdr.GetString(3);
                model1.Roll = sdr.GetString(4);
                model1.Lat = sdr.GetString(5);
                model1.Lng = sdr.GetString(6);
                model1.Time = sdr.GetString(7);
            }
            //create.ExecuteNonQuery();
            return model1;
        }

        public void DeleteAllData()
        {
            string Query = "DROP TABLE IF EXISTS GCS_DB";
            SQLiteCommand cmd = new SQLiteCommand(Query, conn);
            cmd.ExecuteNonQuery();
        }

        public List<GCS_DB_MODEL> getAllData(string namaTabel)
        {
            List<GCS_DB_MODEL> listDBModel = new List<GCS_DB_MODEL>();
            string Query = "SELECT * FROM "+namaTabel;
            SQLiteCommand create = new SQLiteCommand(Query, conn);
            SQLiteDataReader sdr = create.ExecuteReader();
            while (sdr.Read())
            {
                //model1 = new GCS_DB_MODEL(sdr.GetString(1), sdr.GetString(2), sdr.GetString(3),
                //    sdr.GetString(4), sdr.GetString(5), sdr.GetString(6)) ;
                model1 = new GCS_DB_MODEL();
                model1.Alt = sdr.GetString(1);
                model1.Yaw = sdr.GetString(2);
                model1.Pitch = sdr.GetString(3);
                model1.Roll = sdr.GetString(4);
                model1.Lat = sdr.GetString(5);
                model1.Lng = sdr.GetString(6);
                model1.Time = sdr.GetString(7);
                listDBModel.Add(model1);
            }
            return listDBModel;
        }

        public bool ExcelSave(string timeStart, string totalHours, string totalMin, string totalSec)
        {
            string Query = "CREATE TABLE IF NOT EXISTS GCS_DB_"+timeStart+"(ID INTEGER PRIMARY KEY AUTOINCREMENT, "
                + "ALTITUDE TEXT, "
                + "YAW TEXT, "
                + "PITCH TEXT, "
                + "ROLL TEXT, "
                + "LAT TEXT, "
                + "LNG TEXT, "
                + "TIME TEXT)";
            SQLiteCommand command = new SQLiteCommand(Query, conn);
            command.ExecuteNonQuery();
            SQLiteCommand create = new SQLiteCommand(conn);
            create.CommandText = "INSERT INTO GCS_DB_"+timeStart+"(ID, ALTITUDE, YAW, PITCH, ROLL, LAT, LNG, TIME) " +
                "SELECT * FROM GCS_DB";
            create.Prepare();
            try
            {
                create.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Debug.Write(ex.Message);
            }

            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Add(misValue);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            //string cs = "URI=file:GCS_DB.db";
            string data = String.Empty;

            int i = 0;
            int j = 0;

            string stm = "SELECT * FROM GCS_DB";
            SQLiteCommand cmd = new SQLiteCommand(stm, conn);
            SQLiteDataReader rdr = cmd.ExecuteReader();
            //Kasih nama untuk baris pertama
            xlWorkSheet.Cells[1, 1] = "No.";
            xlWorkSheet.Cells[1, 2] = "Altitude";
            xlWorkSheet.Cells[1, 3] = "Yaw";
            xlWorkSheet.Cells[1, 4] = "Pitch";
            xlWorkSheet.Cells[1, 5] = "Roll";
            xlWorkSheet.Cells[1, 6] = "Latitude";
            xlWorkSheet.Cells[1, 7] = "Longitude";
            xlWorkSheet.Cells[1, 8] = "Time";

            while (rdr.Read()) // Reading Rows
            {
                for (j = 0; j <= rdr.FieldCount - 1; j++) // Looping throw colums
                {
                    data = rdr.GetValue(j).ToString();
                    //Masukkin data ke excel
                    xlWorkSheet.Cells[i + 2, j + 1] = data;
                }
                i++;
            }
            xlWorkSheet.Cells[i+3, 1] = "TOTAL FLIGHT TIME : ";
            xlWorkSheet.Cells[i+3, 2] = totalHours + "jam " + totalMin + " menit " + totalSec + " detik";
            //con.Close();

            //xlWorkBook.SaveAs("sqliteToExcel.xls", Excel.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
            //string nama_file = string.Format("FlightLog-{0:dd_MMMM_yyyy HH:mm:ss}.xlsx", DateTime.Now);
            //Dibuat auto overwrite file yang lama
            xlWorkBook.SaveAs(Environment.CurrentDirectory + @"\FlightRecord\"+timeStart+".xlsx");
            xlWorkBook.Close();
            //xlApp.Visible = true;
            xlApp.Quit();
            return true;
                //xlWorkBook.Close(true, misValue, misValue);

            //releaseObject(xlWorkSheet);
            //releaseObject(xlWorkBook);
            //releaseObject(xlApp);
        }

        //Gaktau method di bawah ini utk apa, sementara biarkan saja dulu ada disini
        private static void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }
    }

}
