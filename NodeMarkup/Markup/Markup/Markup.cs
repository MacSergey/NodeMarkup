using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using ObjectId = NodeMarkup.Utilities.ObjectId;
using static ColossalFramework.Math.VectorUtils;

namespace NodeMarkup.Manager
{
    public abstract class Markup : IUpdatePoints, IUpdateLines, IUpdateFillers, IUpdateCrosswalks, IToXml
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
        public abstract MarkupType Type { get; }
        public abstract SupportType Support { get; }
        public virtual MarkupLine.LineType SupportLines => MarkupLine.LineType.Regular;

        public ushort Id { get; }
        protected abstract bool IsExist { get; }

        public Vector3 Position { get; private set; }
        public Vector3 CenterPosition { get; private set; }
        public float CenterRadius { get; private set; }
        public float Radius { get; private set; }
        public float Height => Position.y;
        public abstract string XmlSection { get; }
        public abstract string PanelCaption { get; }

        protected List<Enter> RawEntersList { get; set; } = new List<Enter>();
        protected List<Enter> EntersList { get; set; } = new List<Enter>();
        protected Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        protected Dictionary<MarkupLinePair, MarkupLinesIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, MarkupLinesIntersect>(MarkupLinePair.Comparer);
        protected List<MarkupFiller> FillersList { get; } = new List<MarkupFiller>();
        protected Dictionary<MarkupLine, MarkupCrosswalk> CrosswalksDictionary { get; } = new Dictionary<MarkupLine, MarkupCrosswalk>();

        public bool IsEmpty => !LinesDictionary.Any() && !FillersList.Any();

