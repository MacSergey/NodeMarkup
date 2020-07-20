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

            FillItems();
        }
        protected virtual void FillItems()
        {
            foreach (var value in Enum.GetValues(typeof(EnumType)).OfType<EnumType>())
            {
                DropDown.AddItem(value, GetDescription(value.ToString()));
            }
        }
        protected string GetDescription(string item)
        {
            var description = typeof(EnumType).GetField(item).GetCustomAttributes(typeof(DescriptionAttribute), false).OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? item;
            return NodeMarkup.Localize.ResourceManager.GetString(description, NodeMarkup.Localize.Culture);
        }
    }
    //public class StylePropertyPanel : EnumPropertyPanel<BaseStyle.LineType, StylePropertyPanel.StyleDropDown>
    //{
    //    protected override void FillItems()
    //    {
    //        foreach (var field in typeof(BaseStyle.LineType).GetFields().Skip(1))
    //        {
    //            if(field.GetCustomAttributes(typeof(BaseStyle.SpecialLineAttribute), false).Any())
    //                continue;

    //            var description = (field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description ?? field.Name;
    //            var localizeDescription = NodeMarkup.Localize.ResourceManager.GetString(description, NodeMarkup.Localize.Culture);
    //            var value = (BaseStyle.LineType)field.GetValue(null);
    //            DropDown.AddItem(value, localizeDescription);
    //        }
    //    }
    //    protected override bool IsEqual(BaseStyle.LineType first, BaseStyle.LineType second) => first == second;
    //    public class StyleDropDown : CustomUIDropDown<BaseStyle.LineType> { }
    //}

    public abstract class StylePropertyPanel : EnumPropertyPanel<BaseStyle.LineType, StylePropertyPanel.StyleDropDown>
    {

        protected override bool IsEqual(BaseStyle.LineType first, BaseStyle.LineType second) => first == second;
        public class StyleDropDown : CustomUIDropDown<BaseStyle.LineType> { }
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override void FillItems()
        {
            foreach (var value in Enum.GetValues(typeof(StyleType)).Cast<object>().Cast<BaseStyle.LineType>())
            {
                DropDown.AddItem(value, GetDescription(value.ToString()));
            }
        }
    }
    public class SimpleStylePropertyPanel : StylePropertyPanel<BaseStyle.SimpleLineType> { }
    public class StopStylePropertyPanel : StylePropertyPanel<BaseStyle.StopLineType> { }



    public class MarkupLineListPropertyPanel : ListPropertyPanel<MarkupLine, MarkupLineListPropertyPanel.MarkupLineDropDown>
    {
        protected override bool IsEqual(MarkupLine first, MarkupLine second) => ReferenceEquals(first, second);
        public class MarkupLineDropDown : CustomUIDropDown<MarkupLine> { }
    }
}
