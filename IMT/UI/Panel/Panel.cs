using IMT.Manager;
using IMT.Tools;
using IMT.UI.Editors;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI.Panel
{
    public class IntersectionMarkingToolPanel : ToolPanel<Mod, IntersectionMarkingTool, IntersectionMarkingToolPanel>
    {
        #region PROPERTIES

        public override bool Active
        {
            get => base.Active;
            set
            {
                if (value == Active)
                    return;

                base.Active = value;

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

        private float MinWidth => Settings.AutoCollapseItemsPanel ? 500f : 580f;

        public Marking Marking { get; private set; }
        private bool NeedRefreshOnVisible { get; set; }

        private PanelHeader Header { get; set; }
        private PanelTabStrip TabStrip { get; set; }

        public List<Editor> Editors { get; } = new List<Editor>();
        public Editor PrevEditor { get; set; }
        public Editor CurrentEditor { get; set; }

        private bool available = true;
        public bool Available
        {
            get => available;
            set
            {
                if (value != available)
                {
                    available = value;
                    Header.Available = value;
                    TabStrip.Available = value;
                }
            }
        }
        protected override bool NeedRefresh => base.NeedRefresh && NeedRefreshOnVisible;

        #endregion

        #region BASIC

        public override void Awake()
        {
            SingletonItem<IntersectionMarkingToolPanel>.Instance = this;

            Atlas = CommonTextures.Atlas;
            BackgroundSprite = CommonTextures.PanelBig;
            color = ComponentStyle.PanelColor;
            name = nameof(IntersectionMarkingToolPanel);

            Header = AddUIComponent<PanelHeader>();
            Header.relativePosition = new Vector2(0, 0);
            Header.Target = parent;
            Header.Init(HeaderHeight);

            TabStrip = AddUIComponent<PanelTabStrip>();
            TabStrip.relativePosition = new Vector3(0, HeaderHeight);
            TabStrip.SelectedTabChanged += OnSelectedTabChanged;
            TabStrip.SelectedTab = -1;

            TabStrip.StopLayout();
            {
                CreateEditor<PointsEditor>();
                CreateEditor<LinesEditor>();
                CreateEditor<CrosswalksEditor>();
                CreateEditor<FillerEditor>();
                CreateEditor<StyleTemplateEditor>();
                CreateEditor<IntersectionTemplateEditor>();
            }
            TabStrip.StartLayout();

            AddUIComponent<SizeChanger>();

            minimumSize = GetSize(600);

            base.Awake();
        }
        public override void Start()
        {
            base.Start();

            SetDefaulSize();
            SetMinimumSize();
        }
        public override void OnEnable()
        {
            base.OnEnable();
            UpdatePanel();
        }
        private void SetDefaulSize()
        {
            SingletonMod<Mod>.Logger.Debug($"Set default panel size");
            size = GetSize(600);
        }
        public void SetMinimumSize()
        {
            minimumSize = GetSize(200);
            size = size;
        }
        private Vector2 GetSize(float additional) => new Vector2(MinWidth, Header.height + TabStrip.height + additional);

        #endregion

        #region COMPONENTS

        private void CreateEditor<EditorType>() where EditorType : Editor
        {
            var editor = AddUIComponent<EditorType>();
            editor.Active = false;
            editor.Init(this);
            TabStrip.AddTab(editor);

            Editors.Add(editor);
        }

        #endregion

        #region UPDATE

        public void SetMarking(Marking marking)
        {
            if ((Marking = marking) != null)
            {
                if (isVisible)
                    RefreshPanel();
                else
                    NeedRefreshOnVisible = true;
            }
        }
        public void UpdatePanel()
        {
            SetMinimumSize();
            Available = true;
            foreach (var editor in Editors)
                editor.UpdateEditor();
        }
        public override void RefreshPanel()
        {
            NeedRefreshOnVisible = false;

            Header.Text = Marking.PanelCaption;
            Header.Init(Marking.Type);
            TabStrip.SetVisible(Marking);
            TabStrip.ArrangeTabs();
            TabStrip.SelectedTab = -1;
            SelectEditor<LinesEditor>();
        }
        public void RefreshHeader() => Header.UpdateLayout();
        public void RefreshProperties() => CurrentEditor?.RefreshEditor();

        #endregion

        #region ONEVENTS

        protected override void OnSizeChanged()
        {
            if (Header != null)
                Header.width = width;
            if (TabStrip != null)
                TabStrip.width = width;
            if (CurrentEditor != null)
                SetEditorSize(CurrentEditor);

            base.OnSizeChanged();

            MakePixelPerfect();
        }
        private void SetEditorSize(Editor editor)
        {
            var position = new Vector2(0, TabStrip.relativePosition.y + TabStrip.height);
            editor.relativePosition = position;
            editor.size = size - position;
        }
        private void OnSelectedTabChanged(int index)
        {
            PrevEditor = CurrentEditor;
            CurrentEditor = SelectEditor(index);
        }

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
                SetEditorSize(selectEditor);
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
        public void SelectPrevEditor()
        {
            if (PrevEditor is Editor editor)
            {
                var editorIndex = Editors.IndexOf(editor);
                TabStrip.SelectedTab = editorIndex;
            }
        }

        #endregion

        #region ADD OBJECT

        private void AddObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, IDeletable
        {
            if (Editors.Find(e => e.GetType() == typeof(EditorType)) is EditorType editor)
            {
                editor.Add(item);
                CurrentEditor?.RefreshEditor();
            }
        }
        public void AddLine(MarkingLine line) => AddObject<LinesEditor, MarkingLine>(line);

        #endregion

        #region DELETE OBJECT

        private void DeleteObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, IDeletable
        {
            if (Editors.Find(e => e.GetType() == typeof(EditorType)) is EditorType editor)
            {
                editor.Delete(item);
                CurrentEditor?.RefreshEditor();
            }
        }

        public void DeleteLine(MarkingLine line) => DeleteObject<LinesEditor, MarkingLine>(line);
        public void DeleteCrosswalk(MarkingCrosswalk crosswalk) => DeleteObject<CrosswalksEditor, MarkingCrosswalk>(crosswalk);
        public void DeleteFiller(MarkingFiller filler) => DeleteObject<FillerEditor, MarkingFiller>(filler);

        #endregion

        #region EDIT OBJECT

        private EditorType SelectObject<EditorType, ItemType>(ItemType item)
            where EditorType : Editor, IEditor<ItemType>
            where ItemType : class, ISupport, IDeletable
        {
            if ((Marking.Support & item.Support) == 0)
                return null;

            Available = true;
            var editor = SelectEditor<EditorType>();
            editor?.Edit(item);
            return editor;
        }
        public void SelectPoint(MarkingEnterPoint point) => SelectObject<PointsEditor, MarkingEnterPoint>(point);
        public void SelectLine(MarkingLine line) => SelectObject<LinesEditor, MarkingLine>(line);
        public void SelectCrosswalk(MarkingCrosswalk crosswalk) => SelectObject<CrosswalksEditor, MarkingCrosswalk>(crosswalk);
        public void EditCrosswalk(MarkingCrosswalk crosswalk)
        {
            var editor = SelectObject<CrosswalksEditor, MarkingCrosswalk>(crosswalk);
            editor?.BorderSetup();
        }
        public void SelectFiller(MarkingFiller filler) => SelectObject<FillerEditor, MarkingFiller>(filler);

        private void EditTemplate<EditorType, TemplateType>(TemplateType template, bool editName)
            where EditorType : Editor, IEditor<TemplateType>, ITemplateEditor<TemplateType>
            where TemplateType : Template
        {
            var editor = SelectObject<EditorType, TemplateType>(template);
            if (editName && editor != null)
                editor.EditName();
        }

        public void EditStyleTemplate(StyleTemplate template, bool editName = true) => EditTemplate<StyleTemplateEditor, StyleTemplate>(template, editName);
        public void EditIntersectionTemplate(IntersectionTemplate template, bool editName = true) => EditTemplate<IntersectionTemplateEditor, IntersectionTemplate>(template, editName);

        #endregion

        #region ADDITIONAL

        public bool OnEscape() => CurrentEditor?.OnEscape() == true;

        public void Render(RenderManager.CameraInfo cameraInfo) => CurrentEditor?.Render(cameraInfo);

        #endregion
    }
}
