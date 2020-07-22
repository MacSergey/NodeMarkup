using ColossalFramework.UI;
using NodeMarkup.Manager;
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

        public void Select() => normalBgSprite = "ButtonSmallPressed";
        public void Unselect() => normalBgSprite = "ButtonSmall";
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItem where IconType : UIComponent
    {
        public event Action<EditableItem<EditableObject, IconType>> OnDelete;

        EditableObject _object;
        public abstract string Description { get; }
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
        protected IconType Icon { get; set; }
        private UIButton DeleteButton { get; set; }

        public bool ShowIcon { get; set; }
        public bool ShowDelete { get; set; }

        public EditableItem(bool showIcon, bool showDelete)
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            ShowIcon = showIcon;
            ShowDelete = showDelete;

            if (ShowIcon)
                AddIcon();
            AddLable();
            if (ShowDelete)
                AddDeleteButton();

            atlas = NodeMarkupPanel.InGameAtlas;

            normalBgSprite = "ButtonSmall";
            disabledBgSprite = "ButtonSmallPressed";
            focusedBgSprite = "ButtonSmallPressed";
            hoveredBgSprite = "ButtonSmallHovered";
            pressedBgSprite = "ButtonSmallPressed";


            height = 25;
#if STOPWATCH
            Logger.LogDebug($"{nameof(EditableItem)}.constructor: {sw.ElapsedMilliseconds}ms");
#endif
        }

        private void AddIcon()
        {
            Icon = AddUIComponent<IconType>();
            Icon.isEnabled = ShowIcon;
        }
        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.6f;
            Label.padding = new RectOffset(0, 0, 2, 0);
        }
        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = NodeMarkupPanel.InGameAtlas;
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.isEnabled = ShowDelete;
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);
        protected virtual void OnObjectSet()
        {

        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            var labelWidth = size.x- 3;
            if (ShowIcon)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
                labelWidth -= 25;
            }

            if (ShowDelete)
            {
                DeleteButton.size = new Vector2(size.y - 6, size.y - 6);
                DeleteButton.relativePosition = new Vector2(size.x - (size.y - 3), 3);
                labelWidth -= 25;
            }

            Label.size = new Vector2(labelWidth, size.y);
            Label.relativePosition = new Vector3(ShowIcon ? size.y : 3, 0);
        }

        public virtual void Refresh()
        {
            Text = Object.ToString();
        }
    }

    public class ColorIcon : UIButton
    {
        private static float Border => 1f;
        protected UIButton InnerCircule { get; set; }
        public Color32 InnerColor { set => InnerCircule.color = value; }
        public Color32 BorderColor { set => color = value; }
        public ColorIcon()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            normalBgSprite = "PieChartWhiteBg";
            disabledBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = Color.white;

            InnerCircule = AddUIComponent<UIButton>();
            InnerCircule.atlas = NodeMarkupPanel.InGameAtlas;
            InnerCircule.normalBgSprite = "PieChartWhiteBg";
            InnerCircule.normalFgSprite = "PieChartWhiteFg";
            InnerCircule.disabledBgSprite = "PieChartWhiteBg";
            InnerCircule.disabledFgSprite = "PieChartWhiteFg";
            InnerCircule.isInteractive = false;
            InnerCircule.relativePosition = new Vector3(Border, Border);
            InnerCircule.color = Color.black;
        }
        protected override void OnSizeChanged()
        {
            if (InnerCircule != null)
            {
                InnerCircule.height = height - (Border * 2);
                InnerCircule.width = width - (Border * 2);
            }
        }
    }
    public class StyleIcon : ColorIcon
    {
        protected static Color32 GetStyleColor(Color32 color)
        {
            var ratio = 255 / (float)Math.Max(Math.Max(color.r, color.g), color.b);
            return new Color32((byte)(color.r * ratio), (byte)(color.g * ratio), (byte)(color.b * ratio), 255);
        }
        protected UIButton Thumbnail { get; set; }

        public Color32 StyleColor { set => Thumbnail.color = GetStyleColor(value); }
        public Style.StyleType Type { set => Thumbnail.normalBgSprite = Thumbnail.normalFgSprite = Editor.SpriteNames[value]; }

        public StyleIcon()
        {
            Thumbnail = AddUIComponent<UIButton>();
            Thumbnail.atlas = Editor.StylesAtlas;
            Thumbnail.relativePosition = new Vector3(0, 0);
            Thumbnail.isInteractive = false;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (Thumbnail != null)
                Thumbnail.size = size;
        }
    }
}
