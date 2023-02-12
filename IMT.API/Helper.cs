using ColossalFramework.Plugins;

using System;
using System.Reflection;
using UnityEngine;

namespace IMT.API
{
    public static class Helper
    {
        private static ILogHandler Handle { get; } = UnityEngine.Debug.logger.logHandler;

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
        private static Version Version => Assembly.GetName().Version;

        public static IDataProviderV1 GetProvider(string id)
        {
            Debug($"Trying to create provider for \"{id}\"");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (type.IsClass && typeof(IDataProviderFactory).IsAssignableFrom(type))
                        {
                            var plugin = PluginManager.instance.FindPluginInfo(type.Assembly);
                            if (plugin != null && plugin.isEnabled)
                            {
                                var factory = (IDataProviderFactory)Activator.CreateInstance(type);
                                var provider = factory.GetProvider(id);
                                Debug($"Provider for \"{id}\" was found: {provider.ModVersion} [{(provider.IsBeta ? "Beta" : "Stable")}]");
                                return provider;
                            }
                        }
                    }
                }
                catch { }
            }

            Debug($"Provider for \"{id}\" was not found");
            return null;
        }

        private static void Debug(string message) => Handle.LogFormat(LogType.Log, null, "[{0}][{1}] {2}", $"IMT.API {Version}", Time.realtimeSinceStartup, message);
    }
}
