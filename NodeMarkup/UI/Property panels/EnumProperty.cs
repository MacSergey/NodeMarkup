using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public abstract class EnumPropertyPanel<EnumType, DropDownType> : ListPropertyPanel<EnumType, DropDownType>
        where EnumType : Enum
        where DropDownType : CustomUIDropDown<EnumType>
    {
        private new bool AllowNull
        {
            set => base.AllowNull = value;
        }
        public override void Init()
        {
            AllowNull = false;
            base.Init();

            foreach (var value in Enum.GetValues(typeof(EnumType)).OfType<EnumType>())
            {
                DropDown.AddItem(value, GetDescription(value.ToString()));
            }
        }
        private string GetDescription(string item)
        {
            var description = typeof(EnumType).GetField(item).GetCustomAttributes(typeof(DescriptionAttribute), false).OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? item;
            return NodeMarkup.Localize.ResourceManager.GetString(description, NodeMarkup.Localize.Culture);
        }
    }
    public class StylePropertyPanel : EnumPropertyPanel<LineStyle.LineType, StylePropertyPanel.StyleDropDown>
    {
        protected override bool IsEqual(LineStyle.LineType first, LineStyle.LineType second) => first == second;
        public class StyleDropDown : CustomUIDropDown<LineStyle.LineType> { }
    }
    public class MarkupLineListPropertyPanel : ListPropertyPanel<MarkupLine, MarkupLineListPropertyPanel.MarkupLineDropDown>
    {
        protected override bool IsEqual(MarkupLine first, MarkupLine second) => System.Object.ReferenceEquals(first, second);
        public class MarkupLineDropDown : CustomUIDropDown<MarkupLine> { }
    }
}
