using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class CommanderFunctions
    {
        public static Dictionary<string, Action<string[]>> CmdDic = new()
{
    { "build", args => {
        string file = args[1];
        // 检查有没有 -tagsrch
        if (args.Length > 2 && args[2].StartsWith("-tagsrch "))
        {
            string tagData = args[2].Substring(9);
            string[] files = ParseTagSearch(tagData);
            foreach (string f in files)
                BinaryBuilder.Builder(f.Trim('"'));
        }
        else
        {
            BinaryBuilder.Builder(file);
        }
    }},
    { "exit", _ => Environment.Exit(0) },
    { "help", _ => {
        Console.WriteLine("Commands:");
        Console.WriteLine("  build <file>     Compile .sio → .bin");
        Console.WriteLine("  build [ex:sio]   Compile all .sio files");
        Console.WriteLine("  exit             Exit");
        Console.WriteLine("  help             Show this");
    }},
};

        static string[] ParseTagSearch(string input)
        {
            List<string> files = new List<string>();
            int i = 0;
            while (i < input.Length)
            {
                if (input[i] == '"')
                {
                    int end = input.IndexOf('"', i + 1);
                    if (end < 0) break;
                    files.Add(input.Substring(i + 1, end - i - 1));
                    i = end + 1;
                }
                else i++;
            }
            return files.ToArray();
        }

    }
    public static class ErrorReporter
    {
        // 核心报错函数
        public static void Report(ErrorInfo info)
        {
            // 计算框线宽度（基于最长的内容行）
            int maxLen = Math.Max(
                $"Code: {info.Code}".Length,
                Math.Max(
                    $" *Line: {info.Line}".Length,
                    Math.Max(
                        $" *Reason: {info.Reason}".Length,
                        $" *Document location: {info.Location}".Length
                    )
                )
            );

            int boxWidth = maxLen + 2;  // 左右留白

            // 第一行：Code
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Code: ");
            Console.ResetColor();
            Console.WriteLine(info.Code);

            // 框线顶部
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("├");
            Console.Write(new string('═', boxWidth));
            Console.WriteLine("┐");
            Console.ResetColor();

            // Line
            Console.Write("│ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("*Line: ");
            Console.ResetColor();
            Console.Write(info.Line);
            Console.Write(new string(' ', boxWidth - $"*Line: {info.Line}".Length));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" │");
            Console.ResetColor();

            // Reason
            Console.Write("│ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("*Reason: ");
            Console.ResetColor();
            Console.Write(info.Reason);
            Console.Write(new string(' ', boxWidth - $"*Reason: {info.Reason}".Length));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" │");
            Console.ResetColor();

            // Document location
            Console.Write("│ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("*Document location: ");
            Console.ResetColor();
            Console.Write(info.Location);
            Console.Write(new string(' ', boxWidth - $"*Document location: {info.Location}".Length));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" │");
            Console.ResetColor();

            // 框线底部
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("└");
            Console.Write(new string('═', boxWidth));
            Console.WriteLine("┘");
            Console.ResetColor();
        }

        // 错误信息数据类
        public class ErrorInfo
        {
            public string Code { get; set; }
            public int Line { get; set; }
            public string Reason { get; set; }
            public string Location { get; set; }

            public static ErrorInfo FromEx(string code, Exception ex, string filePath, int line)
            {
                return new ErrorInfo
                {
                    Code = code,
                    Line = line,
                    Reason = ex.Message,
                    Location = filePath
                };
            }
        }

        // 预设错误码（可扩展）
        public static class Codes
        {
            public const string FileNotFound = "SIO-E0001";
            public const string SyntaxError = "SIO-E0002";
            public const string UnknownFunction = "SIO-E0003";
            public const string NasmFailed = "SIO-E0004";
            public const string ParamError = "SIO-E0005";
        }
    }
}
