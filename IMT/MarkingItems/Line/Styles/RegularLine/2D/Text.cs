using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IMT.Manager
{
    public class RegularLineStyleText : RegularLineStyle, IColorStyle, IEffectStyle
    {
        private static Dictionary<TextureId, TextureData> TextTextures { get; } = new Dictionary<TextureId, TextureData>(TextureComparer.Instance);
        private static Dictionary<TextureId, int> TextTextureCount { get; } = new Dictionary<TextureId, int>();

        public override StyleType Type => StyleType.LineText;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        public bool KeepColor => true;
        public override bool CanOverlap => true;

        private PropertyStringValue Text { get; }
        private PropertyStringValue Font { get; }
        private PropertyStructValue<float> Scale { get; }
        private PropertyStructValue<float> Shift { get; }
        private PropertyStructValue<float> Angle { get; }
        private PropertyEnumValue<TextDirection> Direction { get; }
        private PropertyVector2Value Spacing { get; }
        private PropertyEnumValue<TextAlignment> Alignment { get; }
        private PropertyStructValue<float> Offset { get; }

        private Dictionary<MarkingLOD, TextureId> PrevTextureId { get; } = new Dictionary<MarkingLOD, TextureId>()
        {
            { MarkingLOD.NoLOD, default },
            { MarkingLOD.LOD0, default },
            { MarkingLOD.LOD1, default },
        };

#if DEBUG
        private PropertyStructValue<float> Ratio { get; }
#else
        private static float Ratio => 0.025f;
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
                yield return nameof(Spacing);
                yield return nameof(Direction);
                yield return nameof(Alignment);
                yield return nameof(Offset);
                yield return nameof(Shift);
                yield return nameof(Angle);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(Ratio);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<string>(nameof(Text), Text);
                yield return new StylePropertyDataProvider<string>(nameof(Font), Font);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<TextDirection>(nameof(Direction), Direction);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Spacing), Spacing);
                yield return new StylePropertyDataProvider<TextAlignment>(nameof(Alignment), Alignment);
                yield return new StylePropertyDataProvider<float>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<float>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public RegularLineStyleText(Color32 color, Vector2 cracks, Vector2 voids, float texture, string font, string text, float scale, float angle, float shift, TextDirection direction, Vector2 spacing, TextAlignment alignment, float offset) : base(color, default, cracks, voids, texture)
        {
            Text = new PropertyStringValue("TX", StyleChanged, text);
            Font = new PropertyStringValue("F", StyleChanged, font);
            Scale = new PropertyStructValue<float>("S", StyleChanged, scale);
            Angle = new PropertyStructValue<float>("A", StyleChanged, angle);
            Shift = new PropertyStructValue<float>("SF", StyleChanged, shift);
            Direction = new PropertyEnumValue<TextDirection>("V", StyleChanged, direction);
            Spacing = new PropertyVector2Value(StyleChanged, spacing, "SPC", "SPL");
            Alignment = new PropertyEnumValue<TextAlignment>("AL", StyleChanged, alignment);
            Offset = GetOffsetProperty(offset);
#if DEBUG
            Ratio = new PropertyStructValue<float>(StyleChanged, 0.025f);
#endif
        }
        ~RegularLineStyleText()
        {
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
                RemoveTexture(PrevTextureId[lod]);
        }
        protected override void StyleChanged()
        {
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
            {
                RemoveTexture(PrevTextureId[lod]);
                PrevTextureId[lod] = default;
            }
            base.StyleChanged();
        }
        private static void RemoveTexture(TextureId textureId)
        {
            if (textureId.IsDefault)
                return;

            lock (TextTextures)
            {
                if (TextTextureCount.TryGetValue(textureId, out var count))
                {
                    count -= 1;
#if DEBUG
                    var destroyed = false;
#endif
                    if (count <= 0)
                    {
                        TextTextureCount.Remove(textureId);
                        if (TextTextures.TryGetValue(textureId, out var textureData))
                        {
                            if (textureData.texture != null)
                            {
                                Object.Destroy(textureData.texture);
#if DEBUG
                                destroyed = true;
#endif
                            }

                            TextTextures.Remove(textureId);
                        }
                    }
                    else
                        TextTextureCount[textureId] = count;
#if DEBUG
                    SingletonMod<Mod>.Logger.Debug($"Removed({destroyed}) ({count}) {textureId}");
#endif
                }
            }
        }
        private static void AddTexture(TextureId textureId)
        {
            if (textureId.IsDefault)
                return;

            lock (TextTextures)
            {
                if (TextTextureCount.TryGetValue(textureId, out var count))
                    count += 1;
                else
                    count = 1;

                TextTextureCount[textureId] = count;
#if DEBUG
                SingletonMod<Mod>.Logger.Debug($"Added ({count}) {textureId}");
#endif
            }
        }

        public override RegularLineStyle CopyLineStyle() => new RegularLineStyleText(Color, Cracks, Voids, Texture, Font, Text, Scale, Angle, Shift, Direction, Spacing, Alignment, Offset);

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            var text = Text.Value;
            if (Direction == TextDirection.TopToBottom)
                text = string.Join("\n", text.Select(c => c.ToString()).ToArray());
            else if (Direction == TextDirection.BottomToTop)
                text = string.Join("\n", text.Reverse().Select(c => c.ToString()).ToArray());

            var textureId = new TextureId(Font, text, lod == MarkingLOD.LOD0 ? Scale * 2f : Scale * 0.4f, Spacing);
            if (!TextTextures.TryGetValue(textureId, out var textureData))
            {
                var textTexture = RenderHelper.CreateTextTexture(textureId.font, textureId.text, textureId.scale, textureId.spacing, out var textWidth, out var textHeight);
                textTexture.name = textureId.ToString();
                textureData = new TextureData(textTexture, textWidth, textHeight);
                TextTextures[textureId] = textureData;
            }

            var prevTextureId = PrevTextureId[lod];
            if (!TextureComparer.Instance.Equals(textureId, prevTextureId))
            {
                RemoveTexture(prevTextureId);
                AddTexture(textureId);
                PrevTextureId[lod] = textureId;
            }

            var ratio = lod == MarkingLOD.LOD0 ? Ratio : Ratio * 5f;
            var offset = Offset + 0.5f * (textureData.width * ratio * Mathf.Abs(Mathf.Sin(Mathf.Deg2Rad * Angle)) + textureData.height * ratio * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * Angle)));

            var t = Alignment.Value switch
            {
                TextAlignment.Middle => trajectory.Travel(0.5f, Offset),
                TextAlignment.Start when line.Marking.Type == MarkingType.Node => trajectory.Length >= offset ? trajectory.Travel(offset) : 0.5f,
                TextAlignment.Start when line.Marking.Type == MarkingType.Segment => trajectory.Length >= offset ? 1f - trajectory.Invert().Travel(offset) : 0.5f,
                TextAlignment.End when line.Marking.Type == MarkingType.Node => trajectory.Length >= offset ? 1f - trajectory.Invert().Travel(offset) : 0.5f,
                TextAlignment.End when line.Marking.Type == MarkingType.Segment => trajectory.Length >= offset ? trajectory.Travel(offset) : 0.5f,
                _ => 0.5f,
            };

            t = Mathf.Clamp01(t);
            var direction = line.Trajectory.Tangent(t);
            var position = line.Trajectory.Position(t) + direction.MakeFlatNormalized().Turn90(true) * Shift;
            var angle = direction.AbsoluteAngle() + (Angle.Value + (line.Marking.Type == MarkingType.Node ? -90 : 90)) * Mathf.Deg2Rad;
            var width = textureData.texture.width * ratio;
            var height = textureData.texture.height * ratio;

            var data = new DecalData(MaterialType.Text, lod, position, angle, width, height, Color, new DecalData.TextureData(null, textureData.texture), new DecalData.EffectData(this));
            addData(data);
        }


        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<FontPropertyPanel>(this, nameof(Font), MainCategory, AddFontProperty));
            provider.AddProperty(new PropertyInfo<MultilineTextProperty>(this, nameof(Text), MainCategory, AddTextProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Scale), MainCategory, AddScaleProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Angle), AdditionalCategory, AddAngleProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Shift), AdditionalCategory, AddShiftProperty));
            provider.AddProperty(new PropertyInfo<TextDirectionPanel>(this, nameof(Direction), AdditionalCategory, AddDirectionProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Spacing), MainCategory, AddSpacingProperty));
            provider.AddProperty(new PropertyInfo<TextAlignmentPanel>(this, nameof(Alignment), AdditionalCategory, AddAlignmentProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), AdditionalCategory, AddOffsetProperty, RefreshOffsetProperty));
