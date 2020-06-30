using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class Markup
    {
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

        public ushort NodeId { get; }
        Dictionary<ushort, SegmentEnter> EntersDictionary { get; set; } = new Dictionary<ushort, SegmentEnter>();
        Dictionary<MarkupPointPair, MarkupLine> LinesDictionary { get; } = new Dictionary<MarkupPointPair, MarkupLine>(new MarkupPointPairComparer());
        Dictionary<MarkupLinePair, LineIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, LineIntersect>(new MarkupLinePairComparer());

        public RenderBatch[] RenderBatches { get; private set; }


        public IEnumerable<MarkupLine> Lines
        {
            get
            {
                foreach (var line in LinesDictionary.Values)
                    yield return line;
            }
        }
        public IEnumerable<SegmentEnter> Enters
        {
            get
            {
                foreach (var enter in EntersDictionary.Values)
                    yield return enter;
            }
        }

        public Markup(ushort nodeId)
        {
            NodeId = nodeId;

            Update();
        }

        public void Update()
        {
            //Logger.LogDebug($"End update node #{NodeId}");

            UpdateEnters();
            UpdateLines();

            RecalculateDashes();

            //Logger.LogDebug($"End update node #{NodeId}");
        }
        private void UpdateEnters()
        {
            var node = Utilities.GetNode(NodeId);

            var enters = new Dictionary<ushort, SegmentEnter>();

            foreach (var segmentId in node.SegmentsId())
            {
                if (!EntersDictionary.TryGetValue(segmentId, out SegmentEnter enter))
                    enter = new SegmentEnter(this, segmentId);

                enter.Update();

                enters.Add(segmentId, enter);
            }

            EntersDictionary = enters;
        }
        private void UpdateLines()
        {
            var pointPairs = LinesDictionary.Keys.ToArray();
            foreach (var pointPair in pointPairs)
            {
                if (EntersDictionary.ContainsKey(pointPair.First.Enter.SegmentId) && EntersDictionary.ContainsKey(pointPair.Second.Enter.SegmentId))
                    LinesDictionary[pointPair].Update();
                else
                    LinesDictionary.Remove(pointPair);
            }
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
            RecalculateBatches();
        }

        public void RecalculateDashes()
        {
            LineIntersects.Clear();
            foreach (var line in Lines)
            {
                line.RecalculateDashes();
            }
            RecalculateBatches();
        }
        public void RecalculateBatches()
        {
            var dashes = LinesDictionary.Values.SelectMany(l => l.Dashes.Where(d => d.Length > 0.1f)).ToArray();
            RenderBatches = RenderBatch.FromDashes(dashes);
        }

        public MarkupLine AddConnect(MarkupPointPair pointPair, LineStyle.Type lineType)
        {
            var newLine = new MarkupLine(this, pointPair, lineType);
            LinesDictionary[pointPair] = newLine;

            RecalculateBatches();

            return newLine;
        }
        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair);
        public void RemoveConnect(MarkupPointPair pointPair)
        {
            var line = LinesDictionary[pointPair];

            var intersects = GetExistIntersects(line);
            foreach (var intersect in intersects)
            {
                var intersectLine = intersect.Pair.GetOther(line);
                intersectLine.RemoveRules(line);
                LineIntersects.Remove(intersect.Pair);
            }

            LinesDictionary.Remove(pointPair);

            RecalculateDashes();
        }
        public MarkupLine ToggleConnection(MarkupPointPair pointPair, LineStyle.Type lineType)
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
    }
}
