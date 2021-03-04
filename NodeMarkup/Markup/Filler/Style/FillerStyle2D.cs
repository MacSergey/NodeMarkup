using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
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

namespace NodeMarkup.Manager
{
    public interface IPeriodicFiller : IFillerStyle
    {
        PropertyValue<float> Step { get; }
    }
    public interface IOffsetFiller : IFillerStyle, IWidthStyle, IColorStyle
    {
        PropertyValue<float> Offset { get; }
    }
    public interface IRotateFiller : IFillerStyle, IWidthStyle, IColorStyle
    {
        PropertyValue<float> Angle { get; }
    }

    public abstract class Filler2DStyle : FillerStyle
    {
        public Filler2DStyle(Color32 color, float width, float medianOffset) : base(color, width, medianOffset) { }

        public sealed override IStyleData Calculate(MarkupFiller filler, int lod) => new MarkupStyleParts(CalculateProcess(filler, lod));
        protected virtual IEnumerable<MarkupStylePart> CalculateProcess(MarkupFiller filler, int lod)
        {
            var contour = filler.IsMedian ? SetMedianOffset(filler) : filler.Contour.Trajectories.ToArray();
            var rails = GetRails(filler, contour).ToArray();

            foreach (var rail in rails)
            {
                var partItems = GetItems(rail, lod).ToArray();

                foreach (var partItem in partItems)
                {
                    foreach (var dash in GetDashes(partItem, contour))
                        yield return dash;
                }
            }
        }


