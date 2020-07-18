using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : Editor<FillerItem, MarkupFiller, UIPanel>
    {
        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;

        private ButtonPanel AddButton { get; set; }
        private MarkupFiller Filler { get; set; }

        private bool IsSelectFillerMode { get; set; } = false;
        public List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        private IFillerVertex HoverSupportPoint { get; set; }
        private bool IsHoverSupportPoint => HoverSupportPoint != null;

        public FillerEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }
        public override void UpdateEditor()
        {
            base.UpdateEditor();
            AddAddButton();
        }
        private void AddAddButton()
        {
            AddButton = SettingsPanel.AddUIComponent<ButtonPanel>();
            AddButton.Text = "Add Filler";
            AddButton.Init();
            AddButton.OnButtonClick += AddButtonClick;
        }

        private void AddButtonClick()
        {
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                Filler = new MarkupFiller(Markup);
                CalculateSupportPoints();
                IsSelectFillerMode = true;
            }
        }

        public override void OnUpdate()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(NodeMarkupTool.MousePosition);

                foreach (var ыupportPoint in SupportPoints)
                {
                    if (ыupportPoint.IsIntersect(ray))
                    {
                        HoverSupportPoint = ыupportPoint;
                        return;
                    }
                }
            }

            HoverSupportPoint = null;
        }
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverSupportPoint)
            {
                if (HoverSupportPoint.Equals(Filler.First))
                {
                    isDone = true;
                    NodeMarkupPanel.EndEditorAction();
                    return;
                }
                Filler.Add(HoverSupportPoint);
                CalculateSupportPoints();
            }
            isDone = false;
        }
        public override void OnSecondaryMouseClicked(out bool isDone)
        {
            if (Filler.VertexCount == 0)
            {
                NodeMarkupPanel.EndEditorAction();
                isDone = true;
            }
            else
            {
                Filler.Remove();
                CalculateSupportPoints();
                isDone = false;
            }
        }
        private void CalculateSupportPoints()
        {
            SupportPoints.Clear();

            if (Filler.Last is IFillerVertex fillerVertex)
            {
                var fillerVertexes = fillerVertex.Next(Filler.Prev).ToArray();
                SupportPoints.AddRange(fillerVertexes);
            }
            else
            {
                foreach (var intersect in Markup.Intersects)
                {
                    var supportPoint = new IntersectVertexPoint(intersect.Pair);
                    SupportPoints.Add(supportPoint);
                }
                foreach (var enter in Markup.Enters)
                {
                    foreach (var point in enter.Points.Where(p => p.Lines.Any()))
                    {
                        var supportPoint = new EnterVertexPoint(point);
                        SupportPoints.Add(supportPoint);
                    }
                }
            }
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSelectFillerMode)
                return;

            RenderFillerBounds(cameraInfo);
            RenderFillerLines(cameraInfo);
            RenderConnectLine(cameraInfo);
            if (IsHoverSupportPoint)
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverSupportPoint.Position, 1f, -1f, 1280f, false, true);
        }
        private void RenderFillerBounds(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var supportPoint in SupportPoints)
            {
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.red, supportPoint.Position, 0.5f, -1f, 1280f, false, true);
            }
        }
        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = IsHoverSupportPoint && HoverSupportPoint.Equals(Filler.First) ? Color.green : Color.white;
            foreach (var part in Filler.Parts)
            {
                var bezier = part.GetTrajectory();
                NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);

            }
        }
        private void RenderConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Filler.VertexCount == 0)
                return;

            Bezier3 bezier;
            Color color;

            if (IsHoverSupportPoint)
            {
                var linePart = Filler.GetFillerLine(Filler.Last, HoverSupportPoint);
                bezier = linePart.GetTrajectory();

                color = Color.green;
            }
            else
            {
                RaycastInput input = new RaycastInput(NodeMarkupTool.MouseRay, NodeMarkupTool.MouseRayLength);
                NodeMarkupTool.RayCast(input, out RaycastOutput output);

                bezier.a = Filler.Last.Position;
                bezier.b = output.m_hitPos;
                bezier.c = Filler.Last.Position;
                bezier.d = output.m_hitPos;

                color = Color.white;
            }

            NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);
        }

        public override string GetInfo()
        {
            return base.GetInfo();
        }
        public override void EndEditorAction()
        {
            IsSelectFillerMode = false;
        }
    }
    public class FillerItem : EditableItem<MarkupFiller, UIPanel>
    {
        public FillerItem() : base(false, true) { }

        public override string Description => NodeMarkup.Localize.FillerEditor_ItemDescription;
    }
}
