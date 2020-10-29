using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class NodeMarkupPanel : UIPanel
    {
        public static NodeMarkupPanel Instance { get; private set; }
        private static Vector2 DefaultPosition { get; } = new Vector2(100f, 100f);

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        public Markup Markup { get; private set; }

        private PanelHeader Header { get; set; }
        private CustomUITabstrip TabStrip { get; set; }
        private UIPanel SizeChanger { get; set; }
        public List<Editor> Editors { get; } = new List<Editor>();
        public Editor CurrentEditor { get; set; }

        private Vector2 EditorSize => size - new Vector2(0, Header.height + TabStrip.height);
        private Vector2 EditorPosition => new Vector2(0, TabStrip.relativePosition.y + TabStrip.height);

        public static NodeMarkupPanel CreatePanel()
        {
            Logger.LogDebug($"{nameof(NodeMarkupPanel)}.{nameof(CreatePanel)}");
            var uiView = UIView.GetAView();
            Instance = uiView.AddUIComponent(typeof(NodeMarkupPanel)) as NodeMarkupPanel;
            Instance.Init();
            Logger.LogDebug($"Panel created");
            return Instance;
        }
        public static void RemovePanel()
        {
            Logger.LogDebug($"{nameof(NodeMarkupPanel)}.{nameof(RemovePanel)}");
            if (Instance != null)
            {
                Instance.Hide();
                Destroy(Instance);
                Instance = null;
                Logger.LogDebug($"Panel removed");
            }
        }
        public void Init()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "MenuPanel2";
            name = "NodeMarkupPanel";
            SetPosition();

            CreateHeader();
            CreateTabStrip();
            CreateEditors();
            CreateSizeChanger();

            size = new Vector2(550, Header.height + TabStrip.height + 400); ;
            minimumSize = new Vector2(500, Header.height + TabStrip.height + 200);
        }
        private void CreateHeader()
        {
            Header = AddUIComponent<PanelHeader>();
            Header.size = new Vector2(550, 42);
            Header.relativePosition = new Vector2(0, 0);
            Header.target = parent;
        }
        private void CreateTabStrip()
        {
            TabStrip = AddUIComponent<CustomUITabstrip>();
            TabStrip.relativePosition = new Vector3(0, Header.height);
            TabStrip.eventSelectedIndexChanged += TabStripSelectedIndexChanged;
            TabStrip.selectedIndex = -1;
        }

        private void CreateEditors()
        {
            CreateEditor<PointsEditor>();
            CreateEditor<LinesEditor>();
            CreateEditor<CrosswalksEditor>();
            CreateEditor<FillerEditor>();
            CreateEditor<StyleTemplateEditor>();
            CreateEditor<IntersectionTemplateEditor>();
        }

        public static UITextureAtlas ResizeAtlas { get; } = GetStylesIcons();
        private static UITextureAtlas GetStylesIcons()
        {
            var atlas = TextureUtil.GetAtlas(nameof(ResizeAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("resize.png", nameof(ResizeAtlas), 9, 9, new string[] { "resize" }, space: 2);

            return atlas;
        }
        private void CreateSizeChanger()
        {
            SizeChanger = AddUIComponent<UIPanel>();
            SizeChanger.size = new Vector2(9, 9);
            SizeChanger.atlas = ResizeAtlas;
            SizeChanger.backgroundSprite = "resize";
            SizeChanger.color = new Color32(255, 255, 255, 160);
            SizeChanger.eventPositionChanged += SizeChangerPositionChanged;

            var handle = SizeChanger.AddUIComponent<UIDragHandle>();
            handle.size = SizeChanger.size;
            handle.relativePosition = Vector2.zero;
            handle.target = SizeChanger;
        }

        private void SizeChangerPositionChanged(UIComponent component, Vector2 value)
        {
            size = (Vector2)SizeChanger.relativePosition + SizeChanger.size;
            SizeChanger.relativePosition = size - SizeChanger.size;
        }
        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            if (isVisible)
            {
                CheckPosition();
                UpdatePanel();
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (CurrentEditor != null)
                CurrentEditor.size = EditorSize;
            if (Header != null)
                Header.size = new Vector2(size.x, Header.height);
            if (SizeChanger != null)
                SizeChanger.relativePosition = size - SizeChanger.size;
        }
        private void CreateEditor<EditorType>() where EditorType : Editor
        {
            var editor = AddUIComponent<EditorType>();
            editor.Init(this);
            TabStrip.AddTab(editor.Name);

            editor.isVisible = false;
            editor.relativePosition = EditorPosition;

            Editors.Add(editor);
        }
        private void CheckPosition()
        {
            if (absolutePosition.x < 0 || absolutePosition.y < 0)
                SetPosition();
        }
        private void SetPosition()
        {
            Logger.LogDebug($"Set default panel position");
            absolutePosition = DefaultPosition;
        }

        public void UpdatePanel() => CurrentEditor?.UpdateEditor();
        public void SetNode(Markup markup)
        {
            Markup = markup;
            if (Markup != null)
            {
                Header.Text = string.Format(NodeMarkup.Localize.Panel_Caption, Markup.Id);
                TabStrip.selectedIndex = -1;
                SelectEditor<LinesEditor>();
            }
        }
        private int GetEditor(Type editorType) => Editors.FindIndex((e) => e.GetType() == editorType);
        private void TabStripSelectedIndexChanged(UIComponent component, int index)
        {
            CurrentEditor = SelectEditor(index);
            UpdatePanel();
        }
        private Editor SelectEditor(int index)
        {
            if (index >= 0 && Editors.Count > index)
            {
                foreach (var editor in Editors)
                    editor.isVisible = false;

                var selectEditor = Editors[index];
                selectEditor.isVisible = true;
                selectEditor.size = EditorSize;
                return selectEditor;
            }
            else
                return null;
        }
        private EditorType SelectEditor<EditorType>() where EditorType : Editor
        {
            var editorIndex = GetEditor(typeof(EditorType));
            TabStrip.selectedIndex = editorIndex;
            return Editors[editorIndex] as EditorType;
        }

        public void EditPoint(MarkupPoint point)
        {
            var editor = SelectEditor<PointsEditor>();
            editor?.UpdateEditor(point);
        }
        public void EditLine(MarkupLine line)
        {
            var editor = SelectEditor<LinesEditor>();
            editor?.UpdateEditor(line);
        }
        public void EditCrosswalk(MarkupCrosswalk crosswalk)
        {
            var editor = SelectEditor<CrosswalksEditor>();
            if (editor != null)
            {
                editor.UpdateEditor(crosswalk);
                editor.BorderSetup();
            }
        }
        public void EditFiller(MarkupFiller filler)
        {
            var editor = SelectEditor<FillerEditor>();
            editor?.UpdateEditor(filler);
        }
        public void EditTemplate(StyleTemplate template)
        {
            var editor = SelectEditor<StyleTemplateEditor>();
            editor?.UpdateEditor(template);
        }
        public void EditPreset(IntersectionTemplate preset)
        {
            var editor = SelectEditor<IntersectionTemplateEditor>();
            editor?.UpdateEditor(preset);
        }

        public bool OnShortcut(Event e) => CurrentEditor?.OnShortcut(e) == true;
        public void Render(RenderManager.CameraInfo cameraInfo) => CurrentEditor?.Render(cameraInfo);
    } 
}
