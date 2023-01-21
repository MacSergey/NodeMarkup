using ColossalFramework.Plugins;

using System;

namespace IMT.API
{
    public static class Helper
    {
        public static IDataProviderV1 GetProvider(string id)
        {
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
                                return provider;
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
