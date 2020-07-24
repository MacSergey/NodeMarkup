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
    }

    public abstract class SimpleFillerStyle : FillerStyle, ISimpleFiller
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
        public SimpleFillerStyle(Color32 color, float width, float angle, float step, float offset) : base(color, width)
        {
            Angle = angle;
            Step = step;
            Offset = offset;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler)
        {
            var parts = filler.Parts.Select(p => p.GetTrajectory()).ToArray();
            return GetDashes(parts, filler.Rect, filler.Markup.Height);
        }
        protected abstract IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height);
        protected IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, float angleDeg, Rect rect, float height)
        {
            foreach (var point in GetLines(angleDeg, rect, height, out Vector3 normal))
            {
                var intersectSet = new HashSet<MarkupFillerIntersect>();
                foreach (var part in parts)
                {
                    foreach (var t in MarkupFillerIntersect.Intersect(part, point, point + normal))
                        intersectSet.Add(t);
                }

                var intersects = intersectSet.OrderBy(i => i).ToArray();

                for (var i = 1; i < intersects.Length; i += 2)
                {
                    var start = point + normal * intersects[i - 1].FirstT;
                    var end = point + normal * intersects[i].FirstT;
                    var startOffset = GetOffset(intersects[i - 1]);
                    var endOffset = GetOffset(intersects[i]);

                    if ((end - start).magnitude - Width < startOffset + endOffset)
                        continue;

                    var sToE = intersects[i].FirstT >= intersects[i - 1].FirstT;
                    start += normal * (sToE ? startOffset : -startOffset);
                    end += normal * (sToE ? -endOffset : endOffset);

                    var pos = (start + end) / 2;
                    var angle = Mathf.Atan2(normal.z, normal.x);
                    var length = (end - start).magnitude;

                    yield return new MarkupStyleDash(pos, angle, length, Width, Color);

                    float GetOffset(MarkupFillerIntersect intersect)
                    {
                        var sin = Mathf.Sin(intersect.Angle);
                        return sin != 0 ? Offset / sin : 1000f;
                    }
                }
            }
        }
        protected Vector3[] GetLines(float angle, Rect rect, float height, out Vector3 normal)
        {
            var absAngle = Mathf.Abs(angle) * Mathf.Deg2Rad;
            var railLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = railLength * Mathf.Sin(absAngle);
            var dy = railLength * Mathf.Cos(absAngle);

            Line3 rail;
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
                normal = Vector3.zero;
                return new Vector3[0];
            }

            var dir = rail.b - rail.a;
            var length = dir.magnitude + Width * (Step - 1);
            dir.Normalize();
            normal = dir.Turn90(false);
            var itemLength = Width * Step;
            var count = Math.Max((int)(length / itemLength) - 1, 0);
            var start = (length - (itemLength * count)) / 2;

            var result = new Vector3[count];
            for (var i = 0; i < count; i += 1)
            {
                var pos = rail.a + dir * (start + Width / 2 + i * itemLength);
                result[i] = pos;
            }
            return result;
        }

        public override List<UIComponent> GetUIComponents(UIComponent parent, Action onHover = null, Action onLeave = null)
        {
            var components = base.GetUIComponents(parent, onHover, onLeave);
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
            Step = config.GetAttrValue("S", DefaultStep);
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }

    public class StripeFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerStripe;

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset) : base(color, width, angle, step, offset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, Angle, rect, height);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, Angle, Step, Offset);
    }
    public class GridFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset) : base(color, width, angle, step, offset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height)
        {
            foreach (var dash in GetDashes(parts, Angle, rect, height))
                yield return dash;
            foreach (var dash in GetDashes(parts, Angle < 0 ? Angle + 90 : Angle - 90, rect, height))
                yield return dash;
        }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, Angle, Step, Offset);
    }
}
