using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class DashedLineStyle : RegularLineStyle, IRegularLine, IDashedLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Length);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
            }
        }

        public DashedLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength) : base(color, width, cracks, voids, texture)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override RegularLineStyle CopyLineStyle() => new DashedLineStyle(Color, Width, Cracks, Voids, Texture, DashLength, SpaceLength);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Width, DashLength))
            {
                var borders = line.Borders;
                var parts = StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength);
                foreach (var part in parts)
                {
                    var partTrajectory = trajectory.Cut(part.start, part.end);
                    CalculateDashes(trajectory, part, borders, lod, addData);
                }
            }
        }

        protected virtual void CalculateDashes(ITrajectory trajectory, StyleHelper.PartT partT, LineBorders borders, MarkingLOD lod, Action<IStyleData> addData)
        {
            StyleHelper.GetPartParams(trajectory, partT, 0f, out var pos, out var dir);
            if (StyleHelper.CheckBorders(borders, pos, dir, DashLength, Width))
            {
                var data = new DecalData(this, MaterialType.Dash, lod, pos, dir, DashLength, Width, Color);
                addData(data);
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Length), false, AddLengthProperty));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
}
