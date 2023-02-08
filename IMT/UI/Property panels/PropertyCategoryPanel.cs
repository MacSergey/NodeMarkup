using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class CategoryItem : UIAutoLayoutPanel, IReusable
    {
        bool IReusable.InCache { get; set; }
        public PropertyGroupPanel CategoryPanel { get; private set; }

        public CategoryItem()
        {
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(3, 3, 3, 3);
            verticalSpacing = 3;
            autoFitChildrenVertically = true;
        }

        public TypePanel Init<TypePanel>(string name)
            where TypePanel : PropertyGroupPanel
        {
            if (CategoryPanel != null)
                ComponentPool.Free(CategoryPanel);

            CategoryPanel = ComponentPool.Get<TypePanel>(this, name);
            return (TypePanel)CategoryPanel;
        }

        void IReusable.DeInit()
        {
            ComponentPool.Free(CategoryPanel);
            CategoryPanel = null;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (CategoryPanel != null)
                CategoryPanel.width = width - autoLayoutPadding.horizontal;
        }
    }
    public abstract class PropertyCategoryPanel<TypeHeader> : PropertyGroupPanel
        where TypeHeader : BaseCategoryHeaderPanel
    {
        private static Dictionary<string, bool> ExpandList { get; } = new Dictionary<string, bool>();
        protected override UITextureAtlas Atlas => IMTTextures.Atlas;
        protected override string BackgroundSprite => IMTTextures.ButtonWhiteBorder;

        private TypeHeader Header { get; set; }

        public bool? IsExpand
        {
            get
            {
                if (ExpandList.TryGetValue(Category.name, out var isExpand))
                    return isExpand;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    ExpandList.Remove(Category.name);
                }
                else
                {
                    ExpandList[Category.name] = value.Value;
                    Header.IsExpand = value.Value;

                    foreach (var item in components)
                    {
                        if (item is not TypeHeader && item is EditorItem editorItem)
                            editorItem.IsCollapsed = !value.Value;
                    }
                }
            }
        }

        public PropertyCategoryInfo Category { get; private set; }

        public PropertyCategoryPanel()
        {
            verticalSpacing = 3;
            padding = new RectOffset(0, 0, 2, 0);
            autoLayoutPadding = new RectOffset(2, 2, 0, 0);
        }

        public void Init(PropertyCategoryInfo category)
        {
            Category = category;

            Header = ComponentPool.Get<TypeHeader>(this, nameof(Header));
            Header.Init();
            Header.OnExpand += () => IsExpand = !IsExpand;
            Header.Category = category.text;

            IsExpand = IsExpand ?? category.isExpand;

            base.Init();
        }

        public override void DeInit()
        {
            base.DeInit();
            Header = null;
        }

        protected override void OnComponentAdded(UIComponent child)
        {
            base.OnComponentAdded(child);

            if (child is not TypeHeader && child is EditorItem item)
                item.IsCollapsed = IsExpand != true;
        }
    }
    public class DefaultPropertyCategoryPanel : PropertyCategoryPanel<BaseCategoryHeaderPanel> { }

    public class BaseCategoryHeaderPanel : BaseHeaderPanel<CategoryHeaderContent>
    {
        public event Action OnExpand
        {
            add => Content.OnExpand += value;
            remove => Content.OnExpand -= value;
        }

        protected override float DefaultHeight => 24f;
        protected virtual Color32 Color => new Color32(177, 195, 94, 255);
        protected virtual string BackgroundSprite => "ButtonWhite";
        protected virtual UITextureAtlas Atlas => TextureHelper.InGameAtlas;

        public string Category
        {
            get => Content.Category;
            set => Content.Category = value;
        }
        public bool IsExpand { set => Content.IsExpand = value; }

        public BaseCategoryHeaderPanel()
        {
            atlas = Atlas;
            backgroundSprite = BackgroundSprite;
            color = Color;
        }

        public override void DeInit()
        {
            Content.DeInit();
        }
    }

    public class CategoryHeaderContent : HeaderContent
    {
        public event Action OnExpand;

        protected CustomUIButton ExpandButton { get; set; }
        protected CustomUILabel NameLabel { get; set; }

        public string Category
        {
            get => NameLabel.text;
            set => NameLabel.text = value;
        }
        public bool IsExpand { set => ExpandButton.normalBgSprite = value ? IMTTextures.ListItemCollapse : IMTTextures.ListItemExpand; }

        public CategoryHeaderContent()
        {
            AddCollapseButton();
            AddLabel();
        }

        private void AddCollapseButton()
        {
            ExpandButton = AddUIComponent<CustomUIButton>();
            ExpandButton.atlas = IMTTextures.Atlas;
            ExpandButton.size = new Vector2(20, 20);
            ExpandButton.eventClick += ExpandClick;
        }
        private void AddLabel()
        {
            NameLabel = AddUIComponent<CustomUILabel>();
            NameLabel.textScale = 0.8f;
            NameLabel.autoSize = true;
            NameLabel.padding = new RectOffset(0, 0, 2, 0);
            NameLabel.eventClick += ExpandClick;
        }

        public void DeInit()
        {
            OnExpand = null;
            IsExpand = false;
            Category = string.Empty;
        }

        private void ExpandClick(UIComponent component, UIMouseEventParameter eventParam) => OnExpand?.Invoke();
    }
}
