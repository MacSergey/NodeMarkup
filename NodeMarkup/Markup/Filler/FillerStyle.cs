using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
//using Poly2Tri;
//using Poly2Tri.Triangulation.Polygon;

namespace NodeMarkup.Manager
{
    public interface IPeriodicFiller : IFillerStyle, IWidthStyle, IColorStyle
    {
        float Step { get; set; }
        float Offset { get; set; }
    }
    public interface IRotateFiller : IFillerStyle, IWidthStyle, IColorStyle
    {
        float Angle { get; set; }
    }

    public abstract class Filler2DStyle : FillerStyle
    {
        public Filler2DStyle(Color32 color, float width, float medianOffset) : base(color, width, medianOffset) { }

        protected override IStyleData GetStyleData(ILineTrajectory[] trajectories, Rect rect, float height) => new MarkupStyleDashes(GetDashesEnum(trajectories, rect, height));
        protected abstract IEnumerable<MarkupStyleDash> GetDashesEnum(ILineTrajectory[] trajectories, Rect rect, float height);
    }
    public abstract class Filler3DStyle : FillerStyle
    {
        public Filler3DStyle(Color32 color, float width, float medianOffset) : base(color, width, medianOffset) { }
    }

    public abstract class SimpleFillerStyle : Filler2DStyle, IPeriodicFiller, IRotateFiller, IWidthStyle, IColorStyle
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Angle = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStepGrid);
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }

    public class StripeFillerStyle : SimpleFillerStyle
    {
        public override StyleType Type => StyleType.FillerStripe;

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashesEnum(ILineTrajectory[] trajectories, Rect rect, float height) => GetDashes(trajectories, Angle, rect, height, Width, Step, Offset);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
    }
    public class GridFillerStyle : SimpleFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
        protected override IEnumerable<MarkupStyleDash> GetDashesEnum(ILineTrajectory[] trajectories, Rect rect, float height)
        {
            foreach (var dash in GetDashes(trajectories, Angle, rect, height, Width, Step, Offset))
                yield return dash;
            foreach (var dash in GetDashes(trajectories, Angle < 0 ? Angle + 90 : Angle - 90, rect, height, Width, Step, Offset))
                yield return dash;
        }
    }
    public class SolidFillerStyle : Filler2DStyle, IColorStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;

        public SolidFillerStyle(Color32 color, float medianOffset) : base(color, DefaultSolidWidth, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashesEnum(ILineTrajectory[] trajectories, Rect rect, float height) => GetDashes(trajectories, 0f, rect, height, DefaultSolidWidth, 1, 0);

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
    public class ChevronFillerStyle : Filler2DStyle, IPeriodicFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerChevron;

        float _angleBetween;
        float _step;
        bool _invert;
        int _output;
        From _startingFrom;

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
        public From StartingFrom
        {
            get => _startingFrom;
            set
            {
                _startingFrom = value;
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
            {
                components.Add(AddStartingFromProperty(this, parent));
                components.Add(AddInvertAndTurnProperty(this, parent));
            }

            return components;
        }
        protected static FloatPropertyPanel AddAngleBetweenProperty(ChevronFillerStyle chevronStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.Init();
            angleProperty.Value = chevronStyle.AngleBetween;
            angleProperty.OnValueChanged += (float value) => chevronStyle.AngleBetween = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static ChevronFromPropertyPanel AddStartingFromProperty(ChevronFillerStyle chevronStyle, UIComponent parent)
        {
            var fromProperty = ComponentPool.Get<ChevronFromPropertyPanel>(parent);
            fromProperty.Text = Localize.StyleOption_StartingFrom;
            fromProperty.Init();
            fromProperty.SelectedObject = chevronStyle.StartingFrom;
            fromProperty.OnSelectObjectChanged += (From value) => chevronStyle.StartingFrom = value;
            return fromProperty;
        }
        protected static ButtonsPanel AddInvertAndTurnProperty(ChevronFillerStyle chevronStyle, UIComponent parent)
        {
            var buttonsPanel = ComponentPool.Get<ButtonsPanel>(parent);
            var invertIndex = buttonsPanel.AddButton(Localize.StyleOption_Invert);
            var turnIndex = buttonsPanel.AddButton(Localize.StyleOption_Turn);
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

        protected override IEnumerable<MarkupStyleDash> GetDashesEnum(ILineTrajectory[] trajectories, Rect rect, float height)
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

                        yield return new MarkupStyleDash(start, end, dir, partWidth, Color, MaterialType.RectangleFillers);
                    }
                }
            }
        }
        private void GetItems(ILineTrajectory[] trajectories, Rect rect, out List<Vector3[]> positions, out List<Vector3> directions, out float partWidth)
        {
            var halfAngelRad = (Invert ? 360 - AngleBetween : AngleBetween) * Mathf.Deg2Rad / 2;
            var coef = Mathf.Sin(halfAngelRad);
            var width = Width / coef;

            var bezier = GetMiddleBezier(trajectories);
            var lines = new List<ILineTrajectory>();
            if (GetMiddleLineBefore(bezier, halfAngelRad, rect, out Line3 lineBefore))
                lines.Add(new StraightTrajectory(lineBefore).Invert());
            lines.Add(new BezierTrajectory(bezier));
            if (GetMiddleLineAfter(bezier, halfAngelRad, rect, out Line3 lineAfter))
                lines.Add(new StraightTrajectory(lineAfter));

            StyleHelper.GetParts(Width, 0, out int partsCount, out partWidth);
            var partStep = partWidth / coef;

            positions = new List<Vector3[]>();
            directions = new List<Vector3>();

            foreach (var itemPositions in GetItemsPositions(lines, width, width * (Step - 1)))
            {
                var dir = (itemPositions[1] - itemPositions[0]).normalized;
                var dirRight = dir.TurnRad(halfAngelRad, true);
                var dirLeft = dir.TurnRad(halfAngelRad, false);

                var start = partStep / 2;

                var rightPos = new Vector3[partsCount];
                var leftPos = new Vector3[partsCount];

                for (var i = 0; i < partsCount; i += 1)
                {
                    var partPos = itemPositions[0] + dir * (start + partStep * i);

                    rightPos[i] = partPos;
                    leftPos[i] = partPos;
                }

                positions.Add(rightPos);
                directions.Add(dirRight);
                positions.Add(leftPos);
                directions.Add(dirLeft);
            }
        }
        private IEnumerable<Vector3[]> GetItemsPositions(List<ILineTrajectory> lines, float dash, float space)
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

                while (nextT < lines.Count)
                {
                    if (isDash)
                        dashesT.Add(new float[] { currentT, nextT });

                    isDash = !isDash;

                    prevT = currentT;
                    currentT = nextT;
                    nextT = Travel(currentT, isDash ? dash : space);
                }

                float endSpace;
                if (isDash || ((Position(lines.Count) - Position(currentT)).magnitude is float tempLength && tempLength < space / 2))
                    endSpace = (Position(lines.Count) - Position(prevT)).magnitude;
                else
                    endSpace = tempLength;

                startSpace = (startSpace + endSpace) / 2;

                if (Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    break;
            }

            foreach (var dashT in dashesT)
                yield return new Vector3[] { Position(dashT[0]), Position(dashT[1]) };


            Vector3 Position(float t)
            {
                var i = (int)t == t ? (int)t - 1 : (int)t;
                return lines[i].Position(t - i);
            }
            float Travel(float current, float distance)
            {
                var i = (int)current;
                var next = 1f;
                while (i < lines.Count)
                {
                    var line = lines[i];
                    var start = current - i;
                    next = line.Travel(start, distance);

                    if (next < 1)
                        break;
                    else
                    {
                        i += 1;
                        current = i;
                        distance -= (line.Position(1f) - line.Position(start)).magnitude;
                    }
                }

                return i + next;
            }
        }

        private Bezier3 GetMiddleBezier(ILineTrajectory[] trajectories)
        {
            var leftIndex = Output % trajectories.Length;
            var rightIndex = leftIndex.PrevIndex(trajectories.Length, StartingFrom == From.Vertex ? 1 : 2);
            var left = trajectories[leftIndex];
            var right = trajectories[rightIndex];

            var leftLength = left.Length;
            var rightLength = right.Length;
            if (leftLength < rightLength)
                right = right.Cut(right.Travel(0, rightLength - leftLength), 1);
            else
                left = left.Cut(0, left.Travel(0, rightLength));

            var middle = new Bezier3()
            {
                a = (right.EndPosition + left.StartPosition) / 2,
                b = (right.EndDirection.normalized + left.StartDirection.normalized) / 2,
                c = (right.StartDirection.normalized + left.EndDirection.normalized) / 2,
                d = (right.StartPosition + left.EndPosition) / 2,
            };
            NetSegment.CalculateMiddlePoints(middle.a, middle.b, middle.d, middle.c, true, true, out middle.b, out middle.c);
            var middleTrajectory = new BezierTrajectory(middle);

            var cutT = 1f;
            for (var i = 0; i < trajectories.Length; i += 1)
            {
                if (i == leftIndex || i == rightIndex)
                    continue;
                if (MarkupIntersect.Calculate(middleTrajectory, trajectories[i]).FirstOrDefault() is MarkupIntersect intersect && intersect.IsIntersect && 0.1 <= intersect.FirstT && intersect.FirstT < cutT)
                    cutT = intersect.FirstT;
            }

            return cutT == 1f ? middle : middle.Cut(0, cutT);
        }

        private bool GetMiddleLineBefore(Bezier3 middleBezier, float halfAngelRad, Rect rect, out Line3 line)
            => GetMiddleLine(middleBezier.a, (middleBezier.a - middleBezier.b).normalized, halfAngelRad, rect, out line);
        private bool GetMiddleLineAfter(Bezier3 middleBezier, float halfAngelRad, Rect rect, out Line3 line)
            => GetMiddleLine(middleBezier.d, (middleBezier.d - middleBezier.c).normalized, halfAngelRad, rect, out line);

        private bool GetMiddleLine(Vector3 pos, Vector3 dir, float halfAngelRad, Rect rect, out Line3 line)
        {
            var dirRight = dir.TurnRad(halfAngelRad, true);
            var dirLeft = dir.TurnRad(halfAngelRad, false);

            GetRail(dirRight.AbsoluteAngle() * Mathf.Rad2Deg, rect, 0, out Line3 rightRail);
            GetRail(dirLeft.AbsoluteAngle() * Mathf.Rad2Deg, rect, 0, out Line3 leftRail);

            var t = new float[] { 0, GetT(rightRail.a, dirRight), GetT(rightRail.b, dirRight), GetT(leftRail.a, dirLeft), GetT(leftRail.b, dirLeft) }.Max();

            if (t > 0.1)
            {
                line = new Line3(pos, pos + dir * t);
                return true;
            }
            else
            {
                line = default;
                return false;
            }

            float GetT(Vector3 railPos, Vector3 railDir)
            {
                Line2.Intersect(pos.XZ(), (pos + dir).XZ(), railPos.XZ(), (railPos + railDir).XZ(), out float p, out _);
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
            config.Add(new XAttribute("SF", (int)StartingFrom));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            AngleBetween = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStepGrid);
            Invert = config.GetAttrValue("I", 0) == 1;
            Output = config.GetAttrValue("O", 0);
            StartingFrom = (From)config.GetAttrValue("SF", (int)From.Vertex);
        }

        public enum From
        {
            [Description(nameof(Localize.StyleOption_Vertex))]
            Vertex = 0,

            [Description(nameof(Localize.StyleOption_Edge))]
            Edge = 1
        }
    }

    //public class TriangulationFillerStyle : Filler3DStyle
    //{
    //    public override StyleType Type => StyleType.FillerPavement;

    //    float _minAngle;
    //    float _minLength;
    //    float _maxLength;
    //    float _scaleX;
    //    float _scaleY;
    //    public float MinAngle
    //    {
    //        get => _minAngle;
    //        set
    //        {
    //            _minAngle = value;
    //            StyleChanged();
    //        }
    //    }
    //    public float MinLength
    //    {
    //        get => _minLength;
    //        set
    //        {
    //            _minLength = value;
    //            StyleChanged();
    //        }
    //    }
    //    public float MaxLength
    //    {
    //        get => _maxLength;
    //        set
    //        {
    //            _maxLength = value;
    //            StyleChanged();
    //        }
    //    }
    //    public float ScaleX
    //    {
    //        get => _scaleX;
    //        set
    //        {
    //            _scaleX = value;
    //            StyleChanged();
    //        }
    //    }
    //    public float ScaleY
    //    {
    //        get => _scaleY;
    //        set
    //        {
    //            _scaleY = value;
    //            StyleChanged();
    //        }
    //    }

    //    public TriangulationFillerStyle(Color32 color, float width, float medianOffset, float minAngle, float minLength, float maxLength) : base(color, width, medianOffset)
    //    {
    //        MinAngle = MinAngle;
    //        MinLength = minLength;
    //        MaxLength = maxLength;
    //        ScaleX = 0.05f;
    //        ScaleY = 0.023f;
    //    }

    //    public override void CopyTo(Style target)
    //    {
    //        base.CopyTo(target);
    //        if (target is TriangulationFillerStyle triangulationTarget)
    //        {
    //            triangulationTarget.MinAngle = MinAngle;
    //            triangulationTarget.MinLength = MinLength;
    //            triangulationTarget.MaxLength = MaxLength;
    //        }
    //    }

    //    protected override IStyleData GetStyleData(ILineTrajectory[] trajectories, Rect _, float height)
    //    {
    //        var points = trajectories.SelectMany(t => StyleHelper.CalculateSolid(t, MinAngle, MinLength, MaxLength, (tr) => GetPoint(tr))).ToList();
    //        var rect = Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.z), points.Max(p => p.x), points.Max(p => p.z));

    //        for (var i = 0; i < points.Count; i += 1)
    //            points[i] = new Vector3(points[i].x - rect.center.x, 2, (rect.center.y - points[i].z) * 0.451f);

    //        var polygon = new Polygon(points.Select(p => new PolygonPoint(p.x, p.z)));
    //        P2T.Triangulate(polygon);

    //        points.Add(new Vector3(rect.width / -2, 0, rect.height / 2));
    //        points.Add(new Vector3(rect.width / 2, 0, rect.height / 2));
    //        points.Add(new Vector3(rect.width / 2, 0, rect.height / -2));
    //        points.Add(new Vector3(rect.width / -2, 0, rect.height / -2));

    //        var triangles = polygon.Triangles.SelectMany(t => t.Points.Select(p => polygon.IndexOf(p))).ToList();

    //        triangles.Add(points.Count - 2);
    //        triangles.Add(points.Count - 3);
    //        triangles.Add(points.Count - 4);

    //        triangles.Add(points.Count - 1);
    //        triangles.Add(points.Count - 2);
    //        triangles.Add(points.Count - 4);

    //        return new MarkupStyleMesh(rect, height, points.ToArray(), triangles.ToArray(), MaterialType.Pavement, ScaleX, ScaleY);
    //    }
    //    static IEnumerable<Vector3> GetPoint(ILineTrajectory trajectory)
    //    {
    //        yield return new Vector3(trajectory.StartPosition.x, 0, trajectory.StartPosition.z);
    //    }

    //    public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
    //    {
    //        var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
    //        components.Add(AddMinAngleProperty(this, parent, onHover, onLeave));
    //        components.Add(AddMinLengthProperty(this, parent, onHover, onLeave));
    //        components.Add(AddMaxLengthProperty(this, parent, onHover, onLeave));
    //        components.Add(AddScaleXProperty(this, parent, onHover, onLeave));
    //        components.Add(AddScaleYProperty(this, parent, onHover, onLeave));
    //        return components;
    //    }
    //    private static FloatPropertyPanel AddMinAngleProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
    //    {
    //        var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
    //        minAngleProperty.Text = "Min angle";
    //        minAngleProperty.UseWheel = true;
    //        minAngleProperty.WheelStep = 1f;
    //        minAngleProperty.CheckMin = true;
    //        minAngleProperty.MinValue = 5f;
    //        minAngleProperty.CheckMax = true;
    //        minAngleProperty.MaxValue = 90f;
    //        minAngleProperty.Init();
    //        minAngleProperty.Value = triangulationStyle.MinAngle;
    //        minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MinAngle = value;
    //        AddOnHoverLeave(minAngleProperty, onHover, onLeave);
    //        return minAngleProperty;
    //    }
    //    private static FloatPropertyPanel AddMinLengthProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
    //    {
    //        var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
    //        minAngleProperty.Text = "Min length";
    //        minAngleProperty.UseWheel = true;
    //        minAngleProperty.WheelStep = 0.1f;
    //        minAngleProperty.CheckMin = true;
    //        minAngleProperty.MinValue = 1f;
    //        minAngleProperty.Init();
    //        minAngleProperty.Value = triangulationStyle.MinLength;
    //        minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MinLength = value;
    //        AddOnHoverLeave(minAngleProperty, onHover, onLeave);
    //        return minAngleProperty;
    //    }
    //    private static FloatPropertyPanel AddMaxLengthProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
    //    {
    //        var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
    //        minAngleProperty.Text = "Max length";
    //        minAngleProperty.UseWheel = true;
    //        minAngleProperty.WheelStep = 0.1f;
    //        minAngleProperty.CheckMin = true;
    //        minAngleProperty.MinValue = 1f;
    //        minAngleProperty.Init();
    //        minAngleProperty.Value = triangulationStyle.MaxLength;
    //        minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MaxLength = value;
    //        AddOnHoverLeave(minAngleProperty, onHover, onLeave);
    //        return minAngleProperty;
    //    }
    //    private static FloatPropertyPanel AddScaleXProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
    //    {
    //        var scaleProperty = parent.AddUIComponent<FloatPropertyPanel>();
    //        scaleProperty.Text = "ScaleX";
    //        scaleProperty.UseWheel = true;
    //        scaleProperty.WheelStep = 0.01f;
    //        scaleProperty.Init();
    //        scaleProperty.Value = triangulationStyle.ScaleX;
    //        scaleProperty.OnValueChanged += (float value) => triangulationStyle.ScaleX = value;
    //        AddOnHoverLeave(scaleProperty, onHover, onLeave);
    //        return scaleProperty;
    //    }
    //    private static FloatPropertyPanel AddScaleYProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
    //    {
    //        var scaleProperty = parent.AddUIComponent<FloatPropertyPanel>();
    //        scaleProperty.Text = "ScaleY";
    //        scaleProperty.UseWheel = true;
    //        scaleProperty.WheelStep = 0.01f;
    //        scaleProperty.Init();
    //        scaleProperty.Value = triangulationStyle.ScaleY;
    //        scaleProperty.OnValueChanged += (float value) => triangulationStyle.ScaleY = value;
    //        AddOnHoverLeave(scaleProperty, onHover, onLeave);
    //        return scaleProperty;
    //    }

    //    public override FillerStyle CopyFillerStyle() => new TriangulationFillerStyle(Color, Width, MedianOffset, MinAngle, MinLength, MaxLength);
    //}
}
