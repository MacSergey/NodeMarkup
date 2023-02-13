using ColossalFramework.Plugins;
using IMT.API.Proxy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace IMT.API
{
    public static class Helper
    {
        private static MethodInfo ProviderGetter { get; set; }
        private static ILogHandler Handle { get; } = UnityEngine.Debug.logger.logHandler;

        private static Assembly Assembly => Assembly.GetExecutingAssembly();
        public static Version Version
        {
            get
            {
                var attributes = Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true);
                if (attributes != null && attributes.Length > 0 && attributes[0] is AssemblyInformationalVersionAttribute info)
                    return new Version(info.InformationalVersion);
                else
                    return Assembly.GetName().Version;
            }
        }

        private static MethodInfo GetProviderGetter()
        {
            var newestVersion = Version;
            MethodInfo providerGetter = null;
            var typeSignature = typeof(Helper).FullName;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (type.FullName != typeSignature)
                            continue;

                        var versionProperty = type.GetProperty(nameof(Version));
                        if (versionProperty == null)
                            continue;

                        if (versionProperty.PropertyType != typeof(Version) || !versionProperty.CanRead)
                            continue;

                        var version = (Version)versionProperty.GetGetMethod().Invoke(null, new object[0]);
                        if (version < newestVersion)
                            continue;

                        var method = type.GetMethod(nameof(GetProviderImpl), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, new ParameterModifier[0]);
                        if (method == null)
                            continue;

                        if (!typeof(IDataProviderV1).IsAssignableFrom(method.ReturnType))
                            continue;

                        newestVersion = version;
                        providerGetter = method;
                    }
                }
                catch { }
            }

            return providerGetter;
        }

        public static IDataProviderV1 GetProvider(string id)
        {
            if ((ProviderGetter ??= GetProviderGetter()) is MethodInfo providerGetter)
                return (IDataProviderV1)providerGetter.Invoke(null, new object[] { id });
            else
                return GetProviderImpl(id);
        }
        private static IDataProviderV1 GetProviderImpl(string id)
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
                                //return provider;
                                return new ProxyDataProvider(provider, Assembly);
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

        private static Dictionary<Type, Dictionary<string, MethodInfo>> Properties { get; } = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        private static Dictionary<Type, Dictionary<string, MethodInfo>> Methods { get; } = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        internal static object GetPropertyValue(object source, string name)
        {
            var type = source.GetType();
            if (!Properties.TryGetValue(type, out var typeProperties))
            {
                typeProperties = new Dictionary<string, MethodInfo>();
                Properties[type] = typeProperties;
            }
            if(!typeProperties.TryGetValue(name, out var property))
            {
                property = type.GetProperty(name).GetGetMethod();
                typeProperties[name] = property;
            }
            var value = property?.Invoke(source, new Type[0]);
            return value;
        }
        internal static object InvokeMethod(object source, string name, Type[] parameterTypes, object[] parameters)
        {
            var type = source.GetType();
            if (!Methods.TryGetValue(type, out var typeMethods))
            {
                typeMethods = new Dictionary<string, MethodInfo>();
                Methods[type] = typeMethods;
            }
            if (!typeMethods.TryGetValue(name, out var method))
            {
                method = type.GetMethod(name, parameterTypes);
                typeMethods[name] = method;
            }
            var value = method?.Invoke(source, parameters);
            return value;
        }
        internal static Type ConvertType(Type type, Assembly assembly)
        {
            var convertedType = assembly.GetType(type.FullName);
            return convertedType;
        }
    }

    internal class ProviderBinder : Binder
    {
        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        {
            throw new NotImplementedException();
        }

        public override object ChangeType(object value, Type type, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override void ReorderArgumentArray(ref object[] args, object state)
        {
            throw new NotImplementedException();
        }

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }
    }
}
