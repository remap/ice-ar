using UnityEngine;
using System;

public class Debug
{
    static private string applicationTag_ = "ice-ar-app";

    public static void Init(string applicationTag)
    {
        applicationTag_ = applicationTag;
    }

    #region Error
    public static void ErrorFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogError(context, message);
    }

    public static void ErrorFormat(string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogError(message);
    }

    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(String.Format("[{0}] {1}", applicationTag_, message));
    }

    public static void LogError(UnityEngine.Object context, object message)
    {
        UnityEngine.Debug.LogError(String.Format("[{0}] {1}", applicationTag_, message), context);
    }
    #endregion

    #region Warning
    public static void WarningFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogWarning(context, message);
    }

    public static void WarningFormat(string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogWarning(message);
    }

    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(String.Format("[{0}] {1}", applicationTag_, message));
    }

    public static void LogWarning(UnityEngine.Object context, object message)
    {
        UnityEngine.Debug.LogWarning(String.Format("[{0}] {1}", applicationTag_, message), context);
    }
    #endregion

    #region Message
    public static void MessageFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Message(context, message);
    }

    public static void MessageFormat(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Message(message);
    }

    public static void Message(object message)
    {
        UnityEngine.Debug.LogFormat("[{0}] {1}", applicationTag_, message);
    }

    public static void Message(UnityEngine.Object context, object message)
    {
        UnityEngine.Debug.LogFormat(context, "[{0}] {1}", applicationTag_, message);
    }
    #endregion

    #region Verbose
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Log(context, message);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Log(message);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        UnityEngine.Debug.LogFormat("[{0}][TRACE] {1}", applicationTag_, message);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(UnityEngine.Object context, object message)
    {
        UnityEngine.Debug.LogFormat(context, "[{0}][TRACE] {1}", applicationTag_, message);
    }
    #endregion

}
