using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;
using ColossalFramework;
using ModsCommon;

namespace NodeMarkup.Tools
{
    public class SelectToolMode : BaseSelectToolMode<NodeMarkupTool>, IToolModePanel, IToolMode<ToolModeType>, IShortcutMode
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;

        public virtual IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                if (!Underground)
                    yield return NodeMarkupTool.EnterUndergroundShortcut;
                else
                    yield return NodeMarkupTool.ExitUndergroundShortcut;
            }
        }

        public override string GetToolInfo()
        {
            if (IsHoverNode)
                return string.Format(Localize.Tool_InfoHoverNode, HoverNode.Id) + GetStepOverInfo();
            else if (IsHoverSegment)
                return string.Format(Localize.Tool_InfoHoverSegment, HoverSegment.Id) + GetStepOverInfo();
            else if (Settings.IsUndergroundWithModifier)
                return $"{Localize.Tool_SelectInfo}\n\n{string.Format(Localize.Tool_InfoUnderground, LocalizeExtension.Shift.AddInfoColor())}";
            else if (!Underground)
                return $"{Localize.Tool_SelectInfo}\n\n{string.Format(Localize.Tool_EnterUnderground, NodeMarkupTool.EnterUndergroundShortcut.AddInfoColor())}";
            else
                return $"{Localize.Tool_SelectInfo}\n\n{string.Format(Localize.Tool_ExitUnderground, NodeMarkupTool.ExitUndergroundShortcut.AddInfoColor())}";
        }
        private string GetStepOverInfo() => NodeMarkupTool.SelectionStepOverShortcut.NotSet? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, Colors.AddInfoColor(NodeMarkupTool.SelectionStepOverShortcut));

        public override void OnPrimaryMouseClicked(Event e)
        {
            var markup = default(Markup);
            if (IsHoverNode)
                markup = SingletonManager<NodeMarkupManager>.Instance[HoverNode.Id];
            else if (IsHoverSegment)
                markup = SingletonManager<SegmentMarkupManager>.Instance[HoverSegment.Id];
            else
                return;

            SingletonMod<Mod>.Logger.Debug($"Select marking {markup}");
            Tool.SetMarkup(markup);

            if (markup.NeedSetOrder)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_RoadsWasChangedCaption;
                messageBox.MessageText = Localize.Tool_RoadsWasChangedMessage;
                messageBox.OnButton1Click = OnYes;
                messageBox.OnButton2Click = OnNo;
            }
            else
                OnNo();

            bool OnYes()
            {
                BaseOrderToolMode.IntersectionTemplate = markup.Backup;
                Tool.SetMode(ToolModeType.EditEntersOrder);
                markup.NeedSetOrder = false;
                return true;
            }
            bool OnNo()
            {
                Tool.SetDefaultMode();
                markup.NeedSetOrder = false;
                return true;
            }
        }
        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) &&  nodeId.GetNode().m_flags.CheckFlags(NetNode.Flags.None, NetNode.Flags.Middle);

        protected override bool CheckItemClass(ItemClass itemClass) => (itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels) && itemClass switch
        {
            { m_service: ItemClass.Service.Road } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportPlane } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportTrain } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportMetro } => true,
            { m_service: ItemClass.Service.Beautification, m_subService: ItemClass.SubService.BeautificationParks } => true,
            _ => false,
        };

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (Settings.IsUndergroundWithModifier)
            {
                if (!Underground && Utility.OnlyShiftIsPressed)
                    Underground = true;
                else if (Underground && !Utility.OnlyShiftIsPressed)
                    Underground = false;
            }
        }

        public void ChangeUnderground(bool underground)
        {
            Underground = underground;
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) => RenderLight(cameraInfo);
    }
}
