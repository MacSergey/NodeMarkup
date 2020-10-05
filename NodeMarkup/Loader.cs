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

            using (StringReader input = new StringReader(text))
            {
                XmlReaderSettings xmlReaderSettings = GetXmlReaderSettings(options);
                using (XmlReader reader = XmlReader.Create(input, xmlReaderSettings))
                {
                    return XElement.Load(reader, options);
                }
            }
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

            using (var outStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(outStream, CompressionMode.Compress))
                {
                    zipStream.Write(buffer, 0, buffer.Length);
                }
                var compresed = outStream.ToArray();
                return compresed;
            }
        }

        public static string Decompress(byte[] data)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(Decompress)}");

            using (var inStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
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
        }

        public static string GetSaveName()
        {
            var lastSaveField = AccessTools.Field(typeof(SavePanel), "m_LastSaveName");
            var lastSave = lastSaveField.GetValue(null) as string;
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Locale.Get("DEFAULTSAVENAMES", "NewSave");

            return lastSave;
        }
        public static string[] GetImportList()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "MarkingRecovery*.xml");
            return files;
        }

        public static bool ImportData(string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportData)}");

            try
            {
                using (var fileStream = File.OpenRead(file))
                using (var reader = new StreamReader(fileStream))
                {
                    var xml = reader.ReadToEnd();
                    var config = Parse(xml);

                    MarkupManager.FromXml(config, new ObjectsMap());

                    Logger.LogDebug($"Data was imported");

                    return true;
                }
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could import data", error);
                return false;
            }
        }
        public static bool DumpData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpData)}");

            try
            {
                var config = MarkupManager.ToXml();
                var xml = config.ToString(SaveOptions.DisableFormatting);

                return SaveToFile(xml, out path);
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }
        public static bool SaveToFile(string xml, out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(SaveToFile)}");
            try
            {
                var file = $"MarkingRecovery.{GetSaveName()}.{DateTime.Now.Ticks}.xml";
                using (var fileStream = File.Create(file))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(xml);
                }

                path = Path.Combine(Directory.GetCurrentDirectory(), file);
                Logger.LogDebug($"Dump saved {path}");
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }
    }
}
