using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public static class NodeMarkupManager
    {
        static Dictionary<ushort, Markup> NodesMarkup { get; } = new Dictionary<ushort, Markup>();

        public static bool HasMarkup(ushort nodeId) => NodesMarkup.ContainsKey(nodeId);

        public static Markup Get(ushort nodeId)
        {
            if(!NodesMarkup.TryGetValue(nodeId, out Markup markup))
            {
                markup = new Markup(nodeId);
                NodesMarkup[nodeId] = markup;
            }

            return markup;
        }
    }
}