        protected abstract IEnumerable<RailLine> GetRails(MarkupFiller filler, ILineTrajectory[] contour);
        protected Rect GetRect(ILineTrajectory[] contour)
        {
            var firstPos = contour.Any() ? contour[0].StartPosition : default;
            var rect = Rect.MinMaxRect(firstPos.x, firstPos.z, firstPos.x, firstPos.z);

            foreach (var trajectory in contour)
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
        protected StraightTrajectory GetRail(Rect rect, float height, float angle)
        {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;

            var absAngle = Mathf.Abs(angle) * Mathf.Deg2Rad;
            var railLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = railLength * Mathf.Sin(absAngle);
            var dy = railLength * Mathf.Cos(absAngle);

            if (angle == -90 || angle == 90)
                return new StraightTrajectory(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMax, height, rect.yMax));
            else if (90 > angle && angle > 0)
                return new StraightTrajectory(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin + dx, height, rect.yMax - dy));
            else if (angle == 0)
                return new StraightTrajectory(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMin));
            else if (0 > angle && angle > -90)
                return new StraightTrajectory(new Vector3(rect.xMin, height, rect.yMin), new Vector3(rect.xMin + dx, height, rect.yMin + dy));
            else
                return default;
        }

        protected abstract IEnumerable<PartItem> GetItems(RailLine rail, int lod);
        protected IEnumerable<StraightTrajectory> GetParts(RailLine rail, float dash, float space)
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

                while (nextT < rail.Count)
                {
                    if (isDash)
                        dashesT.Add(new float[] { currentT, nextT });

                    isDash = !isDash;

                    prevT = currentT;
                    currentT = nextT;
                    nextT = Travel(currentT, isDash ? dash : space);
                }

                float endSpace;
                if (isDash || ((Position(rail.Count) - Position(currentT)).magnitude is float tempLength && tempLength < space / 2))
                    endSpace = (Position(rail.Count) - Position(prevT)).magnitude;
                else
                    endSpace = tempLength;

                startSpace = (startSpace + endSpace) / 2;

                if (Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    break;
            }

            foreach (var dashT in dashesT)
                yield return new StraightTrajectory(Position(dashT[0]), Position(dashT[1]));


            Vector3 Position(float t)
            {
                var i = (int)t;
                i = i > 0 && i == t ? i - 1 : i;
                return rail[i].Position(t - i);
            }
            float Travel(float current, float distance)
            {
                var i = (int)current;
                var next = 1f;
                while (i < rail.Count)
                {
                    var line = rail[i];
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
        protected void GetItemParams(ref float width, float angle, int lod, out int itemsCount, out float itemWidth, out float itemStep)
        {
            StyleHelper.GetParts(width, 0f, lod, out itemsCount, out itemWidth);

            var coef = Math.Max(Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad), 0.01f);
            width /= coef;
            itemStep = itemWidth / coef;
        }
        protected IEnumerable<PartItem> GetPartItems(StraightTrajectory part, float angle, int itemsCount, float itemWidth, float itemStep, float offset = 0f, bool isBothDir = true)
        {
            var itemDir = part.Direction.TurnDeg(angle, true);

            var start = (part.Length - itemStep * (itemsCount - 1)) / 2;
            for (var i = 0; i < itemsCount; i += 1)
            {
                var itemPos = part.StartPosition + (start + itemStep * i) * part.StartDirection;
                yield return new PartItem(itemPos, itemDir, itemWidth, offset, isBothDir);
            }
        }
        protected IEnumerable<MarkupStylePart> GetDashes(PartItem item, ILineTrajectory[] contour)
        {
            var intersectSet = new HashSet<MarkupIntersect>();
            var straight = new StraightTrajectory(item.Position, item.Position + item.Direction, false);

            GetBorderT(item.BordersBefore, straight, out float beforeMinT, out float beforeMaxT);
            GetBorderT(item.BordersAfter, straight, out float afterMinT, out float afterMaxT);

            foreach (var trajectory in contour)
                intersectSet.AddRange(MarkupIntersect.Calculate(straight, trajectory));

            var intersects = intersectSet.OrderBy(i => i, MarkupIntersect.FirstComparer).ToArray();

            for (var i = 1; i < intersects.Length; i += 2)
            {
                var input = intersects[i - 1].FirstT;
                var output = intersects[i].FirstT;

                if (!item.IsBothDir && input < 0)
                {
                    if (output < 0)
                        continue;
                    else
                        input = 0f;
                }

                if(input < 0 && 0 < output)
                {
                    input = Mathf.Max(input, beforeMinT);
                    output = Mathf.Min(output, beforeMaxT);
                }
                else
                {
                    input = Mathf.Max(input, beforeMinT, afterMinT);
                    output = Mathf.Min(output, beforeMaxT, afterMaxT);
                    if (input > output)
                        continue;
                }

                var start = item.Position + item.Direction * input;
                var end = item.Position + item.Direction * output;

                if (item.Offset != 0)
                {
                    var startOffset = GetOffset(intersects[i - 1], item.Offset);
                    var endOffset = GetOffset(intersects[i], item.Offset);

                    if ((end - start).magnitude - Width < startOffset + endOffset)
                        continue;

                    var isStartToEnd = output >= input;
                    start += item.Direction * (isStartToEnd ? startOffset : -startOffset);
                    end += item.Direction * (isStartToEnd ? -endOffset : endOffset);
                }

                yield return new MarkupStylePart(start, end, item.Direction, item.Width, Color.Value, MaterialType.RectangleFillers);
            }
        }
        private void GetBorderT(List<ILineTrajectory> borders, StraightTrajectory straight, out float minT, out float maxT)
        {
            var intersects = borders.SelectMany(b => MarkupIntersect.Calculate(straight, b)).ToArray();
            var minBorders = intersects.Where(b => b.FirstT < 0).ToArray();
            var maxBorders = intersects.Where(b => b.FirstT > 0).ToArray();
            minT = minBorders.Any() ? minBorders.Max(b => b.FirstT) : float.MinValue;
            maxT = maxBorders.Any() ? maxBorders.Min(b => b.FirstT) : float.MaxValue;
        }

        protected class RailLine : List<ILineTrajectory> { }
        protected class PartItem
        {
            public Vector3 Position { get; }
            public Vector3 Direction { get; }
            public float Width { get; }
            public float Offset { get; }
            public bool IsBothDir { get; }
            public List<ILineTrajectory> BordersBefore { get; } = new List<ILineTrajectory>();
            public List<ILineTrajectory> BordersAfter { get; } = new List<ILineTrajectory>();

            public PartItem(Vector3 position, Vector3 direction, float width, float offset, bool isBothDir)
            {
                Position = position;
                Direction = direction;
                Width = width;
                Offset = offset;
                IsBothDir = isBothDir;
            }
        }
    }
    public abstract class PeriodicFillerStyle : Filler2DStyle, IPeriodicFiller
    {
        public PropertyValue<float> Step { get; }

        public PeriodicFillerStyle(Color32 color, float width, float step, float medianOffset) : base(color, width, medianOffset)
        {
            Step = GetStepProperty(step);
        }

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ILineTrajectory[] contour)
        {
            var rect = GetRect(contour);
            var rail = new RailLine();
            var halfAngelRad = GetAngle() * Mathf.Deg2Rad;
            var middleLine = GetMiddleLine(contour);
            if (GetBeforeMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ILineTrajectory lineBefore))
                rail.Add(lineBefore.Invert());
            rail.Add(middleLine);
            if (GetAfterMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ILineTrajectory lineAfter))
                rail.Add(lineAfter);

            yield return rail;
        }
        protected abstract float GetAngle();
        private ILineTrajectory GetMiddleLine(ILineTrajectory[] contour)
        {
            GetIndexes(contour.Length, out int leftIndex, out int rightIndex);

            var left = contour[leftIndex];
            var right = contour[rightIndex];

            var leftLength = left.Length;
            var rightLength = right.Length;

            var leftRatio = leftLength / (leftLength + rightLength);
            var rightRatio = rightLength / (leftLength + rightLength);

            var middle = new Bezier3()
            {
                a = (right.EndPosition + left.StartPosition) / 2,
                b = (rightRatio * right.EndDirection + leftRatio * left.StartDirection).normalized,
                c = (rightRatio * right.StartDirection + leftRatio * left.EndDirection).normalized,
                d = (right.StartPosition + left.EndPosition) / 2,
            };
            NetSegment.CalculateMiddlePoints(middle.a, middle.b, middle.d, middle.c, true, true, out middle.b, out middle.c);
            return new BezierTrajectory(middle);

        }
        protected abstract void GetIndexes(int count, out int leftIndex, out int rightIndex);
        private bool GetBeforeMiddleLine(ILineTrajectory middleLine, float height, float halfAngelRad, Rect rect, out ILineTrajectory line) => GetAdditionalLine(middleLine.StartPosition, -middleLine.StartDirection, height, halfAngelRad, rect, out line);
        private bool GetAfterMiddleLine(ILineTrajectory middleLine, float height, float halfAngelRad, Rect rect, out ILineTrajectory line) => GetAdditionalLine(middleLine.EndPosition, -middleLine.EndDirection, height, halfAngelRad, rect, out line);
        private bool GetAdditionalLine(Vector3 pos, Vector3 dir, float height, float halfAngelRad, Rect rect, out ILineTrajectory line)
        {
            var dirRight = dir.TurnRad(halfAngelRad, true);
            var dirLeft = dir.TurnRad(halfAngelRad, false);

            var rightRail = GetRail(rect, height, dirRight.AbsoluteAngle() * Mathf.Rad2Deg);
            var leftRail = GetRail(rect, height, dirLeft.AbsoluteAngle() * Mathf.Rad2Deg);

            var t = Mathf.Max(0f, GetT(rightRail.StartPosition, dirRight), GetT(rightRail.EndPosition, dirRight), GetT(leftRail.StartPosition, dirLeft), GetT(leftRail.EndPosition, dirLeft));

            if (t > 0.1)
            {
                line = new StraightTrajectory(pos, pos + dir * t);
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
        protected void GetPartBorders(StraightTrajectory[] parts, float halfAngle, out ILineTrajectory[] startBorders, out ILineTrajectory[] endBorders)
        {
            startBorders = parts.Select(p => new StraightTrajectory(p.StartPosition, p.StartPosition + p.Direction.TurnDeg(halfAngle, true), false)).ToArray();
            endBorders = parts.Select(p => new StraightTrajectory(p.EndPosition, p.EndPosition + p.Direction.TurnDeg(halfAngle, true), false)).ToArray();
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Step.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Step.FromXml(config, DefaultStepGrid);
        }
    }

    public class StripeFillerStyle : PeriodicFillerStyle, IOffsetFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerStripe;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<bool> FollowLines { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyValue<int> LeftRail { get; }
        public PropertyValue<int> RightRail { get; }

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset, bool followLines, int leftRail, int rightRail) : base(color, width, step, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            FollowLines = new PropertyValue<bool>("FL", StyleChanged, followLines);
            Offset = GetOffsetProperty(offset);
            LeftRail = new PropertyValue<int>("LR", StyleChanged, leftRail);
            RightRail = new PropertyValue<int>("RR", StyleChanged, rightRail);
        }
        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset, FollowLines, LeftRail, RightRail);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is StripeFillerStyle stripeTarget)
                stripeTarget.FollowLines.Value = FollowLines;

            if (target is IOffsetFiller offsetTarget)
                offsetTarget.Offset.Value = Offset;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, onHover, onLeave, isTemplate);
            if (!isTemplate)
            {
                components.Add(AddAngleProperty(this, parent, onHover, onLeave));
                components.Add(AddFollowLinesProperty(this, parent));
                components.Add(AddProperty(LeftRail, "Left rail", parent, filler.Contour.VertexCount));
                components.Add(AddProperty(RightRail, "Right rail", parent, filler.Contour.VertexCount));
            }
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
        }
        protected static BoolListPropertyPanel AddFollowLinesProperty(StripeFillerStyle stripeStyle, UIComponent parent)
        {
            var followLinesProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            followLinesProperty.Text = "Follow lines"/*Localize.StyleOption_FollowLines*/;
            followLinesProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            followLinesProperty.SelectedObject = stripeStyle.FollowLines;
            followLinesProperty.OnSelectObjectChanged += (bool value) => stripeStyle.FollowLines.Value = value;
            return followLinesProperty;
        }
        protected static IntListPropertyPanel AddProperty(PropertyValue<int> property, string label, UIComponent parent, int count)
        {
            var firstProperty = ComponentPool.Get<IntListPropertyPanel>(parent);
            firstProperty.Text = label;
            firstProperty.Init(count);
            firstProperty.SelectedObject = property + 1;
            firstProperty.OnSelectObjectChanged += (int value) => property.Value = value - 1;
            return firstProperty;
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ILineTrajectory[] contour)
        {
            if (FollowLines)
            {
                foreach (var rail in base.GetRails(filler, contour))
                    yield return rail;
            }
            else
            {
                var rect = GetRect(contour);
                yield return new RailLine() { GetRail(rect, filler.Markup.Height, Angle) };
            }
        }
        protected override IEnumerable<PartItem> GetItems(RailLine rail, int lod)
        {
            var angle = FollowLines ? 90f - Angle : 90f;
            var width = Width.Value;
            GetItemParams(ref width, angle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(rail, width, width * (Step - 1)).ToArray();
            GetPartBorders(parts, angle, out ILineTrajectory[] startBorders, out ILineTrajectory[] endBorders);

            for (var i = 0; i < parts.Length; i += 1)
            {
                foreach (var item in GetPartItems(parts[i], angle, itemsCount, itemWidth, itemStep, Offset))
                {
                    item.BordersBefore.AddRange(endBorders.Take(i));
                    item.BordersAfter.AddRange(startBorders.Skip(i + 1));
                    yield return item;
                }
            }
        }
        protected override void GetIndexes(int count, out int leftIndex, out int rightIndex)
        {
            leftIndex = LeftRail;
            rightIndex = RightRail;
        }
        protected override float GetAngle() => 90f - Angle;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Angle.ToXml());
            config.Add(FollowLines.ToXml());
            config.Add(Offset.ToXml());
            config.Add(LeftRail.ToXml());
            config.Add(RightRail.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Angle.FromXml(config, DefaultAngle);
            FollowLines.FromXml(config, DefaultFollowLines);
            Offset.FromXml(config, DefaultOffset);
            LeftRail.FromXml(config, 0);
            RightRail.FromXml(config, 1);
        }
    }
    public class GridFillerStyle : Filler2DStyle, IPeriodicFiller, IOffsetFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerGrid;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<float> Step { get; }
        public PropertyValue<float> Offset { get; }

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            Step = GetStepProperty(step);
            Offset = GetOffsetProperty(offset);
        }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;

            if (target is IOffsetFiller offsetTarget)
                offsetTarget.Offset.Value = Offset;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, onHover, onLeave, isTemplate);
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ILineTrajectory[] contour)
        {
            var rect = GetRect(contour);
            yield return new RailLine() { GetRail(rect, filler.Markup.Height, Angle) };
            yield return new RailLine() { GetRail(rect, filler.Markup.Height, Angle < 0 ? Angle + 90 : Angle - 90) };
        }
        protected override IEnumerable<PartItem> GetItems(RailLine rail, int lod)
        {
            var width = Width.Value;
            GetItemParams(ref width, 90f, lod, out int itemsCount, out float itemWidth, out float itemStep);
            foreach (var part in GetParts(rail, width, width * (Step - 1)))
            {
                foreach (var item in GetPartItems(part, 90f, itemsCount, itemWidth, itemStep, Offset))
                    yield return item;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Angle.ToXml());
            config.Add(Step.ToXml());
            config.Add(Offset.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Angle.FromXml(config, DefaultAngle);
            Step.FromXml(config, DefaultStepGrid);
            Offset.FromXml(config, DefaultOffset);
        }
    }
    public class SolidFillerStyle : Filler2DStyle, IColorStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;

        public SolidFillerStyle(Color32 color, float medianOffset) : base(color, DefaultSolidWidth, medianOffset) { }

        public override FillerStyle CopyFillerStyle() => new SolidFillerStyle(Color, DefaultOffset);

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ILineTrajectory[] contour)
        {
            var rect = GetRect(contour);
            yield return new RailLine() { GetRail(rect, filler.Markup.Height, 0) };
        }
        protected override IEnumerable<PartItem> GetItems(RailLine rail, int lod)
        {
            var part = rail.First() as StraightTrajectory;
            var width = part.Length;
            GetItemParams(ref width, 90f, lod, out int itemsCount, out float itemWidth, out float itemStep);
            foreach (var item in GetPartItems(part, 90f, itemsCount, itemWidth, itemStep))
                yield return item;
        }
    }
    public class ChevronFillerStyle : PeriodicFillerStyle, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerChevron;

        public PropertyValue<float> AngleBetween { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<int> Output { get; }
        public PropertyEnumValue<From> StartingFrom { get; }

        public ChevronFillerStyle(Color32 color, float width, float medianOffset, float angleBetween, float step, int output = 0, bool invert = false) : base(color, width, step, medianOffset)
        {
            AngleBetween = GetAngleBetweenProperty(angleBetween);
            Output = GetOutputProperty(output);
            Invert = GetInvertProperty(invert);
            StartingFrom = GetStartingFromProperty(From.Vertex);
        }

        public override FillerStyle CopyFillerStyle() => new ChevronFillerStyle(Color, Width, MedianOffset, AngleBetween, Step);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ChevronFillerStyle chevronTarget)
            {
                chevronTarget.AngleBetween.Value = AngleBetween;
                chevronTarget.Step.Value = Step;
                chevronTarget.Invert.Value = Invert;
                chevronTarget.Output.Value = Output;
            }
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddAngleBetweenProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
            {
                components.Add(AddStartingFromProperty(this, parent));
                components.Add(AddInvertAndTurnProperty(this, parent));
            }
        }
        protected static FloatPropertyPanel AddAngleBetweenProperty(ChevronFillerStyle chevronStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Editor.WheelTip;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.Init();
            angleProperty.Value = chevronStyle.AngleBetween;
            angleProperty.OnValueChanged += (float value) => chevronStyle.AngleBetween.Value = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static ChevronFromPropertyPanel AddStartingFromProperty(ChevronFillerStyle chevronStyle, UIComponent parent)
        {
            var fromProperty = ComponentPool.Get<ChevronFromPropertyPanel>(parent);
            fromProperty.Text = Localize.StyleOption_StartingFrom;
            fromProperty.Init();
            fromProperty.SelectedObject = chevronStyle.StartingFrom;
            fromProperty.OnSelectObjectChanged += (From value) => chevronStyle.StartingFrom.Value = value;
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
                    chevronStyle.Invert.Value = !chevronStyle.Invert;
                else if (index == turnIndex)
                    chevronStyle.Output.Value += 1;
            }

            return buttonsPanel;
        }

        protected override IEnumerable<PartItem> GetItems(RailLine rail, int lod)
        {
            var width = Width.Value;
            var halfAngle = (Invert ? 360 - AngleBetween : AngleBetween) / 2;
            GetItemParams(ref width, halfAngle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(rail, width, width * (Step - 1)).ToArray();
            GetPartBorders(parts, halfAngle, out ILineTrajectory[] leftStartBorders, out ILineTrajectory[] leftEndBorders);
            GetPartBorders(parts, -halfAngle, out ILineTrajectory[] rightStartBorders, out ILineTrajectory[] rightEndBorders);

            for (var i = 0; i < parts.Length; i += 1)
            {
                foreach (var item in GetPartItems(parts[i], halfAngle, itemsCount, itemWidth, itemStep, isBothDir: false))
                {
                    item.BordersBefore.AddRange(Invert ? leftStartBorders.Skip(i + 1) : leftEndBorders.Take(i));
                    item.BordersAfter.AddRange(Invert ? leftEndBorders.Take(i) : leftStartBorders.Skip(i + 1));
                    yield return item;
                }
                foreach (var item in GetPartItems(parts[i], -halfAngle, itemsCount, itemWidth, itemStep, isBothDir: false))
                {
                    item.BordersBefore.AddRange(Invert ? rightStartBorders.Skip(i + 1) : rightEndBorders.Take(i));
                    item.BordersAfter.AddRange(Invert ? rightEndBorders.Take(i) : rightStartBorders.Skip(i + 1));
                    yield return item;
                }
            }
        }
        protected override float GetAngle() => (Invert ? 360 - AngleBetween : AngleBetween) / 2;
        protected override void GetIndexes(int count, out int leftIndex, out int rightIndex)
        {
            leftIndex = Output % count;
            rightIndex = leftIndex.PrevIndex(count, StartingFrom == From.Vertex ? 1 : 2);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(AngleBetween.ToXml());
            config.Add(Output.ToXml());
            config.Add(Invert.ToXml());
            config.Add(StartingFrom.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            AngleBetween.FromXml(config, DefaultAngle);
            Invert.FromXml(config, false);
            Output.FromXml(config, 0);
            StartingFrom.FromXml(config, From.Vertex);
        }

        public enum From
        {
            [Description(nameof(Localize.StyleOption_Vertex))]
            Vertex = 0,

            [Description(nameof(Localize.StyleOption_Edge))]
            Edge = 1
        }
    }
}
