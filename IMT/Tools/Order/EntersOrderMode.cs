using ColossalFramework;
using ColossalFramework.UI;
using IMT.Manager;
using IMT.UI;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Tools
{
    public abstract class BaseEntersOrderToolMode : BaseOrderToolMode<SourceEnter>, IShortcutMode
    {
        public static IntersectionMarkingToolShortcut TurnRightShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(TurnRightShortcut), nameof(TurnRightShortcut), SavedInputKey.Encode(KeyCode.RightArrow, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.TurnRightClick(), ToolModeType.Order);
        public static IntersectionMarkingToolShortcut TurnLeftShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(TurnLeftShortcut), nameof(TurnLeftShortcut), SavedInputKey.Encode(KeyCode.LeftArrow, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.TurnLeftClick(), ToolModeType.Order);
        public static IntersectionMarkingToolShortcut FlipUpShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(FlipUpShortcut), nameof(FlipUpShortcut), SavedInputKey.Encode(KeyCode.UpArrow, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.FlipClick(), ToolModeType.Order);
        public static IntersectionMarkingToolShortcut FlipDownShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(FlipDownShortcut), nameof(FlipDownShortcut), SavedInputKey.Encode(KeyCode.DownArrow, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.FlipClick(), ToolModeType.Order);
        public static IntersectionMarkingToolShortcut ApplyShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ApplyShortcut), nameof(ApplyShortcut), SavedInputKey.Encode(KeyCode.Return, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.ApplyClick(), ToolModeType.Order);
        public static IntersectionMarkingToolShortcut ResetShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ResetShortcut), nameof(ResetShortcut), SavedInputKey.Encode(KeyCode.Backspace, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as BaseEntersOrderToolMode)?.ResetClick(), ToolModeType.Order);

        private GUIButton TurnLeftButton { get; }
        private GUIButton FlipButton { get; }
        private GUIButton TurnRightButton { get; }
        private GUIButton ApplyButton { get; }
        private GUIButton NotApplyButton { get; }
        private GUIButton ResetButton { get; }

        protected override string InfoDrag => Localize.Tool_InfoRoadsDrag;
        protected override string InfoDrop => Localize.Tool_InfoRoadsDrop;
        protected virtual string ApplyButtonText => Localize.Tool_Apply;
        protected virtual string NotApplyButtonText => Localize.Tool_NotApply;
        protected virtual bool AskBeforeApply => false;

        public IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                yield return TurnRightShortcut;
                yield return TurnLeftShortcut;
                yield return FlipUpShortcut;
                yield return FlipDownShortcut;
                yield return ApplyShortcut;
                yield return ResetShortcut;
            }
        }

        public BaseEntersOrderToolMode()
        {
            TurnLeftButton = new GUIButton(1, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.TurnLeftOrderButton].region);
            TurnLeftButton.OnClick += TurnLeftClick;

            FlipButton = new GUIButton(2, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.FlipOrderButton].region);
            FlipButton.OnClick += FlipClick;

            TurnRightButton = new GUIButton(3, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.TurnRightOrderButton].region);
            TurnRightButton.OnClick += TurnRightClick;

            ApplyButton = new GUIButton(4, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.ApplyOrderButton].region);
            ApplyButton.OnClick += ApplyClick;

            NotApplyButton = new GUIButton(5, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.NotApplyOrderButton].region);
            NotApplyButton.OnClick += NotApplyClick;

            ResetButton = new GUIButton(6, 6, IMTTextures.Texture, IMTTextures.Atlas[IMTTextures.ResetOrderButton].region);
            ResetButton.OnClick += ResetClick;
        }
        private void TurnLeftClick()
        {
            Transform((t) => t.PrevIndex(Targets.Length));
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
        private void FlipClick()
        {
            Invert = !Invert;

            foreach (var source in Sources)
                source.Invert = Invert;

            Transform((t) => Targets.Length - t - 1);
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
        private void TurnRightClick()
        {
            Transform((t) => t.NextIndex(Targets.Length));
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
        protected virtual void Exit(bool revert)
        {
            if(revert)
                SetBackup();

            Tool.SetDefaultMode();
        }
        protected void ApplyClick()
        {
            if (AskBeforeApply)
                AskOnExit();
            else
                Exit(false);
        }
        protected void NotApplyClick() => Exit(true);
        private void ResetClick() => Reset(null);

        private void Transform(Func<int, int> func)
        {
            for (var i = 0; i < Sources.Length; i += 1)
            {
                if (Sources[i].Target is Target<SourceEnter> target)
                    Sources[i].Target = Targets[func(target.Index)];
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            Centre = Marking.CenterPosition;
            Radius = Marking.CenterRadius + TargetEnter.Size / 2;

            base.Reset(prevMode);
        }
        protected override Target<SourceEnter>[] GetTargets(IToolMode prevMode) => TargetEnters;
        protected override SourceEnter[] GetSources(IToolMode prevMode) => SourceEnters;

        public override string GetToolInfo()
        {
            if (!IsSelectedSource)
            {
                var mouse = SingletonTool<IntersectionMarkingTool>.Instance.MousePositionScaled;

                if (TurnLeftButton.CheckHover(mouse))
                    return $"{Localize.Tool_InfoTurnСounterClockwise} ({TurnLeftShortcut})";
                else if (FlipButton.CheckHover(mouse))
                    return $"{Localize.Tool_InfoInverseOrder} ({FlipUpShortcut}/{FlipDownShortcut})";
                else if (TurnRightButton.CheckHover(mouse))
                    return $"{Localize.Tool_InfoTurnClockwise} ({TurnRightShortcut})";

                else if (ApplyButton.CheckHover(mouse))
                    return $"{Localize.Tool_InfoPasteApply} ({ApplyShortcut})";
                else if (NotApplyButton.CheckHover(mouse))
                    return Localize.Tool_infoPasteNotApply;
                else if (ResetButton.CheckHover(mouse))
                    return $"{Localize.Tool_InfoPasteReset} ({ResetShortcut})";
            }

            return base.GetToolInfo();
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);

            if (IsHoverSource && HoverSource.Target is TargetEnter)
                Tool.SetMode(ToolModeType.PointsOrder);
            else
            {
                var mouse = SingletonTool<IntersectionMarkingTool>.Instance.MousePositionScaled;

                TurnLeftButton.CheckClick(mouse);
                FlipButton.CheckClick(mouse);
                TurnRightButton.CheckClick(mouse);
                ApplyButton.CheckClick(mouse);
                NotApplyButton.CheckClick(mouse);
                ResetButton.CheckClick(mouse);
            }
        }
        protected abstract string EndCaption { get; }
        protected abstract string EndMessage { get; }

        public override void OnSecondaryMouseClicked() => AskOnExit();
        public override bool OnEscape()
        {
            AskOnExit();
            return true;
        }
        private void AskOnExit()
        {
            var messageBox = MessageBox.Show<ThreeButtonMessageBox>();
            messageBox.CaptionText = EndCaption;
            messageBox.MessageText = EndMessage;
            messageBox.Button1Text = ApplyButtonText;
            messageBox.OnButton1Click = OnApply;
            messageBox.Button2Text = NotApplyButtonText;
            messageBox.OnButton2Click = OnNotApply;
            messageBox.Button3Text = Localize.Tool_Continue;
            messageBox.DefaultButton = 2;

            bool OnApply()
            {
                Exit(false);
                return true;
            }
            bool OnNotApply()
            {
                Exit(true);
                return true;
            }
        }

        private void SetBackup()
        {
            Marking.Clear();
            Marking.FromXml(SingletonMod<Mod>.Version, Backup, new ObjectsMap());
        }

        public override void OnToolGUI(Event e)
        {
            var uiView = UIView.GetAView();
            var position = Centre + Tool.CameraDirection * (Radius + (Baskets.Length == 0 ? 1f : 3f) * TargetEnter.Size);
            var screenPos = uiView.WorldPointToGUI(Camera.main, position) * uiView.inputScale;

            TurnLeftButton.Update(screenPos);
            FlipButton.Update(screenPos);
            TurnRightButton.Update(screenPos);
            ApplyButton.Update(screenPos);
            NotApplyButton.Update(screenPos);
            ResetButton.Update(screenPos);

            TurnLeftButton.OnGUI(e);
            FlipButton.OnGUI(e);
            TurnRightButton.OnGUI(e);
            ApplyButton.OnGUI(e);
            NotApplyButton.OnGUI(e);
            ResetButton.OnGUI(e);

        }
        protected override void RenderOverlayAfterBaskets(RenderManager.CameraInfo cameraInfo) => Centre.RenderCircle(new OverlayData(cameraInfo) { Width = Radius * 2 });

        protected override Target<SourceEnter>[] GetAvailableTargets(SourceEnter source)
        {
            var borders = new EntersBorders(this, source);
            var avalibleTargets = borders.GetTargets(this, Targets).ToArray();
            return avalibleTargets;
        }
        protected override Basket<SourceEnter>[] GetBaskets()
        {
            var sourcesBorders = Sources.Where(s => !(s.Target is TargetEnter)).ToDictionary(s => s, s => new EntersBorders(this, s));
            var baskets = sourcesBorders.GroupBy(b => b.Value, b => b.Key, EntersBorders.Comparer).Select(g => new EntersBasket(this, g.Key, g)).ToArray();
            return baskets;
        }
    }


    public class EditEntersOrderToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.EditEntersOrder;
        protected override string EndCaption => Localize.Tool_EndEditOrderCaption;
        protected override string EndMessage => Localize.Tool_EndEditOrderMessage;
    }
    public class PasteMarkingToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.PasteMarking;
        protected override string EndCaption => Localize.Tool_EndPasteMarkingCaption;
        protected override string EndMessage => Localize.Tool_EndPasteMArkingMessage;
    }
    public class ApplyPresetToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.ApplyPreset;
        protected override string EndCaption => Localize.Tool_EndApplyPresetCaption;
        protected override string EndMessage => Localize.Tool_EndApplyPresetMessage;
    }
    public abstract class BaseApplyPresetToolMode : BaseEntersOrderToolMode 
    {
        protected bool Flip
        {
            get
            {
                var firstId = TargetEnters[0].Enter.Id;
                var secondId = TargetEnters[1].Enter.Id;

                ref var segment = ref Marking.Id.GetSegment();

                var firstIsStart = segment.m_startNode == firstId;
                var secondIsEnd = segment.m_endNode == secondId;
                var segmentInverted = (segment.m_flags & NetSegment.Flags.Invert) != 0;
                var flip = Sources[0].Target != Targets[0];
                flip ^= segmentInverted;
                flip ^= !firstIsStart || !secondIsEnd;

                return flip;
            }
        }
    }
    public class ApplyAllPresetToolMode : BaseApplyPresetToolMode
    {
        public override ToolModeType Type => ToolModeType.ApplyAllPreset;
        protected override string EndCaption => Localize.Tool_EndApplyPresetCaption;
        protected override string EndMessage => string.Format(Localize.Tool_EndApplyAllPresetMessage, Marking.Id.GetSegment().Info.name);
        protected override bool AskBeforeApply => true;

        protected override void Exit(bool revert)
        {
            if (!revert && Marking.Type == MarkingType.Segment)
            {
                var info = Marking.Id.GetSegment().Info;
                var flip = Flip;
                var invert = Invert;
                Tool.ApplyPresetToAsset(info, IntersectionTemplate, flip, invert);
            }
            base.Exit(revert);
        }
    }
    public class LinkPresetToolMode : BaseApplyPresetToolMode
    {
        public string RoadName { get; set; }
        public override ToolModeType Type => ToolModeType.LinkPreset;
        protected override string EndCaption => Localize.Tool_EndLinkPresetCaption;
        protected override string EndMessage => Localize.Tool_EndLinkPresetMessage;
        protected override string ApplyButtonText => Localize.Tool_Link;
        protected override string NotApplyButtonText => Localize.Tool_NotLink;

        protected override void Exit(bool revert)
        {
            if (!revert && !string.IsNullOrEmpty(RoadName) && Marking.Type == MarkingType.Segment)
            {
                var flip = Flip;
                var invert = Invert;
                SingletonManager<RoadTemplateManager>.Instance.SavePreset(RoadName, IntersectionTemplate.Id, flip, invert);
                Panel.UpdatePanel();
            }
            base.Exit(revert);
        }
        public override void Deactivate()
        {
            RoadName = null;
            base.Deactivate();
        }
    }
}
