using IMT.Manager;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IMT.UI
{
    public abstract class StylePropertyPanel : EnumSingleDropDownPropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown, StylePropertyPanel.StyleDropDown.StyleDropDownRef>
    {
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
        protected override void ClearSelector()
        {
            base.ClearSelector();
            Selector.ValueGetter = null;
        }

        public class StyleDropDown : EnumDropDown<Style.StyleType, StyleEntity, StylePopup, StyleDropDown.StyleDropDownRef>
        {
            protected override StyleDropDownRef CreateRef() => new(this);

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

            public class StyleDropDownRef : SimpleDropDownRef<Style.StyleType, StyleDropDown>
            {
                public StyleDropDownRef(StyleDropDown dropDown) : base(dropDown) { }
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



    public class LineAlignmentPropertyPanel : EnumSingleSegmentedPropertyPanel<Alignment, LineAlignmentPropertyPanel.AlignmentSegmented, LineAlignmentPropertyPanel.AlignmentSegmented.AlignmentSegmentedRef>
    {
        protected override bool IsEqual(Alignment first, Alignment second) => first == second;

        public class AlignmentSegmented : UIEnumSegmented<Alignment, AlignmentSegmented.AlignmentSegmentedRef> 
        {
            protected override AlignmentSegmentedRef CreateRef() => new(this);

            public class AlignmentSegmentedRef : SingleSegmentedRef<Alignment, AlignmentSegmented>
            {
                public AlignmentSegmentedRef(AlignmentSegmented segmented) : base(segmented) { }
            }
        }
    }
    public class PropColorPropertyPanel : EnumSingleDropDownPropertyPanel<PropLineStyle.ColorOptionEnum, PropColorPropertyPanel.PropColorDropDown, PropColorPropertyPanel.PropColorDropDown.PropColorDropDownRef>
    {
        protected override bool IsEqual(PropLineStyle.ColorOptionEnum first, PropLineStyle.ColorOptionEnum second) => first == second;

        public class PropColorDropDown : EnumDropDown<PropLineStyle.ColorOptionEnum, PropColorEntity, PropColorPopup, PropColorDropDown.PropColorDropDownRef> 
        {
            protected override PropColorDropDownRef CreateRef() => new(this);

            public class PropColorDropDownRef : SimpleDropDownRef<PropLineStyle.ColorOptionEnum, PropColorDropDown>
            {
                public PropColorDropDownRef(PropColorDropDown dropDown) : base(dropDown) { }
            }
        }
        public class PropColorEntity : SimpleEntity<PropLineStyle.ColorOptionEnum> { }
        public class PropColorPopup : SimplePopup<PropLineStyle.ColorOptionEnum, PropColorEntity> { }
    }
}
