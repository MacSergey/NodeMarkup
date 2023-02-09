using IMT.Manager;
using ModsCommon.UI;
using System.Collections.Generic;
using System.Linq;

namespace IMT.UI.Editors
{
    public interface IPropertyEditor
    {
        Dictionary<string, List<IPropertyInfo>> PropertyInfos { get; }
        Dictionary<string, IPropertyCategoryInfo> CategoryInfos { get; }
        Dictionary<string, CategoryItem> CategoryItems { get; }
        List<EditorItem> StyleProperties { get; }

        UIAutoLayoutPanel MainPanel { get; }
        object EditObject { get; }
        Style Style { get; }
        bool IsTemplate { get; }
    }

    public static class PropertyEditorHelper
    {
        public static void AddProperties(this IPropertyEditor editor)
        {
            editor.ClearProperties();

            var provider = new EditorProvider(editor.EditObject, editor.MainPanel, AddCategoty, AddProperty, editor.RefreshProperties, editor.IsTemplate);
            editor.Style.GetUIComponents(provider);
            editor.Style.GetUICategories(provider);

            editor.MainPanel.StopLayout();
            {
                foreach (var category in editor.PropertyInfos)
                {
                    category.Value.Sort(PropertyInfoComparer.Instance);

                    if(!editor.CategoryInfos.ContainsKey(category.Key))
                        editor.CategoryInfos[category.Key] = new PropertyCategoryInfo<DefaultPropertyCategoryPanel>(category.Key, category.Key, false);
                }

                foreach (var categoryInfo in editor.CategoryInfos.Values)
                {
                    if (editor.PropertyInfos.TryGetValue(categoryInfo.Name, out var propertyInfos))
                    {
                        if (string.IsNullOrEmpty(categoryInfo.Name))
                        {
                            var categoryProvider = new EditorProvider(editor.EditObject, editor.MainPanel, editor.IsTemplate, refresh: editor.RefreshProperties);

                            foreach (var propertyInfo in propertyInfos)
                                propertyInfo.Create(categoryProvider);
                        }
                        else
                        {
                            var categoryItem = categoryInfo.Create(provider);
                            var categoryPanel = categoryItem.CategoryPanel;
                            editor.CategoryItems[categoryInfo.Name] = categoryItem;

                            var categoryProvider = new EditorProvider(editor.EditObject, categoryPanel, editor.IsTemplate, refresh: editor.RefreshProperties);

                            categoryPanel.StopLayout();
                            {
                                foreach (var propertyInfo in propertyInfos)
                                {
                                    var protertyItem = propertyInfo.Create(categoryProvider);
                                    editor.StyleProperties.Add(protertyItem);
                                }
                            }
                            categoryPanel.StartLayout();
                        }
                    }
                }
            }
            editor.MainPanel.StartLayout();

            editor.RefreshProperties();

            void AddProperty(IPropertyInfo propertyInfo)
            {
                if (!editor.PropertyInfos.TryGetValue(propertyInfo.Category.Name, out var list))
                {
                    list = new List<IPropertyInfo>();
                    editor.PropertyInfos[propertyInfo.Category.Name] = list;
                }
                list.Add(propertyInfo);
            }
            void AddCategoty(IPropertyCategoryInfo categoryInfo)
            {
                editor.CategoryInfos[categoryInfo.Name] = categoryInfo;
            }
        }
        public static void RefreshProperties(this IPropertyEditor editor)
        {
            editor.MainPanel.StopLayout();
            {
                foreach (var category in editor.PropertyInfos)
                {
                    if (string.IsNullOrEmpty(category.Key))
                    {
                        var categoryProvider = new EditorProvider(editor.EditObject, editor.MainPanel, editor.IsTemplate);

                        foreach (var propertyInfo in category.Value)
                            propertyInfo.Refresh(categoryProvider);
                    }
                    else if (editor.CategoryItems.TryGetValue(category.Key, out var categoryItem) && categoryItem.CategoryPanel is PropertyGroupPanel categoryPanel)
                    {
                        var categoryProvider = new EditorProvider(editor.EditObject, categoryPanel, editor.IsTemplate);

                        categoryPanel.StopLayout();
                        {
                            var visibleCount = 0;
                            foreach (var propertyInfo in category.Value)
                            {
                                propertyInfo.Refresh(categoryProvider);
                                if (!propertyInfo.IsHidden)
                                    visibleCount += 1;
                            }
                            categoryPanel.isVisible = visibleCount > 0;
                        }
                        categoryPanel.StartLayout();
                    }
                }
            }
            editor.MainPanel.StartLayout();
        }

        public static void ClearProperties(this IPropertyEditor editor)
        {
            var provider = new EditorProvider(editor.EditObject, editor.MainPanel, true);

            editor.MainPanel.StopLayout();
            {
                foreach (var category in editor.PropertyInfos)
                {
                    if (string.IsNullOrEmpty(category.Key))
                    {
                        var categoryProvider = new EditorProvider(editor.EditObject, editor.MainPanel, editor.IsTemplate);
                        foreach (var propertyInfo in category.Value)
                            propertyInfo.Destroy(categoryProvider);
                    }
                    else if(editor.CategoryItems.TryGetValue(category.Key, out var categoryItem) && categoryItem.CategoryPanel is PropertyGroupPanel categoryPanel)
                    {
                        var categoryProvider = new EditorProvider(editor.EditObject, categoryPanel, editor.IsTemplate);

                        categoryPanel.StopLayout();
                        {
                            foreach (var propertyInfo in category.Value)
                                propertyInfo.Destroy(categoryProvider);
                        }
                        categoryPanel.StartLayout();
                    }
                }

                foreach (var categoryPanel in editor.CategoryItems.Values)
                    ComponentPool.Free(categoryPanel);
            }
            editor.MainPanel.StartLayout();

            editor.PropertyInfos.Clear();
            editor.CategoryItems.Clear();
            editor.StyleProperties.Clear();
        }
    }
}
