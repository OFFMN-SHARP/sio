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
}
