using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class MessageBoxBase : UIPanel
    {
        protected static float Width { get; } = 573;
        protected static float Height { get; } = 200;
        protected static float ButtonHeight { get; } = 47;
        protected static float Padding { get; } = 16;
        private static float MaxContentHeight { get; } = 500;

        public static T ShowModal<T>()
        where T : MessageBoxBase
        {
            var uiObject = new GameObject();
            uiObject.transform.parent = UIView.GetAView().transform;
            var messageBox = uiObject.AddComponent<T>();

            UIView.PushModal(messageBox);
            messageBox.Show(true);
            messageBox.Focus();

            var view = UIView.GetAView();

            if (view.panelsLibraryModalEffect != null)
            {
                view.panelsLibraryModalEffect.FitTo(null);
                if (!view.panelsLibraryModalEffect.isVisible || view.panelsLibraryModalEffect.opacity != 1f)
                {
                    view.panelsLibraryModalEffect.Show(false);
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }

            return messageBox;
        }
        public static void HideModal(MessageBoxBase messageBox)
        {
            UIView.PopModal();

            var view = UIView.GetAView();
            if (view.panelsLibraryModalEffect != null)
            {
                if (!UIView.HasModalInput())
                {
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate ()
                    {
                        view.panelsLibraryModalEffect.Hide();
                    });
                }
                else
                {
                    view.panelsLibraryModalEffect.zOrder = UIView.GetModalComponent().zOrder - 1;
                }
            }

            messageBox.Hide();
            Destroy(messageBox.gameObject);
        }

        public string CaprionText { set => Caption.text = value; }

        private UILabel Caption { get; set; }
        protected UIPanel ButtonPanel { get; private set; }
        protected UIScrollablePanel ScrollableContent { get; private set; }
        private UIDragHandle Handle { get; set; }

        public MessageBoxBase()
        {
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = Width;
            height = Height;
            color = new Color32(58, 88, 104, 255);
            backgroundSprite = "MenuPanel";
            clipChildren = true;

            AddHandle();
            AddPanel();
            FillContent();
            AddButtonPanel();
            Init();

            ScrollableContent.eventSizeChanged += ContentSizeChanged;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));
        }

        private void AddHandle()
        {
            Handle = AddUIComponent<UIDragHandle>();
            Handle.size = new Vector2(Width, 42);
            Handle.relativePosition = new Vector2(0, 0);
            //Handle.target = parent;
            Handle.eventSizeChanged += (component, size) =>
            {
                Caption.size = size;
                Caption.CenterToParent();
            };

            Caption = Handle.AddUIComponent<UILabel>();
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.textScale = 1.3f;
            Caption.anchor = UIAnchorStyle.Top;

            Caption.eventTextChanged += (component, text) => Caption.CenterToParent();

            var cancel = Handle.AddUIComponent<UIButton>();
            cancel.normalBgSprite = "buttonclose";
            cancel.hoveredBgSprite = "buttonclosehover";
            cancel.pressedBgSprite = "buttonclosepressed";
            cancel.size = new Vector2(32, 32);
            cancel.relativePosition = new Vector2(527, 4);
            cancel.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Cancel();
        }
        private void AddPanel()
        {
            ScrollableContent = AddUIComponent<UIScrollablePanel>();
            ScrollableContent.width = Width;
            ScrollableContent.autoLayout = true;
            ScrollableContent.autoLayoutDirection = LayoutDirection.Vertical;
            ScrollableContent.autoLayoutPadding = new RectOffset((int)Padding, (int)Padding, 0, 0);
            ScrollableContent.clipChildren = true;
            ScrollableContent.builtinKeyNavigation = true;
            ScrollableContent.scrollWheelDirection = UIOrientation.Vertical;
            ScrollableContent.maximumSize = new Vector2(Width, MaxContentHeight);
            UIUtils.AddScrollbar(this, ScrollableContent);

            ScrollableContent.eventComponentAdded += (UIComponent container, UIComponent child) =>
            {
                child.eventVisibilityChanged += (UIComponent component, bool value) => FitContentChildren();
                child.eventSizeChanged += (UIComponent component, Vector2 value) => FitContentChildren();
                child.eventPositionChanged += (UIComponent component, Vector2 value) => FitContentChildren();
            };
        }
        private void FitContentChildren()
        {
            ScrollableContent.FitChildrenVertically();
            ScrollableContent.width = ScrollableContent.verticalScrollbar.isVisible ? Width - ScrollableContent.verticalScrollbar.width : Width;
        }
        private void ContentSizeChanged(UIComponent component, Vector2 value) => Init();
        private void Init()
        {
            height = Handle.height + ScrollableContent.height + ButtonPanel.height + Padding;
            ScrollableContent.relativePosition = new Vector2(0, Handle.height);
            ButtonPanel.relativePosition = new Vector2(0, Handle.height + ScrollableContent.height + Padding);
            ScrollableContent.verticalScrollbar.relativePosition = ScrollableContent.relativePosition + new Vector3(ScrollableContent.width, 0);
            ScrollableContent.verticalScrollbar.height = ScrollableContent.height;

            foreach (var item in ScrollableContent.components)
            {
                item.width = ScrollableContent.width - 2 * Padding;
            }
        }
        protected virtual void FillContent() { }
        private void AddButtonPanel()
        {
            ButtonPanel = AddUIComponent<UIPanel>();
            ButtonPanel.size = new Vector2(Width, ButtonHeight + 10);
        }
        protected UIButton AddButton(int i, int from, Action action)
        {
            var width = (this.width - (25 * (from + 1))) / from;
            var button = ButtonPanel.AddUIComponent<UIButton>();
            button.normalBgSprite = "ButtonMenu";
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.horizontalAlignment = UIHorizontalAlignment.Center;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.size = new Vector2(width, ButtonHeight);
            button.relativePosition = new Vector2(width * (i - 1) + 25 * i, 0);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => action?.Invoke();
            return button;
        }
        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (!p.used)
            {
                if (p.keycode == KeyCode.Escape)
                {
                    p.Use();
                    Cancel();
                }
                else if (p.keycode == KeyCode.Return)
                {
                    p.Use();
                }
            }
        }

        protected virtual void Cancel() => HideModal(this);
    }
    public abstract class SimpleMessageBox : MessageBoxBase
    {
        private UILabel Message { get; set; }

        public string MessageText { set => Message.text = value; }
        public float MessageScale { set => Message.textScale = value; }
        public UIHorizontalAlignment TextAlignment { set => Message.textAlignment = value; }

        protected override void FillContent()
        {
            AddMessage();
        }
        private void AddMessage()
        {
            Message = ScrollableContent.AddUIComponent<UILabel>();
            Message.textAlignment = UIHorizontalAlignment.Center;
            Message.verticalAlignment = UIVerticalAlignment.Middle;
            Message.textScale = 1.1f;
            Message.wordWrap = true;
            Message.autoHeight = true;
            Message.minimumSize = new Vector2(Width - 2 * Padding, 78);
            Message.size = new Vector2(Width - 2 * Padding, 78);
            Message.relativePosition = new Vector3(17, 7);
            Message.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
            Message.eventTextChanged += (UIComponent component, string value) => Message.PerformLayout();
        }
    }

    public class OneButtonMessageBox : SimpleMessageBox
    {
        private UIButton Button { get; set; }
        public Func<bool> OnButtonClick { get; set; }
        public string ButtonText { set => Button.text = value; }

        public OneButtonMessageBox()
        {
            Button = AddButton(1, 1, ButtonClick);
        }
        protected virtual void ButtonClick()
        {
            if (OnButtonClick?.Invoke() != false)
                Cancel();
        }
    }
    public class TwoButtonMessageBox : SimpleMessageBox
    {
        private UIButton Button1 { get; set; }
        private UIButton Button2 { get; set; }
        public Func<bool> OnButton1Click { get; set; }
        public Func<bool> OnButton2Click { get; set; }
        public string Button1Text { set => Button1.text = value; }
        public string Button2Text { set => Button2.text = value; }
        public TwoButtonMessageBox()
        {
            Button1 = AddButton(1, 2, Button1Click);
            Button2 = AddButton(2, 2, Button2Click);
        }
        protected virtual void Button1Click()
        {
            if (OnButton1Click?.Invoke() != false)
                Cancel();
        }
        protected virtual void Button2Click()
        {
            if (OnButton2Click?.Invoke() != false)
                Cancel();
        }
    }
    public class ThreeButtonMessageBox : SimpleMessageBox
    {
        private UIButton Button1 { get; set; }
        private UIButton Button2 { get; set; }
        private UIButton Button3 { get; set; }
        public Func<bool> OnButton1Click { get; set; }
        public Func<bool> OnButton2Click { get; set; }
        public Func<bool> OnButton3Click { get; set; }
        public string Button1Text { set => Button1.text = value; }
        public string Button2Text { set => Button2.text = value; }
        public string Button3Text { set => Button3.text = value; }
        public ThreeButtonMessageBox()
        {
            Button1 = AddButton(1, 3, Button1Click);
            Button2 = AddButton(2, 3, Button2Click);
            Button3 = AddButton(3, 3, Button3Click);
        }
        protected virtual void Button1Click()
        {
            if (OnButton1Click?.Invoke() != false)
                Cancel();
        }
        protected virtual void Button2Click()
        {
            if (OnButton2Click?.Invoke() != false)
                Cancel();
        }
        protected virtual void Button3Click()
        {
            if (OnButton3Click?.Invoke() != false)
                Cancel();
        }
    }

    public class OkMessageBox : OneButtonMessageBox
    {
        public OkMessageBox()
        {
            ButtonText = NodeMarkup.Localize.MessageBox_OK;
        }
    }
    public class YesNoMessageBox : TwoButtonMessageBox
    {
        public YesNoMessageBox()
        {
            Button1Text = NodeMarkup.Localize.MessageBox_Yes;
            Button2Text = NodeMarkup.Localize.MessageBox_No;
        }
    }
    public class ImportMessageBox : SimpleMessageBox
    {
        private static Regex Regex { get; } = new Regex(@"MarkingRecovery\.(?<name>.+)\.(?<date>\d+)");

        private UIButton ImportButton { get; set; }
        private UIButton CancelButton { get; set; }
        private FileDropDown DropDown { get; set; }
        public ImportMessageBox()
        {
            ImportButton = AddButton(1, 2, ImportClick);
            ImportButton.text = NodeMarkup.Localize.Settings_Import;
            ImportButton.Disable();
            CancelButton = AddButton(2, 2, CancelClick);
            CancelButton.text = NodeMarkup.Localize.Setting_Cancel;

            AddFileList();
        }
        private void AddFileList()
        {
            DropDown = ScrollableContent.AddUIComponent<FileDropDown>();

            DropDown.atlas = NodeMarkupTool.InGameAtlas;
            DropDown.height = 38;
            DropDown.width = Width - 2 * Padding;
            DropDown.listBackground = "OptionsDropboxListbox";
            DropDown.itemHeight = 24;
            DropDown.itemHover = "ListItemHover";
            DropDown.itemHighlight = "ListItemHighlight";
            DropDown.normalBgSprite = "OptionsDropbox";
            DropDown.hoveredBgSprite = "OptionsDropboxHovered";
            DropDown.focusedBgSprite = "OptionsDropboxFocused";
            DropDown.listWidth = (int)DropDown.width;
            DropDown.listHeight = 200;
            DropDown.listPosition = UIDropDown.PopupListPosition.Below;
            DropDown.clampListToScreen = true;
            DropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            DropDown.popupTextColor = new Color32(170, 170, 170, 255);
            DropDown.textScale = 1.25f;
            DropDown.textFieldPadding = new RectOffset(14, 40, 7, 0);
            DropDown.verticalAlignment = UIVerticalAlignment.Middle;
            DropDown.horizontalAlignment = UIHorizontalAlignment.Center;
            DropDown.itemPadding = new RectOffset(14, 14, 0, 0);
            DropDown.verticalAlignment = UIVerticalAlignment.Middle;
            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;

            DropDown.triggerButton = DropDown;

            AddData();
            DropDown.selectedIndex = 0;
        }

        private void DropDownIndexChanged(UIComponent component, int value)
        {
            if (DropDown.SelectedObject != null)
                ImportButton.Enable();
            else
                ImportButton.Disable();
        }

        private void AddData()
        {
            foreach (var file in Serializer.GetImportList())
            {
                var match = Regex.Match(file);
                if (!match.Success)
                    continue;
                var date = new DateTime(long.Parse(match.Groups["date"].Value));
                DropDown.AddItem(file, $"{match.Groups["name"].Value} {date}");
            }
        }

        protected virtual void ImportClick()
        {
            var result = Serializer.OnImportData(DropDown.SelectedObject);

            var resultMessageBox = ShowModal<OkMessageBox>();
            resultMessageBox.CaprionText = NodeMarkup.Localize.Settings_ImportMarkingCaption;
            resultMessageBox.MessageText = result ? NodeMarkup.Localize.Settings_ImportMarkingMessageSuccess : NodeMarkup.Localize.Settings_ImportMarkingMessageFailed;

            Cancel();
        }
        protected virtual void CancelClick()
        {
            Cancel();
        }

        class FileDropDown : CustomUIDropDown<string> { }
    }

    public class WhatsNewMessageBox : MessageBoxBase
    {
        private UIButton Button { get; set; }
        public Func<bool> OnButtonClick { get; set; }

        public WhatsNewMessageBox()
        {
            Button = AddButton(1, 1, ButtonClick);
            Button.text = NodeMarkup.Localize.MessageBox_OK;
        }
        protected virtual void ButtonClick()
        {
            if (OnButtonClick?.Invoke() != false)
                Cancel();
        }

        public void Init(Dictionary<string, string> messages)
        {
            var first = default(VersionMessage);
            foreach (var message in messages)
            {
                var versionMessage = ScrollableContent.AddUIComponent<VersionMessage>();
                versionMessage.width = ScrollableContent.width;
                versionMessage.Init(message.Key, message.Value);

                if (first == null)
                    first = versionMessage;
            }
            first.IsMinimize = false;
        }

        public class VersionMessage : UIPanel
        {
            public bool IsMinimize
            {
                get => !Message.isVisible;
                set => Message.isVisible = !value;
            }
            UIButton Button { get; set; }
            UILabel Message { get; set; }
            string Label { get; set; }
            public VersionMessage()
            {
                autoLayout = true;
                autoLayoutDirection = LayoutDirection.Vertical;
                autoFitChildrenVertically = true;
                autoLayoutPadding = new RectOffset(0, 0, (int)Padding/2, (int)Padding/2);

                AddButton();
                AddText();
            }

            public void AddButton()
            {
                Button = AddUIComponent<UIButton>();
                Button.height = 20;
                Button.horizontalAlignment = UIHorizontalAlignment.Left;
                Button.color = Color.white;
                Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
                Button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => IsMinimize = !IsMinimize;
            }

            public void AddText()
            {
                Message = AddUIComponent<UILabel>();
                Message.textAlignment = UIHorizontalAlignment.Left;
                Message.verticalAlignment = UIVerticalAlignment.Middle;
                Message.textScale = 0.8f;
                Message.wordWrap = true;
                Message.autoHeight = true;
                Message.size = new Vector2(width - 2 * Padding, 0);
                Message.relativePosition = new Vector3(17, 7);
                Message.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
                Message.eventTextChanged += (UIComponent component, string value) => Message.PerformLayout();
                Message.eventVisibilityChanged += (UIComponent component, bool value) => SetLabel();
            }

            public void Init(string version, string message)
            {
                Label = string.Format(NodeMarkup.Localize.Mod_WhatsNewVersion, version);
                Message.text = message;
                IsMinimize = true;

                SetLabel();
            }
            private void SetLabel() => Button.text = $"{(IsMinimize ? "►" : "▼")} {Label}";

            protected override void OnSizeChanged()
            {
                base.OnSizeChanged();
                if (Button != null)
                    Button.width = width;
                if (Message != null)
                    Message.width = width;
            }
        }
    }
}
