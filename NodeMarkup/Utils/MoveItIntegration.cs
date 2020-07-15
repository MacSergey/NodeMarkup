using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodeMarkup.Manager;

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

        public void PasteNode(ushort nodeID, object record)
        {
            
        }

        public void PasteSegment(ushort segmentId, object record, Dictionary<InstanceID, InstanceID> map) { }
    }
}
