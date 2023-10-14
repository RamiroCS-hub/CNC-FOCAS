using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Focas1;

namespace FanucFocasTutorial1
{
    class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;
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

                short toolGroup = 1; //Cambiar por el numero del grupo de herramienta que se quiere leer
                string response = GetToolCount(toolGroup);
                Console.WriteLine(response);
            }
        }

        public static string GetToolCount(short toolGroup) {

            ODBTLIFE3 toolObject = null;
            Focas1.cnc_rdcount(_handle, toolGroup, toolObject);
            string connectionString = "server=NOMBREDELSERVIDOR; database=NOMBREDELADB; User id=IDDEUSUARIO; Password=CONTRASEÑA";
            string query = string.Format("INSERT INTO NOMBREDETABLA (ToolGroup, ToolCount) values ({0}, {1})", toolGroup, toolObject.data);

            SqlConnection _con;
            try
            {
                 _con = new SqlConnection(connectionString);
            }catch (Exception e)
            {
                return "Error: No se pudo conectar a la base de datos" + e;
            }

            if (ExistTable(_con) == 1)
            {
                Console.WriteLine("La tabla existe");
            }
            else
            {
                string newQuery = @"
                    CREATE TABLE MiTabla (
                        Id INT PRIMARY KEY,
                        ToolGroup INT NOT NULL,
                        ToolCount FLOAT NOT NULL
                        Date DATE
                    );";
                DatabaseQuery(_con, newQuery);
            }

            int response = DatabaseQuery(_con, query);
            if(response == 1)
            {
                return "Se efectuo la query exitosamente. Response code: " + response;
            }
            return "Error: Ocurrió un error al insetar los datos en la tabla. Response code: " + response;
            
        }

        private static int DatabaseQuery(SqlConnection _con, string query)
        {
            using (SqlCommand _cmd = new SqlCommand(query, _con))
            {
                _con.Open();
                int response = _cmd.ExecuteNonQuery();
                _con.Close();
                return response;
            }
        }
 

    private static int ExistTable(SqlConnection connection)
    {
        using (connection)
        {
                SqlCommand _cmd = new SqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'nombre_de_la_base_de_datos'\r\nAND table_name = 'nombre_de_la_tabla';");
                int result = _cmd.ExecuteNonQuery();
                return result;
        }
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
    }
}