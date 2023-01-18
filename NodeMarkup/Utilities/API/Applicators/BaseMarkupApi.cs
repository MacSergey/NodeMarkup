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

		public IRegularLineData AddNormalLine(IEntrancePointData startPointData, IRegularLineTemplate line)
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

			return new RegularLineData(generatedLine);
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

			return new LaneLineData(generatedLine);
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

			return new StopLineData(generatedLine);
		}

		public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkTemplate crosswalk)
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

			return new CrosswalkData(generatedCrosswalk);
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

			return new Filler(fillerData);
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
			if (fillerData.MarkingId != Markup.Id)
				throw new MarkingIdNotMatchException(Markup.Id, fillerData.MarkingId);

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

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);
			var endPoint = ApiHelper.GetEntrancePoint(Markup, endPointData);

			if (Markup.TryGetLine<MarkupRegularLine>(startPoint, endPoint, out var line))
			{
				regularLine = new RegularLineData(line);

				return true;
			}

			regularLine = null;

			return false;
		}

		public bool TryGetNormalLine(IEntrancePointData startPointData, out IRegularLineData regularLine)
		{
			if (!Markup.TryGetEnter(startPointData.EntranceId, out var enter)
				|| !enter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Normal, out var endPoint))
			{
				throw new IntersectionMarkingToolException($"Could not get the Normal point from the start point {startPointData}");
			}

			var startPoint = ApiHelper.GetEntrancePoint(Markup, startPointData);

			if (Markup.TryGetLine<MarkupRegularLine>(startPoint, endPoint, out var line))
			{
				regularLine = new RegularLineData(line);

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
				stopLine = new StopLineData(line);

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
				laneLine = new LaneLineData(line);

				return true;
			}

			laneLine = null;

			return false;
		}

		public bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkData crosswalk)
		{
			ApiHelper.CheckPoints(Id, startPointData, endPointData, null);

			var startPoint = ApiHelper.GetCrosswalkPoint(Markup, startPointData);
			var endPoint = ApiHelper.GetCrosswalkPoint(Markup, endPointData);

			if (Markup.TryGetLine<MarkupCrosswalkLine>(startPoint, endPoint, out var line))
			{
				crosswalk = new CrosswalkData(line);

				return true;
			}

			crosswalk = null;

			return false;
		}
		#endregion
	}
}
