﻿using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class GridFillerStyle : PeriodicFillerStyle, IPeriodicFiller, IRotateFiller, IWidthStyle, IColorStyle, IEffectStyle
    {
        public override StyleType Type => StyleType.FillerGrid;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        public bool KeepColor => true;
        protected override float DefaultStep => DefaultStepGrid;

        public PropertyValue<float> Angle { get; }

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
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(Debug);
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
                //yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public GridFillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float angle, float step, Vector2 offset) : base(color, width, cracks, voids, texture, step, offset)
        {
            Angle = GetAngleProperty(angle);
        }

        public override BaseFillerStyle CopyStyle() => new GridFillerStyle(Color, Width, Cracks, Voids, Texture, Angle, Step, Offset);
        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);

            if (target is IRotateFiller rotateTarget)
                rotateTarget.Angle.Value = Angle;

            if (target is IPeriodicFiller periodicTarget)
                periodicTarget.Step.Value = Step;
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Step), MainCategory, AddStepProperty));
            if (!provider.isTemplate)
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Angle), MainCategory, AddAngleProperty));
        }

        protected override ITrajectory[] GetGuides(MarkingFiller filler, ContourGroup contours)
        {
            return new ITrajectory[]
            {
                GetGuide(contours.Limits, filler.Marking.Height, Angle),
                GetGuide(contours.Limits, filler.Marking.Height, Angle < 0 ? Angle + 90 : Angle - 90)
            };
        }

#if DEBUG_PERIODIC_FILLER
        protected override List<Part> GetParts(ITrajectory guide, EdgeSetGroup contours, MarkingLOD lod, Action<IStyleData> addData)
#else
        protected override List<Part> GetParts(ITrajectory guide, ContourGroup contours, MarkingLOD lod)
#endif
        {
            var width = Width.Value;
            var trajectories = GetPartTrajectories(guide, contours.Limits, width, width * (Step - 1));

            var parts = new List<Part>(trajectories.Count);
            for (var i = 0; i < trajectories.Count; i += 1)
            {
                var part = new Part(trajectories[i], null, null, 90f, true);
                if (part.CanIntersect(contours, true))
                    parts.Add(part);
            }
            return parts;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Angle.FromXml(config, DefaultAngle);
        }
    }
}
