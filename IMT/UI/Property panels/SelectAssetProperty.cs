using ColossalFramework.UI;
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
            SetSize();
            base.OnSizeChanged();
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

    public class SelectPropPopup : SearchPopup<PropInfo, PropEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
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
        protected override string GetName(PropInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class SelectTreePopup : SearchPopup<TreeInfo, TreeEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
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
        protected override string GetName(TreeInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
    }
    public class SelectNetPopup : SearchPopup<NetInfo, NetEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
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
        protected override string GetName(NetInfo prefab) => Utilities.Utilities.GetPrefabName(prefab);
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
        private CustomUISprite Screenshot { get; set; }
        private CustomUILabel Title { get; set; }

        protected abstract string LocalizedTitle { get; }

        public PrefabPanel()
        {
            autoLayout = true;

            Screenshot = AddUIComponent<CustomUISprite>();
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
            Title.text = IMT.Localize.StyleOption_AssetNotSet;
            Screenshot.atlas = null;
            Screenshot.spriteName = string.Empty;
        }

        private void Set()
        {
            if (Prefab is PrefabType prefab)
            {
                Screenshot.atlas = prefab.m_Atlas;
                Screenshot.spriteName = prefab.m_Thumbnail;
                Screenshot.isVisible = true;
                autoLayoutPadding = new RectOffset(5, 5, 5, 5);
                Title.text = LocalizedTitle;
            }
            else
            {
                Screenshot.atlas = null;
                Screenshot.spriteName = string.Empty;
                Screenshot.isVisible = false;
                autoLayoutPadding = new RectOffset(8, 8, 5, 5);
                Title.text = IMT.Localize.StyleOption_AssetNotSet;
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
