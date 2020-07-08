using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class MessageBox : UIPanel
    {
        private static float Width { get; } = 573;
        private static float Height { get; } = 200;

        public static T ShowModal<T>()
        where T : MessageBox
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
        public static void HideModal(MessageBox messageBox)
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
        public string MessageText { set => Message.text = value; }
        private UILabel Caption { get; set; }
        private UILabel Message { get; set; }

        public MessageBox()
        {
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = Width;
            height = Height;
            color = new Color32(58, 88, 104, 255);
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));
            backgroundSprite = "MenuPanel";

            AddHandle();
            AddMessage();
        }

        private void AddHandle()
        {
            var handle = AddUIComponent<UIDragHandle>();
            handle.size = new Vector2(Width, 42);
            handle.relativePosition = new Vector2(0, 0);
            handle.target = parent;
            handle.eventSizeChanged += ((component, size) =>
            {
                Caption.size = size;
                Caption.CenterToParent();
            });

            Caption = handle.AddUIComponent<UILabel>(); ;
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.textScale = 1.3f;
            Caption.anchor = UIAnchorStyle.Top;

            Caption.eventTextChanged += ((component, text) => Caption.CenterToParent());

            var cancel = handle.AddUIComponent<UIButton>();
            cancel.normalBgSprite = "buttonclose";
            cancel.hoveredBgSprite = "buttonclosehover";
            cancel.pressedBgSprite = "buttonclosepressed";
            cancel.size = new Vector2(32, 32);
            cancel.relativePosition = new Vector2(527, 4);
            cancel.eventClick += ((UIComponent component, UIMouseEventParameter eventParam) => Cancel());
        }
        private void AddMessage()
        {
            Message = AddUIComponent<UILabel>();
            Message.textAlignment = UIHorizontalAlignment.Center;
            Message.textScale = 1.3f;
            Message.size = new Vector2(536, 78);
            Message.relativePosition = new Vector3(17, 7);
            Message.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
            Message.eventTextChanged += ((UIComponent component, string value) => Message.PerformLayout());
        }
        protected UIButton AddButton(int i, int from, Action action)
        {
            var width = (this.width - (25 * (from + 1))) / from;
            var button = AddUIComponent<UIButton>();
            button.normalBgSprite = "ButtonMenu";
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.horizontalAlignment = UIHorizontalAlignment.Center;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.size = new Vector2(width, 47);
            button.relativePosition = new Vector2(width * (i - 1) + 25 * i, 139);
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
    public class OneButtonMessageBox : MessageBox
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
            if(OnButtonClick?.Invoke() != false)
                Cancel();
        }
    }
    public class TwoButtonMessageBox : MessageBox
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
    public class ThreeButtonMessageBox : MessageBox
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
            ButtonText = "OK";
        }
    }
    public class YesNoMessageBox : TwoButtonMessageBox
    {
        public YesNoMessageBox()
        {
            Button1Text = "Yes";
            Button2Text = "No";
        }
    }
}
