# Sio

**A minimal, bootable programming language for BIOS, UEFI, and Multiboot.**

Sio is a small, compiled language designed to make boot sector and bootloader development accessible. It compiles directly to NASM assembly, and from there to raw binary images ready to `dd` onto a disk or run in QEMU.

If you’ve ever wanted to write your own bootloader but got stuck on assembly syntax, Sio is for you. If you already know assembly, Sio might save you some typing.

---

## What it is

- A **compiled language** whose compiler is written in C#.
- Generates **NASM assembly** as its intermediate output.
- Supports **BIOS (MBR)**, **UEFI**, and **Multiboot** via a single `@ppc=` declaration.
- Comes with a small standard library covering text output, disk I/O, FAT12 filesystem, keyboard input, serial, memory map, and more.
- Includes a minimal **REPL/editor** (`sio idle`) for writing and testing code interactively.
- Supports **protected mode** (`@pctmod=true`) with automatic GDT/IDT setup.
- Allows including other `.sio` files via `@inc=`, with automatic deduplication of functions and libraries.

---

## What it is NOT

- A general-purpose programming language. Sio is for bootloaders and bare-metal experiments, not for writing desktop applications.
- A replacement for GRUB or Limine. You can write a boot menu in Sio, but Sio itself is not a boot manager.
- A C competitor. Sio targets the niche of “I need a boot sector and I don’t want to write 200 lines of assembly by hand.”

---

## Quick start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (to build the compiler)
- [NASM](https://www.nasm.us/) (to assemble the generated output)
- [QEMU](https://www.qemu.org/) (optional, for testing)

### Build the compiler

```bash
git clone https://github.com/OFFMN-SHARP/sio.git
cd sio
dotnet build -c Release
```

### Write your first Sio program

File: `hello.sio`

```
@ppc=bios
@lib=console

var msg = "Hello from Sio!"
putsln(msg)
ret(0)
```

### Compile and run

```bash
sio build hello.sio          # produces hello.bin
qemu-system-x86_64 -hda hello.bin
```

You should see `Hello from Sio!` printed on screen.

---

## Supported platforms

| `@ppc=` value | Target |
|---------------|--------|
| `bios` | 16-bit MBR (512 bytes + optional second stage) |
| `efi` | 64-bit UEFI application |
| `mboot` | 32-bit Multiboot-compliant kernel (GRUB/Limine) |

Add `@pctmod=true` on the next line to enable 32-bit protected mode (BIOS and Multiboot only).

---

## Standard library (`@lib=`)

| Library | Purpose |
|---------|---------|
| `console` | Text output (`putsln`, `putc`, `gopos`, colors) |
| `diskio` | Disk read/write (BIOS INT 13h) |
| `fat12` | FAT12 filesystem read/write |
| `serial` | Serial port output (COM1) |
| `memgr` | Memory map detection (E820) |
| `timekit` | Delays and timers (PIT) |
| `kb-bus` | Keyboard input |
| `powermgr` | Reboot, shutdown |
| `tool` | String/memory utility functions |
| `qasm` | Inline assembly escape hatch |

Use `@lib=all` to import all available libraries.

---

## Language reference

See [docs/language.md](docs/language.md) for the full syntax specification.

Key syntax points:

- **Variables**: `var name = "value"` (type inferred)
- **Functions**: `pdc myFunc(args):` (procedure definition)
- **Output**: `putsln("text")`, `putc('A')`
- **Loops**: `while(cond) { ... }`, `for(i=0; i<10; i+=1) { ... }`
- **Conditionals**: `if(cond) { ... } else { ... }`
- **Return**: `ret(0)`
- **Naming**: camelCase for all identifiers
- **File header**: `@ppc=`, `@lib=`, `@inc=` must be the first three lines

---

## Limitations

- **No package manager yet.** Libraries are bundled with the compiler. Custom libraries require modifying the compiler source.
- **No floating-point support.** Sio targets 16-bit and 32-bit environments where FPU may not be available.
- **Protected mode is “rigid”.** GDT, IDT, and page tables are automatically generated with reasonable defaults. If you need a custom GDT, write it in assembly via the `qasm` library.
- **Single-file output.** All code is compiled into one `.bin` file. For complex multi-stage bootloaders, use multiple `.sio` files with `@inc=`.

---

## Contributing

Sio is MIT-licensed. Contributions are welcome, especially:

- New library backends (ARM, RISC-V, etc.)
- Additional standard library functions
- Documentation improvements
- Bug reports (there will be bugs)

Open an issue or a pull request.

---

## License

MIT. See [LICENSE](LICENSE) for details.

---

## Why “Sio”?

SiO is the chemical formula for silicon monoxide — an unstable intermediate oxide that exists briefly before becoming SiO₂ (quartz, the material of computer chips). A bootloader is the same: a transient intermediate that exists just long enough to bring up the real system, then disappears.

Sio the language is named after this idea. Sio the CLI is named after Sio. The repository is named after Sio. Everything is `sio`, lowercase, because the name should be as simple as the tool.
