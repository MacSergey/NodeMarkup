using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class RegularLineStyleText : RegularLineStyle, IColorStyle
    {
        private static Dictionary<TextureId, Texture2D> ACITextures { get; } = new Dictionary<TextureId, Texture2D>(TextureComparer.Instance);
        private static Dictionary<TextureId, int> ACITextureCount { get; } = new Dictionary<TextureId, int>();
        private static Dictionary<int, Texture2D> MainTextures { get; } = new Dictionary<int, Texture2D>();

        public override StyleType Type => StyleType.LineText;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0/* | MarkupLOD.LOD1*/;
        public override bool CanOverlap => true;

        private PropertyStringValue Text { get; }
        private PropertyStringValue Font { get; }
        private PropertyStructValue<float> Scale { get; }
        private PropertyStructValue<float> Shift { get; }
        private PropertyStructValue<float> Angle { get; }
        private PropertyEnumValue<TextDirection> Direction { get; }
        private PropertyVector2Value Spacing { get; }

        private TextureId PrevTextureId { get; set; }

#if DEBUG
        private PropertyStructValue<float> Ratio { get; }
#else
        private static float Ratio => 0.05f;
#endif

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Text);
                yield return nameof(Font);
                yield return nameof(Color);
                yield return nameof(Scale);
                yield return nameof(Direction);
                yield return nameof(Spacing);
                yield return nameof(Shift);
                yield return nameof(Angle);
#if DEBUG
                yield return nameof(Ratio);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public RegularLineStyleText(Color32 color, string font, string text, float scale, float angle, float shift, TextDirection direction, Vector2 spacing) : base(color, default)
        {
            Text = new PropertyStringValue("TX", StyleChanged, text);
            Font = new PropertyStringValue("F", StyleChanged, font);
            Scale = new PropertyStructValue<float>("S", StyleChanged, scale);
            Angle = new PropertyStructValue<float>("A", StyleChanged, angle);
            Shift = new PropertyStructValue<float>("SF", StyleChanged, shift);
            Direction = new PropertyEnumValue<TextDirection>("V", StyleChanged, direction);
            Spacing = new PropertyVector2Value(StyleChanged, spacing, "SPC", "SPL");
#if DEBUG
            Ratio = new PropertyStructValue<float>(StyleChanged, 0.05f);
#endif
        }
        ~RegularLineStyleText()
        {
            RemoveTexture(PrevTextureId);
        }
        protected override void StyleChanged()
        {
            RemoveTexture(PrevTextureId);
            PrevTextureId = default;
            base.StyleChanged();
        }
        private static void RemoveTexture(TextureId textureId)
        {
            if (textureId.IsDefault)
                return;

            lock (ACITextures)
            {
                if (ACITextureCount.TryGetValue(textureId, out var count))
                {
                    count -= 1;
                    if (count <= 0)
                    {
                        ACITextureCount.Remove(textureId);
                        ACITextures.Remove(textureId);
                    }
                    else
                        ACITextureCount[textureId] = count;
#if DEBUG
                    SingletonMod<Mod>.Logger.Debug($"Removed ({count}) {textureId}");
#endif
                }
            }
        }
        private static void AddTexture(TextureId textureId)
        {
            if (textureId.IsDefault)
                return;

            lock (ACITextures)
            {
                if (ACITextureCount.TryGetValue(textureId, out var count))
                    count += 1;
                else
                    count = 1;

                ACITextureCount[textureId] = count;
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Added ({count}) {textureId}");
#endif
            }
        }

        public override RegularLineStyle CopyLineStyle() => new RegularLineStyleText(Color, Font, Text, Scale, Angle, Shift, Direction, Spacing);

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (string.IsNullOrEmpty(Text))
                return new MarkupPartGroupData(lod);

            var text = Text.Value;
            if (Direction == TextDirection.TopToBottom)
                text = string.Join("\n", text.Select(c => c.ToString()).ToArray());
            else if (Direction == TextDirection.BottomToTop)
                text = string.Join("\n", text.Reverse().Select(c => c.ToString()).ToArray());

            var aciTextureId = new TextureId(Font, text, Scale, Spacing);
            if (!ACITextures.TryGetValue(aciTextureId, out var aciTexture))
            {
                aciTexture = RenderHelper.CreateTextTexture(Font, text, Scale, Spacing);
                ACITextures[aciTextureId] = aciTexture;
            }

            if (!TextureComparer.Instance.Equals(aciTextureId, PrevTextureId))
            {
                RemoveTexture(PrevTextureId);
                AddTexture(aciTextureId);
                PrevTextureId = aciTextureId;
            }

            var mainTextureId = (aciTexture.height << 16) + aciTexture.width;
            if (!MainTextures.TryGetValue(mainTextureId, out var mainTexture))
            {
                mainTexture = TextureHelper.CreateTexture(aciTexture.width, aciTexture.height, UnityEngine.Color.white);
                MainTextures[mainTextureId] = mainTexture;
            }

            Material material = RenderHelper.CreateDecalMaterial(mainTexture, aciTexture);

            var direction = line.Trajectory.Tangent(0.5f);
            var position = line.Trajectory.Position(0.5f) + direction.MakeFlatNormalized().Turn90(true) * Shift;
            var angle = direction.AbsoluteAngle() + (Angle + 90) * Mathf.Deg2Rad;
            var width = aciTexture.width * Ratio;
            var height = aciTexture.height * Ratio;
            var data = new MarkupPartData(position, angle, width, height, Color, material);

            var groupData = new MarkupPartGroupData(lod, new MarkupPartData[] { data });
            return groupData;
        }
        private void GetTextures()
        {

        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddFontProperty(parent, false));
            components.Add(AddTextProperty(parent, false));
            components.Add(AddScaleProperty(parent, false));
            components.Add(AddAngleProperty(parent, true));
            components.Add(AddShiftProperty(parent, true));
            components.Add(AddDirectionProperty(parent, true));
            components.Add(AddSpacingProperty(parent, true));
