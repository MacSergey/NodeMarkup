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
        #region STATIC

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

        #endregion

        #region PROPERTIES

        public string XmlSection => XmlName;
        public ushort Id { get; }
        public float Height { get; private set; }
        List<Enter> EntersList { get; set; } = new List<Enter>();
        Dictionary<ulong, MarkupLine> LinesDictionary { get; } = new Dictionary<ulong, MarkupLine>();
        Dictionary<MarkupLinePair, MarkupLineIntersect> LineIntersects { get; } = new Dictionary<MarkupLinePair, MarkupLineIntersect>(MarkupLinePair.Comparer);
        List<MarkupFiller> FillersList { get; } = new List<MarkupFiller>();

        public bool NeedRecalculateBatches { get; set; }
        public RenderBatch[] RenderBatches { get; private set; } = new RenderBatch[0];

        public IEnumerable<MarkupLine> Lines => LinesDictionary.Values;
        public IEnumerable<Enter> Enters => EntersList;
        public IEnumerable<MarkupFiller> Fillers => FillersList;
        public IEnumerable<MarkupLineIntersect> Intersects => GetAllIntersect().Where(i => i.IsIntersect);

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

            RecalculateDashes();
        }

        private void UpdateEnters()
        {
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
        }
        private void UpdateLines()
        {
            foreach (var line in LinesDictionary.Values.ToArray())
            {
                if (ContainsEnter(line.Start.Enter.Id) && ContainsEnter(line.End.Enter.Id))
                    line.UpdateTrajectory();
                else
                    RemoveLine(line);
            }
        }
        private void UpdateFillers()
        {
            foreach (var filler in FillersList)
                filler.Update();
        }

        public void Update(MarkupPoint point)
        {
            point.Update();

            foreach (var line in GetPointLines(point))
            {
                line.UpdateTrajectory();
            }
            foreach(var filler in GetPointFillers(point))
            {
                filler.Update();
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

        public void Clear()
        {
            LinesDictionary.Clear();
            FillersList.Clear();

            RecalculateDashes();
        }

        #endregion

        #region RECALCULATE

        public void RecalculateDashes()
        {
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
        }
        public void RecalculateBatches()
        {
            var dashes = new List<MarkupStyleDash>();
            dashes.AddRange(Lines.SelectMany(l => l.Dashes));
            dashes.AddRange(Fillers.SelectMany(f => f.Dashes));
            RenderBatches = RenderBatch.FromDashes(dashes).ToArray();
        }

        #endregion

        #region LINES

        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);
        public MarkupLine ToggleConnection(MarkupPointPair pointPair, Style.StyleType lineType)
        {
            if (LinesDictionary.TryGetValue(pointPair.Hash, out MarkupLine line))
            {
                RemoveConnect(line);
                return null;
            }
            else
            {
                line = new MarkupLine(this, pointPair, lineType);
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
                intersect.Pair.GetOther(line).RemoveRules(line);
                LineIntersects.Remove(intersect.Pair);
            }
            foreach (var filler in GetLineFillers(line).ToArray())
            {
                FillersList.Remove(filler);
            }

            LinesDictionary.Remove(line.PointPair.Hash);
        }

        #endregion

        #region GET & CONTAINS

        public bool TryGetLine(ulong lineId, out MarkupLine line) => LinesDictionary.TryGetValue(lineId, out line);
        public bool TryGetEnter(ushort enterId, out Enter enter)
        {
            enter = EntersList.Find(e => e.Id == enterId);
            return enter != null;
        }
        public bool ContainsEnter(ushort enterId) => EntersList.Find(e => e.Id == enterId) != null;
        public bool ContainsLine(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair.Hash);

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

        public IEnumerable<MarkupLine> GetPointLines(MarkupPoint point) => LinesDictionary.Values.Where(l => l.ContainsPoint(point));
        public IEnumerable<MarkupFiller> GetLineFillers(MarkupLine line) => FillersList.Where(f => f.ContainsLine(line));
        public IEnumerable<MarkupFiller> GetPointFillers(MarkupPoint point) => FillersList.Where(f => f.ContainsPoint(point));

        #endregion

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

        #region XML

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
            foreach (var filler in Fillers)
            {
                var fillerConfig = filler.ToXml();
                config.Add(fillerConfig);
            }

            return config;
        }
        public static bool FromXml(string version, XElement config, out Markup markup)
        {
            var nodeId = config.GetAttrValue<ushort>(nameof(Id));
            markup = MarkupManager.Get(nodeId);
            markup.FromXml(version, config);
            return true;
        }
        public void FromXml(string version, XElement config, Dictionary<ObjectId, ObjectId> map = null)
        {
            if (VersionComparer.Instance.Compare(version, "1.2") < 0)
            {
                map = VersionMigration.Befor1_2(this, map);
            }

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

            foreach (var fillerConfig in config.Elements(MarkupFiller.XmlName))
            {
                if (MarkupFiller.FromXml(fillerConfig, this, map, out MarkupFiller filler))
                    FillersList.Add(filler);
            }
        }

        #endregion XML


    }
}
