using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IPeriodicFiller : IFillerStyle, IWidthStyle
    {
        float Step { get; set; }
        float Offset { get; set; }
    }
    public interface IRotateFiller : IFillerStyle, IWidthStyle
    {
        float Angle { get; set; }
    }


    public abstract class SimpleFillerStyle : FillerStyle, IPeriodicFiller, IRotateFiller
    {
        float _angle;
        float _step;
        float _offset;

        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                StyleChanged();
            }
        }
        public float Step
        {
            get => _step;
            set
            {
                _step = value;
                StyleChanged();
            }
        }
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public SimpleFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, medianOffset)
        {
            Angle = angle;
            Step = step;
            Offset = offset;
        }

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IRotateFiller rotateTarget)
            {
                rotateTarget.Angle = Angle;
            }
            if (target is IPeriodicFiller periodicTarget)
            {
                periodicTarget.Step = Step;
                periodicTarget.Offset = Offset;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("A", Angle));
            config.Add(new XAttribute("S", Step));
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Angle = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStepGrid);
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }

    public class StripeFillerStyle : SimpleFillerStyle
    {
        public override StyleType Type => StyleType.FillerStripe;

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] parts, Rect rect, float height) => GetDashes(parts, Angle, rect, height, Width, Step, Offset);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
    }
    public class GridFillerStyle : SimpleFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);

        protected override IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] parts, Rect rect, float height)
        {
            foreach (var dash in GetDashes(parts, Angle, rect, height, Width, Step, Offset))
                yield return dash;
            foreach (var dash in GetDashes(parts, Angle < 0 ? Angle + 90 : Angle - 90, rect, height, Width, Step, Offset))
                yield return dash;
        }
    }
    public class SolidFillerStyle : FillerStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;

        public SolidFillerStyle(Color32 color, float medianOffset) : base(color, DefaultSolidWidth, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] parts, Rect rect, float height) => GetDashes(parts, 0f, rect, height, DefaultSolidWidth, 1, 0);

        public override FillerStyle CopyFillerStyle() => new SolidFillerStyle(Color, DefaultOffset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>();
            components.Add(AddColorProperty(parent));
            if (!isTemplate && editObject is MarkupFiller filler && filler.IsMedian)
                components.Add(AddMedianOffsetProperty(this, parent, onHover, onLeave));

            return components;
        }
    }
    public class ChevronFillerStyle : FillerStyle, IPeriodicFiller
    {
        public override StyleType Type => StyleType.FillerChevron;

        float _angleBetween;
        float _step;
        bool _invert;
        int _output;

        public float AngleBetween
        {
            get => _angleBetween;
            set
            {
                _angleBetween = value;
                StyleChanged();
            }
        }
        public float Step
        {
            get => _step;
            set
            {
                _step = value;
                StyleChanged();
            }
        }
        public float Offset { get; set; }
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }
        public int Output
        {
            get => _output;
            set
            {
                _output = value;
                StyleChanged();
            }
        }

        public ChevronFillerStyle(Color32 color, float width, float medianOffset, float angleBetween, float step, int output = 0, bool invert = false) : base(color, width, medianOffset)
        {
            AngleBetween = angleBetween;
            Step = step;
            Output = output;
            Invert = invert;
        }

        public override FillerStyle CopyFillerStyle() => new ChevronFillerStyle(Color, Width, MedianOffset, AngleBetween, Step);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ChevronFillerStyle chevronTarget)
            {
                chevronTarget.AngleBetween = AngleBetween;
                chevronTarget.Step = Step;
                chevronTarget.Invert = Invert;
                chevronTarget.Output = Output;
            }
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddAngleBetweenProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddInvertAndTurnProperty(this, parent));

            return components;
        }
        protected static FloatPropertyPanel AddAngleBetweenProperty(ChevronFillerStyle chevronStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = parent.AddUIComponent<FloatPropertyPanel>();
            angleProperty.Text = Localize.Filler_AngleBetween;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 45;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.Init();
            angleProperty.Value = chevronStyle.AngleBetween;
            angleProperty.OnValueChanged += (float value) => chevronStyle.AngleBetween = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static ButtonsPanel AddInvertAndTurnProperty(ChevronFillerStyle chevronStyle, UIComponent parent)
        {
            var buttonsPanel = parent.AddUIComponent<ButtonsPanel>();
            var invertIndex = buttonsPanel.AddButton(Localize.Filler_Invert);
            var turnIndex = buttonsPanel.AddButton(Localize.Filler_Turn);
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick(int index)
            {
                if (index == invertIndex)
                    chevronStyle.Invert = !chevronStyle.Invert;
                else if (index == turnIndex)
                    chevronStyle.Output += 1;
            }

            return buttonsPanel;
        }

        protected override IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] trajectories, Rect rect, float height)
        {
            if (trajectories.Length < 3)
                yield break;

            GetItems(trajectories, rect, out List<Vector3[]> positions, out List<Vector3> directions, out float partWidth);

            for (var i = 0; i < directions.Count; i += 1)
            {
                var dir = directions[i];
                foreach (var pos in positions[i])
                {
                    var line = new StraightTrajectory(pos, pos + dir, false);
                    var intersectSet = new HashSet<MarkupIntersect>();
                    foreach (var trajectory in trajectories)
                        intersectSet.AddRange(MarkupIntersect.Calculate(line, trajectory).Where(k => k.FirstT > 0));

                    if (intersectSet.Count % 2 == 1)
                        intersectSet.Add(new MarkupIntersect(0, 0, 0));

                    var intersects = intersectSet.OrderBy(j => j, MarkupIntersect.FirstComparer).ToArray();

                    for (var j = 1; j < intersects.Length; j += 2)
                    {
                        var start = pos + dir * intersects[j - 1].FirstT;
                        var end = pos + dir * intersects[j].FirstT;

                        yield return new MarkupStyleDash(start, end, dir, partWidth, Color);
                    }
                }
            }
        }
        private void GetItems(ILineTrajectory[] trajectories, Rect rect, out List<Vector3[]> positions, out List<Vector3> directions, out float partWidth)
        {
            var halfAngelRad = (Invert ? 360 - AngleBetween : AngleBetween) * Mathf.Deg2Rad / 2;
            var width = Width / Mathf.Sin(halfAngelRad);

            var bezier = GetMiddleBezier(trajectories);
            var line = GetMiddleLine(bezier, halfAngelRad, rect);

            StyleHelper.GetParts(width, 0, out int partsCount, out partWidth);

            positions = new List<Vector3[]>();
            directions = new List<Vector3>();

            foreach (var itemPositions in GetItemsPositions(bezier, line, width, width * (Step - 1)))
            {
                var dir = (itemPositions[1] - itemPositions[0]).normalized;
                var dirRight = dir.TurnRad(halfAngelRad, true);
                var dirLeft = dir.TurnRad(halfAngelRad, false);

                var start = partWidth / 2;

                var rightPos = new Vector3[partsCount];
                var leftPos = new Vector3[partsCount];

                for (var i = 0; i < partsCount; i += 1)
                {
                    var partPos = itemPositions[0] + dir * (start + partWidth * i);

                    rightPos[i] = partPos;
                    leftPos[i] = partPos;
                }

                positions.Add(rightPos);
                directions.Add(dirRight);
                positions.Add(leftPos);
                directions.Add(dirLeft);
            }
        }
        private IEnumerable<Vector3[]> GetItemsPositions(Bezier3 bezier, Line3 line, float dash, float space)
        {
            var dashesT = new List<float[]>();

            var startSpace = space / 2;
            for (var i = 0; i < 3; i += 1)
            {
                dashesT.Clear();
                var isDash = false;

                var prevT = 0f;
                var currentT = 0f;
                var nextT = Travel(currentT, startSpace);

                while (nextT < 2)
                {
                    if (isDash)
                        dashesT.Add(new float[] { currentT, nextT });

                    isDash = !isDash;

                    prevT = currentT;
                    currentT = nextT;
                    nextT = Travel(currentT, isDash ? dash : space);
                }

                float endSpace;
                if (isDash || ((Position(2) - Position(currentT)).magnitude is float tempLength && tempLength < space / 2))
                    endSpace = (Position(2) - Position(prevT)).magnitude;
                else
                    endSpace = tempLength;

                startSpace = (startSpace + endSpace) / 2;

                if (Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    break;
            }

            foreach (var dashT in dashesT)
            {
                yield return new Vector3[] { Position(dashT[0]), Position(dashT[1]) };
            }


            Vector3 Position(float t) => t <= 1 ? bezier.Position(t) : line.a + (line.b - line.a) * (t - 1);
            float Travel(float current, float distance)
            {
                if (current >= 1)
                    return distance / (line.b - line.a).magnitude + current;

                var next = bezier.Travel(current, distance);
                if (next < 1)
                    return next;
                else
                    return (distance - (Position(1) - Position(current)).magnitude) / (line.b - line.a).magnitude + 1;
            }
        }
        private Bezier3 GetMiddleBezier(ILineTrajectory[] trajectories)
        {
            var left = Output % trajectories.Length;
            var right = left == 0 ? trajectories.Length - 1 : left - 1;
            var middle = new Bezier3()
            {
                a = (trajectories[right].EndPosition + trajectories[left].StartPosition) / 2,
                b = (trajectories[right].EndDirection.normalized + trajectories[left].StartDirection.normalized) / 2,
                c = (trajectories[right].StartDirection.normalized + trajectories[left].EndDirection.normalized) / 2,
                d = (trajectories[right].StartPosition + trajectories[left].EndPosition) / 2,
            };
            NetSegment.CalculateMiddlePoints(middle.a, middle.b, middle.d, middle.c, true, true, out middle.b, out middle.c);
            var middleTrajectory = new BezierTrajectory(middle);

            var cutT = 1f;
            for (var i = 0; i < trajectories.Length; i += 1)
            {
                if (i == left || i == right)
                    continue;
                if (MarkupIntersect.Calculate(middleTrajectory, trajectories[i]).FirstOrDefault() is MarkupIntersect intersect && intersect.IsIntersect && intersect.FirstT < cutT)
                    cutT = intersect.FirstT;
            }

            return cutT == 1f ? middle : middle.Cut(0, cutT);
        }
        private Line3 GetMiddleLine(Bezier3 middleBezier, float halfAngelRad, Rect rect)
        {
            var middleDir = (middleBezier.d - middleBezier.c).normalized;
            var dirRight = middleDir.TurnRad(halfAngelRad, true);
            var dirLeft = middleDir.TurnRad(halfAngelRad, false);

            GetRail(dirRight.AbsoluteAngle() * Mathf.Rad2Deg, rect, 0, out Line3 rightRail);
            GetRail(dirLeft.AbsoluteAngle() * Mathf.Rad2Deg, rect, 0, out Line3 leftRail);

            var t = 0f;
            t = Mathf.Max(t, GetT(rightRail.a, dirRight));
            t = Mathf.Max(t, GetT(rightRail.b, dirRight));
            t = Mathf.Max(t, GetT(leftRail.a, dirLeft));
            t = Mathf.Max(t, GetT(leftRail.b, dirLeft));

            return new Line3(middleBezier.d, middleBezier.d + middleDir * t);

            float GetT(Vector3 railPos, Vector3 railDir)
            {
                Line2.Intersect(middleBezier.d.XZ(), (middleBezier.d + middleDir).XZ(), railPos.XZ(), (railPos + railDir).XZ(), out float p, out _);
                return p;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("A", AngleBetween));
            config.Add(new XAttribute("S", Step));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            config.Add(new XAttribute("O", Output));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            AngleBetween = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStepGrid);
            Invert = config.GetAttrValue("I", 0) == 1;
            Output = config.GetAttrValue("O", 0);
        }
    }
}
