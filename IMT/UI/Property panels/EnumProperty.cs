using IMT.Manager;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IMT.UI
{
    public abstract class StylePropertyPanel : EnumSingleDropDownPropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown, ISingleDropDown<Style.StyleType>>
    {
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        protected override void ClearSelector()
        {
            base.ClearSelector();
            Selector.ValueGetter = null;
        }

        public class StyleDropDown : EnumDropDown<Style.StyleType, StyleEntity, StylePopup>
        {

            public Func<IEnumerable<Style.StyleType>> ValueGetter { private get; set; }
            protected override IEnumerable<Style.StyleType> GetValues()
            {
                return ValueGetter != null ? ValueGetter() : base.GetValues();
            }

            public static IEnumerable<Style.StyleType> GetStyleValues<StyleType>()
                where StyleType : Enum
            {
                return EnumExtension.GetEnumValues<StyleType>().IsVisible().Order().ToEnum<Style.StyleType, StyleType>();
            }
        }
        public class StylePopup : SimplePopup<Style.StyleType, StyleEntity> { }
        public class StyleEntity : SimpleEntity<Style.StyleType> { }
    }

    public class RegularStylePropertyPanel : StylePropertyPanel
    {
        protected override void InitSelector(Func<Style.StyleType, bool> selector)
        {
            Selector.ValueGetter = () => StyleDropDown.GetStyleValues<RegularLineStyle.RegularLineType>();
            base.InitSelector(selector);
        }
    }
    public class StopStylePropertyPanel : StylePropertyPanel
    {
        protected override void InitSelector(Func<Style.StyleType, bool> selector)
        {
            Selector.ValueGetter = () => StyleDropDown.GetStyleValues<StopLineStyle.StopLineType>();
            base.InitSelector(selector);
        }
    }
    public class CrosswalkPropertyPanel : StylePropertyPanel
    {
        protected override void InitSelector(Func<Style.StyleType, bool> selector)
        {
            Selector.ValueGetter = () => StyleDropDown.GetStyleValues<BaseCrosswalkStyle.CrosswalkType>();
            base.InitSelector(selector);
        }
    }
    public class FillerStylePropertyPanel : StylePropertyPanel
    {
        protected override void InitSelector(Func<Style.StyleType, bool> selector)
        {
            Selector.ValueGetter = () => StyleDropDown.GetStyleValues<BaseFillerStyle.FillerType>();
            base.InitSelector(selector);
        }
    }



    public class LineAlignmentPropertyPanel : EnumSingleSegmentedPropertyPanel<Alignment, LineAlignmentPropertyPanel.AlignmentSegmented, ISingleSegmented<Alignment>>
    {
        protected override bool IsEqual(Alignment first, Alignment second) => first == second;

        public class AlignmentSegmented : UIEnumSegmented<Alignment> { }
    }
    public class PropColorPropertyPanel : EnumSingleDropDownPropertyPanel<PropLineStyle.ColorOptionEnum, PropColorPropertyPanel.PropColorDropDown, ISingleDropDown<PropLineStyle.ColorOptionEnum>>
    {
        protected override bool IsEqual(PropLineStyle.ColorOptionEnum first, PropLineStyle.ColorOptionEnum second) => first == second;

        public class PropColorDropDown : EnumDropDown<PropLineStyle.ColorOptionEnum, PropColorEntity, PropColorPopup>, ISingleDropDown<PropLineStyle.ColorOptionEnum> { }
        public class PropColorEntity : SimpleEntity<PropLineStyle.ColorOptionEnum> { }
        public class PropColorPopup : SimplePopup<PropLineStyle.ColorOptionEnum, PropColorEntity> { }
    }
}
