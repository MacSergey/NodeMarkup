using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class MarkupCrosswalk : IStyleItem, IToXml, ISupport
    {
        #region PROPERTIES

        public static string XmlName { get; } = "C";
        public string XmlSection => XmlName;

        public string DeleteCaptionDescription => Localize.CrossWalkEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.CrossWalkEditor_DeleteMessageDescription;
        public Markup.SupportType Support => Markup.SupportType.Croswalks;

        public Markup Markup { get; }
        public MarkupCrosswalkLine CrosswalkLine { get; }

        public List<IStyleData> StyleData { get; } = new List<IStyleData>();
        public MarkupEnterLine EnterLine { get; private set; }

        public PropertyValue<MarkupRegularLine> RightBorder { get; }
        public PropertyValue<MarkupRegularLine> LeftBorder { get; }
        public PropertyValue<CrosswalkStyle> Style { get; }

        private StraightTrajectory DefaultRightBorderTrajectory => new StraightTrajectory(EnterLine.Start.Position, EnterLine.Start.Position + NormalDir * TotalWidth);
        private StraightTrajectory DefaultLeftBorderTrajectory => new StraightTrajectory(EnterLine.End.Position, EnterLine.End.Position + NormalDir * TotalWidth);
        public ITrajectory RightBorderTrajectory { get; private set; }
        public ITrajectory LeftBorderTrajectory { get; private set; }

        public ITrajectory[] BorderTrajectories => new ITrajectory[] { EnterLine.Trajectory, RightBorderTrajectory, CrosswalkLine.Trajectory, LeftBorderTrajectory };

        public float TotalWidth => Style.Value.GetTotalWidth(this);
        public float CornerAndNormalAngle => EnterLine.Start.Enter.CornerAndNormalAngle;
        public Vector3 NormalDir => EnterLine.Start.Enter.NormalDir;
        public Vector3 CornerDir => EnterLine.Start.Enter.CornerDir;

        #endregion

        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine line, CrosswalkStyle style, MarkupRegularLine rightBorder = null, MarkupRegularLine leftBorder = null)
        {
            Markup = markup;
            CrosswalkLine = line;
            CrosswalkLine.TrajectoryGetter = GetTrajectory;

            RightBorder = new PropertyClassValue<MarkupRegularLine>("RB", CrosswalkChanged, rightBorder);
            LeftBorder = new PropertyClassValue<MarkupRegularLine>("LB", CrosswalkChanged, leftBorder);
            style.OnStyleChanged = CrosswalkChanged;
            Style = new PropertyClassValue<CrosswalkStyle>(StyleChanged, style);

            CrosswalkLine.Start.Enter.TryGetPoint(CrosswalkLine.Start.Index, MarkupPoint.PointType.Enter, out MarkupPoint startPoint);
            CrosswalkLine.End.Enter.TryGetPoint(CrosswalkLine.End.Index, MarkupPoint.PointType.Enter, out MarkupPoint endPoint);
            EnterLine = new MarkupEnterLine(Markup, startPoint, endPoint);
        }
        private void StyleChanged()
        {
            Style.Value.OnStyleChanged = CrosswalkChanged;
            CrosswalkChanged();
        }
        protected void CrosswalkChanged() => Markup.Update(this, true, true);

        public void Update(bool onlySelfUpdate = false)
        {
            EnterLine.Update(GetAlignment(RightBorder.Value, EnterLine.Start), GetAlignment(LeftBorder.Value, EnterLine.End), true);
            CrosswalkLine.Update(true);

            if (!onlySelfUpdate)
                Markup.Update(this);

            static Alignment GetAlignment(MarkupRegularLine line, MarkupPoint point) => line?.GetAlignment(point) ?? Alignment.Centre;
        }

        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate crosswalk {this}");