#if DEBUG
            if (!provider.isTemplate && Settings.ShowDebugProperties)
            {
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Ratio), DebugCategory, AddRatioProperty));
            }
#endif
        }

        protected void AddFontProperty(FontPropertyPanel fontProperty, EditorProvider provider)
        {
            fontProperty.Label = Localize.StyleOption_Font;
            fontProperty.Init();
            fontProperty.Font = string.IsNullOrEmpty(Font.Value) ? string.Empty : Font.Value;
            fontProperty.OnValueChanged += (value) => Font.Value = value;
        }
        protected void AddTextProperty(MultilineTextProperty textProperty, EditorProvider provider)
        {
            textProperty.Label = Localize.StyleOption_Text;
            textProperty.FieldWidth = 150f;
            textProperty.Init();
            textProperty.Text = Text;
            textProperty.OnTextChanged += (value) => Text.Value = value;
        }
        protected void AddScaleProperty(FloatPropertyPanel sizeProperty, EditorProvider provider)
        {
            ;
            sizeProperty.Label = Localize.StyleOption_ObjectScale;
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = 0.1f;
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = 1f;
            sizeProperty.CheckMax = true;
            sizeProperty.MaxValue = 10f;
            sizeProperty.Init();
            sizeProperty.Value = Scale;
            sizeProperty.OnValueChanged += (value) => Scale.Value = value;
        }
        protected void AddAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.Label = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.CyclicalValue = true;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (value) => Angle.Value = value;
        }
        protected void AddShiftProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Label = Localize.StyleOption_ObjectShift;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.CheckMax = true;
            offsetProperty.MinValue = -50;
            offsetProperty.MaxValue = 50;
            offsetProperty.CyclicalValue = false;
            offsetProperty.Init();
            offsetProperty.Value = Shift;
            offsetProperty.OnValueChanged += (value) => Shift.Value = value;
        }
        protected void AddDirectionProperty(TextDirectionPanel directionProperty, EditorProvider provider)
        {
            directionProperty.Label = Localize.StyleOption_TextDirection;
            directionProperty.Selector.AutoButtonSize = false;
            directionProperty.Selector.ButtonWidth = 33f;
            directionProperty.Init();
            directionProperty.SelectedObject = Direction;
            directionProperty.OnSelectObjectChanged += (value) => Direction.Value = value;
        }
        protected void AddAlignmentProperty(TextAlignmentPanel alignmentProperty, EditorProvider provider)
        {
            alignmentProperty.Label = Localize.StyleOption_TextAlignment;
            alignmentProperty.Init();
            alignmentProperty.SelectedObject = Alignment;
            alignmentProperty.OnSelectObjectChanged += (value) =>
            {
                Alignment.Value = value;
                provider.Refresh();
            };
        }
        private new void AddOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Label = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.CheckMax = true;
            offsetProperty.MinValue = -100;
            offsetProperty.MaxValue = 100;
            offsetProperty.CyclicalValue = false;
            offsetProperty.Init();
            offsetProperty.Value = Offset;
            offsetProperty.OnValueChanged += (value) => Offset.Value = value;
        }
        private void RefreshOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.MinValue = Alignment.Value == TextAlignment.Middle ? -50f : 0f;
            offsetProperty.MaxValue = Alignment.Value == TextAlignment.Middle ? 50f : 100f;
            offsetProperty.SimulateEnterValue(Offset);
        }

        protected void AddSpacingProperty(Vector2PropertyPanel spacingProperty, EditorProvider provider)
        {
            spacingProperty.Label = Localize.StyleOption_Spacing;
            spacingProperty.SetLabels(Localize.StyleOption_SpacingChar, Localize.StyleOption_SpacingLine);
            spacingProperty.UseWheel = true;
            spacingProperty.WheelStep = new Vector2(1f, 1f);
            spacingProperty.WheelTip = Settings.ShowToolTip;
            spacingProperty.CheckMin = true;
            spacingProperty.MinValue = new Vector2(-10f, -10f);
            spacingProperty.CheckMax = true;
            spacingProperty.MaxValue = new Vector2(10f, 10f);
            spacingProperty.FieldsWidth = 50f;
            spacingProperty.Init(0, 1);
            spacingProperty.Value = Spacing;
            spacingProperty.OnValueChanged += (value) => Spacing.Value = value;
        }

