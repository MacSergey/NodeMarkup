using ColossalFramework.Math;
using ColossalFramework.UI;
using ICities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeMarkup.Manager
{
    public interface IPeriodicFiller : IFillerStyle
    {
        PropertyValue<float> Step { get; }
    }
    public interface IOffsetFiller : IFillerStyle
    {
        PropertyValue<float> Offset { get; }
    }
    public interface IRotateFiller : IFillerStyle
    {
        PropertyValue<float> Angle { get; }
    }
    public interface IRailFiller : IFillerStyle
    {
        PropertyValue<int> LeftRailA { get; }
        PropertyValue<int> LeftRailB { get; }
        PropertyValue<int> RightRailA { get; }
        PropertyValue<int> RightRailB { get; }
    }
    public interface IFollowRailFiller : IRailFiller
    {
        PropertyValue<bool> FollowRails { get; }
    }

    public abstract class Filler2DStyle : FillerStyle
    {
        public Filler2DStyle(Color32 color, float width, float lineOffset, float medianOffset) : base(color, width, lineOffset, medianOffset) { }

        protected sealed override IEnumerable<IStyleData> CalculateImpl(MarkupFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod)
        {
            if ((SupportLOD & lod) != 0)
                yield return new MarkupPartGroupData(lod, CalculateProcess(filler, contours, lod));
        }
        protected virtual IEnumerable<MarkupPartData> CalculateProcess(MarkupFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod)
        {
            var originalContour = filler.Contour.TrajectoriesProcessed.ToArray();
            var rails = GetRails(filler, originalContour).ToArray();

            foreach (var rail in rails)
            {
                var partItems = GetItems(rail, lod).ToArray();

                foreach (var partItem in partItems)
                {
                    foreach (var dash in GetDashes(partItem, contours))
                        yield return dash;
                }
            }
        }

        protected abstract IEnumerable<RailLine> GetRails(MarkupFiller filler, ITrajectory[] contour);
        protected Rect GetRect(ITrajectory[] contour)
        {
            var firstPos = contour.FirstOrDefault(t => t != null)?.StartPosition ?? default;
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

        protected abstract IEnumerable<PartItem> GetItems(RailLine rail, MarkupLOD lod);
        protected IEnumerable<StraightTrajectory> GetParts(RailLine rail, float dash, float space)
        {
            foreach (var part in StyleHelper.CalculateDashesBezierT(rail, dash, space, 1))
            {
                var startI = Math.Min((int)part.Start, rail.Count - 1);
                var endI = Math.Min((int)part.End, rail.Count - 1);
                yield return new StraightTrajectory(rail[startI].Position(part.Start - startI), rail[endI].Position(part.End - endI));
            }
        }
        protected void GetItemParams(ref float width, float angle, MarkupLOD lod, out int itemsCount, out float itemWidth, out float itemStep)
        {
            StyleHelper.GetParts(width, 0f, lod, out itemsCount, out itemWidth);

            var coef = Math.Max(Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad), 0.01f);
            width /= coef;
            itemStep = itemWidth / coef;
        }
        protected IEnumerable<PartItem> GetPartItems(StraightTrajectory part, float angle, int itemsCount, float itemWidth, float itemStep, bool isBothDir = true)
        {
            var itemDir = part.Direction.MakeFlat().TurnDeg(angle, true);

            var start = (part.Length - itemStep * (itemsCount - 1)) / 2;
            for (var i = 0; i < itemsCount; i += 1)
            {
                var itemPos = part.StartPosition + (start + itemStep * i) * part.StartDirection;
                yield return new PartItem(itemPos, itemDir, itemWidth, isBothDir);
            }
        }
        protected virtual IEnumerable<MarkupPartData> GetDashes(PartItem item, List<List<FillerContour.Part>> contours) => GetDashesWithoutOrder(item, contours); 
        protected Intersection[] GetDashesIntersects(StraightTrajectory itemStraight, List<List<FillerContour.Part>> contours)
        {
            var intersectSet = new HashSet<Intersection>();
            foreach (var contour in contours)
            {
                foreach (var contourPart in contour)
                    intersectSet.AddRange(Intersection.Calculate(itemStraight, contourPart.Trajectory));
            }

            var intersects = intersectSet.OrderBy(i => i, Intersection.FirstComparer).ToArray();
            return intersects;
        }
        protected IEnumerable<MarkupPartData> GetDashesWithoutOrder(PartItem item, List<List<FillerContour.Part>> contours)
        {
            var straight = new StraightTrajectory(item.Position, item.Position + item.Direction, false);
            var intersects = GetDashesIntersects(straight, contours);

            for (var i = 1; i < intersects.Length; i += 2)
            {
                var start = intersects[i - 1];
                var end = intersects[i];
                var startPos = start.Second.Position(start.SecondT);
                var endPos = end.Second.Position(end.SecondT);
                yield return new MarkupPartData(startPos, endPos, item.Direction, item.Width, Color.Value, MaterialType.RectangleFillers);
            }
        }
        protected IEnumerable<MarkupPartData> GetDashesWithOrder(PartItem item, List<List<FillerContour.Part>> contours)
        {
            var straight = new StraightTrajectory(item.Position, item.Position + item.Direction, false);
            var intersects = GetDashesIntersects(straight, contours);

            var beforeIntersect = Intersection.CalculateSingle(straight, item.Before);
            var afterIntersect = Intersection.CalculateSingle(straight, item.After);

            var beforeT = beforeIntersect.IsIntersect ? beforeIntersect.FirstT : float.MaxValue;
            var afterT = afterIntersect.IsIntersect ? afterIntersect.FirstT : float.MinValue;

            var beforeIsPriority = beforeIntersect.IsIntersect && Mathf.Abs(beforeIntersect.SecondT) < Mathf.Abs(beforeIntersect.FirstT);
            var afterIsPriority = afterIntersect.IsIntersect && Mathf.Abs(afterIntersect.SecondT) < Mathf.Abs(afterIntersect.FirstT);

            for (var i = 1; i < intersects.Length; i += 2)
            {
                if (GetDashesT(item, intersects, i, beforeT, beforeIsPriority, afterT, afterIsPriority, out float input, out float output))
                {
                    var start = item.Position + item.Direction * input;
                    var end = item.Position + item.Direction * output;
                    yield return new MarkupPartData(start, end, item.Direction, item.Width, Color.Value, MaterialType.RectangleFillers);
                }
            }
        }
        private bool GetDashesT(PartItem item, Intersection[] intersects, int i, float beforeT, bool beforeIsPriority, float afterT, bool afterIsPriority, out float input, out float output)
        {
            input = intersects[i - 1].FirstT;
            output = intersects[i].FirstT;

            if (!item.IsBothDir && input < 0)
            {
                if (output < 0)
                    return false;
                else
                    input = 0f;
            }

            var isMain = input <= 0f && output >= 0f;

            if (isMain)
            {
                Cut(beforeT, beforeIsPriority, ref input, ref output);
                Cut(afterT, afterIsPriority, ref input, ref output);
            }
            else if (Skip(beforeT, beforeIsPriority, input, output) || Skip(afterT, afterIsPriority, input, output))
                return false;

            return true;

            static void Cut(float t, bool isPriority, ref float input, ref float output)
            {
                if (t < 0f)
                {
                    if (input < t && isPriority)
                        input = t;
                }
                else
                {
                    if (output > t && isPriority)
                        output = t;
                }
            }
            static bool Skip(float t, bool isPriority, float input, float output) => isPriority && ((t < 0f && input < t) || (t > 0f && output > t));
        }

        protected class RailLine : List<ITrajectory> { }
        protected class PartItem
        {
            public Vector3 Position { get; }
            public Vector3 Direction { get; }
            public float Width { get; }
            public bool IsBothDir { get; }
            public StraightTrajectory Before { get; set; }
            public StraightTrajectory After { get; set; }

            public PartItem(Vector3 position, Vector3 direction, float width, bool isBothDir)
            {
                Position = position;
                Direction = direction;
                Width = width;
                IsBothDir = isBothDir;
            }
        }
    }
    public abstract class PeriodicFillerStyle : Filler2DStyle, IPeriodicFiller
    {
        public PropertyValue<float> Step { get; }

        public PeriodicFillerStyle(Color32 color, float width, float step, float lineOffset, float medianOffset) : base(color, width, lineOffset, medianOffset)
        {
            Step = GetStepProperty(step);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddStepProperty(this, parent));
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);
            var rail = new RailLine();
            var halfAngelRad = GetAngle() * Mathf.Deg2Rad;
            if (GetMiddleLine(filler.Contour) is ITrajectory middleLine)
            {
                if (GetBeforeMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ITrajectory lineBefore))
                    rail.Add(lineBefore.Invert());
                rail.Add(middleLine);
                if (GetAfterMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ITrajectory lineAfter))
                    rail.Add(lineAfter);
            }

            yield return rail;
        }
        protected abstract float GetAngle();
        private ITrajectory GetMiddleLine(FillerContour contour)
        {
            GetRails(contour, out ITrajectory left, out ITrajectory right);
            if (left == null || right == null)
                return null;

            var leftLength = left.Length;
            var rightLength = right.Length;

            var leftRatio = leftLength / (leftLength + rightLength);
            var rightRatio = rightLength / (leftLength + rightLength);

            var straight = new StraightTrajectory((right.EndPosition + left.StartPosition) / 2, (right.StartPosition + left.EndPosition) / 2);
            var middle = new Bezier3()
            {
                a = (right.EndPosition + left.StartPosition) / 2,
                b = GetDirection(rightRatio * right.EndDirection, leftRatio * left.StartDirection, straight.StartDirection),
                c = GetDirection(rightRatio * right.StartDirection, leftRatio * left.EndDirection, straight.EndDirection),
                d = (right.StartPosition + left.EndPosition) / 2,
            };
            NetSegment.CalculateMiddlePoints(middle.a, middle.b, middle.d, middle.c, true, true, out middle.b, out middle.c);
            return new BezierTrajectory(middle);

            static Vector3 GetDirection(Vector3 left, Vector3 right, Vector3 straight)
            {
                var dir = (left + right).normalized;
                if (Vector2.Angle(XZ(left), XZ(right)) > 150f || Vector2.Angle(XZ(dir), XZ(straight)) > 90f)
                    dir = straight;

                return dir;
            }

        }
        protected abstract void GetRails(FillerContour contour, out ITrajectory left, out ITrajectory right);
        private bool GetBeforeMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect rect, out ITrajectory line) => GetAdditionalLine(middleLine.StartPosition, -middleLine.StartDirection, height, halfAngelRad, rect, out line);
        private bool GetAfterMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect rect, out ITrajectory line) => GetAdditionalLine(middleLine.EndPosition, -middleLine.EndDirection, height, halfAngelRad, rect, out line);
        private bool GetAdditionalLine(Vector3 pos, Vector3 dir, float height, float halfAngelRad, Rect rect, out ITrajectory line)
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
                Line2.Intersect(XZ(pos), XZ(pos + dir), XZ(railPos), XZ(railPos + railDir), out float p, out _);
                return p;
            }
        }
        protected void GetPartBorders(StraightTrajectory[] parts, float halfAngle, out StraightTrajectory[] startBorders, out StraightTrajectory[] endBorders)
        {
            startBorders = parts.Select(p => new StraightTrajectory(p.StartPosition, p.StartPosition + p.Direction.TurnDeg(halfAngle, true), false)).ToArray();
            endBorders = parts.Select(p => new StraightTrajectory(p.EndPosition, p.EndPosition + p.Direction.TurnDeg(halfAngle, true), false)).ToArray();
        }
        protected StraightTrajectory GetPartBorder(StraightTrajectory[] borders, StraightTrajectory part, int index, bool isIncrement)
        {
            var step = isIncrement ? 1 : -1;

            var border = default(StraightTrajectory);
            var t = float.MaxValue;

            for (var i = index + step; isIncrement ? i < borders.Length : i >= 0; i += step)
            {
                var intersection = Intersection.CalculateSingle(part, borders[i]);
                if (intersection.IsIntersect && Math.Abs(intersection.FirstT) < 1000f && Math.Abs(intersection.SecondT) < 1000f)
                {
                    if (Mathf.Abs(intersection.FirstT) < Mathf.Abs(t))
                    {
                        border = borders[i];
                        t = intersection.FirstT;
                    }
                    else
                        break;
                }
            }
            return border;
        }

        public override void Render(MarkupFiller filler, OverlayData data)
        {
            GetRails(filler.Contour, out ITrajectory left, out ITrajectory right);

            data.Color = Colors.Green;
            left?.Render(data);

            data.Color = Colors.Red;
            right?.Render(data);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Step.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Step.FromXml(config, DefaultStepGrid);
        }
    }
    public abstract class RailFillerStyle : PeriodicFillerStyle, IRailFiller
    {
        public PropertyValue<int> LeftRailA { get; }
        public PropertyValue<int> RightRailA { get; }
        public PropertyValue<int> LeftRailB { get; }
        public PropertyValue<int> RightRailB { get; }

        public RailFillerStyle(Color32 color, float width, float step, float lineOffset, float medianOffset) : base(color, width, step, lineOffset, medianOffset)
        {
            LeftRailA = GetLeftRailAProperty(0);
            LeftRailB = GetLeftRailBProperty(1);
            RightRailA = GetRightRailAProperty(1);
            RightRailB = GetRightRailBProperty(2);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRailFiller railTarget)
            {
                railTarget.LeftRailA.Value = LeftRailA;
                railTarget.LeftRailB.Value = LeftRailB;
                railTarget.RightRailA.Value = RightRailA;
                railTarget.RightRailB.Value = RightRailB;
            }
        }

        protected override IEnumerable<MarkupPartData> GetDashes(PartItem item, List<List<FillerContour.Part>> contours) => GetDashesWithOrder(item, contours);
        protected override void GetRails(FillerContour contour, out ITrajectory left, out ITrajectory right)
        {
            left = contour.GetRail(LeftRailA, LeftRailB, RightRailA, RightRailB);
            right = contour.GetRail(RightRailA, RightRailB, LeftRailA, LeftRailB);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LeftRailA.ToXml(config);
            LeftRailB.ToXml(config);
            RightRailA.ToXml(config);
            RightRailB.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            LeftRailA.FromXml(config);
            LeftRailB.FromXml(config);
            RightRailA.FromXml(config);
            RightRailB.FromXml(config);
        }
    }

    public class StripeFillerStyle : RailFillerStyle, IFollowRailFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerStripe;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<bool> FollowRails { get; }

        public StripeFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angle, float step, bool followRails = false) : base(color, width, step, lineOffset, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            FollowRails = GetFollowRailsProperty(followRails);
        }
        public override FillerStyle CopyStyle() => new StripeFillerStyle(Color, Width, LineOffset, DefaultOffset, DefaultAngle, Step, FollowRails);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IFollowRailFiller followRailTarget)
                followRailTarget.FollowRails.Value = FollowRails;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent));

            if (!isTemplate)
            {
                components.Add(AddFollowRailsProperty(this, parent));
                AddRailProperty(this, filler.Contour, parent, out var leftRail, out var rightRail);
                components.Add(leftRail);
                components.Add(rightRail);
                components.Add(AddTurnProperty(filler, parent));

                FollowRailChanged(this, parent, FollowRails);
            }
        }
        private ButtonPanel AddTurnProperty(MarkupFiller filler, UIComponent parent)
        {
            var turnButton = ComponentPool.Get<ButtonPanel>(parent, "Turn");
            turnButton.Text = Localize.StyleOption_Turn;
            turnButton.Init();

            turnButton.OnButtonClick += () =>
            {
                var vertexCount = filler.Contour.ProcessedCount;

                if (parent.Find<FillerRailSelectPropertyPanel>("LeftRail") is FillerRailSelectPropertyPanel leftRailProperty)
                    leftRailProperty.Value = (leftRailProperty.Value + 1) % vertexCount;
                if (parent.Find<FillerRailSelectPropertyPanel>("RightRail") is FillerRailSelectPropertyPanel rightRailProperty)
                    rightRailProperty.Value = (rightRailProperty.Value + 1) % vertexCount;
            };

            return turnButton;
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ITrajectory[] contour)
        {
            if (FollowRails)
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
        protected override IEnumerable<PartItem> GetItems(RailLine rail, MarkupLOD lod)
        {
            var angle = FollowRails ? 90f - Angle : 90f;
            var width = Width.Value;
            GetItemParams(ref width, angle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(rail, width, width * (Step - 1)).ToArray();
            GetPartBorders(parts, angle, out StraightTrajectory[] startBorders, out StraightTrajectory[] endBorders);

            for (var i = 0; i < parts.Length; i += 1)
            {
                var before = GetPartBorder(endBorders, startBorders[i], i, false);
                var after = GetPartBorder(startBorders, endBorders[i], i, true);
                foreach (var item in GetPartItems(parts[i], angle, itemsCount, itemWidth, itemStep))
                {
                    item.Before = before;
                    item.After = after;
                    yield return item;
                }
            }
        }

        protected override float GetAngle() => 90f - Angle;

        public override void Render(MarkupFiller filler, OverlayData data)
        {
            if (FollowRails)
                base.Render(filler, data);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            FollowRails.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Angle.FromXml(config, DefaultAngle);
            FollowRails.FromXml(config, DefaultFollowRails);
        }
    }
    public class ChevronFillerStyle : RailFillerStyle, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerChevron;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> AngleBetween { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<int> Output { get; }
        public PropertyEnumValue<From> StartingFrom { get; }

        public ChevronFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angleBetween, float step) : base(color, width, step, lineOffset, medianOffset)
        {
            AngleBetween = GetAngleBetweenProperty(angleBetween);
            Invert = GetInvertProperty(false);

            Output = GetOutputProperty(0);
            StartingFrom = GetStartingFromProperty(From.Vertex);
        }

        public override FillerStyle CopyStyle() => new ChevronFillerStyle(Color, Width, LineOffset, DefaultOffset, AngleBetween, Step);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is ChevronFillerStyle chevronTarget)
            {
                chevronTarget.AngleBetween.Value = AngleBetween;
                chevronTarget.Step.Value = Step;
                chevronTarget.Invert.Value = Invert;
            }
            if (target is IFollowRailFiller followRailTarget)
                followRailTarget.FollowRails.Value = true;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddAngleBetweenProperty(parent));
            if (!isTemplate)
            {
                AddRailProperty(this, filler.Contour, parent, out var leftRail, out var rightRail);
                components.Add(leftRail);
                components.Add(rightRail);
                components.Add(AddInvertAndTurnProperty(filler, parent));
            }
        }
        protected FloatPropertyPanel AddAngleBetweenProperty(UIComponent parent)
        {
            var angleProperty = ComponentPool.GetBefore<FloatPropertyPanel>(parent, nameof(LineOffset), nameof(AngleBetween));
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.Init();
            angleProperty.Value = AngleBetween;
            angleProperty.OnValueChanged += (float value) => AngleBetween.Value = value;

            return angleProperty;
        }
        protected ButtonsPanel AddInvertAndTurnProperty(MarkupFiller filler, UIComponent parent)
        {
            var turnAndInvert = ComponentPool.Get<ButtonsPanel>(parent, "TurnAndInvert");
            var invertIndex = turnAndInvert.AddButton(Localize.StyleOption_Invert);
            var turnIndex = turnAndInvert.AddButton(Localize.StyleOption_Turn);
            turnAndInvert.Init();

            turnAndInvert.OnButtonClick += (int buttonIndex) =>
            {
                if (buttonIndex == invertIndex)
                {
                    Invert.Value = !Invert;
                }
                else if (buttonIndex == turnIndex)
                {
                    var vertexCount = filler.Contour.ProcessedCount;

                    if (parent.Find<FillerRailSelectPropertyPanel>(LeftRail) is FillerRailSelectPropertyPanel leftRailProperty)
                        leftRailProperty.Value = (leftRailProperty.Value + 1) % vertexCount;
                    if (parent.Find<FillerRailSelectPropertyPanel>(RightRail) is FillerRailSelectPropertyPanel rightRailProperty)
                        rightRailProperty.Value = (rightRailProperty.Value + 1) % vertexCount;
                }
            };

            return turnAndInvert;
        }

        protected override IEnumerable<PartItem> GetItems(RailLine rail, MarkupLOD lod)
        {
            var width = Width.Value;
            var halfAngle = (Invert ? 360 - AngleBetween : AngleBetween) / 2;
            GetItemParams(ref width, halfAngle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(rail, width, width * (Step - 1)).ToArray();
            GetPartBorders(parts, halfAngle, out StraightTrajectory[] leftStartBorders, out StraightTrajectory[] leftEndBorders);
            GetPartBorders(parts, -halfAngle, out StraightTrajectory[] rightStartBorders, out StraightTrajectory[] rightEndBorders);

            for (var i = 0; i < parts.Length; i += 1)
            {
                var leftBefore = GetPartBorder(leftEndBorders, leftStartBorders[i], i, false);
                var leftAfter = GetPartBorder(leftStartBorders, leftEndBorders[i], i, true);
                var rightBefore = GetPartBorder(rightEndBorders, rightStartBorders[i], i, false);
                var rightAfter = GetPartBorder(rightStartBorders, rightEndBorders[i], i, true);
                foreach (var item in GetPartItems(parts[i], halfAngle, itemsCount, itemWidth, itemStep, isBothDir: false))
                {
                    item.Before = leftBefore;
                    item.After = leftAfter;
                    yield return item;
                }
                foreach (var item in GetPartItems(parts[i], -halfAngle, itemsCount, itemWidth, itemStep, isBothDir: false))
                {
                    item.Before = rightBefore;
                    item.After = rightAfter;
                    yield return item;
                }
            }
        }
        protected override float GetAngle() => (Invert ? 360 - AngleBetween : AngleBetween) / 2;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            AngleBetween.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            Output.FromXml(config, 0);
            StartingFrom.FromXml(config, From.Vertex);

            LeftRailA.Value = Output;
            LeftRailB.Value = Output + 1;

            if (StartingFrom == From.Vertex)
            {
                RightRailA.Value = Output;
                RightRailB.Value = Output - 1;
            }
            else if (StartingFrom == From.Edge)
            {
                RightRailA.Value = Output - 1;
                RightRailB.Value = Output - 2;
            }

            base.FromXml(config, map, invert);
            AngleBetween.FromXml(config, DefaultAngle);
            Invert.FromXml(config, false);
        }

        public enum From
        {
            [Description(nameof(Localize.StyleOption_Vertex))]
            Vertex = 0,

            [Description(nameof(Localize.StyleOption_Edge))]
            Edge = 1
        }
    }
    public class GridFillerStyle : Filler2DStyle, IPeriodicFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerGrid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<float> Step { get; }

        public GridFillerStyle(Color32 color, float width, float angle, float step, float lineOffset, float medianOffset) : base(color, width, lineOffset, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            Step = GetStepProperty(step);
        }

        public override FillerStyle CopyStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, LineOffset, DefaultOffset);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddStepProperty(this, parent));
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent));
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);
            yield return new RailLine() { GetRail(rect, filler.Markup.Height, Angle) };
            yield return new RailLine() { GetRail(rect, filler.Markup.Height, Angle < 0 ? Angle + 90 : Angle - 90) };
        }
        protected override IEnumerable<PartItem> GetItems(RailLine rail, MarkupLOD lod)
        {
            var width = Width.Value;
            GetItemParams(ref width, 90f, lod, out int itemsCount, out float itemWidth, out float itemStep);
            foreach (var part in GetParts(rail, width, width * (Step - 1)))
            {
                foreach (var item in GetPartItems(part, 90f, itemsCount, itemWidth, itemStep))
                    yield return item;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            Step.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Angle.FromXml(config, DefaultAngle);
            Step.FromXml(config, DefaultStepGrid);
        }
    }
    public class SolidFillerStyle : Filler2DStyle, IRailFiller, IFollowRailFiller, IColorStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<int> LeftRailA { get; }
        public PropertyValue<int> RightRailA { get; }
        public PropertyValue<int> LeftRailB { get; }
        public PropertyValue<int> RightRailB { get; }
        public PropertyValue<bool> FollowRails { get; }

        public SolidFillerStyle(Color32 color, float lineOffset, float medianOffset, bool followRails = false) : base(color, DefaultSolidWidth, lineOffset, medianOffset)
        {
            LeftRailA = GetLeftRailAProperty(0);
            LeftRailB = GetLeftRailBProperty(1);
            RightRailA = GetRightRailAProperty(1);
            RightRailB = GetRightRailBProperty(2);
            FollowRails = GetFollowRailsProperty(followRails);
        }

        public override FillerStyle CopyStyle() => new SolidFillerStyle(Color, LineOffset, DefaultOffset);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRailFiller railTarget)
            {
                railTarget.LeftRailA.Value = LeftRailA;
                railTarget.LeftRailB.Value = LeftRailB;
                railTarget.RightRailA.Value = RightRailA;
                railTarget.RightRailB.Value = RightRailB;
            }
            if (target is IFollowRailFiller followRailTarget)
                followRailTarget.FollowRails.Value = FollowRails;
        }

        protected override IEnumerable<RailLine> GetRails(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);

            if (FollowRails)
            {
                var left = filler.Contour.GetRail(LeftRailA, LeftRailB, RightRailA, RightRailB);
                var right = filler.Contour.GetRail(RightRailA, RightRailB, LeftRailA, LeftRailB);
                var startPos = (right.EndPosition + left.StartPosition) / 2;
                var endPos = (right.StartPosition + left.EndPosition) / 2;
                var angle = (endPos - startPos).Turn90(true).AbsoluteAngle() * Mathf.Rad2Deg;
                yield return new RailLine() { GetRail(rect, filler.Markup.Height, angle) };
            }
            else
                yield return new RailLine() { GetRail(rect, filler.Markup.Height, 0) };
        }
        protected override IEnumerable<PartItem> GetItems(RailLine rail, MarkupLOD lod)
        {
            foreach(var part in rail.OfType<StraightTrajectory>())
            {
                var width = part.Length;
                GetItemParams(ref width, 90f, lod, out int itemsCount, out float itemWidth, out float itemStep);
                foreach (var item in GetPartItems(part, 90f, itemsCount, itemWidth, itemStep))
                    yield return item;
            }
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
            {
                components.Add(AddFollowRailsProperty(this, parent));
                AddRailProperty(this, filler.Contour, parent, out var leftRail, out var rightRail);

                components.Add(leftRail);
                components.Add(rightRail);
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LeftRailA.ToXml(config);
            LeftRailB.ToXml(config);
            RightRailA.ToXml(config);
            RightRailB.ToXml(config);
            FollowRails.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            LeftRailA.FromXml(config);
            LeftRailB.FromXml(config);
            RightRailA.FromXml(config);
            RightRailB.FromXml(config);
            FollowRails.FromXml(config, DefaultFollowRails);
        }
    }
}
