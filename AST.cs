using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sio
{
    public static class AST
    {
        public class FunctionCallInfo
        {
            public string Label;           // ASM 标签名，如 "__console_putsln"
            public int ParamCount;         // 参数个数
            public string[] ParamSlots;       // ← 参数对应的 ASM 变量名
            public string ResultLabel;      // 如果调用有 => 赋值，这里存变量名
        }
        public static string SioCode=string.Empty;
        public static Dictionary<string, FunctionCallInfo> LibFunctions = new();
        public static Dictionary <string,FunctionCallInfo> DevFunctions = new();
        public static Dictionary<string, string[]> Codes= new();
        public static Dictionary<string, string> VariablesLabels = new();
        public static void AddLibFunction(string name, string label,string libname, int paramCount,string relvar,string[] paramSlots)
        {
            LibFunctions[name] = new FunctionCallInfo {
                Label = "_sio_libfn_"+libname+"_"+label,
                ParamCount = paramCount,
                ParamSlots = paramSlots,
                ResultLabel = "_sio_librel_"+libname+"_"+ relvar 
            };
        }
        public static void AddDevFunction(string name, string label, int paramCount, string relvar, string[] paramSlots, string[] code)
        {
            DevFunctions[name] = new FunctionCallInfo
            {
                Label = "_sio_devfn_" + label,
                ParamCount = paramCount,
                ParamSlots = paramSlots,
                ResultLabel = "_sio_devrel_" + relvar
            };
            Codes[name] = code;
        }
        public static void AddVariable(string name)
        {
            VariablesLabels[name] = "_sio_vars_cret_"+name;
        }
        public static string ParserCode()
        {
            string UParsered = string.Join(Environment.NewLine, Program.SIOFile)
                   + Environment.NewLine
                   + Parser.IncludedCode;

            var lines = UParsered.Split(new[] {"\n","\r\n","\n\r" }, StringSplitOptions.None);
            string Parsered = string.Empty;
            foreach ( var line in lines)
            {
                if (!line.StartsWith("#") && !line.StartsWith("@") && !line.StartsWith("["))
                {
                    Parsered += line + Environment.NewLine;
                }
            }
            return Parsered;
        }
        public static Dictionary<string, FunctionCallInfo> ParserFunction()
        {
            var functions = new Dictionary<string, FunctionCallInfo>();
            var lines = SioCode.Split(new[] { "\r\n", "\n", "\n\r" }, StringSplitOptions.None);

            int i = 0;
            while (i < lines.Length)
            {
                string trimmed = lines[i].TrimStart();
                int indent = GetIndentLevel(lines[i]);

                // 检测函数定义
                if ((trimmed.StartsWith("pdc ") || trimmed.StartsWith("pids ")))
                {
                    bool isPids = trimmed.StartsWith("pids ");

                    // 解析函数名和参数
                    // "pdc main():" 或 "pids foo(x, y):"
                    int parenOpen = trimmed.IndexOf('(');
                    int parenClose = trimmed.IndexOf(')');

                    if (parenOpen < 0 || parenClose < 0)
                    {
                        i++;
                        continue;
                    }

                    string funcName = trimmed.Substring(4, parenOpen - 4).Trim();
                    // 注意：有可能是 "pdc main():"，去掉末尾的 ":"
                    if (funcName.EndsWith(":"))
                        funcName = funcName.Substring(0, funcName.Length - 1);

                    string paramsStr = trimmed.Substring(parenOpen + 1, parenClose - parenOpen - 1);
                    string[] paramNames = string.IsNullOrWhiteSpace(paramsStr)
                        ? new string[0]
                        : paramsStr.Split(',').Select(p => p.Trim()).ToArray();

                    // 构建参数槽位名
                    string[] paramSlots = paramNames.Select(p =>
                        $"_sio_args_{funcName}_{p}"
                    ).ToArray();

                    // 返回值槽位
                    string relvar = "ret";
                    string resultLabel = isPids
                        ? $"_sio_pidsrel_{funcName}"
                        : $"_sio_devrel_{funcName}";

                    // 收集函数体内的代码行
                    int funcBodyIndent = indent + 1;
                    i++;
                    List<string> codeLines = new List<string>();

                    while (i < lines.Length)
                    {
                        int lineIndent = GetIndentLevel(lines[i]);
                        string lineTrimmed = lines[i].TrimStart();

                        // 缩进回退到函数定义层级或遇到新的顶层定义
                        if (lineIndent < funcBodyIndent ||
                            (lineIndent == 0 && (lineTrimmed.StartsWith("pdc ") || lineTrimmed.StartsWith("pids "))))
                            break;

                        // 跳过函数签名行本身的残留
                        if (lineIndent >= funcBodyIndent)
                            codeLines.Add(lines[i]);

                        i++;
                    }

                    // 注册函数
                    var funcInfo = new FunctionCallInfo
                    {
                        Label = $"_sio_devfn_{funcName}",
                        ParamCount = paramNames.Length,
                        ParamSlots = paramSlots,
                        ResultLabel = resultLabel
                    };

                    functions[funcName] = funcInfo;
                    Codes[funcName] = codeLines.ToArray();

                    continue;  // i 已经在 while 循环里自增过了
                }

                i++;
            }

            return functions;
        }
        public static int GetIndentLevel(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == ' ') count++;
                else break;
            }
            return count / 4;
        }
        public static Dictionary<string, string> ParserVar()
        {
            var vars = new Dictionary<string, string>();
            var lines = SioCode.Split(new[] { "\r\n", "\n", "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
            int currentIndent = -1;
            bool inFunction = false;

            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();
                int indent = GetIndentLevel(line);

                // 检测函数定义开始
                if ((trimmed.StartsWith("pdc ") || trimmed.StartsWith("pids ")))
                {
                    inFunction = true;
                    currentIndent = indent;
                    continue;
                }

                // 检测函数定义结束（缩进回退到函数定义的层级）
                if (inFunction && indent <= currentIndent && !string.IsNullOrWhiteSpace(trimmed))
                {
                    // 遇到新的顶层结构，退出函数
                    if (trimmed.StartsWith("pdc ") || trimmed.StartsWith("pids "))
                    {
                        inFunction = true;
                        currentIndent = indent;
                        continue;
                    }
                    inFunction = false;
                }

                // 在函数内部找 var 声明
                if (inFunction && trimmed.StartsWith("var "))
                {
                    // var x = "hello"  → 提取 x
                    // var x           → 提取 x
                    string afterVar = trimmed.Substring(4).Trim();
                    int eqIndex = afterVar.IndexOf('=');
                    string varName;

                    if (eqIndex >= 0)
                        varName = afterVar.Substring(0, eqIndex).Trim();
                    else
                    {
                        // 可能后面有换行或注释，取到空格或行尾
                        int spaceIndex = afterVar.IndexOf(' ');
                        varName = spaceIndex >= 0 ? afterVar.Substring(0, spaceIndex) : afterVar;
                    }

                    if (!string.IsNullOrEmpty(varName) && !vars.ContainsKey(varName))
                    {
                        AddVariable(varName);
                        vars[varName] = VariablesLabels[varName];
                    }
                }
            }

            return vars;
        }

        public static void Paeser()
        {
            SioCode = ParserCode();
            DevFunctions = ParserFunction();
            VariablesLabels = ParserVar();
            Program.ParseredAsm.AppendLine(MainBooterASMParser().ToString());
            Program.ParseredAsm.AppendLine(ASMParser().ToString());
        }
        public static StringBuilder MainBooterASMParser()
        {
            var Parsered = new StringBuilder();
            Codes.TryGetValue("main", out var maincode);
            Codes.Remove("main");
            DevFunctions.Remove("main");
            if (maincode == null)
            {
                Codes.TryGetValue("Main", out maincode);
                Codes.Remove("Main");
                DevFunctions.Remove("Main");
                if (maincode == null)
                {
                    Codes.TryGetValue("MAIN", out maincode);
                    Codes.Remove("MAIN");
                    DevFunctions.Remove("MAIN");
                    if (maincode == null)
                    {
                        throw new Exception("No main function found! Please define a main function with 'pdc main():' or 'pids main():'");
                    }
                }
            }
            return Parsered;
        }
        public static StringBuilder ASMParser()
        {
            var Parsered = new StringBuilder();
            return Parsered;
        }
    }
}
