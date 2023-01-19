using NodeMarkup.API.Implementations;
using NodeMarkup.Manager;

using System.Collections.Generic;

using UnityEngine.Profiling;

namespace NodeMarkup.API.Applicators
{
    public abstract class BaseMarkupApi : IMarkingApi
	{
		public IDataProvider Provider { get; }
		public Markup Markup { get; }
		public ushort Id => Markup.Id;
		public int EntranceCount => Markup.EntersCount;

		protected BaseMarkupApi(IDataProvider provider, Markup markup) 
		{
			Provider = provider;
			Markup = markup;
		}

		public void ClearMarkings() => Markup.Clear();
		public void ResetPointOffsets() => Markup.ResetOffsets();
		public void RecalculateDrawData() => Markup.RecalculateDrawData();

		#region Add
		public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineTemplate line)
		{
			ApiHelper.CheckPoints(Markup.Id, startPointData, endPointData, false);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
			{
				throw new IntersectionMarkingToolException($"Line {pair} already exists");
			}

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddRegularLine(pair, style, (Manager.Alignment)(int)line.Alignment);
			
			return new RegularLineData(generatedLine, startPointData, endPointData, this);
		}

		public INormalLineData AddNormalLine(IEntrancePointData startPointData, IRegularLineTemplate line)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exists");

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddRegularLine(pair, style, (Manager.Alignment)(int)line.Alignment);

