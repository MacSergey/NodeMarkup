using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IFillerStyle : IStyle, IColorStyle 
    {
        float MedianOffset { get; set; }
    }
    public abstract class FillerStyle : Style, IFillerStyle
    {
        public static float DefaultAngle { get; } = 0f;
        public static float DefaultStep { get; } = 6f;
        public static float DefaultOffset { get; } = 0f;
        public static float StripeDefaultWidth { get; } = 0.5f;

        public static StripeFillerStyle DefaultStripe => new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultAngle, DefaultStep, DefaultOffset, DefaultOffset);
        public static GridFillerStyle DefaultGrid => new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStep, DefaultOffset, DefaultOffset);
        public static SolidFillerStyle DefaultSolid => new SolidFillerStyle(DefaultColor, DefaultOffset);

        public static FillerStyle GetDefault(FillerType type)
        {
            switch (type)
            {
                case FillerType.Stripe: return DefaultStripe;
                case FillerType.Grid: return DefaultGrid;
                case FillerType.Solid: return DefaultSolid;
                default: return null;
            }
        }

        float _medianOffset;
        public float MedianOffset
        {
            get => _medianOffset;
            set
            {
                _medianOffset = value;
                StyleChanged();
            }
        }

        public FillerStyle(Color32 color, float width, float medianOffset) : base(color, width) 
        {
            MedianOffset = medianOffset;
        }

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IFillerStyle fillerTarget)
            {
                fillerTarget.MedianOffset = MedianOffset;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (!isTemplate && editObject is MarkupFiller filler && filler.IsMedian)
                components.Add(AddMedianOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override Style Copy() => CopyFillerStyle();
        public abstract FillerStyle CopyFillerStyle();
        public virtual IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler)
        {
            var trajectories = filler.Trajectories.ToArray();
            if (filler.IsMedian)
                GetTrajectoriesWithoutMedian(trajectories, filler.Parts.ToArray());

            var rect = GetRect(trajectories);
            return GetDashes(trajectories, rect, filler.Markup.Height);
        }
        public IEnumerable<Bezier3> GetTrajectoriesWithoutMedian(Bezier3[] trajectories, MarkupLinePart[] lineParts)
        {
            for (var i = 0; i < lineParts.Length; i += 1)
            {
                var line = lineParts[i].Line;
                if (line is MarkupFakeLine)
                    continue;

                var prevI = i == 0 ? lineParts.Length - 1 : i - 1;
                if (lineParts[prevI].Line is MarkupFakeLine)
                {
                    trajectories[i] = Shift(trajectories[i]);
                    trajectories[prevI].d = trajectories[prevI].b = trajectories[i].a;
                }

                var nextI = i + 1 == lineParts.Length ? 0 : i + 1;
                if (lineParts[nextI].Line is MarkupFakeLine)
                {
                    trajectories[i] = Shift(trajectories[i].Invert()).Invert();
                    trajectories[nextI].a = trajectories[nextI].c = trajectories[i].d;
                }

                Bezier3 Shift(Bezier3 trajectory)
                {
                    var newT = trajectory.Travel(0, MedianOffset);
                    return trajectory.Cut(newT, 1);
                }
            }

            return trajectories;
        }
        protected abstract IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] trajectories, Rect rect, float height);

        protected IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] trajectories, float angleDeg, Rect rect, float height, float width, float step, float offset)
        {
            foreach (var point in GetLines(angleDeg, rect, height, width, step, offset, out Vector3 normal, out float partWidth))
            {
                var intersectSet = new HashSet<MarkupFillerIntersect>();
                foreach (var trajectory in trajectories)
                {
                    foreach (var t in MarkupFillerIntersect.Intersect(trajectory, point, point + normal))
                        intersectSet.Add(t);
                }

                var intersects = intersectSet.OrderBy(i => i).ToArray();

                for (var i = 1; i < intersects.Length; i += 2)
                {
                    if(Mathf.Abs(intersects[i].FirstT - intersects[i - 1].FirstT) < 0.1f)
                    {
                        i -= 1;
                        continue;
                    }

                    var start = point + normal * intersects[i - 1].FirstT;
                    var end = point + normal * intersects[i].FirstT;

                    if (offset != 0)
                    {
                        var startOffset = GetOffset(intersects[i - 1]);
                        var endOffset = GetOffset(intersects[i]);

                        if ((end - start).magnitude - Width < startOffset + endOffset)
                            continue;

                        var sToE = intersects[i].FirstT >= intersects[i - 1].FirstT;
                        start += normal * (sToE ? startOffset : -startOffset);
                        end += normal * (sToE ? -endOffset : endOffset);
                    }

                    var pos = (start + end) / 2;
                    var angle = Mathf.Atan2(normal.z, normal.x);
                    var length = (end - start).magnitude;

                    yield return new MarkupStyleDash(pos, angle, length, partWidth, Color);

                    float GetOffset(MarkupFillerIntersect intersect)
                    {
                        var sin = Mathf.Sin(intersect.Angle);
                        return sin != 0 ? offset / sin : 1000f;
                    }
                }
            }
        }
        protected List<Vector3> GetLines(float angle, Rect rect, float height, float width, float step, float offset, out Vector3 normal, out float partWidth)
        {
            var results = new List<Vector3>();

            if (!GetRail(angle, rect, height, out Line3 rail))
            {
                normal = Vector3.zero;
                partWidth = width;
                return results;
            }

            var dir = rail.b - rail.a;
            var length = dir.magnitude + width * (step - 1);
            dir.Normalize();
            normal = dir.Turn90(false);

            var itemLength = width * step;
            var stripeCount = Math.Max((int)(length / itemLength) - 1, 0);
            var start = (length - (itemLength * stripeCount)) / 2;

            GetParts(width, offset, out int partsCount, out partWidth);

            for (var i = 0; i < stripeCount; i += 1)
            {
                var stripStart = start + partWidth / 2 + i * itemLength;
                for (var j = 0; j < partsCount; j += 1)
                {
                    results.Add(rail.a + dir * (stripStart + partWidth * j));
                }
            }

            return results;
        }
        private bool GetRail(float angle, Rect rect, float height, out Line3 rail)
        {
            var absAngle = Mathf.Abs(angle) * Mathf.Deg2Rad;
            var railLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = railLength * Mathf.Sin(absAngle);
            var dy = railLength * Mathf.Cos(absAngle);

            if (angle == -90 || angle == 90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMax, height, rect.yMax));
            else if (90 > angle && angle > 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin + dx, height, rect.yMax - dy));
            else if (angle == 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMin));
            else if (0 > angle && angle > -90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMin), new Vector3(rect.xMin + dx, height, rect.yMin + dy));
            else
            {
                rail = default;
                return false;
            }

            return true;
        }
        private void GetParts(float width, float offset, out int count, out float partWidth)
        {
            if (width < 0.2f || offset != 0f)
            {
                count = 1;
                partWidth = width;
            }
            else
            {
                var intWidth = (int)(width * 100);
                var delta = 20;
                var num = 0;
                for (var i = 10; i < 20; i += 1)
                {
                    var iDelta = intWidth - (intWidth / i) * i;
                    if (iDelta < delta)
                    {
                        delta = iDelta;
                        num = i;
                    }
                }
                count = intWidth / num;
                partWidth = num / 100f;
            }
        }
        protected Rect GetRect(Bezier3[] trajectories)
        {
            if (!trajectories.Any())
                return Rect.zero;

            var firstPos = trajectories[0].a;
            var rect = Rect.MinMaxRect(firstPos.x, firstPos.z, firstPos.x, firstPos.z);

            foreach (var trajectory in trajectories)
            {
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

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("MO", MedianOffset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            MedianOffset = config.GetAttrValue("MO", DefaultOffset);
        }

        protected static FloatPropertyPanel AddStepProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var stepProperty = parent.AddUIComponent<FloatPropertyPanel>();
            stepProperty.Text = Localize.Filler_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.Init();
            stepProperty.Value = stripeStyle.Step;
            stepProperty.OnValueChanged += (float value) => stripeStyle.Step = value;
            AddOnHoverLeave(stepProperty, onHover, onLeave);
            return stepProperty;
        }
        protected static FloatPropertyPanel AddAngleProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = parent.AddUIComponent<FloatPropertyPanel>();
            angleProperty.Text = Localize.Filler_Angle;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -90;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 90;
            angleProperty.Init();
            angleProperty.Value = stripeStyle.Angle;
            angleProperty.OnValueChanged += (float value) => stripeStyle.Angle = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static FloatPropertyPanel AddOffsetProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.Filler_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = stripeStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => stripeStyle.Offset = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }
        protected static FloatPropertyPanel AddMedianOffsetProperty(FillerStyle fillerStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.Filler_MedianOffset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = fillerStyle.MedianOffset;
            offsetProperty.OnValueChanged += (float value) => fillerStyle.MedianOffset = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }

        public enum FillerType
        {
            [Description(nameof(Localize.FillerStyle_Stripe))]
            Stripe = StyleType.FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            Grid = StyleType.FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            Solid = StyleType.FillerSolid,
        }
    }
}
