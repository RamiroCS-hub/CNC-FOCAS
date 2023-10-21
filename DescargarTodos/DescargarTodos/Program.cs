using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

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
            static ushort _handle = 0;
            static short _ret = 0;
            static bool _exit = false;
            static int len;
            static void Main(string[] args)
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: FanucFocasTutorial1.exe <IP_ADDRESS> <FILENAME> <PROGRAMNAME>");
                    return;
                }
                string ipAddress = args[0];
                string fileName = args[1];
                string programName = args[2];

                Thread t = new Thread(new ThreadStart(ExitCheck));
                t.Start();

                _ret = Focas1.cnc_allclibhndl3(ipAddress, 8193, 6, out _handle);

                /*
                 _ret code = -16 -> Error del socket de comunicación - Revisar tensión - cable etc...
                 _ret code = -15 -> No existe un DDL para cada serie de CNC
                 _ret code = -8 -> Guardado del nuemero _handle falló
                */

                if (_ret != Focas1.EW_OK)
                {
                    Console.WriteLine($"Unable to connect \n\nReturn Code: {_ret}\n\nExiting....");
                    Console.Read();
                }
                else
                {
                    //Ejecuta la función para descargar archivos desde el CNC
                    StringBuilder file = new StringBuilder();
                    file.Append("R:\\20. Digital\\CNC");
                    file.Append(fileName);
                    string filePath = file.ToString();
                    string lastProgramPath = "" // Path donde se va a guardar el programa mas nuevo del torno

                    string messg = downloadFromCNC(programName, filePath);

                    if (!downloadOrNot.Contains("error"))
                    {
                        Console.WriteLine($"{downloadOrNot}");
                        messg = getLastProgram(filePath, lastProgramPath);
                        Console.WriteLine(messg);

                        if (!writeOrNot.Contains("Error"){
                            string messg = uploadDataToDb();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{messg}");
                    }
                }
            }

            private static string getLastProgram(string filePath, string lastProgramPath)
            {
                int cantPrograms = getCantPrograms(filePath) // Se fija cuantos programas se descargaron
                
                try
                {
                    int actualProgram = 0;
                    string line;
                    StreamReader sr = new StreamReader(filePath);
                    StreamWriter sw = new StreamWriter(lastProgramPath);

                    do
                    {
                        line = sr.ReadLine()
                        if (line.Contains("%"))
                        {
                            actualProgram ++
                        }
                        if(actualProgram == (cantPrograms - 1))
                        {
                            sw.WriteLine(line);
                        }
                    }while(sr.EndOfStream);
                    return "Success: The last program was successfuly write";
                }catch(Exception e)
                {
                    return $"Error: {e}";
                }

            }

            public static int getCantPrograms(string filePath) {
                try
                {
                    string line;
                    StreamReader sr = new StreamReader(filePath);

                    do
                    {
                        line = sr.ReadLine();

                        if (line.Contains("%"))
                        {
                            cantPrograms++;
                        }

                    } while (sr.EndOfStream);

                }
                catch (Exception e)
                {
                    return $"Error: {e}";
                }
            }

            public static string downloadFromCNC(string programPath, string filePath)
            {
                int lenLastWrite = 0;
                Boolean contr = true;
                StreamWriter sw = new StreamWriter(filePath); //Crea el archivo que va a escribir
                short typeOfData = 0; // Defino que se van a descargar archivos NC

                if (_handle == 0)
                {
                    messg = ("Error: Handle do not exist");
                    return "";
                }

                const short BUFFSIZE = 1024;
                char[] buff = new char[BUFFSIZE + 1]; //Variable donde se va a guardar el archivo que se esta descargando

                _ret = Focas1.cnc_upstart4(_handle, typeOfData, programPath);
                if (_ret != Focas1.EW_OK) return $"Error: the _ret was:{_ret}";

                do
                {
                    len = BUFFSIZE;
                    _ret = Focas1.cnc_upload4(_handle, ref len, buff);
                    if (_ret == (short)Focas1.focas_ret.EW_BUFFER) //Buffer vacío
                    {
                        messg = "Error: EW_BUFFER - The Buffer is empty or full";
                        continue;
                    }
                    if (_ret == (short)Focas1.focas_ret.EW_OK)
                    {
                        buff[len] = '\0'; //En la ultima posición de lo descargado se coloca '\0' señalizando el final del String leído
                        byte[] dataToWrite = Encoding.UTF8.GetBytes(new string(buff, 0, len)); // Convert char[] to bytes

                        try
                        {
                            sw.WriteLine(dataToWrite)
                        }

                    lenLastWrite = len;
                    }
                    if (buff[len - 1] == '%') //Si el último caracter es '%' significa que ya leyó todo el archivo
                    {
                        contr = false
                        break;
                    }
                    Array.Clear(buff, 0, buff.Length); //Como lo descargado ya se guardo en un archivo se borra todo y se empieza a descargar lo siguiente
                } while (contr);


                _ret = Focas1.cnc_upend4(_handle);

                if (_ret != Focas1.EW_OK)
                {
                    return $"{messg}, the error was: {_ret}";
                }
                return $"{messg}. {_ret}";
            }

            public static string GetToolLenght(short toolGroup, short toolNumb)
            {

                ODBTLIFE4 toolObject = null;
                Focas1.cnc_rd1length(_handle, toolGroup, toolNumb, toolObject);
                string connectionString = "server=NOMBREDELSERVIDOR; database=NOMBREDELADB; User id=IDDEUSUARIO; Password=CONTRASEÑA";
                string query = string.Format("INSERT INTO NOMBREDETABLA (ToolNumber, ToolLenght) values ({1}, {2})", toolNumb, toolObject.data);

                SqlConnection _con;
                try
                {
                    _con = new SqlConnection(connectionString);
                }
                catch (Exception e)
                {
                    return "Error: No se pudo conectar a la base de datos" + e;
                }

                if (existTable(_con) == 1)
                {
                    Console.WriteLine("La tabla existe");
                }
                else
                {
                    string newQuery = @"
                    CREATE TABLE MiTabla (
                        Id INT PRIMARY KEY,
                        Program VARCHAR(255),
                        Date DATE
                    );";
                    databaseQuery(_con, newQuery);
                }

                int response = databaseQuery(_con, query);
                if (response == 1)
                {
                    return "Se efectuo la query exitosamente. Response code: " + response;
                }
                return "Error: Ocurrió un error al insetar los datos en la tabla. Response code: " + response;

            }

            private static int databaseQuery(SqlConnection _con, string query)
            {
                using (SqlCommand _cmd = new SqlCommand(query, _con))
                {
                    _con.Open();
                    int response = _cmd.ExecuteNonQuery();
                    _con.Close();
                    return response;
                }
            }


            private static int existTable(SqlConnection connection)
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
        }
    }
}