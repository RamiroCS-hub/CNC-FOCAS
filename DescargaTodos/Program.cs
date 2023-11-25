using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq.Expressions;

namespace FanucFocasTutorial1
{ 
   class Program
    {
        static ushort _handle = 0;
        static short _ret = 0;
        static bool _exit = false;

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: FanucFocasTutorial1.exe <IP_ADDRESS> <FILEPATH> <LASTPROGRAMPATH>");
                return;
            }
            //Ejecuta la función para descargar archivos desde el CNC
            string ipAddress = args[0];
            string programsPath = "//CNC_MEM/"; // Path de la carpeta donde están los archivos del 
            string filePath = args[1]; // Path donde se van a guardar todos los programas del torno (Incluír nombre del archivo)
            string lastProgramPath = args[2]; // Path donde se va a guardar el programa mas nuevo del torno (Incluír nombre del archivo)

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

                string messg = DownloadFromCNC(programsPath, filePath);

                if (!messg.Contains("Error"))
                {
                    Console.WriteLine($"{messg}");
                    messg = GetLastProgram(filePath, lastProgramPath);
                    Console.WriteLine(messg);
                }
                else
                {
                    Console.WriteLine($"{messg}");
                }
            }
        }

        static string GetLastProgram(string filePath, string lastProgramPath)
        {
            int cantPrograms = GetCantPrograms(filePath); // Se fija cuantos programas se descargaron

            try
            {
                int actualProgram = 0;
                string line;
                StreamReader sr = new StreamReader(filePath);
                StreamWriter sw = new StreamWriter(lastProgramPath);

                do
                {
                    line = sr.ReadLine();
                    if (line.Contains("%"))
                    {
                        actualProgram++;
                    }
                    if (actualProgram == (cantPrograms - 1))
                    {
                        sw.WriteLine(line);
                    }
                } while (!sr.EndOfStream);

                sw.Close();
                sr.Close();
                return "Success: The last program was successfuly write";
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }

        }

        private static int GetCantPrograms(string filePath)
        {
            try
            {
                string line;
                StreamReader sr = new StreamReader(filePath);
                int cantPrograms = 0;
                do
                {
                    line = sr.ReadLine();

                    if (line.Contains("%"))
                    {
                        cantPrograms++;
                    }

                } while (!sr.EndOfStream);
                sr.Close();

                return cantPrograms++;
            }
            catch (Exception e)
            {
                Console.WriteLine($"El error fue:{e}");
                return 0;
            }
        }

        private static string DownloadFromCNC(string programsPath, string filePath)
        {
            int lenLastWrite = 0;
            bool contr = true;
            
            short typeOfData = 0; // Defino que se van a descargar archivos NC
            string messg = "";
            int len;

            if (_handle == 0)
            {
                return "Error: Handle do not exist";
            }

            const short BUFFSIZE = 1024;
            char[] buff = new char[BUFFSIZE + 1]; //Variable donde se va a guardar el archivo que se esta descargando

            _ret = Focas1.cnc_upstart4(_handle, typeOfData, programsPath);
            if (_ret != Focas1.EW_OK)
            {
                Focas1.ODBERR erroObj = new Focas1.ODBERR();
                Focas1.cnc_getdtailerr(_handle, erroObj);
                Console.WriteLine($"Detailed error: {erroObj.err_no}, {erroObj.err_dtno}");
                return $"Error: the _ret was:{_ret}";
            }

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
                    string dataToWrite = ""; // Convert char[] to string
                    for (int i = 0; i < len; i++)
                    {
                        dataToWrite += buff[i];
                    }

                    Console.WriteLine(dataToWrite);
                    using (StreamWriter sw = new StreamWriter(filePath))
                    {
                        sw.WriteLine(dataToWrite);
                        sw.Close();
                    }

                    lenLastWrite = len;
                }
                if (buff[len - 1] == '%') //Si el último caracter es '%' significa que ya leyó todo el archivo
                {
                    contr = false;
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
