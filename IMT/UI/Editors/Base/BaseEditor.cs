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
        where ItemsPanelType : CustomUIScrollablePanel, IItemPanel<ObjectType>
        where ObjectType : class, IDeletable
    {
        #region PROPERTIES

        private float MinItemsSize
        {
            get

            {
                if (!Settings.AutoCollapseItemsPanel)
                    return Mathf.Min(width * 0.3f, 300f);
                else
                    return 38f;
            }
        }
        private float MaxItemsSize => 250f;
        private float ItemsSize => ItemsExpanded ? MaxItemsSize : MinItemsSize;
        private float ContentSize => width - MinItemsSize;

        public IntersectionMarkingTool Tool => SingletonTool<IntersectionMarkingTool>.Instance;
        protected bool NeedUpdate { get; set; }
        public ObjectType EditObject => ItemsPanel.SelectedObject;

        public ItemsPanelType ItemsPanel { get; protected set; }
        public CustomUIScrollablePanel ContentPanel { get; protected set; }
        protected CustomUILabel EmptyLabel { get; set; }
        private CustomUISprite ItemsShadow { get; }

        private BlurEffect ItemsBlur { get; }
        private BlurEffect ContentBlur { get; }

        private bool availableItems = true;
        public sealed override bool AvailableItems
        {
            get => availableItems;
            set
            {
                if (value != availableItems)
                {
                    availableItems = value;
                    ItemsBlur.opacity = value ? 0.0f : 1.0f;
                    ItemsBlur.isVisible = !value;
                }
            }
        }

        private bool availableContent = true;
        public sealed override bool AvailableContent
        {
            get => availableContent;
            set
            {
                if (value != availableContent)
                {
                    availableContent = value;
                    ContentBlur.opacity = value ? 0.0f : 1.0f;
                    ContentBlur.isVisible = !value;
                }
            }
        }


        private bool itemsExpanded;
        private bool ItemsExpanded
        {
            get => itemsExpanded;
            set
            {
                if(value != itemsExpanded && AvailableItems && Settings.AutoCollapseItemsPanel)
                {
                    itemsExpanded = value;
                    ItemsShadow.isVisible = value;
                    AvailableContent = !value;

                    ValueAnimator.Cancel(AnimationId);
                    var current = ItemsPanel.width;
                    var min = MinItemsSize;
                    var max = MaxItemsSize;

                    if(value)
                    {
                        ItemsPanel.Focus();
                        var time = 0.2f * (max - current) / (max - min);
                        if (min < max && current != max)
                            ValueAnimator.Animate(AnimationId, SetItemPanelWidth, new AnimatedFloat(current, max, time, EasingType.CubicEaseOut));
                    }
                    else
                    {
                        var time = 0.2f * (current - min) / (max - min);
                        if (current != min)
                            ValueAnimator.Animate(AnimationId, SetItemPanelWidth, new AnimatedFloat(current, min, time, EasingType.CubicEaseOut));
                    }
                }
            }
        }
        private string AnimationId => $"{nameof(ItemsPanel)}{ItemsPanel.GetHashCode()}";

        #endregion

        #region CONSTRUCTOR

        public Editor()
        {
            clipChildren = true;

            ItemsPanel = AddUIComponent<ItemsPanelType>();
            ItemsPanel.name = nameof(ItemsPanel);
            ItemsPanel.Atlas = CommonTextures.Atlas;
            ItemsPanel.BackgroundSprite = CommonTextures.PanelBig;
            ItemsPanel.ForegroundSprite = CommonTextures.BorderTop;
            ItemsPanel.color = ItemsPanel.disabledColor = UIStyle.ItemsBackground;
            ItemsPanel.canFocus = true;

            ItemsPanel.Padding = new RectOffset(0, 0, 2, 2);

            ItemsPanel.Init(this);
            ItemsPanel.OnSelectClick += OnItemSelect;
            ItemsPanel.OnDeleteClick += OnItemDelete;
            ItemsPanel.eventMouseEnter += ItemsPanelEnter;
            ItemsPanel.eventMouseLeave += ItemsPanelLeave;
            ItemsPanel.eventSizeChanged += (_, size) =>
            {
                ItemsBlur.size = size;
                ItemsShadow.height = size.y;
                ItemsShadow.relativePosition = ItemsPanel.relativePosition + new Vector3(size.x, 0f);
            };
            ItemsPanel.eventPositionChanged += (_, position) => ItemsBlur.position = position;

            ItemsBlur = AddUIComponent<BlurEffect>();
            ItemsBlur.position = Vector3.zero;
            ItemsBlur.opacity = 0f;
            ItemsBlur.size = ItemsPanel.size;

            ItemsShadow = AddUIComponent<CustomUISprite>();
            ItemsShadow.atlas = CommonTextures.Atlas;
            ItemsShadow.spriteName = CommonTextures.ShadowVertical;
            ItemsShadow.color = new Color32(0, 0, 0, 224);
            ItemsShadow.width = 20f;
            ItemsShadow.isVisible = false;

            ContentPanel = AddUIComponent<CustomUIScrollablePanel>();
            ContentPanel.name = nameof(ContentPanel);
            ContentPanel.Padding = new RectOffset(10, 10, 0, 0);
            ContentPanel.AutoLayout = AutoLayout.Vertical;
            ContentPanel.AutoLayoutSpace = 15;
            ContentPanel.AutoChildrenHorizontally = AutoLayoutChildren.Fill;
            ContentPanel.ScrollOrientation = UIOrientation.Vertical;
            ContentPanel.Atlas = CommonTextures.Atlas;
            ContentPanel.BackgroundSprite = CommonTextures.PanelBig;
            ContentPanel.color = ContentPanel.disabledColor = UIStyle.ContentBackground;
            ContentPanel.zOrder = 0;
            ContentPanel.eventSizeChanged += (_, size) => ContentBlur.size = size;
            ContentPanel.eventPositionChanged += (_, position) => ContentBlur.position = position;

            ContentPanel.ScrollbarSize = 12f;
            ContentPanel.Scrollbar.DefaultStyle();

            ContentBlur = AddUIComponent<BlurEffect>();
            ContentBlur.position = ContentPanel.position;
            ContentBlur.size = ContentPanel.size;
            ContentBlur.color = new Color32(188, 220, 245, 255);
            ContentBlur.opacity = 0f;
            ContentBlur.zOrder = 1;

            EmptyLabel = ContentPanel.AddUIComponent<CustomUILabel>();
            ContentPanel.Ignore(EmptyLabel, true);
            EmptyLabel.HorizontalAlignment = UIHorizontalAlignment.Center;
            EmptyLabel.VerticalAlignment = UIVerticalAlignment.Middle;
            EmptyLabel.Padding = new RectOffset(10, 10, 0, 0);
            EmptyLabel.WordWrap = true;
            EmptyLabel.AutoSize = AutoSize.None;
            EmptyLabel.relativePosition = Vector3.zero;

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
                AvailableContent = !ItemsExpanded;
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

            ContentPanel.size = new Vector2(ContentSize, size.y);
            ContentPanel.relativePosition = new Vector2(MinItemsSize, 0);

            EmptyLabel.size = new Vector2(ContentSize, size.y * 0.667f);
        }
        protected void OnItemSelect(ObjectType editObject)
        {
            OnClear();

            if (editObject != null)
                OnObjectSelect(editObject);
        }
        private void OnItemDelete(ObjectType editObject) => Tool.DeleteItem(editObject, OnObjectDelete);

        private void ItemsPanelEnter(UIComponent component, UIMouseEventParameter eventParam) => ItemsExpanded = true;
        private void ItemsPanelLeave(UIComponent component, UIMouseEventParameter eventParam) => ItemsExpanded = false;
        private void SetItemPanelWidth(float width)
        {
            ItemsPanel.width = width;
            ContentBlur.opacity = width / MaxItemsSize;
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
            foreach (var component in ContentPanel.components.ToArray())
            {
                if (component != ContentPanel.Scrollbar && component != EmptyLabel)
                    ComponentPool.Free(component);
            }
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
        where ItemsPanelType : CustomUIScrollablePanel, IItemPanel<ObjectType>
        where ObjectType : class, IDeletable
    {
        protected PropertyGroupPanel PropertiesPanel { get; private set; }

        public SimpleEditor()
        {
            ContentPanel.Padding = new RectOffset(10, 10, 10, 10);
        }

        protected override void OnObjectSelect(ObjectType editObject)
        {
            PropertiesPanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel, "PropertyPanel");
            PropertiesPanel.PanelStyle = UIStyle.Default.PropertyPanel;
            PropertiesPanel.PauseLayout(() => OnFillPropertiesPanel(editObject));
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
