using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup
{
    public static class Logger
    {
        public static void LogDebug(string message) => Log(Debug.Log, message);
        public static void LogError(string message, Exception error = null) => Log(Debug.LogError, error == null ? message : $"{message}\n{error.Message}\n{error.StackTrace}");

        private static void Log(Action<string> logFunc, string message) => logFunc($"[{nameof(NodeMarkup)}] {message}");
    }
}
