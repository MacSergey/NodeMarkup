using ColossalFramework.Globalization;
using HarmonyLib;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NodeMarkup
{
    public interface IXml
    {
        string XmlSection { get; }
    }
    public interface IToXml : IXml
    {
        XElement ToXml();
    }
    public interface IFromXml : IXml
    {
        void FromXml(XElement config);
    }
    public static class Loader
    {
        public static string Id { get; } = nameof(NodeMarkup);

        public static XElement Parse(string text, LoadOptions options = LoadOptions.None)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(Parse)}");

            using StringReader input = new StringReader(text);
            XmlReaderSettings xmlReaderSettings = GetXmlReaderSettings(options);
            using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
            return XElement.Load(reader, options);
        }

        static XmlReaderSettings GetXmlReaderSettings(LoadOptions o)
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            if ((o & LoadOptions.PreserveWhitespace) == 0)
            {
                xmlReaderSettings.IgnoreWhitespace = true;
            }
            xmlReaderSettings.ProhibitDtd = false;
            xmlReaderSettings.XmlResolver = null;
            return xmlReaderSettings;
        }

        public static byte[] Compress(string xml)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(Compress)}");

            var buffer = Encoding.UTF8.GetBytes(xml);

            using var outStream = new MemoryStream();
            using (var zipStream = new GZipStream(outStream, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
            }
            var compresed = outStream.ToArray();
            return compresed;
        }

        public static string Decompress(byte[] data)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(Decompress)}");

            using var inStream = new MemoryStream(data);
            using var zipStream = new GZipStream(inStream, CompressionMode.Decompress);
            using var outStream = new MemoryStream();
            byte[] buffer = new byte[1000000];
            int readed;
            while ((readed = zipStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outStream.Write(buffer, 0, readed);
            }

            var decompressed = outStream.ToArray();
            var xml = Encoding.UTF8.GetString(decompressed);
            return xml;
        }

        public static string GetSaveName()
        {
            var lastSaveField = AccessTools.Field(typeof(SavePanel), "m_LastSaveName");
            var lastSave = lastSaveField.GetValue(null) as string;
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Locale.Get("DEFAULTSAVENAMES", "NewSave");

            return lastSave;
        }
        public static string MarkingRecovery => nameof(MarkingRecovery);
        public static string TemplatesRecovery => nameof(TemplatesRecovery);

        public static string MarkingName => $"{MarkingRecovery}.{GetSaveName()}";
        private static Regex MarkingRegex { get; } = new Regex(@$"{MarkingRecovery}\.(?<name>.+)\.(?<date>\d+)");
        private static Regex TemplatesRegex { get; } = new Regex(@$"{TemplatesRecovery}\.(?<date>\d+)");
        private static string RecoveryDirectory => Path.Combine(Directory.GetCurrentDirectory(), "IntersectionMarkingTool");

        public static Dictionary<string, string> GetMarkingRestoreList()
        {
            var files = GetRestoreList($"{MarkingRecovery}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = MarkingRegex.Match(file);
                if (!match.Success)
                    continue;

                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                result[file] = $"{match.Groups["name"].Value} {date}";
            }
            return result;
        }
        public static Dictionary<string, string> GetTemplatesRestoreList()
        {
            var files = GetRestoreList($"{TemplatesRecovery}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = TemplatesRegex.Match(file);
                if (!match.Success)
                    continue;

                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                result[file] = date.ToString();
            }
            return result;
        }
        private static string[] GetRestoreList(string pattern)
        {
            var files = Directory.GetFiles(RecoveryDirectory, pattern);
            return files;
        }

        public static bool ImportMarkingData(string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportMarkingData)}");
            return ImportData(file, (config) => MarkupManager.Import(config));
        }
        public static bool ImportTemplatesData(string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportTemplatesData)}");
            return ImportData(file, (config) =>
            {
                Settings.Templates.value = config.ToString(SaveOptions.DisableFormatting);
                TemplateManager.Load();
            });
        }
        private static bool ImportData(string file, Action<XElement> processData)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportData)}");

            try
            {
                using var fileStream = File.OpenRead(file);
                using var reader = new StreamReader(fileStream);
                var xml = reader.ReadToEnd();
                var config = Parse(xml);

                processData(config);

                Logger.LogDebug($"Data was imported");

                return true;
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could import data", error);
                return false;
            }
        }
        public static bool DumpMarkingData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpMarkingData)}");
            return DumpData(MarkupManager.ToXml().ToString(SaveOptions.DisableFormatting), MarkingName, out path);
        }
        public static bool DumpTemplatesData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpTemplatesData)}");
            return DumpData(Settings.Templates, TemplatesRecovery, out path);
        }
        private static bool DumpData(string data, string name, out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpData)}");

            try
            {
                return SaveToFile(name, data, out path);
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }

        public static bool SaveToFile(string name, string xml, out string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(SaveToFile)}");
            try
            {
                if (!Directory.Exists(RecoveryDirectory))
                    Directory.CreateDirectory(RecoveryDirectory);

                file = Path.Combine(RecoveryDirectory, $"{name}.{DateTime.Now.Ticks}.xml");
                using (var fileStream = File.Create(file))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(xml);
                }
                Logger.LogDebug($"Dump saved {file}");
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                file = string.Empty;
                return false;
            }
        }
    }
}
