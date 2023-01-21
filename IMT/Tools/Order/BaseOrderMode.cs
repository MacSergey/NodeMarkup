﻿using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Tools
{
    public abstract class BaseOrderToolMode : IntersectionMarkingToolMode
    {
        public override bool ShowPanel => false;
        public Vector3 Centre { get; protected set; }
        public float Radius { get; protected set; }

        public static IntersectionTemplate IntersectionTemplate { get; set; }

        protected XElement Backup { get; set; }

        public bool Invert { get; protected set; }
        public SourceEnter[] SourceEnters { get; set; } = new SourceEnter[0];
        public TargetEnter[] TargetEnters { get; set; } = new TargetEnter[0];

        protected override void Reset(IToolMode prevMode)
        {
            if (prevMode is BaseOrderToolMode pasteMarkingTool)
            {
                Backup = pasteMarkingTool.Backup;
                Invert = pasteMarkingTool.Invert;
                SourceEnters = pasteMarkingTool.SourceEnters;
                TargetEnters = pasteMarkingTool.TargetEnters;
            }
            else
            {
                Backup = Marking.ToXml();
                Invert = false;
                SourceEnters = IntersectionTemplate.Enters.Select((e, i) => new SourceEnter(e, i)).ToArray();
                TargetEnters = Marking.Enters.Select((e, i) => new TargetEnter(e, i)).ToArray();

                var min = Math.Min(TargetEnters.Length, SourceEnters.Length);
                for (var i = 0; i < min; i += 1)
                    SourceEnters[i].Target = TargetEnters[i];
            }

            Paste();
        }
        protected void Paste()
        {
            var map = new ObjectsMap(Invert);

            foreach (var source in SourceEnters)
            {
                var enterTarget = source.Target as TargetEnter;
                var sourceId = source.Enter.Id;
                var targetId = enterTarget?.Enter.Id ?? 0;
                switch (Marking.Type)
                {
                    case MarkingType.Node:
                        map.AddSegment(sourceId, targetId);
                        break;
                    case MarkingType.Segment:
                        map.AddNode(sourceId, targetId);
                        break;
                }

                if (enterTarget != null)
                {
                    for (var i = 0; i < source.Points.Length; i += 1)
                    {
                        if (source.Points[i].Target is Target target)
                        {
                            var sourceI = (byte)(i + 1);
                            var targetI = (byte)(target.Index + 1);
                            map.AddPoint(enterTarget.Enter.Id, sourceI, targetI);
                        }
                    }
                }
            }

            Marking.Clear();
            Marking.FromXml(SingletonMod<Mod>.Version, IntersectionTemplate.Data, map);
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

        protected override void Reset(IToolMode prevMode)
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

        protected abstract SourceType[] GetSources(IToolMode prevMode);
        protected abstract Target<SourceType>[] GetTargets(IToolMode prevMode);

        public void GetHoverSource() => HoverSource = SingletonTool<IntersectionMarkingTool>.Instance.MouseRayValid ? Sources.FirstOrDefault(s => s.IsHover(SingletonTool<IntersectionMarkingTool>.Instance.MouseRay)) : null;
        public void GetHoverTarget() => HoverTarget = SingletonTool<IntersectionMarkingTool>.Instance.MouseRayValid ? AvailableTargets.FirstOrDefault(t => t.IsHover(SingletonTool<IntersectionMarkingTool>.Instance.MouseRay)) : null;

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
