using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class LibASM
    {
        public static string ConsoleVars = @";<ConsoleVars>
";//里面的代码会放在asm的变量定义区
        public static string ConsoleLib = @";<LibConsole>
";
        public static string Fat12Lib = @";<LibFat12>
";
    }
}
