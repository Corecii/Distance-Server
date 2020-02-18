using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class Log
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
    }

    public static LogLevel ShownLogLevel = LogLevel.Debug;

    public static void WriteLine(LogLevel logLevel, object toWrite)
    {
        if ((int)logLevel < (int)ShownLogLevel)
        {
            return;
        }
        string text = "null";
        if (toWrite != null)
        {
            text = toWrite.ToString();
        }
        UnityEngine.Debug.Log(text);
        System.Console.WriteLine(text);
    }
    public static void WriteLine(object toWrite)
    {
        WriteLine(LogLevel.Debug, toWrite);
    }

    public static void Debug(object toWrite)
    {
        WriteLine(LogLevel.Debug, toWrite);
    }

    static HashSet<string> disabledDebugLines = new HashSet<string>();
    public static void SetDebugLineEnabled(string context, bool enabled)
    {
        if (!enabled)
        {
            disabledDebugLines.Add(context);
        }
        else
        {
            disabledDebugLines.Remove(context);
        }
    }

    static Dictionary<string, int> debugLineValues = new Dictionary<string, int>();
    public static void DebugLine(string context, int line)
    {
        debugLineValues[context] = line;
        if (disabledDebugLines.Contains(context))
        {
            return;
        }
        Debug($"DBG LN : {context} : {line}");
    }

    public static void DebugLine(string context)
    {
        if (!debugLineValues.ContainsKey(context))
        {
            debugLineValues[context] = 0;
        }
        debugLineValues[context]++;
        if (disabledDebugLines.Contains(context))
        {
            return;
        }
        Debug($"DBG LN : {context} : {debugLineValues[context]}");
    }

    public static void Info(object toWrite)
    {
        WriteLine(LogLevel.Info, toWrite);
    }

    public static void Warn(object toWrite)
    {
        WriteLine(LogLevel.Warn, toWrite);
    }

    public static void Error(object toWrite)
    {
        WriteLine(LogLevel.Error, toWrite);
    }
}
