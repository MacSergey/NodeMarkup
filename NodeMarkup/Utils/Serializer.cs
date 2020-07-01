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
                var sw = Stopwatch.StartNew();

                var xml = Encoding.UTF8.GetString(data);
                Logger.LogDebug(xml);

                var config = Parse(xml);
                Manager.Manager.FromXml(config);

                sw.Stop();
                Logger.LogDebug($"Data was loaded in {sw.ElapsedMilliseconds}");
            }
            else
            {
                Logger.LogDebug($"Saved data not founded");
            }
        }

        public override void OnSaveData()
        {
            Logger.LogDebug($"{nameof(Serializer)}.{nameof(OnSaveData)}");

            var config = Manager.Manager.ToXml();
            var xml = config.ToString();
            Logger.LogDebug(xml);

            var data = Encoding.UTF8.GetBytes(xml);
            serializableDataManager.SaveData(Id, data);

            Logger.LogDebug($"Data saved; Size = {data.Length} bytes");
        }

        XElement Parse(string text, LoadOptions options = LoadOptions.None)
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
        XmlReaderSettings GetXmlReaderSettings(LoadOptions o)
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
    }
}
