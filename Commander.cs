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
            if (string.IsNullOrWhiteSpace(command)) return;
            // 1. 解析参数，处理各种括号
            string[] rawArgs = ParseArgs(command);
            if (rawArgs.Length == 0) return;
            // 2. 预处理：展开通配符
            string[] expandedArgs = ExpandWildcards(rawArgs);

            // 3. 查字典执行
            string cmd = expandedArgs[0].ToLower();
            if (CommanderFunctions.CmdDic.TryGetValue(cmd, out var action))
            {
                action(expandedArgs);
            }
            else
            {
                Console.WriteLine($"Unknown command: {cmd}");
            }
        }

        public static string[] ParseArgs(string input)
        {
            List<string> args = new List<string>();
            StringBuilder current = new StringBuilder();
            char? quoteChar = null;  // 只在引号内有效，括号不记录

            foreach (char c in input)
            {
                if (quoteChar != null)
                {
                    if (c == quoteChar)
                    {
                        quoteChar = null;  // 引号结束，内容已经加了
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        quoteChar = c;  // 引号开始，引号本身不保留
                    }
                    else if (c == ' ' || c == '\t')
                    {
                        if (current.Length > 0)
                        {
                            args.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    else
                    {
                        // 包括 ( ) [ ] 都原样保留
                        current.Append(c);
                    }
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }
        public static string[] ExpandWildcards(string[] args)
        {
            List<string> result = new List<string>();
            bool hasWildcard = false;
            List<string> allMatchedFiles = new List<string>();

            foreach (string arg in args)
            {
                // 检测是否包含通配符
                if (arg.Contains('[') || arg.Contains('('))
                {
                    hasWildcard = true;
                    string[] files = FileSearcher.Search(arg);
                    allMatchedFiles.AddRange(files);
                }
                else
                {
                    result.Add(arg);
                }
            }

            if (hasWildcard && allMatchedFiles.Count > 0)
            {
                // 去重
                allMatchedFiles = allMatchedFiles.Distinct().ToList();

                // 格式化为 -tagsrch "file1","file2","file3"
                string tagSearch = "-tagsrch " + string.Join(",",
                    allMatchedFiles.Select(f => $"\"{f}\""));
                result.Add(tagSearch);
            }

            return result.ToArray();
        }
    }
}
