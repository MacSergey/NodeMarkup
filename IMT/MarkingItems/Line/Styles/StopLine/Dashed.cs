using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class DashedStopLineStyle : StopLineStyle, IStopLine, IDashedLine, IEffectStyle
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;
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

        public DashedStopLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength) : base(color, width, cracks, voids, texture)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        protected override void CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Width, DashLength))
            {
                var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
                var parts = StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength);
                foreach (var part in parts)
                {
                    StyleHelper.GetPartParams(trajectory, part, offset, offset, out var pos, out var dir);
                    var data = new DecalData(this, MaterialType.RectangleLines, lod, pos, dir, DashLength, Width, Color);
                    addData(data);
                }
            }
        }

        public override StopLineStyle CopyLineStyle() => new DashedStopLineStyle(Color, Width, Cracks, Voids, Texture, DashLength, SpaceLength);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddLengthProperty(this, parent, false));
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
