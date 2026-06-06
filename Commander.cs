using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class Commander
    {
        public static void Interactive()
        {
            while (true)
            {
                ConsoleColor originalBg = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write("Sio");
                Console.BackgroundColor=originalBg;
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor=ConsoleColor.Blue;
                Console.Write($"[Ver{Program.Ver}|{Environment.UserName}@{Environment.MachineName}]");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.White;
                Console.WriteLine($"<{Directory.GetCurrentDirectory}>");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("└──>");
                Console.ResetColor();
                string Input = Console.ReadLine() ?? "@String_NULL";
                Parser(Input);
                Console.WriteLine();
                Console.WriteLine();
            }
        }
        public static void Parser(string command)
        {

        }
    }
}
