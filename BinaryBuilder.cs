using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sio.ErrorReporter;

namespace Sio
{
    public static class BinaryBuilder
    {
        public static void Builder(string filename)
        {
            if (!CheckNasmAvailable())
            {
                InstallNasm();
            }

            if (!File.Exists(filename))
            {
                ErrorReporter.Report(new ErrorInfo
                {
                    Code = ErrorReporter.Codes.FileNotFound,
                    Line = -1,
                    Reason = $"文件不存在: {filename}",
                    Location = filename
                });
                return;
            }

            // 1. 读取文件
            Program.SIOFileGet(filename);

            // 2. 解析并生成 ASM
            Parser.MainParser();

            // 检查 ParseredAsm 是否为空（解析出错时可能为空）
            if (Program.ParseredAsm.Length == 0)
            {
                ErrorReporter.Report(new ErrorInfo
                {
                    Code = ErrorReporter.Codes.SyntaxError,
                    Line = -1,
                    Reason = "解析失败，未生成 ASM 代码",
                    Location = filename
                });
                return;
            }

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
                long targetSize = 1024 * 1024; // 1 MB
                var fi = new FileInfo(binPath);
                if (fi.Length < targetSize)
                {
                    using (var fs = fi.OpenWrite())
                    {
                        fs.SetLength(targetSize);
                    }
                    Console.WriteLine($"Padded to: {targetSize} bytes");
                }
            }
            else
            {
                ErrorReporter.Report(new ErrorInfo
                {
                    Code = ErrorReporter.Codes.NasmFailed,
                    Line = -1,
                    Reason = $"NASM 编译失败: {error}",
                    Location = asmPath
                });
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