#endif
            StyleData.Clear();
            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
            {
                StyleData.Add(Style.Value.Calculate(this, lod));
            }
        }

        public MarkupRegularLine GetBorder(BorderPosition borderType) => borderType == BorderPosition.Right ? RightBorder : LeftBorder;

        private StraightTrajectory GetOffsetTrajectory(float offset)
        {
            var start = EnterLine.Start.Position + NormalDir * offset;
            var end = EnterLine.End.Position + NormalDir * offset;
            return new StraightTrajectory(start, end, false);
        }
        private StraightTrajectory GetTrajectory()
        {
            var trajectory = GetOffsetTrajectory(TotalWidth);

            RightBorderTrajectory = GetBorderTrajectory(trajectory, RightBorder, 0, DefaultRightBorderTrajectory, out float startT);
            LeftBorderTrajectory = GetBorderTrajectory(trajectory, LeftBorder, 1, DefaultLeftBorderTrajectory, out float endT);

            return trajectory.Cut(startT, endT);
        }
        private ITrajectory GetBorderTrajectory(StraightTrajectory trajectory, MarkupLine border, float defaultT, StraightTrajectory defaultTrajectory, out float t)
        {
            if (border != null && Intersection.CalculateSingle(trajectory, border.Trajectory) is Intersection intersect && intersect.IsIntersect)
            {
                t = intersect.FirstT;
                return EnterLine.PointPair.ContainsPoint(border.Start) ? border.Trajectory.Cut(0, intersect.SecondT) : border.Trajectory.Cut(intersect.SecondT, 1);
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

            return trajectory.Cut(startT, endT);

            static float GetT(StraightTrajectory trajectory, ITrajectory lineTrajectory, float defaultT)
            => Intersection.CalculateSingle(trajectory, lineTrajectory) is Intersection intersect && intersect.IsIntersect ? intersect.FirstT : defaultT;
        }
        public StraightTrajectory GetFullTrajectory(float offset, Vector3 normal)
        {
            var trajectory = GetOffsetTrajectory(offset);

            var startT = GetT(trajectory, normal, new Vector3[] { EnterLine.Start.Position, CrosswalkLine.Trajectory.StartPosition }, 0, MinAggregate);
            var endT = GetT(trajectory, normal, new Vector3[] { EnterLine.End.Position, CrosswalkLine.Trajectory.EndPosition }, 1, MaxAggregate);

            return trajectory.Cut(startT, endT);

            static float MinAggregate(Intersection[] intersects) => intersects.Min(i => i.IsIntersect ? i.FirstT : 0);
            static float MaxAggregate(Intersection[] intersects) => intersects.Max(i => i.IsIntersect ? i.FirstT : 1);
            static float GetT(StraightTrajectory trajectory, Vector3 normal, Vector3[] positions, float defaultT, Func<Intersection[], float> aggregate)
            {
                var intersects = positions.SelectMany(p => Intersection.Calculate(trajectory, new StraightTrajectory(p, p + normal, false))).ToArray();
                return intersects.Any() ? aggregate(intersects) : defaultT;
            }
        }

        public bool IsBorder(MarkupLine line) => line != null && (line == RightBorder.Value || line == LeftBorder.Value);
        public void RemoveBorder(MarkupLine line)
        {
            if (line == RightBorder.Value)
                RightBorder.Value = null;

            if (line == LeftBorder.Value)
                LeftBorder.Value = null;
        }
        public bool ContainsPoint(MarkupPoint point) => EnterLine.ContainsPoint(point);

        public Dependences GetDependences() => Markup.GetCrosswalkDependences(this);
        public void Render(OverlayData data)
        {
            var trajectories = new ITrajectory[4];

            trajectories[0] = EnterLine.Trajectory;

            if (LeftBorder.Value == null)
                trajectories[1] = LeftBorderTrajectory;
            else if (LeftBorder.Value.PointPair.First == EnterLine.PointPair.Second)
                trajectories[1] = LeftBorderTrajectory;
            else
                trajectories[1] = LeftBorderTrajectory.Invert();

            trajectories[2] = CrosswalkLine.Trajectory.Invert();

            if (RightBorder.Value == null)
                trajectories[3] = RightBorderTrajectory.Invert();
            else if (RightBorder.Value.PointPair.Second == EnterLine.PointPair.First)
                trajectories[3] = RightBorderTrajectory;
            else
                trajectories[3] = RightBorderTrajectory.Invert();

            data.AlphaBlend = false;
            var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
            points.RenderArea(triangles, data);
        }

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlName);
            config.AddAttr(MarkupLine.XmlName, CrosswalkLine.PointPair.Hash);
            if (RightBorder.Value != null)
                config.AddAttr("RB", RightBorder.Value.PointPair.Hash);
            if (LeftBorder.Value != null)
                config.AddAttr("LB", LeftBorder.Value.PointPair.Hash);
            config.Add(Style.Value.ToXml());
            return config;
        }
        public void FromXml(XElement config, ObjectsMap map)
        {
            RightBorder.Value = GetBorder(map.Invert ? "LB" : "RB");
            LeftBorder.Value = GetBorder(map.Invert ? "RB" : "LB");
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, map, false, false, out CrosswalkStyle style))
                Style.Value = style;

            MarkupRegularLine GetBorder(string key)
            {
                var lineId = config.GetAttrValue<ulong>(key);
                return Markup.TryGetLine(lineId, map, out MarkupRegularLine line) ? line : null;
            }
        }

        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out MarkupCrosswalk crosswalk)
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

        public override string ToString() => CrosswalkLine.ToString();
    }
}
