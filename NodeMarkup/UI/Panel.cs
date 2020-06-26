using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkupPanel : UIPanel
    {
        public static NodeMarkupPanel Instance { get; private set; }
        private UIDragHandle Handle { get; set; }
        private UILabel Caption { get; set; }
        private UITabPanel TabPanel { get; set; }
        private List<Editor> Editors { get; } = new List<Editor>();

        private static readonly string kTabstripButton = "RoadEditorTabstripButton";
        private static float TabStripHeight => 20;

        public static NodeMarkupPanel CreatePanel()
        {
            var uiView = UIView.GetAView();
            Instance = uiView.AddUIComponent(typeof(NodeMarkupPanel)) as NodeMarkupPanel;
            return Instance;
        }

        public NodeMarkupPanel()
        {
            Init();

            CreateHandle();
            CreateTabStrip();

            eventSizeChanged += ((component, size) => Handle.width = size.x);
        }
        private void Init()
        {
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "MenuPanel2";
            absolutePosition = new Vector3(200, 200);
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoFitChildrenHorizontally = true;
            autoFitChildrenVertically = true;
            name = "NodeMarkupPanel";
        }
        private void CreateHandle()
        {
            Handle = AddUIComponent<UIDragHandle>();
            Handle.height = 42;
            Handle.target = parent;
            Handle.anchor = UIAnchorStyle.Top;
            Handle.eventSizeChanged += ((component, size) =>
            {
                Caption.size = size;
                Caption.CenterToParent();
            });

            Caption = Handle.AddUIComponent<UILabel>();
            Caption.text = nameof(NodeMarkupPanel);
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.anchor = UIAnchorStyle.Top;

            Caption.eventTextChanged += ((component, text) => Caption.CenterToParent());
        }

        private void CreateTabStrip()
        {
            TabPanel = AddUIComponent<UITabPanel>();
            TabPanel.anchor = UIAnchorStyle.Top;
            TabPanel.size = new Vector2(500, 400);

            Editors.Add(TabPanel.AddTab<PointsEditorPanel>("Points"));
            Editors.Add(TabPanel.AddTab<LinesEditorPanel>("Lines"));
        }

        public void SetNode(ushort nodeId)
        {
            Show();
            Caption.text = $"Edit node #{nodeId} markup";

            var markup = NodeMarkupManager.Get(nodeId);
            foreach(var editor in Editors)
            {
                editor.SetMarkup(markup);
            }
        }
    }
}
