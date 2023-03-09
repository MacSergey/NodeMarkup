using ColossalFramework;
using ColossalFramework.UI;
using IMT.Manager;
using IMT.Tools;
using IMT.UI.Panel;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public interface IEditor<ObjectType>
        where ObjectType : class, IDeletable
    {
        void Add(ObjectType editObject);
        void Delete(ObjectType editObject);
        void Edit(ObjectType editObject);
        void RefreshEditor();
    }
    public abstract class Editor : CustomUIPanel, ISupport
    {
        public IntersectionMarkingToolPanel Panel { get; private set; }
        public Marking Marking => Panel.Marking;

        public abstract string Name { get; }
        public abstract Marking.SupportType Support { get; }
        public abstract string EmptyMessage { get; }

        public abstract bool AvailableItems { get; set; }
        public abstract bool AvailableContent { get; set; }

        public bool Active
        {
            get => enabled && isVisible;
            set
            {
                enabled = value;
                isVisible = value;

                if (value)
                    ActiveEditor();
            }
        }

        public void Init(IntersectionMarkingToolPanel panel) => Panel = panel;
        protected abstract void ActiveEditor();
        public abstract void UpdateEditor();
        public abstract void RefreshEditor();

        public virtual void Render(RenderManager.CameraInfo cameraInfo) { }
        public virtual bool OnEscape() => false;
    }
    public abstract class Editor<ItemsPanelType, ObjectType> : Editor, IEditor<ObjectType>
        where ItemsPanelType : AdvancedScrollablePanel, IItemPanel<ObjectType>
        where ObjectType : class, IDeletable
    {
        #region PROPERTIES

        private float ItemsSize
        {
            get

            {
                if (!Settings.AutoCollapseItemsPanel)
                    return Mathf.Min(width * 0.3f, 300f);
                else if ((ItemsPanel is IGroupItemPanel groupPanel) && groupPanel.GroupingEnable)
                    return 40f;
                else
                    return 34f;
            }
        }
        private float ContentSize => width - ItemsSize;

        public IntersectionMarkingTool Tool => SingletonTool<IntersectionMarkingTool>.Instance;
        protected bool NeedUpdate { get; set; }
        public ObjectType EditObject => ItemsPanel.SelectedObject;

        public ItemsPanelType ItemsPanel { get; protected set; }
        public AdvancedScrollablePanel ContentPanel { get; protected set; }
        protected CustomUILabel EmptyLabel { get; set; }
        private CustomUISprite Shadow { get; }

        public sealed override bool AvailableItems
        {
            get => ItemsPanel.isEnabled;
            set => ItemsPanel.SetAvailable(value);
        }
        public sealed override bool AvailableContent
        {
            get => ItemsPanel.isEnabled;
            set => ContentPanel.SetAvailable(value);
        }

        #endregion

        #region CONSTRUCTOR

        public Editor()
        {
            clipChildren = true;

            ItemsPanel = AddUIComponent<ItemsPanelType>();
            ItemsPanel.name = nameof(ItemsPanel);
            ItemsPanel.atlas = CommonTextures.Atlas;
            ItemsPanel.backgroundSprite = CommonTextures.PanelBig;
            ItemsPanel.foregroundSprite = CommonTextures.BorderTop;
            ItemsPanel.color = ItemsPanel.disabledColor = new Color32(99, 107, 107, 255);

            ItemsPanel.Content.autoLayoutPadding = new RectOffset(4, 4, 1, 2);
            ItemsPanel.Content.scrollPadding.top = 2;
            ItemsPanel.Content.scrollPadding.bottom = 2;

            ItemsPanel.Init(this);
            ItemsPanel.OnSelectClick += OnItemSelect;
            ItemsPanel.OnDeleteClick += OnItemDelete;
            ItemsPanel.eventMouseEnter += ItemsPanelEnter;
            ItemsPanel.eventMouseLeave += ItemsPanelLeave;

            Shadow = AddUIComponent<CustomUISprite>();
            Shadow.atlas = CommonTextures.Atlas;
            Shadow.spriteName = CommonTextures.PanelShadow;
            Shadow.color = new Color32(0, 0, 0, 224);
            Shadow.width = 20f;
            Shadow.isVisible = false;

            ContentPanel = AddUIComponent<AdvancedScrollablePanel>();
            ContentPanel.name = nameof(ContentPanel);
            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
            ContentPanel.atlas = CommonTextures.Atlas;
            ContentPanel.backgroundSprite = CommonTextures.PanelBig;
            ContentPanel.color = ContentPanel.disabledColor = new Color32(34, 38, 44, 25);
            ContentPanel.zOrder = 0;

            AddEmptyLabel();
        }

        private void AddEmptyLabel()
        {
            EmptyLabel = AddUIComponent<CustomUILabel>();
            EmptyLabel.textAlignment = UIHorizontalAlignment.Center;
            EmptyLabel.verticalAlignment = UIVerticalAlignment.Middle;
            EmptyLabel.padding = new RectOffset(10, 10, 0, 0);
            EmptyLabel.wordWrap = true;
            EmptyLabel.autoSize = false;

            SwitchEmptyMessage();
        }

        #endregion

        #region UPDATE ADD DELETE EDIT

        protected override void ActiveEditor()
        {
            if (NeedUpdate)
                UpdateEditor();
            else
                RefreshEditor();
        }
        public sealed override void UpdateEditor()
        {
            if (Active)
            {
                AvailableItems = true;
                AvailableContent = true;
                PlaceItems();

                var editObject = EditObject;
                ItemsPanel.SetObjects(GetObjects());
                ItemsPanel.SelectObject(editObject);

                SwitchEmptyMessage();

                NeedUpdate = false;
            }
            else
                NeedUpdate = true;
        }
        public sealed override void RefreshEditor()
        {
            PlaceItems();

            if (EditObject is ObjectType editObject)
                OnObjectUpdate(editObject);
            else
                ItemsPanel.SelectObject(null);
        }

        public virtual void Add(ObjectType addObject)
        {
            ItemsPanel.AddObject(addObject);
            SwitchEmptyMessage();
        }
        public virtual void Delete(ObjectType deleteObject)
        {
            ItemsPanel.DeleteObject(deleteObject);
            SwitchEmptyMessage();
        }
        public virtual void Edit(ObjectType editObject = null)
        {
            ItemsPanel.EditObject(editObject);
            SwitchEmptyMessage();
        }

        protected abstract IEnumerable<ObjectType> GetObjects();

        #endregion

        #region HANDLERS

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            PlaceItems();
        }
        private void PlaceItems()
        {
            ItemsPanel.size = new Vector2(ItemsSize, size.y);
            ItemsPanel.relativePosition = new Vector2(0, 0);

            Shadow.isVisible = false;
            Shadow.height = ItemsPanel.height;
            Shadow.relativePosition = ItemsPanel.relativePosition + new Vector3(ItemsPanel.width, 0f);

            ContentPanel.size = new Vector2(ContentSize, size.y);
            ContentPanel.relativePosition = new Vector2(ItemsSize, 0);

            EmptyLabel.size = new Vector2(ContentSize, size.y * 0.5f);
            EmptyLabel.relativePosition = ContentPanel.relativePosition;
        }
        protected void OnItemSelect(ObjectType editObject)
        {
            OnClear();

            if (editObject != null)
                OnObjectSelect(editObject);
        }
        private void OnItemDelete(ObjectType editObject) => Tool.DeleteItem(editObject, OnObjectDelete);

        private string AnimationId => $"{nameof(ItemsPanel)}{ItemsPanel.GetHashCode()}";
        private void ItemsPanelEnter(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!isEnabled || !Settings.AutoCollapseItemsPanel)
                return;

            ValueAnimator.Cancel(AnimationId);

            var current = ItemsPanel.width;
            var min = ItemsSize;
            var max = 250f;
            var time = 0.2f * (max - current) / (max - min);

            if (min < 250f && current != max)
            {
                Shadow.isVisible = true;
                ValueAnimator.Animate(AnimationId, SetItemsPanelWidth, new AnimatedFloat(current, max, time, EasingType.CubicEaseOut));
            }
        }
        private void ItemsPanelLeave(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!Settings.AutoCollapseItemsPanel)
                return;

            ValueAnimator.Cancel(AnimationId);

            var current = ItemsPanel.width;
            var min = ItemsSize;
            var max = 250f;
            var time = 0.2f * (current - min) / (max - min);

            if (current != min)
            {
                ValueAnimator.Animate(AnimationId, SetItemsPanelWidth, new AnimatedFloat(current, min, time, EasingType.CubicEaseOut), () => Shadow.isVisible = false);
            }
        }
        private void SetItemsPanelWidth(float width)
        {
            ItemsPanel.width = width;
            Shadow.relativePosition = ItemsPanel.relativePosition + new Vector3(width, 0f);
        }

        protected virtual void OnObjectSelect(ObjectType editObject) { }
        protected virtual void OnObjectUpdate(ObjectType editObject) { }
        protected virtual void OnObjectDelete(ObjectType editObject)
        {
            ItemsPanel.DeleteObject(editObject);
            SwitchEmptyMessage();
            RefreshEditor();
        }
        protected virtual void OnClear()
        {
            foreach (var component in ContentPanel.Content.components.ToArray())
                ComponentPool.Free(component);
        }

        #endregion

        #region ADDITIONAL

        protected void SwitchEmptyMessage()
        {
            if (ItemsPanel.IsEmpty)
            {
                EmptyLabel.isVisible = true;
                EmptyLabel.text = EmptyMessage;
            }
            else
                EmptyLabel.isVisible = false;
        }
        public void RefreshSelectedItem() => ItemsPanel.RefreshSelectedItem();

        #endregion
    }
    public abstract class SimpleEditor<ItemsPanelType, ObjectType> : Editor<ItemsPanelType, ObjectType>
        where ItemsPanelType : AdvancedScrollablePanel, IItemPanel<ObjectType>
        where ObjectType : class, IDeletable
    {
        protected PropertyGroupPanel PropertiesPanel { get; private set; }

        public SimpleEditor()
        {
            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 10, 10);
        }

        protected override void OnObjectSelect(ObjectType editObject)
        {
            PropertiesPanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content, nameof(ContentPanel));
            PropertiesPanel.StopLayout();
            OnFillPropertiesPanel(editObject);
            PropertiesPanel.StartLayout();
            PropertiesPanel.Init();
        }
        protected abstract void OnFillPropertiesPanel(ObjectType editObject);
        protected override void OnClear()
        {
            base.OnClear();
            PropertiesPanel = null;
        }
    }

    public readonly struct EditorProvider
    {
        public readonly IPropertyContainer editor;
        public readonly UIComponent parent;
        private readonly Action refresh;
        private readonly Action<IPropertyInfo> addProperty;
        private readonly Action<IPropertyCategoryInfo> addCategory;
        public readonly bool isTemplate;

        public EditorProvider(IPropertyContainer editor, UIComponent parent, Action<IPropertyCategoryInfo> addCategory, Action<IPropertyInfo> addProperty, Action refresh, bool isTemplate)
        {
            this.editor = editor;
            this.parent = parent;
            this.addCategory = addCategory;
            this.addProperty = addProperty;
            this.refresh = refresh;
            this.isTemplate = isTemplate;
        }
        public EditorProvider(IPropertyContainer editor, UIComponent parent, bool isTemplate, Action<IPropertyCategoryInfo> addCategory = null, Action<IPropertyInfo> addProperty = null, Action refresh = null) : this(editor, parent, addCategory, addProperty, refresh, isTemplate) { }

        public void AddCategory(IPropertyCategoryInfo category) => addCategory?.Invoke(category);
        public void AddProperty(IPropertyInfo property) => addProperty?.Invoke(property);
        public void Refresh() => refresh?.Invoke();

        public T GetItem<T>(string name) where T : UIComponent, IReusable => ComponentPool.Get<T>(parent, name);
        public void DestroyItem<T>(T item) where T : UIComponent, IReusable => ComponentPool.Free(item);
    }
}
