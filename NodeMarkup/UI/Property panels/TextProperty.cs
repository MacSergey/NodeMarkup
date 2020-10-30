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
        private static Color32 ErrorColor { get; } = new Color32(246, 85, 85, 255);

        private UILabel Label { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public TextProperty()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            color = ErrorColor;

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
}
