using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class ScreenshotProperty : EditorItem, IReusable
    {
        private static Material Material { get; } = new Material(Shader.Find("UI/Default UI Shader"));
        private UITextureSprite Sprite { get; }
        public Texture2D Texture 
        {
            set
            {
                Sprite.texture = value;
            }
        }

        public ScreenshotProperty()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "ButtonWhite";

            //autoLayout = true;
            //autoFitChildrenVertically = true;
            //clipChildren = true;

            Sprite = AddUIComponent<UITextureSprite>();
            Sprite.material = Material;
            Sprite.relativePosition = new Vector2(10, 10);
        }

        public override void DeInit()
        {
            base.DeInit();
            Texture = null;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            height = width;
            Sprite.size = new Vector2(width - 20, width - 20);
        }
    }
}
