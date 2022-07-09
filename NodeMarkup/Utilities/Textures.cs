using ColossalFramework.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public static class NodeMarkupTextures
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

        public static string TurnLeftOrderButton => nameof(TurnLeftOrderButton);
        public static string FlipOrderButton => nameof(FlipOrderButton);
        public static string TurnRightOrderButton => nameof(TurnRightOrderButton);
        public static string ApplyOrderButton => nameof(ApplyOrderButton);
        public static string NotApplyOrderButton => nameof(NotApplyOrderButton);
        public static string ResetOrderButton => nameof(ResetOrderButton);

        public static string ListItemBackground { get; } = nameof(ListItemBackground);
        public static string ListItemCollapse { get; } = nameof(ListItemCollapse);
        public static string ListItemExpand { get; } = nameof(ListItemExpand);

        public static string SingleButtonIcons { get; } = nameof(SingleButtonIcons);
        public static string RangeButtonIcons { get; } = nameof(RangeButtonIcons);

        static NodeMarkupTextures()
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

            //OrderButtons
            spriteParams[TurnLeftOrderButton] = new RectOffset();
            spriteParams[FlipOrderButton] = new RectOffset();
            spriteParams[TurnRightOrderButton] = new RectOffset();
            spriteParams[ApplyOrderButton] = new RectOffset();
            spriteParams[NotApplyOrderButton] = new RectOffset();
            spriteParams[ResetOrderButton] = new RectOffset();

            //ButtonIcons
            spriteParams[SingleButtonIcons] = new RectOffset();
            spriteParams[RangeButtonIcons] = new RectOffset();

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

            Atlas = TextureHelper.CreateAtlas(nameof(NodeMarkup), spriteParams);
        }
    }
}
