using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class MarkupCrosswalk : IUpdate, IToXml
    {
        #region PROPERTIES

        public static string XmlName { get; } = "C";
        public string XmlSection => XmlName;

        public Markup Markup { get; }
        public MarkupCrosswalkLine Line { get; }

        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];
        public MarkupEnterLine EnterLine { get; private set; }

        MarkupRegularLine _rightBorder;
        MarkupRegularLine _leftBorder;
        CrosswalkStyle _style;

        public MarkupRegularLine RightBorder
        {
            get => _rightBorder;
            set
            {
                _rightBorder = value;
                CrosswalkChanged();
            }
        }
        public MarkupRegularLine LeftBorder
        {
            get => _leftBorder;
            set
            {
                _leftBorder = value;
                CrosswalkChanged();
            }
        }
        public CrosswalkStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = CrosswalkChanged;
                CrosswalkChanged();
            }
        }
        private StraightTrajectory DefaultRightBorderTrajectory => new StraightTrajectory(EnterLine.Start.Position, EnterLine.Start.Position + NormalDir * TotalWidth);
        private StraightTrajectory DefaultLeftBorderTrajectory => new StraightTrajectory(EnterLine.End.Position, EnterLine.End.Position + NormalDir * TotalWidth);
        public ILineTrajectory RightBorderTrajectory { get; private set; }
        public ILineTrajectory LeftBorderTrajectory { get; private set; }

        public ILineTrajectory[] BorderTrajectories => new ILineTrajectory[] { EnterLine.Trajectory, new StraightTrajectory((StraightTrajectory)Line.Trajectory), RightBorderTrajectory, LeftBorderTrajectory };

        public float TotalWidth => Style.GetTotalWidth(this);
        public float CornerAndNormalAngle => EnterLine.Start.Enter.CornerAndNormalAngle;
        public Vector3 NormalDir => EnterLine.Start.Enter.NormalDir;
        public Vector3 CornerDir => EnterLine.Start.Enter.CornerDir;


        #endregion
        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine crosswalkLine, CrosswalkStyle.CrosswalkType crosswalkType = CrosswalkStyle.CrosswalkType.Existent) :
            this(markup, crosswalkLine, TemplateManager.GetDefault<CrosswalkStyle>((Style.StyleType)(int)crosswalkType))
        { }
        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine line, CrosswalkStyle style, MarkupRegularLine rightBorder = null, MarkupRegularLine leftBorder = null)
        {
            Markup = markup;
            Line = line;
            Line.TrajectoryGetter = GetTrajectory;
            _style = style;
            _style.OnStyleChanged = CrosswalkChanged;
            _rightBorder = rightBorder;
            _leftBorder = leftBorder;

            GetEnterLine();
        }
        private void GetEnterLine()
        {
            Line.Start.Enter.TryGetPoint(Line.Start.Num, MarkupPoint.PointType.Enter, out MarkupPoint startPoint);
            Line.End.Enter.TryGetPoint(Line.End.Num, MarkupPoint.PointType.Enter, out MarkupPoint endPoint);
            EnterLine = new MarkupEnterLine(Markup, startPoint.Num < endPoint.Num ? startPoint : endPoint, startPoint.Num < endPoint.Num ? endPoint : startPoint);
        }

        protected void CrosswalkChanged() => Markup.Update(this, true);

        public void Update(bool onlySelfUpdate = false)
        {
            EnterLine.Update(true);
            if(!onlySelfUpdate)
                Markup.Update(this);
        }
        public void RecalculateDashes() => Dashes = Style.Calculate(this).ToArray();
        public void Render(RenderManager.CameraInfo cameraInfo, Color color)
        {
            foreach (var trajectory in BorderTrajectories)
                NodeMarkupTool.RenderTrajectory(cameraInfo, color, trajectory);
        }

        public MarkupRegularLine GetBorder(BorderPosition borderType) => borderType == BorderPosition.Right ? RightBorder : LeftBorder;

        private StraightTrajectory GetOffsetTrajectory(float offset)
        {
            var start = EnterLine.Start.Position + NormalDir * offset;
            var end = EnterLine.End.Position + NormalDir * offset;
            return new StraightTrajectory(start, end, false);
        }
        public StraightTrajectory GetTrajectory()
        {
            var trajectory = GetOffsetTrajectory(TotalWidth);

            RightBorderTrajectory = GetBorderTrajectory(trajectory, RightBorder, 0, DefaultRightBorderTrajectory, out float startT);
            LeftBorderTrajectory = GetBorderTrajectory(trajectory, LeftBorder, 1, DefaultLeftBorderTrajectory, out float endT);

            return new StraightTrajectory((StraightTrajectory)trajectory.Cut(startT, endT), false);
        }
        private ILineTrajectory GetBorderTrajectory(StraightTrajectory trajectory, MarkupLine border, float defaultT, StraightTrajectory defaultTrajectory, out float t)
        {
            if (border != null && MarkupIntersect.CalculateSingle(trajectory, border.Trajectory) is MarkupIntersect intersect && intersect.IsIntersect)
            {
                t = intersect.FirstT;
                return EnterLine.PointPair.ContainPoint(border.Start) ? border.Trajectory.Cut(0, intersect.SecondT) : border.Trajectory.Cut(intersect.SecondT, 1);
            }
            else
            {
                t = defaultT;
                return defaultTrajectory;
            }
        }

        public StraightTrajectory GetTrajectory(float offset)
        {
            var trajectory = GetOffsetTrajectory(offset);

            var startT = GetT(trajectory, RightBorderTrajectory, 0);
            var endT = GetT(trajectory, LeftBorderTrajectory, 1);

            return (StraightTrajectory)trajectory.Cut(startT, endT);
        }
        public StraightTrajectory GetFullTrajectory(float offset, Vector3 normal)
        {
            var trajectory = GetOffsetTrajectory(offset);

            var startT = GetT(trajectory, normal, new Vector3[] { EnterLine.Start.Position, Line.Trajectory.StartPosition }, 0, MinAggregate);
            var endT = GetT(trajectory, normal, new Vector3[] { EnterLine.End.Position, Line.Trajectory.EndPosition }, 1, MaxAggregate);

            return (StraightTrajectory)trajectory.Cut(startT, endT);
        }
        private float MinAggregate(MarkupIntersect[] intersects) => intersects.Min(i => i.IsIntersect ? i.FirstT : 0);
        private float MaxAggregate(MarkupIntersect[] intersects) => intersects.Max(i => i.IsIntersect ? i.FirstT : 1);

        private float GetT(StraightTrajectory trajectory, ILineTrajectory lineTrajectory, float defaultT)
            => MarkupIntersect.CalculateSingle(trajectory, lineTrajectory) is MarkupIntersect intersect && intersect.IsIntersect ? intersect.FirstT : defaultT;

        private float GetT(StraightTrajectory trajectory, Vector3 normal, IEnumerable<Vector3> positions, float defaultT, Func<MarkupIntersect[], float> aggregate)
        {
            var intersects = positions.SelectMany(p => MarkupIntersect.Calculate(trajectory, new StraightTrajectory(p, p + normal, false))).ToArray();
            return intersects.Any() ? aggregate(intersects) : defaultT;
        }


        public bool IsBorder(MarkupLine line) => line != null && (line == RightBorder || line == LeftBorder);
        public void RemoveBorder(MarkupLine line)
        {
            if (line == RightBorder)
                RightBorder = null;

            if (line == LeftBorder)
                LeftBorder = null;
        }
        public bool ContainsPoint(MarkupPoint point) => EnterLine.ContainsPoint(point);

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlName);
            config.Add(new XAttribute(MarkupLine.XmlName, Line.PointPair.Hash));
            if (RightBorder != null)
                config.Add(new XAttribute("RB", RightBorder.PointPair.Hash));
            if (LeftBorder != null)
                config.Add(new XAttribute("LB", LeftBorder.PointPair.Hash));
            config.Add(Style.ToXml());
            return config;
        }
        public void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map)
        {
            _rightBorder = GetBorder("RB");
            _leftBorder = GetBorder("LB");
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, out CrosswalkStyle style))
            {
                _style = style;
                _style.OnStyleChanged = CrosswalkChanged;
            }

            MarkupRegularLine GetBorder(string key)
            {
                var lineId = config.GetAttrValue<ulong>(key);
                return Markup.TryGetLine(lineId, map, out MarkupRegularLine line) ? line : null;
            }
        }

        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out MarkupCrosswalk crosswalk)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            if (markup.TryGetLine(lineId, map, out MarkupCrosswalkLine line))
            {
                crosswalk = line.Crosswalk;
                crosswalk.FromXml(config, map);
                return true;
            }
            else
            {
                crosswalk = null;
                return false;
            }
        }

        #endregion

        public override string ToString() => Line.ToString();
    }
}
