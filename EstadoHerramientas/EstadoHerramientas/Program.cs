using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanucFocasTutorial1
{
    class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;
        static int len, fileLen;
        static int startPos = 0;
        static string messg;
        static bool _exit = false;

        static void Main(string[] args)
        {
            Thread t = new Thread(new ThreadStart(ExitCheck));
            t.Start();

            _ret = Focas1.cnc_allclibhndl3("192.168.0.28", 8193, 6, out _handle);

            /*
             _ret code = -16 -> Error del socket de comunicación - Revisar tensión - cable etc...
             _ret code = -15 -> No existe un DDL para cada serie de CNC
             _ret code = -8 -> Guardado del nuemero _handle falló
            */

            if (_ret != Focas1.EW_OK)
            {
                Console.WriteLine($"Unable to connect to 192.168.0.26 on port 8193\n\nReturn Code: {_ret}\n\nExiting....");
                Console.Read();
            }
            else
            {
                Console.WriteLine($"Our Focas handle is {_handle}");

                string mode = GetMode();
                Console.WriteLine($"\n\nMode is: {mode}");

                string status = GetStatus();
                Console.WriteLine($"\n\nStatus is: {status}\n\n");

                /*int partcount = GetPartCount();
                Console.WriteLine($"\n\nPart Count is: {partcount}\n\n");*/

                //Ejecuta la función para descargar un NC al CNC
                short toolGroup = 1; //Cambiar por el numero del grupo de herramienta que se quiere leer
                short toolNumb = 1; //Cambiar por el numero de la herramienta que sse quiere leer
                GetToolLenght(toolGroup, toolNumb);
                Console.WriteLine(downloadOrNot)
        
            }
        }

        public static string GetToolLenght(short toolGroup, short toolNumb) {
            Focas1.cnc_rd1length(_handle, toolGroup, toolNumb).data;
            string connectionString = "server=(local)\SQLExpress;database=Northwind;integrated Security=SSPI;";
            
            string query = "SELECT TOP 5 * FROM dbo.Customers ORDER BY CustomerID";
            databaseQuery(connectionString, query);
            
        }

        private static databaseQuery(string connection, string query)
        {
            using (SqlConnection _con = new SqlConnection(connection))
                
                using (SqlCommand _cmd = new SqlCommand(query, _con))
                {
                    DataTable customerTable = new DataTable("Top5Customers");

                    SqlDataAdapter _dap = new SqlDataAdapter(_cmd);

                    _con.Open();
                    _dap.Fill(customerTable);
                    _con.Close();

                }
            }
        /*string query = @"
CREATE TABLE MiTabla (
    Columna1 INT PRIMARY KEY,
    Columna2 VARCHAR(255) NOT NULL,
    Columna3 DATE
);";
        private static void databaseQuery(string connection, string query)
{
    using (SqlConnection _con = new SqlConnection(connection))
    using (SqlCommand _cmd = new SqlCommand(query, _con))
    {
        _con.Open();
        _cmd.ExecuteNonQuery();
        _con.Close();
    }
}*/
    }

    private static void ExitCheck()
        {
            while (Console.ReadLine() != "exit")
            {
                continue;
            }

            _exit = true;
        }

        public static string GetMode()
        {
            if (_handle == 0)
            {
                Console.WriteLine("9Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Mode = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Mode);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain mode.\nReturn Code: {_ret}");
                return "";
            }

            string modestr = ModeNumberToString(Mode.aut);

            return $"Mode is: {modestr}";
        }

        public static string ModeNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "MDI"; }
                case 1: { return "MEM"; }
                case 3: { return "EDIT"; }
                case 4: { return "HND"; }
                case 5: { return "JOG"; }
                case 6: { return "Teach in JOG"; }
                case 7: { return "Teach in HND"; }
                case 8: { return "INC"; }
                case 9: { return "REF"; }
                case 10: { return "RMT"; }
                default: { return "UNAVAILABLE"; }
            }
        }

        public static string GetStatus()
        {
            if (_handle == 0)
            {
                Console.WriteLine("Error: Please obtain a handle before calling this method");
                return "";
            }

            Focas1.ODBST Status = new Focas1.ODBST();

            _ret = Focas1.cnc_statinfo(_handle, Status);

            if (_ret != 0)
            {
                Console.WriteLine($"Error: Unable to obtain status.\nReturn Code: {_ret}");
                return "";
            }

            string statusstr = StatusNumberToString(Status.run);

            return $"Status is: {statusstr}";
        }

        public static string StatusNumberToString(int num)
        {
            switch (num)
            {
                case 0: { return "STOP"; }
                case 1: { return "HOLD"; }
                case 2: { return "START"; }
                case 3: { return "MDI"; }
                case 4: { return "MSTR"; }
                default: { return "UNAVAILABLE"; }
            }
        }
        /*public static string GetProgramName()
        {
            if (_handle == 0)
            {
                return "UNAVAILABLE";
            }

            Focas1.ODBEXEPRG rdProg = new Focas1.ODBEXEPRG();

            _ret = Focas1.cnc_exeprgname(_handle, rdProg);

            if (_ret != Focas1.EW_OK)
                return _ret.ToString();
            return new string(rdProg.name).Trim('\0');
        }
        public static int GetPartCount()
        {
            Focas1.IODBPSD_1 partcount = new Focas1.IODBPSD_1();
            _ret = Focas1.cnc_rdparam3(_handle, 6711, 0, 8, 0, partcount);
            if (_ret != Focas1.EW_OK)
                return 0;
            return partcount.ldata;
        }

        
        public static string GetProgramComment(short _programNumber)
        {
            Focas1.PRGDIR2 dir = new Focas1.PRGDIR2(); // array to hold the program directory information
            short num = 1; // How much programs to be read

            short ret = Focas1.cnc_rdprogdir2(_handle, 1, ref _programNumber, ref num, dir);

            if (ret != Focas1.EW_OK)
            {
                throw new Exception($"Cannot retrieve data about the program directory. Error {ret}");
            }
            else
            {
                // Convert the character array to a string
                StringBuilder commentBuilder = new StringBuilder();
                for (int i = 0; i < dir.dir1.comment.Length && dir.dir1.comment[i] != '\0'; i++)
                {
                    commentBuilder.Append(dir.dir1.comment[i]);
                }
                return commentBuilder.ToString();
            }
        }*/
    }
}