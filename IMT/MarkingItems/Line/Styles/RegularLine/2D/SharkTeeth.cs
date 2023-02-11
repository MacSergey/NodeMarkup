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
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public class SharkTeethLineStyle : RegularLineStyle, IColorStyle, IAsymLine, ISharkLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineSharkTeeth;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<float> Angle { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Triangle);
                yield return nameof(Space);
                yield return nameof(Angle);
                yield return nameof(Invert);
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
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public SharkTeethLineStyle(Color32 color, Vector2 cracks, Vector2 voids, float texture, float baseValue, float height, float space, float angle) : base(color, 0f, cracks, voids, texture)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
            Invert = GetInvertProperty(true);
            Angle = GetAngleProperty(angle);
        }
        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Height, Base))
            {
                var borders = line.Borders;
                var ratio = Mathf.Cos(Angle * Mathf.Deg2Rad);
                var parts = StyleHelper.CalculateDashed(trajectory, Base / ratio, Space / ratio);
                var offset = Height / (Invert ? 2 : -2);
                foreach (var part in parts)
                {
                    StyleHelper.GetPartParams(trajectory, Invert ? part.Invert : part, offset, out var pos, out var dir);
                    if (StyleHelper.CheckBorders(borders, pos, dir, Base, Height))
                    {
                        var data = new DecalData(this, MaterialType.Triangle, lod, pos, dir, Base, Height, Color);
                        addData(data);
                    }
                }
            }
        }

        public override RegularLineStyle CopyLineStyle() => new SharkTeethLineStyle(Color, Cracks, Voids, Texture, Base, Height, Space, Angle);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Triangle), MainCategory, AddTriangleProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Space), MainCategory, AddSpaceProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Angle), AdditionalCategory, AddAngleProperty, RefreshAngleProperty));

            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<ButtonPanel>(this, nameof(Invert), MainCategory, AddInvertProperty));
            }
        }

        protected void AddAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.Text = Localize.StyleOption_SharkToothAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -60f;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 60f;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (value) => Angle.Value = value;
        }
        protected virtual void RefreshAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.Value = Angle;
        }

        new protected void AddInvertProperty(ButtonPanel invertButton, EditorProvider provider)
        {
            invertButton.Text = Localize.StyleOption_Invert;
            invertButton.Init();

            invertButton.OnButtonClick += () =>
            {
                Invert.Value = !Invert;
                Angle.Value = -Angle;
                provider.Refresh();
            };
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            Invert.ToXml(config);
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
            Angle.FromXml(config, DefaultSharkAngle);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;
        }
    }
}
