using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodeMarkup.Manager;
using System.Xml.Linq;

namespace NodeMarkup.Utils
{
    public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    {
        public string Name => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public IMoveItIntegration GetInstance() => new MoveItIntegration();
    }

    public class MoveItIntegration : IMoveItIntegration
    {
        public object CopyNode(ushort nodeID)
        {
            if (MarkupManager.TryGetMarkup(nodeID, out Markup markup))
            {
                var data = markup.ToXml();
                return data;
            }
            else
                return null;
        }

        public object CopySegment(ushort segmentId) => null;
        public object Decode64(string base64Data)
        {
            throw new NotImplementedException();
        }

        public string Encode64(object record)
        {
            throw new NotImplementedException();
        }

        public void PasteNode(ushort nodeID, object record, Dictionary<InstanceID, InstanceID> sourceMap)
        {
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

        public void PasteSegment(ushort segmentId, object record, Dictionary<InstanceID, InstanceID> map) { }
    }
}
