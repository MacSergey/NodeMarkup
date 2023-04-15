using ColossalFramework.UI;
using IMT.Manager;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI
{
    public class IntersectionTemplateInfoProperty : BaseEditorPanel, IReusable
    {
        private static UIDynamicFont Font { get; } = GetFont();
        private static UIDynamicFont GetFont()
        {
            var font = Instantiate(UIView.GetAView().defaultFont as UIDynamicFont);
            font.baseline = 24;
            font.lineHeight = 24;
            return font;
        }
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

        private float Size => 200f;
        protected override float DefaultHeight => Size + 10;

        protected virtual Color32 TextColor => Color.white;

        private static Material Material { get; } = new Material(Shader.Find("UI/Default UI Shader"));
        private static Texture2D Empty { get; } = TextureHelper.CreateTexture(400, 400, Color.black);
        private UITextureSprite Screenshot { get; set; }
        private CustomUILabel NoScreenshot { get; set; }
        public CustomUISlicedSprite ScreenshotMask { get; set; }

        private CustomUIPanel Info { get; set; }
        private CustomUILabel Titles { get; set; }
        private CustomUILabel Values { get; set; }

        public IntersectionTemplateInfoProperty() : base()
        {
            PauseLayout(() =>
            {
                AutoChildrenVertically = AutoLayoutChildren.Fit;
                AutoLayoutStart = ModsCommon.UI.LayoutStart.MiddleLeft;
                Padding = new RectOffset(5, 5, 5, 5);

                AddScreenshot();
                AddNoScreenshot();

                Info = AddUIComponent<CustomUIPanel>();
                Info.name = nameof(Info);
                Info.PauseLayout(() =>
                {
                    Info.AutoLayout = AutoLayout.Horizontal;
                    Info.AutoChildrenVertically = AutoLayoutChildren.Fit;
                    Info.AutoLayoutStart = ModsCommon.UI.LayoutStart.MiddleCentre;

                    Titles = AddLabel(UIHorizontalAlignment.Right);
                    Values = AddLabel(UIHorizontalAlignment.Left);
                });
            });
        }
        public void Init(IntersectionTemplate template)
        {
            Screenshot.texture = template.HasPreview ? template.Preview : Empty;
            NoScreenshot.isVisible = !template.HasPreview;

            var titlesText = new List<string>();
            var valuesText = new List<string>();

            titlesText.Add(IMT.Localize.PresetInfo_Roads);
            valuesText.Add(template.Roads.ToString());
            titlesText.Add(IMT.Localize.PresetInfo_Lines);
            valuesText.Add(template.Lines.ToString());
            titlesText.Add(IMT.Localize.PresetInfo_Crosswalks);
            valuesText.Add(template.Crosswalks.ToString());
            titlesText.Add(IMT.Localize.PresetInfo_Fillers);
            valuesText.Add(template.Fillers.ToString());

            for (var i = 0; i < template.Enters.Length; i += 1)
            {
                titlesText.Add(string.Format(IMT.Localize.PresetInfo_RoadPoints, i + 1));
                valuesText.Add(template.Enters[i].PointCount.ToString());
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
            Screenshot.name = nameof(Screenshot);
            Screenshot.isInteractive = false;
            Screenshot.material = Material;
            Screenshot.size = new Vector2(Size, Size);
            Screenshot.relativePosition = new Vector2(ItemsPadding, 5);

            ScreenshotMask = Screenshot.AddUIComponent<CustomUISlicedSprite>();
            ScreenshotMask.name = nameof(ScreenshotMask);
            ScreenshotMask.isInteractive = false;
            ScreenshotMask.size = Screenshot.size;
            ScreenshotMask.relativePosition = Vector3.zero;
        }
        private void AddNoScreenshot()
        {
            NoScreenshot = Screenshot.AddUIComponent<CustomUILabel>();
            NoScreenshot.name = nameof(NoScreenshot);
            NoScreenshot.isInteractive = false;
            NoScreenshot.AutoSize = AutoSize.None;
            NoScreenshot.size = new Vector2(Size, Size);
            NoScreenshot.position = new Vector2(0, 0);
            NoScreenshot.WordWrap = true;

            NoScreenshot.textScale = 1.2f;
            NoScreenshot.text = IMT.Localize.PresetInfo_NoScreenshot;

            NoScreenshot.HorizontalAlignment = UIHorizontalAlignment.Center;
            NoScreenshot.VerticalAlignment = UIVerticalAlignment.Middle;
        }
        private CustomUILabel AddLabel(UIHorizontalAlignment alignment)
        {
            var label = Info.AddUIComponent<CustomUILabel>();
            label.isInteractive = false;
            label.font = Font;
            label.AutoSize = AutoSize.All;
            label.textScale = 0.65f;
            label.Padding = new RectOffset(alignment == UIHorizontalAlignment.Left ? 2 : 0, alignment == UIHorizontalAlignment.Right ? 2 : 0, 1, 2);
            label.textColor = TextColor;
            label.HorizontalAlignment = alignment;
            label.VerticalAlignment = UIVerticalAlignment.Bottom;
            return label;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Info.width = width - Screenshot.width - Padding.horizontal - AutoLayoutSpace;
        }

        public override void SetStyle(ControlStyle style)
        {
            ScreenshotMask.atlas = style.PropertyPanel.BgAtlas;
            ScreenshotMask.spriteName = style.PropertyPanel.MaskSprite;
            ScreenshotMask.color = style.PropertyPanel.BgColors.normal;
        }

        private class CustomUITextureSprite : UITextureSprite
        {
            protected override void OnTooltipEnter(UIMouseEventParameter p) { return; }
            protected override void OnTooltipHover(UIMouseEventParameter p) { return; }
            protected override void OnTooltipLeave(UIMouseEventParameter p) { return; }
        }
    }
}
