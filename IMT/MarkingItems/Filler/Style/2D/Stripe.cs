using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class StripeFillerStyle : GuideFillerStyle, IFollowGuideFiller, IRotateFiller, IWidthStyle, IColorStyle, ITexture
    {
        public override StyleType Type => StyleType.FillerStripe;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;

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
                yield return nameof(Scratches);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(RenderOnly);
                yield return nameof(Start);
                yield return nameof(End);
                yield return nameof(StartBorder);
                yield return nameof(EndBorder);
#endif
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
                yield return new StylePropertyDataProvider<int>(nameof(LeftGuideB), LeftGuideB);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideA), RightGuideA);
                yield return new StylePropertyDataProvider<int>(nameof(RightGuideB), RightGuideB);
                yield return new StylePropertyDataProvider<bool>(nameof(FollowGuides), FollowGuides);
            }
        }

        public StripeFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float angle, float step, Vector2 scratches, Vector2 voids, bool followGuides = false) : base(color, width, step, lineOffset, medianOffset, scratches, voids)
        {
            Angle = GetAngleProperty(angle);
            FollowGuides = GetFollowGuidesProperty(followGuides);
        }
        public override FillerStyle CopyStyle() => new StripeFillerStyle(Color, Width, LineOffset, DefaultOffset, DefaultAngle, Step, Scratches, Voids, FollowGuides);
        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IFollowGuideFiller followGuideTarget)
                followGuideTarget.FollowGuides.Value = FollowGuides;
        }
        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
            {
                components.Add(AddAngleProperty(this, parent, false));
            }
        }

        protected override ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours)
        {
            if (FollowGuides)
                return base.GetGuides(filler, contours);
            else
                return new ITrajectory[] { GetGuide(contours.Limits, filler.Marking.Height, Angle) };
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var angle = FollowGuides ? 90f - Angle : 90f;
            var coef = Math.Max(Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad), 0.01f);
            var width = Width.Value / coef;

            var limits = contours.GetLimits();
            var trajectories = GetPartTrajectories(guide, limits, width, width * (Step - 1));
            var parts = new List<Part>(trajectories.Count);

            if (!FollowGuides)
            {
                for (var i = 0; i < trajectories.Count; i += 1)
                {
                    var part = new Part(trajectories[i], null, null, angle, true);
                    if (part.CanIntersect(contours, true))
                        parts.Add(part);
                }
            }
            else
            {
                var borders = BorderPair.GetBorders(trajectories, contours, angle, true);
                for (var i = 0; i < borders.Count; i += 1)
                {
#if DEBUG
                    Border? startBorder = null;
                    Border? endBorder = null;
                    if (RenderOnly == -1 || i == RenderOnly)
                        borders[i].GetPartBorders(borders, i, out startBorder, out endBorder);
#else
                    borders[i].GetPartBorders(borders, i, out var startBorder, out var endBorder);
#endif
                    parts.Add(new Part(borders[i].trajectory, startBorder, endBorder, angle, true));
                }

#if DEBUG_PERIODIC_FILLER
                var dashes = new List<MarkingPartData>();

                for (var i = 0; i < borders.Count; i += 1)
                    borders[i].Draw(dashes);

                if (RenderOnly != -1 && parts.Count > RenderOnly)
                {
                    parts[RenderOnly].startBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
                    parts[RenderOnly].endBorder?.Draw(dashes, UnityEngine.Color.blue, 0.3f);
                }

                addData(new MarkingPartGroupData(lod, dashes));
#endif
            }

            return parts;
        }

        protected override float GetAngle() => 90f - Angle;

        public override void Render(MarkingFiller filler, OverlayData data)
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
}
