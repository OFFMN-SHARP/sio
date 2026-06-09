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
            public bool IsPids;
            public string ResultLabel;      // 如果调用有 => 赋值，这里存变量名
        }
        public static string SioCode = string.Empty;
        public static Dictionary<string, FunctionCallInfo> LibFunctions = new();
        public static Dictionary<string, FunctionCallInfo> DevFunctions = new();
        public static Dictionary<string, string> StringConstants = new();
        public static Dictionary<string, string[]> Codes = new();
        public static Dictionary<string, string> VariablesLabels = new();
        public static void AddLibFunction(string name, string label, string libname, int paramCount, string relvar, string[] paramSlots)
        {
            LibFunctions[name] = new FunctionCallInfo
            {
                Label = "_sio_libfn_" + libname + "_" + label,
                ParamCount = paramCount,
                ParamSlots = paramSlots,
                ResultLabel = "_sio_librel_" + libname + "_" + relvar
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
            VariablesLabels[name] = "_sio_vars_cret_" + name;
        }
        public static string ParserCode()
        {
            string UParsered = string.Join(Environment.NewLine, Program.SIOFile)
                   + Environment.NewLine
                   + Parser.IncludedCode;

            var lines = UParsered.Split(new[] { "\n", "\r\n", "\n\r" }, StringSplitOptions.None);
            string Parsered = string.Empty;
            foreach (var line in lines)
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
                        IsPids = isPids,
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
        public const string Indent = "    ";
        public static StringBuilder MainBooterASMParser()
        {
            var Parsered = new StringBuilder();
            Codes.TryGetValue("main", out var maincode);
            bool isPids = false;
            if (DevFunctions.TryGetValue("main", out var mainInfo))
                isPids = mainInfo.IsPids;
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
            if (maincode != null)
            {
                string asm = Generate(0, isPids, maincode);
                Parsered.AppendLine("MainBooter:");
                Parsered.Append(asm);
                if (!isPids)
                {
                    Parsered.AppendLine("    cli");
                    Parsered.AppendLine("    hlt");
                }
            }
            return Parsered;
        }
        public static StringBuilder ASMParser()
        {
            var Parsered = new StringBuilder();
            // 遍历所有非 main 函数
            foreach (var kvp in Codes)
            {
                string funcName = kvp.Key;
                string[] code = kvp.Value;

                if (!DevFunctions.TryGetValue(funcName, out var funcInfo)) continue;

                // 生成函数标签
                Parsered.AppendLine($"_sio_devfn_{funcName}:");

                // 如果是 pids，生成自动释放检查
                if (funcInfo.IsPids)
                {
                    Parsered.AppendLine($"    cmp byte [{GetDisposeFlag(funcName)}], 1");
                    Parsered.AppendLine("    jne .skip_dispose");
                    Parsered.AppendLine($"    call {GetDisposeFunc(funcName)}");
                    Parsered.AppendLine(".skip_dispose:");
                }

                // 生成函数体
                string body = Generate(0, funcInfo.IsPids, code);
                Parsered.Append(body);

                // pids 的 .exit 已经在 Generate 里生成了
                if (!funcInfo.IsPids)
                    Parsered.AppendLine("    ret");
            }
            if (StringConstants.Count > 0)
            {
                Parsered.AppendLine("; ===== String Constants =====");
                foreach (var kvp in StringConstants)
                {
                    Parsered.AppendLine($"{kvp.Key}: db \"{kvp.Value}\", 0");
                }
            }
            return Parsered;
        }

        public static StringBuilder output = new StringBuilder();
        public static int labelCounter = 0;
        public static string NewLabel() => $".L{labelCounter++}";

        public static string Generate(int indentLevel, bool pids, string[] lines)
        {
            var output = new StringBuilder();
            int i = 0;
            List<string> finalCode = new();   // final 块代码
            List<string> disposeCode = new(); // ids 块代码（pids 的清理）
            bool hasFinal = false;
            bool hasIds = false;

            while (i < lines.Length)
            {
                string line = lines[i];
                int indent = GetIndentLevel(line);
                string trimmed = line.TrimStart();

                // 缩进回退到当前层级以下 → 块结束
                if (indent < indentLevel)
                    break;

                // 只处理当前层级的语句
                if (indent == indentLevel)
                {
                    // === ids: ===
                    if (trimmed == "ids:")
                    {
                        hasIds = true;
                        // 收集 ids 块内的代码（缩进 +1）
                        i++;
                        while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                        {
                            disposeCode.Add(lines[i]);
                            i++;
                        }
                        continue;
                    }

                    // === final: ===
                    if (trimmed == "final:")
                    {
                        hasFinal = true;
                        // 收集 final 块内的代码（缩进 +1）
                        i++;
                        while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                        {
                            finalCode.Add(lines[i]);
                            i++;
                        }
                        continue;
                    }

                    // === var 声明 ===
                    if (trimmed.StartsWith("var "))
                    {
                        string varName = ExtractVarName(trimmed);
                        AddVariable(varName);
                        output.AppendLine($"    ; var {varName}");
                        // 如果有初始值
                        int eqIndex = trimmed.IndexOf('=');
                        if (eqIndex >= 0)
                        {
                            string initValue = trimmed.Substring(eqIndex + 1).Trim();
                            output.AppendLine($"    mov word [{VariablesLabels[varName]}], {ParseValue(initValue)}");
                        }
                        i++;
                        continue;
                    }

                    // === ret ===
                    if (trimmed.StartsWith("ret("))
                    {
                        // 提取返回值
                        int end = trimmed.IndexOf(')');
                        string retVal = trimmed.Substring(4, end - 4).Trim();

                        if (!string.IsNullOrEmpty(retVal))
                            output.AppendLine($"    mov ax, {ParseValue(retVal)}");

                        if (pids)
                            output.AppendLine("    jmp .exit");
                        else
                            output.AppendLine("    ret");
                        i++;
                        continue;
                    }

                    // === if ===
                    if (trimmed.StartsWith("if "))
                    {
                        string elseLabel = NewLabel();
                        string endLabel = NewLabel();

                        // 解析条件 "if(x == 1):"
                        int parenOpen = trimmed.IndexOf('(');
                        int parenClose = trimmed.IndexOf(')');
                        string condition = trimmed.Substring(parenOpen + 1, parenClose - parenOpen - 1);
                        var (left, op, right) = ParseCondition(condition);

                        // 生成条件判断
                        output.AppendLine($"    mov ax, {ParseValue(left)}");
                        output.AppendLine($"    cmp ax, {ParseValue(right)}");
                        output.AppendLine($"    j{GetOppositeJump(op)} {elseLabel}");

                        // if 体
                        i++;
                        output.Append(Generate(indentLevel + 1, pids, lines.Skip(i).ToArray()));

                        // 跳过已处理的 if 体
                        while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                            i++;

                        // === elif ===
                        while (i < lines.Length)
                        {
                            string nextTrimmed = lines[i].TrimStart();
                            if (GetIndentLevel(lines[i]) == indentLevel && nextTrimmed.StartsWith("elif "))
                            {
                                output.AppendLine($"    jmp {endLabel}");
                                output.AppendLine($"{elseLabel}:");

                                // 解析 elif 条件
                                int po = nextTrimmed.IndexOf('(');
                                int pc = nextTrimmed.IndexOf(')');
                                string elifCond = nextTrimmed.Substring(po + 1, pc - po - 1);
                                var (l, o, r) = ParseCondition(elifCond);

                                output.AppendLine($"    mov ax, {ParseValue(l)}");
                                output.AppendLine($"    cmp ax, {ParseValue(r)}");
                                string nextElseLabel = NewLabel();
                                output.AppendLine($"    j{GetOppositeJump(o)} {nextElseLabel}");

                                i++;
                                output.Append(Generate(indentLevel + 1, pids, lines.Skip(i).ToArray()));

                                while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                                    i++;

                                elseLabel = nextElseLabel;
                                continue;
                            }
                            break;
                        }

                        // === else ===
                        if (i < lines.Length)
                        {
                            string nextTrimmed = lines[i].TrimStart();
                            if (GetIndentLevel(lines[i]) == indentLevel && nextTrimmed == "else:")
                            {
                                output.AppendLine($"    jmp {endLabel}");
                                output.AppendLine($"{elseLabel}:");
                                i++;
                                output.Append(Generate(indentLevel + 1, pids, lines.Skip(i).ToArray()));

                                while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                                    i++;
                            }
                            else
                            {
                                output.AppendLine($"{elseLabel}:");
                            }
                        }
                        else
                        {
                            output.AppendLine($"{elseLabel}:");
                        }

                        output.AppendLine($"{endLabel}:");

                        // 插入 final 代码（if 结束后执行）
                        if (hasFinal)
                        {
                            foreach (var fl in finalCode)
                                output.AppendLine($"    {fl.TrimStart()}");
                        }

                        continue;
                    }

                    // === while ===
                    if (trimmed.StartsWith("while "))
                    {
                        string loopLabel = NewLabel();
                        string endLabel = NewLabel();

                        int po = trimmed.IndexOf('(');
                        int pc = trimmed.IndexOf(')');
                        string condition = trimmed.Substring(po + 1, pc - po - 1);
                        var (left, op, right) = ParseCondition(condition);

                        output.AppendLine($"{loopLabel}:");
                        output.AppendLine($"    mov ax, {ParseValue(left)}");
                        output.AppendLine($"    cmp ax, {ParseValue(right)}");
                        output.AppendLine($"    j{GetOppositeJump(op)} {endLabel}");

                        i++;
                        output.Append(Generate(indentLevel + 1, pids, lines.Skip(i).ToArray()));

                        while (i < lines.Length && GetIndentLevel(lines[i]) > indentLevel)
                            i++;

                        output.AppendLine($"    jmp {loopLabel}");
                        output.AppendLine($"{endLabel}:");

                        if (hasFinal)
                        {
                            foreach (var fl in finalCode)
                                output.AppendLine($"    {fl.TrimStart()}");
                        }

                        continue;
                    }

                    // === 函数调用（最通用的匹配） ===
                    // 匹配: func() 或 func(args) 或 var = func() 或 func.udsp() / func.cdsp()
                    if (trimmed.Contains('('))
                    {
                        // 检查是不是 .udsp / .cdsp
                        if (trimmed.EndsWith(".udsp()"))
                        {
                            string target = trimmed.Substring(0, trimmed.Length - 7);
                            output.AppendLine($"    ; udsp {target}");
                            output.AppendLine($"    mov byte [{GetDisposeFlag(target)}], 0");
                            i++;
                            continue;
                        }
                        if (trimmed.EndsWith(".cdsp()"))
                        {
                            string target = trimmed.Substring(0, trimmed.Length - 7);
                            output.AppendLine($"    ; cdsp {target}");
                            output.AppendLine($"    call {GetDisposeFunc(target)}");
                            i++;
                            continue;
                        }

                        // 检查有没有 => 赋值
                        string assignVar = null;
                        string callPart = trimmed;
                        int arrowIndex = trimmed.IndexOf("=>");
                        if (arrowIndex >= 0)
                        {
                            assignVar = trimmed.Substring(0, arrowIndex).Trim();
                            callPart = trimmed.Substring(arrowIndex + 2).Trim();
                        }

                        // 解析函数名和参数
                        int po2 = callPart.IndexOf('(');
                        int pc2 = callPart.LastIndexOf(')');
                        string funcName = callPart.Substring(0, po2).Trim();
                        string argsStr = callPart.Substring(po2 + 1, pc2 - po2 - 1);
                        string[] args = string.IsNullOrWhiteSpace(argsStr)
                            ? new string[0]
                            : argsStr.Split(',').Select(a => a.Trim()).ToArray();

                        // 查函数表
                        FunctionCallInfo funcInfo = null;
                        if (DevFunctions.TryGetValue(funcName, out var devFunc))
                            funcInfo = devFunc;
                        else if (LibFunctions.TryGetValue(funcName, out var libFunc))
                            funcInfo = libFunc;

                        if (funcInfo != null)
                        {
                            // 设置参数
                            for (int a = 0; a < args.Length && a < funcInfo.ParamSlots.Length; a++)
                            {
                                string argAsm = ParseValue(args[a]);
                                output.AppendLine($"    mov word [{funcInfo.ParamSlots[a]}], {argAsm}");
                            }

                            // call
                            output.AppendLine($"    call {funcInfo.Label}");

                            // 如果有 => 赋值，存返回值
                            if (assignVar != null)
                            {
                                AddVariable(assignVar);
                                output.AppendLine($"    mov [{VariablesLabels[assignVar]}], ax");
                            }
                        }
                        else
                        {
                            // 函数未找到，生成错误注释
                            output.AppendLine($"    ; ERROR: unknown function '{funcName}'");
                        }

                        i++;
                        continue;
                    }
                }

                // 如果在当前层级没有匹配，继续下一行
                i++;
            }

            // === pids 的 .exit 标签 ===
            if (pids && (hasIds || hasFinal))
            {
                output.AppendLine(".exit:");

                // 先执行 ids 清理代码
                foreach (var dl in disposeCode)
                    output.AppendLine($"    {dl.TrimStart()}");

                // 再执行 final 代码
                foreach (var fl in finalCode)
                    output.AppendLine($"    {fl.TrimStart()}");

                output.AppendLine("    ret");
            }

            return output.ToString();
        }

        public static (string left, string op, string right) ParseCondition(string cond)
        {
            // "x == 1" → ("x", "==", "1")
            // "x != 0" → ("x", "!=", "0")
            // "x < 10" → ("x", "<", "10")
            string[] ops = { "!=", "==", "<=", ">=", "<", ">" };
            foreach (string op in ops)
            {
                int idx = cond.IndexOf(op);
                if (idx >= 0)
                {
                    string left = cond.Substring(0, idx).Trim();
                    string right = cond.Substring(idx + op.Length).Trim();
                    return (left, op, right);
                }
            }
            return (cond, "!=", "0"); // 默认当作布尔值
        }

        public static string GetOppositeJump(string op) => op switch
        {
            "==" => "ne",
            "!=" => "e",
            "<" => "ge",
            ">" => "le",
            "<=" => "g",
            ">=" => "l",
            _ => "e"
        };

        public static string ParseValue(string val)
        {
            // 如果是数字
            if (int.TryParse(val, out _))
                return val;

            if (val.StartsWith("\"") && val.EndsWith("\""))
            {
                string content = val.Substring(1, val.Length - 2);  // 去掉引号，得到 "hello"
                string strLabel = $"_sio_str_{labelCounter++}";
                StringConstants[strLabel] = content;  // ← 存到字典里
                return strLabel;
            }

            // 如果是变量名
            if (VariablesLabels.ContainsKey(val))
                return $"[{VariablesLabels[val]}]";

            // 如果是常量引用 console.width 等
            if (val.Contains('.'))
                return $"[{val}]";

            // 默认当作变量
            return val;
        }

        public static string ExtractVarName(string line)
        {
            // "var x = 5" → "x"
            // "var name"  → "name"
            string afterVar = line.Substring(4).Trim();
            int eqIndex = afterVar.IndexOf('=');
            if (eqIndex >= 0)
                return afterVar.Substring(0, eqIndex).Trim();
            int spaceIndex = afterVar.IndexOf(' ');
            return spaceIndex >= 0 ? afterVar.Substring(0, spaceIndex) : afterVar;
        }
        // 获取 pids 函数的自动释放标志位标签
        public static string GetDisposeFlag(string funcName)
        {
            return $"_sio_pidsflag_{funcName}";
        }

        // 获取 pids 函数的清理函数标签
        public static string GetDisposeFunc(string funcName)
        {
            return $"_sio_pidsclean_{funcName}";
        }

        // 生成 pids 函数的清理函数（在 ASMParser 里调用）
        public static StringBuilder GenerateDisposeFunc(string funcName, string[] finalCode, string[] idsCode)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"_sio_pidsclean_{funcName}:");

            // 先执行 ids 块
            foreach (var line in idsCode)
                sb.AppendLine($"    {line.TrimStart()}");

            // 再执行 final 块
            foreach (var line in finalCode)
                sb.AppendLine($"    {line.TrimStart()}");

            sb.AppendLine("    ret");
            return sb;
        }

        // 生成 pids 函数的标志初始化（在 ASMParser 里调用）
        public static StringBuilder GenerateDisposeFlagInit(string funcName)
        {
            var sb = new StringBuilder();
            // 默认自动释放标志为 1（自动释放）
            sb.AppendLine($"{GetDisposeFlag(funcName)}: db 1");
            return sb;
        }
    }
}
