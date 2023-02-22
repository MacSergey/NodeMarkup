using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using ObjectId = IMT.Utilities.ObjectId;

namespace IMT.Manager
{
    public abstract class Marking : IUpdatePoints, IUpdateLines, IUpdateFillers, IUpdateCrosswalks, IToXml
    {
        [Flags]
        public enum SupportType
        {
            None = 0,
            Points = 1 << 0,
            Enters = 1 << 1,
            Lines = 1 << 2,
            Fillers = 1 << 3,
            Croswalks = 1 << 4,
            StyleTemplates = 1 << 5,
            IntersectionTemplates = 1 << 6,
        }

        #region PROPERTIES
        public abstract MarkingType Type { get; }
        public abstract SupportType Support { get; }
        public virtual LineType SupportLines => LineType.Regular | LineType.Lane;

        public ushort Id { get; }
        protected abstract bool IsExist { get; }

        public Vector3 Position { get; private set; }
        public Vector3 CenterPosition { get; private set; }
        public float CenterRadius { get; private set; }
        public float Radius { get; private set; }
        public float Height => Position.y;
        public abstract string XmlSection { get; }
        public abstract string PanelCaption { get; }
        public abstract bool IsUnderground { get; }

        protected List<Entrance> RawEntersList { get; set; } = new List<Entrance>();
        protected List<Entrance> EntersList { get; set; } = new List<Entrance>();
        protected Dictionary<ulong, MarkingLine> LinesDictionary { get; } = new Dictionary<ulong, MarkingLine>();
        protected Dictionary<MarkingLinePair, MarkingLinesIntersect> LineIntersects { get; } = new Dictionary<MarkingLinePair, MarkingLinesIntersect>(MarkingLinePair.Comparer);
        protected List<MarkingFiller> FillersList { get; } = new List<MarkingFiller>();
        protected Dictionary<MarkingLine, MarkingCrosswalk> CrosswalksDictionary { get; } = new Dictionary<MarkingLine, MarkingCrosswalk>();

        public bool IsEmpty => !LinesDictionary.Any() && !FillersList.Any();

        public IEnumerable<MarkingLine> Lines => LinesDictionary.Values;
        public IEnumerable<Entrance> Enters => EntersList;
        public IEnumerable<MarkingFiller> Fillers => FillersList;
        public IEnumerable<MarkingCrosswalk> Crosswalks => CrosswalksDictionary.Values;
        public IEnumerable<MarkingLinesIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);

        public int EntersCount => EntersList.Count;
        public int LinesCount => LinesDictionary.Count;
        public int CrosswalksCount => CrosswalksDictionary.Count;
        public int FillersCount => FillersList.Count;


        public bool NeedRecalculateDrawData { get; private set; }
        private HashSet<IStyleItem> RecalculateList { get; set; } = new HashSet<IStyleItem>();
        public MarkingRenderData DrawData { get; } = new MarkingRenderData();


        private bool _needSetOrder;
        public bool NeedSetOrder
        {
            get => _needSetOrder;
            set
            {
                if (_needSetOrder && !value)
                    Backup = null;
                else if (!_needSetOrder && value)
                    Backup = new IntersectionTemplate(this);

                _needSetOrder = value;
            }
        }
        public IntersectionTemplate Backup { get; private set; }

        protected EnterDic<ITrajectory> BetweenEnters { get; } = new EnterDic<ITrajectory>();
        public IEnumerable<ITrajectory> Contour
        {
            get
            {
                foreach (var enter in Enters)
                    yield return enter.Line;
                foreach (var line in BetweenEnters.Values)
                    yield return line;
            }
        }

        #endregion

        public Marking(ushort id)
        {
            Id = id;

            if (!IsExist)
                throw new NotExistItemException(Type, id);
        }

        #region UPDATE

        private bool UpdateInProgress { get; set; } = false;
        private bool LoadInProgress { get; set; } = false;

