using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var intersect = new List<float>();
                foreach (var part in parts)
                {
                    if (MarkupLineIntersect.Intersect(part, point, point + normal, out _, out float t))
                        intersect.Add(t);
                }

                intersect.Sort();

                for (var i = 1; i < intersect.Count; i += 2)
                {
                    var start = point + normal * intersect[i - 1];
                    var end = point + normal * intersect[i];

                    var pos = (start + end) / 2;
                    var angle = Mathf.Atan2(normal.z, normal.x);
                    var length = (end - start).magnitude - (2 * Offset);

                    if(length > 0)
                        yield return new MarkupStyleDash(pos, angle, length, Width, Color);
                }
            }

            yield break;
        }
        private Vector3[] GetLines(Rect rect, float height, out Vector3 normal)
        {
            var xDelta = Math.Max(rect.height - rect.width, 0);
            var yDelta = Math.Max(rect.width - rect.height, 0);
            var square = Rect.MinMaxRect(rect.xMin - xDelta, rect.yMin - yDelta, rect.xMax + xDelta, rect.yMax + yDelta);

            Line3 rail = default;
            var angle = Mathf.Abs(Angle) <= 45 ? Angle : 90 - Angle;
            var delta = Mathf.Tan(angle * Mathf.Deg2Rad) * square.width;

            if (Angle == -90 || Angle == 90)
                rail = new Line3(new Vector3(square.xMin, height, square.yMax), new Vector3(square.xMax, height, square.yMax));
            else if (90 > Angle && Angle > 45)
                rail = new Line3(new Vector3(square.xMin, height, square.yMax), new Vector3(square.xMax, height, square.yMax - delta));
            else if (Angle == 45)
                rail = new Line3(new Vector3(square.xMin, height, square.yMax), new Vector3(square.xMax, height, square.yMin));
            else if (45 > Angle && Angle > 0)
                rail = new Line3(new Vector3(square.xMin, height, square.yMax), new Vector3(square.xMin + delta, height, square.yMin));
            else if (Angle == 0)
                rail = new Line3(new Vector3(square.xMin, height, square.yMax), new Vector3(square.xMin, height, square.yMin));
            else if (0 > Angle && Angle > -45)
                rail = new Line3(new Vector3(square.xMin, height, square.yMin), new Vector3(square.xMin + delta, height, square.yMax));
            else if (Angle == -45)
                rail = new Line3(new Vector3(square.xMax, height, square.yMax), new Vector3(square.xMin, height, square.yMin));
            else if (-45 > Angle && Angle > -90)
                rail = new Line3(new Vector3(square.xMin, height, square.yMin), new Vector3(square.xMin, height, square.yMin + delta));

            var dir = rail.b - rail.a;
            var length = dir.magnitude;
            dir.Normalize();
            normal = dir.Turn90(false);
            var count = Math.Max((int)(length / Step) - 1, 0);
            var start = (length - (Step * count)) / 2;

            var result = new Vector3[count];
            for(var i = 0; i < count; i +=1)
            {
                var pos = rail.a + dir * (i * Step + start);
                result[i] = pos;
            }
            return result;
        }

        public override FillerStyle Copy() => new StrokeFillerStyle(Color, Width, Angle, Step, Offset);
    }
}
