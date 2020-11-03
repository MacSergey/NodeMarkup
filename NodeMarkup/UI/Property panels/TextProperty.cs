using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class TextProperty : EditorItem, IReusable
    {
        protected static Color32 ErrorColor { get; } = new Color32(253, 77, 60, 255);
        protected static Color32 WarningColorBak { get; } = new Color32(253, 140, 44, 255);

        private UILabel Label { get; set; }
        protected virtual Color32 Color { get; } = UnityEngine.Color.white;

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public TextProperty()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            color = Color;

            autoLayout = true;
            autoFitChildrenVertically = true;

            Label = AddUIComponent<UILabel>();
            Label.textScale = 0.7f;
            Label.autoSize = false;
            Label.autoHeight = true;
            Label.wordWrap = true;
            Label.padding = new RectOffset(5, 5, 5, 5);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Label.width = width;
        }
    }

    public class ErrorTextProperty : TextProperty
    {
        protected override Color32 Color => ErrorColor;
    }
    public class WarningTextProperty : TextProperty
    {
        protected override Color32 Color => WarningColor;
    }
}
