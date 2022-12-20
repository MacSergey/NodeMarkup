using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class SelectPrefabProperty<PrefabType> : EditorPropertyPanel, IReusable
        where PrefabType : PrefabInfo
    {
        public event Action<PrefabType> OnValueChanged;
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;

        public abstract PrefabType Prefab { get; set; }
        public Func<PrefabType, bool> PrefabSelectPredicate { get; set; }
        public Func<PrefabType, string> PrefabSortPredicate { get; set; }

        public override void DeInit()
        {
            base.DeInit();
            Prefab = null;
            PrefabSelectPredicate = null;
            OnValueChanged = null;
        }

        public override void Init() => Init(null);
        public void Init(float height)
        {
            base.Init(height);
        }

        protected void ValueChanged() => OnValueChanged?.Invoke(Prefab);
    }
    public abstract class SelectPrefabProperty<PrefabType, PanelType, EntityType, PopupType> : SelectPrefabProperty<PrefabType>
        where PrefabType : PrefabInfo
        where PanelType : PrefabPanel<PrefabType>
        where EntityType : PrefabEntity<PrefabType, PanelType>
        where PopupType : Popup<PrefabType, EntityType>
    {

        protected override float DefaultHeight => 100f;

        private CustomUIButton Background { get; set; }
        private PanelType Panel { get; set; }
        private CustomUIButton Button { get; set; }
        private PopupType Popup { get; set; }

        private PrefabType _prefab;
        public override PrefabType Prefab
        {
            get => _prefab;
            set
            {
                if (value != _prefab)
                {
                    _prefab = value;
                    Panel.Prefab = value;
                }
            }
        }

        private IEnumerable<PrefabType> Prefabs
        {
            get
            {
                var count = PrefabCollection<PrefabType>.LoadedCount();
                for (uint i = 0; i < count; i += 1)
                    yield return PrefabCollection<PrefabType>.GetLoaded(i);
            }
        }

        public SelectPrefabProperty()
        {
            Background = Content.AddUIComponent<CustomUIButton>();
            Background.atlas = CommonTextures.Atlas;
            Background.normalBgSprite = CommonTextures.FieldNormal;
            Background.hoveredBgSprite = CommonTextures.FieldHovered;
            Background.disabledBgSprite = CommonTextures.FieldDisabled;

            Panel = Background.AddUIComponent<PanelType>();
            Panel.relativePosition = new Vector3(0f, 0f);

            Button = Background.AddUIComponent<CustomUIButton>();
            Button.atlas = TextureHelper.InGameAtlas;
            Button.textVerticalAlignment = UIVerticalAlignment.Middle;
            Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            Button.normalFgSprite = "IconDownArrow";
            Button.hoveredFgSprite = "IconDownArrowHovered";
            Button.pressedFgSprite = "IconDownArrowPressed";
            Button.focusedFgSprite = "IconDownArrow";
            Button.disabledFgSprite = "IconDownArrowDisabled";
            Button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            Button.horizontalAlignment = UIHorizontalAlignment.Right;
            Button.verticalAlignment = UIVerticalAlignment.Middle;
            Button.relativePosition = new Vector3(0f, 0f);
            Button.eventClick += ButtonClick;
        }
        public override void Update()
        {
            base.Update();

            if (Input.GetMouseButtonDown(0))
                CheckPopup();
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (Popup == null)
                OpenPopup();
            else
                ClosePopup();
        }
        protected void OpenPopup()
        {
            Button.isInteractive = false;

            var root = GetRootContainer();
            Popup = root.AddUIComponent<PopupType>();
            Popup.canFocus = true;
            Popup.atlas = CommonTextures.Atlas;
            Popup.backgroundSprite = CommonTextures.FieldHovered;
            Popup.ItemHover = CommonTextures.FieldNormal;
            Popup.ItemSelected = CommonTextures.FieldFocused;
            Popup.EntityHeight = 50f;
            Popup.MaxVisibleItems = 10;
            Popup.maximumSize = new Vector2(230f, 700f);
            Popup.Init(PrefabSortPredicate != null ? Prefabs.OrderBy(PrefabSortPredicate) : Prefabs, PrefabSelectPredicate);
            Popup.Focus();
            Popup.SelectedObject = Prefab;

            Popup.eventKeyDown += OnPopupKeyDown;
            Popup.OnSelectedChanged += OnSelectedChanged;

            SetPopupPosition();
            Popup.parent.eventPositionChanged += SetPopupPosition;
        }

        public virtual void ClosePopup()
        {
            Button.isInteractive = true;

            if (Popup != null)
            {
                Popup.eventLeaveFocus -= OnPopupLeaveFocus;
                Popup.eventKeyDown -= OnPopupKeyDown;

                ComponentPool.Free(Popup);
                Popup = null;
            }
        }
        private void CheckPopup()
        {
            if (Popup != null)
            {
                var uiView = Popup.GetUIView();
                var mouse = uiView.ScreenPointToGUI(Input.mousePosition / uiView.inputScale);
                var popupRect = new Rect(Popup.absolutePosition, Popup.size);
                if (!popupRect.Contains(mouse))
                    ClosePopup();
            }
        }

        private void OnSelectedChanged(PrefabType prefab)
        {
            Prefab = prefab;
            ValueChanged();
            ClosePopup();
        }
        private void OnPopupLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => CheckPopup();
        private void OnPopupKeyDown(UIComponent component, UIKeyEventParameter p)
        {
            if (p.keycode == KeyCode.Escape)
            {
                ClosePopup();
                p.Use();
            }
        }
        private void SetPopupPosition(UIComponent component = null, Vector2 value = default)
        {
            if (Popup != null)
            {
                UIView uiView = Popup.GetUIView();
                var screen = uiView.GetScreenResolution();
                var position = Button.absolutePosition + new Vector3(0, Button.height);
                position.x = MathPos(position.x, Popup.width, screen.x);
                position.y = MathPos(position.y, Popup.height, screen.y);

                Popup.relativePosition = position - Popup.parent.absolutePosition;
            }

            static float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        private void SetSize()
        {
            if (Background != null)
                Background.size = new Vector2(230f, height - 10f);
            if (Panel != null)
                Panel.size = new Vector2(200f, height - 10f);
            if (Button != null)
            {
                Button.size = new Vector2(230f, height - 10f);
                Button.scaleFactor = 20f / Button.height;
            }
        }
    }

    public class SelectPropProperty : SelectPrefabProperty<PropInfo, PropPanel, PropEntity, SelectPropPopup> { }
    public class SelectTreeProperty : SelectPrefabProperty<TreeInfo, TreePanel, TreeEntity, SelectTreePopup> { }
    public class SelectNetworkProperty : SelectPrefabProperty<NetInfo, NetPanel, NetEntity, SelectNetPopup> { }

    public abstract class SelectPrefabPopup<PrefabType, EntityPanel> : Popup<PrefabType, EntityPanel>
        where PrefabType : PrefabInfo
        where EntityPanel : PopupEntity<PrefabType>
    {
        private bool CanSubmit { get; set; } = true;
        protected CustomUITextField Search { get; private set; }
        private CustomUILabel NothingFound { get; set; }
        private CustomUIButton ResetButton { get; set; }

        public SelectPrefabPopup()
        {
            Search = AddUIComponent<CustomUITextField>();
            Search.atlas = TextureHelper.InGameAtlas;
            Search.selectionSprite = "EmptySprite";
            Search.normalBgSprite = "TextFieldPanel";
            Search.color = new Color32(10, 10, 10, 255);
            Search.relativePosition = new Vector2(5f, 5f);
            Search.height = 20f;
            Search.builtinKeyNavigation = true;
            Search.cursorWidth = 1;
            Search.cursorBlinkTime = 0.45f;
            Search.selectOnFocus = true;
            Search.textScale = 0.7f;
            Search.padding = new RectOffset(20, 30, 6, 0);
            Search.horizontalAlignment = UIHorizontalAlignment.Left;
            Search.eventTextChanged += SearchTextChanged;

            var loop = Search.AddUIComponent<UISprite>();
            loop.atlas = TextureHelper.InGameAtlas;
            loop.spriteName = "ContentManagerSearch";
            loop.size = new Vector2(10f, 10f);
            loop.relativePosition = new Vector2(5f, 5f);

            ResetButton = Search.AddUIComponent<CustomUIButton>();
            ResetButton.atlas = TextureHelper.InGameAtlas;
            ResetButton.normalFgSprite = "ContentManagerSearchReset";
            ResetButton.size = new Vector2(10f, 10f);
            ResetButton.hoveredColor = new Color32(127, 127, 127, 255);
            ResetButton.isVisible = false;
            ResetButton.eventClick += ResetClick;

            NothingFound = AddUIComponent<CustomUILabel>();
            NothingFound.text = NodeMarkup.Localize.AssetPopup_NothingFound;
            NothingFound.autoSize = false;
            NothingFound.autoHeight = false;
            NothingFound.height = EntityHeight;
            NothingFound.relativePosition = new Vector2(0, 30f);
            NothingFound.verticalAlignment = UIVerticalAlignment.Middle;
            NothingFound.textAlignment = UIHorizontalAlignment.Center;
            NothingFound.isVisible = false;
        }


        public override void DeInit()
        {
            base.DeInit();
            CanSubmit = false;
            Search.text = string.Empty;
        }
        protected override bool Filter(PrefabType prefab)
        {
            if (base.Filter(prefab))
            {
                name = GetPrebName(prefab);
                if (name.ToUpper().Contains(Search.text.ToUpper()))
                    return true;
            }

            return false;
        }
        protected abstract string GetPrebName(PrefabType prefab);

        private void SearchTextChanged(UIComponent component, string value)
        {
            if (CanSubmit)
                Refresh();

            ResetButton.isVisible = !string.IsNullOrEmpty(value);
        }
        private void ResetClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Search.text = string.Empty;
        }

        protected override void RefreshItems()
        {
            base.RefreshItems();
            Search.width = width - 10f;
            NothingFound.size = new Vector2(width, EntityHeight);
            NothingFound.isVisible = VisibleCount == 0;
            ResetButton.relativePosition = new Vector2(Search.width - 15f, 5f);
        }
        protected override float GetHeight() => Math.Max(base.GetHeight(), EntityHeight) + 30f;
        protected override Vector2 GetEntityPosition(int index) => base.GetEntityPosition(index) + new Vector2(0f, 30f);
        protected override Vector2 GetScrollPosition() => base.GetScrollPosition() + new Vector2(0f, 30f);
    }
    public class SelectPropPopup : SelectPrefabPopup<PropInfo, PropEntity>
    {
        private static string SearchText { get; set; } = string.Empty;
        public override void Init(IEnumerable<PropInfo> values, Func<PropInfo, bool> selector = null)
        {
            Search.text = SearchText;
            base.Init(values, selector);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }
        protected override string GetPrebName(PropInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class SelectTreePopup : SelectPrefabPopup<TreeInfo, TreeEntity>
    {
        private static string SearchText { get; set; } = string.Empty;
        public override void Init(IEnumerable<TreeInfo> values, Func<TreeInfo, bool> selector = null)
        {
            Search.text = SearchText;
            base.Init(values, selector);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }
        protected override string GetPrebName(TreeInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class SelectNetPopup : SelectPrefabPopup<NetInfo, NetEntity>
    {
        private static string SearchText { get; set; } = string.Empty;
        public override void Init(IEnumerable<NetInfo> values, Func<NetInfo, bool> selector = null)
        {
            Search.text = SearchText;
            base.Init(values, selector);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }
        protected override string GetPrebName(NetInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }

    public abstract class PrefabEntity<PrefabType, PanelType> : PopupEntity<PrefabType>
        where PrefabType : PrefabInfo
        where PanelType : PrefabPanel<PrefabType>
    {
        private PanelType Panel { get; set; }

        public override PrefabType Object
        {
            get => base.Object;
            protected set
            {
                base.Object = value;
                Panel.Prefab = value;
            }
        }
        public PrefabEntity()
        {
            Panel = AddUIComponent<PanelType>();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Panel.size = size;
        }
    }
    public class PropEntity : PrefabEntity<PropInfo, PropPanel> { }
    public class TreeEntity : PrefabEntity<TreeInfo, TreePanel> { }
    public class NetEntity : PrefabEntity<NetInfo, NetPanel> { }

    public abstract class PrefabPanel<PrefabType> : CustomUIPanel, IReusable
        where PrefabType : PrefabInfo
    {
        bool IReusable.InCache { get; set; }

        private PrefabType _prefab;
        public PrefabType Prefab
        {
            get => _prefab;
            set
            {
                if (value != _prefab)
                {
                    _prefab = value;
                    Set();
                }
            }
        }
        private CustomUIPanel Screenshot { get; set; }
        private CustomUILabel Title { get; set; }

        protected abstract string LocalizedTitle { get; }

        public PrefabPanel()
        {
            autoLayout = true;

            Screenshot = AddUIComponent<CustomUIPanel>();
            Screenshot.size = new Vector2(90f, 90f);

            Title = AddUIComponent<CustomUILabel>();
            Title.autoSize = false;
            Title.wordWrap = true;
            Title.textScale = 0.7f;
            Title.verticalAlignment = UIVerticalAlignment.Middle;

            Set();
        }

        public void DeInit()
        {
            Title.text = NodeMarkup.Localize.StyleOption_AssetNotSet;
            Screenshot.atlas = null;
            Screenshot.backgroundSprite = string.Empty;
        }

        private void Set()
        {
            if (Prefab is PrefabType prefab)
            {
                Screenshot.atlas = prefab.m_Atlas;
                Screenshot.backgroundSprite = prefab.m_Thumbnail;
                Screenshot.isVisible = true;
                autoLayoutPadding = new RectOffset(5, 5, 5, 5);
                Title.text = LocalizedTitle;
            }
            else
            {
                Screenshot.atlas = null;
                Screenshot.backgroundSprite = string.Empty;
                Screenshot.isVisible = false;
                autoLayoutPadding = new RectOffset(8, 8, 5, 5);
                Title.text = NodeMarkup.Localize.StyleOption_AssetNotSet;
            }

            SetPosition();
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
                Screenshot.size = new Vector2(height - autoLayoutPadding.vertical, height - autoLayoutPadding.vertical);
                var titleWidth = width - (Screenshot.isVisible ? Screenshot.width + autoLayoutPadding.horizontal * 2f : autoLayoutPadding.horizontal);
                Title.size = new Vector2(titleWidth, height - autoLayoutPadding.vertical);
            }
        }
    }
    public class PropPanel : PrefabPanel<PropInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Prefab);
    }
    public class TreePanel : PrefabPanel<TreeInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Prefab);
    }
    public class NetPanel : PrefabPanel<NetInfo>
    {
        protected override string LocalizedTitle => Utilities.Utilities.GetPrefabName(Prefab);
    }
}
