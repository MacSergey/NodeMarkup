using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI
{
    public class FontPropertyPanel : EditorPropertyPanel, IReusable
    {
        public event Action<string> OnValueChanged;

        public string Font
        {
            get => FontStyle switch
            {
                FontStyle.Bold => $"{FontFamily} Bold",
                FontStyle.Italic => $"{FontFamily} Italic",
                FontStyle.BoldAndItalic => $"{FontFamily} Bold Italic",
                _ => FontFamily
            };
            set
            {
                if (value.EndsWith("Bold Italic"))
                    UpdateFont(value.Substring(0, value.Length - "Bold Italic".Length).Trim(), FontStyle.BoldAndItalic);
                else if (value.EndsWith("Bold"))
                    UpdateFont(value.Substring(0, value.Length - "Bold".Length).Trim(), FontStyle.Bold);
                else if (value.EndsWith("Italic"))
                    UpdateFont(value.Substring(0, value.Length - "Italic".Length).Trim(), FontStyle.Italic);
                else
                    UpdateFont(value, FontStyle.Normal);
            }
        }
        private FontStyle FontStyle
        {
            get => FontStyleSelector.SelectedObject;
            set => FontStyleSelector.SelectedObject = value;
        }
        private string FontFamily
        {
            get => FontFamilySelector.SelectedObject;
            set => FontFamilySelector.SelectedObject = value;
        }

        private float _width = 230f;
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                FontFamilySelector.PopupWidth = value;
                SetSize();
            }
        }

        private FontDropDown FontFamilySelector { get; set; }
        private FontStyleSegmented FontStyleSelector { get; set; }

        protected override void FillContent()
        {
            FontFamilySelector = Content.AddUIComponent<FontDropDown>();
            FontFamilySelector.name = nameof(FontFamilySelector);
            FontFamilySelector.DropDownDefaultStyle();
            FontFamilySelector.height = 20f;
            FontFamilySelector.OnSelectObject += FontFamilyChanged;

            FontStyleSelector = Content.AddUIComponent<FontStyleSegmented>();
            FontStyleSelector.name = nameof(FontStyleSelector);
            FontStyleSelector.PauseLayout(() =>
            {
                FontStyleSelector.AutoButtonSize = false;
                FontStyleSelector.ButtonWidth = 20f;
                FontStyleSelector.isVisible = false;
            });
            FontStyleSelector.OnSelectObject += FontStyleChanged;
            FontFamilySelector.PopupWidth = Width;
        }
        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            FontFamilySelector.Clear();
            FontStyleSelector.Clear();

            _width = 230f;
        }

        public override void Init() => Init(null);
        public new void Init(float? height)
        {
            base.Init(height);

            FontFamilySelector.Clear();
            FontFamilySelector.AddItem(string.Empty);

            var fonts = TextRenderHelper.InstalledFonts;
            foreach (var font in fonts.Where(f => !f.EndsWith("Bold") && !f.EndsWith("Italic")))
                FontFamilySelector.AddItem(font);
        }

        private void FontFamilyChanged(string fontFamily)
        {
            UpdateFont(fontFamily, FontStyle);
            OnValueChanged?.Invoke(Font);
        }
        private void FontStyleChanged(FontStyle style)
        {
            OnValueChanged?.Invoke(Font);
        }

        private void UpdateFont(string fontFamily, FontStyle fontStyle)
        {
            FontFamily = fontFamily;

            FontStyleSelector.PauseLayout(() =>
            {
                FontStyleSelector.Clear();

                if (string.IsNullOrEmpty(fontFamily))
                {
                    FontStyleSelector.isVisible = false;
                }
                else
                {
                    var styles = new HashSet<FontStyle>();
                    foreach (var fontName in TextRenderHelper.InstalledFonts.Where(f => f.StartsWith(fontFamily)))
                    {
                        if (fontName.EndsWith("Bold Italic"))
                            styles.Add(FontStyle.BoldAndItalic);
                        else if (fontName.EndsWith("Bold"))
                            styles.Add(FontStyle.Bold);
                        else if (fontName.EndsWith("Italic"))
                            styles.Add(FontStyle.Italic);
                        else
                            styles.Add(FontStyle.Normal);
                    }

                    if (styles.Count >= 2)
                    {
                        FontStyleSelector.isVisible = true;

                        foreach (var style in EnumExtension.GetEnumValues<FontStyle>())
                        {
                            if (styles.Contains(style))
                            {
                                var label = style switch
                                {
                                    FontStyle.Bold => IMT.Localize.StyleOption_FontStyleBold,
                                    FontStyle.Italic => IMT.Localize.StyleOption_FontStyleItalic,
                                    FontStyle.BoldAndItalic => IMT.Localize.StyleOption_FontStyleBoldItalic,
                                    _ => IMT.Localize.StyleOption_FontStyleRegular,
                                };
                                var sprite = style switch
                                {
                                    FontStyle.Bold => IMTTextures.BoldButtonIcon,
                                    FontStyle.Italic => IMTTextures.ItalicButtonIcon,
                                    FontStyle.BoldAndItalic => IMTTextures.BoldItalicButtonIcon,
                                    _ => IMTTextures.RegularButtonIcon,
                                };

                                FontStyleSelector.AddItem(style, new OptionData(label, IMTTextures.Atlas, sprite));
                            }

                            if (styles.Contains(fontStyle))
                                FontStyle = fontStyle;
                            else
                                FontStyle = FontStyle.Normal;
                        }
                    }
                    else
                    {
                        FontStyleSelector.isVisible = false;
                    }
                }
            });

            SetSize();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        private void SetSize()
        {
            if (FontFamilySelector != null)
            {
                Content.PauseLayout(() =>
                {
                    if (FontStyleSelector.isVisible)
                        FontFamilySelector.width = Mathf.Max(100f, Width - FontStyleSelector.width - Content.Padding.horizontal);
                    else
                        FontFamilySelector.width = Width;
                });
            }
        }
        public override void SetStyle(ControlStyle style)
        {
            FontFamilySelector.DropDownStyle = style.DropDown;
            FontStyleSelector.SetStyle(style.Segmented);
        }

    }

    public class FontDropDown : SelectItemDropDown<string, FontEntity, FontPopup>
    {
        public float PopupWidth { get; set; }
        protected override Func<string, bool> Selector => null;
        protected override Func<string, string, int> Sorter => (nameA, nameB) => nameA.CompareTo(nameB);

        public FontDropDown() : base()
        {
            Entity.TextScale = 0.7f;
        }

        protected override void SetPopupStyle()
        {
            Popup.PopupDefaultStyle(20f);
            if (DropDownStyle != null)
                Popup.PopupStyle = DropDownStyle;
        }
        protected override void InitPopup()
        {
            Popup.MaximumSize = new Vector2(PopupWidth, 700f);
            Popup.width = PopupWidth;
            Popup.MaxVisibleItems = 25;
            base.InitPopup();
        }
    }
    public class FontStyleSegmented : UIOnceSegmented<FontStyle> { }

    public class FontPopup : SearchPopup<string, FontEntity>
    {
        protected override string EmptyText => IMT.Localize.AssetPopup_NothingFound;
        protected override string GetName(string value) => value ?? IMT.Localize.StyleOption_DefaultFont;
        protected override void SetEntityStyle(FontEntity entity)
        {
            entity.EntityDefaultStyle<string, FontEntity>();
            if (PopupStyle != null)
                entity.EntityStyle = PopupStyle;
        }
    }
    public class FontEntity : PopupEntity<string>
    {
        private CustomUILabel Label { get; }
        public float TextScale
        {
            get => Label.textScale;
            set => Label.textScale = value;
        }

        public override void SetObject(int index, string font, bool selected)
        {
            base.SetObject(index, font, selected);
            Label.text = string.IsNullOrEmpty(font) ? IMT.Localize.StyleOption_DefaultFont : font;
        }

        public FontEntity()
        {
            Label = AddUIComponent<CustomUILabel>();
            Label.autoSize = false;
            Label.HorizontalAlignment = UIHorizontalAlignment.Left;
            Label.VerticalAlignment = UIVerticalAlignment.Middle;
            Label.Padding = new RectOffset(8, 0, 3, 0);
            Label.textScale = 0.9f;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Label.size = size;
        }
    }
}
