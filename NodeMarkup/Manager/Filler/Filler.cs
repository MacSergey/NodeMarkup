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
    public class MarkupFiller
    {
        public Markup Markup { get; }

        FillerStyle _style;
        public FillerStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = OnStyleChanged;
                OnStyleChanged();
            }
        }

        List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => VertexCount >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IEnumerable<IFillerVertex> Vertices => SupportPoints;
        public int VertexCount => SupportPoints.Count;

        public bool IsDone => VertexCount >= 3 && First.Equals(Last);

        List<MarkupLinePart> LineParts { get; } = new List<MarkupLinePart>();
        public IEnumerable<MarkupLinePart> Parts => LineParts;
        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];

        public Rect Rect
        {
            get
            {
                if (!IsDone)
                    return Rect.zero;

                var firstPos = First.Position;
                var rect = Rect.MinMaxRect(firstPos.x, firstPos.z, firstPos.x, firstPos.z);

                foreach (var part in LineParts)
                {
                    var trajectory = part.GetTrajectory();
                    Set(trajectory.a);
                    Set(trajectory.b);
                    Set(trajectory.c);
                    Set(trajectory.d);
                }

                return rect;

                void Set(Vector3 pos)
                {
                    if (pos.x < rect.xMin)
                        rect.xMin = pos.x;
                    else if (pos.x > rect.xMax)
                        rect.xMax = pos.x;

                    if (pos.z < rect.yMin)
                        rect.yMin = pos.z;
                    else if (pos.z > rect.yMax)
                        rect.yMax = pos.z;
                }
            }
        }


        public MarkupFiller(Markup markup, FillerStyle style)
        {
            Markup = markup;
            Style = style;
        }
        public MarkupFiller(Markup markup, FillerStyle.FillerType fillerType) : this(markup, FillerStyle.GetDefault(fillerType)) { }

        public void Add(IFillerVertex supportPoint)
        {
            SupportPoints.Add(supportPoint);
            if (VertexCount >= 2)
                LineParts.Add(GetFillerLine(Last, Prev));
        }
        public void Remove()
        {
            if (SupportPoints.Any())
                SupportPoints.RemoveAt(SupportPoints.Count - 1);
            if (LineParts.Any())
                LineParts.RemoveAt(LineParts.Count - 1);
        }

        private void OnStyleChanged() => Markup.Update(this);

        public FillerLinePart GetFillerLine(IFillerVertex first, IFillerVertex second)
        {
            var line = first.GetCommonLine(second);
            var linePart = new FillerLinePart(line, first.GetPartEdge(line), second.GetPartEdge(line));
            return linePart;
        }
        public IEnumerable<IFillerVertex> GetNextСandidates()
        {
            if (Last is IFillerVertex last)
                return last.GetNextCandidates(Prev);
            else
                return GetBeginCandidates();
        }
        private IEnumerable<IFillerVertex> GetBeginCandidates()
        {
            foreach (var intersect in Markup.Intersects)
            {
                yield return new IntersectFillerVertex(this, intersect.Pair);
            }
            foreach (var enter in Markup.Enters)
            {
                foreach (var point in enter.Points.Where(p => p.IsEdge || p.Lines.Any()))
                {
                    yield return new EnterFillerVertex(this, point);
                }
            }
        }

        public void GetMinMaxT(IFillerVertex fillerVertex, MarkupLine line, out float resultT, out float resultMinT, out float resultMaxT)
        {
            fillerVertex.GetT(line, out float t);
            var minT = -1f;
            var maxT = 2f;

            foreach (var linePart in LineParts)
            {
                linePart.GetFromT(out float fromT);
                linePart.GetToT(out float toT);

                if (linePart.Line == line)
                {
                    Set(fromT, false);
                    Set(toT, false);
                }
                else if (line.Markup.GetIntersect(new MarkupLinePair(line, linePart.Line)) is MarkupLineIntersect intersect && intersect.IsIntersect)
                {
                    var linePartT = intersect[linePart.Line];

                    if ((fromT <= linePartT && linePartT <= toT) || (toT <= linePartT && linePartT <= fromT))
                        Set(intersect[line], true);
                }
                else if (linePart.Line.IsEnterLine)
                {
                    if (line.Start.Enter == linePart.Line.Start.Enter && CheckEnter(line.Start.Num, linePart.Line.Start.Num, linePart.Line.End.Num))
                        Set(0, true);
                    if (line.End.Enter == linePart.Line.Start.Enter && CheckEnter(line.End.Num, linePart.Line.Start.Num, linePart.Line.End.Num))
                        Set(1, true);
                }
            }

            void Set(float tt, bool isStrict)
            {
                if (minT < tt && (isStrict ? tt < t : tt <= t))
                    minT = tt;

                if (maxT > tt && (isStrict ? tt > t : tt >= t))
                    maxT = tt;
            }
            bool CheckEnter(byte num, byte start, byte end) => (start <= num && num <= end) || (end <= num && num <= start);

            resultT = t;
            resultMinT = minT;
            resultMaxT = maxT;
        }

        public void GetMinMaxNum(EnterFillerVertex vertex, out byte resultNum, out byte resultMinNum, out byte resultMaxNum)
        {
            var num = vertex.Point.Num;
            var minNum = (byte)0;
            var maxNum = (byte)(vertex.Enter.PointCount + 1);

            foreach (var linePart in LineParts)
            {
                if (linePart.From is EnterPointEdge fromVertex && fromVertex.Point.Enter == vertex.Enter)
                    Set(fromVertex.Point.Num);
                if (linePart.To is EnterPointEdge toVertex && toVertex.Point.Enter == vertex.Enter)
                    Set(toVertex.Point.Num);
            }

            void Set(byte n)
            {
                if (minNum < n && n < num)
                    minNum = n;

                if (maxNum > n && n > num)
                    maxNum = n;
            }

            resultNum = num;
            resultMinNum = minNum;
            resultMaxNum = maxNum;
        }
        public IEnumerable<IFillerVertex> GetLinePoints(IFillerVertex fillerVertex, MarkupLine line)
        {
            GetMinMaxT(fillerVertex, line, out float t, out float minT, out float maxT);

            foreach (var intersectLine in line.IntersectLines)
            {
                var vertex = new IntersectFillerVertex(this, line, intersectLine);
                if (vertex.GetT(line, out float tt) && tt != t && minT < tt && tt < maxT)
                    yield return vertex;
            }

            switch (First)
            {
                case EnterFillerVertex firstE when line.ContainPoint(firstE.Point) && ((line.Start == firstE.Point && minT == 0) || (line.End == firstE.Point && maxT == 1)):
                    yield return firstE;
                    break;
                case IntersectFillerVertex firstI when firstI.LinePair.ContainLine(line) && firstI.GetT(line, out float firstT) && (firstT == minT || firstT == maxT):
                    yield return firstI;
                    break;
            }

            if (t != 0 && minT < 0 && 0 < maxT)
                yield return new EnterFillerVertex(this, line.Start);

            if (t != 1 && minT < 1 && 1 < maxT)
                yield return new EnterFillerVertex(this, line.End);
        }

        public void Update()
        {
            foreach (var part in LineParts)
            {
                if (part.Line is MarkupFakeLine fakeLine)
                    fakeLine.UpdateTrajectory();
            }
        }
        public void RecalculateDashes()
        {
            Dashes = Style.Calculate(this).ToArray();
        }
    }
    public class FillerLinePart : MarkupLinePart
    {
        public override string XmlSection => throw new NotImplementedException();
        public FillerLinePart(MarkupLine line, ILinePartEdge from, ILinePartEdge to) : base(line, from, to) { }
    }

    public class MarkupFakeLine : MarkupLine
    {
        public MarkupFakeLine(Markup markup, MarkupPoint first, MarkupPoint second) : base(markup, first, second) { }
        public override void UpdateTrajectory()
        {
            Trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                b = PointPair.Second.Position,
                c = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
        }
    }

}

