using ICities;
using System;
using System.Text;
using NodeMarkup.Manager;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.IO.Compression;
using HarmonyLib;
using ColossalFramework.Globalization;

namespace NodeMarkup.Utils
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

    public class Serializer : SerializableDataExtensionBase
    {
        static string Id { get; } = nameof(NodeMarkup);
        public override void OnLoadData()
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(OnLoadData)}");

            if (serializableDataManager.LoadData(Id) is byte[] data)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    var decompress = Decompress(data);
#if DEBUG
                    Logger.LogDebug(decompress);
#endif
                    var config = Parse(decompress);
                    MarkupManager.FromXml(config);

                    sw.Stop();
                    Logger.LogDebug($"Data was loaded in {sw.ElapsedMilliseconds}ms; Size = {data.Length} bytes");
                }
                catch(Exception error)
                {
                    Logger.LogError(() => "Could load data", error);
                }
            }
            else
                Logger.LogDebug($"Saved data not founded");
        }
        public static string[] GetImportList()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "MarkingRecovery*.xml");
            return files;
        }
        public static bool OnImportData(string file)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(OnImportData)}");

            try
            {
                using (var fileStream = File.OpenRead(file))
                using (var reader = new StreamReader(fileStream))
                {
                    var xml = reader.ReadToEnd();
                    var config = Parse(xml);

                    MarkupManager.FromXml(config);

                    Logger.LogDebug($"Data was imported");

                    return true;
                }
            }
            catch(Exception error)
            {
                Logger.LogError(() => "Could import data", error);
                return false;
            }
        }

        public override void OnSaveData()
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(OnSaveData)}");

            string xml = string.Empty;
            try
            {
                var sw = Stopwatch.StartNew();

                var config = MarkupManager.ToXml();
                xml = config.ToString(SaveOptions.DisableFormatting);
#if DEBUG
            Logger.LogDebug(xml);
#endif
                var compress = Compress(xml);
                serializableDataManager.SaveData(Id, compress);

                sw.Stop();
                Logger.LogDebug($"Data saved in {sw.ElapsedMilliseconds}ms; Size = {compress.Length} bytes");
            }
            catch(Exception error)
            {
                Logger.LogError(() => "Save data failed", error);
                SaveSettingDump(xml, out _);
                throw;
            }
        }
        public static bool OnDumpData(out string path)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(OnDumpData)}");

            try
            {
                var config = MarkupManager.ToXml();
                var xml = config.ToString(SaveOptions.DisableFormatting);

                return SaveSettingDump(xml, out path);
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }

        private static bool SaveSettingDump(string xml, out string path)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(SaveSettingDump)}");
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
            catch(Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);

                path = string.Empty;
                return false;
            }
        }
        private static string GetSaveName()
        {
            var lastSaveField = AccessTools.Field(typeof(SavePanel), "m_LastSaveName");
            var lastSave = lastSaveField.GetValue(null) as string;
            if(string.IsNullOrEmpty(lastSave))
                lastSave = Locale.Get("DEFAULTSAVENAMES", "NewSave");

            return lastSave;
        }

        public static XElement Parse(string text, LoadOptions options = LoadOptions.None)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(Parse)}");

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

        static byte[] Compress(string xml)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(Compress)}");

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

        static string Decompress(byte[] data)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(Decompress)}");

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
    }
}
