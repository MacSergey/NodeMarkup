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
            AddTitleDatas();
            AddValueDatas();
        }
        public void Init(IntersectionTemplate template)
        {
            Screenshot.texture = template.HasPreview ? template.Preview : Empty;
            NoScreenshot.isVisible = !template.HasPreview;

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
            NoScreenshot.wordWrap = true;

            NoScreenshot.textScale = 1.2f;
            NoScreenshot.text = NodeMarkup.Localize.PresetInfo_NoScreenshot;

            NoScreenshot.textAlignment = UIHorizontalAlignment.Center;
            NoScreenshot.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private void AddTitles()
        {
            Titles = AddUIComponent<UIPanel>();
            Titles.autoLayoutDirection = LayoutDirection.Vertical;
            Titles.clipChildren = true;

            Titles.eventSizeChanged += (UIComponent component, Vector2 value) => SetPosition();
        }
        private void AddValues()
        {
            Values = AddUIComponent<UIPanel>();
            Values.autoLayoutDirection = LayoutDirection.Vertical;
            Values.clipChildren = true;
            Values.eventSizeChanged += (UIComponent component, Vector2 value) => SetPosition();
        }
        private void AddTitleDatas()
        {
            AddTitleData(NodeMarkup.Localize.PresetInfo_Roads);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Lines);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Crosswalks);
            AddTitleData(NodeMarkup.Localize.PresetInfo_Fillers);
        }
        private void AddValueDatas()
        {
            Roads = AddValueData(string.Empty);
            Lines = AddValueData(string.Empty);
            Crosswalks = AddValueData(string.Empty);
            Fillers = AddValueData(string.Empty);
        }

        private UILabel AddTitleData(string text) => AddData(Titles, Values, text,  UIHorizontalAlignment.Right);
        private UILabel AddValueData(string text) => AddData(Values, Titles, text, UIHorizontalAlignment.Left);

        private UILabel AddData(UIPanel parent, UIPanel other, string text, UIHorizontalAlignment alignment)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.autoSize = false;
            label.autoHeight = false;
            label.textScale = 0.65f;
            label.height = 0;
            label.eventTextChanged += (UIComponent component, string value) => SetSize(parent, other);
            label.padding = new RectOffset(alignment == UIHorizontalAlignment.Left ? 2 : 0, alignment == UIHorizontalAlignment.Right ? 2 : 0, 1, 2);
            label.text = text;
            label.textColor = TextColor;
            label.textAlignment = alignment;
            label.verticalAlignment = UIVerticalAlignment.Bottom;
            return label;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetSize(UIPanel panel, UIPanel other)
        {
            if (panel != null)
            {
                var labels = panel.components.OfType<UILabel>().ToArray();
                var otherLabels = other.components.OfType<UILabel>().ToArray();

                for(var i = 0; i < labels.Length; i+=1)
                {
                    labels[i].autoSize = true;
                    labels[i].autoSize = false;

                    if (i < otherLabels.Length)
                        labels[i].height = Mathf.Max(labels[i].height, otherLabels[i].height);
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
                var space = Mathf.Max(width - Screenshot.width - Titles.width - Values.width, 0f) / 2;
                Values.relativePosition = new Vector2(width - Values.width - space, (height - Values.height) / 2);
                Titles.relativePosition = new Vector2(Values.relativePosition.x - Titles.width, (height - Titles.height) / 2);

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
