using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class PresetInfoProperty : EditorItem, IReusable
    {
        private float Size => 200f;
        protected override float DefaultHeight => Size + 10;

        protected virtual Color32 TextColor => Color.white;

        private static Material Material { get; } = new Material(Shader.Find("UI/Default UI Shader"));
        private static Texture2D Empty { get; } = RenderHelper.CreateTexture(400, 400, Color.black);
        private UITextureSprite Screenshot { get; set; }
        private UILabel NoScreenshot { get; set; }
        private UIPanel Titles { get; set; }
        private UIPanel Values { get; set; }
        private UILabel Roads { get; set; }
        private UILabel Lines { get; set; }
        private UILabel Crosswalks { get; set; }
        private UILabel Fillers { get; set; }
        private List<UILabel> Temp { get; set; } = new List<UILabel>();

        public PresetInfoProperty()
        {
            AddScreenshot();
            AddNoScreenshot();
            AddTitles();
            AddValues();
        }
        public void Init(IntersectionTemplate template)
        {
            Screenshot.texture = template.HasScreenshot ? template.Screenshot : Empty;
            NoScreenshot.isVisible = !template.HasScreenshot;

            Roads.text = template.Roads.ToString();
            Lines.text = template.Lines.ToString();
            Crosswalks.text = template.Crosswalks.ToString();
            Fillers.text = template.Fillers.ToString();

            for(var i = 0; i < template.Enters.Length; i += 1)
            {
                Temp.Add(AddTitleData(string.Format(NodeMarkup.Localize.PresetInfo_RoadPoints, i + 1)));
                Temp.Add(AddValueData( template.Enters[i].Points.ToString()));
            }

            Titles.autoLayout = true;
            Titles.autoLayout = false;
            Titles.FitChildrenVertically();
            Values.autoLayout = true;
            Values.autoLayout = false;
            Values.FitChildrenVertically();

            Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            foreach (var item in Temp)
                ComponentPool.Free(item);

            Temp.Clear();

            Screenshot.texture = null;
        }

        private void AddScreenshot()
        {
            Screenshot = AddUIComponent<CustomUITextureSprite>();
            Screenshot.material = Material;
            Screenshot.size = new Vector2(Size, Size);
            Screenshot.relativePosition = new Vector2(0, 5);
        }
        private void AddNoScreenshot()
        {
            NoScreenshot = Screenshot.AddUIComponent<UILabel>();
            NoScreenshot.autoSize = false;
            NoScreenshot.size = new Vector2(Size, Size);
            NoScreenshot.position = new Vector2(0, 0);

            NoScreenshot.textScale = 1.2f;
            NoScreenshot.text = NodeMarkup.Localize.PresetInfo_NoScreenshot;

            NoScreenshot.atlas = atlas;
            NoScreenshot.backgroundSprite = EmptySprite;
            NoScreenshot.color = Color.black;

            NoScreenshot.textAlignment = UIHorizontalAlignment.Center;
            NoScreenshot.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private void AddTitles()
        {
            Titles = AddUIComponent<UIPanel>();
            Titles.autoLayoutDirection = LayoutDirection.Vertical;
            Titles.clipChildren = true;

            Titles.eventSizeChanged += (UIComponent component, Vector2 value) => SetPosition();

            AddTitleData(NodeMarkup.Localize.PresetInfo_Roads);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Lines);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Crosswalks);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Fillers);
        }
        private void AddValues()
        {
            Values = AddUIComponent<UIPanel>();
            Values.autoLayoutDirection = LayoutDirection.Vertical;
            Values.clipChildren = true;
            Values.eventSizeChanged += (UIComponent component, Vector2 value) => SetPosition();

            Roads = AddValueData(string.Empty);
            Lines = AddValueData(string.Empty);
            Crosswalks = AddValueData(string.Empty);
            Fillers = AddValueData(string.Empty);
        }

        private UILabel AddTitleData(string text) => AddData(Titles, text, UIHorizontalAlignment.Right);
        private UILabel AddValueData(string text) => AddData(Values, text, UIHorizontalAlignment.Left);

        private UILabel AddData(UIPanel parent, string text, UIHorizontalAlignment alignment)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.eventTextChanged += (UIComponent component, string value) => SetSize(parent);
            label.padding = new RectOffset(2, 2, 1, 1);
            label.text = text;
            label.textScale = 0.8f;
            label.textColor = TextColor;
            label.textAlignment = alignment;
            return label;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetSize(UIPanel panel)
        {
            if (panel != null)
            {
                var labels = panel.components.OfType<UILabel>().ToArray();
                foreach (var label in labels)
                {
                    label.autoSize = true;
                    label.autoSize = false;
                }

                var width = labels.Max(l => l.width);

                panel.width = width;
                foreach (var label in labels)
                    label.width = width;
            }
        }
        private void SetPosition()
        {
            if (Screenshot != null && Titles != null && Values != null)
            {
                var space = (width - Screenshot.width - Titles.width - Values.width) / 2;
                Titles.relativePosition = new Vector2(Screenshot.width + space, (height - Titles.height) / 2);
                Values.relativePosition = new Vector2(Titles.relativePosition.x + Titles.width, (height - Values.height) / 2);
            }
        }
        private class CustomUITextureSprite : UITextureSprite
        {
            protected override void OnTooltipEnter(UIMouseEventParameter p) { return; }
            protected override void OnTooltipHover(UIMouseEventParameter p) { return; }
            protected override void OnTooltipLeave(UIMouseEventParameter p) { return; }
        }
    }
}
