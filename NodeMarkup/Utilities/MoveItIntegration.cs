using ModsCommon;
using MoveItIntegration;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NodeMarkup.Utilities
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

        public override string Name => SingletonMod<Mod>.NameRaw;

        public override string Description => Localize.Mod_Description;

        public override Version DataVersion => new Version(1, 0);

        public override object Copy(InstanceID sourceInstanceID) => sourceInstanceID.Type switch
        {
            InstanceType.NetNode when SingletonManager<NodeMarkupManager>.Instance.TryGetMarkup(sourceInstanceID.NetNode, out Manager.NodeMarkup nodeMarkup) => nodeMarkup.ToXml(),
            InstanceType.NetSegment when SingletonManager<SegmentMarkupManager>.Instance.TryGetMarkup(sourceInstanceID.NetSegment, out SegmentMarkup segmentMarkup) => segmentMarkup.ToXml(),
            _ => null,
        };


        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap) => Paste(targetInstanceID, record, sourceMap, PasteMapGetter);
        private ObjectsMap PasteMapGetter(Markup markup, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            var map = new ObjectsMap();
            map.FromDictionary(sourceMap);
            return map;
        }

        public override void Mirror(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap, float instanceRotation, float mirrorRotation) => Paste(targetInstanceID, record, sourceMap, MirrorMapGetter);
        private ObjectsMap MirrorMapGetter(Markup markup, Dictionary<InstanceID, InstanceID> sourceMap)
        {
            var map = new ObjectsMap(true);
            foreach (var source in sourceMap.Where(p => IsCorrect(p)))
            {
                if (!markup.TryGetEnter(source.Value.NetSegment, out Enter enter))
                    continue;

                map.AddSegment(source.Key.NetSegment, source.Value.NetSegment);
                map.AddMirrorEnter(enter);
            }
            return map;

            static bool IsCorrect(KeyValuePair<InstanceID, InstanceID> pair) => pair.Key.Type == InstanceType.NetSegment && pair.Value.Type == InstanceType.NetSegment;
        }

        private void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> sourceMap, Func<Markup, Dictionary<InstanceID, InstanceID>, ObjectsMap> mapGetter)
        {
            if (record is not XElement config)
                return;

            Markup markup;
            switch (targetInstanceID.Type)
            {
                case InstanceType.NetNode:
                    markup = SingletonManager<NodeMarkupManager>.Instance[targetInstanceID.NetNode];
                    break;
                case InstanceType.NetSegment:
                    markup = SingletonManager<SegmentMarkupManager>.Instance[targetInstanceID.NetSegment];
                    break;
                default:
                    return;
            }

            var map = mapGetter(markup, sourceMap);
            markup.FromXml(SingletonMod<Mod>.Version, config, map);
        }

        public override string Encode64(object record) => record == null ? null : EncodeUtil.BinaryEncode64(record?.ToString());
        public override object Decode64(string record, Version dataVersion)
        {
            if (record == null || record.Length == 0)
                return null;

            using StringReader input = new StringReader((string)EncodeUtil.BinaryDecode64(record));
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                ProhibitDtd = false,
                XmlResolver = null
            };
            using XmlReader reader = XmlReader.Create(input, xmlReaderSettings);
            return XElement.Load(reader, LoadOptions.None);
        }
    }
}
