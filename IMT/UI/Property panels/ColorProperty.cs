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
    public class IMTColorPropertyPanel : ColorPropertyPanel<IMTColorPicker, IMTColorPickerPopup>
    {
        private static Color32? Buffer { get; set; }
        //private static Queue<Color32> History = new Queue<Color32>();

        Color32? defaultColor;
        public Color32 DefaultColor
        {
            get => defaultColor ?? Manager.Style.DefaultMarkingColor;
            set => defaultColor = value;
        }

        private CustomUIButton CopyButton { get; set; }
        private CustomUIButton PasteButton { get; set; }

        protected override void FillContent()
        {
            base.FillContent();

            CopyButton = Content.AddUIComponent<CustomUIButton>();
            CopyButton.name = nameof(CopyButton);
            CopyButton.SetDefaultStyle();
            CopyButton.size = new Vector2(20f, 20f);
            CopyButton.FgAtlas = IMTTextures.Atlas;
            CopyButton.AllFgSprites = IMTTextures.CopyButtonIcon;
            CopyButton.tooltip = IMT.Localize.Editor_ColorCopy;
            CopyButton.eventClick += (_, _) => Copy();

            PasteButton = Content.AddUIComponent<CustomUIButton>();
            PasteButton.name = nameof(PasteButton);
            PasteButton.SetDefaultStyle();
            PasteButton.size = new Vector2(20f, 20f);
            PasteButton.FgAtlas = IMTTextures.Atlas;
            PasteButton.AllFgSprites = IMTTextures.PasteButtonIcon;
            PasteButton.tooltip = IMT.Localize.Editor_ColorPaste;
            PasteButton.eventClick += (_, _) => Paste();

            ColorPicker.OnAfterPopupOpen += ColorPickerPopupOpen;
            ColorPicker.OnBeforePopupClose += ColorPickerPopupClose;
        }

        private void ColorPickerPopupOpen(IMTColorPickerPopup popup)
        {
            popup.OnCopy += Copy;
            popup.OnPaste += Paste;
            popup.OnDefault += SetDefault;

            popup.CanPaste = Buffer.HasValue;

            //popup.FillColorHistory(History.ToArray());
        }
        private void ColorPickerPopupClose(IMTColorPickerPopup popup)
        {
            //foreach (var item in History)
            //{
            //    if (Equals(item, popup.SelectedColor))
            //        return;
            //}

            //History.Enqueue(popup.SelectedColor);

            //if (History.Count > 10)
            //    History.Dequeue();
        }

        public void Init(Color32? defaultColor = null)
        {
            this.defaultColor = defaultColor;
            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();
            defaultColor = null;
        }

        private void Copy()
        {
            Buffer = Value;
            if (ColorPicker.Popup != null)
                ColorPicker.Popup.CanPaste = true;
        }
        private void Paste()
        {
            if (Buffer != null)
                ValueChanged(Buffer.Value, true, OnChangedValue);
        }
        private void SetDefault() => ValueChanged(DefaultColor, true, OnChangedValue);

        public override void SetStyle(ControlStyle style)
        {
            base.SetStyle(style);

            CopyButton.ButtonStyle = style.Button;
            CopyButton.FgAtlas = IMTTextures.Atlas;
            CopyButton.AllFgSprites = IMTTextures.CopyButtonIcon;

            PasteButton.ButtonStyle = style.Button;
            PasteButton.FgAtlas = IMTTextures.Atlas;
            PasteButton.AllFgSprites = IMTTextures.PasteButtonIcon;
        }
    }

    public class IMTColorPicker : ColorPickerButton<IMTColorPickerPopup> { }
    public class IMTColorPickerPopup : ColorPickerPopup
    {
        public event Action OnCopy;
        public event Action OnPaste;
        public event Action OnDefault;

        private CustomUIButton CopyButton { get; set; }
        private CustomUIButton PasteButton { get; set; }
        private CustomUIButton DefaultButton { get; set; }
        //private CustomUIPanel SamplesPanel { get; set; }

        public bool CanPaste
        {
            set => PasteButton.isEnabled = value;
        }

        public override ColorPickerStyle ColorPickerStyle
        {
            set
            {
                base.ColorPickerStyle = value;

                CopyButton.ButtonStyle = value.Button;
                PasteButton.ButtonStyle = value.Button;
                DefaultButton.ButtonStyle = value.Button;
            }
        }

        protected override void FillPopup()
        {
            base.FillPopup();

            //SamplesPanel = AddUIComponent<CustomUIPanel>();
            //SamplesPanel.AutoLayout = AutoLayout.Horizontal;
            //SamplesPanel.AutoChildrenHorizontally = AutoLayoutChildren.Fit;
            //SamplesPanel.AutoChildrenVertically = AutoLayoutChildren.Fit;
            //SamplesPanel.AutoLayoutSpace = 8;
            //SamplesPanel.Padding = new RectOffset(10, 10, 10, 10);

            //SamplesPanel.Atlas = CommonTextures.Atlas;
            //SamplesPanel.BackgroundSprite = CommonTextures.PanelBig;
            //SamplesPanel.BgColors = ComponentStyle.DarkPrimaryColor25;

            var buttonPanel = AddUIComponent<CustomUIPanel>();
            buttonPanel.PauseLayout(() =>
            {
                buttonPanel.AutoLayout = AutoLayout.Horizontal;
                buttonPanel.AutoChildrenHorizontally = AutoLayoutChildren.Fit;
                buttonPanel.AutoChildrenVertically = AutoLayoutChildren.Fit;
                buttonPanel.AutoLayoutSpace = 10;

                CopyButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorCopy);
                CopyButton.eventClick += Copy;

                PasteButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorPaste);
                PasteButton.eventClick += Paste;

                DefaultButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorDefault);
                DefaultButton.eventClick += SetDefault;
            });
        }

        public override void DeInit()
        {
            base.DeInit();

            OnCopy = null;
            OnPaste = null;
            OnDefault = null;

            //SamplesPanel.PauseLayout(() =>
            //{
            //    foreach (var item in SamplesPanel.components.ToArray())
            //    {
            //        SamplesPanel.RemoveUIComponent(item);
            //        Destroy(item);
            //    }
            //}, false);
        }

        private CustomUIButton CreateButton(UIComponent parent, string text)
        {
            var button = parent.AddUIComponent<CustomUIButton>();
            button.SetDefaultStyle();
            button.size = new Vector2(70f, 20f);
            button.TextPadding = new RectOffset(0, 0, 5, 0);
            button.textScale = 0.6f;
            button.text = text;
            return button;
        }

        private void Copy(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void Paste(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();
        private void SetDefault(UIComponent component, UIMouseEventParameter eventParam) => OnDefault?.Invoke();


        //public void FillColorHistory(Color32[] colors)
        //{
        //    SamplesPanel.PauseLayout(() =>
        //    {
        //        foreach (var color in colors)
        //        {
        //            var button = SamplesPanel.AddUIComponent<CustomUIButton>();
        //            button.size = new Vector2(20f, 20f);
        //            button.SpritePadding = new RectOffset(2, 2, 2, 2);

        //            button.Atlas = CommonTextures.Atlas;
        //            button.BgSprites = new SpriteSet(default, CommonTextures.Circle, CommonTextures.Circle, default, default);
        //            button.FgSprites = CommonTextures.Circle;

        //            var fgColor = color;
        //            fgColor.a = 255;
        //            button.FgColors = fgColor;
        //            button.eventClick += (_, _) => ColorChanged(color, true, OnValueChanged);
        //        }
        //    });
        //}
    }
}

