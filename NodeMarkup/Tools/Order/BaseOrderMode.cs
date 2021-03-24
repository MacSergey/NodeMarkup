using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BaseOrderToolMode : BaseToolMode
    {
        public override bool ShowPanel => false;
        public Vector3 Centre { get; protected set; }
        public float Radius { get; protected set; }

        public static IntersectionTemplate IntersectionTemplate { get; set; }

        protected XElement Backup { get; set; }

        public bool IsMirror { get; protected set; }
        public SourceEnter[] SourceEnters { get; set; } = new SourceEnter[0];
        public TargetEnter[] TargetEnters { get; set; } = new TargetEnter[0];

        protected override void Reset(BaseToolMode prevMode)
        {
            if (prevMode is BaseOrderToolMode pasteMarkupTool)
            {
                Backup = pasteMarkupTool.Backup;
                IsMirror = pasteMarkupTool.IsMirror;
                SourceEnters = pasteMarkupTool.SourceEnters;
                TargetEnters = pasteMarkupTool.TargetEnters;
            }
            else
            {
                Backup = Markup.ToXml();
                IsMirror = false;
                SourceEnters = IntersectionTemplate.Enters.Select((e, i) => new SourceEnter(e, i)).ToArray();
                TargetEnters = Markup.Enters.Select((e, i) => new TargetEnter(e, i)).ToArray();

                var min = Math.Min(TargetEnters.Length, SourceEnters.Length);
                for (var i = 0; i < min; i += 1)
                    SourceEnters[i].Target = TargetEnters[i];
            }

            Paste();
        }
        protected void Paste()
        {
            Markup.Clear();
            var map = new ObjectsMap(IsMirror);

            foreach (var source in SourceEnters)
            {
                var enterTarget = source.Target as TargetEnter;
                var sourceId = source.Enter.Id;
                var targetId = enterTarget?.Enter.Id ?? 0;
                switch (Markup.Type)
                {
                    case MarkupType.Node:
                        map.AddSegment(sourceId, targetId);
                        break;
                    case MarkupType.Segment:
                        map.AddNode(sourceId, targetId);
                        break;
                }

                if (enterTarget != null)
                {
                    for (var i = 0; i < source.Points.Length; i += 1)
                        map.AddPoint(enterTarget.Enter.Id, (byte)(i + 1), (byte)((source.Points[i].Target as Target)?.Num + 1 ?? 0));
                }
            }

            Markup.FromXml(Mod.Version, IntersectionTemplate.Data, map);
            Panel.UpdatePanel();
        }
    }
    public abstract class BaseOrderToolMode<SourceType> : BaseOrderToolMode
        where SourceType : Source<SourceType>
    {
        public SourceType[] Sources { get; set; } = new SourceType[0];
        public Target<SourceType>[] Targets { get; set; } = new Target<SourceType>[0];

        public SourceType HoverSource { get; protected set; }
        public bool IsHoverSource => HoverSource != null;

        public SourceType SelectedSource { get; protected set; }
        public bool IsSelectedSource => SelectedSource != null;

        public Target<SourceType> HoverTarget { get; protected set; }
        public bool IsHoverTarget => HoverTarget != null;

        public Target<SourceType>[] AvailableTargets { get; protected set; }

        protected Basket<SourceType>[] Baskets { get; set; } = new Basket<SourceType>[0];

        protected override void Reset(BaseToolMode prevMode)
        {
            base.Reset(prevMode);

            HoverSource = null;
            SelectedSource = null;
            HoverTarget = null;

            Targets = GetTargets(prevMode);
            Sources = GetSources(prevMode);

            foreach (var target in Targets)
                target.Update(this);

            SetAvailableTargets();
            SetBaskets();
        }

        protected abstract SourceType[] GetSources(BaseToolMode prevMode);
        protected abstract Target<SourceType>[] GetTargets(BaseToolMode prevMode);

        public void GetHoverSource() => HoverSource = NodeMarkupTool.MouseRayValid ? Sources.FirstOrDefault(s => s.IsHover(NodeMarkupTool.MouseRay)) : null;
        public void GetHoverTarget() => HoverTarget = NodeMarkupTool.MouseRayValid ? AvailableTargets.FirstOrDefault(t => t.IsHover(NodeMarkupTool.MouseRay)) : null;

        public override void OnToolUpdate()
        {
            foreach (var source in Sources)
                source.Update(this);

            GetHoverSource();
            GetHoverTarget();
        }
        protected abstract string InfoDrop { get; }
        protected abstract string InfoDrag { get; }
        public override string GetToolInfo()
        {
            if (IsSelectedSource)
                return InfoDrop;
            else
                return InfoDrag;
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSource)
            {
                SelectedSource = HoverSource;
                SetAvailableTargets();
            }
        }
        public override void OnPrimaryMouseClicked(Event e) => EndDrag();
        public override void OnMouseUp(Event e)
        {
            if (IsSelectedSource)
            {
                if (IsHoverTarget)
                {
                    foreach (var source in Sources)
                    {
                        if (source.Target == HoverTarget)
                            source.Target = null;
                    }

                    SelectedSource.Target = HoverTarget;
                }
                else
                    SelectedSource.Target = null;

                EndDrag();
                Paste();
            }
        }
        private void EndDrag()
        {
            SelectedSource = null;
            SetAvailableTargets();
            SetBaskets();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var basket in Baskets)
                basket.Render(cameraInfo, this);

            RenderOverlayAfterBaskets(cameraInfo);

            foreach (var target in Targets)
                target.Render(cameraInfo, this);

            RenderOverlayAfterTargets(cameraInfo);

            foreach (var source in Sources)
            {
                if (!IsSelectedSource || SelectedSource == source || (source.Target != null && source.Target != HoverTarget))
                    source.Render(cameraInfo, this);
            }
        }
        protected virtual void RenderOverlayAfterBaskets(RenderManager.CameraInfo cameraInfo) { }
        protected virtual void RenderOverlayAfterTargets(RenderManager.CameraInfo cameraInfo) { }

        protected void SetAvailableTargets() => AvailableTargets = IsSelectedSource ? GetAvailableTargets(SelectedSource) : Targets.ToArray();
        protected abstract Target<SourceType>[] GetAvailableTargets(SourceType source);
        protected void SetBaskets() => Baskets = GetBaskets();
        protected abstract Basket<SourceType>[] GetBaskets();
    }
}