        public void Update()
        {
            if (UpdateInProgress)
                return;

            UpdateInProgress = true;

            try
            {
                UpdateEnters();
                UpdateLines();
                UpdateFillers();
                UpdateCrosswalks();

                RecalculateAllStyleData();
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Failed to update {Type} #{Id}", error);
            }

            UpdateInProgress = false;
        }
        protected virtual void UpdateEnters()
        {
            Position = GetPosition();

            var oldEnters = RawEntersList;
            var before = oldEnters.Select(e => e.Id).ToArray();
            var after = GetEnters().ToArray();

            var still = before.Intersect(after).ToArray();
            var delete = before.Except(still).ToArray();
            var add = after.Except(still).ToArray();
            var changed = oldEnters.Where(e => e.LanesChanged).Select(e => e.Id).Except(delete).ToArray();
            var notChanged = still.Except(changed).ToArray();

            var newEnters = notChanged.Select(id => oldEnters.Find(e => e.Id == id)).ToList();
            foreach (var addId in add)
                newEnters.Add(NewEnter(addId));
            foreach (var changedId in changed)
                newEnters.Add(NewEnter(changedId));

            foreach (var enter in newEnters)
                enter.Update();
            newEnters.Sort((e1, e2) => e1.CompareTo(e2));

            UpdateBackup(delete, add, changed, oldEnters, newEnters);

            RawEntersList = newEnters;
            EntersList = RawEntersList.Where(e => e.PointCount != 0).ToList();

            UpdateRadius();
            UpdateContour();

            foreach (var enter in EntersList)
                enter.UpdatePoints();

            GetCentreAndRadius(out var center, out var radius);
            CenterPosition = center;
            CenterRadius = radius;
        }
        private void UpdateContour()
        {
            BetweenEnters.Clear();

            for (var i = 0; i < EntersList.Count; i += 1)
            {
                var j = i.NextIndex(EntersList.Count);
                var prev = EntersList[i];
                var next = EntersList[j];

                var betweenBezier = new Bezier3()
                {
                    a = prev.LastPointSide,
                    d = next.FirstPointSide
                };
                NetSegment.CalculateMiddlePoints(betweenBezier.a, prev.NormalDir, betweenBezier.d, next.NormalDir, true, true, out betweenBezier.b, out betweenBezier.c);

                BetweenEnters[i, j] = new BezierTrajectory(betweenBezier);
            }
        }

        protected abstract Vector3 GetPosition();

        protected abstract IEnumerable<ushort> GetEnters();
        protected abstract Entrance NewEnter(ushort id);

        private void UpdateBackup(ushort[] delete, ushort[] add, ushort[] changed, List<Entrance> oldEnters, List<Entrance> newEnters)
        {
            if ((delete.Length != 1 || add.Length != 1) && changed.Length == 0)
                return;

            var auto = false;
            var map = new ObjectsMap();

            if (delete.Length == 1 && add.Length == 1 && UpdateBackup(delete[0], add[0], oldEnters, newEnters))
            {
                map.AddSegment(delete[0], add[0]);
                auto = true;
            }

            foreach (var item in changed)
                auto |= UpdateBackup(item, item, oldEnters, newEnters);

            if (!auto)
                return;

            var currentData = ToXml();
            RawEntersList = newEnters;
            Clear();
            FromXml(SingletonMod<Mod>.Version, currentData, map);
        }
        private bool UpdateBackup(ushort delete, ushort add, List<Entrance> oldEnters, List<Entrance> newEnters)
        {
            var oldEnter = oldEnters.Find(e => e.Id == delete);
            var newEnter = newEnters.Find(e => e.Id == add);

            var before = oldEnter.PointCount;
            var after = newEnter.PointCount;

            if (before != after && !NeedSetOrder && !IsEmpty && HaveLines(oldEnter))
                NeedSetOrder = true;

            if (NeedSetOrder && delete != add)
            {
                var pair = Backup.Map.FirstOrDefault(p => p.Value.Type == ObjectId.SegmentType && p.Value.Segment == delete);

                if (pair.Key is not null && pair.Value is not null)
                {
                    Backup.Map.Remove(pair.Key);
                    Backup.Map.AddSegment(pair.Key.Segment, add);
                }
                else
                    Backup.Map.AddSegment(delete, add);
            }

            return before == after;
        }

        private void UpdateRadius() => Radius = EntersList.Where(e => e.Position != null).Aggregate(0f, (delta, e) => Mathf.Max(delta, (Position - e.Position).magnitude));

