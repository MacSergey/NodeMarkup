using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework.UI;
using System.Reflection.Emit;

namespace NodeMarkup.UI
{
    public class FontPtopertyPanel : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<string> OnValueChanged;


        public string Font
        {
            get => FontStyleSelector.SelectedObject switch
            {
                UnityEngine.FontStyle.Bold => $"{FontFamily} Bold",
                UnityEngine.FontStyle.Italic => $"{FontFamily} Italic",
                UnityEngine.FontStyle.BoldAndItalic => $"{FontFamily} Bold Italic",
                _ => FontFamily
            };
            set
            {
                FontFamily = value;
                Refresh();
                RefreshFontStyle();
            }
        }
        public UnityEngine.FontStyle FontStyle
        {
            get => FontStyleSelector.SelectedObject;
            set => FontStyleSelector.SelectedObject = value;
        }

        private string _fontFamily;
        public string FontFamily 
        {
            get => _fontFamily;
            private set
            {
                _fontFamily = value;
                FontSelector.text = value ?? NodeMarkup.Localize.StyleOption_DefaultFont;
            }
        }

        private float _width = 230f;
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                SetSize();
            }
        }

        private CustomUIButton FontSelector { get; }
        private CustomUIButton Button { get; }

        private FontPopup Popup { get; set; }
        private FontStyleSegmented FontStyleSelector { get; }

        private static IEnumerable<string> AvailableFonts
        {
            get
            {
                yield return null;

                var fonts = TextRenderHelper.InstalledFonts;
                foreach (var font in fonts.Where(f => !f.EndsWith("Bold") && !f.EndsWith("Italic")))
                {
                    yield return font;
                }
            }
        }

        public FontPtopertyPanel()
        {
            FontSelector = Content.AddUIComponent<CustomUIButton>();
            FontSelector.atlas = CommonTextures.Atlas;
            FontSelector.normalBgSprite = CommonTextures.FieldNormal;
            FontSelector.hoveredBgSprite = CommonTextures.FieldHovered;
            FontSelector.disabledBgSprite = CommonTextures.FieldDisabled;
            FontSelector.isInteractive = false;
            FontSelector.enabled = true;
            FontSelector.autoSize = false;
            FontSelector.textHorizontalAlignment = UIHorizontalAlignment.Left;
            FontSelector.textVerticalAlignment = UIVerticalAlignment.Middle;
            FontSelector.height = 20;
            FontSelector.textScale = 0.7f;
            FontSelector.textPadding = new RectOffset(8, 0, 4, 0);
            FontSelector.eventClick += ButtonClick;
            FontSelector.eventSizeChanged += ItemSizeChanged;

            Button = FontSelector.AddUIComponent<CustomUIButton>();
            Button.atlas = TextureHelper.InGameAtlas;
            Button.text = string.Empty;
            Button.size = size;
            Button.relativePosition = new Vector3(0f, 0f);
            Button.textVerticalAlignment = UIVerticalAlignment.Middle;
            Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            Button.normalFgSprite = "IconDownArrow";
            Button.hoveredFgSprite = "IconDownArrowHovered";
            Button.pressedFgSprite = "IconDownArrowPressed";
            Button.focusedFgSprite = "IconDownArrow";
            Button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            Button.horizontalAlignment = UIHorizontalAlignment.Right;
            Button.verticalAlignment = UIVerticalAlignment.Middle;
            Button.textScale = 0.8f;

            FontStyleSelector = Content.AddUIComponent<FontStyleSegmented>();
            FontStyleSelector.StopLayout();
            FontStyleSelector.AutoButtonSize = false;
            FontStyleSelector.ButtonWidth = 20f;
            FontStyleSelector.isVisible = false;
            FontStyleSelector.StartLayout();
            FontStyleSelector.OnSelectObjectChanged += FontStyleChanged;
            FontStyleSelector.eventSizeChanged += ItemSizeChanged;
        }
        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;

            FontStyleSelector.Clear();
            FontFamily = string.Empty;
            _width = 230f;
        }
        public override void Update()
        {
            base.Update();

            if (Input.GetMouseButtonDown(0))
                CheckPopup();
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (Popup == null)
                OpenPopup();
            else
                ClosePopup();
        }
        protected void OpenPopup()
        {
            Button.isInteractive = false;

            var root = GetRootContainer();
            Popup = root.AddUIComponent<FontPopup>();
            Popup.canFocus = true;
            Popup.atlas = CommonTextures.Atlas;
            Popup.backgroundSprite = CommonTextures.FieldHovered;
            Popup.ItemHover = CommonTextures.FieldNormal;
            Popup.ItemSelected = CommonTextures.FieldFocused;
            Popup.EntityHeight = 20f;
            Popup.MaxVisibleItems = 25;
            Popup.maximumSize = new Vector2(230f, 700f);
            Popup.Init(AvailableFonts);
            Popup.Focus();
            Popup.SelectedObject = FontFamily;

            Popup.eventKeyDown += OnPopupKeyDown;
            Popup.OnSelectedChanged += OnFontSelected;

            SetPopupPosition();
            Popup.parent.eventPositionChanged += SetPopupPosition;
        }

        public virtual void ClosePopup()
        {
            Button.isInteractive = true;

            if (Popup != null)
            {
                Popup.eventLeaveFocus -= OnPopupLeaveFocus;
                Popup.eventKeyDown -= OnPopupKeyDown;

                ComponentPool.Free(Popup);
                Popup = null;
            }
        }
        private void CheckPopup()
        {
            if (Popup != null)
            {
                var uiView = Popup.GetUIView();
                var mouse = uiView.ScreenPointToGUI(Input.mousePosition / uiView.inputScale);
                var popupRect = new Rect(Popup.absolutePosition, Popup.size);
                if (!popupRect.Contains(mouse))
                    ClosePopup();
            }
        }
        private void OnFontSelected(string font)
        {
            Font = font;
            OnValueChanged?.Invoke(Font);
            ClosePopup();
        }
        private void OnPopupLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => CheckPopup();
        private void OnPopupKeyDown(UIComponent component, UIKeyEventParameter p)
        {
            if (p.keycode == KeyCode.Escape)
            {
                ClosePopup();
                p.Use();
            }
        }
        private void SetPopupPosition(UIComponent component = null, Vector2 value = default)
        {
            if (Popup != null)
            {
                UIView uiView = Popup.GetUIView();
                var screen = uiView.GetScreenResolution();
                var position = Button.absolutePosition + new Vector3(0, Button.height);
                position.x = MathPos(position.x, Popup.width, screen.x);
                position.y = MathPos(position.y, Popup.height, screen.y);

                Popup.relativePosition = position - Popup.parent.absolutePosition;
            }

            static float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
        }

        private void ItemSizeChanged(UIComponent component, Vector2 value) => Refresh();

        private void FontStyleChanged(UnityEngine.FontStyle style)
        {
            OnValueChanged?.Invoke(Font);
        }

        private void RefreshFontStyle()
        {
            FontStyleSelector.StopLayout();

            var selectedStyle = FontStyleSelector.SelectedObject;
            FontStyleSelector.Clear();

            var font = FontFamily;
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

                    foreach (var style in EnumExtension.GetEnumValues<UnityEngine.FontStyle>())
                    {
                        if (styles.Contains(style))
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

            SetSize();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        private void SetSize()
        {
            if (FontSelector != null)
            {
                if (FontStyleSelector.isVisible)
                    FontSelector.width = Math.Max(100, Width - FontStyleSelector.width - Content.autoLayoutPadding.horizontal);
                else
                    FontSelector.width = Width;

                FontSelector.height = height - 10f;
            }
            if (Button != null)
                Button.size = FontSelector.size;
        }

        public class FontStyleSegmented : UIOnceSegmented<UnityEngine.FontStyle> { }

        public class FontPopup : SearchPopup<string, FontEntity>
        {
            protected override string GetName(string value) => value ?? NodeMarkup.Localize.StyleOption_DefaultFont;
        }
        public class FontEntity : PopupEntity<string>
        {
            private CustomUILabel Label { get; }

            public override string Object 
            { 
                get => Label.text; 
                protected set => Label.text = value ?? NodeMarkup.Localize.StyleOption_DefaultFont; 
            }

            public FontEntity()
            {
                Label = AddUIComponent<CustomUILabel>();
                Label.autoSize = false;
                Label.textAlignment = UIHorizontalAlignment.Left;
                Label.verticalAlignment = UIVerticalAlignment.Middle;
                Label.padding = new RectOffset(5, 0, 3, 0);
                Label.textScale = 0.9f;
            }
            protected override void OnSizeChanged()
            {
                base.OnSizeChanged();
                Label.size = size;
            }
        }
    }
}
