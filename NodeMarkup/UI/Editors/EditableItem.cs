using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditableItem : UIButton
    {
        protected UILabel Label { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItem where IconType : UIComponent
    {
        EditableObject _object;
        public EditableObject Object
        {
            get => _object;
            set
            {
                _object = value;
                Refresh();
                OnObjectSet();
            }
        }
        public IconType Icon { get; }

        public EditableItem()
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            atlas = NodeMarkupPanel.InGameAtlas;

            normalBgSprite = "ButtonSmall";
            disabledBgSprite = "ButtonSmallPressed";
            focusedBgSprite = "ButtonSmallPressed";
            hoveredBgSprite = "ButtonSmallHovered";
            pressedBgSprite = "ButtonSmallPressed";

            Icon = AddUIComponent<IconType>();

            AddLable();

            height = 25;
#if STOPWATCH
            Logger.LogDebug($"{nameof(EditableItem)}.constructor: {sw.ElapsedMilliseconds}ms");
#endif
        }

        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.7f;
        }
        protected virtual void OnObjectSet()
        {

        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (Icon != null)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
            }

            if (Label != null)
            {
                Label.size = new Vector2(size.x - size.y, size.y);
                Label.relativePosition = new Vector3(size.y, 0);
            }
        }
        public virtual void Refresh()
        {
            Text = Object.ToString();
        }
    }

    public class ColorIcon : UIButton
    {
        private UIButton ColorCircule { get; set; }
        public Color32 Color
        {
            get => ColorCircule.color;
            set
            {
                ColorCircule.color = value;
                ColorCircule.disabledColor = value;
            }
        }
        public ColorIcon()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            normalBgSprite = "PieChartWhiteBg";
            disabledBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = UnityEngine.Color.white;

            ColorCircule = AddUIComponent<UIButton>();
            ColorCircule.atlas = NodeMarkupPanel.InGameAtlas;
            ColorCircule.normalBgSprite = "PieChartWhiteBg";
            ColorCircule.normalFgSprite = "PieChartWhiteFg";
            ColorCircule.disabledBgSprite = "PieChartWhiteBg";
            ColorCircule.disabledFgSprite = "PieChartWhiteFg";
            ColorCircule.isInteractive = false;
            ColorCircule.relativePosition = new Vector3(2, 2);
        }
        protected override void OnSizeChanged()
        {
            if (ColorCircule != null)
            {
                ColorCircule.height = height - 4;
                ColorCircule.width = width - 4;
            }
        }
    }
}
