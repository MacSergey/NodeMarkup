using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Editors;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class NodeMarkupPanel : CustomUIPanel
    {
        #region PROPERTIES

        public static NodeMarkupPanel Instance { get; private set; }
        private static Vector2 DefaultPosition { get; } = new Vector2(100f, 100f);

        public bool Active
        {
            get => enabled && isVisible;
            set
            {
                if (value == Active)
                    return;

                enabled = value;
                isVisible = value;

                if (value)
                {
                    if (CurrentEditor is Editor editor)
                        editor.Active = true;
                }
                else
                {
                    foreach (var editor in Editors)
                        editor.Active = false;
                }
            }
        }

        private float Width => 550f;

        public Markup Markup { get; private set; }
        private bool NeedUpdateOnVisible { get; set; }
        public bool IsHover => isVisible && new Rect(relativePosition, size).Contains(NodeMarkupTool.MousePosition);

        private PanelHeader Header { get; set; }
        private PanelTabStrip TabStrip { get; set; }
        private CustomUIPanel SizeChanger { get; set; }
        public List<Editor> Editors { get; } = new List<Editor>();
        public Editor CurrentEditor { get; set; }

        private float HeaderHeight => 42f;
        private Vector2 EditorSize => size - new Vector2(0, Header.height + TabStrip.height);
        private Vector2 EditorPosition => new Vector2(0, TabStrip.relativePosition.y + TabStrip.height);

        public bool Available
        {
            set
            {
                Header.Available = value;
                TabStrip.SetAvailable(value);
            }
        }

        #endregion

        #region BASIC

        public static void CreatePanel()
        {
            Mod.Logger.Debug($"Create panel");
            UIView.GetAView().AddUIComponent(typeof(NodeMarkupPanel));
            Mod.Logger.Debug($"Panel created");
        }
        public override void Awake()
        {
            base.Awake();

            Instance = this;

            atlas = TextureHelper.InGameAtlas;
            backgroundSprite = "MenuPanel2";
            name = "NodeMarkupPanel";

            CreateHeader();
            CreateTabStrip();
            CreateEditors();
            CreateSizeChanger();

            minimumSize = new Vector2(Width, Header.height + TabStrip.height + 200);

            Instance.Active = false;
        }
        public override void Start()
        {
            base.Start();
            SetDefaultPosition();
            SetDefaulSize();
        }
        public override void OnEnable()
        {
            base.OnEnable();

            CheckPosition();
            UpdatePanel();
        }
        public static void RemovePanel()
        {
            Mod.Logger.Debug($"Remove panel");
            if (Instance != null)
            {
                Instance.Hide();
                Destroy(Instance);
                Instance = null;
                Mod.Logger.Debug($"Panel removed");
            }
        }
        private void CheckPosition()
        {
            if (absolutePosition.x < 0 || absolutePosition.y < 0)
                SetDefaultPosition();
        }
        private void SetDefaultPosition()
        {
            Mod.Logger.Debug($"Set default panel position");
            absolutePosition = DefaultPosition;
        }
        private void SetDefaulSize() => size = new Vector2(Width, Header.height + TabStrip.height + 400);

        #endregion

        #region COMPONENTS

        private void CreateHeader()
        {
            Header = AddUIComponent<PanelHeader>();
            Header.size = new Vector2(Width, HeaderHeight);
            Header.relativePosition = new Vector2(0, 0);
            Header.target = parent;
        }
        private void CreateTabStrip()
        {
            TabStrip = AddUIComponent<PanelTabStrip>();
            TabStrip.relativePosition = new Vector3(0, HeaderHeight);
            TabStrip.SelectedTabChanged += OnSelectedTabChanged;
            TabStrip.SelectedTab = -1;
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
        private void CreateEditor<EditorType>() where EditorType : Editor
        {
            var editor = AddUIComponent<EditorType>();
            editor.Active = false;
            editor.Init(this);
            TabStrip.AddTab(editor);

            Editors.Add(editor);
        }
        private void CreateSizeChanger()
        {
            SizeChanger = AddUIComponent<SizeChanger>();
            SizeChanger.eventPositionChanged += SizeChangerPositionChanged;
            SizeChanger.eventDoubleClick += SizeChangerDoubleClick;
        }

        #endregion

        #region UPDATE

        public void SetMarkup(Markup markup)
        {
            if ((Markup = markup) != null)
            {
                if (isVisible)
                    UpdatePanelOnVisible();
                else
                    NeedUpdateOnVisible = true;
            }
        }
        public void UpdatePanel()
        {
            Available = true;
            foreach (var editor in Editors)
                editor.UpdateEditor();
        }
        private void UpdatePanelOnVisible()
        {
            NeedUpdateOnVisible = false;

            Header.Text = Markup.PanelCaption;
            TabStrip.SetVisible(Markup);
            TabStrip.ArrangeTabs();
            TabStrip.SelectedTab = -1;
            SelectEditor<LinesEditor>();
        }

        #endregion

        #region ONEVENTS

        private void SizeChangerPositionChanged(UIComponent component, Vector2 value)
        {
            size = (Vector2)SizeChanger.relativePosition + SizeChanger.size;
            SizeChanger.relativePosition = size - SizeChanger.size;
        }
        private void SizeChangerDoubleClick(UIComponent component, UIMouseEventParameter eventParam) => SetDefaulSize();
        protected override void OnSizeChanged()
        {
            var swAll = Stopwatch.StartNew();
            base.OnSizeChanged();

            if (Header != null)
                Header.width = width;
            if (TabStrip != null)
                TabStrip.width = width;
            if (CurrentEditor != null)
            {
                CurrentEditor.size = EditorSize;
                CurrentEditor.relativePosition = EditorPosition;
            }
            if (SizeChanger != null)
                SizeChanger.relativePosition = size - SizeChanger.size;

            MakePixelPerfect(false);

            Mod.Logger.Debug($"Panel size changed: {swAll.ElapsedTicks};");
        }
        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();
            if (isVisible && NeedUpdateOnVisible)
                UpdatePanelOnVisible();
        }
        private void OnSelectedTabChanged(int index) => CurrentEditor = SelectEditor(index);

        #endregion

        #region GET SELECT

        private Editor SelectEditor(int index)
        {
            if (index >= 0 && Editors.Count > index)
            {
                foreach (var editor in Editors)
                    editor.Active = false;

                var selectEditor = Editors[index];
                selectEditor.Active = true;
                selectEditor.size = EditorSize;
                selectEditor.relativePosition = EditorPosition;
                return selectEditor;
            }
            else
                return null;
        }
        private EditorType SelectEditor<EditorType>() where EditorType : Editor
        {
            var editorIndex = Editors.FindIndex((e) => e.GetType() == typeof(EditorType));
            TabStrip.SelectedTab = editorIndex;
            return Editors[editorIndex] as EditorType;
        }

        #endregion

        #region ADD OBJECT

        private void AddObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, ISupport, IDeletable
        {
            if (Editors.Find(e => e.GetType() == typeof(EditorType)) is EditorType editor)
            {
                editor.Add(item);
                CurrentEditor?.RefreshEditor();
            }
        }
        public void AddLine(MarkupLine line) => AddObject<LinesEditor, MarkupLine>(line);

        #endregion

        #region DELETE OBJECT

        private void DeleteObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, ISupport, IDeletable
        {
            if (Editors.Find(e => e.GetType() == typeof(EditorType)) is EditorType editor)
            {
                editor.Delete(item);
                CurrentEditor?.RefreshEditor();
            }
        }

        public void DeleteLine(MarkupLine line) => DeleteObject<LinesEditor, MarkupLine>(line);
        public void DeleteCrosswalk(MarkupCrosswalk crosswalk) => DeleteObject<CrosswalksEditor, MarkupCrosswalk>(crosswalk);

        #endregion

        #region EDIT OBJECT

        private EditorType EditObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, ISupport, IDeletable
        {
            if (Markup is not ISupport<ItemType>)
                return null;

            Available = true;
            var editor = SelectEditor<EditorType>();
            editor?.Edit(item);
            return editor;
        }
        public void EditPoint(MarkupEnterPoint point) => EditObject<PointsEditor, MarkupEnterPoint>(point);
        public void EditLine(MarkupLine line) => EditObject<LinesEditor, MarkupLine>(line);
        public void EditCrosswalk(MarkupCrosswalk crosswalk)
        {
            AddLine(crosswalk.CrosswalkLine);
            var editor = EditObject<CrosswalksEditor, MarkupCrosswalk>(crosswalk);
            editor?.BorderSetup();
        }
        public void EditFiller(MarkupFiller filler) => EditObject<FillerEditor, MarkupFiller>(filler);

        private void EditTemplate<EditorType, TemplateType>(TemplateType template, bool editName)
            where EditorType : Editor, IEditor<TemplateType>, ITemplateEditor<TemplateType>
            where TemplateType : Template
        {
            var editor = EditObject<EditorType, TemplateType>(template);
            if (editName && editor != null)
                editor.EditName();
        }

        public void EditStyleTemplate(StyleTemplate template, bool editName = true) => EditTemplate<StyleTemplateEditor, StyleTemplate>(template, editName);
        public void EditIntersectionTemplate(IntersectionTemplate template, bool editName = true) => EditTemplate<IntersectionTemplateEditor, IntersectionTemplate>(template, editName);

        #endregion

        #region ADDITIONAL

        public bool OnShortcut(Event e) => CurrentEditor?.OnShortcut(e) == true;
        public bool OnEscape() => CurrentEditor?.OnEscape() == true;

        public void Render(RenderManager.CameraInfo cameraInfo) => CurrentEditor?.Render(cameraInfo);

        #endregion
    }
    public class SizeChanger : CustomUIPanel
    {
        private bool InProgress { get; set; }
        public SizeChanger()
        {
            size = new Vector2(9, 9);
            atlas = TextureHelper.CommonAtlas;
            backgroundSprite = TextureHelper.ResizeSprite;
            color = new Color32(255, 255, 255, 160);

            var handle = AddUIComponent<CustomUIDragHandle>();
            handle.size = size;
            handle.relativePosition = Vector2.zero;
            handle.target = this; ;
        }

        protected override void OnPositionChanged()
        {
            if (!InProgress)
            {
                InProgress = true;
                base.OnPositionChanged();
                InProgress = false;
            }
        }
    }
}
