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
        List<Enter> EntersList { get; set; } = new List<Enter>();
        Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        Dictionary<MarkupLinePair, LineIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, LineIntersect>(MarkupLinePair.Comparer);

        public bool NeedRecalculate { get; set; }
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
        public IEnumerable<LineIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);
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
                    LinesDictionary[line.PointPair.Hash].Update();
                else
                    LinesDictionary.Remove(line.PointPair.Hash);
            }
#if DEBUG
            Logger.LogDebug($"End update lines");
#endif
        }

        public void Update(MarkupPoint point)
        {
            point.Update();
            foreach (var line in Lines.Where(l => l.ContainPoint(point)))
            {
                line.Update();
            }
            RecalculateDashes();
        }
        public void Update(MarkupLine line)
        {
            line.Update();
            line.RecalculateDashes();
            NeedRecalculate = true;
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
            NeedRecalculate = true;
#if DEBUG
            Logger.LogDebug($"End recalculate dashes");
#endif
        }

        public void RecalculateBatches()
        {
#if DEBUG
            Logger.LogDebug($"Start recalculate batches");
#endif
            var dashes = LinesDictionary.Values.SelectMany(l => l.Dashes.Where(d => d.Length > 0.1f)).ToArray();
            RenderBatches = RenderBatch.FromDashes(dashes).ToArray();
#if DEBUG
            Logger.LogDebug($"End recalculate batches: {RenderBatches.Length}; dashes: {dashes.Length}");
#endif
        }

        public MarkupLine AddConnect(MarkupPointPair pointPair, LineStyle.LineType lineType)
        {
            var newLine = new MarkupLine(this, pointPair, lineType);
            LinesDictionary[pointPair.Hash] = newLine;

            NeedRecalculate = true;

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
        public void Clear()
        {
            LinesDictionary.Clear();

            RecalculateDashes();
        }
        public MarkupLine ToggleConnection(MarkupPointPair pointPair, LineStyle.LineType lineType)
        {
            if (!ExistConnection(pointPair))
                return AddConnect(pointPair, lineType);
            else
            {
                RemoveConnect(pointPair);
                return null;
            }
        }
        public IEnumerable<LineIntersect> GetExistIntersects(MarkupLine line) => LineIntersects.Values.Where(i => i.Pair.ContainLine(line));
        public IEnumerable<LineIntersect> GetIntersects(MarkupLine line) => Lines.Where(l => l != line).Select(l => GetIntersect(new MarkupLinePair(line, l)));

        public LineIntersect GetIntersect(MarkupLinePair linePair)
        {
            if (!LineIntersects.TryGetValue(linePair, out LineIntersect intersect))
            {
                MarkupLineIntersect.Calculate(linePair, out intersect);
                LineIntersects.Add(linePair, intersect);
            }

            return intersect;
        }
        public IEnumerable<LineIntersect> GetAllIntersect()
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
