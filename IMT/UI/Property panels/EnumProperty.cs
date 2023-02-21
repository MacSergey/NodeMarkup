using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;

namespace IMT.UI
{
    public abstract class StylePropertyPanel : EnumOncePropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown>
    {
        public class StyleDropDown : UIDropDown<Style.StyleType> { }
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override IEnumerable<Style.StyleType> GetValues()
        {
            foreach (var value in EnumExtension.GetEnumValues<StyleType>())
                yield return value.ToEnum<Style.StyleType, StyleType>();
        }
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        protected override string GetDescription(Style.StyleType value) => value.Description();
    }
    public class RegularStylePropertyPanel : StylePropertyPanel<RegularLineStyle.RegularLineType> { }
    public class StopStylePropertyPanel : StylePropertyPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkPropertyPanel : StylePropertyPanel<BaseCrosswalkStyle.CrosswalkType> { }
    public class FillerStylePropertyPanel : StylePropertyPanel<BaseFillerStyle.FillerType> { }



    public class MarkingLineListPropertyPanel : ListPropertyPanel<MarkingLine, MarkingLineListPropertyPanel.MarkingLineDropDown>
    {
        protected override bool IsEqual(MarkingLine first, MarkingLine second) => ReferenceEquals(first, second);
        public class MarkingLineDropDown : UIDropDown<MarkingLine> { }
    }
    public class ChevronFromPropertyPanel : EnumOncePropertyPanel<ChevronFillerStyle.From, ChevronFromPropertyPanel.ChevronFromSegmented>
    {
        protected override bool IsEqual(ChevronFillerStyle.From first, ChevronFillerStyle.From second) => first == second;
        public class ChevronFromSegmented : UIOnceSegmented<ChevronFillerStyle.From> { }
        protected override string GetDescription(ChevronFillerStyle.From value) => value.Description();
    }
    public class LineAlignmentPropertyPanel : EnumOncePropertyPanel<Alignment, LineAlignmentPropertyPanel.AlignmentSegmented>
    {
        protected override bool IsEqual(Alignment first, Alignment second) => first == second;
        public class AlignmentSegmented : UIOnceSegmented<Alignment> { }
        protected override string GetDescription(Alignment value) => value.Description();
    }
    public class PropColorPropertyPanel : EnumOncePropertyPanel<PropLineStyle.ColorOptionEnum, PropColorPropertyPanel.PropColorDropDown>
    {
        protected override bool IsEqual(PropLineStyle.ColorOptionEnum first, PropLineStyle.ColorOptionEnum second) => first == second;
        public class PropColorDropDown : UIDropDown<PropLineStyle.ColorOptionEnum> { }
        protected override string GetDescription(PropLineStyle.ColorOptionEnum value) => value.Description();
    }
}
