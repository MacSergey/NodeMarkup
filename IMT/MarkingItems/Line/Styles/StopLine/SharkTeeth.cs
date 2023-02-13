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
    public class SharkTeethStopLineStyle : StopLineStyle, IColorStyle, ISharkLine, IEffectStyle
    {
        public override StyleType Type { get; } = StyleType.StopLineSharkTeeth;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Triangle);
                yield return nameof(Space);
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
                yield return new StylePropertyDataProvider<float>(nameof(Base), Base);
                yield return new StylePropertyDataProvider<float>(nameof(Height), Height);
                yield return new StylePropertyDataProvider<float>(nameof(Space), Space);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public SharkTeethStopLineStyle(Color32 color, Vector2 cracks, Vector2 voids, float texture, float baseValue, float height, float space) : base(color, 0f, cracks, voids, texture)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
        }
        protected override void CalculateImpl(MarkingStopLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Base, Height))
            {
                var parts = StyleHelper.CalculateDashed(trajectory, Base, Space);
                foreach (var part in parts)
                {
                    StyleHelper.GetPartParams(trajectory, part.Invert, Height * -0.5f, out var pos, out var angle);
                    var data = new DecalData(this, MaterialType.Triangle, lod, pos, angle, Base, Height, Color);
                    addData(data);
                }
            }
        }

        public override StopLineStyle CopyLineStyle() => new SharkTeethStopLineStyle(Color, Cracks, Voids, Texture, Base, Height, Space);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethStopLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }

        protected override void GetUIComponents(MarkingStopLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Triangle), MainCategory, AddTriangleProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Space), MainCategory, AddSpaceProperty));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
        }
    }
}
