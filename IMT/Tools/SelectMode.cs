using IMT.Manager;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Tools
{
    public class SelectToolMode : BaseSelectToolMode<IntersectionMarkingTool>, IToolModePanel, IToolMode<ToolModeType>, IShortcutMode
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;

        public virtual IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                if (!Underground)
                    yield return IntersectionMarkingTool.EnterUndergroundShortcut;
                else
                    yield return IntersectionMarkingTool.ExitUndergroundShortcut;
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
                return $"{Localize.Tool_SelectInfo}\n\n{string.Format(Localize.Tool_EnterUnderground, IntersectionMarkingTool.EnterUndergroundShortcut.AddInfoColor())}";
            else
                return $"{Localize.Tool_SelectInfo}\n\n{string.Format(Localize.Tool_ExitUnderground, IntersectionMarkingTool.ExitUndergroundShortcut.AddInfoColor())}";
        }
        private string GetStepOverInfo() => IntersectionMarkingTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, Colors.AddInfoColor(IntersectionMarkingTool.SelectionStepOverShortcut));

        public override void OnPrimaryMouseClicked(Event e)
        {
            var marking = default(Marking);
            if (IsHoverNode)
                marking = SingletonManager<NodeMarkingManager>.Instance[HoverNode.Id];
            else if (IsHoverSegment)
                marking = SingletonManager<SegmentMarkingManager>.Instance[HoverSegment.Id];
            else
                return;

            SingletonMod<Mod>.Logger.Debug($"Select marking {marking}");
            Tool.SetMarking(marking);

            if (marking.NeedSetOrder)
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
                BaseOrderToolMode.IntersectionTemplate = marking.Backup;
                Tool.SetMode(ToolModeType.EditEntersOrder);
                marking.NeedSetOrder = false;
                return true;
            }
            bool OnNo()
            {
                Tool.SetDefaultMode();
                marking.NeedSetOrder = false;
                return true;
            }
        }
        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) && nodeId.GetNode().m_flags.CheckFlags(NetNode.Flags.None, NetNode.Flags.Middle);

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
