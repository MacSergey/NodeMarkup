﻿using ColossalFramework.UI;
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

        protected static float ItemsRatio => 0.3f;
        protected static float ContentRatio => 1f - ItemsRatio;

        public IntersectionMarkingTool Tool => SingletonTool<IntersectionMarkingTool>.Instance;
        protected bool NeedUpdate { get; set; }
        public ObjectType EditObject => ItemsPanel.SelectedObject;

        protected ItemsPanelType ItemsPanel { get; set; }
        protected AdvancedScrollablePanel ContentPanel { get; set; }
        protected CustomUILabel EmptyLabel { get; set; }

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
            atlas = TextureHelper.InGameAtlas;
            //backgroundSprite = "UnlockingItemBackground";
            backgroundSprite = "TextFieldPanel";
            color = new Color32(68, 68, 75, 255);

            ItemsPanel = AddUIComponent<ItemsPanelType>();
            ItemsPanel.atlas = TextureHelper.InGameAtlas;
            //ItemsPanel.backgroundSprite = "ScrollbarTrack";
            ItemsPanel.backgroundSprite = "TextFieldPanel";
            ItemsPanel.color = new Color32(142, 142, 142, 255);
            ItemsPanel.name = nameof(ItemsPanel);
            ItemsPanel.Init(this);
            ItemsPanel.OnSelectClick += OnItemSelect;
            ItemsPanel.OnDeleteClick += OnItemDelete;

            ContentPanel = AddUIComponent<AdvancedScrollablePanel>();
            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
            ContentPanel.atlas = TextureHelper.InGameAtlas;
            //ContentPanel.backgroundSprite = "UnlockingItemBackground";
            ContentPanel.backgroundSprite = "TextFieldPanel";
            ContentPanel.color = new Color32(68, 68, 75, 255);
            ContentPanel.name = nameof(ContentPanel);

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
        private void OnItemDelete(ObjectType editObject) => Tool.DeleteItem(editObject, OnObjectDelete);

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
        public readonly object editObject;
        public readonly UIComponent parent;
        private readonly Action refresh;
        private readonly Action<IPropertyInfo> addProperty;
        private readonly Action<PropertyCategoryInfo> addCategory;
        public readonly bool isTemplate;

        public EditorProvider(object editObject, UIComponent parent, Action<PropertyCategoryInfo> addCategory, Action<IPropertyInfo> addProperty, Action refresh, bool isTemplate)
        {
            this.editObject = editObject;
            this.parent = parent;
            this.addCategory = addCategory;
            this.addProperty = addProperty;
            this.refresh = refresh;
            this.isTemplate = isTemplate;
        }
        public EditorProvider(object editObject, UIComponent parent, bool isTemplate, Action<PropertyCategoryInfo> addCategory = null, Action<IPropertyInfo> addProperty = null, Action refresh = null) : this(editObject, parent, addCategory, addProperty, refresh, isTemplate) { }

        public void AddCategory(PropertyCategoryInfo category) => addCategory?.Invoke(category);
        public void AddProperty(IPropertyInfo property) => addProperty?.Invoke(property);
        public void Refresh() => refresh?.Invoke();

        public T GetItem<T>(string name) where T : EditorItem, IReusable => ComponentPool.Get<T>(parent, name);
        public void DestroyItem<T>(T item) where T : EditorItem, IReusable => ComponentPool.Free(item);
    }
}
