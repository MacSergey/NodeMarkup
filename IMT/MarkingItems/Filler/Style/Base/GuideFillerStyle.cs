using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace IMT.Manager
{
    public abstract class GuideFillerStyle : PeriodicFillerStyle, IGuideFiller
    {
        public PropertyValue<int> LeftGuideA { get; }
        public PropertyValue<int> RightGuideA { get; }
        public PropertyValue<int> LeftGuideB { get; }
        public PropertyValue<int> RightGuideB { get; }

        public GuideFillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float step, Vector2 offset) : base(color, width, cracks, voids, texture, step, offset)
        {
            LeftGuideA = GetLeftGuideAProperty(0);
            LeftGuideB = GetLeftGuideBProperty(1);
            RightGuideA = GetRightGuideAProperty(1);
            RightGuideB = GetRightGuideBProperty(2);
        }

        public override void CopyTo(BaseFillerStyle target)
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

        protected abstract float GetAngle();

        protected override ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours)
        {
            var parts = new List<ITrajectory>();
            var halfAngelRad = GetAngle() * Mathf.Deg2Rad;
            if (GetMiddleLine(filler.Contour) is ITrajectory middleLine)
            {
                if (GetBeforeMiddleLine(middleLine, filler.Marking.Height, halfAngelRad, contours.Limits, out ITrajectory lineBefore))
                    parts.Add(lineBefore.Invert());
                parts.Add(middleLine);
                if (GetAfterMiddleLine(middleLine, filler.Marking.Height, halfAngelRad, contours.Limits, out ITrajectory lineAfter))
                    parts.Add(lineAfter);
            }

            return new ITrajectory[] { parts.Count == 1 ? parts[0] : new CombinedTrajectory(parts) };
        }
        private ITrajectory GetMiddleLine(FillerContour contour)
        {
            GetGuides(contour, out ITrajectory left, out ITrajectory right);
            if (left == null || right == null)
                return null;

            var leftLength = left.Length;
            var rightLength = right.Length;

            var leftRatio = leftLength / (leftLength + rightLength);
            var rightRatio = rightLength / (leftLength + rightLength);

            var straight = new StraightTrajectory((right.EndPosition + left.StartPosition) * 0.5f, (right.StartPosition + left.EndPosition) * 0.5f);
            var middle = new Bezier3()
            {
                a = (right.EndPosition + left.StartPosition) * 0.5f,
                b = GetDirection(rightRatio * right.EndDirection, leftRatio * left.StartDirection, straight.StartDirection),
                c = GetDirection(rightRatio * right.StartDirection, leftRatio * left.EndDirection, straight.EndDirection),
                d = (right.StartPosition + left.EndPosition) * 0.5f,
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
        private bool GetBeforeMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect limits, out ITrajectory line) => GetAdditionalLine(middleLine.StartPosition, -middleLine.StartDirection, height, halfAngelRad, limits, out line);
        private bool GetAfterMiddleLine(ITrajectory middleLine, float height, float halfAngelRad, Rect limits, out ITrajectory line) => GetAdditionalLine(middleLine.EndPosition, -middleLine.EndDirection, height, halfAngelRad, limits, out line);

        private bool GetAdditionalLine(Vector3 pos, Vector3 dir, float height, float halfAngelRad, Rect limits, out ITrajectory line)
        {
            var dirRight = dir.TurnRad(halfAngelRad, true);
            var dirLeft = dir.TurnRad(halfAngelRad, false);

            var rightGuide = GetGuide(limits, height, dirRight.AbsoluteAngle() * Mathf.Rad2Deg);
            var leftGuide = GetGuide(limits, height, dirLeft.AbsoluteAngle() * Mathf.Rad2Deg);

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

        protected void GetGuides(FillerContour contour, out ITrajectory left, out ITrajectory right)
        {
            left = contour.GetGuide(LeftGuideA, LeftGuideB, RightGuideA, RightGuideB);
            right = contour.GetGuide(RightGuideA, RightGuideB, LeftGuideA, LeftGuideB);
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);

            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<FillerGuidePropertyPanel>(this, nameof(Guide), MainCategory, AddGuideProperty));
            }
        }
        protected void AddGuideProperty(FillerGuidePropertyPanel guideProperty, EditorProvider provider)
        {
            if (provider.editor.EditObject is MarkingFiller filler)
            {
                var contour = filler.Contour;
                guideProperty.Text = Localize.StyleOption_Rails;
                guideProperty.Init(contour.ProcessedCount);
                guideProperty.LeftGuide = new FillerGuide(contour.GetCorrectIndex(LeftGuideA), contour.GetCorrectIndex(LeftGuideB));
                guideProperty.RightGuide = new FillerGuide(contour.GetCorrectIndex(RightGuideA), contour.GetCorrectIndex(RightGuideB));
                guideProperty.Follow = (this as IFollowGuideFiller)?.FollowGuides.Value;
                guideProperty.OnValueChanged += (bool follow, FillerGuide left, FillerGuide right) =>
                {
                    if (this is IFollowGuideFiller followGuideStyle)
                        followGuideStyle.FollowGuides.Value = follow;

                    LeftGuideA.Value = left.a;
                    LeftGuideB.Value = left.b;
                    RightGuideA.Value = right.a;
                    RightGuideB.Value = right.b;
                };
            }
        }

        public override void Render(MarkingFiller filler, OverlayData data)
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
}
