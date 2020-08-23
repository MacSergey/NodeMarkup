using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        public static float DefaultStepStripe { get; } = 3f;
        public static float DefaultStepGrid { get; } = 6f;
        public static float DefaultOffset { get; } = 0f;
        public static float StripeDefaultWidth { get; } = 0.5f;
        public static float DefaultAngleBetween { get; } = 90f;

        static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultAngle, DefaultStepStripe, DefaultOffset, DefaultOffset)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
        };

        public static FillerStyle GetDefault(FillerType type) => Defaults.TryGetValue(type, out FillerStyle style) ? style.CopyFillerStyle() : null;

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
            var trajectories = filler.IsMedian ? GetTrajectoriesWithoutMedian(filler) : filler.Trajectories.ToArray();
            var rect = GetRect(trajectories);
            return GetDashes(trajectories, rect, filler.Markup.Height);
        }
        public ILineTrajectory[] GetTrajectoriesWithoutMedian(MarkupFiller filler)
        {
            var lineParts = filler.Parts.ToArray();
            var trajectories = filler.TrajectoriesRaw.ToArray();

            for (var i = 0; i < lineParts.Length; i += 1)
            {
                if (trajectories[i] == null)
                    continue;

                var line = lineParts[i].Line;
                if (line is MarkupEnterLine)
                    continue;

                var prevI = i == 0 ? lineParts.Length - 1 : i - 1;
                if (lineParts[prevI].Line is MarkupEnterLine && trajectories[prevI] != null)
                {
                    trajectories[i] = Shift(trajectories[i]);
                    trajectories[prevI] = new StraightTrajectory(trajectories[prevI].StartPosition, trajectories[i].StartPosition);
                }

                var nextI = i + 1 == lineParts.Length ? 0 : i + 1;
                if (lineParts[nextI].Line is MarkupEnterLine && trajectories[nextI] != null)
                {
                    trajectories[i] = Shift(trajectories[i].Invert()).Invert();
                    trajectories[nextI] = new StraightTrajectory(trajectories[i].EndPosition, trajectories[nextI].EndPosition);
                }

                ILineTrajectory Shift(ILineTrajectory trajectory)
                {
                    var newT = trajectory.Travel(0, MedianOffset);
                    return trajectory.Cut(newT, 1);
                }
            }

            return trajectories.Where(t => t != null).Select(t => t).ToArray();
        }
        protected abstract IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] trajectories, Rect rect, float height);

        protected IEnumerable<MarkupStyleDash> GetDashes(ILineTrajectory[] trajectories, float angleDeg, Rect rect, float height, float width, float step, float offset)
        {
            foreach (var point in GetItems(angleDeg, rect, height, width, step, offset, out Vector3 normal, out float partWidth))
            {
                var intersectSet = new HashSet<MarkupIntersect>();
                var straight = new StraightTrajectory(point, point + normal, false);
                foreach (var trajectory in trajectories)
                    intersectSet.AddRange(MarkupIntersect.Calculate(straight, trajectory));

                var intersects = intersectSet.OrderBy(i => i, MarkupIntersect.FirstComparer).ToArray();

                for (var i = 1; i < intersects.Length; i += 2)
                {
                    var start = point + normal * intersects[i - 1].FirstT;
                    var end = point + normal * intersects[i].FirstT;

                    if (offset != 0)
                    {
                        var startOffset = GetOffset(intersects[i - 1], offset);
                        var endOffset = GetOffset(intersects[i], offset);

                        if ((end - start).magnitude - Width < startOffset + endOffset)
                            continue;

                        var isStartToEnd = intersects[i].FirstT >= intersects[i - 1].FirstT;
                        start += normal * (isStartToEnd ? startOffset : -startOffset);
                        end += normal * (isStartToEnd ? -endOffset : endOffset);
                    }

                    yield return new MarkupStyleDash(start, end, normal, partWidth, Color);
                }
            }
        }
        protected float GetOffset(MarkupIntersect intersect, float offset)
        {
            var sin = Mathf.Sin(intersect.Angle);
            return sin != 0 ? offset / sin : 1000f;
        }
        protected List<Vector3> GetItems(float angle, Rect rect, float height, float width, float step, float offset, out Vector3 normal, out float partWidth)
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
            var itemsCount = Math.Max((int)(length / itemLength) - 1, 0);
            var start = (length - (itemLength * itemsCount)) / 2;

            StyleHelper.GetParts(width, offset, out int partsCount, out partWidth);

            for (var i = 0; i < itemsCount; i += 1)
            {
                var stripStart = start + partWidth / 2 + i * itemLength;
                for (var j = 0; j < partsCount; j += 1)
                {
                    results.Add(rail.a + dir * (stripStart + partWidth * j));
                }
            }

            return results;
        }
        protected bool GetRail(float SceneAngle, Rect rect, float height, out Line3 rail)
        {
            if (SceneAngle > 90)
                SceneAngle -= 180;
            else if (SceneAngle < -90)
                SceneAngle += 180;

            var absAngle = Mathf.Abs(SceneAngle) * Mathf.Deg2Rad;
            var railLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = railLength * Mathf.Sin(absAngle);
            var dy = railLength * Mathf.Cos(absAngle);

            if (SceneAngle == -90 || SceneAngle == 90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMax, height, rect.yMax));
            else if (90 > SceneAngle && SceneAngle > 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin + dx, height, rect.yMax - dy));
            else if (SceneAngle == 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMin));
            else if (0 > SceneAngle && SceneAngle > -90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMin), new Vector3(rect.xMin + dx, height, rect.yMin + dy));
            else
            {
                rail = default;
                return false;
            }

            return true;
        }
        protected Rect GetRect(ILineTrajectory[] trajectories)
        {
            if (!trajectories.Any())
                return Rect.zero;

            var firstPos = trajectories[0].StartPosition;
            var rect = Rect.MinMaxRect(firstPos.x, firstPos.z, firstPos.x, firstPos.z);

            foreach (var trajectory in trajectories)
            {
                switch (trajectory)
                {
                    case BezierTrajectory bezierTrajectory:
                        Set(bezierTrajectory.Trajectory.a);
                        Set(bezierTrajectory.Trajectory.b);
                        Set(bezierTrajectory.Trajectory.c);
                        Set(bezierTrajectory.Trajectory.d);
                        break;
                    case StraightTrajectory straightTrajectory:
                        Set(straightTrajectory.Trajectory.a);
                        Set(straightTrajectory.Trajectory.b);
                        break;
                }
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
        protected static FloatPropertyPanel AddAngleProperty(IRotateFiller rotateStyle, UIComponent parent, Action onHover, Action onLeave)
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
            angleProperty.Value = rotateStyle.Angle;
            angleProperty.OnValueChanged += (float value) => rotateStyle.Angle = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static FloatPropertyPanel AddStepProperty(IPeriodicFiller periodicStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var stepProperty = parent.AddUIComponent<FloatPropertyPanel>();
            stepProperty.Text = Localize.Filler_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.Init();
            stepProperty.Value = periodicStyle.Step;
            stepProperty.OnValueChanged += (float value) => periodicStyle.Step = value;
            AddOnHoverLeave(stepProperty, onHover, onLeave);
            return stepProperty;
        }
        protected static FloatPropertyPanel AddOffsetProperty(IPeriodicFiller periodicStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.Filler_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = periodicStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => periodicStyle.Offset = value;
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

            [Description(nameof(Localize.FillerStyle_Chevron))]
            Chevron = StyleType.FillerChevron,
        }
    }
}
