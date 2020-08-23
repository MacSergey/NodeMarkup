using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public interface IUpdate
    {
        void Update(bool onlySelfUpdate = false);
    }
    public interface IUpdate<Type>
        where Type : IUpdate
    {
        void Update(Type item, bool recalculate = false);
    }
    public class Markup : IUpdate<MarkupPoint>, IUpdate<MarkupLine>, IUpdate<MarkupFiller>, IUpdate<MarkupCrosswalk>, IToXml
    {
        #region STATIC

        public static string XmlName { get; } = "M";

        #endregion

        #region PROPERTIES

        public string XmlSection => XmlName;
        public ushort Id { get; }
        public float Height { get; private set; }

        List<Enter> EntersList { get; set; } = new List<Enter>();
        Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        Dictionary<MarkupLinePair, MarkupLinesIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, MarkupLinesIntersect>(MarkupLinePair.Comparer);
        List<MarkupFiller> FillersList { get; } = new List<MarkupFiller>();
        Dictionary<MarkupLine, MarkupCrosswalk> CrosswalksDictionary { get; } = new Dictionary<MarkupLine, MarkupCrosswalk>();
        List<Bezier3> ContourParts { get; set; } = new List<Bezier3>();

        public IEnumerable<MarkupLine> Lines => LinesDictionary.Values;
        public IEnumerable<Enter> Enters => EntersList;
        public IEnumerable<MarkupFiller> Fillers => FillersList;
        public IEnumerable<MarkupCrosswalk> Crosswalks => CrosswalksDictionary.Values;
        public IEnumerable<MarkupLinesIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);
        public IEnumerable<Bezier3> Contour => ContourParts;

        public bool NeedRecalculateBatches { get; set; }
        public RenderBatch[] RenderBatches { get; private set; } = new RenderBatch[0];

        #endregion

        public Markup(ushort nodeId)
        {
            Id = nodeId;
            Update();
        }

        #region UPDATE

        public void Update()
        {
            UpdateEnters();
            UpdateLines();
            UpdateFillers();
            UpdateCrosswalks();

            RecalculateDashes();
        }

        private void UpdateEnters()
        {
            var node = Utilities.GetNode(Id);
            Height = node.m_position.y;

            var oldEnters = EntersList;
            var exists = oldEnters.Select(e => e.Id).ToList();
            var update = node.SegmentsId().ToList();

            var still = exists.Intersect(update).ToArray();
            var delete = exists.Except(still).ToArray();
            var add = update.Except(still).ToArray();

            var newEnters = still.Select(id => oldEnters.Find(e => e.Id == id)).ToList();
            newEnters.AddRange(add.Select(id => new Enter(this, id)));
            newEnters.Sort((e1, e2) => e1.AbsoluteAngle.CompareTo(e2.AbsoluteAngle));

            if (delete.Length == 1 && add.Length == 1 && oldEnters.Find(e => e.Id == delete[0]).PointCount == newEnters.Find(e => e.Id == add[0]).PointCount)
            {
                var map = new Dictionary<ObjectId, ObjectId>()
                {
                    {new ObjectId() {Segment = delete[0] },  new ObjectId() {Segment = add[0] }}
                };

                var currentData = ToXml();
                EntersList = newEnters;
                Clear();
                FromXml(Mod.Version, currentData, map);
            }
            else
                EntersList = newEnters;

            foreach (var enter in EntersList)
                enter.Update();

            UpdateNodeСontour();

            foreach (var enter in EntersList)
                enter.UpdatePoints();
        }
        private void UpdateNodeСontour()
        {
            var contourParts = new List<Bezier3>();

            for (var i = 0; i < EntersList.Count; i += 1)
            {
                var prev = EntersList[i];
                var currentBezier = new Bezier3()
                {
                    a = prev.LeftSide,
                    d = prev.RightSide
                };
                var currentDir = (currentBezier.d - currentBezier.a).normalized;
                NetSegment.CalculateMiddlePoints(currentBezier.a, currentDir, currentBezier.d, -currentDir, true, true, out currentBezier.b, out currentBezier.c);
                contourParts.Add(currentBezier);


                var next = GetNextEnter(i);
                var betweenBezier = new Bezier3()
                {
                    a = prev.RightSide,
                    d = next.LeftSide
                };
                NetSegment.CalculateMiddlePoints(betweenBezier.a, prev.NormalDir, betweenBezier.d, next.NormalDir, true, true, out betweenBezier.b, out betweenBezier.c);
                contourParts.Add(betweenBezier);
            }

            ContourParts = contourParts;
        }

        private void UpdateLines()
        {
            foreach (var line in LinesDictionary.Values.ToArray())
            {
                if (ContainsEnter(line.Start.Enter.Id) && ContainsEnter(line.End.Enter.Id))
                    line.Update();
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

            if (recalculate)
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

            if (recalculate)
                RecalculateDashes();
        }
        public void Update(MarkupFiller filler, bool recalculate = false)
        {
            filler.Update();
            if (recalculate)
                RecalculateDashes();
        }
        public void Update(MarkupCrosswalk crosswalk, bool recalculate = false)
        {
            crosswalk.Line.Update();
            if (recalculate)
                RecalculateDashes();
        }

        public void Clear()
        {
            LinesDictionary.Clear();
            FillersList.Clear();
            CrosswalksDictionary.Clear();

            RecalculateDashes();
        }

        #endregion

        #region RECALCULATE

        public void RecalculateDashes()
        {
            LineIntersects.Clear();
            foreach (var line in Lines)
                line.RecalculateDashes();

            foreach (var filler in Fillers)
                filler.RecalculateDashes();

            foreach (var crosswalk in Crosswalks)
                crosswalk.RecalculateDashes();

            NeedRecalculateBatches = true;
        }
        public void RecalculateBatches()
        {
            var dashes = new List<MarkupStyleDash>();
            dashes.AddRange(Lines.SelectMany(l => l.Dashes));
            dashes.AddRange(Fillers.SelectMany(f => f.Dashes));
            dashes.AddRange(Crosswalks.SelectMany(c => c.Dashes));
            RenderBatches = RenderBatch.FromDashes(dashes).ToArray();
        }

        #endregion

        #region LINES

        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);
        public MarkupLine ToggleConnection(MarkupPointPair pointPair, Style.StyleType style)
        {
            if (LinesDictionary.TryGetValue(pointPair.Hash, out MarkupLine line))
            {
                RemoveConnect(line);
                return null;
            }
            else
            {
                line = MarkupLine.FromStyle(this, pointPair, style);
                LinesDictionary[pointPair.Hash] = line;
                NeedRecalculateBatches = true;
                return line;
            }
        }
        public void RemoveConnect(MarkupLine line)
        {
            RemoveLine(line);
            RecalculateDashes();
        }
        private void RemoveLine(MarkupLine line)
        {
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

            LinesDictionary.Remove(line.PointPair.Hash);
        }

        #endregion

        #region GET & CONTAINS

        public bool TryGetLine(ulong lineId, out MarkupLine line) => LinesDictionary.TryGetValue(lineId, out line);
        public bool TryGetLine<LineType>(ulong lineId, out LineType line)
            where LineType : MarkupLine
        {
            if (LinesDictionary.TryGetValue(lineId, out MarkupLine rawLine) && rawLine is LineType)
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
        public bool TryGetLine<LineType>(ulong lineId, Dictionary<ObjectId, ObjectId> map, out LineType line)
            where LineType : MarkupLine
        {
            if (MarkupPointPair.FromHash(lineId, this, map, out MarkupPointPair pair))
                return TryGetLine(pair.Hash, out line);
            else
            {
                line = null;
                return false;
            }
        }

        public bool TryGetEnter(ushort enterId, out Enter enter)
        {
            enter = EntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => EntersList.Find(e => e.Id == enterId) != null;
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
        public Enter GetNextEnter(int index) => EntersList[index == EntersList.Count - 1 ? 0 : index + 1];
        public Enter GetPrevEnter(Enter current) => GetPrevEnter(EntersList.IndexOf(current));
        public Enter GetPrevEnter(int index) => EntersList[index == 0 ? EntersList.Count - 1 : index - 1];

        public IEnumerable<MarkupLine> GetPointLines(MarkupPoint point) => Lines.Where(l => l.ContainsPoint(point));
        public IEnumerable<MarkupFiller> GetLineFillers(MarkupLine line) => FillersList.Where(f => f.ContainsLine(line));
        public IEnumerable<MarkupFiller> GetPointFillers(MarkupPoint point) => FillersList.Where(f => f.ContainsPoint(point));
        public IEnumerable<MarkupCrosswalk> GetPointCrosswalks(MarkupPoint point) => Crosswalks.Where(c => c.ContainsPoint(point));
        public IEnumerable<MarkupCrosswalk> GetLinesIsBorder(MarkupLine line) => Crosswalks.Where(c => c.IsBorder(line));

        #endregion

        #region FILLERS

        public void AddFiller(MarkupFiller filler)
        {
            FillersList.Add(filler);
            filler.RecalculateDashes();
            NeedRecalculateBatches = true;
        }
        public void RemoveFiller(MarkupFiller filler)
        {
            FillersList.Remove(filler);
            NeedRecalculateBatches = true;
        }

        #endregion

        #region CROSSWALK

        public void AddCrosswalk(MarkupCrosswalk crosswalk)
        {
            CrosswalksDictionary[crosswalk.Line] = crosswalk;
            crosswalk.RecalculateDashes();
            NeedRecalculateBatches = true;
        }
        public void RemoveCrosswalk(MarkupCrosswalk crosswalk) => RemoveConnect(crosswalk.Line);

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
        public static bool FromXml(Version version, XElement config, out Markup markup)
        {
            var nodeId = config.GetAttrValue<ushort>(nameof(Id));

            try
            {
                markup = MarkupManager.Get(nodeId);
                markup.FromXml(version, config);
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError(() => $"Could load node #{nodeId} markup", error);
                markup = null;
                MarkupManager.LoadErrors += 1;
                return false;
            }
        }
        public void FromXml(Version version, XElement config, Dictionary<ObjectId, ObjectId> map = null)
        {
            if (version < new Version("1.2"))
                map = VersionMigration.Befor1_2(this, map);

            foreach (var pointConfig in config.Elements(MarkupPoint.XmlName))
                MarkupPoint.FromXml(pointConfig, this, map);

            var toInit = new Dictionary<MarkupLine, XElement>();
            foreach (var lineConfig in config.Elements(MarkupLine.XmlName))
            {
                if (MarkupLine.FromXml(lineConfig, this, map, out MarkupLine line))
                {
                    LinesDictionary[line.Id] = line;
                    toInit[line] = lineConfig;
                }
            }

            foreach (var pair in toInit)
                pair.Key.FromXml(pair.Value, map);

            foreach (var fillerConfig in config.Elements(MarkupFiller.XmlName))
            {
                if (MarkupFiller.FromXml(fillerConfig, this, map, out MarkupFiller filler))
                    FillersList.Add(filler);
            }
            foreach (var crosswalkConfig in config.Elements(MarkupCrosswalk.XmlName))
            {
                if (MarkupCrosswalk.FromXml(crosswalkConfig, this, map, out MarkupCrosswalk crosswalk))
                    CrosswalksDictionary[crosswalk.Line] = crosswalk;
            }

            Update();
        }

        #endregion XML

        public enum Item
        {
            [Description(nameof(Localize.LineStyle_RegularGroup))]
            RegularLine = 0x100,

            [Description(nameof(Localize.LineStyle_StopGroup))]
            StopLine = 0x200,

            [Description(nameof(Localize.FillerStyle_Group))]
            Filler = 0x400,

            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = 0x800,
        }
    }
}