        private void UpdateLines()
        {
            foreach (var line in LinesDictionary.Values.ToArray())
            {
                if (ContainsEnter(line.Start.Enter.Id) && ContainsEnter(line.End.Enter.Id))
                    line.Update(true);
                else
                    RemoveLine(line);
            }
        }
        private void UpdateFillers()
        {
            foreach (var filler in FillersList)
                filler.Update(true);
        }
        private void UpdateCrosswalks()
        {
            foreach (var crosswalk in Crosswalks)
                crosswalk.Update(true);
        }

        public void Update(MarkingPoint point, bool recalculate = false, bool recalcDependences = false)
        {
            if (LoadInProgress)
                return;

            point.Update();

            foreach (var line in GetPointLines(point))
                line.Update();

            foreach (var filler in GetPointFillers(point))
                filler.Update();

            foreach (var crosswalk in GetPointCrosswalks(point))
                crosswalk.Update();

            if (recalculate && !UpdateInProgress)
                RecalculateAllStyleData();
        }
        public void Update(MarkingLine line, bool recalculate = false, bool recalcDependences = false)
        {
            if (LoadInProgress)
                return;

            var toRecalculate = new HashSet<IStyleItem>();
            toRecalculate.Add(line);

            line.Update(true);

            if (recalcDependences)
            {
                foreach (var intersect in GetExistIntersects(line).ToArray())
                {
                    LineIntersects.Remove(intersect.pair);
                    var otherLine = intersect.pair.GetOther(line);
                    otherLine.Update();
                    toRecalculate.Add(otherLine);
                }

                foreach (var filler in GetLineFillers(line))
                {
                    filler.Update();
                    toRecalculate.Add(filler);
                }

                foreach (var crosswalk in GetLinesIsBorder(line))
                {
                    crosswalk.Update();
                    toRecalculate.Add(crosswalk);
                }

                if (line is MarkingCrosswalkLine crosswalkLine)
                {
                    crosswalkLine.Crosswalk.Update(true);
                    toRecalculate.Add(crosswalkLine.Crosswalk);
                }
            }

            if (recalculate && !UpdateInProgress)
                RecalculateStyleData(toRecalculate);
        }
        public void Update(MarkingFiller filler, bool recalculate = false, bool recalcDependences = false)
        {
            if (LoadInProgress)
                return;

            filler.Update();
            if (recalculate && !UpdateInProgress)
                RecalculateStyleData(filler);
        }
        public void Update(MarkingCrosswalk crosswalk, bool recalculate = false, bool recalcDependences = false) => Update(crosswalk.CrosswalkLine, recalculate, recalcDependences);

        public void Clear()
        {
            LinesDictionary.Clear();
            FillersList.Clear();
            CrosswalksDictionary.Clear();
            NeedSetOrder = false;

            RecalculateAllStyleData();
        }
        public void ResetOffsets()
        {
            foreach (var enter in Enters)
                enter.ResetPoints();
        }

        #endregion

        #region RECALCULATE

        public void RecalculateAllStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate markup {this}");
#endif
            LineIntersects.Clear();

            var toRecalculate = new HashSet<IStyleItem>();

            foreach (var line in Lines)
                toRecalculate.Add(line);

            foreach (var filler in Fillers)
                toRecalculate.Add(filler);

            foreach (var crosswalk in Crosswalks)
                toRecalculate.Add(crosswalk);