        public IEnumerable<MarkupLine> Lines => LinesDictionary.Values;
        public IEnumerable<Enter> Enters => EntersList;
        public IEnumerable<MarkupFiller> Fillers => FillersList;
        public IEnumerable<MarkupCrosswalk> Crosswalks => CrosswalksDictionary.Values;
        public IEnumerable<MarkupLinesIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);

        public int EntersCount => EntersList.Count;
        public int LinesCount => LinesDictionary.Count;
        public int CrosswalksCount => CrosswalksDictionary.Count;
        public int FillersCount => FillersList.Count;


        public bool NeedRecalculateDrawData { get; private set; }
        public LodDictionaryArray<IDrawData> DrawData { get; } = new LodDictionaryArray<IDrawData>();

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

        public Markup(ushort id)
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
        protected void UpdateEnters()
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
            newEnters.AddRange(add.Select(id => NewEnter(id)));
            newEnters.AddRange(changed.Select(id => NewEnter(id)));

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
        protected abstract Enter NewEnter(ushort id);

        private void UpdateBackup(ushort[] delete, ushort[] add, ushort[] changed, List<Enter> oldEnters, List<Enter> newEnters)
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
        private bool UpdateBackup(ushort delete, ushort add, List<Enter> oldEnters, List<Enter> newEnters)
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

        public void Update(MarkupPoint point, bool recalculate = false, bool recalcDependences = false)
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
        public void Update(MarkupLine line, bool recalculate = false, bool recalcDependences = false)
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
                    LineIntersects.Remove(intersect.Pair);
                    var otherLine = intersect.Pair.GetOther(line);
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

                if (line is MarkupCrosswalkLine crosswalkLine)
                {
                    crosswalkLine.Crosswalk.Update(true);
                    toRecalculate.Add(crosswalkLine.Crosswalk);
                }
            }

            if (recalculate && !UpdateInProgress)
                RecalculateStyleData(toRecalculate);
        }
        public void Update(MarkupFiller filler, bool recalculate = false, bool recalcDependences = false)
        {
            if (LoadInProgress)
                return;

            filler.Update();
            if (recalculate && !UpdateInProgress)
                RecalculateStyleData(filler);
        }
        public void Update(MarkupCrosswalk crosswalk, bool recalculate = false, bool recalcDependences = false) => Update(crosswalk.CrosswalkLine, recalculate, recalcDependences);

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
            toRecalculate?.RecalculateStyleData();
            NeedRecalculateDrawData = true;
        }
        public void RecalculateStyleData(HashSet<IStyleItem> toRecalculate)
        {
            foreach (var item in toRecalculate)
                item.RecalculateStyleData();

            NeedRecalculateDrawData = true;
        }

        public void RecalculateDrawData()
        {
            DrawData.Clear();

            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
            {
                var dashes = new List<MarkupStylePart>();
                var drawData = new List<IDrawData>();

                Seporate(Lines.SelectMany(l => l.StyleData[lod]));
                Seporate(Fillers.SelectMany(f => f.StyleData[lod]));
                Seporate(Crosswalks.Select(c => c.StyleData[lod]));

                drawData.AddRange(RenderBatch.FromDashes(dashes));
                DrawData[lod] = drawData.ToArray();

                void Seporate(IEnumerable<IStyleData> stylesData)
                {
                    foreach (var styleData in stylesData)
                    {
                        if (styleData is IEnumerable<MarkupStylePart> styleDashes)
                            dashes.AddRange(styleDashes);
                        else if (styleData != null)
                            drawData.AddRange(styleData.GetDrawData());
                    }
                }
            }

            NeedRecalculateDrawData = false;
        }

        #endregion

        #region LINES

        public bool ExistLine(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

        private LineType AddLine<LineType>(MarkupPointPair pointPair, Func<LineType> newLine)
            where LineType : MarkupLine
        {
            if (!TryGetLine(pointPair, out LineType line))
            {
                line = newLine();
                LinesDictionary[pointPair.Hash] = line;
                RecalculateStyleData();
            }

            return line;
        }
        public MarkupRegularLine AddRegularLine(MarkupPointPair pointPair, RegularLineStyle style, Alignment alignment = Alignment.Centre)
            => AddLine(pointPair, () => pointPair.IsNormal ? new MarkupNormalLine(this, pointPair, style, alignment) : new MarkupRegularLine(this, pointPair, style, alignment));
        public MarkupStopLine AddStopLine(MarkupPointPair pointPair, StopLineStyle style) => AddLine(pointPair, () => new MarkupStopLine(this, pointPair, style));
        public MarkupCrosswalkLine AddCrosswalkLine(MarkupPointPair pointPair, CrosswalkStyle style) => AddLine(pointPair, () => new MarkupCrosswalkLine(this, pointPair, style));


        public void RemoveLine(MarkupLine line) => RemoveLine(line, true);
        private void RemoveLine(MarkupLine line, bool recalculate = false)
        {
            var toRecalculate = new HashSet<IStyleItem>();

            LinesDictionary.Remove(line.PointPair.Hash);

            foreach (var intersect in GetExistIntersects(line).ToArray())
            {
                if (intersect.Pair.GetOther(line) is MarkupRegularLine regularLine)
                {
                    if (regularLine.RemoveRules(line))
                        toRecalculate.Add(regularLine);
                }

                LineIntersects.Remove(intersect.Pair);
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
        public Dependences GetLineDependences(MarkupLine line)
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
                if (intersect.Pair.GetOther(line) is MarkupRegularLine regularLine)
                    dependences.Rules += regularLine.GetLineDependences(line);
            }

            return dependences;
        }

        #endregion

        #region FILLERS

        public void AddFiller(MarkupFiller filler)
        {
            FillersList.Add(filler);
            RecalculateStyleData(filler);
        }
        public void RemoveFiller(MarkupFiller filler)
        {
            FillersList.Remove(filler);
            RecalculateStyleData();
        }

        #endregion

        #region CROSSWALK

        public void AddCrosswalk(MarkupCrosswalk crosswalk)
        {
            CrosswalksDictionary[crosswalk.CrosswalkLine] = crosswalk;
            RecalculateStyleData(crosswalk);
        }
        public void RemoveCrosswalk(MarkupCrosswalk crosswalk) => RemoveLine(crosswalk.CrosswalkLine);
        public Dependences GetCrosswalkDependences(MarkupCrosswalk crosswalk)
        {
            var dependences = GetLineDependences(crosswalk.CrosswalkLine);
            dependences.Crosswalks = 0;
            dependences.Lines = crosswalk.CrosswalkLine.Rules.Any() ? 1 : 0;
            return dependences;
        }
        public void CutLinesByCrosswalk(MarkupCrosswalk crosswalk)
        {
            var enter = crosswalk.CrosswalkLine.Start.Enter;
            var lines = Lines.Where(l => l.Type == MarkupLine.LineType.Regular && l.PointPair.ContainsEnter(enter)).ToArray();

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

                    if ((line.End.Type == MarkupPoint.PointType.Enter && line.End.Enter == enter) ^ fromT < toT)
                        rule.From = new LinesIntersectEdge(intersect.Pair);
                    else
                        rule.To = new LinesIntersectEdge(intersect.Pair);
                }
            }
        }

        #endregion

        #region GET & CONTAINS

        public bool TryGetLine(MarkupPointPair pointPair, out MarkupLine line) => LinesDictionary.TryGetValue(pointPair.Hash, out line);
        public bool TryGetLine<LineType>(MarkupPoint first, MarkupPoint second, out LineType line) where LineType : MarkupLine => TryGetLine(new MarkupPointPair(first, second), out line);
        public bool TryGetLine<LineType>(MarkupPointPair pointPair, out LineType line)
            where LineType : MarkupLine
        {
            if (LinesDictionary.TryGetValue(pointPair.Hash, out MarkupLine rawLine) && rawLine is LineType)
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
            where LineType : MarkupLine
        {
            if (MarkupPointPair.FromHash(lineId, this, map, out MarkupPointPair pair, out _))
                return TryGetLine(pair, out line);
            else
            {
                line = null;
                return false;
            }
        }

        public bool TryGetEnter(ushort enterId, out Enter enter)
        {
            enter = RawEntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => RawEntersList.Find(e => e.Id == enterId) != null;
        public bool ContainsLine(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

        public IEnumerable<MarkupLinesIntersect> GetExistIntersects(MarkupLine line, bool onlyIntersect = false)
            => LineIntersects.Values.Where(i => i.Pair.ContainLine(line) && (!onlyIntersect || i.IsIntersect));
        public IEnumerable<MarkupLinesIntersect> GetIntersects(MarkupLine line)
        {
            foreach (var otherLine in Lines)
            {
                if (otherLine != line)
                    yield return GetIntersect(new MarkupLinePair(line, otherLine));
            }
        }

        public MarkupLinesIntersect GetIntersect(MarkupLine first, MarkupLine second) => GetIntersect(new MarkupLinePair(first, second));
        public MarkupLinesIntersect GetIntersect(MarkupLinePair linePair)
        {
            if (!LineIntersects.TryGetValue(linePair, out MarkupLinesIntersect intersect))
            {
                intersect = MarkupLinesIntersect.Calculate(linePair);
                LineIntersects.Add(linePair, intersect);
            }

            return intersect;
        }
        public IEnumerable<MarkupLinesIntersect> GetAllIntersect()
        {
            var lines = Lines.ToArray();
            for (var i = 0; i < lines.Length; i += 1)
            {
                for (var j = i + 1; j < lines.Length; j += 1)
                {
                    yield return GetIntersect(new MarkupLinePair(lines[i], lines[j]));
                }
            }
        }

        public Enter GetNextEnter(Enter current) => GetNextEnter(EntersList.IndexOf(current));
        public Enter GetNextEnter(int index) => EntersList[index.NextIndex(EntersList.Count)];
        public Enter GetPrevEnter(Enter current) => GetPrevEnter(EntersList.IndexOf(current));
        public Enter GetPrevEnter(int index) => EntersList[index.PrevIndex(EntersList.Count)];

        public IEnumerable<MarkupLine> GetPointLines(MarkupPoint point) => Lines.Where(l => l.ContainsPoint(point));
        public IEnumerable<MarkupFiller> GetLineFillers(MarkupLine line) => FillersList.Where(f => f.ContainsLine(line));
        public IEnumerable<MarkupFiller> GetPointFillers(MarkupPoint point) => FillersList.Where(f => f.ContainsPoint(point));
        public IEnumerable<MarkupCrosswalk> GetPointCrosswalks(MarkupPoint point) => Crosswalks.Where(c => c.ContainsPoint(point));
        public IEnumerable<MarkupCrosswalk> GetLinesIsBorder(MarkupLine line) => Crosswalks.Where(c => c.IsBorder(line));

        public bool HaveLines(Enter enter) => Lines.Any(l => l.ContainsEnter(enter));
        public bool HaveLines(MarkupPoint point) => Lines.Any(l => l.ContainsPoint(point));

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);

            foreach (var enter in Enters)
            {
                foreach (var point in enter.Points)
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


            foreach (var pointConfig in config.Elements(MarkupPoint.XmlName))
                MarkupPoint.FromXml(pointConfig, this, map);

            var toInitLines = new Dictionary<MarkupLine, XElement>();
            var invertLines = new HashSet<MarkupLine>();
            foreach (var lineConfig in config.Elements(MarkupLine.XmlName))
            {
                if (MarkupLine.FromXml(lineConfig, this, map, out MarkupLine line, out bool invertLine))
                {
                    LinesDictionary[line.Id] = line;
                    toInitLines[line] = lineConfig;
                    if (invertLine)
                        invertLines.Add(line);
                }
            }

            foreach (var pair in toInitLines)
                pair.Key.FromXml(pair.Value, map, invertLines.Contains(pair.Key));

            if ((Support & SupportType.Fillers) != 0)
            {
                foreach (var fillerConfig in config.Elements(MarkupFiller.XmlName))
                {
                    if (MarkupFiller.FromXml(fillerConfig, this, map, out MarkupFiller filler))
                        FillersList.Add(filler);
                }
            }

            if ((Support & SupportType.Croswalks) != 0)
            {
                foreach (var crosswalkConfig in config.Elements(MarkupCrosswalk.XmlName))
                {
                    if (MarkupCrosswalk.FromXml(crosswalkConfig, this, map, out MarkupCrosswalk crosswalk))
                        CrosswalksDictionary[crosswalk.CrosswalkLine] = crosswalk;
                }
            }

            LoadInProgress = false;

            if (needUpdate)
                Update();
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
            var newCentre = (points[i] + points[j]) / 2;
            var newRadius = (points[i] - points[j]).magnitude / 2;

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
            var pos1 = (points[i] + points[j]) / 2;
            var pos2 = (points[j] + points[k]) / 2;

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
    public abstract class Markup<EnterType> : Markup
        where EnterType : Enter
    {
        public new IEnumerable<EnterType> Enters => EntersList.Cast<EnterType>();

        public Markup(ushort id) : base(id)
        {
            Update();
        }
    }
    public enum MarkupType
    {
        Node,
        Segment
    }
}
