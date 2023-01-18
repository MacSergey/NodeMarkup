using NodeMarkup.Manager;

using System.Collections.Generic;

namespace NodeMarkup.API.Applicators
{
    public abstract class BaseMarkupApi : IMarkingData
	{
		public Markup Markup { get; set; }
		public ushort Id => Markup.Id;
		public int EntranceCount => Markup.EntersCount;

		protected BaseMarkupApi(Markup markup) { Markup = markup; }

		public void ClearMarkings() => Markup.Clear();
		public void ResetPointOffsets() => Markup.ResetOffsets();

		public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineTemplate line)
		{
			MarkupHelper.CheckPoints(Markup.Id, startPointData, endPointData, false);

			var startPoint = MarkupHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = MarkupHelper.GetEntrancePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exists");

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddRegularLine(pair, style, (Manager.Alignment)(int)line.Alignment);

			return new RegularLine(generatedLine);
		}

		public IRegularLineData AddNormalLine(IEntrancePointData startPointData, IRegularLineTemplate line)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = MarkupHelper.GetEntrancePoint(Markup, startPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exists");

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddRegularLine(pair, style, (Manager.Alignment)(int)line.Alignment);

			return new RegularLine(generatedLine);
		}

		public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IRegularLineTemplate line)
		{
			MarkupHelper.CheckPoints(Id, startPointData, endPointData, false);

			var startPoint = MarkupHelper.GetLanePoint(Markup, startPointData);
			var endPoint = MarkupHelper.GetLanePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exist");

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddLaneLine(pair, style);

			return new RegularLine(generatedLine);
		}

		public IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineTemplate line)
		{
			MarkupHelper.CheckPoints(Id, startPointData, endPointData, true);

			if (startPointData.Index == endPointData.Index)
				throw new CreateLineException(startPointData, endPointData, "Start and end of stop line must have a different index");

			var startPoint = MarkupHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = MarkupHelper.GetEntrancePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exists");

			var style = StopLineConverter.GetStopLineStyle(line);

			var generatedLine = Markup.AddStopLine(pair, style);

			return new StopLine(generatedLine);
		}

		public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkTemplate crosswalk)
		{
			MarkupHelper.CheckPoints(Id, startPointData, endPointData, true);

			if (startPointData.Index == endPointData.Index)
				throw new CreateLineException(startPointData, endPointData, "Start and end of crosswalk must have a different index");

			var startPoint = MarkupHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = MarkupHelper.GetCrosswalkPoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Crosswalk {pair} already exist");

			var style = CrosswalkConverter.GetCrosswalkStyle(crosswalk);

			var generatedCrosswalk = Markup.AddCrosswalkLine(pair, style);

			return new Crosswalk(generatedCrosswalk);
		}

		public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerTemplate filler)
		{
			var contour = MarkupHelper.GetFillerContour(Markup, pointDatas);

			var style = FillerConverter.GetFillerStyle(filler);

			if (filler.Guides != null && style is IGuideFiller guideFiller)
			{
				guideFiller.LeftGuideA.Value = filler.Guides.LeftGuideA;
				guideFiller.LeftGuideB.Value = filler.Guides.LeftGuideB;
				guideFiller.RightGuideA.Value = filler.Guides.RightGuideA;
				guideFiller.RightGuideB.Value = filler.Guides.RightGuideB;
			}

			var fillerData = Markup.AddFiller(contour, style, out var lines);

			return new Filler(fillerData);
		}
	}
}
