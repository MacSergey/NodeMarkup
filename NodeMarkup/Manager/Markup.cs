using ColossalFramework.Math;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class Markup : IToXml
    {
        public static string XmlName { get; } = "M";

        public static Color32[] OverlayColors { get; } = new Color32[]
        {
            new Color32(204, 0, 0, 224),
            new Color32(0, 204, 0, 224),
            new Color32(0, 0, 204, 224),
            new Color32(204, 0, 255, 224),
            new Color32(255, 204, 0, 224),
            new Color32(0, 255, 204, 224),
            new Color32(204, 255, 0, 224),
            new Color32(0, 204, 255, 224),
            new Color32(255, 0, 204, 224),
        };

        public ushort Id { get; }
        public float Height { get; private set; }
        List<Enter> EntersList { get; set; } = new List<Enter>();
        Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        Dictionary<MarkupLinePair, MarkupLineIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, MarkupLineIntersect>(MarkupLinePair.Comparer);
        List<MarkupFiller> FillersList { get; } = new List<MarkupFiller>();

        public bool NeedRecalculateBatches { get; set; }
        public RenderBatch[] RenderBatches { get; private set; } = new RenderBatch[0];


        public IEnumerable<MarkupLine> Lines
        {
            get
            {
                foreach (var line in LinesDictionary.Values)
                    yield return line;
            }
        }
        public IEnumerable<Enter> Enters => EntersList;
        public IEnumerable<MarkupFiller> Fillers => FillersList;
        public IEnumerable<MarkupLineIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);

        public bool TryGetLine(ulong lineId, out MarkupLine line) => LinesDictionary.TryGetValue(lineId, out line);
        public bool TryGetEnter(ushort enterId, out Enter enter)
        {
            enter = EntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => EntersList.Find(e => e.Id == enterId) != null;

        public string XmlSection => XmlName;

        public Markup(ushort nodeId)
        {
            Id = nodeId;

            Update();
        }

        public void Update()
        {
#if DEBUG
            Logger.LogDebug($"Start update node #{Id}");
#endif
            UpdateEnters();
            UpdateLines();
            UpdateFillers();

            RecalculateDashes();
#if DEBUG
            Logger.LogDebug($"End update node #{Id}");
#endif
        }
        private void UpdateEnters()
        {
#if DEBUG
            Logger.LogDebug($"Start update enters");
#endif
            var node = Utilities.GetNode(Id);
            Height = node.m_position.y;

            var enters = new List<Enter>();

            foreach (var segmentId in node.SegmentsId())
            {
                if (!TryGetEnter(segmentId, out Enter enter))
                    enter = new Enter(this, segmentId);

                enter.Update();
                enters.Add(enter);
            }
            enters.Sort((e1, e2) => e1.CornerAngle.CompareTo(e2.CornerAngle));
            EntersList = enters;

#if DEBUG
            Logger.LogDebug($"End update enters");
#endif
        }
        private void UpdateLines()
        {
#if DEBUG
            Logger.LogDebug($"Start update lines");
#endif
            var lines = LinesDictionary.Values.ToArray();
            foreach (var line in lines)
            {
                if (ContainsEnter(line.Start.Enter.Id) && ContainsEnter(line.End.Enter.Id))
                    LinesDictionary[line.PointPair.Hash].UpdateTrajectory();
                else
                    LinesDictionary.Remove(line.PointPair.Hash);
            }
#if DEBUG
            Logger.LogDebug($"End update lines");
#endif
        }
        private void UpdateFillers()
        {
#if DEBUG
            Logger.LogDebug($"Start update fillers");
#endif
            var fillers = FillersList.ToArray();
            foreach (var filler in fillers)
            {
                filler.Update();
            }
#if DEBUG
            Logger.LogDebug($"End update fillers");
#endif
        }

        public void Update(MarkupPoint point)
        {
            point.Update();
            foreach (var line in Lines.Where(l => l.ContainPoint(point)))
            {
                line.UpdateTrajectory();
            }
            RecalculateDashes();
        }
        public void Update(MarkupLine line)
        {
            line.UpdateTrajectory();
            line.RecalculateDashes();
            NeedRecalculateBatches = true;
        }
        public void Update(MarkupFiller filler)
        {
            filler.RecalculateDashes();
            NeedRecalculateBatches = true;
        }

        public void RecalculateDashes()
        {
#if DEBUG
            Logger.LogDebug($"Start recalculate dashes");
#endif
            LineIntersects.Clear();
            foreach (var line in Lines)
            {
                line.RecalculateDashes();
            }
            foreach (var filler in Fillers)
            {
                filler.RecalculateDashes();
            }
            NeedRecalculateBatches = true;
#if DEBUG
            Logger.LogDebug($"End recalculate dashes");
#endif
        }

        public void RecalculateBatches()
        {
#if DEBUG
            Logger.LogDebug($"Start recalculate batches");
#endif
            var dashes = new List<MarkupStyleDash>();
            dashes.AddRange(Lines.SelectMany(l => l.Dashes));
            dashes.AddRange(Fillers.SelectMany(f => f.Dashes));
            RenderBatches = RenderBatch.FromDashes(dashes).ToArray();
#if DEBUG
            Logger.LogDebug($"End recalculate batches: {RenderBatches.Length}; dashes: {dashes.Count}");
#endif
        }

        public MarkupLine AddConnect(MarkupPointPair pointPair, LineStyle.StyleType lineType)
        {
            var newLine = new MarkupLine(this, pointPair, lineType);
            LinesDictionary[pointPair.Hash] = newLine;

            NeedRecalculateBatches = true;

            return newLine;
        }
        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);
        public void RemoveConnect(MarkupPointPair pointPair)
        {
            var line = LinesDictionary[pointPair.Hash];

            var intersects = GetExistIntersects(line).ToArray();
            foreach (var intersect in intersects)
            {
                var intersectLine = intersect.Pair.GetOther(line);
                intersectLine.RemoveRules(line);
                LineIntersects.Remove(intersect.Pair);
            }

            LinesDictionary.Remove(pointPair.Hash);

            RecalculateDashes();
        }
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
        public void Clear()
        {
            LinesDictionary.Clear();

            RecalculateDashes();
        }
        public MarkupLine ToggleConnection(MarkupPointPair pointPair, LineStyle.StyleType lineType)
        {
            if (!ExistConnection(pointPair))
                return AddConnect(pointPair, lineType);
            else
            {
                RemoveConnect(pointPair);
                return null;
            }
        }
        public IEnumerable<MarkupLineIntersect> GetExistIntersects(MarkupLine line) => LineIntersects.Values.Where(i => i.Pair.ContainLine(line));
        public IEnumerable<MarkupLineIntersect> GetIntersects(MarkupLine line) => Lines.Where(l => l != line).Select(l => GetIntersect(new MarkupLinePair(line, l)));

        public MarkupLineIntersect GetIntersect(MarkupLinePair linePair)
        {
            if (!LineIntersects.TryGetValue(linePair, out MarkupLineIntersect intersect))
            {
                MarkupLineIntersect.Calculate(linePair, out intersect);
                LineIntersects.Add(linePair, intersect);
            }

            return intersect;
        }
        public IEnumerable<MarkupLineIntersect> GetAllIntersect()
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

        public Enter GetNextEnter(Enter current)
        {
            var index = EntersList.IndexOf(current);
            return EntersList[index == EntersList.Count - 1 ? 0 : index + 1];
        }
        public Enter GetPrevEnter(Enter current)
        {
            var index = EntersList.IndexOf(current);
            return EntersList[index == 0 ? EntersList.Count - 1 : index - 1];
        }
        public IEnumerable<MarkupLine> GetLinesFromPoint(MarkupPoint point)
        {
            foreach (var line in LinesDictionary.Values)
            {
                if (line.ContainPoint(point))
                    yield return line;
            }
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id.ToString())
            );

            foreach (var enter in Enters)
            {
                foreach (var point in enter.Points)
                {
                    var pointConfig = point.ToXml();
                    config.Add(pointConfig);
                }
            }
            foreach (var line in Lines)
            {
                var lineConfig = line.ToXml();
                config.Add(lineConfig);
            }

            return config;
        }
        public static bool FromXml(XElement config, out Markup markup)
        {
            var nodeId = config.GetAttrValue<ushort>(nameof(Id));
            markup = MarkupManager.Get(nodeId);
            markup.FromXml(config);
            return true;
        }
        public void FromXml(XElement config, Dictionary<InstanceID, InstanceID> map = null)
        {
            foreach (var pointConfig in config.Elements(MarkupPoint.XmlName))
            {
                MarkupPoint.FromXml(pointConfig, this, map);
            }

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
            {
                pair.Key.FromXml(pair.Value, map);
            }
        }
    }
}
