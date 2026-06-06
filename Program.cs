using System.Text;

namespace Sio
{
    public static class Program
    {
        public static StringBuilder ParseredAsm = new StringBuilder();
        public static string[]? SIOFile;
        public static double Ver = 0.01;
        public static string ParserTip = "empty";
        public static string CurrentFilePath;
        public static bool EnableProtectedMode= false;
        public static void SIOFileGet(string filename) { SIOFile = File.ReadAllLines(filename); CurrentFilePath = Path.GetFullPath(filename); }
        public static void WelcomeScreen()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            for(int i = 1; i <= Console.WindowWidth-2; i++)
            {
                Console.SetCursorPosition(i, 0);
                Console.Write("─");
                Console.SetCursorPosition(i, Console.WindowHeight - 2);
                Console.Write("─");
            }
            for(int i = 1; i <= Console.WindowHeight - 3; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("│");
                Console.SetCursorPosition(Console.WindowWidth-1, i);
                Console.Write("│");
            }
            Console.SetCursorPosition(0, 0);
            Console.Write("┌");
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            Console.Write("└");
            Console.SetCursorPosition(Console.WindowWidth-1, Console.WindowHeight - 2);
            Console.Write("┘");
            Console.SetCursorPosition(Console.WindowWidth-1, 0);
            Console.Write("┐");
            Console.SetCursorPosition(1, 1);
            Console.Write("* Welcome to Sio IDLE[Interactive mode]");
            Console.SetCursorPosition(1, 4);
            Console.Write("Do you know that?:");
            int AllBuffer = (Console.WindowHeight - 8) * (Console.WindowWidth - 3);
            string ParseredTip = "[Empty]";
            if (ParserTip.Length > AllBuffer)
            {
                ParseredTip = ParserTip.Substring(0, AllBuffer - 3);
                ParseredTip += "[M]";
            }
            else ParseredTip = ParserTip;
            var TipList = ParseredTip.Chunk(Console.WindowWidth - 3);
            int count = 5;
            foreach ( var t in TipList )
            {
                Console.SetCursorPosition(1, count);
                Console.Write(t);
                count++;
            }
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Press any key to start.");
            Console.ResetColor();
            Console.ReadKey();
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("                       ");
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
        }
        public static void Main(string[] args)
        {
            WelcomeScreen();
            Commander.Interactive();
        }
    }
}
