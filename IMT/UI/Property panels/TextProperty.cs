using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.UI
{
    public class MultilineTextProperty : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<string> OnTextChanged;

        protected override float DefaultHeight => TextPanel.FieldCount * 20f + (TextPanel.FieldCount - 1) * TextPanel.autoLayoutPadding.vertical + 10f;
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

            private StringUITextField[] Fields { get; } = new StringUITextField[3];
            public int FieldCount => Fields.Length;
            private int Lines { get; set; } = 0;

            public string Text
            {
                get
                {
                    var text = string.Join("\n", Fields.Take(Lines).Select(f => f.Value).ToArray());
                    return text;
                }
                set
                {
                    var lines = value.Split('\n');
                    for (int i = 0; i < Fields.Length; i += 1)
                    {
                        Fields[i].Value = lines.Length > i ? lines[i] : string.Empty;
                    }
                    Refresh();
                }
            }
            public float FieldWidth
            {
                get => Fields.Average(f => f.width);
                set
                {
                    for (int i = 0; i < Fields.Length; i += 1)
                        Fields[i].width = value;
                }
            }

            public MultilineText()
            {
                autoLayoutDirection = LayoutDirection.Vertical;
                autoFitChildrenHorizontally = true;
                autoFitChildrenVertically = true;
                autoLayoutPadding = new UnityEngine.RectOffset(0, 0, 1, 1);
                StopLayout();
                {
                    for (int i = 0; i < Fields.Length; i += 1)
                    {
                        var field = AddUIComponent<StringUITextField>();
                        field.SetDefaultStyle();
                        field.eventTextChanged += FieldTextChanged;
                        field.OnValueChanged += FieldValueChanged;
                        Fields[i] = field;
                    }
                }
                StartLayout();

                Refresh();
            }

            public void DeInit()
            {
                for (int i = 0; i < Fields.Length; i += 1)
                    Fields[i].Value = string.Empty;

                Refresh();
            }

            public void Refresh()
            {
                var lines = Fields.Length;
                while (lines > 0)
                {
                    if (string.IsNullOrEmpty(Fields[lines - 1].text))
                        lines -= 1;
                    else
                        break;
                }
                for (var i = 0; i < Fields.Length; i += 1)
                {
                    Fields[i].isEnabled = i <= lines;
                }
                Lines = lines;
            }

            private void FieldValueChanged(string text) => OnTextChanged?.Invoke(Text);
            private void FieldTextChanged(UIComponent component, string value) => Refresh();
        }
    }
}
