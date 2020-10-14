using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.Tools
{
    public abstract class BasePasteMarkupToolMode : BaseToolMode
    {
        public static UITextureAtlas ButtonAtlas { get; } = GetButtonsIcons();
        private static UITextureAtlas GetButtonsIcons()
        {
            var spriteNames = new string[]
            {
                "TurnLeft",
                "Flip",
                "TurnRight",
            };

            var atlas = TextureUtil.GetAtlas(nameof(PasteMarkupEntersOrderToolMode));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("PasteButtons.png", nameof(PasteMarkupEntersOrderToolMode), 50, 50, spriteNames, new RectOffset(0, 0, 0, 0));

            return atlas;
        }

        public Vector3 Centre { get; protected set; }
        public float Radius { get; protected set; }

        protected XElement Backup { get; set; }
        protected MarkupBuffer Buffer => Tool.MarkupBuffer;

        protected bool IsMirror { get; set; }
        public SourceEnter[] SourceEnters { get; set; } = new SourceEnter[0];
        public TargetEnter[] TargetEnters { get; set; } = new TargetEnter[0];

        protected override void Reset(BaseToolMode prevMode)
        {
            if (prevMode is BasePasteMarkupToolMode pasteMarkupTool)
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
                SourceEnters = Tool.MarkupBuffer.Enters.Select((e, i) => new SourceEnter(e, i)).ToArray();
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
                map.AddEnter(source.Enter.Id, source.Target?.Enter.Id ?? 0);

                if (source.Target != null)
                {
                    for (var i = 0; i < source.Points.Length; i += 1)
                        map.AddPoint(source.Target.Enter.Id, i + 1, source.Points[i].Target?.Num + 1 ?? 0);
                }
            }

            Markup.FromXml(Mod.Version, Buffer.Data, map);
            Panel.UpdatePanel();
        }
    }
    public abstract class BasePasteMarkupToolMode<SourceType, TargetType> : BasePasteMarkupToolMode
        where SourceType : Source<TargetType>
        where TargetType : Target
    {
        public SourceType[] Sources { get; set; } = new SourceType[0];
        public TargetType[] Targets { get; set; } = new TargetType[0];

        public SourceType HoverSource { get; protected set; }
        public bool IsHoverSource => HoverSource != null;

        public SourceType SelectedSource { get; protected set; }
        public bool IsSelectedSource => SelectedSource != null;

        public TargetType HoverTarget { get; protected set; }
        public bool IsHoverTarget => HoverTarget != null;

        public TargetType[] AvailableTargets { get; protected set; }

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
        }

        protected abstract SourceType[] GetSources(BaseToolMode prevMode);
        protected abstract TargetType[] GetTargets(BaseToolMode prevMode);

        public void GetHoverSource() => HoverSource = NodeMarkupTool.MouseRayValid ? Sources.FirstOrDefault(s => s.IsHover(NodeMarkupTool.MouseRay)) : null;
        public void GetHoverTarget() => HoverTarget = NodeMarkupTool.MouseRayValid ? AvailableTargets.FirstOrDefault(t => t.IsHover(NodeMarkupTool.MouseRay)) : null;

        public override void OnUpdate()
        {
            foreach (var source in Sources)
                source.Update(this);

            GetHoverSource();
            GetHoverTarget();
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSource)
            {
                SelectedSource = HoverSource;
                SetAvailableTargets();
            }
        }
        protected void SetAvailableTargets() => AvailableTargets = IsSelectedSource ? GetAvailableTargets(SelectedSource).ToArray() : Targets.ToArray();
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
        public override void OnPrimaryMouseClicked(Event e) => EndDrag();
        private void EndDrag()
        {
            SelectedSource = null;
            SetAvailableTargets();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var target in Targets)
                target.Render(cameraInfo, this);

            foreach (var source in Sources)
            {
                if (!IsSelectedSource || SelectedSource == source || (source.Target != null && source.Target != HoverTarget))
                    source.Render(cameraInfo, this);
            }
        }

        private IEnumerable<TargetType> GetAvailableTargets(SourceType source)
        {
            var a = GetAvailableBorder(source, s => !IsMirror ? s.PrevIndex(Sources.Length) : s.NextIndex(Sources.Length), AvailableTargetsGetter) ?? Targets.First();
            var b = GetAvailableBorder(source, s => !IsMirror ? s.NextIndex(Sources.Length) : s.PrevIndex(Sources.Length), AvailableTargetsGetter) ?? Targets.Last();

            yield return a;
            for (var target = Targets[a.Num.NextIndex(Targets.Length)]; target != b; target = Targets[target.Num.NextIndex(Targets.Length)])
                yield return target;
            if (b != a)
                yield return b;
        }
        private TargetType GetAvailableBorder(SourceType source, Func<int, int> func, Func<int, SourceType, bool> condition)
        {
            var i = func(source.Num);
            for (; condition(i, source) && Sources[i].Target == null; i = func(i)) { }
            return Sources[i].Target;
        }
        protected abstract Func<int, SourceType, bool> AvailableTargetsGetter { get; }
    }
}
