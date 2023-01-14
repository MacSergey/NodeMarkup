using ColossalFramework.Math;
using ColossalFramework.UI;
using ICities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.API;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using NodeMarkup.Utilities.API;
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
    public interface IGuideFiller : IFillerStyle
    {
        PropertyValue<int> LeftGuideA { get; }
        PropertyValue<int> LeftGuideB { get; }
        PropertyValue<int> RightGuideA { get; }
        PropertyValue<int> RightGuideB { get; }
    }
    public interface IFollowGuideFiller : IGuideFiller
    {
        PropertyValue<bool> FollowGuides { get; }
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
            var guides = GetGuides(filler, originalContour).ToArray();

            foreach (var guide in guides)
            {
                var partItems = GetItems(guide, lod).ToArray();

                foreach (var partItem in partItems)
                {
                    foreach (var dash in GetDashes(partItem, contours))
                        yield return dash;
                }
            }
        }

        protected abstract IEnumerable<GuideLine> GetGuides(MarkupFiller filler, ITrajectory[] contour);
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
        protected StraightTrajectory GetGuide(Rect rect, float height, float angle)
        {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;

            var absAngle = Mathf.Abs(angle) * Mathf.Deg2Rad;
            var guideLength = rect.width * Mathf.Sin(absAngle) + rect.height * Mathf.Cos(absAngle);
            var dx = guideLength * Mathf.Sin(absAngle);
            var dy = guideLength * Mathf.Cos(absAngle);

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

        protected abstract IEnumerable<PartItem> GetItems(GuideLine guide, MarkupLOD lod);
        protected IEnumerable<StraightTrajectory> GetParts(GuideLine guide, float dash, float space)
        {
            foreach (var part in StyleHelper.CalculateDashesBezierT(guide, dash, space, 1))
            {
                var startI = Math.Min((int)part.Start, guide.Count - 1);
                var endI = Math.Min((int)part.End, guide.Count - 1);
                yield return new StraightTrajectory(guide[startI].Position(part.Start - startI), guide[endI].Position(part.End - endI));
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
                yield return new MarkupPartData(startPos, endPos, item.Direction, item.Width, Color.Value, RenderHelper.MaterialLib[MaterialType.RectangleFillers]);
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
                    yield return new MarkupPartData(start, end, item.Direction, item.Width, Color.Value, RenderHelper.MaterialLib[MaterialType.RectangleFillers]);
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

        protected class GuideLine : List<ITrajectory> { }
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
            components.Add(AddStepProperty(this, parent, false));
        }

        protected override IEnumerable<GuideLine> GetGuides(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);
            var guide = new GuideLine();
            var halfAngelRad = GetAngle() * Mathf.Deg2Rad;
            if (GetMiddleLine(filler.Contour) is ITrajectory middleLine)
            {
                if (GetBeforeMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ITrajectory lineBefore))
                    guide.Add(lineBefore.Invert());
                guide.Add(middleLine);
                if (GetAfterMiddleLine(middleLine, filler.Markup.Height, halfAngelRad, rect, out ITrajectory lineAfter))
                    guide.Add(lineAfter);
            }

            yield return guide;
        }
        protected abstract float GetAngle();
        private ITrajectory GetMiddleLine(FillerContour contour)
        {
            GetGuides(contour, out ITrajectory left, out ITrajectory right);
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
        protected abstract void GetGuides(FillerContour contour, out ITrajectory left, out ITrajectory right);
        private bool GetBeforeMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect rect, out ITrajectory line) => GetAdditionalLine(middleLine.StartPosition, -middleLine.StartDirection, height, halfAngelRad, rect, out line);
        private bool GetAfterMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect rect, out ITrajectory line) => GetAdditionalLine(middleLine.EndPosition, -middleLine.EndDirection, height, halfAngelRad, rect, out line);
        private bool GetAdditionalLine(Vector3 pos, Vector3 dir, float height, float halfAngelRad, Rect rect, out ITrajectory line)
        {
            var dirRight = dir.TurnRad(halfAngelRad, true);
            var dirLeft = dir.TurnRad(halfAngelRad, false);

            var rightGuide = GetGuide(rect, height, dirRight.AbsoluteAngle() * Mathf.Rad2Deg);
            var leftGuide = GetGuide(rect, height, dirLeft.AbsoluteAngle() * Mathf.Rad2Deg);

            var t = Mathf.Max(0f, GetT(rightGuide.StartPosition, dirRight), GetT(rightGuide.EndPosition, dirRight), GetT(leftGuide.StartPosition, dirLeft), GetT(leftGuide.EndPosition, dirLeft));

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

            float GetT(Vector3 guidePos, Vector3 guideDir)
            {
                Line2.Intersect(XZ(pos), XZ(pos + dir), XZ(guidePos), XZ(guidePos + guideDir), out float p, out _);
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
            GetGuides(filler.Contour, out ITrajectory left, out ITrajectory right);

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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Step.FromXml(config, DefaultStepGrid);
        }
    }
    public abstract class GuideFillerStyle : PeriodicFillerStyle, IGuideFiller
    {
        public PropertyValue<int> LeftGuideA { get; }
        public PropertyValue<int> RightGuideA { get; }
        public PropertyValue<int> LeftGuideB { get; }
        public PropertyValue<int> RightGuideB { get; }

        public GuideFillerStyle(Color32 color, float width, float step, float lineOffset, float medianOffset) : base(color, width, step, lineOffset, medianOffset)
        {
            LeftGuideA = GetLeftGuideAProperty(0);
            LeftGuideB = GetLeftGuideBProperty(1);
            RightGuideA = GetRightGuideAProperty(1);
            RightGuideB = GetRightGuideBProperty(2);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IGuideFiller guideTarget)
            {
                guideTarget.LeftGuideA.Value = LeftGuideA;
                guideTarget.LeftGuideB.Value = LeftGuideB;
                guideTarget.RightGuideA.Value = RightGuideA;
                guideTarget.RightGuideB.Value = RightGuideB;
            }
        }

        protected override IEnumerable<MarkupPartData> GetDashes(PartItem item, List<List<FillerContour.Part>> contours) => GetDashesWithOrder(item, contours);
        protected override void GetGuides(FillerContour contour, out ITrajectory left, out ITrajectory right)
        {
            left = contour.GetGuide(LeftGuideA, LeftGuideB, RightGuideA, RightGuideB);
            right = contour.GetGuide(RightGuideA, RightGuideB, LeftGuideA, LeftGuideB);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LeftGuideA.ToXml(config);
            LeftGuideB.ToXml(config);
            RightGuideA.ToXml(config);
            RightGuideB.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LeftGuideA.FromXml(config);
            LeftGuideB.FromXml(config);
            RightGuideA.FromXml(config);
            RightGuideB.FromXml(config);
        }
    }

    public class StripeFillerStyle : GuideFillerStyle, IFollowGuideFiller, IRotateFiller, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerStripe;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        protected static string Turn => string.Empty;

        public PropertyValue<float> Angle { get; }
        public PropertyValue<bool> FollowGuides { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Offset);
                yield return nameof(Guide);
                yield return nameof(Turn);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
            }
        }

        public StripeFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angle, float step, bool followGuides = false) : base(color, width, step, lineOffset, medianOffset)
        {
            Angle = GetAngleProperty(angle);
            FollowGuides = GetFollowGuidesProperty(followGuides);
        }
        public override FillerStyle CopyStyle() => new StripeFillerStyle(Color, Width, LineOffset, DefaultOffset, DefaultAngle, Step, FollowGuides);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = FollowGuides;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, false));

            if (!isTemplate)
            {
                components.Add(AddGuideProperty(this, filler.Contour, parent, true));
                components.Add(AddTurnProperty(filler, parent, false));
            }
        }

        private ButtonPanel AddTurnProperty(MarkupFiller filler, UIComponent parent, bool canCollapse)
        {
            var turnButton = ComponentPool.Get<ButtonPanel>(parent, nameof(Turn));
            turnButton.Text = Localize.StyleOption_Turn;
            turnButton.CanCollapse = canCollapse;
            turnButton.Init();

            turnButton.OnButtonClick += () =>
            {
                var vertexCount = filler.Contour.ProcessedCount;

                if (parent.Find<FillerGuidePropertyPanel>(Guide) is FillerGuidePropertyPanel guideProperty)
                {
                    guideProperty.LeftGuide = (guideProperty.LeftGuide + 1) % vertexCount;
                    guideProperty.RightGuide = (guideProperty.RightGuide + 1) % vertexCount;
                }
            };

            return turnButton;
        }

        protected override IEnumerable<GuideLine> GetGuides(MarkupFiller filler, ITrajectory[] contour)
        {
            if (FollowGuides)
            {
                foreach (var guide in base.GetGuides(filler, contour))
                    yield return guide;
            }
            else
            {
                var rect = GetRect(contour);
                yield return new GuideLine() { GetGuide(rect, filler.Markup.Height, Angle) };
            }
        }
        protected override IEnumerable<PartItem> GetItems(GuideLine guide, MarkupLOD lod)
        {
            var angle = FollowGuides ? 90f - Angle : 90f;
            var width = Width.Value;
            GetItemParams(ref width, angle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(guide, width, width * (Step - 1)).ToArray();
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
            if (FollowGuides)
                base.Render(filler, data);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            FollowGuides.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Angle.FromXml(config, DefaultAngle);
            FollowGuides.FromXml(config, DefaultFollowGuides);
        }
    }
    public class ChevronFillerStyle : GuideFillerStyle, IWidthStyle, IColorStyle
    {
        public override StyleType Type => StyleType.FillerChevron;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> AngleBetween { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<int> Output { get; }
        public PropertyEnumValue<From> StartingFrom { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(AngleBetween);
                yield return nameof(Offset);
                yield return nameof(Guide);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(AngleBetween), AngleBetween);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

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
            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = true;
        }
        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddAngleBetweenProperty(parent, false));
            if (!isTemplate)
            {
                components.Add(AddGuideProperty(this, filler.Contour, parent, true));
                components.Add(AddInvertAndTurnProperty(filler, parent, false));
            }
        }

        protected FloatPropertyPanel AddAngleBetweenProperty(UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(AngleBetween));
            angleProperty.Text = Localize.StyleOption_AngleBetween;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = 30;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 150;
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = AngleBetween;
            angleProperty.OnValueChanged += (float value) => AngleBetween.Value = value;

            return angleProperty;
        }
        protected ButtonsPanel AddInvertAndTurnProperty(MarkupFiller filler, UIComponent parent, bool canCollapse)
        {
            var turnAndInvert = ComponentPool.Get<ButtonsPanel>(parent, "TurnAndInvert");
            var invertIndex = turnAndInvert.AddButton(Localize.StyleOption_Invert);
            var turnIndex = turnAndInvert.AddButton(Localize.StyleOption_Turn);
            turnAndInvert.CanCollapse = canCollapse;
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
                    if (parent.Find<FillerGuidePropertyPanel>(Guide) is FillerGuidePropertyPanel guideProperty)
                    {
                        guideProperty.LeftGuide = (guideProperty.LeftGuide + 1) % vertexCount;
                        guideProperty.RightGuide = (guideProperty.RightGuide + 1) % vertexCount;
                    }
                }
            };

            return turnAndInvert;
        }

        protected override IEnumerable<PartItem> GetItems(GuideLine guide, MarkupLOD lod)
        {
            var width = Width.Value;
            var halfAngle = (Invert ? 360 - AngleBetween : AngleBetween) / 2;
            GetItemParams(ref width, halfAngle, lod, out int itemsCount, out float itemWidth, out float itemStep);

            var parts = GetParts(guide, width, width * (Step - 1)).ToArray();
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Output.FromXml(config, 0);
            StartingFrom.FromXml(config, From.Vertex);

            LeftGuideA.Value = Output;
            LeftGuideB.Value = Output + 1;

            if (StartingFrom == From.Vertex)
            {
                RightGuideA.Value = Output;
                RightGuideB.Value = Output - 1;
            }
            else if (StartingFrom == From.Edge)
            {
                RightGuideA.Value = Output - 1;
                RightGuideB.Value = Output - 2;
            }

            base.FromXml(config, map, invert, typeChanged);
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

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

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
            components.Add(AddStepProperty(this, parent, false));
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, false));
        }

        protected override IEnumerable<GuideLine> GetGuides(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);
            yield return new GuideLine() { GetGuide(rect, filler.Markup.Height, Angle) };
            yield return new GuideLine() { GetGuide(rect, filler.Markup.Height, Angle < 0 ? Angle + 90 : Angle - 90) };
        }
        protected override IEnumerable<PartItem> GetItems(GuideLine guide, MarkupLOD lod)
        {
            var width = Width.Value;
            GetItemParams(ref width, 90f, lod, out int itemsCount, out float itemWidth, out float itemStep);
            foreach (var part in GetParts(guide, width, width * (Step - 1)))
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Angle.FromXml(config, DefaultAngle);
            Step.FromXml(config, DefaultStepGrid);
        }
    }
    public class SolidFillerStyle : Filler2DStyle, IGuideFiller, IFollowGuideFiller, IColorStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<int> LeftGuideA { get; }
        public PropertyValue<int> RightGuideA { get; }
        public PropertyValue<int> LeftGuideB { get; }
        public PropertyValue<int> RightGuideB { get; }
        public PropertyValue<bool> FollowGuides { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Offset);
                yield return nameof(Guide);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideA), LeftGuideA);
                yield return new StylePropertyDataProvider<bool>(nameof(FollowGuides), FollowGuides);
            }
        }

        public SolidFillerStyle(Color32 color, float lineOffset, float medianOffset, bool followGuides = false) : base(color, DefaultSolidWidth, lineOffset, medianOffset)
        {
            LeftGuideA = GetLeftGuideAProperty(0);
            LeftGuideB = GetLeftGuideBProperty(1);
            RightGuideA = GetRightGuideAProperty(1);
            RightGuideB = GetRightGuideBProperty(2);
            FollowGuides = GetFollowGuidesProperty(followGuides);
        }

        public override FillerStyle CopyStyle() => new SolidFillerStyle(Color, LineOffset, DefaultOffset);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IGuideFiller guideTarget)
            {
                guideTarget.LeftGuideA.Value = LeftGuideA;
                guideTarget.LeftGuideB.Value = LeftGuideB;
                guideTarget.RightGuideA.Value = RightGuideA;
                guideTarget.RightGuideB.Value = RightGuideB;
            }
            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = FollowGuides;
        }

        protected override IEnumerable<GuideLine> GetGuides(MarkupFiller filler, ITrajectory[] contour)
        {
            var rect = GetRect(contour);

            if (FollowGuides)
            {
                var left = filler.Contour.GetGuide(LeftGuideA, LeftGuideB, RightGuideA, RightGuideB);
                var right = filler.Contour.GetGuide(RightGuideA, RightGuideB, LeftGuideA, LeftGuideB);
                var startPos = (right.EndPosition + left.StartPosition) / 2;
                var endPos = (right.StartPosition + left.EndPosition) / 2;
                var angle = (endPos - startPos).Turn90(true).AbsoluteAngle() * Mathf.Rad2Deg;
                yield return new GuideLine() { GetGuide(rect, filler.Markup.Height, angle) };
            }
            else
                yield return new GuideLine() { GetGuide(rect, filler.Markup.Height, 0) };
        }
        protected override IEnumerable<PartItem> GetItems(GuideLine guide, MarkupLOD lod)
        {
            foreach (var part in guide.OfType<StraightTrajectory>())
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
                components.Add(AddGuideProperty(this, filler.Contour, parent, true));
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LeftGuideA.ToXml(config);
            LeftGuideB.ToXml(config);
            RightGuideA.ToXml(config);
            RightGuideB.ToXml(config);
            FollowGuides.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LeftGuideA.FromXml(config);
            LeftGuideB.FromXml(config);
            RightGuideA.FromXml(config);
            RightGuideB.FromXml(config);
            FollowGuides.FromXml(config, DefaultFollowGuides);
        }
    }
}
