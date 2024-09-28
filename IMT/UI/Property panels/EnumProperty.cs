using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;

namespace IMT.UI
{
    public abstract class StylePropertyPanel : EnumSinglePropertyPanel<Style.StyleType, StylePropertyPanel.StyleDropDown, StylePropertyPanel.StyleDropDown.StyleDropDownRef>
    {
        public class StyleDropDown : SimpleDropDown<Style.StyleType, StyleEntity, StylePopup, StyleDropDown.StyleDropDownRef> 
        {
            protected override StyleDropDownRef CreateRef() => new(this);

            public class StyleDropDownRef : SimpleDropDownRef<Style.StyleType, StyleDropDown>
            {
                public StyleDropDownRef(StyleDropDown dropDown) : base(dropDown) { }
            }
        }
        public class StylePopup : SimplePopup<Style.StyleType, StyleEntity> { }
        public class StyleEntity : SimpleEntity<Style.StyleType> { }

        public override void SetStyle(ControlStyle style)
        {
            Selector.DropDownStyle = style.DropDown;
        }
    }
    public abstract class StylePropertyPanel<StyleType> : StylePropertyPanel
        where StyleType : Enum
    {
        protected override IEnumerable<Style.StyleType> GetValues()
        {
            foreach (var value in EnumExtension.GetEnumValues<StyleType>().IsVisible().Order())
                yield return value.ToEnum<Style.StyleType, StyleType>();
        }
        protected override bool IsEqual(Style.StyleType first, Style.StyleType second) => first == second;
    }
    public class RegularStylePropertyPanel : StylePropertyPanel<RegularLineStyle.RegularLineType> { }
    public class StopStylePropertyPanel : StylePropertyPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkPropertyPanel : StylePropertyPanel<BaseCrosswalkStyle.CrosswalkType> { }
    public class FillerStylePropertyPanel : StylePropertyPanel<BaseFillerStyle.FillerType> { }


    public class LineAlignmentPropertyPanel : AutoEnumSinglePropertyPanel<Alignment, LineAlignmentPropertyPanel.AlignmentSegmented, LineAlignmentPropertyPanel.AlignmentSegmented.AlignmentSegmentedRef>
    {
        protected override bool IsEqual(Alignment first, Alignment second) => first == second;

        public class AlignmentSegmented : UISingleEnumSegmented<Alignment, AlignmentSegmented.AlignmentSegmentedRef> 
        {
            protected override AlignmentSegmentedRef CreateRef() => new(this);

            public class AlignmentSegmentedRef : SingleSegmentedRef<Alignment, AlignmentSegmented>
            {
                public AlignmentSegmentedRef(AlignmentSegmented segmented) : base(segmented) { }
            }
        }
    }
    public class PropColorPropertyPanel : EnumSinglePropertyPanel<PropLineStyle.ColorOptionEnum, PropColorPropertyPanel.PropColorDropDown, PropColorPropertyPanel.PropColorDropDown.PropColorDropDownRef>
    {
        protected override bool IsEqual(PropLineStyle.ColorOptionEnum first, PropLineStyle.ColorOptionEnum second) => first == second;

        public class PropColorDropDown : SimpleDropDown<PropLineStyle.ColorOptionEnum, PropColorEntity, PropColorPopup, PropColorDropDown.PropColorDropDownRef> 
        {
            protected override PropColorDropDownRef CreateRef() => new(this);

            public class PropColorDropDownRef : SimpleDropDownRef<PropLineStyle.ColorOptionEnum, PropColorDropDown>
            {
                public PropColorDropDownRef(PropColorDropDown dropDown) : base(dropDown) { }
            }
        }
        public class PropColorEntity : SimpleEntity<PropLineStyle.ColorOptionEnum> { }
        public class PropColorPopup : SimplePopup<PropLineStyle.ColorOptionEnum, PropColorEntity> { }
        public override void SetStyle(ControlStyle style)
        {
            Selector.DropDownStyle = style.DropDown;
        }
    }
}
