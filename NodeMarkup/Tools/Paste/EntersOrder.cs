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
    public class EntersOrderToolMode : BaseOrderToolMode<SourceEnter>
    {
        public override ToolModeType Type => ToolModeType.PasteMarkupEnterOrder;
        public override void OnSecondaryMouseClicked() => Tool.SetDefaultMode();

        private GUIButton TurnLeft { get; }
        private GUIButton Flip { get; }
        private GUIButton TurnRight { get; }

        public EntersOrderToolMode()
        {
            TurnLeft = new GUIButton(1, 3, ButtonAtlas.texture, ButtonAtlas[nameof(TurnLeft)].region);
            TurnLeft.OnClick += OnTurnLeft;

            Flip = new GUIButton(2, 3, ButtonAtlas.texture, ButtonAtlas[nameof(Flip)].region);
            Flip.OnClick += OnFlip;

            TurnRight = new GUIButton(3, 3, ButtonAtlas.texture, ButtonAtlas[nameof(TurnRight)].region);
            TurnRight.OnClick += OnTurnRight;
        }
        private void OnTurnLeft()
        {
            Transform((t) => t.NextIndex(Targets.Length));
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
        private void OnFlip()
        {
            IsMirror = !IsMirror;

            foreach (var source in Sources)
                source.IsMirror = IsMirror;

            Transform((t) => Targets.Length - t - 1);
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
        private void OnTurnRight()
        {
            Transform((t) => t.PrevIndex(Targets.Length));
            SetAvailableTargets();
            SetBaskets();
            Paste();
        }
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

        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);

            if (IsHoverSource && HoverSource.Target is TargetEnter)
                Tool.SetMode(ToolModeType.PasteMarkupPointOrder);
            else
            {
                var mouse = GetMouse();

                TurnLeft.CheckClick(mouse);
                Flip.CheckClick(mouse);
                TurnRight.CheckClick(mouse);
            }
        }
        public override string GetToolInfo()
        {
            if (!IsSelectedSource)
            {
                var mouse = GetMouse();

                if (TurnLeft.CheckHover(mouse))
                    return Localize.Tool_InfoTurnСounterClockwise;
                else if (Flip.CheckHover(mouse))
                    return Localize.Tool_InfoChangeOrder;
                else if (TurnRight.CheckHover(mouse))
                    return Localize.Tool_InfoTurnClockwise;
            }

            return base.GetToolInfo();
        }
        public override void OnGUI(Event e)
        {
            var uiView = UIView.GetAView();
            var screenPos = uiView.WorldPointToGUI(Camera.main, Centre) * uiView.inputScale;

            TurnLeft.Update(screenPos);
            Flip.Update(screenPos);
            TurnRight.Update(screenPos);

            TurnLeft.OnGUI(e);
            Flip.OnGUI(e);
            TurnRight.OnGUI(e);

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
}
