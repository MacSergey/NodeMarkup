using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.API.Internal
{
	internal class NodeMarkupApi : BaseMarkupApi<NodeMarkupManager, Manager.NodeMarkup>
	{
		public NodeMarkupApi(ushort id) : base(id)
		{

		}
	}
}
