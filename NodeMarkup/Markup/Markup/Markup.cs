using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IRender
    {
        void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null);
    }
    public interface IDeletable
    {
        string DeleteCaptionDescription { get; }
        string DeleteMessageDescription { get; }
        Dependences GetDependences();
    }
    public interface ISupport { }
    public interface ISupport<Type> where Type : ISupport { }
    public interface ISupportPoints : ISupport<MarkupPoint> { }
    public interface ISupportEnters : ISupport<Enter> { }
    public interface ISupportLines : ISupport<MarkupLine> { }
    public interface ISupportFillers : ISupport<MarkupFiller> { }
    public interface ISupportCrosswalks : ISupport<MarkupCrosswalk> { }
    public interface ISupportStyleTemplate : ISupport<StyleTemplate> { }
    public interface ISupportIntersectionTemplate : ISupport<IntersectionTemplate> { }

    public interface IUpdate : ISupport
    {
        void Update(bool onlySelfUpdate = false);
    }
    public interface IUpdate<Type>
        where Type : IUpdate
    {
        void Update(Type item, bool recalculate = false);
    }
    public interface IUpdatePoints : IUpdate<MarkupPoint> { }
    public interface IUpdateLines : IUpdate<MarkupLine> { }
    public interface IUpdateFillers : IUpdate<MarkupFiller> { }
    public interface IUpdateCrosswalks : IUpdate<MarkupCrosswalk> { }

    public interface IItem : IUpdate, IDeletable, IRender { }

    public interface IStyleData
    {
        IEnumerable<IDrawData> GetDrawData();
    }
    public abstract class Markup : IUpdatePoints, IUpdateLines, IUpdateFillers, IUpdateCrosswalks, ISupportEnters, ISupportPoints, ISupportLines, ISupportFillers, ISupportStyleTemplate, IToXml
    {
        #region PROPERTIES
        public abstract MarkupType Type { get; }
        public virtual MarkupLine.LineType SupportLines => MarkupLine.LineType.Regular;

        public ushort Id { get; }
        public Vector3 Position { get; private set; }
        public float Radius { get; private set; }
        public float Height => Position.y;
        public abstract string XmlSection { get; }
        public abstract string PanelCaption { get; }

        protected List<Enter> RowEntersList { get; set; } = new List<Enter>();
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

        public int LinesCount => LinesDictionary.Count;
        public int CrosswalksCount => CrosswalksDictionary.Count;
        public int FillersCount => FillersList.Count;


        public bool NeedRecalculateDrawData { get; set; }
        public Dictionary<int,List<IDrawData>> DrawData { get; private set; } = new Dictionary<int, List<IDrawData>>();

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

        #endregion

        public Markup(ushort id)
        {
            Id = id;
        }

        #region UPDATE

        private bool UpdateProgress { get; set; } = false;
        public void Update()
        {
            if (UpdateProgress)
                return;

            UpdateProgress = true;

            UpdateEnters();
            UpdateLines();
            UpdateFillers();
            UpdateCrosswalks();

            RecalculateDashes();

            UpdateProgress = false;
        }
        protected void UpdateEnters()
        {
            Position = GetPosition();

            var oldEnters = RowEntersList;
            var before = oldEnters.Select(e => e.Id).ToList();
            var after = GetEnters().ToList();

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

            RowEntersList = newEnters;
            EntersList = RowEntersList.Where(e => e.PointCount != 0).ToList();

            UpdateRadius();
            UpdateEntersProcess();

            foreach (var enter in EntersList)
                enter.UpdatePoints();
        }
        protected virtual void UpdateEntersProcess() { }
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
            RowEntersList = newEnters;
            Clear();
            FromXml(Mod.Version, currentData, map);
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
                if (Backup.Map.FirstOrDefault(p => p.Value.Type == ObjectType.Segment && p.Value.Segment == delete) is KeyValuePair<ObjectId, ObjectId> pair)
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

        public void Update(MarkupPoint point, bool recalculate = false)
        {
            point.Update();

            foreach (var line in GetPointLines(point))
                line.Update();

            foreach (var filler in GetPointFillers(point))
                filler.Update();

            foreach (var crosswalk in GetPointCrosswalks(point))
                crosswalk.Update();

            if (recalculate && !UpdateProgress)
                RecalculateDashes();
        }
        public void Update(MarkupLine line, bool recalculate = false)
        {
            line.Update(true);

            foreach (var intersect in GetExistIntersects(line).ToArray())
            {
                LineIntersects.Remove(intersect.Pair);
                intersect.Pair.GetOther(line).Update();
            }

            foreach (var filler in GetLineFillers(line))
                filler.Update();

            foreach (var crosswalk in GetLinesIsBorder(line))
                crosswalk.Update();

            if (recalculate && !UpdateProgress)
                RecalculateDashes();
        }
        public void Update(MarkupFiller filler, bool recalculate = false)
        {
            filler.Update();
            if (recalculate && !UpdateProgress)
                RecalculateDashes();
        }
        public void Update(MarkupCrosswalk crosswalk, bool recalculate = false)
        {
            crosswalk.Line.Update();
            if (recalculate && !UpdateProgress)
                RecalculateDashes();
        }

        public void Clear()
        {
            LinesDictionary.Clear();
            FillersList.Clear();
            CrosswalksDictionary.Clear();
            NeedSetOrder = false;

            RecalculateDashes();
        }
        public void ResetOffsets()
        {
            foreach (var enter in Enters)
                enter.ResetOffsets();
        }

        #endregion

        #region RECALCULATE

        public void RecalculateDashes()
        {
            LineIntersects.Clear();
            foreach (var line in Lines)
                line.RecalculateStyleData();

            foreach (var filler in Fillers)
                filler.RecalculateStyleData();

            foreach (var crosswalk in Crosswalks)
                crosswalk.RecalculateStyleData();

            NeedRecalculateDrawData = true;
        }
        public void RecalculateDrawData()
        {
            DrawData.Clear();

            for (var i = 0; i <= 1; i += 1)
            {
                var dashes = new List<MarkupStylePart>();
                var drawData = new List<IDrawData>();

                Seporate(Lines.SelectMany(l => l.StyleData[i]));
                Seporate(Fillers.Select(l => l.StyleData[i]));
                Seporate(Crosswalks.Select(l => l.StyleData[i]));

                drawData.AddRange(RenderBatch.FromDashes(dashes));
                DrawData.Add(i, drawData);

                void Seporate(IEnumerable<IStyleData> stylesData)
                {
                    foreach (var styleData in stylesData)
                    {
                        if (styleData is IEnumerable<MarkupStylePart> styleDashes)
                            dashes.AddRange(styleDashes);
                        else
                            drawData.AddRange(styleData.GetDrawData());
                    }
                }
            }
        }

        #endregion

        #region LINES

        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

        public MarkupLine AddConnection(MarkupPointPair pointPair, Style.StyleType style)
        {
            if (!TryGetLine(pointPair, out MarkupLine line))
            {
                line = MarkupLine.FromStyle(this, pointPair, style);
                LinesDictionary[pointPair.Hash] = line;
                NeedRecalculateDrawData = true;
            }

            return line;
        }
        public void RemoveConnect(MarkupLine line)
        {
            RemoveLine(line);
            RecalculateDashes();
        }
        private void RemoveLine(MarkupLine line)
        {
            LinesDictionary.Remove(line.PointPair.Hash);

            foreach (var intersect in GetExistIntersects(line).ToArray())
            {
                if (intersect.Pair.GetOther(line) is MarkupRegularLine regularLine)
                    regularLine.RemoveRules(line);

                LineIntersects.Remove(intersect.Pair);
            }
            foreach (var filler in GetLineFillers(line).ToArray())
                FillersList.Remove(filler);

            if (CrosswalksDictionary.ContainsKey(line))
                CrosswalksDictionary.Remove(line);
            else
            {
                foreach (var crosswalk in GetLinesIsBorder(line))
                    crosswalk.RemoveBorder(line);
            }
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
            filler.RecalculateStyleData();
            NeedRecalculateDrawData = true;
        }
        public void RemoveFiller(MarkupFiller filler)
        {
            FillersList.Remove(filler);
            NeedRecalculateDrawData = true;
        }

        #endregion

        #region CROSSWALK

        public void AddCrosswalk(MarkupCrosswalk crosswalk)
        {
            CrosswalksDictionary[crosswalk.Line] = crosswalk;
            crosswalk.RecalculateStyleData();
            NeedRecalculateDrawData = true;
        }
        public void RemoveCrosswalk(MarkupCrosswalk crosswalk) => RemoveConnect(crosswalk.Line);
        public Dependences GetCrosswalkDependences(MarkupCrosswalk crosswalk)
        {
            var dependences = GetLineDependences(crosswalk.Line);
            dependences.Crosswalks = 0;
            dependences.Lines = 1;
            return dependences;
        }
        public void CutLinesByCrosswalk(MarkupCrosswalk crosswalk)
        {
            var enter = crosswalk.Line.Start.Enter;
            var lines = Lines.Where(l => l.Type == MarkupLine.LineType.Regular && l.PointPair.ContainsEnter(enter)).ToArray();

            foreach (var line in lines)
            {
                if (Settings.NotCutBordersByCrosswalk && crosswalk.IsBorder(line))
                    continue;

                var intersect = GetIntersect(line, crosswalk.Line);
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
            enter = RowEntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => RowEntersList.Find(e => e.Id == enterId) != null;
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

        #endregion

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlSection, new XAttribute(nameof(Id), Id));

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

        public virtual void FromXml(Version version, XElement config, ObjectsMap map)
        {
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

            if (this is ISupportFillers)
            {
                foreach (var fillerConfig in config.Elements(MarkupFiller.XmlName))
                {
                    if (MarkupFiller.FromXml(fillerConfig, this, map, out MarkupFiller filler))
                        FillersList.Add(filler);
                }
            }

            if (this is ISupportCrosswalks)
            {
                foreach (var crosswalkConfig in config.Elements(MarkupCrosswalk.XmlName))
                {
                    if (MarkupCrosswalk.FromXml(crosswalkConfig, this, map, out MarkupCrosswalk crosswalk))
                        CrosswalksDictionary[crosswalk.Line] = crosswalk;
                }
            }

            Update();
        }

        #endregion

        public override string ToString() => $"{Id}:{RowEntersList.Count}";

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
        public enum MarkupType
        {
            NodeMarkup,
            SegmentMarkup
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
}
