using System.Text;

namespace OnvifDeviceManager.Services;

public static class CrashLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OnvifDeviceManager", "logs");

    private static readonly string LogFile = Path.Combine(LogDir, "crash.log");

    static CrashLogger()
    {
        try { Directory.CreateDirectory(LogDir); } catch { }
    }

    public static string LogFilePath => LogFile;

    /// <summary>Short multi-line summary for dialogs (empty <see cref="Exception.Message"/>, inner chain, first stack line).</summary>
    public static string FormatExceptionForUser(Exception ex)
    {
        var sb = new StringBuilder();
        var cur = ex;
        for (var i = 0; cur != null && i < 4; i++)
        {
            var msg = string.IsNullOrWhiteSpace(cur.Message) ? $"({cur.GetType().Name})" : cur.Message;
            if (i == 0)
                sb.AppendLine(msg);
            else
                sb.AppendLine($"  → {cur.GetType().Name}: {msg}");
            cur = cur.InnerException;
        }

        var stack = ex.StackTrace;
        if (!string.IsNullOrWhiteSpace(stack))
        {
            var line = stack.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(line))
                sb.AppendLine().Append("At: ").Append(line);
        }

        return sb.ToString().TrimEnd();
    }

    public static void Log(string context, Exception ex)
    {
        try
        {
            var entry = $"""
                === {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===
                Context: {context}
                Type: {ex.GetType().FullName}
                Message: {ex.Message}
                Stack: {ex.StackTrace}
                {(ex.InnerException != null ? $"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\nInner Stack: {ex.InnerException.StackTrace}" : "")}
                
                """;
            File.AppendAllText(LogFile, entry);

            // Keep log file under 1MB
            var info = new FileInfo(LogFile);
            if (info.Exists && info.Length > 1_000_000)
            {
                var lines = File.ReadAllLines(LogFile);
                File.WriteAllLines(LogFile, lines.Skip(lines.Length / 2));
            }
        }
        catch { }
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }
}
