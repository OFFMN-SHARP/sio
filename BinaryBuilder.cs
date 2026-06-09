using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class BinaryBuilder
    {
        public static void Builder(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"File not found: {filename}");
                return;
            }

            // 1. 读取文件
            Program.SIOFileGet(filename);

            // 2. 解析并生成 ASM
            Parser.MainParser();

            // 3. 写 ASM 文件
            string asmPath = Path.ChangeExtension(filename, ".asm");
            File.WriteAllText(asmPath, Program.ParseredAsm.ToString());
            Console.WriteLine($"Generated: {asmPath}");

            // 4. 调 NASM 编译
            string binPath = Path.ChangeExtension(filename, ".bin");
            Process proc = new Process();
            proc.StartInfo.FileName = "nasm";
            proc.StartInfo.Arguments = $"-f bin \"{asmPath}\" -o \"{binPath}\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                Console.WriteLine($"Compiled: {binPath}");
                Console.WriteLine($"Size: {new FileInfo(binPath).Length} bytes");
            }
            else
            {
                Console.WriteLine("NASM Error:");
                Console.WriteLine(error);
            }
        }
    }
}
