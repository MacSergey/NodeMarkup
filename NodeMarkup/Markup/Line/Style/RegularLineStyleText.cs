using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class RegularLineStyleText : RegularLineStyle, IColorStyle
    {
        private static Dictionary<int, Texture2D> Textures { get; } = new Dictionary<int, Texture2D>();

        public override StyleType Type => StyleType.LineText;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0/* | MarkupLOD.LOD1*/;

        private PropertyStringValue Text { get; }
        private PropertyStructValue<float> Scale { get; }
        private PropertyStructValue<float> Angle { get; }
        private PropertyBoolValue Vertical { get; }
#if DEBUG
        private PropertyStructValue<float> Ratio { get; }
#endif

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Text);
                yield return nameof(Color);
                yield return nameof(Scale);
                yield return nameof(Angle);
                yield return nameof(Vertical);
#if DEBUG
                yield return nameof(Ratio);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public RegularLineStyleText(Color32 color, string text, float scale, float angle, bool vertical) : base(color, default)
        {
            Text = new PropertyStringValue("T", StyleChanged, text);
            Scale = new PropertyStructValue<float>(StyleChanged, scale);
            Angle = new PropertyStructValue<float>(StyleChanged, angle);
            Vertical = new PropertyBoolValue(StyleChanged, vertical);
#if DEBUG
            Ratio = new PropertyStructValue<float>(StyleChanged, 0.05f);
#endif
        }

        public override RegularLineStyle CopyLineStyle() => new RegularLineStyleText(Color, Text, Scale, Angle, Vertical);

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var text = Text.Value;
            if(Vertical)
            {
                text = text.Replace(" ", "\n");
                text = string.Join("\n",text.Select(c => c.ToString()).ToArray());
            }

            var aciTexture = RenderHelper.CreateTextTexture(text, Scale);

            var textureId = (aciTexture.height << 16) + aciTexture.width;
            if (!Textures.TryGetValue(textureId, out var mainTexture))
            {
                mainTexture = TextureHelper.CreateTexture(aciTexture.width, aciTexture.height, UnityEngine.Color.white);
                Textures[textureId] = mainTexture;
            }

            Material material = RenderHelper.CreateDecalMaterial(mainTexture, aciTexture);

            var position = line.Trajectory.Position(0.5f);
            var angle = line.Trajectory.Tangent(0.5f).AbsoluteAngle() + Angle * Mathf.Deg2Rad;
            var width = aciTexture.width * Ratio;
            var height = aciTexture.height * Ratio;
            var data = new MarkupPartData(position, angle, width, height, Color, material);

            var groupData = new MarkupPartGroupData(lod, new MarkupPartData[] { data });
            return groupData;
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddTextProperty(parent, false));
            components.Add(AddScaleProperty(parent, false));
            components.Add(AddAngleProperty(parent, false));
            components.Add(AddVerticalProperty(parent, false));
#if DEBUG
            components.Add(AddRatioProperty(parent, false));
#endif
        }
        protected StringPropertyPanel AddTextProperty(UIComponent parent, bool canCollapse)
        {
            var textProperty = ComponentPool.Get<StringPropertyPanel>(parent, nameof(Text));
            textProperty.Text = "Text";
            textProperty.FieldWidth = 230f;
            textProperty.CanCollapse = canCollapse;
            textProperty.Init();
            textProperty.Value = Text;
            textProperty.OnValueChanged += (string value) => Text.Value = value;

            return textProperty;
        }
        protected FloatPropertyPanel AddScaleProperty(UIComponent parent, bool canCollapse)
        {
            var sizeProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Scale));
            sizeProperty.Text = "Size";
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = 0.1f;
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = 0.1f;
            sizeProperty.CanCollapse = canCollapse;
            sizeProperty.Init();
            sizeProperty.Value = Scale;
            sizeProperty.OnValueChanged += (float value) => Scale.Value = value;

            return sizeProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Angle));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.CyclicalValue = true;
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (float value) => Angle.Value = value;

            return angleProperty;
        }
        protected BoolListPropertyPanel AddVerticalProperty(UIComponent parent, bool canCollapse)
        {
            var parallelProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Vertical));
            parallelProperty.Text = "Vertical";
            parallelProperty.CanCollapse = canCollapse;
            parallelProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            parallelProperty.SelectedObject = Vertical;
            parallelProperty.OnSelectObjectChanged += (value) => Vertical.Value = value;

            return parallelProperty;
        }

#if DEBUG
        protected FloatPropertyPanel AddRatioProperty(UIComponent parent, bool canCollapse)
        {
            var sizeProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Ratio));
            sizeProperty.Text = "Pixel ratio";
            sizeProperty.Format = Localize.NumberFormat_Meter;
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = 0.01f;
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = 0.005f;
            sizeProperty.CanCollapse = canCollapse;
            sizeProperty.Init();
            sizeProperty.Value = Ratio;
            sizeProperty.OnValueChanged += (float value) => Ratio.Value = value;

            return sizeProperty;
        }
#endif
    }
}
