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
        public static void AddLibFunction(string name, string label, string libname, int paramCount, string relvar, string[] paramSlots)
        {
            AST.AddLibFunction(name,label,libname,paramCount,relvar,paramSlots);
        }
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
            if (Program.SIOFile.Length <= 2) return;
            string? incLine = Program.SIOFile[2];
            if (string.IsNullOrEmpty(incLine) || !incLine.StartsWith("@inc=")) return;
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
                }
                foreach (var kvp in FileTextParsered)
                {
                    IncludedCode.AppendLine(kvp.Value);
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
                        case "console":
                            AddLibFunction("putsln", "putsln", "console", 1, "ret", new[] { "_sio_args_console_putsln_string" });
                            AddLibFunction("puts", "puts", "console", 1, "ret", new[] { "_sio_args_console_puts_string" });
                            AddLibFunction("curoff", "curoff", "console", 0, "ret", new string[0]);
                            AddLibFunction("curon", "curon", "console", 0, "ret", new string[0]);
                            AddLibFunction("gopos", "gopos", "console", 2, "ret", new[] { "_sio_args_console_gopos_row", "_sio_args_console_gopos_col" });
                            AddLibFunction("gotpos", "gotpos", "console", 0, "ret", new string[0]);
                            AddLibFunction("clscr", "clscr", "console", 0, "ret", new string[0]);
                            AddLibFunction("clln", "clln", "console", 0, "ret", new string[0]);
                            AddLibFunction("txtclor", "txtclor", "console", 1, "ret", new[] { "_sio_args_console_txtclor_color" });
                            AddLibFunction("bgclor", "bgclor", "console", 1, "ret", new[] { "_sio_args_console_bgclor_color" });
                            AddLibFunction("usrpwd", "usrpwd", "console", 1, "ret", new[] { "_sio_args_console_usrpwd_prompt" });
                            AddLibFunction("typewriter", "typewriter", "console", 2, "ret", new[] { "_sio_args_console_typewriter_string", "_sio_args_console_typewriter_speed" });
                            AddLibFunction("stpgecode", "stpgecode", "console", 1, "ret", new[] { "_sio_args_console_stpgecode_code" });
                            break;

                        case "diskio":
                            AddLibFunction("rstdsk", "rstdsk", "diskio", 1, "ret", new[] { "_sio_args_diskio_rstdsk_drive" });
                            AddLibFunction("wrsec", "wrsec", "diskio", 4, "ret", new[] { "_sio_args_diskio_wrsec_drive", "_sio_args_diskio_wrsec_sector", "_sio_args_diskio_wrsec_count", "_sio_args_diskio_wrsec_buffer" });
                            AddLibFunction("rdsec", "rdsec", "diskio", 4, "ret", new[] { "_sio_args_diskio_rdsec_drive", "_sio_args_diskio_rdsec_sector", "_sio_args_diskio_rdsec_count", "_sio_args_diskio_rdsec_buffer" });
                            break;

                        case "fat12":
                            AddLibFunction("addftxt", "addftxt", "fat12", 2, "ret", new[] { "_sio_args_fat12_addftxt_filename", "_sio_args_fat12_addftxt_text" });
                            AddLibFunction("ovrftxt", "ovrftxt", "fat12", 2, "ret", new[] { "_sio_args_fat12_ovrftxt_filename", "_sio_args_fat12_ovrftxt_text" });
                            AddLibFunction("wrftxt", "wrftxt", "fat12", 2, "ret", new[] { "_sio_args_fat12_wrftxt_filename", "_sio_args_fat12_wrftxt_text" });
                            AddLibFunction("fmt", "fmt", "fat12", 1, "ret", new[] { "_sio_args_fat12_fmt_drive" });
                            AddLibFunction("rdfile", "rdfile", "fat12", 2, "ret", new[] { "_sio_args_fat12_rdfile_filename", "_sio_args_fat12_rdfile_buffer" });
                            AddLibFunction("opfile", "opfile", "fat12", 1, "ret", new[] { "_sio_args_fat12_opfile_filename" });
                            AddLibFunction("mkfile", "mkfile", "fat12", 1, "ret", new[] { "_sio_args_fat12_mkfile_filename" });
                            AddLibFunction("mkflder", "mkflder", "fat12", 1, "ret", new[] { "_sio_args_fat12_mkflder_path" });
                            AddLibFunction("tree", "tree", "fat12", 1, "ret", new[] { "_sio_args_fat12_tree_path" });
                            AddLibFunction("fwatch", "fwatch", "fat12", 1, "ret", new[] { "_sio_args_fat12_fwatch_path" });
                            break;

                        case "serial":
                            AddLibFunction("serprtwr", "serprtwr", "serial", 2, "ret", new[] { "_sio_args_serial_serprtwr_port", "_sio_args_serial_serprtwr_data" });
                            AddLibFunction("serprtrd", "serprtrd", "serial", 1, "ret", new[] { "_sio_args_serial_serprtrd_port" });
                            break;

                        case "memgr":
                            AddLibFunction("gotmemap", "gotmemap", "memgr", 1, "ret", new[] { "_sio_args_memgr_gotmemap_buffer" });
                            AddLibFunction("mkpgecall", "mkpgecall", "memgr", 1, "ret", new[] { "_sio_args_memgr_mkpgecall_size" });
                            AddLibFunction("dspage", "dspage", "memgr", 1, "ret", new[] { "_sio_args_memgr_dspage_page" });
                            AddLibFunction("meset", "meset", "memgr", 3, "ret", new[] { "_sio_args_memgr_meset_addr", "_sio_args_memgr_meset_value", "_sio_args_memgr_meset_len" });
                            AddLibFunction("mwatch", "mwatch", "memgr", 2, "ret", new[] { "_sio_args_memgr_mwatch_addr", "_sio_args_memgr_mwatch_len" });
                            break;

                        case "timekit":
                            AddLibFunction("slpms", "slpms", "timekit", 1, "ret", new[] { "_sio_args_timekit_slpms_ms" });
                            AddLibFunction("slphur", "slphur", "timekit", 1, "ret", new[] { "_sio_args_timekit_slphur_hours" });
                            AddLibFunction("goticks", "goticks", "timekit", 0, "ret", new string[0]);
                            AddLibFunction("twatch", "twatch", "timekit", 1, "ret", new[] { "_sio_args_timekit_twatch_label" });
                            break;

                        case "kb-bus":
                            AddLibFunction("rdky", "rdky", "kb-bus", 0, "ret", new string[0]);
                            AddLibFunction("chkky", "chkky", "kb-bus", 0, "ret", new string[0]);
                            AddLibFunction("chgcode", "chgcode", "kb-bus", 1, "ret", new[] { "_sio_args_kbbus_chgcode_layout" });
                            break;

                        case "powermgr":
                            AddLibFunction("reset", "reset", "powermgr", 0, "ret", new string[0]);
                            AddLibFunction("halt", "halt", "powermgr", 0, "ret", new string[0]);
                            AddLibFunction("stopmchine", "stopmchine", "powermgr", 0, "ret", new string[0]);
                            break;

                        case "tool":
                            AddLibFunction("mecpy", "mecpy", "tool", 3, "ret", new[] { "_sio_args_tool_mecpy_src", "_sio_args_tool_mecpy_dst", "_sio_args_tool_mecpy_len" });
                            AddLibFunction("strlen", "strlen", "tool", 1, "ret", new[] { "_sio_args_tool_strlen_str" });
                            AddLibFunction("strcmp", "strcmp", "tool", 2, "ret", new[] { "_sio_args_tool_strcmp_str1", "_sio_args_tool_strcmp_str2" });
                            AddLibFunction("strspilt", "strspilt", "tool", 3, "ret", new[] { "_sio_args_tool_strspilt_str", "_sio_args_tool_strspilt_delim", "_sio_args_tool_strspilt_buf" });
                            AddLibFunction("strmatch", "strmatch", "tool", 2, "ret", new[] { "_sio_args_tool_strmatch_str", "_sio_args_tool_strmatch_pattern" });
                            AddLibFunction("strreplace", "strreplace", "tool", 3, "ret", new[] { "_sio_args_tool_strreplace_str", "_sio_args_tool_strreplace_old", "_sio_args_tool_strreplace_newstr" });
                            break;

                        case "qasm":
                            AddLibFunction("wrasm", "wrasm", "qasm", 1, "ret", new[] { "_sio_args_qasm_wrasm_code" });
                            AddLibFunction("ovrdeasmln", "ovrdeasmln", "qasm", 2, "ret", new[] { "_sio_args_qasm_ovrdeasmln_line", "_sio_args_qasm_ovrdeasmln_code" });
                            AddLibFunction("adasm", "adasm", "qasm", 2, "ret", new[] { "_sio_args_qasm_adasm_line", "_sio_args_qasm_adasm_code" });
                            AddLibFunction("rmasmln", "rmasmln", "qasm", 1, "ret", new[] { "_sio_args_qasm_rmasmln_line" });
                            break;

                        case "pe&elf":
                            AddLibFunction("apploadpe", "apploadpe", "peelf", 1, "ret", new[] { "_sio_args_peelf_apploadpe_path" });
                            AddLibFunction("apploadelf", "apploadelf", "peelf", 1, "ret", new[] { "_sio_args_peelf_apploadelf_path" });
                            AddLibFunction("sysloadelf", "sysloadelf", "peelf", 1, "ret", new[] { "_sio_args_peelf_sysloadelf_path" });
                            AddLibFunction("sysloadpe", "sysloadpe", "peelf", 1, "ret", new[] { "_sio_args_peelf_sysloadpe_path" });
                            AddLibFunction("chkpe", "chkpe", "peelf", 1, "ret", new[] { "_sio_args_peelf_chkpe_path" });
                            AddLibFunction("chkelf", "chkelf", "peelf", 1, "ret", new[] { "_sio_args_peelf_chkelf_path" });
                            AddLibFunction("getentp", "getentp", "peelf", 1, "ret", new[] { "_sio_args_peelf_getentp_path" });
                            break;

                        case "advpci&vga":
                            AddLibFunction("vgamode", "vgamode", "advpcivga", 1, "ret", new[] { "_sio_args_advpcivga_vgamode_mode" });
                            AddLibFunction("vgadrwpx", "vgadrwpx", "advpcivga", 3, "ret", new[] { "_sio_args_advpcivga_vgadrwpx_x", "_sio_args_advpcivga_vgadrwpx_y", "_sio_args_advpcivga_vgadrwpx_color" });
                            AddLibFunction("vgadrwln", "vgadrwln", "advpcivga", 5, "ret", new[] { "_sio_args_advpcivga_vgadrwln_x1", "_sio_args_advpcivga_vgadrwln_y1", "_sio_args_advpcivga_vgadrwln_x2", "_sio_args_advpcivga_vgadrwln_y2", "_sio_args_advpcivga_vgadrwln_color" });
                            AddLibFunction("vgadrwshp", "vgadrwshp", "advpcivga", 6, "ret", new[] { "_sio_args_advpcivga_vgadrwshp_type", "_sio_args_advpcivga_vgadrwshp_x", "_sio_args_advpcivga_vgadrwshp_y", "_sio_args_advpcivga_vgadrwshp_w", "_sio_args_advpcivga_vgadrwshp_h", "_sio_args_advpcivga_vgadrwshp_color" });
                            AddLibFunction("pcicfgrd", "pcicfgrd", "advpcivga", 2, "ret", new[] { "_sio_args_advpcivga_pcicfgrd_bus", "_sio_args_advpcivga_pcicfgrd_device" });
                            AddLibFunction("pcifind", "pcifind", "advpcivga", 2, "ret", new[] { "_sio_args_advpcivga_pcifind_vendor", "_sio_args_advpcivga_pcifind_device" });
                            break;
                    }
                }
                // 所有 AddLibFunction 注册完之后，插入库的 ASM
                Program.ParseredAsm.AppendLine("; ===== Library Variables =====");
                foreach (string lib in LibGeter)
                {
                    switch (lib)
                    {
                        case "console": Program.ParseredAsm.Append(LibASM.ConsoleVars); break;
                        case "diskio": Program.ParseredAsm.Append(LibASM.DiskioVars); break;
                        case "fat12": Program.ParseredAsm.Append(LibASM.Fat12Vars); break;
                        case "serial": Program.ParseredAsm.Append(LibASM.SerialVars); break;
                        case "memgr": Program.ParseredAsm.Append(LibASM.MemgrVars); break;
                        case "timekit": Program.ParseredAsm.Append(LibASM.TimekitVars); break;
                        case "kb-bus": Program.ParseredAsm.Append(LibASM.KbbusVars); break;
                        case "powermgr": Program.ParseredAsm.Append(LibASM.PowermgrVars); break;
                        case "tool": Program.ParseredAsm.Append(LibASM.ToolVars); break;
                        case "qasm": Program.ParseredAsm.Append(LibASM.QasmVars); break;
                        case "pe&elf": Program.ParseredAsm.Append(LibASM.PeelfVars); break;
                        case "advpci&vga": Program.ParseredAsm.Append(LibASM.AdvpcivgaVars); break;
                    }
                }

                Program.ParseredAsm.AppendLine("; ===== Library Implementations =====");
                foreach (string lib in LibGeter)
                {
                    switch (lib)
                    {
                        case "console": Program.ParseredAsm.Append(LibASM.ConsoleLib); break;
                        case "diskio": Program.ParseredAsm.Append(LibASM.DiskioLib); break;
                        case "fat12": Program.ParseredAsm.Append(LibASM.Fat12Lib); break;
                        case "serial": Program.ParseredAsm.Append(LibASM.SerialLib); break;
                        case "memgr": Program.ParseredAsm.Append(LibASM.MemgrLib); break;
                        case "timekit": Program.ParseredAsm.Append(LibASM.TimekitLib); break;
                        case "kb-bus": Program.ParseredAsm.Append(LibASM.KbbusLib); break;
                        case "powermgr": Program.ParseredAsm.Append(LibASM.PowermgrLib); break;
                        case "tool": Program.ParseredAsm.Append(LibASM.ToolLib); break;
                        case "qasm": Program.ParseredAsm.Append(LibASM.QasmLib); break;
                        case "pe&elf": Program.ParseredAsm.Append(LibASM.PeelfLib); break;
                        case "advpci&vga": Program.ParseredAsm.Append(LibASM.AdvpcivgaLib); break;
                    }
                }
            }
        }
        public static StringBuilder IncludedCode = new StringBuilder();
        public static void MainParser()
        {
            PctMode();         // 1. @pctmode
            Include();         // 2. @inc= 收集被包含文件
            Head();            // 3. 引导头（先生成）
            Import();          // 4. @lib= 库代码（追加在引导头后面）
            AST.Paeser();      // 5. 用户代码（追加在库代码后面）
        }
    }
}
