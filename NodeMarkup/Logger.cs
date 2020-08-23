using System;
using System.IO;
using UnityEngine;

namespace NodeMarkup
{
    public static class Logger
    {
        static string LogFile { get; } = Path.Combine(Application.dataPath, $"{nameof(NodeMarkup)}.log");
        static Logger()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    File.Delete(LogFile);
                }
                File.Create(LogFile);
            }
            catch { }
        }
        public static bool Enable { get; set; } = true;
        public static bool EnableDebug { get; set; } = true;

        public static void LogDebug(string message)
        {
            if (EnableDebug)
                Log(Debug.Log, message);
        }
        public static void LogDebug(Func<string> message)
        {
            if (EnableDebug)
                Log(Debug.Log, message);
        }
        public static void LogInfo(Func<string> message) => Log(Debug.Log, message);
        public static void LogWarning(Func<string> message) => Log(Debug.LogWarning, message);


        public static void LogError(Func<string> message = null, Exception error = null) => Log(Debug.LogError, error == null ? message : () => $"{message?.Invoke()}\n{error.Message}\n{error.StackTrace}");

        private static void Log(Action<string> logFunc, Func<string> message)
        {
            if (Enable)
                Log(logFunc, message?.Invoke());
        }
        private static void Log(Action<string> logFunc, string message)
        {
            //try
            //{
            //    using (StreamWriter w = File.AppendText(LogFile))
            //    {
            //        w.WriteLine(message);
            //    }
            //}
            //catch { }

            logFunc?.Invoke($"[{nameof(NodeMarkup)}] {message}");
        }
    }
}
