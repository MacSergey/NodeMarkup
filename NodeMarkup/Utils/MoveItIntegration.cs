using MoveItIntegration;
using System;
using System.Collections.Generic;
using IMT.Manager;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Linq;

namespace IMT.Utils
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

        public override string Name => Mod.ShortName;

        public override string Description => Localize.Mod_Description;

        public override Version DataVersion => new Version(1, 0);

        public override object Copy(InstanceID sourceInstanceID) => sourceInstanceID.Type switch
        {
            InstanceType.NetNode when MarkupManager.NodeManager.TryGetMarkup(sourceInstanceID.NetNode, out NodeMarkup nodeMarkup) => nodeMarkup.ToXml(),
            InstanceType.NetSegment when MarkupManager.SegmentManager.TryGetMarkup(sourceInstanceID.NetSegment, out SegmentMarkup segmentMarkup) => segmentMarkup.ToXml(),
            _ => null,
        };


        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap)
            => Paste(targetInstanceID, record, false, sourceMap, PasteMapFiller);
        private void PasteMapFiller(Markup markup, ObjectsMap map, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            foreach (var source in sourceMap.Where(p => IsCorrect(p)))
                map.AddSegment(source.Key.NetSegment, source.Value.NetSegment);
        }

        public override void Mirror(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap, float instanceRotation, float mirrorRotation)
            => Paste(targetInstanceID, record, true, sourceMap, MirrorMapFiller);
        private void MirrorMapFiller(Markup markup, ObjectsMap map, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            foreach (var source in sourceMap.Where(p => IsCorrect(p)))
            {
                if (!markup.TryGetEnter(source.Value.NetSegment, out Enter enter))
                    continue;

                var sourceSegment = source.Key.NetSegment;
                var targetSetment = source.Value.NetSegment;
                map.AddSegment(sourceSegment, targetSetment);
                map.AddMirrorEnter(enter);
            }
        }

        private void Paste(InstanceID targetInstanceID, object record, bool isMirror, Dictionary<InstanceID, InstanceID> sourceMap, Action<Markup, ObjectsMap, Dictionary<InstanceID, InstanceID>> mapFiller)
        {
            if (!(record is XElement config))
                return;

            var markup = default(Markup);

            switch (targetInstanceID.Type)
            {
                case InstanceType.NetNode:
                    markup = MarkupManager.NodeManager.Get(targetInstanceID.NetNode);
                    break;
                case InstanceType.NetSegment:
                    markup = MarkupManager.SegmentManager.Get(targetInstanceID.NetNode);
                    break;
                default:
                    return;
            }

            var map = new ObjectsMap(isMirror);
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
