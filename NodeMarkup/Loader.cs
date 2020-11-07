using ColossalFramework.Globalization;
using ColossalFramework.Importers;
using ColossalFramework.IO;
using ColossalFramework.Packaging;
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
using UnityEngine;

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
        public static string GetString(XElement config) => config.ToString(SaveOptions.DisableFormatting);

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
        public static string PresetsRecovery => nameof(PresetsRecovery);

        public static string MarkingName => $"{MarkingRecovery}.{GetSaveName()}";
        private static Regex MarkingRegex { get; } = new Regex(@$"{MarkingRecovery}\.(?<name>.+)\.(?<date>\d+)");
        private static Regex TemplatesRegex { get; } = new Regex(@$"{TemplatesRecovery}\.(?<date>\d+)");
        private static Regex PresetsRegex { get; } = new Regex(@$"{PresetsRecovery}\.(?<date>\d+)");
        private static string RecoveryDirectory => Path.Combine(Directory.GetCurrentDirectory(), "IntersectionMarkingTool");
        private static string ScreenshotDirectory => Path.Combine(RecoveryDirectory, "TemplateScreenshots");

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
        public static Dictionary<string, string> GetStylesRestoreList() => GetTemplatesRestoreList(TemplatesRecovery, TemplatesRegex);
        public static Dictionary<string, string> GetIntersectionsRestoreList() => GetTemplatesRestoreList(PresetsRecovery, PresetsRegex);

        private static Dictionary<string, string> GetTemplatesRestoreList(string name, Regex regex)
        {
            var files = GetRestoreList($"{name}*.xml");
            var result = new Dictionary<string, string>();
            foreach (var file in files)
            {
                var match = regex.Match(file);
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
        public static bool ImportStylesData(string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportStylesData)}");
            return ImportTemplatesData(file, TemplateManager.StyleManager);
        }
        public static bool ImportIntersectionsData(string file)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(ImportIntersectionsData)}");
            return ImportTemplatesData(file, TemplateManager.IntersectionManager);
        }
        private static bool ImportTemplatesData(string file, TemplateManager manager)
        {
            return ImportData(file, (config) =>
            {
                manager.Saved.value = GetString(config);
                manager.Load();
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
                Logger.LogError("Could not import data", error);
                return false;
            }
        }
        public static bool DumpMarkingData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpMarkingData)}");
            return DumpData(GetString(MarkupManager.ToXml()), MarkingName, out path);
        }
        public static bool DumpTemplatesData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpTemplatesData)}");
            return DumpData(Settings.Templates, TemplatesRecovery, out path);
        }
        public static bool DumpPresetsData(out string path)
        {
            Logger.LogDebug($"{nameof(Loader)}.{nameof(DumpPresetsData)}");
            return DumpData(Settings.Intersections, PresetsRecovery, out path);
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
                Logger.LogError("Save dump failed", error);

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
                Logger.LogError("Save dump failed", error);

                file = string.Empty;
                return false;
            }
        }

        public static void LoadAsset(GameObject gameObject, Package.Asset asset)
        {
            try
            {
                if (!(gameObject.GetComponent<MarkingInfo>() is MarkingInfo markingInfo))
                    return;

                var templateConfig = Parse(markingInfo.data);
                if (TemplateAsset.FromPackage(templateConfig, asset, out TemplateAsset templateAsset))
                {
                    if (templateAsset.NeedLoadPreview && asset.package.Find(templateAsset.MetaPreview) is Package.Asset assetPreview && assetPreview.Instantiate<Texture>() is Texture2D preview)
                        templateAsset.Template.Preview = preview;

                    TemplateManager.AddAssetTemplate(templateAsset);
                }
                Logger.LogDebug($"Asset loaded: {templateAsset}");
            }
            catch (Exception error)
            {
                Logger.LogError($"Could not load asset: {asset.fullName}", error);
            }
        }
        public static bool SaveAsset(TemplateAsset templateAsset)
        {
            try
            {
                var meta = new CustomAssetMetaData()
                {
                    name = templateAsset.Template.Name,
                    timeStamp = DateTime.Now,
                    type = CustomAssetMetaData.Type.Unknown,
                    dlcMask = SteamHelper.DLC_BitMask.None,
                    steamTags = new string[] { "Marking" },
                    guid = templateAsset.Template.Id.ToString(),
                };

                var package = new Package(meta.name)
                {
                    packageMainAsset = meta.name,
                    packageAuthor = $"steamid:{templateAsset.AuthorId}"
                };

                var gameObject = new GameObject(typeof(MarkingInfo).Name);
                var markingInfo = gameObject.AddComponent<MarkingInfo>();
                markingInfo.data = GetString(templateAsset.Template.ToXml());
                meta.assetRef = package.AddAsset(templateAsset.MetaData, markingInfo.gameObject);

                if (templateAsset.Preview is Image image)
                    meta.imageRef = package.AddAsset(templateAsset.MetaPreview, image, false, Image.BufferFileFormat.PNG, false, false);

                if (templateAsset.SeparatePreview && templateAsset.SteamPreview is Image steamImage)
                    meta.steamPreviewRef = package.AddAsset(templateAsset.MetaSteamPreview, steamImage, false, Image.BufferFileFormat.PNG, false, false);
                else
                    meta.steamPreviewRef = meta.imageRef;

                package.AddAsset(meta.name, meta, UserAssetType.CustomAssetMetaData);

                var path = GetSavePathName(templateAsset.FileName);
                package.Save(path);

                Logger.LogDebug($"Asset {templateAsset} saved to {path}");

                return true;
            }
            catch (Exception error)
            {
                Logger.LogError($"Could not save asset {templateAsset}", error);
                return false;
            }
        }
        public static string GetSavePathName(string saveName)
        {
            string path = PathUtils.AddExtension(PathEscaper.Escape(saveName), PackageManager.packageExtension);
            return Path.Combine(DataLocation.assetsPath, path);
        }

        public static bool SaveScreenshot(Image image, Guid id)
        {
            try
            {
                var data = image.GetFormattedImage(Image.BufferFileFormat.PNG);
                var path = Path.Combine(ScreenshotDirectory, $"{id}.png");
                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError($"Could not save screenshot {id}", error);
                return false;
            }
        }
        public static bool LoadScreenshot(Guid id, out Image image)
        {
            try
            {
                if (!Directory.Exists(ScreenshotDirectory))
                    Directory.CreateDirectory(ScreenshotDirectory);

                var path = Path.Combine(ScreenshotDirectory, $"{id}.png");
                var data = File.ReadAllBytes(path);
                image = new Image(data);
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError($"Could not load screenshot {id}", error);
                image = null;
                return false;
            }

        }
    }
}
