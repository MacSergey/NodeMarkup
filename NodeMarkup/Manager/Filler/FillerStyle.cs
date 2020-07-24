using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISimpleFiller : IFillerStyle
    {
        float Angle { get; set; }
        float Step { get; set; }
        float Offset { get; set; }
        float MedianOffset { get; set; }
    }

    public abstract class SimpleFillerStyle : FillerStyle, ISimpleFiller
    {
        float _angle;
        float _step;
        float _offset;
        float _medianOffset;

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
        public float MedianOffset
        {
            get => _medianOffset;
            set
            {
                _medianOffset = value;
                StyleChanged();
            }
        }

        public SimpleFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width)
        {
            Angle = angle;
            Step = step;
            Offset = offset;
            MedianOffset = medianOffset;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler)
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
        protected IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] trajectories, float angleDeg, Rect rect, float height)
        {
            foreach (var point in GetLines(angleDeg, rect, height, out Vector3 normal, out float width))
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
                    var start = point + normal * intersects[i - 1].FirstT;
                    var end = point + normal * intersects[i].FirstT;

                    if (Offset != 0)
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

                    yield return new MarkupStyleDash(pos, angle, length, width, Color);

                    float GetOffset(MarkupFillerIntersect intersect)
                    {
                        var sin = Mathf.Sin(intersect.Angle);
                        return sin != 0 ? Offset / sin : 1000f;
                    }
                }
            }
        }
        protected List<Vector3> GetLines(float angle, Rect rect, float height, out Vector3 normal, out float partWidth)
        {
            var results = new List<Vector3>();

            if (!GetRail(angle, rect, height, out Line3 rail))
            {
                normal = Vector3.zero;
                partWidth = Width;
                return results;
            }

            var dir = rail.b - rail.a;
            var length = dir.magnitude + Width * (Step - 1);
            dir.Normalize();
            normal = dir.Turn90(false);

            var itemLength = Width * Step;
            var stripeCount = Math.Max((int)(length / itemLength) - 1, 0);
            var start = (length - (itemLength * stripeCount)) / 2;

            GetParts(out int partsCount, out partWidth);

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
        private void GetParts(out int count, out float width)
        {
            if (Width < 0.2f || Offset != 0f)
            {
                count = 1;
                width = Width;
            }
            else
            {
                var intWidth = (int)(Width * 100);
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
                width = num / 100f;
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

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            if (!isTemplate && editObject is MarkupFiller filler && filler.IsMedian)
                components.Add(AddMedianOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("A", Angle));
            config.Add(new XAttribute("S", Step));
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("MO", MedianOffset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Angle = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStep);
            Offset = config.GetAttrValue("O", DefaultOffset);
            MedianOffset = config.GetAttrValue("MO", DefaultOffset);
        }
    }

    public class StripeFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerStripe;

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, Angle, rect, height);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, Angle, Step, Offset, MedianOffset);
    }
    public class GridFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height)
        {
            foreach (var dash in GetDashes(parts, Angle, rect, height))
                yield return dash;
            foreach (var dash in GetDashes(parts, Angle < 0 ? Angle + 90 : Angle - 90, rect, height))
                yield return dash;
        }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, Angle, Step, Offset, MedianOffset);
    }
}
