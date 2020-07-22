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
    public interface IStrokeFiller
    {
        float Angle { get; set; }
        float Step { get; set; }
        float Offset { get; set; }
    }

    public class StrokeFillerStyle : FillerStyle, IStrokeFiller
    {
        public override StyleType Type => StyleType.FillerStroke;

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

        public StrokeFillerStyle(Color32 color, float width, float angle, float step, float offset) : base(color, width)
        {
            Angle = angle;
            Step = step;
            Offset = offset;
        }
        public override IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler)
        {
            var parts = filler.Parts.Select(p => p.GetTrajectory()).ToArray();

            foreach (var point in GetLines(filler.Rect, filler.Markup.Height, out Vector3 normal))
            {
                var intersectSet = new HashSet<float>();
                foreach (var part in parts)
                {
                    foreach (var t in MarkupLineIntersect.Intersect(part, point, point + normal))
                        intersectSet.Add(t);
                }

                var intersects = intersectSet.OrderBy(i => i).ToArray();

                for (var i = 1; i < intersects.Length; i += 2)
                {
                    var start = point + normal * intersects[i - 1];
                    var end = point + normal * intersects[i];

                    var pos = (start + end) / 2;
                    var angle = Mathf.Atan2(normal.z, normal.x);
                    var length = (end - start).magnitude - (2 * Offset);

                    if (length > 0)
                        yield return new MarkupStyleDash(pos, angle, length, Width, Color);
                }
            }

            yield break;
        }
        private Vector3[] GetLines(Rect rect, float height, out Vector3 normal)
        {
            var absAngle = Mathf.Abs(Angle) * Mathf.Deg2Rad;
            var railLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = railLength * Mathf.Sin(absAngle);
            var dy = railLength * Mathf.Cos(absAngle);

            Line3 rail;
            if (Angle == -90 || Angle == 90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMax, height, rect.yMax));
            else if (90 > Angle && Angle > 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin + dx, height, rect.yMax - dy));
            else if (Angle == 0)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMin));
            else if (0 > Angle && Angle > -90)
                rail = new Line3(new Vector3(rect.xMin, height, rect.yMin), new Vector3(rect.xMin + dx, height, rect.yMin + dy));
            else
            {
                normal = Vector3.zero;
                return new Vector3[0];
            }

            var dir = rail.b - rail.a;
            var length = dir.magnitude;
            dir.Normalize();
            normal = dir.Turn90(false);
            var count = Math.Max((int)(length / Step) - 1, 0);
            var start = (length - (Step * count)) / 2;

            var result = new Vector3[count];
            for (var i = 0; i < count; i += 1)
            {
                var pos = rail.a + dir * (i * Step + start);
                result[i] = pos;
            }
            return result;
        }

        public override FillerStyle CopyFillerStyle() => new StrokeFillerStyle(Color, Width, Angle, Step, Offset);

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
}
