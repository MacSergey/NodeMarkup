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
        private List<FillerSupportPointBound> FillerBounds { get; } = new List<FillerSupportPointBound>();
        private FillerSupportPointBound HoverFillerBounds { get; set; }
        private bool IsHoverFillerBounds => HoverFillerBounds != null;

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
                CalculateBounds();
                IsSelectFillerMode = true;
            }
        }

        public override void OnUpdate()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(NodeMarkupTool.MousePosition);

                foreach (var fillerBounds in FillerBounds)
                {
                    if (fillerBounds.IsIntersect(ray))
                    {
                        HoverFillerBounds = fillerBounds;
                        return;
                    }
                }
            }

            HoverFillerBounds = null;
        }
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverFillerBounds)
            {
                if (HoverFillerBounds.SupportPoint.Equals(Filler.First))
                {
                    isDone = true;
                    NodeMarkupPanel.EndEditorAction();
                    return;
                }
                Filler.Add(HoverFillerBounds.SupportPoint);
                CalculateBounds();
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
                CalculateBounds();
                isDone = false;
            }
        }
        private void CalculateBounds()
        {
            FillerBounds.Clear();

            if (Filler.Last is IFillerVertex fillerVertex)
            {
                var fillerVertexes = fillerVertex.Next(Filler.Prev).ToArray();
                FillerBounds.AddRange(fillerVertexes.Select(v => new FillerSupportPointBound(v)));
            }
            else
            {
                foreach (var intersect in Markup.Intersects)
                {
                    var supportPoint = new IntersectSupportPoint(intersect.Pair);
                    FillerBounds.Add(new FillerSupportPointBound(supportPoint));
                }
                foreach (var enter in Markup.Enters)
                {
                    foreach (var point in enter.Points)
                    {
                        var supportPoint = new EnterSupportPoint(point);
                        FillerBounds.Add(new FillerSupportPointBound(supportPoint));
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
            if (IsHoverFillerBounds)
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverFillerBounds.Position, 1f, -1f, 1280f, false, true);
        }
        private void RenderFillerBounds(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var fillerBounds in FillerBounds)
            {
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.red, fillerBounds.Position, 0.5f, -1f, 1280f, false, true);
            }
        }
        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = IsHoverFillerBounds && HoverFillerBounds.SupportPoint.Equals(Filler.First) ? Color.green : Color.white;
            var fillerVertexes = Filler.Vertices.ToArray();
            for (var i = 1; i < fillerVertexes.Length; i += 1)
            {
                var bezier = new Bezier3()
                {
                    a = fillerVertexes[i - 1].Position,
                    b = fillerVertexes[i].Position,
                    c = fillerVertexes[i - 1].Position,
                    d = fillerVertexes[i].Position
                };
                NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);
            }
        }
        private void RenderConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Filler.VertexCount == 0)
                return;

            var bezier = new Bezier3();
            Color color;

            if (IsHoverFillerBounds)
            {
                bezier.a = Filler.Last.Position;
                bezier.b = HoverFillerBounds.Position;
                bezier.c = Filler.Last.Position;
                bezier.d = HoverFillerBounds.Position;

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
