using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class LibASM
    {
        // ============================================================
        // 1. Console
        // ============================================================
        public static string ConsoleVars = @"
; ===== Console Vars =====
_sio_args_console_putsln_string:         dw 0
_sio_args_console_puts_string:           dw 0
_sio_args_console_gopos_row:             db 0
_sio_args_console_gopos_col:             db 0
_sio_args_console_txtclor_color:         db 0
_sio_args_console_bgclor_color:          db 0
_sio_args_console_usrpwd_prompt:         dw 0
_sio_args_console_typewriter_string:     dw 0
_sio_args_console_typewriter_speed:      dw 0
_sio_args_console_stpgecode_code:        db 0
_sio_librel_console_ret:                 dw 0
";
        public static string ConsoleLib = @"
; ===== Console Lib =====
; INT 10h 文本模式操作
_sio_libfn_console_putsln:
    push si
    mov si, [_sio_args_console_putsln_string]
.loop_pln:
    lodsb
    or al, al
    jz .done_pln
    mov ah, 0x0E
    int 0x10
    jmp .loop_pln
.done_pln:
    mov al, 0x0D
    mov ah, 0x0E
    int 0x10
    mov al, 0x0A
    mov ah, 0x0E
    int 0x10
    pop si
    ret
_sio_libfn_console_puts:
    push si
    mov si, [_sio_args_console_puts_string]
.loop_ps:
    lodsb
    or al, al
    jz .done_ps
    mov ah, 0x0E
    int 0x10
    jmp .loop_ps
.done_ps:
    pop si
    ret
_sio_libfn_console_curoff:
    push ax
    push cx
    mov ah, 0x01
    mov cx, 0x2607
    int 0x10
    pop cx
    pop ax
    ret
_sio_libfn_console_curon:
    push ax
    push cx
    mov ah, 0x01
    mov cx, 0x0507
    int 0x10
    pop cx
    pop ax
    ret
_sio_libfn_console_clscr:
    push ax
    mov ah, 0x00
    mov al, 0x03
    int 0x10
    pop ax
    ret
_sio_libfn_console_gopos:
    push ax
    push bx
    push dx
    mov ah, 0x02
    mov bh, 0
    mov dh, [_sio_args_console_gopos_row]
    mov dl, [_sio_args_console_gopos_col]
    int 0x10
    pop dx
    pop bx
    pop ax
    ret
_sio_libfn_console_txtclor:
    push ax
    push bx
    mov bl, [_sio_args_console_txtclor_color]
    mov ah, 0x0B
    mov bh, 0
    int 0x10
    pop bx
    pop ax
    ret
_sio_libfn_console_bgclor:
    push ax
    push bx
    mov bl, [_sio_args_console_bgclor_color]
    mov ah, 0x0B
    mov bh, 0
    int 0x10
    pop bx
    pop ax
    ret
_sio_libfn_console_gotpos:
    push ax
    push bx
    push cx
    push dx
    mov ah, 0x03
    mov bh, 0
    int 0x10
    mov [_sio_librel_console_ret], dx  ; dh=row, dl=col
    pop dx
    pop cx
    pop bx
    pop ax
    ret

_sio_libfn_console_usrpwd:
    ; 简化为始终等待输入，实际需要键盘库配合
    ret

_sio_libfn_console_typewriter:
    push si
    mov si, [_sio_args_console_typewriter_string]
.loop_tw:
    lodsb
    or al, al
    jz .done_tw
    mov ah, 0x0E
    int 0x10
    ; 延时由调用方控制
    jmp .loop_tw
.done_tw:
    pop si
    ret

_sio_libfn_console_stpgecode:
    ret

_sio_libfn_console_clln:
    push ax
    push bx
    push cx
    push dx
    mov ah, 0x09
    mov al, ' '
    mov bh, 0
    mov bl, 0x07
    mov cx, 80
    int 0x10
    pop dx
    pop cx
    pop bx
    pop ax
    ret

";

        // ============================================================
        // 2. DiskIO
        // ============================================================
        public static string DiskioVars = @"
; ===== DiskIO Vars =====
_sio_args_diskio_rstdsk_drive:           db 0
_sio_args_diskio_wrsec_drive:            db 0
_sio_args_diskio_wrsec_sector:           dw 0
_sio_args_diskio_wrsec_count:            db 0
_sio_args_diskio_wrsec_buffer:           dw 0
_sio_args_diskio_rdsec_drive:            db 0
_sio_args_diskio_rdsec_sector:           dw 0
_sio_args_diskio_rdsec_count:            db 0
_sio_args_diskio_rdsec_buffer:           dw 0
_sio_librel_diskio_ret:                  dw 0
";
        public static string DiskioLib = @"
; ===== DiskIO Lib =====
; 待实现：INT 13h 磁盘操作

_sio_libfn_diskio_rstdsk:
    ; TODO
    ret

_sio_libfn_diskio_wrsec:
    ; TODO
    ret

_sio_libfn_diskio_rdsec:
    ; TODO
    ret
";

        // ============================================================
        // 3. FAT12
        // ============================================================
        public static string Fat12Vars = @"
; ===== FAT12 Vars =====
_sio_args_fat12_addftxt_filename:        dw 0
_sio_args_fat12_addftxt_text:            dw 0
_sio_args_fat12_ovrftxt_filename:        dw 0
_sio_args_fat12_ovrftxt_text:            dw 0
_sio_args_fat12_wrftxt_filename:         dw 0
_sio_args_fat12_wrftxt_text:             dw 0
_sio_args_fat12_fmt_drive:               db 0
_sio_args_fat12_rdfile_filename:         dw 0
_sio_args_fat12_rdfile_buffer:           dw 0
_sio_args_fat12_opfile_filename:         dw 0
_sio_args_fat12_mkfile_filename:         dw 0
_sio_args_fat12_mkflder_path:            dw 0
_sio_args_fat12_tree_path:               dw 0
_sio_args_fat12_fwatch_path:             dw 0
_sio_librel_fat12_ret:                   dw 0
";
        public static string Fat12Lib = @"
; ===== FAT12 Lib =====
; 待实现：FAT12 文件系统操作

_sio_libfn_fat12_addftxt:
    ; TODO
    ret

_sio_libfn_fat12_ovrftxt:
    ; TODO
    ret

_sio_libfn_fat12_wrftxt:
    ; TODO
    ret

_sio_libfn_fat12_fmt:
    ; TODO
    ret

_sio_libfn_fat12_rdfile:
    ; TODO
    ret

_sio_libfn_fat12_opfile:
    ; TODO
    ret

_sio_libfn_fat12_mkfile:
    ; TODO
    ret

_sio_libfn_fat12_mkflder:
    ; TODO
    ret

_sio_libfn_fat12_tree:
    ; TODO
    ret

_sio_libfn_fat12_fwatch:
    ; TODO
    ret
";

        // ============================================================
        // 4. Serial
        // ============================================================
        public static string SerialVars = @"
; ===== Serial Vars =====
_sio_args_serial_serprtwr_port:          dw 0
_sio_args_serial_serprtwr_data:          db 0
_sio_args_serial_serprtrd_port:          dw 0
_sio_librel_serial_ret:                  dw 0
";
        public static string SerialLib = @"
; ===== Serial Lib =====

_sio_libfn_serial_serprtwr:
    ; 写串口：AL = 数据，DX = 端口
    push ax
    push dx
    
    mov dx, [_sio_args_serial_serprtwr_port]
    mov al, [_sio_args_serial_serprtwr_data]
    out dx, al
    
    pop dx
    pop ax
    ret

_sio_libfn_serial_serprtrd:
    ; 读串口：DX = 端口，返回值 AL = 数据
    push ax
    push dx
    
    mov dx, [_sio_args_serial_serprtrd_port]
    in al, dx
    mov byte [_sio_librel_serial_ret], al
    
    pop dx
    pop ax
    ret

";

        // ============================================================
        // 5. Memgr
        // ============================================================
        public static string MemgrVars = @"
; ===== Memgr Vars =====
_sio_args_memgr_gotmemap_buffer:         dw 0
_sio_args_memgr_mkpgecall_size:          dw 0
_sio_args_memgr_dspage_page:             dw 0
_sio_args_memgr_meset_addr:              dw 0
_sio_args_memgr_meset_value:             db 0
_sio_args_memgr_meset_len:               dw 0
_sio_args_memgr_mwatch_addr:             dw 0
_sio_args_memgr_mwatch_len:              dw 0
_sio_librel_memgr_ret:                   dw 0
";
        public static string MemgrLib = @"
; ===== Memgr Lib =====
; 待实现：内存管理（页分配、内存映射）

_sio_libfn_memgr_gotmemap:
    ; TODO
    ret

_sio_libfn_memgr_mkpgecall:
    ; TODO
    ret

_sio_libfn_memgr_dspage:
    ; TODO
    ret

_sio_libfn_memgr_meset:
    ; TODO
    ret

_sio_libfn_memgr_mwatch:
    ; TODO
    ret
";

        // ============================================================
        // 6. Timekit
        // ============================================================
        public static string TimekitVars = @"
; ===== Timekit Vars =====
_sio_args_timekit_slpms_ms:              dw 0
_sio_args_timekit_slphur_hours:          dw 0
_sio_args_timekit_twatch_label:          dw 0
_sio_librel_timekit_ret:                 dw 0
";
        public static string TimekitLib = @"
; ===== Timekit Lib =====

_sio_libfn_timekit_slpms:
    ; INT 15h AH=86h: 微秒级延时
    ; 参数: CX:DX = 微秒数（1ms = 1000us）
    push ax
    push cx
    push dx
    
    ; 把毫秒转成微秒
    mov ax, [_sio_args_timekit_slpms_ms]
    mov cx, 1000
    mul cx
    mov cx, dx      ; 高16位
    mov dx, ax      ; 低16位
    mov ah, 0x86
    int 0x15
    
    pop dx
    pop cx
    pop ax
    ret

_sio_libfn_timekit_slphur:
    ; 延时小时：简单循环调用 slpms
    push ax
    push bx
    
    mov bx, [_sio_args_timekit_slphur_hours]
.loop_hur:
    ; 每调用一次延时 1000ms
    push bx
    mov word [_sio_args_timekit_slpms_ms], 1000
    call _sio_libfn_timekit_slpms
    pop bx
    dec bx
    jnz .loop_hur
    
    pop bx
    pop ax
    ret

_sio_libfn_timekit_goticks:
    ; INT 1Ah AH=00h: 获取系统滴答数
    push ax
    push bx
    
    mov ah, 0x00
    int 0x1A
    ; 返回 CX:DX = 滴答数
    mov word [_sio_librel_timekit_ret], dx
    mov word [_sio_librel_timekit_ret + 2], cx
    
    pop bx
    pop ax
    ret

_sio_libfn_timekit_twatch:
    ; 占位：时间监视器
    ret

";

        // ============================================================
        // 7. Kb-bus
        // ============================================================
        public static string KbbusVars = @"
; ===== Kb-bus Vars =====
_sio_args_kbbus_chgcode_layout:          db 0
_sio_librel_kbbus_ret:                   dw 0
";
        public static string KbbusLib = @"
; ===== Kb-bus Lib =====

_sio_libfn_kbbus_rdky:
    ; INT 16h AH=00h: 读取按键（阻塞）
    push ax
    mov ah, 0x00
    int 0x16
    ; AL = ASCII 码, AH = 扫描码
    mov word [_sio_librel_kbbus_ret], ax
    pop ax
    ret

_sio_libfn_kbbus_chkky:
    ; INT 16h AH=01h: 检查按键（非阻塞）
    push ax
    mov ah, 0x01
    int 0x16
    jz .no_key
    ; 有按键：ZF=0，AL=ASCII，AH=扫描码
    mov word [_sio_librel_kbbus_ret], 1
    pop ax
    ret
.no_key:
    ; 无按键：ZF=1
    mov word [_sio_librel_kbbus_ret], 0
    pop ax
    ret

_sio_libfn_kbbus_chgcode:
    ; 切换键盘布局：通过 INT 16h 设置
    ret

";

        // ============================================================
        // 8. Powermgr
        // ============================================================
        public static string PowermgrVars = @"
; ===== Powermgr Vars =====
_sio_librel_powermgr_ret:                dw 0
";
        public static string PowermgrLib = @"
;; ===== Powermgr Lib =====
_sio_libfn_powermgr_reset:
    ; 键盘控制器复位
    mov al, 0xFE
    out 0x64, al
    cli
    hlt
_sio_libfn_powermgr_halt:
    cli
    hlt
_sio_libfn_powermgr_stopmchine:
    ; 尝试 ACPI 关机（简化版：死循环停机）
    cli
    hlt
";

        // ============================================================
        // 9. Tool
        // ============================================================
        public static string ToolVars = @"
; ===== Tool Vars =====
_sio_args_tool_mecpy_src:                dw 0
_sio_args_tool_mecpy_dst:                dw 0
_sio_args_tool_mecpy_len:                dw 0
_sio_args_tool_strlen_str:               dw 0
_sio_args_tool_strcmp_str1:              dw 0
_sio_args_tool_strcmp_str2:              dw 0
_sio_args_tool_strspilt_str:             dw 0
_sio_args_tool_strspilt_delim:           dw 0
_sio_args_tool_strspilt_buf:             dw 0
_sio_args_tool_strmatch_str:             dw 0
_sio_args_tool_strmatch_pattern:         dw 0
_sio_args_tool_strreplace_str:           dw 0
_sio_args_tool_strreplace_old:           dw 0
_sio_args_tool_strreplace_newstr:        dw 0
_sio_librel_tool_ret:                    dw 0
";
        public static string ToolLib = @"
; ===== Tool Lib =====

_sio_libfn_tool_mecpy:
    ; memcpy(dst, src, len)
    push si
    push di
    push cx
    
    mov si, [_sio_args_tool_mecpy_src]
    mov di, [_sio_args_tool_mecpy_dst]
    mov cx, [_sio_args_tool_mecpy_len]
    cld
    rep movsb
    
    pop cx
    pop di
    pop si
    ret

_sio_libfn_tool_strlen:
    ; strlen(str) → 返回长度
    push si
    
    mov si, [_sio_args_tool_strlen_str]
    xor ax, ax
.loop_sl:
    lodsb
    or al, al
    jz .done_sl
    inc ax
    jmp .loop_sl
.done_sl:
    mov word [_sio_librel_tool_ret], ax
    
    pop si
    ret

_sio_libfn_tool_strcmp:
    ; strcmp(str1, str2) → 返回 0=相等
    push si
    push di
    
    mov si, [_sio_args_tool_strcmp_str1]
    mov di, [_sio_args_tool_strcmp_str2]
.loop_sc:
    lodsb
    scasb
    jne .diff_sc
    or al, al
    jnz .loop_sc
    xor ax, ax
    jmp .done_sc
.diff_sc:
    mov ax, 1
.done_sc:
    mov word [_sio_librel_tool_ret], ax
    
    pop di
    pop si
    ret

_sio_libfn_tool_strspilt:
    ; 字符串分割（简化版：按字符分割到缓冲区）
    push si
    push di
    
    mov si, [_sio_args_tool_strspilt_str]
    mov di, [_sio_args_tool_strspilt_buf]
    mov al, [_sio_args_tool_strspilt_delim]
    
.loop_ss:
    lodsb
    cmp al, 0
    je .done_ss
    stosb
    jmp .loop_ss
.done_ss:
    xor al, al
    stosb
    
    pop di
    pop si
    ret

_sio_libfn_tool_strmatch:
    ; 字符串匹配（简化：检查 str 是否包含 pattern）
    push si
    push di
    push ax
    
    mov si, [_sio_args_tool_strmatch_str]
    mov di, [_sio_args_tool_strmatch_pattern]
    
.search_outer:
    push si
    push di
.inner_loop:
    lodsb
    scasb
    jne .next_pos
    or al, al
    jnz .inner_loop
    ; 完全匹配
    pop di
    pop si
    mov word [_sio_librel_tool_ret], 1
    pop ax
    pop di
    pop si
    ret
.next_pos:
    pop di
    pop si
    inc si
    mov al, [si]
    or al, al
    jnz .search_outer
    ; 未找到
    mov word [_sio_librel_tool_ret], 0
    pop ax
    pop di
    pop si
    ret

_sio_libfn_tool_strreplace:
    ; 字符串替换（占位）
    ret

";

        // ============================================================
        // 10. Qasm
        // ============================================================
        public static string QasmVars = @"
; ===== Qasm Vars =====
_sio_args_qasm_wrasm_code:               dw 0
_sio_args_qasm_ovrdeasmln_line:          dw 0
_sio_args_qasm_ovrdeasmln_code:          dw 0
_sio_args_qasm_adasm_line:               dw 0
_sio_args_qasm_adasm_code:               dw 0
_sio_args_qasm_rmasmln_line:             dw 0
_sio_librel_qasm_ret:                    dw 0
";
        public static string QasmLib = @"
; ===== Qasm Lib =====
; 待实现：运行时汇编代码编辑

_sio_libfn_qasm_wrasm:
    ; TODO
    ret

_sio_libfn_qasm_ovrdeasmln:
    ; TODO
    ret

_sio_libfn_qasm_adasm:
    ; TODO
    ret

_sio_libfn_qasm_rmasmln:
    ; TODO
    ret
";

        // ============================================================
        // 11. Pe&Elf
        // ============================================================
        public static string PeelfVars = @"
; ===== Pe&Elf Vars =====
_sio_args_peelf_apploadpe_path:          dw 0
_sio_args_peelf_apploadelf_path:         dw 0
_sio_args_peelf_sysloadelf_path:         dw 0
_sio_args_peelf_sysloadpe_path:          dw 0
_sio_args_peelf_chkpe_path:              dw 0
_sio_args_peelf_chkelf_path:             dw 0
_sio_args_peelf_getentp_path:            dw 0
_sio_librel_peelf_ret:                   dw 0
";
        public static string PeelfLib = @"
; ===== Pe&Elf Lib =====
; 待实现：PE/ELF 文件加载器

_sio_libfn_peelf_apploadpe:
    ; TODO
    ret

_sio_libfn_peelf_apploadelf:
    ; TODO
    ret

_sio_libfn_peelf_sysloadelf:
    ; TODO
    ret

_sio_libfn_peelf_sysloadpe:
    ; TODO
    ret

_sio_libfn_peelf_chkpe:
    ; TODO
    ret

_sio_libfn_peelf_chkelf:
    ; TODO
    ret

_sio_libfn_peelf_getentp:
    ; TODO
    ret
";

        // ============================================================
        // 12. Advpci&Vga
        // ============================================================
        public static string AdvpcivgaVars = @"
; ===== Advpci&Vga Vars =====
_sio_args_advpcivga_vgamode_mode:        db 0
_sio_args_advpcivga_vgadrwpx_x:          dw 0
_sio_args_advpcivga_vgadrwpx_y:          dw 0
_sio_args_advpcivga_vgadrwpx_color:      db 0
_sio_args_advpcivga_vgadrwln_x1:         dw 0
_sio_args_advpcivga_vgadrwln_y1:         dw 0
_sio_args_advpcivga_vgadrwln_x2:         dw 0
_sio_args_advpcivga_vgadrwln_y2:         dw 0
_sio_args_advpcivga_vgadrwln_color:      db 0
_sio_args_advpcivga_vgadrwshp_type:      db 0
_sio_args_advpcivga_vgadrwshp_x:         dw 0
_sio_args_advpcivga_vgadrwshp_y:         dw 0
_sio_args_advpcivga_vgadrwshp_w:         dw 0
_sio_args_advpcivga_vgadrwshp_h:         dw 0
_sio_args_advpcivga_vgadrwshp_color:     db 0
_sio_args_advpcivga_pcicfgrd_bus:        db 0
_sio_args_advpcivga_pcicfgrd_device:     db 0
_sio_args_advpcivga_pcifind_vendor:      dw 0
_sio_args_advpcivga_pcifind_device:      dw 0
_sio_librel_advpcivga_ret:               dw 0
";
        public static string AdvpcivgaLib = @"
; ===== Advpci&Vga Lib =====
; 待实现：VGA 图形模式 + PCI 总线扫描

_sio_libfn_advpcivga_vgamode:
    ; TODO
    ret

_sio_libfn_advpcivga_vgadrwpx:
    ; TODO
    ret

_sio_libfn_advpcivga_vgadrwln:
    ; TODO
    ret

_sio_libfn_advpcivga_vgadrwshp:
    ; TODO
    ret

_sio_libfn_advpcivga_pcicfgrd:
    ; TODO
    ret

_sio_libfn_advpcivga_pcifind:
    ; TODO
    ret
";
    }
}
