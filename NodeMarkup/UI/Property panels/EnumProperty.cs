using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI
{
    public abstract class StylePropertyPanel : EnumOncePropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown>
    {
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        public class StyleDropDown : UIDropDown<Style.StyleType> { }
        protected override string GetDescription(Style.StyleType value) => value.Description();
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override void FillItems()
        {
            foreach (var value in Enum.GetValues(typeof(StyleType)).Cast<object>().Cast<Style.StyleType>())
            {
                if (value.IsVisible())
                    Selector.AddItem(value, GetDescription(value));
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
        public class MarkupLineDropDown : UIDropDown<MarkupLine> { }
    }
    public class ChevronFromPropertyPanel : EnumOncePropertyPanel<ChevronFillerStyle.From, ChevronFromPropertyPanel.ChevronFromSegmented>
    {
        protected override bool IsEqual(ChevronFillerStyle.From first, ChevronFillerStyle.From second) => first == second;
        public class ChevronFromSegmented : UIOnceSegmented<ChevronFillerStyle.From> { }
        protected override string GetDescription(ChevronFillerStyle.From value) => value.Description();
    }
    public class LineAlignmentPropertyPanel : EnumOncePropertyPanel<LineStyle.StyleAlignment, LineAlignmentPropertyPanel.AlignmentSegmented>
    {
        protected override bool IsEqual(LineStyle.StyleAlignment first, LineStyle.StyleAlignment second) => first == second;
        public class AlignmentSegmented : UIOnceSegmented<LineStyle.StyleAlignment> { }
        protected override string GetDescription(LineStyle.StyleAlignment value) => value.Description();
    }
}
