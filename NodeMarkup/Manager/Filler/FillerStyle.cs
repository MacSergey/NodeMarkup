using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, Angle, rect, height, Width, Step, Offset);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
    }
    public class GridFillerStyle : SimpleFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);

        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height)
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
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, 0f, rect, height, DefaultSolidWidth, 1, 0);

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
        float _offset;
        bool _invert;

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
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }

        public ChevronFillerStyle(Color32 color, float width, float medianOffset, float angleBetween, float step, float offset, bool invert) : base(color, width, medianOffset)
        {
            AngleBetween = angleBetween;
            Step = step;
            Offset = offset;
            Invert = invert;
        }

        public override FillerStyle CopyFillerStyle() => new ChevronFillerStyle(Color, Width, MedianOffset, AngleBetween, Step, Offset, Invert);
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddAngleBetweenProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            components.Add(AddInvertProperty(this, parent));
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
        protected static BoolPropertyPanel AddInvertProperty(ChevronFillerStyle chevronStyle, UIComponent parent)
        {
            var invertProperty = parent.AddUIComponent<BoolPropertyPanel>();
            invertProperty.Text = Localize.Filler_Invert;
            invertProperty.Init();
            invertProperty.Value = chevronStyle.Invert;
            invertProperty.OnValueChanged += (bool value) => chevronStyle.Invert = value;
            return invertProperty;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler)
        {
            var trajectories = filler.Trajectories.ToArray();
            if (trajectories.Length != 3)
                return new MarkupStyleDash[0];

            if (filler.IsMedian)
                trajectories = GetTrajectoriesWithoutMedian(trajectories, filler.Parts.ToArray());

            var middle = GetMiddle(trajectories);

            return GetDashes(trajectories, middle);
        }
        private Bezier3 GetMiddle(Bezier3[] trajectories)
        {
            var middle = new Bezier3()
            {
                a = (trajectories[0].d + trajectories[1].a) / 2,
                b = (((trajectories[0].c - trajectories[0].d) + (trajectories[1].b - trajectories[1].a)) / 2).normalized,
                c = (((trajectories[0].b - trajectories[0].a) + (trajectories[1].c - trajectories[1].d)) / 2).normalized,
                d = (trajectories[0].a + trajectories[1].d) / 2,
            };
            NetSegment.CalculateMiddlePoints(middle.a, middle.b, middle.d, middle.c, true, true, out middle.b, out middle.c);

            if (MarkupLineIntersect.Intersect(middle, trajectories[2], out float cutT, out _))
                middle = middle.Cut(0, cutT);

            return middle;
        }
        private IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] trajectories, Bezier3 middle)
        {
            var halfAngelRad = (Invert ? 360 - AngleBetween : AngleBetween) * Mathf.Deg2Rad / 2;
            var width = Width / Mathf.Sin(halfAngelRad);

            GetParts(width, 0, out int partsCount, out float partWidth);

            var length = middle.Length() + width * (Step - 1);
            var itemLength = width * Step;
            var itemsCount = Math.Max((int)(length / itemLength) - 1, 0);
            var start = (length - (itemLength * itemsCount)) / 2;

            var getDashFunc = Offset == 0 ? (GetDashesDelegate)GetItemDashes : (GetDashesDelegate)GetItemDashesWithOffset;

            for (var i = 0; i < itemsCount; i += 1)
            {
                var itemStart = start + partWidth / 2 + i * itemLength;
                var itemStartT = middle.Travel(0f, itemStart);
                var dir = middle.Tangent(itemStartT);
                var leftDir = dir.Turn(halfAngelRad, false).normalized;
                var rightDir = dir.Turn(halfAngelRad, true).normalized;

                foreach (var dash in getDashFunc(trajectories, middle, itemStart, itemStartT, leftDir, partsCount, partWidth))
                    yield return dash;

                foreach (var dash in getDashFunc(trajectories, middle, itemStart, itemStartT, rightDir, partsCount, partWidth))
                    yield return dash;
            }
        }
        private delegate IEnumerable<MarkupStyleDash> GetDashesDelegate(Bezier3[] trajectories, Bezier3 middle, float start, float startT, Vector3 dir, int partsCount, float partWidth);
        private IEnumerable<MarkupStyleDash> GetItemDashes(Bezier3[] trajectories, Bezier3 middle, float start, float startT, Vector3 dir, int partsCount, float partWidth)
        {
            for (var i = 0; i < partsCount; i += 1)
            {
                var startPos = GetPartPos(middle, start, startT, partWidth, i);
                if (!(GetIntersect(trajectories, startPos, dir) is MarkupFillerIntersect intersect))
                    continue;
                var endPos = startPos + dir * intersect.FirstT;

                yield return new MarkupStyleDash(startPos, endPos, dir, partWidth, Color);
            }
        }
        private IEnumerable<MarkupStyleDash> GetItemDashesWithOffset(Bezier3[] trajectories, Bezier3 middle, float start, float startT, Vector3 dir, int partsCount, float partWidth)
        {
            var mainStartPos = GetPartPos(middle, start, startT, partWidth, 0);
            if (!(GetIntersect(trajectories, mainStartPos, dir) is MarkupFillerIntersect intersect))
                yield break;

            var mainEndPos = mainStartPos + dir * intersect.FirstT;
            var offset = GetOffset(intersect, Offset);

            if ((mainEndPos - mainStartPos).magnitude < offset)
                yield break;
            else
            {
                mainEndPos -= dir * offset;
                yield return new MarkupStyleDash(mainStartPos, mainEndPos, dir, partWidth, Color);
            }

            var normalStart = mainEndPos;
            var normalEnd = dir.Turn90(true);

            for (var i = 1; i < partsCount; i += 1)
            {
                var startPos = GetPartPos(middle, start, startT, partWidth, i);
                if (Line2.Intersect(startPos.XZ(), (startPos + dir).XZ(), normalStart.XZ(), normalEnd.XZ(), out float v, out _) && v > 0)
                {
                    var endPos = startPos + v * dir;
                    yield return new MarkupStyleDash(startPos, endPos, dir, partWidth, Color);
                }
            }
        }

        private Vector3 GetPartPos(Bezier3 middle, float start, float startT, float partWidth, int index)
        {
            var partStart = start + partWidth * index;

            if (partStart.NearlyEqual(start, Vector3.kEpsilon))
                return middle.Position(startT);
            else
            {
                var partT = middle.Travel(startT, partStart - start);
                return middle.Position(partT);
            }
        }
        private MarkupFillerIntersect GetIntersect(Bezier3[] trajectories, Vector3 pos, Vector3 dir)
        {
            var result = default(MarkupFillerIntersect);
            foreach (var trajectory in trajectories)
            {
                foreach (var intersect in MarkupFillerIntersect.Intersect(trajectory, pos, pos + dir))
                {
                    if (intersect.FirstT >= 0 && (result == null || intersect.FirstT < result.FirstT))
                        result = intersect;
                }
            }
            return result;
        }

        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] trajectories, Rect rect, float height) => throw new NotSupportedException();

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("A", AngleBetween));
            config.Add(new XAttribute("S", Step));
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("I", Invert));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            AngleBetween = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStepGrid);
            Offset = config.GetAttrValue("O", DefaultOffset);
            Invert = config.GetAttrValue("I", false);
        }
    }
}
