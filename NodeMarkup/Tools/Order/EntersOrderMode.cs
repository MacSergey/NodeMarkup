using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeMarkup.Tools
{
    public abstract class BaseEntersOrderToolMode : BaseOrderToolMode<SourceEnter>
    {
        private GUIButton TurnLeftButton { get; }
        private GUIButton FlipButton { get; }
        private GUIButton TurnRightButton { get; }
        private GUIButton ApplyButton { get; }
        private GUIButton NotApplyButton { get; }
        private GUIButton ResetButton { get; }

        protected override string InfoDrag => Localize.Tool_InfoRoadsDrag;
        protected override string InfoDrop => Localize.Tool_InfoRoadsDrop;

        public BaseEntersOrderToolMode()
        {
            TurnLeftButton = new GUIButton(1, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.TurnLeftOrderButton].region);
            TurnLeftButton.OnClick += TurnLeftClick;

            FlipButton = new GUIButton(2, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.FlipOrderButton].region);
            FlipButton.OnClick += FlipClick;

            TurnRightButton = new GUIButton(3, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.TurnRightOrderButton].region);
            TurnRightButton.OnClick += TurnRightClick;

            ApplyButton = new GUIButton(4, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.ApplyOrderButton].region);
            ApplyButton.OnClick += ApplyClick;

            NotApplyButton = new GUIButton(5, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.NotApplyOrderButton].region);
            NotApplyButton.OnClick += NotApplyClick;

            ResetButton = new GUIButton(6, 6, NodeMarkupTextures.Texture, NodeMarkupTextures.Atlas[NodeMarkupTextures.ResetOrderButton].region);
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
            IsMirror = !IsMirror;

            foreach (var source in Sources)
                source.IsMirror = IsMirror;

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
        private void ApplyClick() => Tool.SetDefaultMode();
        private void NotApplyClick()
        {
            SetBackup();
            ApplyClick();
        }
        private void ResetClick() => Reset(null);

        private void Transform(Func<int, int> func)
        {
            for (var i = 0; i < Sources.Length; i += 1)
            {
                if (Sources[i].Target is Target<SourceEnter> target)
                    Sources[i].Target = Targets[func(target.Num)];
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            Centre = Markup.CenterPosition;
            Radius = Markup.CenterRadius + TargetEnter.Size / 2;

            base.Reset(prevMode);
        }
        protected override Target<SourceEnter>[] GetTargets(IToolMode prevMode) => TargetEnters;
        protected override SourceEnter[] GetSources(IToolMode prevMode) => SourceEnters;

        public override string GetToolInfo()
        {
            if (!IsSelectedSource)
            {
                var mouse = SingletonTool<NodeMarkupTool>.Instance.MousePositionScaled;

                if (TurnLeftButton.CheckHover(mouse))
                    return Localize.Tool_InfoTurnСounterClockwise;
                else if (FlipButton.CheckHover(mouse))
                    return Localize.Tool_InfoInverseOrder;
                else if (TurnRightButton.CheckHover(mouse))
                    return Localize.Tool_InfoTurnClockwise;

                else if (ApplyButton.CheckHover(mouse))
                    return Localize.Tool_InfoPasteApply;
                else if (NotApplyButton.CheckHover(mouse))
                    return Localize.Tool_infoPasteNotApply;
                else if (ResetButton.CheckHover(mouse))
                    return Localize.Tool_InfoPasteReset;
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
                var mouse = SingletonTool<NodeMarkupTool>.Instance.MousePositionScaled;

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
        public override void OnSecondaryMouseClicked() => Exit();
        public override bool OnEscape()
        {
            Exit();
            return true;
        }
        private void Exit()
        {
            var messageBox = MessageBox.Show<ThreeButtonMessageBox>();
            messageBox.CaptionText = EndCaption;
            messageBox.MessageText = EndMessage;
            messageBox.Button1Text = Localize.Tool_Apply;
            messageBox.OnButton1Click = OnApply;
            messageBox.Button2Text = Localize.Tool_NotApply;
            messageBox.OnButton2Click = OnNotApply;
            messageBox.Button3Text = Localize.Tool_Continue;

            bool OnApply()
            {
                ApplyClick();
                return true;
            }
            bool OnNotApply()
            {
                NotApplyClick();
                return true;
            }
        }

        private void SetBackup()
        {
            Markup.Clear();
            Markup.FromXml(SingletonMod<Mod>.Version, Backup, new ObjectsMap());
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

    public class PasteEntersOrderToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.PasteEntersOrder;
        protected override string EndCaption => Localize.Tool_EndPasteOrderCaption;
        protected override string EndMessage => Localize.Tool_EndPasteOrderMessage;
    }
    public class EditEntersOrderToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.EditEntersOrder;
        protected override string EndCaption => Localize.Tool_EndEditOrderCaption;
        protected override string EndMessage => Localize.Tool_EndEditOrderMessage;
    }
    public class ApplyIntersectionTemplateOrderToolMode : BaseEntersOrderToolMode
    {
        public override ToolModeType Type => ToolModeType.ApplyIntersectionTemplateOrder;
        protected override string EndCaption => Localize.Tool_EndApplyPresetOrderCaption;
        protected override string EndMessage => Localize.Tool_EndApplyPresetOrderMessage;
    }
}
