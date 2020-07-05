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
        Dictionary<ushort, Enter> EntersDictionary { get; set; } = new Dictionary<ushort, Enter>();
        Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        Dictionary<MarkupLinePair, LineIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, LineIntersect>(new MarkupLinePairComparer());

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
        public IEnumerable<Enter> Enters
        {
            get
            {
                foreach (var enter in EntersDictionary.Values)
                    yield return enter;
            }
        }
        public bool TryGetLine(ulong lineId, out MarkupLine line) => LinesDictionary.TryGetValue(lineId, out line);
        public bool TryGetEnter(ushort enterId, out Enter enter) => EntersDictionary.TryGetValue(enterId, out enter);

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

            var enters = new Dictionary<ushort, Enter>();

            foreach (var segmentId in node.SegmentsId())
            {
                if (!EntersDictionary.TryGetValue(segmentId, out Enter enter))
                    enter = new Enter(this, segmentId);

                enter.Update();

                enters.Add(segmentId, enter);
            }

            EntersDictionary = enters;

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
                if (EntersDictionary.ContainsKey(line.Start.Enter.Id) && EntersDictionary.ContainsKey(line.End.Enter.Id))
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

            var intersects = GetExistIntersects(line);
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
        public LineIntersect[] GetExistIntersects(MarkupLine line)
        {
            var intersects = LineIntersects.Values.Where(i => i.Pair.ContainLine(line)).ToArray();
            return intersects;
        }
        public LineIntersect[] GetIntersects(MarkupLine line)
        {
            var intersects = Lines.Where(l => l != line).Select(l => GetIntersect(new MarkupLinePair(line, l))).ToArray();
            return intersects;
        }

        public LineIntersect GetIntersect(MarkupLinePair linePair)
        {
            if (!LineIntersects.TryGetValue(linePair, out LineIntersect intersect))
            {
                MarkupLineIntersect.Calculate(linePair, out intersect);
                LineIntersects.Add(linePair, intersect);
            }

            return intersect;
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id.ToString())
            );

            foreach(var enter in Enters)
            {
                foreach(var point in enter.Points)
                {
                    var pointConfig = point.ToXml();
                    config.Add(pointConfig);
                }
            }
            foreach(var line in Lines)
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
        public void FromXml(XElement config)
        {
            var nodeId = config.GetAttrValue<ushort>(nameof(Id));
            var markup = MarkupManager.Get(nodeId);

            foreach (var pointConfig in config.Elements(MarkupPoint.XmlName))
            {
                MarkupPoint.FromXml(pointConfig, this);
            }

            foreach (var lineConfig in config.Elements(MarkupLine.XmlName))
            {
                if(MarkupLine.FromXml(lineConfig, this, out MarkupLine line))
                    LinesDictionary.Add(line.Id, line);
            }

            foreach (var lineConfig in config.Elements(MarkupLine.XmlName))
            {
                var lineId = lineConfig.GetAttrValue<ulong>(nameof(MarkupLine.Id));
                if (TryGetLine(lineId, out MarkupLine line))
                    line.FromXml(lineConfig);
            }
        }
    }
}
