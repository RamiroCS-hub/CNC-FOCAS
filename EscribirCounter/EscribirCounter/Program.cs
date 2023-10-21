using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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
                long newCount = 1; //Cambiar por el valor que quiere que el count de la herramienta tome
                string response = WriteToolCount(toolGroup, newCount);
                /*CODIGOS DE ERROR:
                  W_LENGTH
                    (2)	data block length error
                    Size of IDBWRC structure(length) is wrong.
                  EW_NUMBER
                    (3)	data number error
                    The specification of tool group number (datano_s,datano_e) is wrong.
                  EW_DATA
                    (5)	data error(0i-F, 30i only)
                    Tool life counter(count) is out of range.
                  EW_NOOPT
                    (6)	no option
                    The tool life management function is necessary.*/
                Console.WriteLine(response);
            }
        }

        public static string WriteToolCount(short toolGroup, long newCount) {
            unsafe
            {
                IDBWRC writeStruct = null;
                writeStruct = CreateStructure(toolGroup, toolGroup, newCount);

                _ret = Focas1.cnc_wrcountr(_handle, (short)sizeof(IDBWRC), writeStruct);
            }
            if(_ret != Focas1.EW_OK)
            {
                return $"Error: An error had ocurred: {_ret}";
            }
            return "Success: All went good";
        }
    private static IDBWRC CreateStructure(short startGroup, short endGroup, long newCount)
        {
            IDBWRC newStructure = new()
            {
                datano_s = startGroup,
                dummy = 0,
                datano_e = endGroup,
                data = new
                {
                    dummy = 0,
                    count = newCount
                }
            };
            return newStructure;
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