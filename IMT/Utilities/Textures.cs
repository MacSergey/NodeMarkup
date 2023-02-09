using ColossalFramework.UI;
using IMT.Manager;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Utilities
{
    public static class IMTTextures
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        public static string ActivationButtonNormal => nameof(ActivationButtonNormal);
        public static string ActivationButtonActive => nameof(ActivationButtonActive);
        public static string ActivationButtonHover => nameof(ActivationButtonHover);
        public static string ActivationButtonIconNormal => nameof(ActivationButtonIconNormal);
        public static string ActivationButtonIconActive => nameof(ActivationButtonIconActive);
        public static string ActivationButtonIconHover => nameof(ActivationButtonIconHover);

        public static string UUIButtonNormal => nameof(UUIButtonNormal);
        public static string UUIButtonHovered => nameof(UUIButtonHovered);
        public static string UUIButtonPressed => nameof(UUIButtonPressed);
        //public static string UUIDisabled => nameof(UUIDisabled);

        public static string AddTemplateHeaderButton => nameof(AddTemplateHeaderButton);
        public static string ApplyTemplateHeaderButton => nameof(ApplyTemplateHeaderButton);
        public static string CopyHeaderButton => nameof(CopyHeaderButton);
        public static string PasteHeaderButton => nameof(PasteHeaderButton);
        public static string DuplicateHeaderButton => nameof(DuplicateHeaderButton);
        public static string SetDefaultHeaderButton => nameof(SetDefaultHeaderButton);
        public static string UnsetDefaultHeaderButton => nameof(UnsetDefaultHeaderButton);
        public static string ApplyHeaderButton => nameof(ApplyHeaderButton);
        public static string ApplyAllHeaderButton => nameof(ApplyAllHeaderButton);
        public static string PackageHeaderButton => nameof(PackageHeaderButton);
        public static string ClearHeaderButton => nameof(ClearHeaderButton);
        public static string EditHeaderButton => nameof(EditHeaderButton);
        public static string SaveHeaderButton => nameof(SaveHeaderButton);
        public static string NotSaveHeaderButton => nameof(NotSaveHeaderButton);
        public static string OffsetHeaderButton => nameof(OffsetHeaderButton);
        public static string EdgeLinesHeaderButton => nameof(EdgeLinesHeaderButton);
        public static string CutHeaderButton => nameof(CutHeaderButton);
        public static string BeetwenIntersectionsHeaderButton => nameof(BeetwenIntersectionsHeaderButton);
        public static string WholeStreetHeaderButton => nameof(WholeStreetHeaderButton);
        public static string LinkHeaderButton => nameof(LinkHeaderButton);
        public static string UnlinkHeaderButton => nameof(UnlinkHeaderButton);
        public static string ResetHeaderButton => nameof(ResetHeaderButton);
        public static string ApplyStyleHeaderButton => nameof(ApplyStyleHeaderButton);
        public static string CopyToAllHeaderButton => nameof(CopyToAllHeaderButton);
        public static string CopyToSameHeaderButton => nameof(CopyToSameHeaderButton);

        public static string TurnLeftOrderButton => nameof(TurnLeftOrderButton);
        public static string FlipOrderButton => nameof(FlipOrderButton);
        public static string TurnRightOrderButton => nameof(TurnRightOrderButton);
        public static string ApplyOrderButton => nameof(ApplyOrderButton);
        public static string NotApplyOrderButton => nameof(NotApplyOrderButton);
        public static string ResetOrderButton => nameof(ResetOrderButton);

        public static string ListItemBackground { get; } = nameof(ListItemBackground);
        public static string ListItemCollapse { get; } = nameof(ListItemCollapse);
        public static string ListItemExpand { get; } = nameof(ListItemExpand);

        public static string AutoButtonIcon { get; } = nameof(AutoButtonIcon);
        public static string SingleButtonIcon { get; } = nameof(SingleButtonIcon);
        public static string RangeButtonIcon { get; } = nameof(RangeButtonIcon);

        public static string RegularButtonIcon { get; } = nameof(RegularButtonIcon);
        public static string BoldButtonIcon { get; } = nameof(BoldButtonIcon);
        public static string ItalicButtonIcon { get; } = nameof(ItalicButtonIcon);
        public static string BoldItalicButtonIcon { get; } = nameof(BoldItalicButtonIcon);

        public static string LeftToRightButtonIcon { get; } = nameof(LeftToRightButtonIcon);
        public static string TopToBottomButtonIcon { get; } = nameof(TopToBottomButtonIcon);
        public static string BottomToTopButtonIcon { get; } = nameof(BottomToTopButtonIcon);


        public static string DynamicFixedButtonIcon { get; } = nameof(DynamicFixedButtonIcon);
        public static string DynamicFreeButtonIcon { get; } = nameof(DynamicFreeButtonIcon);
        public static string FixedFixedButtonIcon { get; } = nameof(FixedFixedButtonIcon);
        public static string FixedFreeButtonIcon { get; } = nameof(FixedFreeButtonIcon);

        public static string RotateButtonIcon { get; } = nameof(RotateButtonIcon);
        public static string CopyButtonIcon { get; } = nameof(CopyButtonIcon);
        public static string PasteButtonIcon { get; } = nameof(PasteButtonIcon);
        public static string AdditionalButtonIcon { get; } = nameof(AdditionalButtonIcon);
        public static string ApplyButtonIcon { get; } = nameof(ApplyButtonIcon);
        public static string ApplyAllButtonIcon { get; } = nameof(ApplyAllButtonIcon);
        public static string CopyToAllButtonIcon { get; } = nameof(CopyToAllButtonIcon);
        public static string CopyToSameButtonIcon { get; } = nameof(CopyToSameButtonIcon);

        public static string ButtonWhiteBorder { get; } = nameof(ButtonWhiteBorder);

        static IMTTextures()
        {
            var spriteParams = new Dictionary<string, RectOffset>();

            //ActivationButton
            spriteParams[ActivationButtonNormal] = new RectOffset();
            spriteParams[ActivationButtonActive] = new RectOffset();
            spriteParams[ActivationButtonHover] = new RectOffset();
            spriteParams[ActivationButtonIconNormal] = new RectOffset();
            spriteParams[ActivationButtonIconActive] = new RectOffset();
            spriteParams[ActivationButtonIconHover] = new RectOffset();

            //UUIButton
            spriteParams[UUIButtonNormal] = new RectOffset();
            spriteParams[UUIButtonHovered] = new RectOffset();
            spriteParams[UUIButtonPressed] = new RectOffset();

            //HeaderButtons
            spriteParams[AddTemplateHeaderButton] = new RectOffset();
            spriteParams[ApplyTemplateHeaderButton] = new RectOffset();
            spriteParams[CopyHeaderButton] = new RectOffset();
            spriteParams[PasteHeaderButton] = new RectOffset();
            spriteParams[DuplicateHeaderButton] = new RectOffset();
            spriteParams[SetDefaultHeaderButton] = new RectOffset();
            spriteParams[UnsetDefaultHeaderButton] = new RectOffset();
            spriteParams[ApplyHeaderButton] = new RectOffset();
            spriteParams[ApplyAllHeaderButton] = new RectOffset();
            spriteParams[PackageHeaderButton] = new RectOffset();
            spriteParams[ClearHeaderButton] = new RectOffset();
            spriteParams[EditHeaderButton] = new RectOffset();
            spriteParams[SaveHeaderButton] = new RectOffset();
            spriteParams[NotSaveHeaderButton] = new RectOffset();
            spriteParams[OffsetHeaderButton] = new RectOffset();
            spriteParams[EdgeLinesHeaderButton] = new RectOffset();
            spriteParams[CutHeaderButton] = new RectOffset();
            spriteParams[BeetwenIntersectionsHeaderButton] = new RectOffset();
            spriteParams[WholeStreetHeaderButton] = new RectOffset();
            spriteParams[LinkHeaderButton] = new RectOffset();
            spriteParams[UnlinkHeaderButton] = new RectOffset();
            spriteParams[ResetHeaderButton] = new RectOffset();
            spriteParams[ApplyStyleHeaderButton] = new RectOffset();
            spriteParams[CopyToSameHeaderButton] = new RectOffset();
            spriteParams[CopyToAllHeaderButton] = new RectOffset();

            //OrderButtons
            spriteParams[TurnLeftOrderButton] = new RectOffset();
            spriteParams[FlipOrderButton] = new RectOffset();
            spriteParams[TurnRightOrderButton] = new RectOffset();
            spriteParams[ApplyOrderButton] = new RectOffset();
            spriteParams[NotApplyOrderButton] = new RectOffset();
            spriteParams[ResetOrderButton] = new RectOffset();
            spriteParams[ButtonWhiteBorder] = new RectOffset(6, 6, 6, 6);

            //ButtonIcons
            spriteParams[AutoButtonIcon] = new RectOffset();
            spriteParams[SingleButtonIcon] = new RectOffset();
            spriteParams[RangeButtonIcon] = new RectOffset();
            spriteParams[RegularButtonIcon] = new RectOffset();
            spriteParams[BoldButtonIcon] = new RectOffset();
            spriteParams[ItalicButtonIcon] = new RectOffset();
            spriteParams[BoldItalicButtonIcon] = new RectOffset();
            spriteParams[LeftToRightButtonIcon] = new RectOffset();
            spriteParams[TopToBottomButtonIcon] = new RectOffset();
            spriteParams[BottomToTopButtonIcon] = new RectOffset();

            spriteParams[DynamicFixedButtonIcon] = new RectOffset();
            spriteParams[DynamicFreeButtonIcon] = new RectOffset();
            spriteParams[FixedFixedButtonIcon] = new RectOffset();
            spriteParams[FixedFreeButtonIcon] = new RectOffset();

            spriteParams[RotateButtonIcon] = new RectOffset();
            spriteParams[CopyButtonIcon] = new RectOffset();
            spriteParams[PasteButtonIcon] = new RectOffset();
            spriteParams[AdditionalButtonIcon] = new RectOffset();
            spriteParams[ApplyButtonIcon] = new RectOffset();
            spriteParams[ApplyAllButtonIcon] = new RectOffset();
            spriteParams[CopyToAllButtonIcon] = new RectOffset();
            spriteParams[CopyToSameButtonIcon] = new RectOffset();

            //ListItem
            spriteParams[ListItemBackground] = new RectOffset(4, 4, 4, 4);
            spriteParams[ListItemCollapse] = new RectOffset();
            spriteParams[ListItemExpand] = new RectOffset();

            foreach (var item in EnumExtension.GetEnumValues<RegularLineStyle.RegularLineType>())
                spriteParams.Add(item.ToEnum<Style.StyleType, RegularLineStyle.RegularLineType>().ToString(), new RectOffset());

            foreach (var item in EnumExtension.GetEnumValues<StopLineStyle.StopLineType>())
                spriteParams.Add(item.ToEnum<Style.StyleType, StopLineStyle.StopLineType>().ToString(), new RectOffset());

            foreach (var item in EnumExtension.GetEnumValues<CrosswalkStyle.CrosswalkType>())
                spriteParams.Add(item.ToEnum<Style.StyleType, CrosswalkStyle.CrosswalkType>().ToString(), new RectOffset());

            foreach (var item in EnumExtension.GetEnumValues<FillerStyle.FillerType>())
                spriteParams.Add(item.ToEnum<Style.StyleType, FillerStyle.FillerType>().ToString(), new RectOffset());

            Atlas = TextureHelper.CreateAtlas(nameof(IMT), spriteParams);
        }
    }
}
