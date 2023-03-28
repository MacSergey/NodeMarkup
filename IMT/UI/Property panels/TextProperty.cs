using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using LayoutStart = ModsCommon.UI.LayoutStart;

namespace IMT.UI
{
    public class MultilineTextProperty : EditorPropertyPanel, IReusable
    {
        public event Action<string> OnTextChanged;

        protected override float DefaultHeight => TextPanel.LineCount * 20f + (TextPanel.LineCount - 1) * TextPanel.Padding.vertical + ItemsPadding * 2;
        private MultilineText TextPanel { get; set; }

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

        protected override void FillContent()
        {
            TextPanel = Content.AddUIComponent<MultilineText>();
            TextPanel.name = nameof(TextPanel);
            TextPanel.OnTextChanged += TextChanged;
        }
        public override void DeInit()
        {
            base.DeInit();
            Text = string.Empty;
        }

        private void TextChanged(string text) => OnTextChanged?.Invoke(text);

        public override void SetStyle(ControlStyle style)
        {
            TextPanel.SetStyle(style);
        }

        public class MultilineText : CustomUIPanel, IReusable
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
                PauseLayout(() =>
                {
                    autoLayout = AutoLayout.Vertical;
                    autoChildrenHorizontally = AutoLayoutChildren.Fit;
                    autoChildrenVertically = AutoLayoutChildren.Fit;
                    autoLayoutStart = LayoutStart.TopRight;
                    autoLayoutSpace = 2;

                    for (int i = 0; i < Lines.Length; i += 1)
                    {
                        var line = AddUIComponent<MultilineTextItem>();

                        line.Field.eventTextChanged += FieldTextChanged;
                        line.Field.OnValueChanged += FieldValueChanged;
                        line.LabelItem.text = string.Format(IMT.Localize.StyleOption_LineNumber, i + 1);

                        Lines[i] = line;
                    }
                });

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

            public void SetStyle(ControlStyle style)
            {
                foreach (var line in Lines)
                {
                    line.Field.TextFieldStyle = style.TextField;
                    line.LabelItem.LabelStyle = style.Label;
                }
            }
        }
        public class MultilineTextItem : CustomUIPanel
        {
            public CustomUILabel LabelItem { get; private set; }
            public StringUITextField Field { get; private set; }

            public MultilineTextItem()
            {
                PauseLayout(() => 
                {
                    autoLayout = AutoLayout.Horizontal;
                    autoChildrenHorizontally = AutoLayoutChildren.Fit;
                    autoChildrenVertically = AutoLayoutChildren.Fit;
                    autoLayoutSpace = 2;

                    LabelItem = AddUIComponent<CustomUILabel>();
                    LabelItem.textScale = 0.7f;
                    LabelItem.Padding = new RectOffset(0, 8, 5, 0);

                    Field = AddUIComponent<StringUITextField>();
                    Field.SetDefaultStyle();
                });
            }
        }
    }
}
