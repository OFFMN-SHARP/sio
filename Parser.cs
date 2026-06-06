using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Sio
{
    public static class Parser
    {
        public static List<string> AtLibs = new List<string>();
        public static void PctMode()
        {
            string pctget = "@pctmode=false";

            if (Program.SIOFile != null && Program.SIOFile.Length > 3)
            {
                string? rawLine = Program.SIOFile[3];
                if (rawLine != null) pctget = rawLine;
            }
            if (!pctget.StartsWith("@pctmode="))
            {
                Program.EnableProtectedMode = false;
                return;
            }
            // 截取 @pctmode= 后面的部分，去空格，去注释（按 ; 或 # 分割）
            string rawValue = pctget.Substring(9).Trim();
            int commentIndex = rawValue.IndexOfAny(new[] { ';', '#' });
            if (commentIndex >= 0) rawValue = rawValue.Substring(0, commentIndex).Trim();

            string value = rawValue.ToLower();
            Program.EnableProtectedMode = value == "true" || value == "1";
        }
        public static void Include()
        {
            List<string> paths = new List<string>();
            string incget = Program.SIOFile[2] ?? "@inc=undef";
            if(incget == "@inc=undef")return;
            else
            {
                incget = incget.Substring(5);
                string path =incget.Trim();
                if (path.EndsWith("?"))
                {
                    int count = path.Reverse().TakeWhile(c => c == '?').Count();
                    string dir = path.Substring(0, path.Length-count);
                    if (!Directory.Exists(dir)) throw new Exception("Error Path");
                    var pathsls = Directory.GetFiles(dir).ToList();
                    foreach(var pth in pathsls)
                    {
                        if (Path.GetFileNameWithoutExtension(pth).Length <= count && Path.GetExtension(pth) == ".sio")
                        {
                            paths.Add(pth);
                        }
                    }
                }
                else if (path.EndsWith("*"))
                {
                    int cut = path.Length - 1;
                    if (!Directory.Exists(path.Substring(0,cut))) throw new Exception("Error Path");
                    List<string> paths2 = new List<string>();
                    paths2 = Directory.GetFiles(path.Substring(0,cut)).ToList();
                    if(paths2.Count > 0)
                    {
                        foreach(string path2 in paths2)
                        {
                            if (Path.GetExtension(path2) == ".sio")
                            {
                                paths.Add(path2);
                            }
                        }
                    }
                }
                else if (path.EndsWith(".sio"))
                {
                    if (!File.Exists(path)) throw new Exception("Error Path");
                    path = Path.GetFullPath(path);
                    paths.Add(path);
                }
                else throw new Exception("Not enough path");
                string mainFile = Path.GetFullPath(Program.CurrentFilePath);
                paths = paths.Where(p => Path.GetFullPath(p) != mainFile).ToList();
                string libLine = Program.SIOFile[1];
                if (paths.Count <=0)return;
                Dictionary<string,string>FileText=new Dictionary<string,string>();
                Dictionary<string, string> FileTextParsered = new Dictionary<string, string>();
                List<string> AtLibs = new List<string>();
                foreach(var file in paths)
                {
                    var all = File.ReadAllText(file);
                    FileText.Add(file, all);
                }
                foreach (var file in paths)
                {
                    var alltxt=string.Empty;
                    FileText.TryGetValue(file,out alltxt);
                    var alllns = alltxt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                    List<string> RmAts = new List<string>();
                    foreach (var line in alllns)
                    {
                        if (line.StartsWith("@lib="))
                        {
                            AtLibs.Add(line.Substring(5).Trim());
                        }else if (line.StartsWith("@"))
                        {
                            RmAts.Add(line);
                        }
                    }
                    foreach(var line in RmAts)
                    {
                        alllns.Remove(line);
                    }
                    var allpsred = string.Join(Environment.NewLine,alllns);
                    FileTextParsered.Add(file, allpsred);
                    foreach (var kvp in FileTextParsered)
                    {
                        IncludedCode.AppendLine("; [Included: " + kvp.Key + "]");
                        IncludedCode.AppendLine(kvp.Value);
                        IncludedCode.AppendLine("; [/Included]");
                    }
                }
            }
        }
        public static void Head()
        {
            string headget = Program.SIOFile[0] ?? "@ppc=undef";
            StringBuilder BootHeadle = new StringBuilder();
            switch (headget)
            {
                case "@ppc=efi":
                    BootHeadle.AppendLine(@"
[bits 64]
global _start

_start:
    sub rsp, 8
    call MainBooter
    add rsp, 8
    ret

");
                    break;
                case "@ppc=bios":
                    BootHeadle.AppendLine(@"
; ===== 第一段：MBR 加载器 =====
[bits 16]
[org 0x7C00]

start:
    xor ax, ax
    mov ds, ax
    mov es, ax
    mov ss, ax
    mov sp, 0x7C00

    ; 加载第二段
    mov ah, 0x02
    mov al, 8
    mov ch, 0
    mov cl, 2
    mov dh, 0
    mov dl, 0x80
    mov bx, 0x7E00
    int 0x13
    jc disk_error
    jmp 0x7E00

disk_error:
    mov ah, 0x0E
    mov al, 'E'
    int 0x10
    cli
    hlt

; MBR 填充到 510 字节
times 510-($-$$) db 0
dw 0xAA55

; ===== 第二段：运行时库 + MainBooter =====
; 注意：这里的 org 告诉 NASM 这段代码将被加载到 0x7E00
[org 0x7E00]
");
                    // 有保护模式：先切换，再进 MainBooter
                    if (Program.EnableProtectedMode)
                    {
                        BootHeadle.AppendLine(@"
; ===== 切换到保护模式 =====
    cli
    lgdt [gdt_descriptor]
    mov eax, cr0
    or eax, 0x1
    mov cr0, eax
    jmp 0x08:protected_mode_entry

[bits 32]
protected_mode_entry:
    mov ax, 0x10
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    mov esp, 0x7C00
    jmp MainBooter

; ===== GDT =====
gdt_start:
    dq 0x0                      ; 空描述符
    dq 0x00CF9A000000FFFF       ; 代码段：基址0，界限4G，执行/读
    dq 0x00CF92000000FFFF       ; 数据段：基址0，界限4G，读/写
gdt_end:

gdt_descriptor:
    dw gdt_end - gdt_start - 1
    dd gdt_start
");
                    }
                    else
                    {
                        // 无保护模式：直接进 MainBooter（16 位实模式）
                        BootHeadle.AppendLine(@"    jmp MainBooter
");
                    }
                    break;
                case "@ppc=mboot":
                    BootHeadle.AppendLine(@"
[bits 32]
section .multiboot_header
align 4
multiboot_header:
    dd 0x1BADB002
    dd 0x00000003
    dd -(0x1BADB002 + 0x00000003)

section .text
global _start
_start:
    mov [mboot_info_ptr], ebx
    mov [mboot_magic], eax
    call MainBooter
    cli
    hlt

section .bss
mboot_info_ptr: resd 1
mboot_magic: resd 1

");
                    break;
                default:
                    Console.WriteLine("Can not found boot headle.");
                    Environment.Exit(1);
                    break;
            }
            Program.ParseredAsm = BootHeadle;
        }

        public static void Import()
        {
            if (String.IsNullOrEmpty(Program.SIOFile[1]) || Program.SIOFile[1].Length<6)
            {
                throw new Exception("Can not found any lib import");
            }else
            {//@lib=
                string libLine = Program.SIOFile[1];
                string libList = libLine.Substring(5);  // 去掉 "@lib="
                string[] LibSender = libList.Split(',');
                List<string> LibGeter = LibSender.Distinct()
                    .Select(lib=>lib.Trim())
                    .Where(lib => !string.IsNullOrEmpty(lib))
                    .ToList();
                if(LibGeter.Count == 1 && LibGeter[0] == "all")
                {
                    LibGeter = new List<string>()
                    {
                        "fat12",
                        "console",
                        "tool",
                        "qasm",
                        "kb-bus",
                        "powermgr",
                        "diskio",
                        "serial",
                        "memgr",
                        "timekit",
                        "advpci&vga",
                        "pe&elf"
                    };
                }
                if (LibGeter.Contains("pe&elf"))
                {
                    if (!LibGeter.Contains("memgr")) LibGeter.Add("memgr");
                    if (!LibGeter.Contains("timekit")) LibGeter.Add("timekit");
                    if (!LibGeter.Contains("console")) LibGeter.Add("console");
                }
                if (LibGeter.Contains("advpci&vga") && !LibGeter.Contains("powermgr"))
                    LibGeter.Add("powermgr");
                if (LibGeter.Contains("console"))
                {
                    if (!LibGeter.Contains("advpci&vga")) LibGeter.Add("advpci&vga");
                    if (!LibGeter.Contains("kb-bus")) LibGeter.Add("kb-bus");
                }
                if (LibGeter.Contains("fat12") && !LibGeter.Contains("diskio"))
                    LibGeter.Add("diskio");
                if (LibGeter.Contains("tool") && !LibGeter.Contains("memgr"))
                    LibGeter.Add("memgr");
                if (LibGeter.Contains("timekit") && !LibGeter.Contains("powermgr"))
                    LibGeter.Add("powermgr");
                if (LibGeter.Contains("qasm") && !LibGeter.Contains("tool"))
                    LibGeter.Add("tool");
                if (LibGeter.Contains("serial") && !LibGeter.Contains("console"))
                    LibGeter.Add("console");
                if (LibGeter.Contains("diskio"))
                {
                    if (!LibGeter.Contains("memgr")) LibGeter.Add("memgr");
                    if (!LibGeter.Contains("tool")) LibGeter.Add("tool");
                }
                if (LibGeter.Contains("kb-bus") && !LibGeter.Contains("serial"))
                    LibGeter.Add("serial");
                if (LibGeter.Contains("powermgr") && !LibGeter.Contains("memgr"))
                    LibGeter.Add("memgr");
                foreach (string atlib in Parser.AtLibs)
                {
                    string[] libs = atlib.Split(',');
                    foreach (string lib in libs)
                    {
                        string trimmed = lib.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !LibGeter.Contains(trimmed))
                        {
                            LibGeter.Add(trimmed);
                        }
                    }
                }
                if (LibGeter.Contains("all"))
                {
                    LibGeter = new List<string>()
                    {
                        "fat12",
                        "console",
                        "tool",
                        "qasm",
                        "kb-bus",
                        "powermgr",
                        "diskio",
                        "serial",
                        "memgr",
                        "timekit",
                        "advpci&vga",
                        "pe&elf"
                    };
                }
                foreach (string lib in LibGeter)
                {
                    switch (lib)
                    {
                        case "fat12":
                            break;
                        case "console":
                            break;
                        case "tool":
                            break;
                        case "qasm":
                            break;
                        case "kb-bus":
                            break;
                        case "powermgr":
                            break;
                        case "diskio"://还是得改，dk太像diverkit了
                            break;
                        case "serial":
                            break;
                        case "memgr":
                            break;
                        case "timekit":
                            break;
                        case "advpci&vga":
                            break;
                        case "pe&elf":
                            break;
                    }
                }
            }
        }
        public static void MainBooter()
        {
            StringBuilder Booter = new StringBuilder();
            Booter.AppendLine("MainBooter:");
            Program.ParseredAsm.AppendLine(Booter.ToString());
        }
        public static StringBuilder IncludedCode = new StringBuilder();
        public static void MainParser()
        {
            PctMode();
            Include();
            Import();
            Head();
            MainBooter();
        }
    }
}