            RecalculateStyleData(toRecalculate);
        }
        public void RecalculateStyleData(IStyleItem toRecalculate = null)
        {
            lock (RecalculateList)
            {
                if (toRecalculate != null)
                    RecalculateList.Add(toRecalculate);

                NeedRecalculateDrawData = true;
            }
        }
        public void RecalculateStyleData(HashSet<IStyleItem> toRecalculate)
        {
            lock (RecalculateList)
            {
                RecalculateList.AddRange(toRecalculate);
                NeedRecalculateDrawData = true;
            }
        }

        public void RecalculateDrawData()
        {
            lock (RecalculateList)
            {
                DrawData.Clear();

                foreach (var item in RecalculateList)
                    item.RecalculateStyleData();

                foreach(var line in Lines)
                {
                    foreach(var styleData in line.StyleData)
                        DrawData[styleData.LODType][styleData.LOD].Add(styleData);
                }
                foreach (var fillers in Fillers)
                {
                    foreach (var styleData in fillers.StyleData)
                        DrawData[styleData.LODType][styleData.LOD].Add(styleData);
                }
                foreach (var crosswalk in Crosswalks)
                {
                    foreach (var styleData in crosswalk.StyleData)
                        DrawData[styleData.LODType][styleData.LOD].Add(styleData);
                }

                RecalculateList.Clear();
                NeedRecalculateDrawData = false;
            }
        }

        #endregion

        #region LINES

        public bool ExistLine(MarkingPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

        private LineType AddLine<LineType>(MarkingPointPair pointPair, Func<LineType> newLine)
            where LineType : MarkingLine
        {
            if (!TryGetLine(pointPair, out LineType line))
            {
                line = newLine();
                LinesDictionary[pointPair.Hash] = line;
                RecalculateStyleData();
            }

            return line;
        }
        public MarkingRegularLine AddLine(MarkingPointPair pointPair, RegularLineStyle style, Alignment alignment = Alignment.Centre)
        {
            if (pointPair.IsNormal)
                return AddNormalLine(pointPair, style, alignment);
            else if (pointPair.IsLane)
                return AddLaneLine(pointPair, style);
            else
                return AddRegularLine(pointPair, style, alignment);
        }
        public MarkingRegularLine AddRegularLine(MarkingPointPair pointPair, RegularLineStyle style, Alignment alignment = Alignment.Centre)
        {
            return AddLine(pointPair, () => new MarkingRegularLine(this, pointPair, style, alignment));
        }
        public MarkingNormalLine AddNormalLine(MarkingPointPair pointPair, RegularLineStyle style, Alignment alignment = Alignment.Centre)
        {
            return AddLine(pointPair, () => new MarkingNormalLine(this, pointPair, style, alignment));
        }
        public MarkingLaneLine AddLaneLine(MarkingPointPair pointPair, RegularLineStyle style)
        {
            return AddLine(pointPair, () => new MarkingLaneLine(this, pointPair, style));
        }
        public MarkingStopLine AddStopLine(MarkingPointPair pointPair, StopLineStyle style)
        {
            return AddLine(pointPair, () => new MarkingStopLine(this, pointPair, style));
        }
        public MarkingCrosswalkLine AddCrosswalkLine(MarkingPointPair pointPair, BaseCrosswalkStyle style)
        {
            return AddLine(pointPair, () => new MarkingCrosswalkLine(this, pointPair, style));
        }


        public void RemoveLine(MarkingLine line) => RemoveLine(line, true);
        private void RemoveLine(MarkingLine line, bool recalculate = false)
        {
            var toRecalculate = new HashSet<IStyleItem>();

            LinesDictionary.Remove(line.PointPair.Hash);

            foreach (var intersect in GetExistIntersects(line).ToArray())
            {
                if (intersect.pair.GetOther(line) is MarkingRegularLine regularLine)
                {
                    if (regularLine.RemoveRules(line))
                        toRecalculate.Add(regularLine);
                }

                LineIntersects.Remove(intersect.pair);
            }
            foreach (var filler in GetLineFillers(line).ToArray())
                FillersList.Remove(filler);

            if (CrosswalksDictionary.ContainsKey(line))
                CrosswalksDictionary.Remove(line);
            else
            {
                foreach (var crosswalk in GetLinesIsBorder(line))
                {
                    crosswalk.RemoveBorder(line);
                    toRecalculate.Add(crosswalk);
                }
            }

            if (recalculate)
                RecalculateStyleData(toRecalculate);
        }
        public Dependences GetLineDependences(MarkingLine line)
        {
            var dependences = new Dependences
            {
                Rules = 0,
                Fillers = GetLineFillers(line).Count(),
                Crosswalks = CrosswalksDictionary.ContainsKey(line) ? 1 : 0,
                CrosswalkBorders = GetLinesIsBorder(line).Count(),
            };
            foreach (var intersect in GetExistIntersects(line).ToArray())
            {
                if (intersect.pair.GetOther(line) is MarkingRegularLine regularLine)
                    dependences.Rules += regularLine.GetLineDependences(line);
            }

            return dependences;
        }

        #endregion

        #region FILLERS

        public void AddFiller(MarkingFiller filler)
        {
            FillersList.Add(filler);
            RecalculateStyleData(filler);
        }
        public MarkingFiller AddFiller(FillerContour contour, BaseFillerStyle style, out List<MarkingRegularLine> lines)
        {
            lines = new List<MarkingRegularLine>();
            foreach (var part in contour.RawEdges)
            {
                if (part.Line is MarkingFillerTempLine line)
                {
                    var newLine = AddLine(part.Line.PointPair, null, line.Alignment);
                    lines.Add(newLine);
                }
            }
            contour.Update();

            var filler = new MarkingFiller(contour, style);
            FillersList.Add(filler);
            RecalculateStyleData(filler);

            return filler;
        }
        public void RemoveFiller(MarkingFiller filler)
        {
            FillersList.Remove(filler);
            RecalculateStyleData();
        }

        #endregion

        #region CROSSWALK

        public void AddCrosswalk(MarkingCrosswalk crosswalk)
        {
            CrosswalksDictionary[crosswalk.CrosswalkLine] = crosswalk;
            RecalculateStyleData(crosswalk);
        }
        public void RemoveCrosswalk(MarkingCrosswalk crosswalk) => RemoveLine(crosswalk.CrosswalkLine);
        public Dependences GetCrosswalkDependences(MarkingCrosswalk crosswalk)
        {
            var dependences = GetLineDependences(crosswalk.CrosswalkLine);
            dependences.Crosswalks = 0;
            dependences.Lines = crosswalk.CrosswalkLine.Rules.Any() ? 1 : 0;
            return dependences;
        }
        public void CutLinesByCrosswalk(MarkingCrosswalk crosswalk)
        {
            var enter = crosswalk.CrosswalkLine.Start.Enter;
            var lines = Lines.Where(l => l.Type == LineType.Regular && l.PointPair.ContainsEnter(enter)).ToArray();

            foreach (var line in lines)
            {
                if (Settings.NotCutBordersByCrosswalk && crosswalk.IsBorder(line))
                    continue;

                var intersect = GetIntersect(line, crosswalk.CrosswalkLine);
                if (!intersect.IsIntersect)
                    continue;

                foreach (var rule in line.Rules)
                {
                    if (!rule.GetFromT(out float fromT) || !rule.GetToT(out float toT))
                        continue;

                    if (!(fromT < intersect.FirstT && intersect.FirstT < toT) && !(toT < intersect.FirstT && intersect.FirstT < fromT))
                        continue;

                    if ((line.End.Type == MarkingPoint.PointType.Enter && line.End.Enter == enter) ^ fromT < toT)
                        rule.From = new LinesIntersectEdge(intersect.pair);
                    else
                        rule.To = new LinesIntersectEdge(intersect.pair);
                }
            }
        }

        #endregion

        #region GET & CONTAINS

        public bool TryGetLine(MarkingPointPair pointPair, out MarkingLine line) => LinesDictionary.TryGetValue(pointPair.Hash, out line);
        public bool TryGetLine<LineType>(MarkingPoint first, MarkingPoint second, out LineType line) where LineType : MarkingLine => TryGetLine(new MarkingPointPair(first, second), out line);
        public bool TryGetLine<LineType>(MarkingPointPair pointPair, out LineType line)
            where LineType : MarkingLine
        {
            if (LinesDictionary.TryGetValue(pointPair.Hash, out MarkingLine rawLine) && rawLine is LineType)
            {
                line = rawLine as LineType;
                return true;
            }
            else
            {
                line = null;
                return false;
            }
        }
        public bool TryGetLine<LineType>(ulong lineId, ObjectsMap map, out LineType line)
            where LineType : MarkingLine
        {
            if (MarkingPointPair.FromHash(lineId, this, map, out MarkingPointPair pair, out _))
                return TryGetLine(pair, out line);
            else
            {
                line = null;
                return false;
            }
        }
        public bool TryGetFiller(int id, out MarkingFiller filler)
        {
            filler = FillersList.Find(f => f.Id == id);
            return filler != null;
        }

        public bool TryGetEnter(ushort enterId, out Entrance enter)
        {
            enter = RawEntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => RawEntersList.Find(e => e.Id == enterId) != null;
        public bool ContainsLine(MarkingPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

        public IEnumerable<MarkingLinesIntersect> GetExistIntersects(MarkingLine line, bool onlyIntersect = false)
            => LineIntersects.Values.Where(i => i.pair.ContainLine(line) && (!onlyIntersect || i.IsIntersect));
        public IEnumerable<MarkingLinesIntersect> GetIntersects(MarkingLine line)
        {
            foreach (var otherLine in Lines)
            {
                if (otherLine != line)
                    yield return GetIntersect(new MarkingLinePair(line, otherLine));
            }
        }

        public MarkingLinesIntersect GetIntersect(MarkingLine first, MarkingLine second) => GetIntersect(new MarkingLinePair(first, second));
        public MarkingLinesIntersect GetIntersect(MarkingLinePair linePair)
        {
            if (!LineIntersects.TryGetValue(linePair, out MarkingLinesIntersect intersect))
            {
                intersect = MarkingLinesIntersect.Calculate(linePair);
                LineIntersects[linePair] = intersect;
            }

            return intersect;
        }
        public IEnumerable<MarkingLinesIntersect> GetAllIntersect()
        {
            var lines = Lines.ToArray();
            for (var i = 0; i < lines.Length; i += 1)
            {
                for (var j = i + 1; j < lines.Length; j += 1)
                {
                    yield return GetIntersect(new MarkingLinePair(lines[i], lines[j]));
                }
            }
        }

        public Entrance GetNextEnter(Entrance current) => GetNextEnter(EntersList.IndexOf(current));
        public Entrance GetNextEnter(int index) => EntersList[index.NextIndex(EntersList.Count)];
        public Entrance GetPrevEnter(Entrance current) => GetPrevEnter(EntersList.IndexOf(current));
        public Entrance GetPrevEnter(int index) => EntersList[index.PrevIndex(EntersList.Count)];

        public IEnumerable<MarkingLine> GetPointLines(MarkingPoint point) => Lines.Where(l => l.ContainsPoint(point));
        public IEnumerable<MarkingFiller> GetLineFillers(MarkingLine line) => FillersList.Where(f => f.ContainsLine(line));
        public IEnumerable<MarkingFiller> GetPointFillers(MarkingPoint point) => FillersList.Where(f => f.ContainsPoint(point));
        public IEnumerable<MarkingCrosswalk> GetPointCrosswalks(MarkingPoint point) => Crosswalks.Where(c => c.ContainsPoint(point));
        public IEnumerable<MarkingCrosswalk> GetLinesIsBorder(MarkingLine line) => Crosswalks.Where(c => c.IsBorder(line));

        public bool HaveLines(Entrance enter) => Lines.Any(l => l.ContainsEnter(enter));
        public bool HaveLines(MarkingPoint point) => Lines.Any(l => l.ContainsPoint(point));

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);

            foreach (var enter in Enters)
            {
                foreach (var point in enter.EnterPoints)
                    config.Add(point.ToXml());
            }
            foreach (var line in Lines)
                config.Add(line.ToXml());

            foreach (var filler in Fillers)
                config.Add(filler.ToXml());

            foreach (var crosswalk in Crosswalks)
                config.Add(crosswalk.ToXml());

            return config;
        }

        public virtual void FromXml(Version version, XElement config, ObjectsMap map, bool needUpdate = true)
        {
            LoadInProgress = true;
#if BETA
            if (version < new Version("1.8.0.761"))
                map = VersionMigration.Befor1_9(this, map);
#else
            if (version < new Version("1.9"))
                map = VersionMigration.Befor1_9(this, map);
#endif
            foreach (var pointConfig in config.Elements(MarkingPoint.XmlName))
                MarkingPoint.FromXml(pointConfig, this, map);

            var toInitLines = new Dictionary<MarkingLine, XElement>();
            var invertLines = new HashSet<MarkingLine>();
            foreach (var lineConfig in config.Elements(MarkingLine.XmlName))
            {
                if (MarkingLine.FromXml(lineConfig, this, map, out MarkingLine line, out bool invertLine))
                {
                    LinesDictionary[line.Id] = line;
                    toInitLines[line] = lineConfig;
                    if (invertLine)
                        invertLines.Add(line);
                }
            }

            var typeChanged = config.Name.LocalName != XmlSection;
            foreach (var pair in toInitLines)
                pair.Key.FromXml(pair.Value, map, invertLines.Contains(pair.Key), typeChanged);

            if ((Support & SupportType.Fillers) != 0)
            {
                foreach (var fillerConfig in config.Elements(MarkingFiller.XmlName))
                {
                    if (MarkingFiller.FromXml(fillerConfig, this, map, out MarkingFiller filler))
                        FillersList.Add(filler);
                }
            }

            if ((Support & SupportType.Croswalks) != 0)
            {
                foreach (var crosswalkConfig in config.Elements(MarkingCrosswalk.XmlName))
                {
                    if (MarkingCrosswalk.FromXml(crosswalkConfig, this, map, out MarkingCrosswalk crosswalk))
                        CrosswalksDictionary[crosswalk.CrosswalkLine] = crosswalk;
                }
            }

            LoadInProgress = false;

            if (needUpdate)
                Update();
        }

        public void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            foreach (var line in Lines)
                line.GetUsedAssets(networks, props, trees);

            foreach (var filler in Fillers)
                filler.GetUsedAssets(networks, props, trees);

            foreach (var crosswalk in Crosswalks)
                crosswalk.GetUsedAssets(networks, props, trees);
        }

        #endregion

        #region UTILITY

        public void GetCentreAndRadius(out Vector3 centre, out float radius)
        {
            var points = Enters.Where(e => e.Position != null).SelectMany(e => new Vector3[] { e.FirstPointSide, e.LastPointSide }).ToArray();

            if (points.Length == 0)
            {
                centre = Position;
                radius = Radius;
                return;
            }

            centre = Position;
            radius = 1000f;

            for (var i = 0; i < points.Length; i += 1)
            {
                for (var j = i + 1; j < points.Length; j += 1)
                {
                    GetCircle2Points(points, i, j, ref centre, ref radius);

                    for (var k = j + 1; k < points.Length; k += 1)
                        GetCircle3Points(points, i, j, k, ref centre, ref radius);
                }
            }
        }
        private void GetCircle2Points(Vector3[] points, int i, int j, ref Vector3 centre, ref float radius)
        {
            var newCentre = (points[i] + points[j]) * 0.5f;
            var newRadius = (points[i] - points[j]).magnitude * 0.5f;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private void GetCircle3Points(Vector3[] points, int i, int j, int k, ref Vector3 centre, ref float radius)
        {
            var pos1 = (points[i] + points[j]) * 0.5f;
            var pos2 = (points[j] + points[k]) * 0.5f;

            var dir1 = (points[i] - points[j]).Turn90(true).normalized;
            var dir2 = (points[j] - points[k]).Turn90(true).normalized;

            Line2.Intersect(XZ(pos1), XZ(pos1 + dir1), XZ(pos2), XZ(pos2 + dir2), out float p, out _);
            var newCentre = pos1 + dir1 * p;
            var newRadius = (newCentre - points[i]).magnitude;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j, k))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private bool AllPointsInCircle(Vector3[] points, Vector3 centre, float radius, params int[] ignore)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                if (ignore.Any(j => j == i))
                    continue;

                if ((centre - points[i]).magnitude > radius)
                    return false;
            }

            return true;
        }

        #endregion

        public override string ToString() => $"{Id}:{RawEntersList.Count}";

        public enum Item
        {
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            RegularLine = 0x100,

            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            StopLine = 0x200,

            [Description(nameof(Localize.FillerStyle_Group))]
            Filler = 0x400,

            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = 0x800,

            [Description(nameof(Localize.LineStyle_LaneGroup))]
            Lane = 0x1000,
        }

        protected class EnterDic<T> : Dictionary<int, T>
        {
            public T this[int i, int j]
            {
                get => this[GetId(i, j)];
                set => this[GetId(i, j)] = value;
            }
            private int GetId(int i, int j) => (i + 1) * 10 + j + 1;

            public bool TryGetValue(int i, int j, out T value) => TryGetValue(GetId(i, j), out value);
        }
    }
    public abstract class Marking<EnterType> : Marking
        where EnterType : Entrance
    {
        public new IEnumerable<EnterType> Enters => EntersList.Cast<EnterType>();
        public bool TryGetEnter(ushort enterId, out EnterType enter)
        {
            if (base.TryGetEnter(enterId, out var e))
            {
                enter = e as EnterType;
                return enter != null;
            }
            else
            {
                enter = null;
                return false;
            }
        }
        public Marking(ushort id) : base(id)
        {
            Update();
        }
    }
    public enum MarkingType
    {
        Node,
        Segment
    }
}