#if DEBUG
            components.Add(AddRatioProperty(parent, true));
#endif
        }
        protected FontPtopertyPanel AddFontProperty(UIComponent parent, bool canCollapse)
        {
            var fontProperty = ComponentPool.Get<FontPtopertyPanel>(parent, nameof(Font));
            fontProperty.Text = Localize.StyleOption_Font;
            fontProperty.UseWheel = true;
            fontProperty.CanCollapse = canCollapse;
            fontProperty.Init();
            fontProperty.Font = string.IsNullOrEmpty(Font.Value) ? null : Font.Value;
            fontProperty.OnValueChanged += (string value) => Font.Value = value;
            return fontProperty;
        }
        protected StringPropertyPanel AddTextProperty(UIComponent parent, bool canCollapse)
        {
            var textProperty = ComponentPool.Get<StringPropertyPanel>(parent, nameof(Text));
            textProperty.Text = Localize.StyleOption_Text;
            textProperty.FieldWidth = 230f;
            //textProperty.Multyline = true;
            //textProperty.TextScale = 1f;
            textProperty.CanCollapse = canCollapse;
            textProperty.Init();
            textProperty.Value = Text;
            textProperty.OnValueChanged += (string value) => Text.Value = value;

            return textProperty;
        }
        protected FloatPropertyPanel AddScaleProperty(UIComponent parent, bool canCollapse)
        {
            var sizeProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Scale));
            sizeProperty.Text = Localize.StyleOption_ObjectScale;
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = 0.1f;
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = 1f;
            sizeProperty.CheckMax = true;
            sizeProperty.MaxValue = 10f;
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
        protected FloatPropertyPanel AddShiftProperty(UIComponent parent, bool canCollapse)
        {
            var shiftProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Shift));
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.Format = Localize.NumberFormat_Meter;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.CyclicalValue = false;
            shiftProperty.CanCollapse = canCollapse;
            shiftProperty.Init();
            shiftProperty.Value = Shift;
            shiftProperty.OnValueChanged += (float value) => Shift.Value = value;

            return shiftProperty;
        }
        protected TextDirectionPanel AddDirectionProperty(UIComponent parent, bool canCollapse)
        {
            var directionProperty = ComponentPool.Get<TextDirectionPanel>(parent, nameof(Direction));
            directionProperty.Text = Localize.StyleOption_TextDirection;
            directionProperty.CanCollapse = canCollapse;
            directionProperty.Selector.AutoButtonSize = false;
            directionProperty.Selector.ButtonWidth = 33f;
            directionProperty.Init();
            directionProperty.SelectedObject = Direction;
            directionProperty.OnSelectObjectChanged += (value) => Direction.Value = value;

            return directionProperty;
        }
        protected Vector2PropertyPanel AddSpacingProperty(UIComponent parent, bool canCollapse)
        {
            var spacingProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Spacing));
            spacingProperty.Text = Localize.StyleOption_Spacing;
            spacingProperty.SetLabels(Localize.StyleOption_SpacingChar, Localize.StyleOption_SpacingLine);
            spacingProperty.UseWheel = true;
            spacingProperty.WheelStep = new Vector2(1f, 1f);
            spacingProperty.WheelTip = Settings.ShowToolTip;
            spacingProperty.CheckMin = true;
            spacingProperty.MinValue = new Vector2(-10f, -10f);
            spacingProperty.CheckMax = true;
            spacingProperty.MaxValue = new Vector2(10f, 10f);
            spacingProperty.CanCollapse = canCollapse;
            spacingProperty.FieldsWidth = 50f;
            spacingProperty.Init(0, 1);
            spacingProperty.Value = Spacing;
            spacingProperty.OnValueChanged += (Vector2 value) => Spacing.Value = value;

            return spacingProperty;
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

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Font.ToXml(config);
            Text.ToXml(config);
            Scale.ToXml(config);
            Angle.ToXml(config);
            Shift.ToXml(config);
            Direction.ToXml(config);
            Spacing.ToXml(config);
            return config;
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Font.FromXml(config, string.Empty);
            Text.FromXml(config, string.Empty);
            Scale.FromXml(config, DefaultTextScale);
            Angle.FromXml(config, DefaultObjectAngle);
            Shift.FromXml(config, DefaultObjectShift);
            Direction.FromXml(config, TextDirection.LeftToRight);
            Spacing.FromXml(config, Vector2.zero);
            if (map.IsMirror ^ invert)
            {
                Angle.Value = Angle.Value >= 0 ? Angle.Value - 180f : Angle.Value + 180f;
                Shift.Value = -Shift.Value;
            }
        }

        public enum TextDirection
        {
            [Description(nameof(Localize.StyleOption_TextDirectionLtoR))]
            [Sprite(nameof(NodeMarkupTextures.LeftToRightButtonIcons))]
            LeftToRight,

            [Description(nameof(Localize.StyleOption_TextDirectionTtoB))]
            [Sprite(nameof(NodeMarkupTextures.TopToBottomButtonIcons))]
            TopToBottom,

            [Description(nameof(Localize.StyleOption_TextDirectionBtoT))]
            [Sprite(nameof(NodeMarkupTextures.BottomToTopButtonIcons))]
            BottomToTop,
        }
        public class TextDirectionPanel : EnumOncePropertyPanel<TextDirection, TextDirectionPanel.TextDirectionSegmented>
        {
            protected override bool IsEqual(TextDirection first, TextDirection second) => first == second;
            protected override string GetDescription(TextDirection value) => value.Description();

            protected override void FillItems(Func<TextDirection, bool> selector)
            {
                Selector.StopLayout();
                foreach (var value in GetValues())
                {
                    if (selector?.Invoke(value) != false)
                    {
                        var sprite = value.Sprite();
                        if (string.IsNullOrEmpty(sprite))
                            Selector.AddItem(value, GetDescription(value));
                        else
                            Selector.AddItem(value, GetDescription(value), NodeMarkupTextures.Atlas, sprite);
                    }
                }
                Selector.StartLayout();
            }

            public class TextDirectionSegmented : UIOnceSegmented<TextDirection> { }
        }

        public struct TextureId
        {
            public string font;
            public string text;
            public float scale;
            public Vector2 spacing;

            public TextureId(string font, string text, float scale, Vector2 spacing)
            {
                this.font = font;
                this.text = text;
                this.scale = scale;
                this.spacing = spacing;
            }

            public bool IsDefault => string.IsNullOrEmpty(text) && string.IsNullOrEmpty(font) && scale == default && spacing == default;

            public override string ToString()
            {
                return $"Text:\"{text?.Replace("\n", "\\n")}\" Font:\"{font}\" Scale:{scale} Spacing:{spacing}";
            }
        }
        public class TextureComparer : IEqualityComparer<TextureId>
        {
            public static TextureComparer Instance { get; } = new TextureComparer();
            public bool Equals(TextureId x, TextureId y)
            {
                return x.text == y.text && x.font == y.font && x.scale == y.scale && x.spacing == y.spacing;
            }

            public int GetHashCode(TextureId id)
            {
                int hash = 17;
                if (id.text != null)
                    hash = hash * 31 + id.text.GetHashCode();
                if (id.font != null)
                    hash = hash * 31 + id.font.GetHashCode();
                hash = hash * 31 + id.scale.GetHashCode();
                hash = hash * 31 + id.spacing.GetHashCode();
                return hash;
            }
        }
    }
}
