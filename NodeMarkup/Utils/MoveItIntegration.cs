using MoveItIntegration;
using System;
using System.Collections.Generic;
using NodeMarkup.Manager;
using System.Xml.Linq;
using System.Xml;
using System.IO;

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
        {
            if (targetInstanceID.Type == InstanceType.NetNode)
            {
                ushort nodeID = targetInstanceID.NetNode;
                var map = new Dictionary<ObjectId, ObjectId>();
                foreach (var source in sourceMap)
                {
                    if (source.Key.Type == InstanceType.NetSegment && source.Value.Type == InstanceType.NetSegment)
                    {
                        map.Add(new ObjectId() { Segment = source.Key.NetSegment }, new ObjectId() { Segment = source.Value.NetSegment });
                    }
                }

                if (record is XElement config)
                {
                    var markup = MarkupManager.Get(nodeID);
                    markup.FromXml(Mod.Version, config, map);
                }
            }
        }

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
