using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using IMT.Manager;
using IMT.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class MarkupLineSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge>
    {
        public new event Action<MarkupLineSelectPropertyPanel> OnSelect;
        public new event Action<MarkupLineSelectPropertyPanel> OnHover;
        public new event Action<MarkupLineSelectPropertyPanel> OnLeave;

        protected override string NotSet => IMT.Localize.SelectPanel_NotSet;
        public EdgePosition Position { get; set; }
        protected override float Width => 230f;

        public override void DeInit()
        {
            base.DeInit();

            OnSelect = null;
            OnHover = null;
            OnLeave = null;
        }

        protected override void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(this);
        protected override void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
        protected override void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);

        protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
    }
    public class MarkupCrosswalkSelectPropertyPanel : SelectPropertyPanel<MarkupRegularLine>
    {
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnSelect;
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnHover;
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnLeave;

        protected override string NotSet => IMT.Localize.SelectPanel_NotSet;
        public BorderPosition Position { get; set; }
        protected override float Width => 150f;

        public MarkupCrosswalkSelectPropertyPanel()
        {
            AddReset();
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSelect = null;
            OnHover = null;
            OnLeave = null;
        }

        private void AddReset()
        {
            var button = AddButton(Control);

            button.size = new Vector2(20f, 20f);
            button.text = "×";
            button.tooltip = IMT.Localize.CrosswalkStyle_ResetBorder;
            button.textScale = 1.3f;
            button.textPadding = new RectOffset(0, 0, 0, 0);
            button.eventClick += ResetClick;
        }
        private void ResetClick(UIComponent component, UIMouseEventParameter eventParam) => SelectedObject = null;

        protected override void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(this);
        protected override void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
        protected override void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);

        protected override bool IsEqual(MarkupRegularLine first, MarkupRegularLine second) => ReferenceEquals(first, second);
    }
}
