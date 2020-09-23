using MoveItIntegration;
using System;
using System.Collections.Generic;
using NodeMarkup.Manager;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Linq;

namespace NodeMarkup.Utils
{
    public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    {
        public string Name => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public MoveItIntegrationBase GetInstance() => new MoveItIntegration();
    }

    public class MoveItIntegration : MoveItIntegrationBase
    {
        public override string ID => "CS.macsergey.NodeMarkup";

        public override string Name => Mod.StaticName;

        public override string Description => Localize.Mod_Description;

        public override Version DataVersion => new Version(1, 0);

        public override object Copy(InstanceID sourceInstanceID)
        {
            if (sourceInstanceID.Type == InstanceType.NetNode)
            {
                ushort nodeID = sourceInstanceID.NetNode;
                if (MarkupManager.TryGetMarkup(nodeID, out Markup markup))
                {
                    var data = markup.ToXml();
                    return data;
                }
                else
                    return null;
            }
            return null;
        }

        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap)
            => Paste(targetInstanceID, record, sourceMap, PasteMapFiller);
        private void PasteMapFiller(Markup markup, PasteMap map, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            foreach (var source in sourceMap.Where(p => IsCorrect(p)))
                map[new ObjectId() { Segment = source.Key.NetSegment }] = new ObjectId() { Segment = source.Value.NetSegment };
        }

        public override void Mirror(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap)
            => Paste(targetInstanceID, record, sourceMap, MirrorMapFiller);
        private void MirrorMapFiller(Markup markup, PasteMap map, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            foreach (var source in sourceMap.Where(p => IsCorrect(p)))
            {
                if (!markup.TryGetEnter(source.Value.NetSegment, out Enter enter))
                    continue;

                var sourceSegment = source.Key.NetSegment;
                var targetSetment = source.Value.NetSegment;
                map[new ObjectId() { Segment = sourceSegment }] = new ObjectId() { Segment = targetSetment };
                var count = enter.PointCount + 1;
                for (var i = 1; i < count; i += 1)
                {
                    foreach (var pointType in Enum.GetValues(typeof(MarkupPoint.PointType)).OfType<MarkupPoint.PointType>())
                    {
                        var sourcePoint = new ObjectId() { Point = MarkupPoint.GetId(targetSetment, (byte)i, pointType) };
                        var targetPoint = new ObjectId() { Point = MarkupPoint.GetId(targetSetment, (byte)(count - i), pointType) };
                        map[sourcePoint] = targetPoint;

                    }
                }
            }
        }

        private void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap, Action<Markup, PasteMap, Dictionary<InstanceID, InstanceID>> mapFiller)
        {
            if (targetInstanceID.Type != InstanceType.NetNode || !(record is XElement config))
                return;

            ushort nodeID = targetInstanceID.NetNode;
            var map = new PasteMap(true);
            var markup = MarkupManager.Get(nodeID);
            mapFiller(markup, map, sourceMap);
            markup.FromXml(Mod.Version, config, map);
        }
        private bool IsCorrect(KeyValuePair<InstanceID, InstanceID> pair) => pair.Key.Type == InstanceType.NetSegment && pair.Value.Type == InstanceType.NetSegment;


        public override string Encode64(object record)
        {
            if (record == null) return null;
            return EncodeUtil.BinaryEncode64(record.ToString());
        }

        public override object Decode64(string record, Version dataVersion)
        {
            if (record == null || record.Length == 0) return null;

            // XElement.Parse throws MissingMethodException
            // Method not found: System.Xml.XmlReaderSettings.set_MaxCharactersFromEntities
            XElement xml;
            using (StringReader input = new StringReader((string)EncodeUtil.BinaryDecode64(record)))
            {
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    ProhibitDtd = false,
                    XmlResolver = null
                };
                using (XmlReader reader = XmlReader.Create(input, xmlReaderSettings))
                {
                    xml = XElement.Load(reader, LoadOptions.None);
                }
            }
            return xml;
        }
    }
}
