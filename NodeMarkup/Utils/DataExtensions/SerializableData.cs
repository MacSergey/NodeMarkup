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
using NodeMarkup.UI;
using HarmonyLib;
using ColossalFramework.Globalization;

namespace NodeMarkup.Utils
{
    public class SerializableDataExtension : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
            Logger.LogDebug($"{nameof(SerializableDataExtension)}.{nameof(OnLoadData)}");

            if (serializableDataManager.LoadData(Loader.Id) is byte[] data)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    var decompress = Loader.Decompress(data);
#if DEBUG
                    Logger.LogDebug(decompress);
#endif
                    var config = Loader.Parse(decompress);
                    MarkupManager.FromXml(config, new ObjectsMap());

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
            Logger.LogDebug($"{nameof(SerializableDataExtension)}.{nameof(OnSaveData)}");

            string xml = string.Empty;
            try
            {
                var sw = Stopwatch.StartNew();

                var config = MarkupManager.ToXml();
                xml = config.ToString(SaveOptions.DisableFormatting);
#if DEBUG
            Logger.LogDebug(xml);
#endif
                var compress = Loader.Compress(xml);
                serializableDataManager.SaveData(Loader.Id, compress);

                sw.Stop();
                Logger.LogDebug($"Data saved in {sw.ElapsedMilliseconds}ms; Size = {compress.Length} bytes");
            }
            catch(Exception error)
            {
                Logger.LogError(() => "Save data failed", error);
                Loader.SaveToFile(Loader.MarkingName, xml, out _);
                throw;
            }
        }
    }
}
