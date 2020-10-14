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

namespace NodeMarkup.UI
{
    public class NodeMarkupPanel : UIPanel
    {
        public static NodeMarkupPanel Instance { get; private set; }

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        public Markup Markup { get; private set; }

        private UIPanelDragHeader Header { get; set; }
        private CustomUITabstrip TabStrip { get; set; }
        private UIPanel SizeChanger { get; set; }
        public List<Editor> Editors { get; } = new List<Editor>();
        public Editor CurrentEditor { get; set; }

        private Vector2 EditorSize => size - new Vector2(0, Header.height + TabStrip.height);
        private Vector2 EditorPosition => new Vector2(0, TabStrip.relativePosition.y + TabStrip.height);

        public static NodeMarkupPanel CreatePanel()
        {
            var uiView = UIView.GetAView();
            Instance = uiView.AddUIComponent(typeof(NodeMarkupPanel)) as NodeMarkupPanel;
            Instance.Init();
            return Instance;
        }
        public static void RemovePanel()
        {
            if (Instance != null)
            {
                Instance.Hide();
                Destroy(Instance);
                Instance = null;
            }
        }
        public void Init()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "MenuPanel2";
            absolutePosition = new Vector3(100, 100);
            name = "NodeMarkupPanel";

            CreateHeader();
            CreateTabStrip();
            CreateEditors();
            CreateSizeChanger();

            size = new Vector2(550, Header.height + TabStrip.height + 400); ;
            minimumSize = new Vector2(500, Header.height + TabStrip.height + 200);
        }
        private void CreateHeader()
        {
            Header = AddUIComponent<UIPanelDragHeader>();
            Header.size = new Vector2(550, 42);
            Header.relativePosition = new Vector2(0, 0);
            Header.target = parent;

            Header.Buttons.OnCopy += OnCopy;
            Header.Buttons.OnPaste += OnPaste;
            Header.Buttons.OnClear += OnClear;
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
            CreateEditor<TemplateEditor>();
        }

        public static UITextureAtlas ResizeAtlas { get; } = GetStylesIcons();
        private static UITextureAtlas GetStylesIcons()
        {
            var atlas = TextureUtil.GetAtlas(nameof(ResizeAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("resize.png", nameof(ResizeAtlas), 9, 9, new string[] { "resize"}, space: 2);

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

        private void CreateEditor<EditorType>() where EditorType : Editor
        {
            var editor = AddUIComponent<EditorType>();
            editor.Init(this);
            TabStrip.AddTab(editor.Name);

            editor.isVisible = false;
            editor.relativePosition = EditorPosition;

            Editors.Add(editor);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            //foreach (var editor in Editors)
            //    editor.size = EditorSize;
            if (CurrentEditor != null)
                CurrentEditor.size = EditorSize;
            if (Header != null)
                Header.size = new Vector2(size.x, Header.height);
            if (SizeChanger != null)
                SizeChanger.relativePosition = size - SizeChanger.size;
        }

        public void UpdatePanel() => CurrentEditor?.UpdateEditor();
        public void SetNode(Markup markup)
        {
            Markup = markup;

            if (Markup != null)
            {
                Show();
                Header.Text = string.Format(NodeMarkup.Localize.Panel_Caption, Markup.Id);
                TabStrip.selectedIndex = -1;
                SelectEditor<LinesEditor>();
            }
            else
                Hide();
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
            editor?.UpdateEditor(crosswalk);
        }
        public void EditTemplate(StyleTemplate template)
        {
            var editor = SelectEditor<TemplateEditor>();
            editor?.UpdateEditor(template);
        }
        public void EditFiller(MarkupFiller filler)
        {
            var editor = SelectEditor<FillerEditor>();
            editor?.UpdateEditor(filler);
        }
        public bool OnShortcut(Event e) => CurrentEditor?.OnShortcut(e) == true;
        public void Render(RenderManager.CameraInfo cameraInfo) => CurrentEditor?.Render(cameraInfo);

        private void OnClear() => Tool.DeleteAllMarking();
        private void OnCopy() => Tool.CopyMarkup();
        private void OnPaste() => Tool.PasteMarkup();
    }
    public class UIPanelDragHeader : UIDragHandle
    {
        private UILabel Caption { get; set; }
        public MainHeaderContent Buttons { get; private set; }

        public string Text
        {
            get => Caption.text;
            set => Caption.text = value;
        }

        public UIPanelDragHeader()
        {
            CreateCaption();
            CreateButtonsPanel();
        }

        private void CreateCaption()
        {
            Caption = AddUIComponent<UILabel>();
            Caption.autoSize = false;
            Caption.text = nameof(NodeMarkupPanel);
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private void CreateButtonsPanel()
        {
            Buttons = AddUIComponent<MainHeaderContent>();
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Buttons.autoLayout = true;
            Buttons.autoLayout = false;
            Buttons.FitChildrenHorizontally();
            Buttons.height = height;

            foreach (var item in Buttons.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Buttons.height - item.height) / 2);

            Caption.width = width - Buttons.width;
            Caption.relativePosition = new Vector2(0, (height - Caption.height) / 2);

            Buttons.relativePosition = new Vector2(Caption.width - 5, (height - Buttons.height) / 2);
        }
    }
    public class MainHeaderContent : HeaderContent
    {
        public event Action OnClear;
        public event Action OnCopy;
        public event Action OnPaste;

        UIButton Copy { get; set; }
        UIButton Paste { get; set; }
        UIButton Clear { get; set; }

        public MainHeaderContent()
        {
            Copy = AddButton("Copy", NodeMarkup.Localize.Panel_CopyMarking, (UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke());
            Paste = AddButton("Paste", NodeMarkup.Localize.Panel_PasteMarking, (UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke());
            Clear = AddButton("Clear", NodeMarkup.Localize.Panel_ClearMarking, (UIComponent component, UIMouseEventParameter eventParam) => OnClear?.Invoke());
        }
    }
}
