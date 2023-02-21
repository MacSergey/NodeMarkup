using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI
{
    public class SelectThemeProperty : EditorPropertyPanel, IReusable
    {
        public event Action<ThemeHelper.IThemeData> OnValueChanged;
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;

        private CustomUIButton Background { get; set; }
        private ThemePanel Panel { get; set; }
        private CustomUIButton Button { get; set; }
        private SelectThemePopup Popup { get; set; }

        private ThemeHelper.IThemeData theme;
        public ThemeHelper.IThemeData Theme
        {
            get => theme;
            set
            {
                if (value != theme)
                {
                    theme = value;
                    Panel.Theme = value;
                }
            }
        }
        public string RawName
        {
            get => Panel.RawName;
            set => Panel.RawName = value;
        }

        private static IEnumerable<ThemeHelper.IThemeData> Themes
        {
            get
            {
                //yield return ThemeHelper.DefaultTheme;

                foreach (var theme in ThemeHelper.ThemeDatas)
                    yield return theme;
            }
        }


        public SelectThemeProperty()
        {
            Background = Content.AddUIComponent<CustomUIButton>();
            Background.atlas = CommonTextures.Atlas;
            Background.normalBgSprite = CommonTextures.FieldNormal;
            Background.hoveredBgSprite = CommonTextures.FieldHovered;
            Background.disabledBgSprite = CommonTextures.FieldDisabled;

            Panel = Background.AddUIComponent<ThemePanel>();
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

        public override void Init() => Init(null);
        public void Init(float height)
        {
            base.Init(height);
        }
        public override void DeInit()
        {
            base.DeInit();
            Theme = null;
            OnValueChanged = null;
        }
        protected void ValueChanged() => OnValueChanged?.Invoke(Theme);

        public override void Update()
        {
            base.Update();
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
            Popup = root.AddUIComponent<SelectThemePopup>();
            Popup.canFocus = true;
            Popup.atlas = CommonTextures.Atlas;
            Popup.backgroundSprite = CommonTextures.FieldHovered;
            Popup.ItemHover = CommonTextures.FieldNormal;
            Popup.ItemSelected = CommonTextures.FieldFocused;
            Popup.EntityHeight = 50f;
            Popup.MaxVisibleItems = 10;
            Popup.maximumSize = new Vector2(230f, 700f);
            Popup.Init(Themes);
            Popup.Focus();
            Popup.SelectedObject = Theme;

            Popup.eventKeyDown += OnPopupKeyDown;
            Popup.eventLeaveFocus += OnPopupLeaveFocus;
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
            if (Popup == null)
                return;

            if (!Popup.containsFocus)
            {
                ClosePopup();
                return;
            }

            if (Input.GetMouseButtonDown(0) && !Popup.Raycast(GetCamera().ScreenPointToRay(Input.mousePosition)))
            {
                ClosePopup();
                return;
            }
        }
        private void OnSelectedChanged(ThemeHelper.IThemeData theme)
        {
            Theme = theme;
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

    public class SelectThemePopup : SearchPopup<ThemeHelper.IThemeData, ThemeEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchText { get; set; } = string.Empty;
        public override void Init(IEnumerable<ThemeHelper.IThemeData> values, Func<ThemeHelper.IThemeData, bool> selector = null)
        {
            Search.text = SearchText;
            base.Init(values, selector);
        }
        public override void DeInit()
        {
            SearchText = Search.text;
            base.DeInit();
        }

        protected override string GetName(ThemeHelper.IThemeData value) => value.Name;
    }

    public class ThemeEntity : PopupEntity<ThemeHelper.IThemeData>
    {
        private ThemePanel Panel { get; set; }

        public override ThemeHelper.IThemeData Object
        {
            get => base.Object;
            protected set
            {
                base.Object = value;
                Panel.Theme = value;
            }
        }
        public ThemeEntity()
        {
            Panel = AddUIComponent<ThemePanel>();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Panel.size = size;
        }
    }

    public class ThemePanel : CustomUIPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        private ThemeHelper.IThemeData theme;
        private string rawName;

        public ThemeHelper.IThemeData Theme
        {
            get => theme;
            set
            {
                if (value != theme)
                {
                    theme = value;
                    rawName = theme.Id;
                    Set();
                }
            }
        }
        public string RawName
        {
            get => rawName;
            set
            {
                rawName = value;
                Set();
            }
        }

        //private CustomUITextureSprite Screenshot { get; set; }
        private CustomUILabel Title { get; set; }

        public ThemePanel()
        {
            autoLayout = true;

            //Screenshot = AddUIComponent<CustomUITextureSprite>();
            //Screenshot.size = new Vector2(90f, 90f);

            Title = AddUIComponent<CustomUILabel>();
            Title.autoSize = false;
            Title.wordWrap = true;
            Title.textScale = 0.7f;
            Title.verticalAlignment = UIVerticalAlignment.Middle;

            Set();
        }

        public void DeInit()
        {
            //Screenshot.texture = null;
            theme = default;
        }

        private void Set()
        {
            if (Theme is ThemeHelper.IThemeData data)
            {
                //Screenshot.texture = Theme.screenshot;
                //Screenshot.isVisible = true;
                autoLayoutPadding = new RectOffset(5, 5, 5, 5);
                Title.text = data.Name;
            }
            else
            {
                //Screenshot.texture = null;
                //Screenshot.isVisible = false;
                autoLayoutPadding = new RectOffset(8, 8, 5, 5);
                Title.text = string.IsNullOrEmpty(RawName) ? IMT.Localize.StyleOption_AssetNotSet : string.Format(IMT.Localize.StyleOption_AssetMissed, RawName);
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
            //if (Screenshot != null && Title != null)
            //{
            //    Screenshot.size = new Vector2(height - autoLayoutPadding.vertical, height - autoLayoutPadding.vertical);
            //    var titleWidth = width - (Screenshot.isVisible ? Screenshot.width + autoLayoutPadding.horizontal * 2f : autoLayoutPadding.horizontal);
            //    Title.size = new Vector2(titleWidth, height - autoLayoutPadding.vertical);
            //}
            if (Title != null)
            {
                var titleWidth = width - autoLayoutPadding.horizontal;
                Title.size = new Vector2(titleWidth, height - autoLayoutPadding.vertical);
            }
        }
    }
}
