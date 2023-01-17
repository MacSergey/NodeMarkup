using KianCommons;

using ModsCommon;

using NodeMarkup.API.Styles;
using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;

using RegularLineStyle = NodeMarkup.API.Styles.RegularLineStyle;

namespace NodeMarkup.API.Internal
{
    internal class BaseMarkupApi<MarkingType, TypeMarkup> 
		where MarkingType : MarkupManager<TypeMarkup>, IManager, new()
		where TypeMarkup : Markup
	{
        protected TypeMarkup Markup { get; }

        protected BaseMarkupApi(ushort id)
        {
            Markup = SingletonManager<MarkingType>.Instance.GetOrCreateMarkup(id);
        }

		public Styles.IRegularLine AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, RegularLine line)
		{
			DataProvider.CheckPoints(Markup.Id, startPointData, endPointData, false);

			var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
			var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exist");

			var style = GetRegularLineStyle(line);

			var generatedLine = Markup.AddRegularLine(pair, style, line.Alignment);

			return new RegularLine(generatedLine);
		}

		private Manager.RegularLineStyle GetRegularLineStyle(RegularLine info)
		{
			switch (info.Style)
			{
				case RegularLineStyle.Solid:
					return new SolidLineStyle(info.Color, info.Width);
					
				case RegularLineStyle.Dashed:
					return new DashedLineStyle(info.Color, info.Width, info.DashLength, info.SpaceLength);

				case RegularLineStyle.DoubleSolid:
					return new DoubleSolidLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.Offset);

				case RegularLineStyle.DoubleDashed:
					return new DoubleDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);
					
				case RegularLineStyle.SolidAndDashed:
					return new SolidAndDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case RegularLineStyle.DoubleDashedAsym:
					return new DoubleDashedAsymLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.AsymDashLength, info.SpaceLength, info.Offset);

				case RegularLineStyle.Pavement:
					return new PavementLineStyle(info.Width, info.Elevation);

				case RegularLineStyle.SharkTeeth:
					if (info.SharkTeethInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.SharkTeethInfo));

					return info.SharkTeethInfo.LineStyle(info.Color);

				case RegularLineStyle.ZigZag:
					if (info.ZigZagInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.ZigZagInfo));

					return info.ZigZagInfo.LineStyle(info.Color);

				case RegularLineStyle.Prop:
					if (info.PropInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.PropInfo));

					return info.PropInfo.LineStyle();

				case RegularLineStyle.Tree:
					if (info.TreeInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.TreeInfo));

					return info.TreeInfo.LineStyle();

				case RegularLineStyle.Text:
					if (info.TextInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.TextInfo));

					return info.TextInfo.LineStyle(info.Color);

				case RegularLineStyle.Network:
					if (info.NetworkInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.NetworkInfo));

					return info.NetworkInfo.LineStyle();

				default:
					throw new NotImplementedException();
			}
		}

	}
}
