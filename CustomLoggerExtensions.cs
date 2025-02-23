using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public static class CustomLoggerExtensions
{
    private static readonly ConditionalWeakTable<string, Stopwatch> _stopwatches = new();
     public static void LogStartExecutionTime(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
        var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
        //var key = new ExecutionKey(className, method);
        var key = className + method;

        var stopwatch = _stopwatches.Where(x=>x.Key.Contains(key)).FirstOrDefault().Value;
        if (stopwatch != null)
        {
            logger.LogWarning($"Execution timer for {methodName} in {filePath} was already started.");
            return;
        }

        stopwatch = Stopwatch.StartNew();
        _stopwatches.Add(key, stopwatch);
        logger.Log(logLevel, $"Started {methodName} in {filePath}: {message}");
    }

    public static void LogExecutionTime(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
         var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
       // var key = new ExecutionKey(className, method);
       var key = className + method;
        var stopwatch = _stopwatches.Where(x=>x.Key.Contains(key)).FirstOrDefault().Value;
        if (stopwatch == null)
        {
            logger.LogWarning($"No execution timer found for {methodName} in {filePath}. Call LogStartExecutionTime first.");
            return;
        }

        stopwatch.Stop();
        _stopwatches.Remove(key);
        logger.Log(logLevel, $"Completed {methodName} in {filePath}: {message} | Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }
public static void LogInformation(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
       var activity = Activity.Current;
        var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
      
        logger.LogInformation(className, method);
    }
     private static string GetClassName(string filePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
     private static string GetFullMethodName(string input)
    {
        // var stackTrace = new StackTrace();
        // var frame = stackTrace.GetFrame(2); // Adjust the frame index as needed
        // var method = frame?.GetMethod();
        // return method.Name;
        return Regex.Replace(input, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }
     private class ExecutionKey
    {
        public string ClassName { get; }
        public string MethodName { get; }

        public ExecutionKey(string className, string methodName)
        {
            ClassName = className;
            MethodName = methodName;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExecutionKey other)
            {
                return ClassName == other.ClassName && MethodName == other.MethodName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassName, MethodName);
        }
    }

}
