using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IDashedLine
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine
    {
        float Offset { get; set; }
    }

    public abstract class LineStyle
    {
        public static Color DefaultColor { get; } = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffser { get; } = 0.15f;

        public static float AngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;

        public static SolidLineStyle DefaultSolid => new SolidLineStyle(DefaultColor);
        public static DashedLineStyle DefaultDashed => new DashedLineStyle(DefaultColor, DefaultDashLength, DefaultSpaceLength);
        public static DoubleSolidLineStyle DefaultDoubleSolid => new DoubleSolidLineStyle(DefaultColor, DefaultOffser);
        public static DoubleDashedStyle DefaultDoubleDashed => new DoubleDashedStyle(DefaultColor, DefaultDashLength, DefaultSpaceLength, DefaultOffser);

        public static LineStyle GetDefault(Type type)
        {
            switch(type)
            {
                case Type.Solid: return DefaultSolid;
                case Type.Dash: return DefaultDashed;
                case Type.DoubleSolid: return DefaultDoubleSolid;
                case Type.DoubleDash: return DefaultDoubleDashed;
                default: return null;
            }    
        }

        public Color Color { get; set; }
        public abstract Type LineType { get; }

        public LineStyle(Color color)
        {
            Color = color;
        }

        public abstract IEnumerable<MarkupDash> Calculate(Bezier3 trajectory);

        public enum Type
        {
            Solid,
            Dash,
            DoubleSolid,
            DoubleDash
        }
    }
    public class SolidLineStyle : LineStyle
    {
        public override Type LineType { get; } = Type.Solid;

        public SolidLineStyle(Color color) : base(color) { }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            var deltaAngle = trajectory.DeltaAngle();
            var direction = trajectory.d - trajectory.a;
            var length = direction.magnitude;

            if ((180 - deltaAngle > AngleDelta || length > MaxLength) && length >= MinLength)
            {
                trajectory.Divide(out Bezier3 first, out Bezier3 second);
                foreach (var dash in Calculate(first))
                {
                    yield return dash;
                }
                foreach (var dash in Calculate(second))
                {
                    yield return dash;
                }
            }
            else
            {
                foreach(var dash in CalculateDashes(trajectory, direction, length))
                {
                    yield return dash;
                }
            }
        }
        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, Vector3 direction, float length)
        {
            var position = (trajectory.d + trajectory.a) / 2;
            var angle = Mathf.Atan2(direction.z, direction.x);
            var dash = new MarkupDash(position, angle, length, Color);
            yield return dash;
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IDoubleLine
    {
        public override Type LineType { get; } = Type.DoubleSolid;
        public float Offset { get; set; }

        public DoubleSolidLineStyle(Color color, float offset) : base(color) 
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, Vector3 direction, float length)
        {
            var startDirection = (trajectory.b - trajectory.a).Turn90(true).normalized;
            var endDirection = (trajectory.d - trajectory.c).Turn90(true).normalized;

            yield return CalculateDash(trajectory, startDirection, endDirection, 1);
            yield return CalculateDash(trajectory, startDirection, endDirection, -1);
        }
        private MarkupDash CalculateDash(Bezier3 trajectory, Vector3 startDirection, Vector3 endDirection, float sign)
        {
            var startPosition = trajectory.a + sign * startDirection * Offset;
            var endPosition = trajectory.d + sign * endDirection * Offset;

            var position = (endPosition + startPosition) / 2;
            var direction = endPosition - startPosition;
            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, direction.magnitude, Color);

            return dash;
        }
    }
    public class DashedLineStyle : LineStyle, IDashedLine
    {
        public override Type LineType { get; } = Type.Dash;
        public float DashLength { get; set; }
        public float SpaceLength { get; set; }

        public DashedLineStyle(Color color, float dashLength, float spaceLength) : base(color)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            var length = trajectory.Length();
            var dashCount = (int)((length - SpaceLength) / (DashLength + SpaceLength));

            var startSpaceT = (1 - ((DashLength + SpaceLength) * dashCount - SpaceLength) / length) / 2;
            var dashT = DashLength / length;
            var spaceT = DashLength / length;

            int index = 0;
            while(true)
            {
                var startT = startSpaceT + (dashT + spaceT) * index;
                var endT = startT + dashT;

                if (endT >= 1)
                    break;

                foreach (var dash in CalculateDashes(trajectory, startT, endT))
                    yield return dash;

                index += 1;
            }
        }

        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            var position = (startPosition + endPosition) / 2;
            var direction = trajectory.Tangent((startT + endT) / 2);

            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, DashLength, Color);
            yield return dash;
        }
    }
    public class DoubleDashedStyle : DashedLineStyle, IDoubleLine
    {
        public override Type LineType { get; } = Type.DoubleDash;
        public float Offset { get; set; }
        public DoubleDashedStyle(Color color, float dashLength, float spaceLength, float offset) : base(color, dashLength, spaceLength)
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            var startDirection = trajectory.Tangent(startT).Turn90(true).normalized;
            var endDirection = trajectory.Tangent(endT).Turn90(true).normalized;

            yield return CalculateDash(startPosition, endPosition, startDirection, endDirection, 1);
            yield return CalculateDash(startPosition, endPosition, startDirection, endDirection, -1);
        }
        private MarkupDash CalculateDash(Vector3 startCentrePos, Vector3 endCentrePos, Vector3 startDirection, Vector3 endDirection, float sign)
        {
            var startPosition = startCentrePos + sign * startDirection * Offset;
            var endPosition = endCentrePos + sign * endDirection * Offset;

            var position = (startPosition + endPosition) / 2;
            var direction = (endPosition - startPosition);

            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, DashLength, Color);
            return dash;
        }
    }

    public class MarkupDash
    {
        public Vector3 Position { get; }
        public float Angle { get; }
        public float Length { get; }
        //public float Width { get; }
        public Color Color { get; }

        public MarkupDash(Vector3 position, float angle, float length, /*float width, */Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            //Width = width;
            Color = color;
        }
    }
}
