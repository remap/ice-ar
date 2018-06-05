using UnityEngine;
using System;

public interface ILogComponent {
    string getLogComponentName();
    bool isLoggingEnabled();
}

public class Debug
{
    static private string applicationTag_ = "ice-ar-app";

    public static void Init(string applicationTag)
    {
        applicationTag_ = applicationTag;
    }

    #region Exception
    public static void LogException(System.Exception exception)
    {
        ErrorFormat("exception occurred: {0}, stacktrace: {1}", 
                    replaceNewLines(exception.Message),
                    replaceNewLines(exception.StackTrace));
        //UnityEngine.Debug.LogException(exception);
    }

    public static void LogException(object context, System.Exception exception)
    {
        ErrorFormat("exception occurred in {0}: {1}, stacktrace: {2}",
                    context, replaceNewLines(exception.Message),
                    replaceNewLines(exception.StackTrace));
        //UnityEngine.Debug.LogException(exception, context);
    }
    #endregion

    #region Error
    public static void ErrorFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogError(context, message);
    }

    public static void ErrorFormat(ILogComponent component, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogError(component, message);
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
        if (context is ILogComponent && (context as ILogComponent).isLoggingEnabled())
            UnityEngine.Debug.LogError(String.Format("[{0}][{1}] {2}", applicationTag_,
                                                       (context as ILogComponent).getLogComponentName(), message));
        else
            UnityEngine.Debug.LogError(String.Format("[{0}] {1}", applicationTag_, message), context);
    }

    public static void LogError(ILogComponent component, object message)
    {
        if (component.isLoggingEnabled())
            UnityEngine.Debug.LogError(String.Format("[{0}][{1}] {2}", applicationTag_,
                                                       component.getLogComponentName(), message));
    }
    #endregion

    #region Warning
    public static void WarningFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogWarning(context, message);
    }

    public static void WarningFormat(ILogComponent component, string template, params object[] args)
    {
        var message = string.Format(template, args);
        LogWarning(component, message);
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
        if (context is ILogComponent && (context as ILogComponent).isLoggingEnabled())
            UnityEngine.Debug.LogWarning(String.Format("[{0}][{1}] {2}", applicationTag_, 
                                                       (context as ILogComponent).getLogComponentName(), message));
        else
            UnityEngine.Debug.LogWarning(String.Format("[{0}] {1}", applicationTag_, message), context);
    }

    public static void LogWarning(ILogComponent component, object message)
    {
        if (component.isLoggingEnabled())
            UnityEngine.Debug.LogWarning(String.Format("[{0}][{1}] {2}", applicationTag_,
                                                       component.getLogComponentName(), message));
    }
    #endregion

    #region Message
    public static void MessageFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Message(context, message);
    }

    public static void MessageFormat(ILogComponent component, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Message(component, message);
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
        if (context is ILogComponent && (context as ILogComponent).isLoggingEnabled())
            UnityEngine.Debug.LogFormat("[{0}][{1}] {2}", applicationTag_, (context as ILogComponent).getLogComponentName(), message);
        else
            UnityEngine.Debug.LogFormat(context, "[{0}] {1}", applicationTag_, message);
    }

    public static void Message(ILogComponent component, object message)
    {
        if (component.isLoggingEnabled())
            UnityEngine.Debug.LogFormat("[{0}][{1}] {2}", applicationTag_, component.getLogComponentName(), message);
    }
    #endregion

    #region Verbose
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(UnityEngine.Object context, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Log(context, message);
    }

    public static void LogFormat(ILogComponent component, string template, params object[] args)
    {
        var message = string.Format(template, args);
        Log(component, message);
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
        UnityEngine.Debug.LogFormat("[TRACE][{0}] {1}", applicationTag_, message);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(UnityEngine.Object context, object message)
    {
        if (context is ILogComponent && (context as ILogComponent).isLoggingEnabled())
            UnityEngine.Debug.LogFormat("[TRACE][{0}][{1}] {2}", applicationTag_, (context as ILogComponent).getLogComponentName(), message);
        else
            UnityEngine.Debug.LogFormat(context, "[TRACE][{0}] {1}", applicationTag_, message);
    }

    public static void Log(ILogComponent component, object message)
    {
        if (component.isLoggingEnabled())
            UnityEngine.Debug.LogFormat("[TRACE][{0}][{1}] {2}", applicationTag_, component.getLogComponentName(), message);
    }
    #endregion

    private static string replaceNewLines(string s)
    {
        return s.Replace(System.Environment.NewLine, System.Environment.NewLine + "[" + applicationTag_ + "]");
    }
}