			return new NormalLineData(generatedLine, startPointData, new NormalPointData(endPoint as MarkupNormalPoint, startPointData.Entrance), this);
		}

		public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IRegularLineTemplate line)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, false);

			var startPoint = ApiHelper.GetLanePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetLanePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exist");

			var style = RegularLineConverter.GetRegularLineStyle(line);

			var generatedLine = Markup.AddLaneLine(pair, style);

			return new LaneLineData(generatedLine, startPointData, endPointData, this);
		}

		public IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineTemplate line)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, true);

			if (startPointData.Index == endPointData.Index)
				throw new CreateLineException(startPointData, endPointData, "Start and end of stop line must have a different index");

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Line {pair} already exists");

			var style = StopLineConverter.GetStopLineStyle(line);

			var generatedLine = Markup.AddStopLine(pair, style);

			return new StopLineData(generatedLine, startPointData, endPointData, this);
		}

		public ICrosswalkLineData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkTemplate crosswalk)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, true);

			if (startPointData.Index == endPointData.Index)
				throw new CreateLineException(startPointData, endPointData, "Start and end of crosswalk must have a different index");

			var startPoint = ApiHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = ApiHelper.GetCrosswalkPoint(Markup, endPointData);

			var pair = new MarkupPointPair(startPoint, endPoint);

			if (Markup.ExistLine(pair))
				throw new IntersectionMarkingToolException($"Crosswalk {pair} already exist");

			var style = CrosswalkConverter.GetCrosswalkStyle(crosswalk);

			var generatedCrosswalk = Markup.AddCrosswalkLine(pair, style);

			return new CrosswalkLineData(generatedCrosswalk, startPointData, endPointData, this);
		}

		public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerTemplate filler)
		{
			var contour = ApiHelper.GetFillerContour(Markup, pointDatas);

			var style = FillerConverter.GetFillerStyle(filler);

			if (filler.Guides != null && style is IGuideFiller guideFiller)
			{
				guideFiller.LeftGuideA.Value = filler.Guides.LeftGuideA;
				guideFiller.LeftGuideB.Value = filler.Guides.LeftGuideB;
				guideFiller.RightGuideA.Value = filler.Guides.RightGuideA;
				guideFiller.RightGuideB.Value = filler.Guides.RightGuideB;
			}

			var fillerData = Markup.AddFiller(contour, style, out var lines);

			return new FillerData(fillerData, pointDatas, this);
		}
		#endregion

		#region Remove
		private bool RemoveLine(MarkupPoint startPoint, MarkupPoint endPoint)
		{
			if (Markup.TryGetLine(new MarkupPointPair(startPoint, endPoint), out var line))
			{
				Markup.RemoveLine(line);

				Provider.Log($"Line {line} removed");

				return true;
			}

			return false;
		}

		public bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);
			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);
			return RemoveLine(startPoint, endPoint);
		}

		public bool RemoveNormalLine(IEntrancePointData startPointData)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);

			return RemoveLine(startPoint, endPoint);
		}

		public bool RemoveStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			return RemoveLine(startPoint, endPoint);
		}

		public bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetLanePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetLanePoint(Markup, endPointData);

			return RemoveLine(startPoint, endPoint);
		}

		public bool RemoveCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);
			var startPoint = ApiHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = ApiHelper.GetCrosswalkPoint(Markup, endPointData);
			return RemoveLine(startPoint, endPoint);
		}

		public bool RemoveFiller(IFillerData fillerData)
		{
			if (fillerData.Marking.Id != Markup.Id)
				throw new MarkingIdNotMatchException(Markup.Id, fillerData.Marking.Id);

			if (Markup.TryGetFiller(fillerData.Id, out var filler))
			{
				Markup.RemoveFiller(filler);
				Provider.Log($"Filler {filler} removed");
				return true;
			}
			else
				return false;
		}
		#endregion

		#region Exists
		public bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
		}

		public bool NormalLineExist(IEntrancePointData startPointData)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);

			return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
		}

		public bool StopLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
		}

		public bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetLanePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetLanePoint(Markup, endPointData);

			return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
		}

		public bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = ApiHelper.GetCrosswalkPoint(Markup, endPointData);

			return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
		}
		#endregion

		#region Get
		public bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLine)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData, out var startEnter);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData, out var endEnter);

			if (Markup.TryGetLine<MarkupRegularLine>(startPoint, endPoint, out var line))
			{
				var startEnterData = startEnter is SegmentEnter segmentEnter1 ? (IEntranceData)new NodeEntranceData(segmentEnter1) : startEnter is NodeEnter nodeEnter1 ? new SegmentEntranceData(nodeEnter1) : null;
				var endEnterData = endEnter is SegmentEnter segmentEnter2 ? (IEntranceData)new NodeEntranceData(segmentEnter2) : endEnter is NodeEnter nodeEnter2 ? new SegmentEntranceData(nodeEnter2) : null;

				regularLine = new RegularLineData(line, new EntrancePointData(startPoint, startEnterData), new EntrancePointData(endPoint, endEnterData), this);

				return true;
			}

			regularLine = null;

			return false;
		}

		public bool TryGetNormalLine(IEntrancePointData startPointData, out INormalLineData regularLine)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData, out var startEnter);

			if (Markup.TryGetLine<MarkupRegularLine>(startPoint, endPoint, out var line))
			{
				var startEnterData = startEnter is SegmentEnter segmentEnter1 ? (IEntranceData)new NodeEntranceData(segmentEnter1) : startEnter is NodeEnter nodeEnter1 ? new SegmentEntranceData(nodeEnter1) : null;
			
				regularLine = new NormalLineData(line, new EntrancePointData(startPoint, startEnterData), new LanePointData(endPoint as MarkupLanePoint, startEnterData), this);

				return true;
			}

			regularLine = null;

			return false;
		}

		public bool TryGetStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData stopLine)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			if (Markup.TryGetLine<MarkupStopLine>(startPoint, endPoint, out var line))
			{
				stopLine = new StopLineData(line, startPointData, endPointData, this);

				return true;
			}

			stopLine = null;

			return false;
		}

		public bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLine)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetLanePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetLanePoint(Markup, endPointData);

			if (Markup.TryGetLine<MarkupLaneLine>(startPoint, endPoint, out var line))
			{
				laneLine = new LaneLineData(line, startPointData, endPointData, this);

				return true;
			}

			laneLine = null;

			return false;
		}

		public bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkLineData crosswalk)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = ApiHelper.GetCrosswalkPoint(Markup, endPointData);

			if (Markup.TryGetLine<MarkupCrosswalkLine>(startPoint, endPoint, out var line))
			{
				crosswalk = new CrosswalkLineData(line, startPointData, endPointData, this);

				return true;
			}

			crosswalk = null;

			return false;
		}
		#endregion
	}
}
