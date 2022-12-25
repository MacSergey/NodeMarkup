using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class FontPtopertyPanel : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<string, UnityEngine.FontStyle> OnValueChanged;


        public string Font
        {
            get => FontSelector.SelectedObject;
            set
            {
                FontSelector.SelectedObject = value;
                Refresh();
            }
        }
        public UnityEngine.FontStyle FontStyle
        {
            get => FontStyleSelector.SelectedObject;
            set => FontStyleSelector.SelectedObject = value;
        }

        public bool UseWheel
        {
            set => FontSelector.UseWheel = value;
        }
        private float _width = 230f;
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                RefreshWidth();
            }
        }

        private StringDropDown FontSelector { get; }
        private FontStyleSegmented FontStyleSelector { get; }

        public FontPtopertyPanel()
        {
            FontSelector = Content.AddUIComponent<StringDropDown>();
            FontSelector.SetDefaultStyle(new Vector2(100, 20));
            FontSelector.UseWheel = true;

            FontSelector.AddItem(null, NodeMarkup.Localize.StyleOption_DefaultFont);
            var fonts = TextRenderHelper.InstalledFonts;
            foreach(var font in fonts.Where(f => !f.EndsWith("Bold") && !f.EndsWith("Italic")))
            {
                FontSelector.AddItem(font);
            }
            FontSelector.autoListWidth = false;
            FontSelector.OnSelectObjectChanged += FontChanged;
            FontSelector.eventSizeChanged += ItemSizeChanged;

            FontStyleSelector = Content.AddUIComponent<FontStyleSegmented>();
            FontStyleSelector.StopLayout();
            FontStyleSelector.AutoButtonSize = false;
            FontStyleSelector.ButtonWidth = 20f;
            FontStyleSelector.isVisible = false;
            FontStyleSelector.StartLayout();
            FontStyleSelector.OnSelectObjectChanged += FontStyleChanged;
            FontStyleSelector.eventSizeChanged += ItemSizeChanged;

            RefreshWidth();
        }

        private void ItemSizeChanged(ColossalFramework.UI.UIComponent component, Vector2 value) => Refresh();

        private void FontChanged(string font)
        {
            RefreshFontStyle();
            OnValueChanged?.Invoke(font, FontStyleSelector.SelectedObject);
        }
        private void FontStyleChanged(UnityEngine.FontStyle style)
        {
            OnValueChanged?.Invoke(FontSelector.SelectedObject, style);
        }

        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            FontStyleSelector.Clear();
            UseWheel = false;
            _width = 230f;
        }

        private void RefreshFontStyle()
        {
            FontStyleSelector.StopLayout();

            var selectedStyle = FontStyleSelector.SelectedObject;
            FontStyleSelector.Clear();

            var font = FontSelector.SelectedObject;
            if (string.IsNullOrEmpty(font))
            {
                FontStyleSelector.isVisible = false;
            }
            else
            {
                var styles = new HashSet<UnityEngine.FontStyle>();
                foreach (var fontName in TextRenderHelper.InstalledFonts.Where(f => f.StartsWith(font)))
                {
                    if (fontName.EndsWith("Bold Italic"))
                        styles.Add(UnityEngine.FontStyle.BoldAndItalic);
                    else if (fontName.EndsWith("Bold"))
                        styles.Add(UnityEngine.FontStyle.Bold);
                    else if (fontName.EndsWith("Italic"))
                        styles.Add(UnityEngine.FontStyle.Italic);
                    else
                        styles.Add(UnityEngine.FontStyle.Normal);
                }

                if (styles.Count >= 2)
                {
                    FontStyleSelector.isVisible = true;

                    foreach(var style in EnumExtension.GetEnumValues<UnityEngine.FontStyle>())
                    {
                        if(styles.Contains(style))
                        {
                            var label = style switch
                            {
                                UnityEngine.FontStyle.Bold => NodeMarkup.Localize.StyleOption_FontStyleBold,
                                UnityEngine.FontStyle.Italic => NodeMarkup.Localize.StyleOption_FontStyleItalic,
                                UnityEngine.FontStyle.BoldAndItalic => NodeMarkup.Localize.StyleOption_FontStyleBoldItalic,
                                _ => NodeMarkup.Localize.StyleOption_FontStyleRegular,
                            };
                            var sprite = style switch
                            {
                                UnityEngine.FontStyle.Bold => NodeMarkupTextures.BoldButtonIcons,
                                UnityEngine.FontStyle.Italic => NodeMarkupTextures.ItalicButtonIcons,
                                UnityEngine.FontStyle.BoldAndItalic => NodeMarkupTextures.BoldItalicButtonIcons,
                                _ => NodeMarkupTextures.RegularButtonIcons,
                            };

                            FontStyleSelector.AddItem(style, label, NodeMarkupTextures.Atlas, sprite);
                        }

                        if (styles.Contains(selectedStyle))
                            FontStyleSelector.SelectedObject = selectedStyle;
                        else
                            FontStyleSelector.SelectedObject = UnityEngine.FontStyle.Normal;
                    }
                }
                else
                {
                    FontStyleSelector.isVisible = false;
                }
            }

            FontStyleSelector.StartLayout();

            RefreshWidth();
        }

        private void RefreshWidth()
        {
            if (FontStyleSelector.isVisible)
                FontSelector.width = Math.Max(100, Width - FontStyleSelector.width - Content.autoLayoutPadding.horizontal);
            else
                FontSelector.width = Width;

            FontSelector.listWidth = (int)Width;
        }


        public class FontStyleSegmented : UIOnceSegmented<UnityEngine.FontStyle> { }
    }
}
