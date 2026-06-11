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
            if (!CheckNasmAvailable())
            {
                Console.WriteLine("NASM not found. Installing...");
                InstallNasm();
            }

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

        public static bool CheckNasmAvailable()
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "nasm",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                proc.WaitForExit(1000);
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }
        public static void InstallNasm()
        {
            if (OperatingSystem.IsWindows())
            {
                //Process.Start("winget", "install nasm").WaitForExit();
                using(Process proc = new Process())
                {
                    proc.StartInfo.FileName = "powershell";
                    proc.StartInfo.Arguments = "-Command \"winget install nasm\"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.Start();
                    proc.BeginErrorReadLine();
                    proc.BeginOutputReadLine();
                    List<string> Lines = new List<string>();
                    proc.OutputDataReceived += (sender, e) =>{
                        Lines.Add(e.Data??"");
                        TaskWindow.Draw("Installing NASM", Lines.ToArray());
                    };
                    proc.ErrorDataReceived += (sender, e) =>{
                        Lines.Add(e.Data??"");
                        TaskWindow.Draw("Installing NASM", Lines.ToArray());
                    };
                    proc.WaitForExitAsync();
                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine("Failed to install NASM:");
                    }
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                // 检测是 apt 系还是 pacman 系
                if (File.Exists("/usr/bin/apt"))
                    //Process.Start("sudo", "apt install -y nasm").WaitForExit();
                    using(Process proc=new Process())
                    {
                        proc.StartInfo.FileName = "sudo";
                        proc.StartInfo.Arguments = "apt install -y nasm";
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.Start();
                        proc.BeginErrorReadLine();
                        proc.BeginOutputReadLine();
                        List<string> Lines = new List<string>();
                        proc.OutputDataReceived += (sender, e) =>{
                            Lines.Add(e.Data??"");
                            TaskWindow.Draw("Installing NASM", Lines.ToArray());
                        };
                        proc.ErrorDataReceived += (sender, e) =>{
                            Lines.Add(e.Data??"");
                            TaskWindow.Draw("Installing NASM", Lines.ToArray());
                        };
                        proc.WaitForExitAsync();
                        if (proc.ExitCode != 0)
                        {
                            Console.WriteLine("Failed to install NASM:");
                        }
                    }
                else if (File.Exists("/usr/bin/pacman"))
                    //Process.Start("sudo", "pacman -S nasm").WaitForExit();
                    using(Process proc=new Process())
                    {
                        proc.StartInfo.FileName = "sudo";
                        proc.StartInfo.Arguments = "pacman -S nasm";
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.Start();
                        proc.BeginErrorReadLine();
                        proc.BeginOutputReadLine();
                        List<string> Lines = new List<string>();
                        proc.OutputDataReceived += (sender, e) =>{
                            Lines.Add(e.Data??"");
                            TaskWindow.Draw("Installing NASM", Lines.ToArray());
                        };
                        proc.ErrorDataReceived += (sender, e) =>{
                            Lines.Add(e.Data??"");
                            TaskWindow.Draw("Installing NASM", Lines.ToArray());
                        };
                        proc.WaitForExitAsync();
                        if (proc.ExitCode != 0)
                        {
                            Console.WriteLine("Failed to install NASM:");
                        }
                    }
            }
        }
    }
}
