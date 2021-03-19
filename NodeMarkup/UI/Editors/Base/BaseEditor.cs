using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Panel;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public interface IEditor<ObjectType>
        where ObjectType : class, IDeletable
    {
        void Add(ObjectType editObject);
        void Delete(ObjectType editObject);
        void Edit(ObjectType editObject);
        void RefreshEditor();
    }
    public abstract class Editor : UIPanel
    {
        public static string WheelTip => Settings.ShowToolTip ? NodeMarkup.Localize.FieldPanel_ScrollWheel : string.Empty;

        public NodeMarkupPanel Panel { get; private set; }

        public abstract string Name { get; }
        public abstract Type SupportType { get; }
        public abstract string EmptyMessage { get; }

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

        public void Init(NodeMarkupPanel panel) => Panel = panel;
        protected abstract void ActiveEditor();
        public abstract void UpdateEditor();
        public abstract void RefreshEditor();

        public virtual void Render(RenderManager.CameraInfo cameraInfo) { }
        public virtual bool OnShortcut(Event e) => false;
        public virtual bool OnEscape() => false;
    }
    public abstract class Editor<ItemsPanelType, ObjectType> : Editor, IEditor<ObjectType>
        where ItemsPanelType : AdvancedScrollablePanel, IItemPanel<ObjectType>
        where ObjectType : class, IDeletable
    {
        #region PROPERTIES

        protected static float ItemsRatio => 0.3f;
        protected static float ContentRatio => 1f - ItemsRatio;

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        protected Markup Markup => Panel.Markup;
        protected bool NeedUpdate { get; set; }
        public ObjectType EditObject => ItemsPanel.SelectedObject;

        protected ItemsPanelType ItemsPanel { get; set; }
        protected AdvancedScrollablePanel ContentPanel { get; set; }
        protected UILabel EmptyLabel { get; set; }

        public bool AvailableItems { set => ItemsPanel.SetAvailable(value); }
        public bool AvailableContent { set => ContentPanel.SetAvailable(value); }

        #endregion

        #region CONSTRUCTOR

        public Editor()
        {
            clipChildren = true;
            atlas = TextureHelper.InGameAtlas;
            backgroundSprite = "UnlockingItemBackground";

            ItemsPanel = AddUIComponent<ItemsPanelType>();
            ItemsPanel.atlas = TextureHelper.InGameAtlas;
            ItemsPanel.backgroundSprite = "ScrollbarTrack";
            ItemsPanel.OnSelectClick += OnItemSelect;
            ItemsPanel.OnDeleteClick += OnItemDelete;

            ContentPanel = AddUIComponent<AdvancedScrollablePanel>();
            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
            ContentPanel.atlas = TextureHelper.InGameAtlas;
            ContentPanel.backgroundSprite = "UnlockingItemBackground";

            AddEmptyLabel();
        }

        private void AddEmptyLabel()
        {
            EmptyLabel = AddUIComponent<UILabel>();
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

                var editObject = EditObject;
                ItemsPanel.Init(GetObjects());
                ItemsPanel.SelectObject(editObject);

                SwitchEmptyMessage();

                NeedUpdate = false;
            }
            else
                NeedUpdate = true;
        }
        public sealed override void RefreshEditor() 
        {
            if (EditObject is ObjectType editObject)
                OnObjectUpdate(editObject);
            else
                ItemsPanel.SelectObject(null);
        }

        public virtual void Add(ObjectType deleteObject)
        {
            ItemsPanel.AddObject(deleteObject);
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

            ItemsPanel.size = new Vector2(size.x * ItemsRatio, size.y);
            ItemsPanel.relativePosition = new Vector2(0, 0);

            ContentPanel.size = new Vector2(size.x * ContentRatio, size.y);
            ContentPanel.relativePosition = new Vector2(size.x * ItemsRatio, 0);

            EmptyLabel.size = new Vector2(size.x * ContentRatio, size.y / 2);
            EmptyLabel.relativePosition = ContentPanel.relativePosition;
        }
        protected void OnItemSelect(ObjectType editObject)
        {
            OnClear();

            if (editObject != null)
                OnObjectSelect(editObject);
        }
        private void OnItemDelete(ObjectType editObject)
        {
            Tool.DeleteItem(editObject, () => OnObjectDelete(editObject));
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
            PropertiesPanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
            PropertiesPanel.StopLayout();
            OnFillPropertiesPanel(editObject);
            PropertiesPanel.StartLayout();
            PropertiesPanel.Init();
        }
        protected abstract void OnFillPropertiesPanel(ObjectType editObject);
    }


    //public abstract class Editor : UIPanel
    //{
    //    protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
    //    public NodeMarkupPanel Panel { get; private set; }
    //    protected Markup Markup => Panel.Markup;
    //    public abstract Type SupportType { get; }

    //    protected AdvancedScrollablePanel ItemsPanel { get; set; }
    //    protected AdvancedScrollablePanel ContentPanel { get; set; }

    //    public bool AvailableItems { set => ItemsPanel.SetAvailable(value); }
    //    public bool AvailableContent { set => ContentPanel.SetAvailable(value); }

    //    protected static float ItemsRatio => 0.3f;
    //    protected static float ContentRatio => 1f - ItemsRatio;

    //    protected UILabel EmptyLabel { get; set; }

    //    public abstract string Name { get; }
    //    public abstract string EmptyMessage { get; }

    //    public static string WheelTip => Settings.ShowToolTip ? NodeMarkup.Localize.FieldPanel_ScrollWheel : string.Empty;

    //    public virtual bool Active
    //    {
    //        set
    //        {
    //            enabled = value;
    //            isVisible = value;
    //        }
    //    }

    //    public Editor()
    //    {
    //        clipChildren = true;
    //        atlas = TextureHelper.InGameAtlas;
    //        backgroundSprite = "UnlockingItemBackground";

    //        ItemsPanel = AddUIComponent<AdvancedScrollablePanel>();
    //        ItemsPanel.atlas = TextureHelper.InGameAtlas;
    //        ItemsPanel.backgroundSprite = "ScrollbarTrack";

    //        ContentPanel = AddUIComponent<AdvancedScrollablePanel>();
    //        ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
    //        ContentPanel.atlas = TextureHelper.InGameAtlas;
    //        ContentPanel.backgroundSprite = "UnlockingItemBackground";

    //        AddEmptyLabel();
    //    }

    //    private void AddEmptyLabel()
    //    {
    //        EmptyLabel = AddUIComponent<UILabel>();
    //        EmptyLabel.textAlignment = UIHorizontalAlignment.Center;
    //        EmptyLabel.verticalAlignment = UIVerticalAlignment.Middle;
    //        EmptyLabel.padding = new RectOffset(10, 10, 0, 0);
    //        EmptyLabel.wordWrap = true;
    //        EmptyLabel.autoSize = false;

    //        SwitchEmpty();
    //    }
    //    protected void SwitchEmpty()
    //    {
    //        if (ItemsPanel.Content.components.Any())
    //            EmptyLabel.isVisible = false;
    //        else
    //        {
    //            EmptyLabel.isVisible = true;
    //            EmptyLabel.text = EmptyMessage;
    //        }
    //    }

    //    public virtual void Init(NodeMarkupPanel panel) => Panel = panel;
    //    public virtual void UpdateEditor()
    //    {
    //        AvailableItems = true;
    //        AvailableContent = true;
    //    }
    //    protected virtual void ClearItems() { }
    //    protected virtual void ClearContent() { }
    //    protected virtual void FillItems() { }
    //    public virtual void Select(int index) { }
    //    public virtual void Render(RenderManager.CameraInfo cameraInfo) { }
    //    public virtual bool OnShortcut(Event e) => false;
    //    public virtual bool OnEscape() => false;

    //    protected abstract void ItemClick(UIComponent component, UIMouseEventParameter eventParam);
    //    protected abstract void ItemHover(UIComponent component, UIMouseEventParameter eventParam);
    //    protected abstract void ItemLeave(UIComponent component, UIMouseEventParameter eventParam);
    //}
    //public abstract class Editor<EditableItemType, EditableObject, ItemIcon> : Editor, IEditor<EditableObject>
    //    where EditableItemType : EditItem<EditableObject, ItemIcon>
    //    where ItemIcon : UIComponent
    //    where EditableObject : class, IDeletable
    //{
    //    protected PropertyGroupPanel PropertiesPanel { get; private set; }
    //    protected virtual bool UsePropertiesPanel => true;

    //    EditableItemType _selectItem;

    //    protected EditableItemType HoverItem { get; set; }
    //    protected bool IsHoverItem => HoverItem != null;
    //    protected EditableItemType SelectItem
    //    {
    //        get => _selectItem;
    //        set
    //        {
    //            if (_selectItem != null)
    //                _selectItem.IsSelect = false;

    //            _selectItem = value;

    //            if (_selectItem != null)
    //                _selectItem.IsSelect = true;
    //        }
    //    }
    //    public EditableObject EditObject => SelectItem?.Object;

    //    public Editor()
    //    {
    //        if (UsePropertiesPanel)
    //            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 10, 10);
    //    }

    //    public override void Update()
    //    {
    //        base.Update();

    //        if (SelectItem is EditableItemType editableItem)
    //            editableItem.IsSelect = true;
    //    }
    //    protected override void OnSizeChanged()
    //    {
    //        base.OnSizeChanged();

    //        ItemsPanel.size = new Vector2(size.x * ItemsRatio, size.y);
    //        ItemsPanel.relativePosition = new Vector2(0, 0);

    //        ContentPanel.size = new Vector2(size.x * ContentRatio, size.y);
    //        ContentPanel.relativePosition = new Vector2(size.x * ItemsRatio, 0);

    //        EmptyLabel.size = new Vector2(size.x * ContentRatio, size.y / 2);
    //        EmptyLabel.relativePosition = ContentPanel.relativePosition;
    //    }
    //    public virtual EditableItemType AddItem(EditableObject editableObject)
    //    {
    //        var item = GetItem(ItemsPanel.Content);
    //        InitItem(item, editableObject);

    //        SwitchEmpty();

    //        return item;
    //    }
    //    protected virtual EditableItemType GetItem(UIComponent parent)
    //    {
    //        var newItem = ComponentPool.Get<EditableItemType>(parent);
    //        newItem.width = parent.width;
    //        return newItem;
    //    }
    //    protected void InitItem(EditableItemType item, EditableObject editableObject)
    //    {
    //        item.Init(editableObject);
    //        item.eventClick += ItemClick;
    //        item.eventMouseEnter += ItemHover;
    //        item.eventMouseLeave += ItemLeave;
    //        item.OnDelete += DeleteItem;
    //    }

    //    protected void DeleteItem(EditItem<EditableObject, ItemIcon> deleteItem)
    //    {
    //        if (deleteItem is EditableItemType item)
    //            Tool.DeleteItem(item.Object, () => OnDeleteItem(item));
    //    }
    //    protected virtual void OnDeleteItem(EditableItemType item)
    //    {
    //        OnObjectDelete(item.Object);
    //        var isSelect = item == SelectItem;
    //        var index = Math.Min(item.parent.components.IndexOf(item), item.parent.components.Count - 2);
    //        DeleteItem(item);

    //        if (!isSelect)
    //            return;

    //        ClearContent();
    //        SelectItem = null;
    //        Select(index);
    //    }

    //    protected override void ClearItems() => ClearItems(ItemsPanel.Content);
    //    protected void ClearItems(UIComponent parent)
    //    {
    //        SelectItem = null;

    //        var components = parent.components.ToArray();
    //        foreach (var component in components)
    //        {
    //            if (component is EditableItemType item)
    //                DeleteItem(item);
    //            else
    //                DeleteUIComponent(component);
    //        }
    //    }
    //    protected virtual void DeleteItem(EditableItemType item)
    //    {
    //        if (HoverItem == item)
    //            HoverItem = null;

    //        item.eventClick -= ItemClick;
    //        item.eventMouseEnter -= ItemHover;
    //        item.eventMouseLeave -= ItemLeave;
    //        item.OnDelete -= DeleteItem;
    //        ComponentPool.Free(item);

    //        SwitchEmpty();
    //    }
    //    protected void DeleteUIComponent(UIComponent component)
    //    {
    //        ItemsPanel.Content.RemoveUIComponent(component);
    //        Destroy(component);
    //    }
    //    protected virtual EditableItemType GetItem(EditableObject editObject) => ItemsPanel.Content.components.OfType<EditableItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
    //    public virtual void Edit(EditableObject selectObject = null)
    //    {
    //        base.UpdateEditor();

    //        var editObject = EditObject;

    //        if (selectObject != null && selectObject == editObject)
    //        {
    //            OnObjectUpdate();
    //            return;
    //        }

    //        ClearItems();
    //        if (Markup != null)
    //        {
    //            ItemsPanel.StopLayout();
    //            FillItems();
    //            ItemsPanel.StartLayout();
    //        }

    //        if (selectObject != null && GetItem(selectObject) is EditableItemType selectItem)
    //            Select(selectItem);
    //        else if (editObject != null && GetItem(editObject) is EditableItemType editItem)
    //        {
    //            SelectItem = editItem;
    //            ScrollTo(SelectItem);
    //            OnObjectUpdate();
    //        }
    //        else
    //        {
    //            SelectItem = null;
    //            ClearContent();
    //            Select(0);
    //        }

    //        SwitchEmpty();
    //    }
    //    public override void UpdateEditor() => Edit(null);

    //    protected override void ClearContent()
    //    {
    //        if (UsePropertiesPanel)
    //            ClearContent(PropertiesPanel);

    //        ClearContent(ContentPanel.Content);

    //        OnClear();
    //    }
    //    private void ClearContent(UIComponent parent)
    //    {
    //        if (parent == null)
    //            return;

    //        var components = parent.components.ToArray();
    //        foreach (var component in components)
    //        {
    //            if (component != EmptyLabel)
    //                ComponentPool.Free(component);
    //        }
    //    }

    //    protected override void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => ItemClick((EditableItemType)component);
    //    protected virtual void ItemClick(EditableItemType item)
    //    {
    //        ContentPanel.StopLayout();
    //        ClearContent();
    //        SelectItem = item;

    //        if (UsePropertiesPanel)
    //        {
    //            PropertiesPanel = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
    //            PropertiesPanel.StopLayout();
    //        }
    //            OnObjectSelect();
    //        if (UsePropertiesPanel)
    //        {
    //            PropertiesPanel.StartLayout();
    //            PropertiesPanel.Init();
    //        }

    //        ContentPanel.StartLayout();
    //    }
    //    protected override void ItemHover(UIComponent component, UIMouseEventParameter eventParam)
    //    {
    //        if (ItemsPanel.isEnabled && component is EditableItemType editableItem)
    //            ItemHover(editableItem);
    //    }
    //    protected virtual void ItemHover(EditableItemType editableItem) => HoverItem = editableItem;
    //    protected override void ItemLeave(UIComponent component, UIMouseEventParameter eventParam) => ItemLeave();
    //    protected virtual void ItemLeave() => HoverItem = null;

    //    protected virtual void OnObjectSelect() { }
    //    protected virtual void OnClear() { }
    //    protected virtual void OnObjectDelete(EditableObject editableObject) { }
    //    protected virtual void OnObjectUpdate() { }
    //    public override void Select(int index) => Select(ItemsPanel.Content, index);
    //    protected void Select(UIComponent parent, int index)
    //    {
    //        if (index >= 0 && parent.components.Count > index && parent.components[index] is EditableItemType item)
    //            Select(item);
    //    }
    //    public virtual void Select(EditableItemType item)
    //    {
    //        ItemClick(item);
    //        ScrollTo(item);
    //    }
    //    public virtual void ScrollTo(EditableItemType item)
    //    {
    //        ItemsPanel.Content.ScrollToBottom();
    //        ItemsPanel.Content.ScrollIntoView(item);
    //    }
    //    protected virtual void RefreshItems()
    //    {
    //        foreach (var item in ItemsPanel.Content.components.OfType<EditableItemType>())
    //            item.Refresh();
    //    }
    //}
}