#if DEBUG
        protected void AddRatioProperty(FloatPropertyPanel sizeProperty, EditorProvider provider)
        {
            sizeProperty.Label = "Pixel ratio";
            sizeProperty.Format = Localize.NumberFormat_Meter;
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = 0.01f;
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = 0.005f;
            sizeProperty.Init();
            sizeProperty.Value = Ratio;
            sizeProperty.OnValueChanged += (value) => Ratio.Value = value;
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
            Alignment.ToXml(config);
            Offset.ToXml(config);
            return config;
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Font.FromXml(config, string.Empty);
            Text.FromXml(config, string.Empty);
            Scale.FromXml(config, DefaultTextScale);
            Angle.FromXml(config, DefaultObjectAngle);
            Shift.FromXml(config, DefaultObjectShift);
            Direction.FromXml(config, TextDirection.LeftToRight);
            Spacing.FromXml(config, Vector2.zero);
            Alignment.FromXml(config, TextAlignment.Middle);
            Offset.FromXml(config, 0f);

            if (invert ^ typeChanged)
            {
                Angle.Value = Angle.Value >= 0 ? Angle.Value - 180f : Angle.Value + 180f;
                Shift.Value = -Shift.Value;
                Alignment.Value = Alignment.Value switch
                {
                    TextAlignment.Start => TextAlignment.End,
                    TextAlignment.End => TextAlignment.Start,
                    _ => TextAlignment.Middle,
                };
            }
        }

        public enum TextDirection
        {
            [Description(nameof(Localize.StyleOption_TextDirectionLtoR))]
            [Sprite(nameof(IMTTextures.LeftToRightButtonIcon))]
            LeftToRight,

            [Description(nameof(Localize.StyleOption_TextDirectionTtoB))]
            [Sprite(nameof(IMTTextures.TopToBottomButtonIcon))]
            TopToBottom,

            [Description(nameof(Localize.StyleOption_TextDirectionBtoT))]
            [Sprite(nameof(IMTTextures.BottomToTopButtonIcon))]
            BottomToTop,
        }
        public enum TextAlignment
        {
            [Description(nameof(Localize.StyleOption_TextAlignmentStart))]
            Start,

            [Description(nameof(Localize.StyleOption_TextAlignmentMiddle))]
            Middle,

            [Description(nameof(Localize.StyleOption_TextAlignmentEnd))]
            End,
        }

        public class TextDirectionPanel : EnumOncePropertyPanel<TextDirection, TextDirectionPanel.TextDirectionSegmented>
        {
            protected override bool IsEqual(TextDirection first, TextDirection second) => first == second;
            protected override string GetDescription(TextDirection value) => value.Description();

            protected override void FillItems(Func<TextDirection, bool> selector)
            {
                Selector.PauseLayout(() =>
                {
                    foreach (var value in GetValues())
                    {
                        if (selector?.Invoke(value) != false)
                        {
                            var sprite = value.Sprite();
                            if (string.IsNullOrEmpty(sprite))
                                Selector.AddItem(value, new OptionData(GetDescription(value)));
                            else
                                Selector.AddItem(value, new OptionData(GetDescription(value), IMTTextures.Atlas, sprite));
                        }
                    }
                });
            }
            public override void SetStyle(ControlStyle style)
            {
                Selector.SegmentedStyle = style.Segmented;
            }

            public class TextDirectionSegmented : UIOnceSegmented<TextDirection> { }
        }
        public class TextAlignmentPanel : EnumOncePropertyPanel<TextAlignment, TextAlignmentPanel.TextAlignmentSegmented>
        {
            protected override bool IsEqual(TextAlignment first, TextAlignment second) => first == second;
            protected override string GetDescription(TextAlignment value) => value.Description();
            public override void SetStyle(ControlStyle style)
            {
                Selector.SegmentedStyle = style.Segmented;
            }

            public class TextAlignmentSegmented : UIOnceSegmented<TextAlignment> { }
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
        public struct TextureData
        {
            public Texture2D texture;
            public float width;
            public float height;

            public TextureData(Texture2D texture, float width, float height)
            {
                this.texture = texture;
                this.width = width;
                this.height = height;
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
