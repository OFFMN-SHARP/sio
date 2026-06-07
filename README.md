# Sio

**A bootable programming language for BIOS/UEFI/Multiboot — work in progress.**

Sio is a compiled language targeting boot sector and bootloader development.
It compiles to NASM assembly, then to raw binary for QEMU or real hardware.

**Current status: active development, not yet usable.**

---

## What exists today

| Component |状态|
|-----------|--------|
| **Parser** (`Parser.cs`) | ✅ `@ppc=` / `@lib=` / `@inc=` / `@pctmode=` parsing done |
| **AST** (`AST.cs`) | ✅ Function/variable scanning, code generation framework |
| **Head generation** | ✅ MBR, UEFI, Multiboot entry code generation |
| **REPL/CLI** | ✅ Interactive mode with colored prompt |
| **lib registration** | ✅ 12 libraries, 65 functions registered in `Import()` |
| **Lib ASM stubs** | ✅ All variable slots and function labels written (all `; TODO`) |
| **ASM implementation** | ❌ **All library functions are stubs — none actually work yet** |
| **`pdc main()` → MainBooter** | ✅ AST generates the label and code |
| **`pids` / `ids` / `udsp` / `cdsp`** | ✅ AST recognizes them, generates `jmp .exit` patterns |
| **if/while/ret** | ✅ AST generates conditional jumps |
| **for / case** | ❌ Not implemented |
| **Full compile pipeline** | ❌ No NASM invocation, no `.bin` output yet |
| **Hello World** | ❌ **Not yet — blocked on console.asm implementation** |

---

## Known issues

- **`Import()` inserts library ASM, but all functions are `; TODO` stubs**
- **`Include()` has duplicate `IncludedCode` append bug** (appends per file per file)
- **`IncludedCode` mixes comments into AST source** (no separation between pure code and annotated code)
- **`MainParser()` had infinite recursion** (fixed now: `Head()` → `Import()` → `AST.Paeser()`)
- **String constant pool not generated** (`ParseValue("hello")` produces label references but no `db` data)
- **No NASA/AFL invocation** — compiler outputs ASM text, stops there
- **`for` loop not implemented in `Generate()`**

---

## Quick start (when it works)

```bash
# Prerequisites
# - .NET 8.0 SDK
# - NASM
# - QEMU (optional, for testing)

# Build
git clone https://github.com/OFFMN-SHARP/sio.git
cd sio
dotnet build -c Release

# Write (when libraries are implemented)
@ppc=bios
@lib=console

pdc main():
    putsln("Hello from Sio!")
    ret(0)

# Eventually:
# ./sio build hello.sio → hello.bin
# qemu-system-x86_64 -hda hello.bin
```

---

## What's left to do

Priority order:

1. **Implement `console.asm`** — `putsln` / `puts` / `clscr` at minimum (INT 10h)
2. **Implement `tool.asm`** — `mecpy` / `strlen` / `strcmp` (used by other libs)
3. **Add NASM invocation** — shell out to NASM after ASM generation
4. **Add string constant pool** — collect `"hello"` literals, emit `db` data
5. **Fix `Include()` duplicate bug** — `IncludedCode` appended inside foreach loop
6. **Implement `diskio.asm`** — INT 13h read/write
7. **Implement `fat12.asm`** — depends on `diskio`
8. **Implement remaining libs** — serial, memgr, timekit, kbbus, powermgr, qasm, pe&elf, advpci&vga
9. **Add `for` loop** to AST `Generate()`
10. **Add `case`** (optional — can be expressed as if/elif/else)

---

## Architecture

```
.sio file
  ├── @ppc=bios/efi/mboot     → Head() generates entry code
  ├── @lib=xxx                → Import() registers 65+ functions
  ├── @inc=path               → Include() merges other .sio files
  └── pdc/pids functions      → AST.Paeser() generates ASM

Parser.cs → Head() → Import() → AST.Paeser()
                              ↓
                    Program.ParseredAsm (StringBuilder)
                              ↓
                    Invoke NASM → .bin file
```

---

## Library status

| Library | Functions | ASM stubs | Impl. needed |
|---------|-----------|-----------|--------------|
| console | 13 | ✅ Labels + vars | ❌ All `; TODO` |
| diskio | 3 | ✅ | ❌ |
| fat12 | 10 | ✅ | ❌ |
| serial | 2 | ✅ | ❌ |
| memgr | 5 | ✅ | ❌ |
| timekit | 4 | ✅ | ❌ |
| kbbus | 3 | ✅ | ❌ |
| powermgr | 3 | ✅ | ❌ |
| tool | 6 | ✅ | ❌ |
| qasm | 4 | ✅ | ❌ |
| pe&elf | 7 | ✅ | ❌ |
| advpci&vga | 6 | ✅ | ❌ |

**Total: 65 function slots, 0 implemented.**

---

##名字

SiO = silicon monoxide — an unstable intermediate oxide that exists briefly before becoming SiO₂. A bootloader is the same: a transient that exists just long enough to bring up the real system, then disappears. Sio the language is named after this idea.

---

## License

MIT
