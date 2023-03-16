using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI
{
    public class MultilineTextProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<string> OnTextChanged;

        protected override float DefaultHeight => TextPanel.LineCount * 20f + (TextPanel.LineCount - 1) * TextPanel.autoLayoutPadding.vertical + ItemsPadding * 2;
        public MultilineText TextPanel { get; set; }

        public MultilineTextProperty()
        {
            TextPanel = Content.AddUIComponent<MultilineText>();
            TextPanel.OnTextChanged += TextChanged;
        }

        public string Text
        {
            get => TextPanel.Text;
            set => TextPanel.Text = value;
        }
        public float FieldWidth
        {
            get => TextPanel.FieldWidth;
            set => TextPanel.FieldWidth = value;
        }

        public override void DeInit()
        {
            base.DeInit();
            Text = string.Empty;
        }

        private void TextChanged(string text) => OnTextChanged?.Invoke(text);

        public class MultilineText : UIAutoLayoutPanel, IReusable
        {
            bool IReusable.InCache { get; set; }

            public event Action<string> OnTextChanged;

            private MultilineTextItem[] Lines { get; } = new MultilineTextItem[3];
            public int LineCount => Lines.Length;
            private int UsedLines { get; set; } = 0;

            public string Text
            {
                get
                {
                    var text = string.Join("\n", Lines.Take(UsedLines).Select(f => f.Field.Value).ToArray());
                    return text;
                }
                set
                {
                    var lines = value.Split('\n');
                    for (int i = 0; i < Lines.Length; i += 1)
                    {
                        Lines[i].Field.Value = lines.Length > i ? lines[i] : string.Empty;
                    }
                    Refresh();
                }
            }
            public float FieldWidth
            {
                get => Lines.Average(f => f.Field.width);
                set
                {
                    for (int i = 0; i < Lines.Length; i += 1)
                        Lines[i].Field.width = value;
                }
            }

            public MultilineText()
            {
                autoLayoutDirection = LayoutDirection.Vertical;
                autoFitChildrenHorizontally = true;
                autoFitChildrenVertically = true;
                autoLayoutStart = LayoutStart.TopRight;
                autoLayoutPadding = new RectOffset(0, 0, 1, 1);
                StopLayout();
                {
                    for (int i = 0; i < Lines.Length; i += 1)
                    {
                        var line = AddUIComponent<MultilineTextItem>();
                        line.zOrder = 0;

                        line.Field.eventTextChanged += FieldTextChanged;
                        line.Field.OnValueChanged += FieldValueChanged;
                        line.LabelItem.text = string.Format(IMT.Localize.StyleOption_LineNumber, i + 1);

                        Lines[i] = line;
                    }
                }
                StartLayout();

                Refresh();
            }

            public void DeInit()
            {
                for (int i = 0; i < Lines.Length; i += 1)
                    Lines[i].Field.Value = string.Empty;

                Refresh();
            }

            public void Refresh()
            {
                var lines = Lines.Length;
                while (lines > 0)
                {
                    if (string.IsNullOrEmpty(Lines[lines - 1].Field.text))
                        lines -= 1;
                    else
                        break;
                }
                for (var i = 0; i < Lines.Length; i += 1)
                {
                    Lines[i].isEnabled = i <= lines;
                }
                UsedLines = lines;
            }

            private void FieldValueChanged(string text) => OnTextChanged?.Invoke(Text);
            private void FieldTextChanged(UIComponent component, string value) => Refresh();
        }
        public class MultilineTextItem : UIAutoLayoutPanel
        {
            public CustomUILabel LabelItem { get; }
            public StringUITextField Field { get; }

            public MultilineTextItem()
            {
                autoLayoutDirection = LayoutDirection.Horizontal;
                autoFitChildrenHorizontally = true;
                autoFitChildrenVertically = true;
                autoLayoutPadding = new UnityEngine.RectOffset(0, 0, 1, 1);

                StopLayout();
                {
                    LabelItem = AddUIComponent<CustomUILabel>();
                    LabelItem.textScale = 0.7f;
                    LabelItem.padding = new RectOffset(0, 8, 5, 0);

                    Field = AddUIComponent<StringUITextField>();
                    Field.SetDefaultStyle();
                }
                StartLayout();
            }
        }
    }
}
