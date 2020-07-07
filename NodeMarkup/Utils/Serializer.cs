using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodeMarkup.Manager;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.IO.Compression;
using ColossalFramework.IO;
using System.Linq.Expressions;

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
                    Manager.MarkupManager.FromXml(config);

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
                SaveSettingDump(xml);
                throw;
            }
        }
        private void SaveSettingDump(string xml)
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(SaveSettingDump)}");
            try
            {
                var file = $"MarkingRecovery{DateTime.Now.Ticks}.bak";
                using (var fileStream = File.Create(file))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(xml);
                }
                Logger.LogDebug($"Dump saved {file}");
            }
            catch(Exception error)
            {
                Logger.LogError(() => "Save dump failed", error);
            }
        }

        public static XElement Parse(string text, LoadOptions options = LoadOptions.None)
        {
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
