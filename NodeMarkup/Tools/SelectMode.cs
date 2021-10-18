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
    public class SelectToolMode : BaseSelectToolMode<NodeMarkupTool>, IToolModePanel, IToolMode<ToolModeType>
    {
        public bool ShowPanel => false;
        public ToolModeType Type => ToolModeType.Select;


        public override string GetToolInfo()
        {
            if (IsHoverNode)
                return string.Format(Localize.Tool_InfoHoverNode, HoverNode.Id) + GetStepOverInfo();
            else if (IsHoverSegment)
                return string.Format(Localize.Tool_InfoHoverSegment, HoverSegment.Id) + GetStepOverInfo();
            else
                return Localize.Tool_SelectInfo;
        }
        private string GetStepOverInfo() =>
            NodeMarkupTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n"
            + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, Colors.AddInfoColor(NodeMarkupTool.SelectionStepOverShortcut))
            + (Tool.MarkupBuffer != null
                ? '\n' + string.Format(Localize.Tool_InfoCopyMarkingApply, LocalizeExtension.Alt.AddInfoColor(), KeyCode.Mouse0.GetLocale().AddInfoColor())
                : string.Empty)
            + ((IsHoverNode && SingletonManager<NodeMarkupManager>.Instance.HasMarkup(HoverNode.Id)) ||
               (IsHoverSegment && SingletonManager<SegmentMarkupManager>.Instance.HasMarkup(HoverSegment.Id))
                ? '\n' + string.Format(Localize.Tool_InfoCopyMarkingGet, LocalizeExtension.Alt.AddInfoColor(), KeyCode.Mouse1.GetLocale().AddInfoColor())
                : string.Empty)
            ;

        public override void OnPrimaryMouseClicked(Event e)
        {
            var markup = default(Markup);
            if (Utility.OnlyAltIsPressed)
            {
                if (Tool.MarkupBuffer == null)
                    return; // nothing to clone

                if (IsHoverNode)
                    markup = SingletonManager<NodeMarkupManager>.Instance[HoverNode.Id];
                else if (IsHoverSegment)
                    markup = SingletonManager<SegmentMarkupManager>.Instance[HoverSegment.Id];
                else
                    return;

                markup.Clear();
                var SourceEnters = Tool.MarkupBuffer.Enters.Select((e, i) => new SourceEnter(e, i)).ToArray();
                var TargetEnters = markup.Enters.Select((e, i) => new TargetEnter(e, i)).ToArray();

                var min = Math.Min(TargetEnters.Length, SourceEnters.Length);
                for (var i = 0; i < min; i += 1)
                    SourceEnters[i].Target = TargetEnters[i];
                var map = new ObjectsMap(false);

                foreach (var source in SourceEnters)
                {
                    var enterTarget = source.Target as TargetEnter;
                    var sourceId = source.Enter.Id;
                    var targetId = enterTarget?.Enter.Id ?? 0;
                    switch (markup.Type)
                    {
                        case MarkupType.Node:
                            map.AddSegment(sourceId, targetId);
                            break;
                        case MarkupType.Segment:
                            map.AddNode(sourceId, targetId);
                            break;
                    }

                    if (enterTarget != null)
                    {
                        for (var i = 0; i < source.Points.Length; i += 1)
                            map.AddPoint(enterTarget.Enter.Id, (byte)(i + 1), (byte)((source.Points[i].Target as Target)?.Num + 1 ?? 0));
                    }
                }

                markup.FromXml(SingletonMod<Mod>.Version, Tool.MarkupBuffer.Data, map);
            }
            else
            {
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
        }

        public override void OnSecondaryMouseClicked()
        {
            if (Utility.OnlyAltIsPressed)
            {
                if (!(IsHoverNode && SingletonManager<NodeMarkupManager>.Instance.HasMarkup(HoverNode.Id)) &&
                        !(IsHoverSegment && SingletonManager<SegmentMarkupManager>.Instance.HasMarkup(HoverSegment.Id)))
                    return; // Nothing to clone

                Markup markup;
                if (IsHoverNode)
                    markup = SingletonManager<NodeMarkupManager>.Instance[HoverNode.Id];
                else if (IsHoverSegment)
                    markup = SingletonManager<SegmentMarkupManager>.Instance[HoverSegment.Id];
                else
                    return;

                SingletonMod<Mod>.Logger.Debug($"Copy marking");
                Tool.MarkupBuffer = new IntersectionTemplate(markup);
            }
            else
            {
                base.OnSecondaryMouseClicked();
            }
        }

        protected override bool IsValidNode(ushort nodeId) => nodeId.GetNode().m_flags.CheckFlags(NetNode.Flags.None, NetNode.Flags.Middle | NetNode.Flags.Underground);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default && itemClass switch
        {
            { m_service: ItemClass.Service.Road } => true,
            { m_service: ItemClass.Service.PublicTransport, m_subService: ItemClass.SubService.PublicTransportPlane } => true,
            _ => false,
        };

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo) => RenderLight(cameraInfo);
    }
}
