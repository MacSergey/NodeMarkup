using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI
{
    public abstract class SelectPrefabProperty<PrefabType> : EditorPropertyPanel, IReusable
        where PrefabType : PrefabInfo
    {
        public event Action<PrefabType> OnValueChanged;
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;

        public abstract PrefabType Prefab { get; set; }
        public abstract string RawName { get; set; }
        public abstract Func<PrefabType, bool> SelectPredicate { get; set; }
        public abstract Func<PrefabType, PrefabType, int> SortPredicate { get; set; }

        public override void DeInit()
        {
            base.DeInit();
            Prefab = null;
            SelectPredicate = null;
            OnValueChanged = null;
        }

        public override void Init() => Init(null);
        public new virtual void Init(float? height)
        {
            base.Init(height);
        }

        protected void ValueChanged(PrefabType prefab) => OnValueChanged?.Invoke(prefab);
    }
    public abstract class SelectPrefabProperty<PrefabType, EntityType, PopupType, DropDownType> : SelectPrefabProperty<PrefabType>
        where PrefabType : PrefabInfo
        where EntityType : PrefabEntity<PrefabType>
        where PopupType : Popup<PrefabType, EntityType>
        where DropDownType : PrefabDropDown<PrefabType, PopupType, EntityType>
    {
        protected override float DefaultHeight => 100f;

        private DropDownType DropDown { get; }

        public override PrefabType Prefab
        {
            get => DropDown.SelectedObject;
            set => DropDown.SelectedObject = value;
        }
        public override string RawName
        {
            get => DropDown.RawName;
            set => DropDown.RawName = value;
        }
        public override Func<PrefabType, bool> SelectPredicate
        {
            get => DropDown.SelectPredicate;
            set => DropDown.SelectPredicate = value;
        }
        public override Func<PrefabType, PrefabType, int> SortPredicate
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

        public SelectPrefabProperty()
        {
            DropDown = Content.AddUIComponent<DropDownType>();
            DropDown.DefaultStyle();
            DropDown.OnSelectedObjectChanged += ValueChanged;
        }
        public override void Init(float? height)
        {
            base.Init(height);

            DropDown.Clear();
            var count = PrefabCollection<PrefabType>.LoadedCount();
            for (uint i = 0; i < count; i += 1)
                DropDown.AddItem(PrefabCollection<PrefabType>.GetLoaded(i));
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (DropDown != null)
            {
                DropDown.size = new Vector2(230f, height - 10f);
                DropDown.scaleFactor = 20f / DropDown.height;
            }
        }
    }

    public class SelectPropProperty : SelectPrefabProperty<PropInfo, PropEntity, PropPopup, PropDropDown> { }
    public class SelectTreeProperty : SelectPrefabProperty<TreeInfo, TreeEntity, TreePopup, TreeDropDown> { }
    public class SelectNetworkProperty : SelectPrefabProperty<NetInfo, NetEntity, NetPopup, NetDropDown> { }

    public abstract class PrefabDropDown<PrefabType, PopupType, EntityType> : AdvancedDropDown<PrefabType, PopupType, EntityType>
        where PrefabType : PrefabInfo
        where EntityType : PrefabEntity<PrefabType>
        where PopupType : Popup<PrefabType, EntityType>
    {
        public string RawName
        {
            get => Entity.RawName;
            set => Entity.RawName = value;
        }
        public Func<PrefabType, bool> SelectPredicate { get; set; }
        public Func<PrefabType, PrefabType, int> SortPredicate { get; set; }

        public PrefabDropDown()
        {
            Entity.ShowFavorite = false;
        }
        protected override void SetPopupStyle() => Popup.DefaultStyle(50f);
        protected override void InitPopup()
        {
            Popup.DefaultStyle(50f);
            Popup.MaximumSize = new Vector2(width, 700f);
            Popup.width = width;
            Popup.MaxVisibleItems = 10;
            Popup.Init(Objects, SelectPredicate, SortPredicate);
        }
    }
    public class PropDropDown : PrefabDropDown<PropInfo, PropPopup, PropEntity> { }
    public class TreeDropDown : PrefabDropDown<TreeInfo, TreePopup, TreeEntity> { }
    public class NetDropDown : PrefabDropDown<NetInfo, NetPopup, NetEntity> { }


    public class PropPopup : SearchPopup<PropInfo, PropEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchText { get; set; } = string.Empty;

        public override void Init(IEnumerable<PropInfo> values, Func<PropInfo, bool> selector, Func<PropInfo, PropInfo, int> sorter)
        {
            Search.text = SearchText;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }
        protected override string GetName(PropInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class TreePopup : SearchPopup<TreeInfo, TreeEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchText { get; set; } = string.Empty;

        public override void Init(IEnumerable<TreeInfo> values, Func<TreeInfo, bool> selector, Func<TreeInfo, TreeInfo, int> sorter)
        {
            Search.text = SearchText;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }
        protected override string GetName(TreeInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class NetPopup : SearchPopup<NetInfo, NetEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchText { get; set; } = string.Empty;

        public override void Init(IEnumerable<NetInfo> values, Func<NetInfo, bool> selector, Func<NetInfo, NetInfo, int> sorter)
        {
            Search.text = SearchText;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
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
        private CustomUILabel Title { get; set; }
        private CustomUIButton Favorite { get; set; }

        protected abstract string LocalizedTitle { get; }
        private bool IsFavorite => SingletonManager<FavoritePrefabsManager>.Instance.IsFavorite(RawName);

        public PrefabEntity()
        {
            Screenshot = AddUIComponent<CustomUISprite>();
            Screenshot.size = new Vector2(90f, 90f);

            Title = AddUIComponent<CustomUILabel>();
            Title.autoSize = false;
            Title.wordWrap = true;
            Title.textScale = 0.7f;
            Title.verticalAlignment = UIVerticalAlignment.Middle;

            Favorite = AddUIComponent<CustomUIButton>();
            Favorite.atlas = IMTTextures.Atlas;
            Favorite.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
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
            if (Object is PrefabType prefab)
            {
                Screenshot.atlas = prefab.m_Atlas;
                Screenshot.spriteName = prefab.m_Thumbnail;
                Screenshot.isVisible = !string.IsNullOrEmpty(Screenshot.spriteName);
                Favorite.isVisible = ShowFavorite;
                SetFavoriteButton();
                Title.text = LocalizedTitle;
            }
            else
            {
                Screenshot.atlas = null;
                Screenshot.spriteName = string.Empty;
                Screenshot.isVisible = false;
                Favorite.isVisible = false;
                Title.text = string.IsNullOrEmpty(RawName) ? IMT.Localize.StyleOption_AssetNotSet : string.Format(IMT.Localize.StyleOption_AssetMissed, RawName);
            }

            SetPosition();
        }

        private Color32 FavoriteNormal => new Color32(255, 215, 0, 255);
        private Color32 FavoriteHovered => new Color32(255, 200, 0, 255);
        private Color32 FavoritePressed => new Color32(255, 190, 0, 255);
        private void SetFavoriteButton()
        {
            if (IsFavorite)
            {
                Favorite.tooltip = IMT.Localize.StyleOption_RemoveFromFavorites;
                Favorite.SetFgSprite(new ModsCommon.UI.SpriteSet(IMTTextures.SetDefaultHeaderButton, IMTTextures.UnsetDefaultHeaderButton, IMTTextures.UnsetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton));
                Favorite.SetFgColor(new ColorSet(FavoriteNormal, FavoriteHovered, FavoritePressed, FavoriteNormal, FavoriteNormal));
            }
            else
            {
                Favorite.tooltip = IMT.Localize.StyleOption_AddToFavorites;
                Favorite.SetFgSprite(new ModsCommon.UI.SpriteSet(IMTTextures.NotSetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.SetDefaultHeaderButton, IMTTextures.NotSetDefaultHeaderButton, IMTTextures.NotSetDefaultHeaderButton));
                Favorite.SetFgColor(new ColorSet(Color.white, FavoriteHovered, FavoritePressed, Color.white, Color.white));
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetPosition()
        {
            if (Screenshot != null && Title != null)
            {
                Screenshot.size = new Vector2(height - 10f, height - 10f);
                Screenshot.relativePosition = new Vector2(5f, 5f);
                Title.size = size;
                Favorite.size = new Vector2(20f, height - 10f);
                Favorite.relativePosition = new Vector2(width - Favorite.width - 5f, 5f);

                var left = Screenshot.isVisible ? Mathf.CeilToInt(Screenshot.relativePosition.x + Screenshot.width) + 5 : 8;
                var right = Math.Max(Favorite.isVisible ? Mathf.CeilToInt(width - Favorite.relativePosition.x) + 5 : 8, Padding.right);
                Title.padding = new RectOffset(left, right, 5, 5);
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
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Object);
    }
    public class TreeEntity : PrefabEntity<TreeInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Object);
    }
    public class NetEntity : PrefabEntity<NetInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Object);
    }
}
