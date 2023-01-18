using ColossalFramework.Plugins;

using System;

namespace NodeMarkup.API
{
	public static class Helper
	{
		private static IDataProviderFactory factory;

		private static IDataProviderFactory GetFactory()
		{
			if (factory != null)
			{
				return factory;
			}

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (var type in assembly.GetExportedTypes())
					{
						if (type.IsClass && typeof(IDataProviderFactory).IsAssignableFrom(type))
						{
							var plugin = PluginManager.instance.FindPluginInfo(type.Assembly);

							if (plugin != null && plugin.isEnabled)
							{
								factory = (IDataProviderFactory)Activator.CreateInstance(type);

								return factory;
							}
						}
					}
				}
				catch { }
			}

			return null;
		}

		public static IDataProviderV1 GetProviderV1()
		{
			return GetFactory()?.GetProviderV1();
		}
	}
}
