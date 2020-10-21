using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BaseToolMode
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        public Markup Markup => Tool.Markup;
        protected NodeMarkupPanel Panel => NodeMarkupPanel.Instance;

        public virtual void Start(BaseToolMode prevMode) => Reset(prevMode);
        public virtual void End() { }
        protected virtual void Reset(BaseToolMode prevMode) { }

        public virtual void OnUpdate() { }
        public virtual string GetToolInfo() => null;

        public virtual void OnGUI(Event e) { }
        public virtual bool ProcessShortcuts(Event e) => false;
        public virtual void OnMouseDown(Event e) { }
        public virtual void OnMouseDrag(Event e) { }
        public virtual void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public virtual void OnPrimaryMouseClicked(Event e) { }
        public virtual void OnSecondaryMouseClicked() { }
        public virtual void RenderOverlay(RenderManager.CameraInfo cameraInfo) { }

        protected string GetCreateToolTip<StyleType>(string text)
            where StyleType : Enum
        {
            var modifiers = GetStylesModifier<StyleType>().ToArray();
            return modifiers.Any() ? $"{text}:\n{string.Join("\n", modifiers)}" : text;
        }
        protected IEnumerable<string> GetStylesModifier<StyleType>()
            where StyleType : Enum
        {
            foreach (var style in Utilities.GetEnumValues<StyleType>())
            {
                var general = (Style.StyleType)(object)style;
                var modifier = (StyleModifier)NodeMarkupTool.StylesModifier[general].value;
                if (modifier != StyleModifier.NotSet)
                    yield return $"{general.Description()} - {modifier.Description()}";
            }
        }
    }

    public enum ToolModeType
    {
        SelectNode,
        MakeLine,
        MakeCrosswalk,
        MakeFiller,
        PanelAction,
        PasteEntersOrder,
        EditEntersOrder,
        PointsOrder,
        DragPoint,
    }
}
