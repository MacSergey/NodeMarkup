using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class IntersectionTemplateInfoProperty : EditorItem, IReusable
    {
        private static UIDynamicFont Font { get; } = GetFont();
        private static UIDynamicFont GetFont()
        {
            var font = Instantiate(UIView.GetAView().defaultFont as UIDynamicFont);
            font.baseline = 24;
            font.lineHeight = 24;
            return font;
        }

        private float Size => 200f;
        protected override float DefaultHeight => Size + 10;

        protected virtual Color32 TextColor => Color.white;

        private static Material Material { get; } = new Material(Shader.Find("UI/Default UI Shader"));
        private static Texture2D Empty { get; } = TextureHelper.CreateTexture(400, 400, Color.black);
        private UITextureSprite Screenshot { get; set; }
        private CustomUILabel NoScreenshot { get; set; }
        private CustomUILabel Titles { get; set; }
        private CustomUILabel Values { get; set; }

        public IntersectionTemplateInfoProperty()
        {
            AddScreenshot();
            AddNoScreenshot();
            Titles = AddLabel(UIHorizontalAlignment.Right);
            Values = AddLabel(UIHorizontalAlignment.Left);
        }
        public void Init(IntersectionTemplate template)
        {
            Screenshot.texture = template.HasPreview ? template.Preview : Empty;
            NoScreenshot.isVisible = !template.HasPreview;

            var titlesText = new List<string>();
            var valuesText = new List<string>();

            titlesText.Add(NodeMarkup.Localize.PresetInfo_Roads);
            valuesText.Add(template.Roads.ToString());
            titlesText.Add(NodeMarkup.Localize.PresetInfo_Lines);
            valuesText.Add(template.Lines.ToString());
            titlesText.Add(NodeMarkup.Localize.PresetInfo_Crosswalks);
            valuesText.Add(template.Crosswalks.ToString());
            titlesText.Add(NodeMarkup.Localize.PresetInfo_Fillers);
            valuesText.Add(template.Fillers.ToString());

            for (var i = 0; i < template.Enters.Length; i += 1)
            {
                titlesText.Add(string.Format(NodeMarkup.Localize.PresetInfo_RoadPoints, i + 1));
                valuesText.Add(template.Enters[i].Points.ToString());
            }

            Titles.text = string.Join("\n", titlesText.ToArray());
            Values.text = string.Join("\n", valuesText.ToArray());

            Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Titles.text = string.Empty;
            Values.text = string.Empty;

            Screenshot.texture = null;
        }

        private void AddScreenshot()
        {
            Screenshot = AddUIComponent<CustomUITextureSprite>();
            Screenshot.material = Material;
            Screenshot.size = new Vector2(Size, Size);
            Screenshot.relativePosition = new Vector2(ItemsPadding, 5);
        }
        private void AddNoScreenshot()
        {
            NoScreenshot = Screenshot.AddUIComponent<CustomUILabel>();
            NoScreenshot.autoSize = false;
            NoScreenshot.size = new Vector2(Size, Size);
            NoScreenshot.position = new Vector2(0, 0);
            NoScreenshot.wordWrap = true;

            NoScreenshot.textScale = 1.2f;
            NoScreenshot.text = NodeMarkup.Localize.PresetInfo_NoScreenshot;

            NoScreenshot.textAlignment = UIHorizontalAlignment.Center;
            NoScreenshot.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private CustomUILabel AddLabel(UIHorizontalAlignment alignment)
        {
            var label = AddUIComponent<CustomUILabel>();
            label.font = Font;
            label.autoSize = true;
            label.textScale = 0.65f;
            label.padding = new RectOffset(alignment == UIHorizontalAlignment.Left ? 2 : 0, alignment == UIHorizontalAlignment.Right ? 2 : 0, 1, 2);
            label.textColor = TextColor;
            label.textAlignment = alignment;
            label.verticalAlignment = UIVerticalAlignment.Bottom;
            label.eventTextChanged += LabelTextChanged;
            return label;
        }

        private void LabelTextChanged(UIComponent component, string value) => SetPosition();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetPosition()
        {
            if (Screenshot != null && Titles != null && Values != null)
            {
                var space = Mathf.Max(width - Screenshot.width - Screenshot.relativePosition.x - Titles.width - Values.width, 0f) / 2;
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
