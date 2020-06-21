using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup
{
    public static class Logger
    {
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
        private static void Log(Action<string> logFunc, string message) => logFunc?.Invoke($"[{nameof(NodeMarkup)}] {message}");
    }
}
