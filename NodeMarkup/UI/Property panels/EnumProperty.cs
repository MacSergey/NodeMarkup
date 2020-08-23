using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Linq;

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
                DropDown.AddItem(value, Utilities.EnumDescription(value));
            }
        }
    }
    public abstract class StylePropertyPanel : EnumPropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown>
    {
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        public class StyleDropDown : CustomUIDropDown<Style.StyleType> { }
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override void FillItems()
        {
            foreach (var value in Enum.GetValues(typeof(StyleType)).Cast<object>().Cast<Style.StyleType>())
            {
                DropDown.AddItem(value, Utilities.EnumDescription(value));
            }
        }
    }
    public class RegularStylePropertyPanel : StylePropertyPanel<RegularLineStyle.RegularLineType> { }
    public class StopStylePropertyPanel : StylePropertyPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkPropertyPanel : StylePropertyPanel<CrosswalkStyle.CrosswalkType> { }
    public class FillerStylePropertyPanel : StylePropertyPanel<FillerStyle.FillerType> { }



    public class MarkupLineListPropertyPanel : ListPropertyPanel<MarkupLine, MarkupLineListPropertyPanel.MarkupLineDropDown>
    {
        protected override bool IsEqual(MarkupLine first, MarkupLine second) => ReferenceEquals(first, second);
        public class MarkupLineDropDown : CustomUIDropDown<MarkupLine> { }
    }
}
