using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BaseEntersOrderToolMode : BaseOrderToolMode<SourceEnter>
    {
        public static UITextureAtlas ButtonAtlas { get; } = GetButtonsIcons();
        private static UITextureAtlas GetButtonsIcons()
        {
            var spriteNames = new string[]
            {
                nameof(TurnLeftButton),
                nameof(FlipButton),
                nameof(TurnRightButton),
                nameof(ApplyButton),
                nameof(NotApplyButton),
                nameof(ResetButton)
            };

            var atlas = TextureUtil.GetAtlas(nameof(BaseEntersOrderToolMode));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("PasteButtons.png", nameof(BaseEntersOrderToolMode), 50, 50, spriteNames, new RectOffset(0, 0, 0, 0));

            return atlas;
        }

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
            TurnLeftButton = new GUIButton(1, 3, 1, 2, ButtonAtlas.texture, ButtonAtlas[nameof(TurnLeftButton)].region);
            TurnLeftButton.OnClick += TurnLeftClick;

            FlipButton = new GUIButton(2, 3, 1, 2, ButtonAtlas.texture, ButtonAtlas[nameof(FlipButton)].region);
            FlipButton.OnClick += FlipClick;

            TurnRightButton = new GUIButton(3, 3, 1, 2, ButtonAtlas.texture, ButtonAtlas[nameof(TurnRightButton)].region);
            TurnRightButton.OnClick += TurnRightClick;

            ApplyButton = new GUIButton(1, 3, 2, 2, ButtonAtlas.texture, ButtonAtlas[nameof(ApplyButton)].region);
            ApplyButton.OnClick += ApplyClick;

            NotApplyButton = new GUIButton(2, 3, 2, 2, ButtonAtlas.texture, ButtonAtlas[nameof(NotApplyButton)].region);
            NotApplyButton.OnClick += NotApplyClick;

            ResetButton = new GUIButton(3, 3, 2, 2, ButtonAtlas.texture, ButtonAtlas[nameof(ResetButton)].region);
            ResetButton.OnClick += ResetClick;
        }
        private void TurnLeftClick()
        {
            Transform((t) => t.NextIndex(Targets.Length));
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
            Transform((t) => t.PrevIndex(Targets.Length));
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

        protected override void Reset(BaseToolMode prevMode)
        {
            UpdateCentreAndRadius();

            base.Reset(prevMode);
        }
        protected override Target<SourceEnter>[] GetTargets(BaseToolMode prevMode) => TargetEnters;
        protected override SourceEnter[] GetSources(BaseToolMode prevMode) => SourceEnters;

        public override string GetToolInfo()
        {
            if (!IsSelectedSource)
            {
                var mouse = GetMouse();

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
                var mouse = GetMouse();

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
        public override void OnSecondaryMouseClicked()
        {
            var messageBox = MessageBoxBase.ShowModal<ThreeButtonMessageBox>();
            messageBox.CaprionText = EndCaption;
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
            Markup.FromXml(Mod.Version, Backup, new ObjectsMap());
        }

        public override void OnGUI(Event e)
        {
            var uiView = UIView.GetAView();
            var screenPos = uiView.WorldPointToGUI(Camera.main, Centre) * uiView.inputScale;

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
        protected override void RenderOverlayAfterBaskets(RenderManager.CameraInfo cameraInfo)
        {
            NodeMarkupTool.RenderCircle(cameraInfo, Colors.White, Centre, Radius * 2);
        }

        private void UpdateCentreAndRadius()
        {
            var points = Markup.Enters.Where(e => e.Position != null).SelectMany(e => new Vector3[] { e.LeftSide, e.RightSide }).ToArray();

            if (points.Length == 0)
            {
                Centre = Markup.Position;
                Radius = Markup.Radius;
                return;
            }

            var centre = Markup.Position;
            var radius = 1000f;

            for (var i = 0; i < points.Length; i += 1)
            {
                for (var j = i + 1; j < points.Length; j += 1)
                {
                    GetCircle2Points(points, i, j, ref centre, ref radius);

                    for (var k = j + 1; k < points.Length; k += 1)
                        GetCircle3Points(points, i, j, k, ref centre, ref radius);
                }
            }

            Centre = centre;
            Radius = radius + TargetEnter.Size / 2;
        }
        private void GetCircle2Points(Vector3[] points, int i, int j, ref Vector3 centre, ref float radius)
        {
            var newCentre = (points[i] + points[j]) / 2;
            var newRadius = (points[i] - points[j]).magnitude / 2;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private void GetCircle3Points(Vector3[] points, int i, int j, int k, ref Vector3 centre, ref float radius)
        {
            var pos1 = (points[i] + points[j]) / 2;
            var pos2 = (points[j] + points[k]) / 2;

            var dir1 = (points[i] - points[j]).Turn90(true).normalized;
            var dir2 = (points[j] - points[k]).Turn90(true).normalized;

            Line2.Intersect(pos1.XZ(), (pos1 + dir1).XZ(), pos2.XZ(), (pos2 + dir2).XZ(), out float p, out _);
            var newCentre = pos1 + dir1 * p;
            var newRadius = (newCentre - points[i]).magnitude;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j, k))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private bool AllPointsInCircle(Vector3[] points, Vector3 centre, float radius, params int[] ignore)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                if (ignore.Any(j => j == i))
                    continue;

                if ((centre - points[i]).magnitude > radius)
                    return false;
            }

            return true;
        }

        private Vector2 GetMouse()
        {
            var uiView = UIView.GetAView();
            return uiView.ScreenPointToGUI(NodeMarkupTool.MousePosition / uiView.inputScale) * uiView.inputScale;
        }
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
}
