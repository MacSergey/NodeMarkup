using NodeMarkup.Manager;
using NodeMarkup.UI.Panel;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BaseToolMode : MonoBehaviour
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        public Markup Markup => Tool.Markup;
        protected NodeMarkupPanel Panel => NodeMarkupPanel.Instance;

        public BaseToolMode()
        {
            Disable();
        }

        public virtual void Activate(BaseToolMode prevMode)
        {
            enabled = true;
            Reset(prevMode);
        }
        public virtual void Deactivate() => Disable();
        private void Disable() => enabled = false;

        protected virtual void Reset(BaseToolMode prevMode) { }

        public virtual void Update() { }

        public virtual void OnToolUpdate() { }
        public virtual string GetToolInfo() => null;

        public virtual void OnToolGUI(Event e) { }
        public virtual void OnMouseDown(Event e) { }
        public virtual void OnMouseDrag(Event e) { }
        public virtual void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public virtual void OnPrimaryMouseClicked(Event e) { }
        public virtual void OnSecondaryMouseClicked() { }
        public virtual bool OnEscape() => false;
        public virtual void RenderOverlay(RenderManager.CameraInfo cameraInfo) { }
    }

    public enum ToolModeType
    {
        None = 0,

        Select = 1,
        MakeLine = 2,
        MakeCrosswalk = 4,
        MakeFiller = 8,
        PanelAction = 16,
        PasteEntersOrder = 32,
        EditEntersOrder = 64,
        ApplyIntersectionTemplateOrder = 128,
        PointsOrder = 256,
        DragPoint = 512,

        MakeItem = MakeLine | MakeCrosswalk
    }
}
