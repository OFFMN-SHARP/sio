using System.Text;
using System.Text.RegularExpressions;

public static class FileSearcher
{
    // 解析通配符，返回匹配的文件列表
    public static string[] Search(string pattern)
    {
        // 分离目录和通配符部分
        // "./tst/(??)" → dir="./tst/", wild="(??)"
        // "./data/[ex:sio]" → dir="./data/", wild="[ex:sio]"

        string dir = ".";
        string wild = pattern;

        // 找最后一个 / 或 \ 分割目录和文件名部分
        int lastSlash = pattern.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSlash >= 0)
        {
            dir = pattern.Substring(0, lastSlash + 1);
            wild = pattern.Substring(lastSlash + 1);
        }

        if (!Directory.Exists(dir))
            return new string[0];

        // 获取目录下所有文件
        string[] allFiles = Directory.GetFiles(dir);

        // 解析通配符链
        string[] wildcards = ParseWildcards(wild);

        // 逐个过滤
        List<string> results = new List<string>();
        foreach (string file in allFiles)
        {
            if (MatchAll(file, wildcards))
                results.Add(file);
        }

        return results.ToArray();
    }

    // 解析通配符字符串成标签数组
    // "[ex:sio][nhd:mai]" → ["ex:sio", "nhd:mai"]
    // "(??)" → ["?(??)"]
    // 混合: "./tst/(??)[ex:sio]" → ["?(??)", "ex:sio"]
    public static string[] ParseWildcards(string wild)
    {
        List<string> tags = new List<string>();
        int i = 0;

        while (i < wild.Length)
        {
            if (wild[i] == '[')
            {
                // 特质通配符 [key:value]
                int end = wild.IndexOf(']', i);
                if (end < 0) break;
                tags.Add(wild.Substring(i + 1, end - i - 1));  // 去掉 []
                i = end + 1;
            }
            else if (wild[i] == '(')
            {
                // Unix 通配符 (??) 或 (*)
                int end = wild.IndexOf(')', i);
                if (end < 0) break;
                tags.Add("?" + wild.Substring(i + 1, end - i - 1));  // 加 ? 前缀区分
                i = end + 1;
            }
            else
            {
                // 普通字符（目录分隔符等）
                i++;
            }
        }

        return tags.ToArray();
    }

    // 匹配单个文件是否满足所有标签
    public static bool MatchAll(string filePath, string[] tags)
    {
        string fileName = Path.GetFileName(filePath);
        string fileContent = "";
        string[]? fileLines = null;

        foreach (string tag in tags)
        {
            if (!MatchOne(filePath, fileName, ref fileContent, ref fileLines, tag))
                return false;
        }
        return true;
    }

    // 匹配单个标签
    public static bool MatchOne(string filePath, string fileName,
        ref string fileContent, ref string[]? fileLines, string tag)
    {
        if (tag.StartsWith("?"))
        {
            // Unix 通配符
            string pattern = tag.Substring(1);
            return MatchUnixWildcard(fileName, pattern);
        }

        // 特质通配符
        int colon = tag.IndexOf(':');
        if (colon < 0) return false;

        string key = tag.Substring(0, colon);
        string value = tag.Substring(colon + 1);

        switch (key)
        {
            case "ex":    // 后缀匹配
                return fileName.EndsWith("." + value, StringComparison.OrdinalIgnoreCase);

            case "nhd":   // 文件名开头
                return fileName.StartsWith(value, StringComparison.OrdinalIgnoreCase);

            case "ned":   // 文件名结尾
                return fileName.EndsWith(value, StringComparison.OrdinalIgnoreCase);

            case "nin":   // 文件名包含
                return fileName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

            case "?:":    // 文件名长度
                if (int.TryParse(value, out int len))
                    return Path.GetFileNameWithoutExtension(fileName).Length == len;
                return false;

            case "flnh":  // 文件头内容
                if (fileContent == "") fileContent = ReadFirstBytes(filePath, value.Length);
                return fileContent.StartsWith(value);

            case "fled":  // 文件尾内容
                if (fileContent == "") fileContent = ReadLastBytes(filePath, value.Length);
                return fileContent.EndsWith(value);

            case "fin":   // 文件内容全文搜索
                if (fileContent == "") fileContent = File.ReadAllText(filePath);
                return fileContent.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

            case "fln":   // 某行内容
                // fln(2):XXXX
                int parenOpen = tag.IndexOf('(');
                int parenClose = tag.IndexOf(')');
                if (parenOpen < 0 || parenClose < 0) return false;

                string lineNumStr = tag.Substring(parenOpen + 1, parenClose - parenOpen - 1);
                if (!int.TryParse(lineNumStr, out int lineNum)) return false;

                // 实际值在最后一个 : 后面
                int lastColon = tag.LastIndexOf(':');
                string lineContent = tag.Substring(lastColon + 1);

                if (fileLines == null) fileLines = File.ReadAllLines(filePath);

                if (lineNum >= 0 && lineNum < fileLines.Length)
                    return fileLines[lineNum].IndexOf(lineContent, StringComparison.OrdinalIgnoreCase) >= 0;
                return false;

            case "*":     // 全部文件
                return true;
        }

        return false;
    }

    // Unix 通配符匹配（?=任意一个字符，*=任意多个字符）
    public static bool MatchUnixWildcard(string name, string pattern)
    {
        // 把 Unix 通配符转成正则
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\?", ".")
            .Replace("\\*", ".*") + "$";

        return Regex.IsMatch(name, regexPattern, RegexOptions.IgnoreCase);
    }

    public static string ReadFirstBytes(string path, int count)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[count];
            int read = fs.Read(buffer, 0, count);
            return Encoding.ASCII.GetString(buffer, 0, read);
        }
    }

    public static string ReadLastBytes(string path, int count)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            if (fs.Length < count)
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
                return Encoding.ASCII.GetString(buffer);
            }

            fs.Seek(-count, SeekOrigin.End);
            byte[] buffer2 = new byte[count];
            fs.Read(buffer2, 0, count);
            return Encoding.ASCII.GetString(buffer2);
        }
    }
}
