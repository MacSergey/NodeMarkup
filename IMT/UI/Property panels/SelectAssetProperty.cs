﻿using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI
{
    public  interface ISelectPrefabProperty<PrefabType>
        where PrefabType : PrefabInfo
    {
        public event Action<PrefabType> OnValueChanged;

        public abstract PrefabType Prefab { get; set; }
        public abstract string RawName { get; set; }
        public abstract Func<PrefabType, bool> SelectPredicate { get; set; }
        public abstract Func<PrefabType, PrefabType, int> SortPredicate { get; set; }

    }
    public abstract class SelectPrefabProperty<PrefabType, EntityType, PopupType, DropDownType> : EditorPropertyPanel, IReusable, ISelectPrefabProperty<PrefabType>
        where PrefabType : PrefabInfo
        where EntityType : PrefabEntity<PrefabType>
        where PopupType : ObjectPopup<PrefabType, EntityType>
        where DropDownType : PrefabDropDown<PrefabType, EntityType, PopupType>
    {
        public event Action<PrefabType> OnValueChanged;
        private DropDownType DropDown { get; set; }

        public PrefabType Prefab
        {
            get => DropDown.SelectedObject;
            set => DropDown.SelectedObject = value;
        }
        public string RawName
        {
            get => DropDown.RawName;
            set => DropDown.RawName = value;
        }
        public Func<PrefabType, bool> SelectPredicate
        {
            get => DropDown.SelectPredicate;
            set => DropDown.SelectPredicate = value;
        }
        public Func<PrefabType, PrefabType, int> SortPredicate
        {
            get => DropDown.SortPredicate;
            set => DropDown.SortPredicate = (objA, objB) =>
            {
                var isFavoriteA = SingletonManager<FavoritePrefabsManager>.Instance.IsFavorite(objA.name);
                var isFavoriteB = SingletonManager<FavoritePrefabsManager>.Instance.IsFavorite(objB.name);

                if (isFavoriteA != isFavoriteB)
                    return isFavoriteB.CompareTo(isFavoriteA);
                else
                    return value(objA, objB);
            };
        }

        protected override void FillContent()
        {
            DropDown = Content.AddUIComponent<DropDownType>();
            DropDown.name = nameof(DropDown);
            DropDown.DropDownDefaultStyle();
            DropDown.size = new Vector2(230f, 50f);
            DropDown.ScaleFactor = 20f / DropDown.height;
            DropDown.OnSelectObject += ValueChanged;
        }
        public override void Init()
        {
            DropDown.Clear();
            var count = PrefabCollection<PrefabType>.LoadedCount();
            for (uint i = 0; i < count; i += 1)
                DropDown.AddItem(PrefabCollection<PrefabType>.GetLoaded(i));
        }
        public override void DeInit()
        {
            base.DeInit();
            OnValueChanged = null;
            DropDown.Clear();
            SelectPredicate = null;
        }

        protected void ValueChanged(PrefabType prefab) => OnValueChanged?.Invoke(prefab);

        public override void SetStyle(ControlStyle style)
        {
            DropDown.DropDownStyle = style.DropDown;
        }
    }

    public class SelectPropProperty : SelectPrefabProperty<PropInfo, PropEntity, PropPopup, PropDropDown> { }
    public class SelectTreeProperty : SelectPrefabProperty<TreeInfo, TreeEntity, TreePopup, TreeDropDown> { }
    public class SelectNetworkProperty : SelectPrefabProperty<NetInfo, NetEntity, NetPopup, NetDropDown> { }

    public abstract class PrefabDropDown<PrefabType, EntityType, PopupType> : SelectItemDropDown<PrefabType, EntityType, PopupType>
        where PrefabType : PrefabInfo
        where EntityType : PrefabEntity<PrefabType>
        where PopupType : ObjectPopup<PrefabType, EntityType>
    {
        public string RawName
        {
            get => Entity.RawName;
            set => Entity.RawName = value;
        }
        public Func<PrefabType, bool> SelectPredicate { get; set; }
        public Func<PrefabType, PrefabType, int> SortPredicate { get; set; }
        protected override Func<PrefabType, bool> Selector => SelectPredicate;
        protected override Func<PrefabType, PrefabType, int> Sorter => SortPredicate;

        public PrefabDropDown()
        {
            Entity.ShowFavorite = false;
        }
        protected override void SetPopupStyle()
        {
            Popup.PopupDefaultStyle(50f);
            if (DropDownStyle != null)
                Popup.PopupStyle = DropDownStyle;
        }

        protected override void InitPopup()
        {
            Popup.MaximumSize = new Vector2(width, 700f);
            Popup.width = width;
            Popup.MaxVisibleItems = 10;
            base.InitPopup();
        }
    }
    public class PropDropDown : PrefabDropDown<PropInfo, PropEntity, PropPopup> { }
    public class TreeDropDown : PrefabDropDown<TreeInfo, TreeEntity, TreePopup> { }
    public class NetDropDown : PrefabDropDown<NetInfo, NetEntity, NetPopup> { }

    public abstract class PrefabPopup<PrefabType, EntityType> : SearchPopup<PrefabType, EntityType>
        where PrefabType : PrefabInfo
        where EntityType : PrefabEntity<PrefabType>
    {
        protected override void SetEntityStyle(EntityType entity)
        {
            entity.EntityDefaultStyle<PrefabType, EntityType>();
            if (PopupStyle != null)
                entity.EntityStyle = PopupStyle;
        }
    }
    public class PropPopup : PrefabPopup<PropInfo, PropEntity>
    {
        protected override string EmptyText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchCache { get; set; } = string.Empty;

        public override void Init(IEnumerable<PropInfo> values, Func<PropInfo, bool> selector, Func<PropInfo, PropInfo, int> sorter)
        {
            Search.text = SearchCache;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchCache = Search.text;
            base.DeInit();
        }
        protected override string GetName(PropInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class TreePopup : PrefabPopup<TreeInfo, TreeEntity>
    {
        protected override string EmptyText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchCache { get; set; } = string.Empty;

        public override void Init(IEnumerable<TreeInfo> values, Func<TreeInfo, bool> selector, Func<TreeInfo, TreeInfo, int> sorter)
        {
            Search.text = SearchCache;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchCache = Search.text;
            base.DeInit();
        }
        protected override string GetName(TreeInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class NetPopup : PrefabPopup<NetInfo, NetEntity>
    {
        protected override string EmptyText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchCache { get; set; } = string.Empty;

        public override void Init(IEnumerable<NetInfo> values, Func<NetInfo, bool> selector, Func<NetInfo, NetInfo, int> sorter)
        {
            Search.text = SearchCache;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchCache = Search.text;
            base.DeInit();
        }
        protected override string GetName(NetInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }

    public abstract class PrefabEntity<PrefabType> : PopupEntity<PrefabType>
        where PrefabType : PrefabInfo
    {
        private string rawName;
        private bool showFavorite;

        public string RawName
        {
            get => rawName;
            set
            {
                rawName = value;
                Set();
            }
        }
        public bool ShowFavorite
        {
            get => showFavorite;
            set
            {
                showFavorite = value;
                Set();
            }
        }

        private CustomUISprite Screenshot { get; set; }
        private CustomUIButton Favorite { get; set; }

        protected abstract string LocalizedTitle { get; }
        private bool IsFavorite => SingletonManager<FavoritePrefabsManager>.Instance.IsFavorite(RawName);

        public PrefabEntity()
        {
            Screenshot = AddUIComponent<CustomUISprite>();
            Screenshot.size = new Vector2(90f, 90f);

            WordWrap = true;
            textScale = 0.7f;
            TextVerticalAlignment = UIVerticalAlignment.Middle;
            TextHorizontalAlignment = UIHorizontalAlignment.Left;

            Favorite = AddUIComponent<CustomUIButton>();
            Favorite.Atlas = IMTTextures.Atlas;
            Favorite.IconMode = SpriteMode.Fill;
            Favorite.size = new Vector2(20, 90);
            Favorite.eventClick += FavoriteClick;

            showFavorite = true;

            Set();
        }
        public override void DeInit()
        {
            Screenshot.atlas = null;
            Screenshot.spriteName = string.Empty;
            showFavorite = true;
            RawName = string.Empty;
        }
        public override void SetObject(int index, PrefabType prefab, bool selected)
        {
            base.SetObject(index, prefab, selected);
            rawName = prefab?.name ?? string.Empty;
            Set();
        }
        private void Set()
        {
            if (EditObject is PrefabType prefab)
            {
                Screenshot.atlas = prefab.m_Atlas;
                Screenshot.spriteName = prefab.m_Thumbnail;
                Screenshot.isVisible = !string.IsNullOrEmpty(Screenshot.spriteName);
                Favorite.isVisible = ShowFavorite;
                SetFavoriteButton();
                text = LocalizedTitle;
            }
            else
            {
                Screenshot.atlas = null;
                Screenshot.spriteName = string.Empty;
                Screenshot.isVisible = false;
                Favorite.isVisible = false;
                text = string.IsNullOrEmpty(RawName) ? IMT.Localize.StyleOption_AssetNotSet : string.Format(IMT.Localize.StyleOption_AssetMissed, RawName);
            }

            SetPosition();
        }

        private Color32 FavoriteNormal => UIStyle.ItemFavoriteNormal;
        private Color32 FavoriteHovered => UIStyle.ItemFavoriteHovered;
        private Color32 FavoritePressed => UIStyle.ItemFavoritePressed;
        private void SetFavoriteButton()
        {
            if (IsFavorite)
            {
                Favorite.tooltip = IMT.Localize.StyleOption_RemoveFromFavorites;
                Favorite.IconSprites = new SpriteSet(IMTTextures.SetDefaultHeaderButton, IMTTextures.UnsetDefaultHeaderButton, IMTTextures.UnsetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton);
                Favorite.IconColors = new ColorSet(FavoriteNormal, FavoriteNormal, FavoriteHovered, FavoriteNormal, FavoriteNormal);
            }
            else
            {
                Favorite.tooltip = IMT.Localize.StyleOption_AddToFavorites;
                Favorite.IconSprites = new SpriteSet(IMTTextures.NotSetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.NotSetDefaultHeaderButton, IMTTextures.NotSetDefaultHeaderButton);
                Favorite.IconColors = new ColorSet(Color.white, FavoriteHovered, FavoritePressed, Color.white, Color.white);
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetPosition()
        {
            if (Screenshot != null)
            {
                Screenshot.size = new Vector2(height - 10f, height - 10f);
                Screenshot.relativePosition = new Vector2(5f, 5f);
                Favorite.size = new Vector2(20f, height - 10f);
                Favorite.relativePosition = new Vector2(width - Favorite.width - 5f, 5f);

                var left = Screenshot.isVisible ? Mathf.CeilToInt(Screenshot.relativePosition.x + Screenshot.width) + 5 : 8;
                var right = Math.Max(Favorite.isVisible ? Mathf.CeilToInt(width - Favorite.relativePosition.x) + 5 : 8, Padding.right);
                TextPadding = new RectOffset(left, right, 5, 5);
            }
        }

        private void FavoriteClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            eventParam.Use();

            SingletonManager<FavoritePrefabsManager>.Instance.Set(RawName, !IsFavorite);
            SetFavoriteButton();
        }
    }
    public class PropEntity : PrefabEntity<PropInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(EditObject);
    }
    public class TreeEntity : PrefabEntity<TreeInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(EditObject);
    }
    public class NetEntity : PrefabEntity<NetInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(EditObject);
    }
}
