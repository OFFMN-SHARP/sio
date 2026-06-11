using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class TaskWindow
    {
        public static int Step = 0;
        public static void Draw(string title, string[] lines)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.SetCursorPosition(i, 0);
                Console.Write("─");
                Console.SetCursorPosition(i, Console.WindowHeight - 2);
                Console.Write("─");
            }
            for (int i = 0; i < Console.WindowHeight - 2; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("│");
                Console.SetCursorPosition(Console.WindowWidth - 1, i);
                Console.Write("│");
            }
            Console.SetCursorPosition(1, 0);
            Console.Write($"[{title}]");
            Step++;
            if (Step >= Console.WindowWidth)
            {
                Step = 0;
            }
            Console.SetCursorPosition(Step, Console.WindowHeight - 1);
            string[] displaylines = new string[Console.WindowHeight - 3];
            if (lines.Length > displaylines.Length)
            {
                int cut = lines.Length - displaylines.Length;
                string[] undisplaylines = lines.Skip(cut).ToArray();
                int index = 0;
                foreach (var line in undisplaylines)
                {
                    if (line.Length > Console.WindowWidth - 2)
                    {
                        var tline = line.Substring(0, Console.WindowWidth - 5) + "[M]";
                    }
                    displaylines[index] = line;
                    index++;
                }
            }
            else
            {
                int index = 0;
                foreach (var line in lines)
                {
                    if (line.Length > Console.WindowWidth - 2)
                    {
                        var tline = line.Substring(0, Console.WindowWidth - 5) + "[M]";
                    }
                    displaylines[index] = line;
                    index++;
                }
            }
            for(int i = 1; i < Console.WindowHeight - 3; i++)
            {
                Console.SetCursorPosition(1, i);
                if (!string.IsNullOrEmpty(displaylines[i - 1]))
                {
                    Console.Write(displaylines[i - 1]);
                }else Console.Write(new string(' ', Console.WindowWidth - 2));
            }
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("[" + new string('─', Console.WindowWidth - 2) + "]");
            Console.SetCursorPosition(Step+1, Console.WindowHeight - 1);
            Console.Write("═");
        }
    }
}
