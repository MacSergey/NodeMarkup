using ColossalFramework.UI;
using IMT.Manager;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class LinesEditor : Editor<LineItemsPanel, MarkingLine>, IPropertyEditor
    {
        #region PROPERTIES

        public static Color HoverAlpha
        {
            get
            {
                var color = CommonColors.Hover;
                color.a = 128;
                return color;
            }
        }
        public override string Name => IMT.Localize.LineEditor_Lines;
        public override string EmptyMessage => IMT.Localize.LineEditor_EmptyMessage;
        public override Marking.SupportType Support => Marking.SupportType.Lines;

        private PropertyGroupPanel LineProperties { get; set; }
        private CustomUIButton AddRuleButton { get; set; }

        public List<ILinePartEdge> SupportPoints { get; } = new List<ILinePartEdge>();
        public bool SupportRules => EditObject is MarkingRegularLine;
        public bool CanDivide => EditObject.IsSupportRules && SupportPoints.Count > 2;
        private bool AddRuleAvailable => EditObject.IsSupportRules;
        public bool IsSplit => EditObject.PointPair.IsSplit;
        public IEnumerable<RulePanel> RulePanels => ContentPanel.Content.components.OfType<RulePanel>();

        private RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton HoverPartEdgeButton { get; set; }
        private RulePanel HoverRulePanel { get; set; }
        Action LinePropertiesVisibleAction { get; set; }
        private PartEdgeToolMode PartEdgeToolMode { get; }

        object IPropertyEditor.EditObject => EditObject;
        bool IPropertyEditor.IsTemplate => false;

        #endregion

        #region BASIC

        public LinesEditor()
        {
            ContentPanel.Content.autoLayoutPadding = new RectOffset(10, 10, 10, 10);
            PartEdgeToolMode = Tool.CreateToolMode<PartEdgeToolMode>();
            PartEdgeToolMode.Init(this);
        }

        protected override IEnumerable<MarkingLine> GetObjects() => Marking.Lines;
        protected override void OnObjectSelect(MarkingLine editObject)
        {
            ContentPanel.PauseLayout(() =>
            {
                GetRuleEdges(editObject);

                AddLineProperties(EditObject);
                AddRulePanels(editObject);
                AddAddButton();
            });
        }
        protected override void OnObjectUpdate(MarkingLine editObject)
        {
            RefreshSelectedItem();
            GetRuleEdges(editObject);
            SetLinePropertiesVisible();
            RefreshRulePanels();
            SetAddButtonVisible();
        }
        protected override void OnObjectDelete(MarkingLine line)
        {
            var fillers = Marking.GetLineFillers(line).ToArray();

            if (line is MarkingCrosswalkLine crosswalkLine)
                Panel.DeleteCrosswalk(crosswalkLine.Crosswalk);
            foreach (var filler in fillers)
                Panel.DeleteFiller(filler);

            Marking.RemoveLine(line);

            base.OnObjectDelete(line);
        }
        protected override void OnClear()
        {
            base.OnClear();
            HoverRulePanel = null;
            LineProperties = null;
            LinePropertiesVisibleAction = null;
            AddRuleButton = null;
        }
        private void GetRuleEdges(MarkingLine editObject)
        {
            SupportPoints.Clear();
            SupportPoints.AddRange(editObject.RulesEdges);
        }
        private void AddLineProperties(MarkingLine editObject)
        {
            LineProperties = ComponentPool.Get<PropertyGroupPanel>(ContentPanel.Content);
            LineProperties.Init();

            if (editObject is MarkingRegularLine line)
            {
                var aligment = AddAlignmentProperty(line.RawAlignment, IMT.Localize.LineEditor_LineAlignment);
                var clipSidewalk = AddClipSidewalkProperty(line);
                LinePropertiesVisibleAction = () =>
                {
                    clipSidewalk.isVisible = line.PointPair.NetworkType == NetworkType.Road && line.Type == LineType.Regular;
                    aligment.isVisible = IsSplit;
                    LineProperties.isVisible = clipSidewalk.isVisibleSelf || aligment.isVisibleSelf;
                };
            }
            else if (editObject is MarkingStopLine stopLine)
            {
                var start = AddAlignmentProperty(stopLine.RawStartAlignment, IMT.Localize.LineEditor_LineStartAlignment);
                var end = AddAlignmentProperty(stopLine.RawEndAlignment, IMT.Localize.LineEditor_LineEndAlignment);
                LinePropertiesVisibleAction = () =>
                {
                    start.isVisible = stopLine.Start.IsSplit;
                    end.isVisible = stopLine.End.IsSplit;
                    LineProperties.isVisible = start.isVisibleSelf || end.isVisibleSelf;
                };
            }

            SetLinePropertiesVisible();
        }
        private LineAlignmentPropertyPanel AddAlignmentProperty(PropertyEnumValue<Alignment> property, string label)
        {
            var alignment = ComponentPool.Get<LineAlignmentPropertyPanel>(LineProperties, "LineAlignment");

            alignment.Label = label;
            alignment.Init();
            alignment.SelectedObject = property;
            alignment.OnSelectObjectChanged += (value) => property.Value = value;

            return alignment;
        }
        private BoolPropertyPanel AddClipSidewalkProperty(MarkingRegularLine line)
        {
            var clipSidewalk = ComponentPool.Get<BoolPropertyPanel>(LineProperties, nameof(line.ClipSidewalk));

            clipSidewalk.Label = IMT.Localize.LineEditor_ClipSidewalk;
            clipSidewalk.Init();
            clipSidewalk.Value = line.ClipSidewalk;
            clipSidewalk.OnValueChanged += (value) => line.ClipSidewalk.Value = value;

            return clipSidewalk;
        }
        private void AddRulePanels(MarkingLine editObject)
        {
            var isExpand = !Settings.CollapseRules || editObject.RuleCount <= 1;
            foreach (var rule in editObject.Rules)
                AddRulePanel(rule, isExpand);
        }

        private RulePanel AddRulePanel(MarkingLineRawRule rule, bool isExpand)
        {
            var rulePanel = ComponentPool.Get<RulePanel>(ContentPanel.Content);
            rulePanel.Init(this, rule, isExpand);
            rulePanel.OnEnter += RuleMouseEnter;
            rulePanel.OnLeave += RuleMouseLeave;
            return rulePanel;
        }
        private void RemoveRulePanel(RulePanel rulePanel)
        {
            if (HoverRulePanel == rulePanel)
                HoverRulePanel = null;

            ComponentPool.Free(rulePanel);
        }
        private void AddAddButton()
        {
            AddRuleButton = ContentPanel.Content.AddUIComponent<CustomUIButton>();
            AddRuleButton.name = nameof(AddRuleButton);
            AddRuleButton.SetDefaultStyle();
            AddRuleButton.size = new Vector2(ContentPanel.Content.ItemSize.x, 30f);
            AddRuleButton.text = IMT.Localize.LineEditor_AddRuleButton;
            AddRuleButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            AddRuleButton.textPadding.top = 5;
            AddRuleButton.tooltip = IntersectionMarkingTool.AddRuleShortcut;
            AddRuleButton.eventClick += (_, _) => AddRule();
            SetAddButtonVisible();
        }
        private void SetLinePropertiesVisible() => LinePropertiesVisibleAction?.Invoke();
        private void SetAddButtonVisible()
        {
            AddRuleButton.zOrder = -1;
            AddRuleButton.isVisible = AddRuleAvailable;
        }

        private void AddRule()
        {
            if (EditObject is not MarkingRegularLine regularLine)
                return;

            var newRule = regularLine.AddRule(CanDivide);
            var rulePanel = AddRulePanel(newRule, true);
            SetAddButtonVisible();

            ContentPanel.Content.ScrollToBottom();

            if (CanDivide && Settings.QuickRuleSetup)
                SetupRule(rulePanel);

            RefreshEditor();
        }
        private void RefreshRulePanels()
        {
            var rulePanels = RulePanels.ToArray();

            foreach (var rulePanel in rulePanels)
            {
                if (EditObject.ContainsRule(rulePanel.Rule))
                    rulePanel.Refresh();
                else
                    RemoveRulePanel(rulePanel);
            }
            var isExpand = !Settings.CollapseRules || EditObject.RuleCount <= 1;
            foreach (var rule in EditObject.Rules)
            {
                if (!rulePanels.Any(r => r.Rule == rule))
                    AddRulePanel(rule, isExpand);
            }
        }
        public void ExpandRules(bool isExpand)
        {
            foreach (var rulePanel in RulePanels)
            {
                rulePanel.IsExpand = isExpand;
            }
        }
        void IPropertyEditor.RefreshProperties()
        {
            foreach (var rulePanel in RulePanels)
            {
                rulePanel.RefreshProperties();
            }
        }

        #endregion

        #region RULE

        private void SetupRule(RulePanel rulePanel) => SelectRuleEdge(rulePanel.From.Selector, (_) => SelectRuleEdge(rulePanel.To.Selector, (_) => SetStyle(rulePanel)));
        private bool SetStyle(RulePanel rulePanel)
        {
            var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(EditObject.PointPair.NetworkType, EditObject.Type, RegularLineStyle.RegularLineType.Dashed);
            rulePanel.ApplyStyle(style);
            ContentPanel.Content.ScrollToBottom();
            ContentPanel.Content.ScrollIntoViewRecursive(rulePanel);
            return true;
        }
        public void DeleteRule(RulePanel rulePanel)
        {
            if (EditObject is not MarkingRegularLine regularLine)
                return;

            if (Settings.DeleteWarnings && Settings.DeleteWarningsType == 0)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = IMT.Localize.LineEditor_DeleteRuleCaption;
                messageBox.MessageText = $"{IMT.Localize.LineEditor_DeleteRuleMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}";
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                regularLine.RemoveRule(rulePanel.Rule as MarkingLineRawRule<RegularLineStyle>);
                RemoveRulePanel(rulePanel);
                RefreshEditor();
                return true;
            }
        }
        public bool SelectRuleEdge(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton selectPanel) => SelectRuleEdge(selectPanel, null);
        public bool SelectRuleEdge(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == PartEdgeToolMode && selectPanel == PartEdgeToolMode.SelectButton)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(PartEdgeToolMode);
                PartEdgeToolMode.SelectButton = selectPanel;
                PartEdgeToolMode.AfterSelectButton = afterAction;
                selectPanel.Focus();
                return false;
            }
        }
        public void EnterRuleEdge(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton selectButton) => HoverPartEdgeButton = selectButton;
        public void LeaveRuleEdge(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton selectButton) => HoverPartEdgeButton = null;
        private void RuleMouseEnter(RulePanel rulePanel, UIMouseEventParameter eventParam) => HoverRulePanel = rulePanel;
        private void RuleMouseLeave(RulePanel rulePanel, UIMouseEventParameter eventParam)
        {
            var uiView = rulePanel.GetUIView();
            var mouse = uiView.ScreenPointToGUI((eventParam.position + eventParam.moveDelta) / uiView.inputScale);
            var ruleRect = new Rect(ContentPanel.absolutePosition + rulePanel.relativePosition, rulePanel.size);
            var contentRect = new Rect(ContentPanel.absolutePosition, ContentPanel.size);

            if (eventParam.source == rulePanel || !ruleRect.Contains(mouse) || !contentRect.Contains(mouse))
                HoverRulePanel = null;
        }

        #endregion

        #region HANDLERS

        public void AddRuleShortcut()
        {
            if (AddRuleAvailable)
                AddRule();
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            {
                ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = CommonColors.Hover, Width = 2f, AlphaBlend = false });
                HoverRulePanel?.Rule.Line.RenderRule(HoverRulePanel.Rule, new OverlayData(cameraInfo) { Color = HoverAlpha, Width = 2f, AlphaBlend = false });
                HoverPartEdgeButton?.Value?.Render(new OverlayData(cameraInfo) { Color = CommonColors.Hover });
            }
        }

        #endregion
    }
    public class PartEdgeToolMode : BasePanelMode<LinesEditor, RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton, ILinePartEdge>
    {
        protected override bool IsHover => PointsSelector.IsHoverPoint;
        protected override ILinePartEdge Hover => PointsSelector.HoverPoint;
        public PointsSelector<ILinePartEdge> PointsSelector { get; set; }
        protected override void OnSetButton() => PointsSelector = new PointsSelector<ILinePartEdge>(Editor.SupportPoints, SelectButton.Position == EdgePosition.Start ? CommonColors.Green : CommonColors.Red);

        public override void Deactivate()
        {
            base.Deactivate();
            Editor.RefreshEditor();
        }
        public override void OnToolUpdate() => PointsSelector?.OnUpdate();
        public override string GetToolInfo()
        {
            var info = SelectButton?.Position switch
            {
                EdgePosition.Start => Localize.LineEditor_InfoSelectFrom,
                EdgePosition.End => Tool.GetModifierToolTip<RegularLineStyle.RegularLineType>(Localize.LineEditor_InfoSelectTo, Editor.EditObject.PointPair.NetworkType, Editor.EditObject.Type),
                _ => string.Empty,
            };

            return info;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) => PointsSelector.Render(cameraInfo);
    }

    public class LineItemsPanel : ItemsGroupPanel<LineItem, MarkingLine, LineGroup, LineType>
    {
        public override bool GroupingEnable => Settings.GroupLines.value;
        public override int Compare(MarkingLine x, MarkingLine y)
        {
            int result;

            if ((result = x.Start.Enter.CompareTo(y.Start.Enter)) == 0)
                if ((result = x.Start.Index.CompareTo(y.Start.Index)) == 0)
                    if ((result = x.Type.CompareTo(y.Type)) == 0)
                        if ((result = x.End.Enter.CompareTo(y.End.Enter)) == 0)
                            if ((result = x.End.Index.CompareTo(y.End.Index)) == 0)
                                result = x.Type.CompareTo(y.Type);

            return result;
        }
        public override int Compare(LineType x, LineType y) => x.CompareTo(y);

        protected override string GroupName(LineType group) => group.Description();
        protected override LineType SelectGroup(MarkingLine editObject) => editObject.Type;
    }
    public class LineItem : EditItem<MarkingLine, LineIcon>
    {
        private bool HasOverlapped { get; set; }

        public override ModsCommon.UI.SpriteSet ForegroundSprites => !HasOverlapped ? base.ForegroundSprites : new ModsCommon.UI.SpriteSet()
        {
            normal = CommonTextures.BorderBig,
            hovered = CommonTextures.PanelSmall,
            pressed = CommonTextures.PanelSmall,
            focused = CommonTextures.BorderBig,
            disabled = CommonTextures.PanelSmall,
        };
        public override ModsCommon.UI.SpriteSet ForegroundSelectedSprites => !HasOverlapped ? base.ForegroundSelectedSprites : new ModsCommon.UI.SpriteSet(CommonTextures.PanelSmall);

        public override ColorSet ForegroundColors => !HasOverlapped ? base.ForegroundColors : new ColorSet()
        {
            normal = IMTColors.ItemErrorNormal,
            hovered = IMTColors.ItemErrorNormal,
            pressed = IMTColors.ItemErrorPressed,
            focused = IMTColors.ItemErrorFocused,
            disabled = null,
        };
        public override ColorSet ForegroundSelectedColors => !HasOverlapped ? base.ForegroundSelectedColors : new ColorSet(IMTColors.ItemErrorFocused);

        public override ColorSet TextColor => !HasOverlapped ? base.TextColor : new ColorSet(Color.white);
        public override ColorSet TextSelectedColor => !HasOverlapped ? base.TextSelectedColor : new ColorSet(Color.white);

        public override void Refresh()
        {
            base.Refresh();

            SetIcon();

            HasOverlapped = EditObject.HasOverlapped;
            SetStyle();
        }
        private void SetIcon()
        {
            if (!ShowIcon)
                return;

            var rules = EditObject.Rules.ToArray();
            Icon.Count = rules.Length;
            if (rules.Length == 1)
            {
                Icon.Type = rules[0].Style.Value.Type;
                Icon.StyleColor = rules[0].Style.Value is IColorStyle ? rules[0].Style.Value.Color : Color.white;
            }
        }
    }
    public class LineGroup : EditGroup<LineType, LineItem, MarkingLine> { }
}
