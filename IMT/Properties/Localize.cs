namespace IMT
{
	public class Localize
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static ModsCommon.LocalizeManager LocaleManager {get;} = new ModsCommon.LocalizeManager("Localize", typeof(Localize).Assembly);

		/// <summary>
		/// Delete {0}
		/// </summary>
		public static string Tool_DeleteCaption => LocaleManager.GetString("Tool_DeleteCaption", Culture);

		/// <summary>
		/// Do you really want to delete {0} "{1}"?
		/// </summary>
		public static string Tool_DeleteMessage => LocaleManager.GetString("Tool_DeleteMessage", Culture);

		/// <summary>
		/// Apply template
		/// </summary>
		public static string HeaderPanel_ApplyTemplate => LocaleManager.GetString("HeaderPanel_ApplyTemplate", Culture);

		/// <summary>
		/// Save as template
		/// </summary>
		public static string HeaderPanel_SaveAsTemplate => LocaleManager.GetString("HeaderPanel_SaveAsTemplate", Culture);

		/// <summary>
		/// Set as default
		/// </summary>
		public static string HeaderPanel_SetAsDefault => LocaleManager.GetString("HeaderPanel_SetAsDefault", Culture);

		/// <summary>
		/// Unset as default
		/// </summary>
		public static string HeaderPanel_UnsetAsDefault => LocaleManager.GetString("HeaderPanel_UnsetAsDefault", Culture);

		/// <summary>
		/// Add Rule
		/// </summary>
		public static string LineEditor_AddRuleButton => LocaleManager.GetString("LineEditor_AddRuleButton", Culture);

		/// <summary>
		/// Color
		/// </summary>
		public static string StyleOption_Color => LocaleManager.GetString("StyleOption_Color", Culture);

		/// <summary>
		/// Dash length
		/// </summary>
		public static string StyleOption_DashedLength => LocaleManager.GetString("StyleOption_DashedLength", Culture);

		/// <summary>
		/// Delete rule
		/// </summary>
		public static string LineEditor_DeleteRuleCaption => LocaleManager.GetString("LineEditor_DeleteRuleCaption", Culture);

		/// <summary>
		/// Do you really want to delete the rule?
		/// </summary>
		public static string LineEditor_DeleteRuleMessage => LocaleManager.GetString("LineEditor_DeleteRuleMessage", Culture);

		/// <summary>
		/// From
		/// </summary>
		public static string LineRule_From => LocaleManager.GetString("LineRule_From", Culture);

		/// <summary>
		/// Select the rule's "From" point
		/// </summary>
		public static string LineEditor_InfoSelectFrom => LocaleManager.GetString("LineEditor_InfoSelectFrom", Culture);

		/// <summary>
		/// Select the rule's "To" point
		/// </summary>
		public static string LineEditor_InfoSelectTo => LocaleManager.GetString("LineEditor_InfoSelectTo", Culture);

		/// <summary>
		/// line
		/// </summary>
		public static string LineEditor_DeleteCaptionDescription => LocaleManager.GetString("LineEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// Lines
		/// </summary>
		public static string LineEditor_Lines => LocaleManager.GetString("LineEditor_Lines", Culture);

		/// <summary>
		/// Offset
		/// </summary>
		public static string StyleOption_Offset => LocaleManager.GetString("StyleOption_Offset", Culture);

		/// <summary>
		/// Space length
		/// </summary>
		public static string StyleOption_SpaceLength => LocaleManager.GetString("StyleOption_SpaceLength", Culture);

		/// <summary>
		/// Style
		/// </summary>
		public static string Editor_Style => LocaleManager.GetString("Editor_Style", Culture);

		/// <summary>
		/// To
		/// </summary>
		public static string LineRule_To => LocaleManager.GetString("LineRule_To", Culture);

		/// <summary>
		/// Intersection with {0}
		/// </summary>
		public static string LineRule_IntersectWith => LocaleManager.GetString("LineRule_IntersectWith", Culture);

		/// <summary>
		/// Self-edge point {0}
		/// </summary>
		public static string LineRule_SelfEdgePoint => LocaleManager.GetString("LineRule_SelfEdgePoint", Culture);

		/// <summary>
		/// Dashed
		/// </summary>
		public static string LineStyle_Dashed => LocaleManager.GetString("LineStyle_Dashed", Culture);

		/// <summary>
		/// Double dashed
		/// </summary>
		public static string LineStyle_DoubleDashed => LocaleManager.GetString("LineStyle_DoubleDashed", Culture);

		/// <summary>
		/// Double solid
		/// </summary>
		public static string LineStyle_DoubleSolid => LocaleManager.GetString("LineStyle_DoubleSolid", Culture);

		/// <summary>
		/// Solid
		/// </summary>
		public static string LineStyle_Solid => LocaleManager.GetString("LineStyle_Solid", Culture);

		/// <summary>
		/// Just do make markings at intersections
		/// </summary>
		public static string Mod_Description => LocaleManager.GetString("Mod_Description", Culture);

		/// <summary>
		/// Edit node #{0} markings
		/// </summary>
		public static string Panel_NodeCaption => LocaleManager.GetString("Panel_NodeCaption", Culture);

		/// <summary>
		/// point
		/// </summary>
		public static string PointEditor_DeleteCaptionDescription => LocaleManager.GetString("PointEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// Offset
		/// </summary>
		public static string PointEditor_Offset => LocaleManager.GetString("PointEditor_Offset", Culture);

		/// <summary>
		/// Points
		/// </summary>
		public static string PointEditor_Points => LocaleManager.GetString("PointEditor_Points", Culture);

		/// <summary>
		/// Not set
		/// </summary>
		public static string SelectPanel_NotSet => LocaleManager.GetString("SelectPanel_NotSet", Culture);

		/// <summary>
		/// Add new line rule
		/// </summary>
		public static string Settings_ShortcutAddNewLineRule => LocaleManager.GetString("Settings_ShortcutAddNewLineRule", Culture);

		/// <summary>
		/// Delete all node lines
		/// </summary>
		public static string Settings_ShortcutDeleteAllNodeLines => LocaleManager.GetString("Settings_ShortcutDeleteAllNodeLines", Culture);

		/// <summary>
		/// Delete markings from all intersections
		/// </summary>
		public static string Settings_DeleteMarkingButton => LocaleManager.GetString("Settings_DeleteMarkingButton", Culture);

		/// <summary>
		/// Delete all markings
		/// </summary>
		public static string Settings_DeleteMarkingCaption => LocaleManager.GetString("Settings_DeleteMarkingCaption", Culture);

		/// <summary>
		/// Do you really want to remove all markings?
		/// </summary>
		public static string Settings_DeleteMarkingMessage => LocaleManager.GetString("Settings_DeleteMarkingMessage", Culture);

		/// <summary>
		/// Dump markings data to file
		/// </summary>
		public static string Settings_DumpMarkingButton => LocaleManager.GetString("Settings_DumpMarkingButton", Culture);

		/// <summary>
		/// Copy path to clipboard
		/// </summary>
		public static string Settings_CopyPathToClipboard => LocaleManager.GetString("Settings_CopyPathToClipboard", Culture);

		/// <summary>
		/// Dump markings data
		/// </summary>
		public static string Settings_DumpMarkingCaption => LocaleManager.GetString("Settings_DumpMarkingCaption", Culture);

		/// <summary>
		/// Dump failed
		/// </summary>
		public static string Settings_DumpMessageFailed => LocaleManager.GetString("Settings_DumpMessageFailed", Culture);

		/// <summary>
		/// Dump successfully saved to file
		/// </summary>
		public static string Settings_DumpMessageSuccess => LocaleManager.GetString("Settings_DumpMessageSuccess", Culture);

		/// <summary>
		/// Restore markings data from file
		/// </summary>
		public static string Settings_RestoreMarkingButton => LocaleManager.GetString("Settings_RestoreMarkingButton", Culture);

		/// <summary>
		/// Restore markings data
		/// </summary>
		public static string Settings_RestoreMarkingCaption => LocaleManager.GetString("Settings_RestoreMarkingCaption", Culture);

		/// <summary>
		/// Do you really want to restore markings?
		/// </summary>
		public static string Settings_RestoreMarkingMessage => LocaleManager.GetString("Settings_RestoreMarkingMessage", Culture);

		/// <summary>
		/// Markings data restore failed
		/// </summary>
		public static string Settings_RestoreMarkingMessageFailed => LocaleManager.GetString("Settings_RestoreMarkingMessageFailed", Culture);

		/// <summary>
		/// Markings data successfully restored
		/// </summary>
		public static string Settings_RestoreMarkingMessageSuccess => LocaleManager.GetString("Settings_RestoreMarkingMessageSuccess", Culture);

		/// <summary>
		/// Quick rule setup
		/// </summary>
		public static string Settings_QuickRuleSetup => LocaleManager.GetString("Settings_QuickRuleSetup", Culture);

		/// <summary>
		/// Render distance
		/// </summary>
		public static string Settings_RenderDistance => LocaleManager.GetString("Settings_RenderDistance", Culture);

		/// <summary>
		/// Confirm before deleting
		/// </summary>
		public static string Settings_ShowDeleteWarnings => LocaleManager.GetString("Settings_ShowDeleteWarnings", Culture);

		/// <summary>
		/// template
		/// </summary>
		public static string TemplateEditor_DeleteCaptionDescription => LocaleManager.GetString("TemplateEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// Name
		/// </summary>
		public static string TemplateEditor_Name => LocaleManager.GetString("TemplateEditor_Name", Culture);

		/// <summary>
		/// Templates
		/// </summary>
		public static string TemplateEditor_Templates => LocaleManager.GetString("TemplateEditor_Templates", Culture);

		/// <summary>
		/// New template
		/// </summary>
		public static string Template_NewTemplate => LocaleManager.GetString("Template_NewTemplate", Culture);

		/// <summary>
		/// Clear node markings
		/// </summary>
		public static string Tool_ClearMarkingsCaption => LocaleManager.GetString("Tool_ClearMarkingsCaption", Culture);

		/// <summary>
		/// Do you really want to clear all node #{0} markings?
		/// </summary>
		public static string Tool_ClearMarkingsMessage => LocaleManager.GetString("Tool_ClearMarkingsMessage", Culture);

		/// <summary>
		/// Click to create a line
		/// </summary>
		public static string Tool_InfoCreateLine => LocaleManager.GetString("Tool_InfoCreateLine", Culture);

		/// <summary>
		/// Click to delete the line
		/// </summary>
		public static string Tool_InfoDeleteLine => LocaleManager.GetString("Tool_InfoDeleteLine", Culture);

		/// <summary>
		/// Node #{0}
		/// </summary>
		public static string Tool_InfoHoverNode => LocaleManager.GetString("Tool_InfoHoverNode", Culture);

		/// <summary>
		/// Select a node or segment to change markings
		/// </summary>
		public static string Tool_SelectInfo => LocaleManager.GetString("Tool_SelectInfo", Culture);

		/// <summary>
		/// Select endpoint for regular line
		/// </summary>
		public static string Tool_InfoSelectLineEndPoint => LocaleManager.GetString("Tool_InfoSelectLineEndPoint", Culture);

		/// <summary>
		/// Select a point to create or delete a line
		/// </summary>
		public static string Tool_InfoSelectLineStartPoint => LocaleManager.GetString("Tool_InfoSelectLineStartPoint", Culture);

		/// <summary>
		/// [NEW] Fillers. Space between lines can be filled in to indicate the areas prohibited for stopping or
		/// </summary>
		public static string Mod_WhatsNewMessage1_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_2", Culture);

		/// <summary>
		/// Restore
		/// </summary>
		public static string Settings_Restore => LocaleManager.GetString("Settings_Restore", Culture);

		/// <summary>
		/// Invert
		/// </summary>
		public static string StyleOption_Invert => LocaleManager.GetString("StyleOption_Invert", Culture);

		/// <summary>
		/// Width
		/// </summary>
		public static string StyleOption_Width => LocaleManager.GetString("StyleOption_Width", Culture);

		/// <summary>
		/// Solid and Dashed
		/// </summary>
		public static string LineStyle_SolidAndDashed => LocaleManager.GetString("LineStyle_SolidAndDashed", Culture);

		/// <summary>
		/// Solid
		/// </summary>
		public static string LineStyle_StopSolid => LocaleManager.GetString("LineStyle_StopSolid", Culture);

		/// <summary>
		/// Click to delete the stop line
		/// </summary>
		public static string Tool_InfoDeleteStopLine => LocaleManager.GetString("Tool_InfoDeleteStopLine", Culture);

		/// <summary>
		/// Click to create a stop line
		/// </summary>
		public static string Tool_InfoCreateStopLine => LocaleManager.GetString("Tool_InfoCreateStopLine", Culture);

		/// <summary>
		/// Fillers
		/// </summary>
		public static string FillerEditor_Fillers => LocaleManager.GetString("FillerEditor_Fillers", Culture);

		/// <summary>
		/// filler
		/// </summary>
		public static string FillerEditor_DeleteCaptionDescription => LocaleManager.GetString("FillerEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// Stripes
		/// </summary>
		public static string FillerStyle_Stripe => LocaleManager.GetString("FillerStyle_Stripe", Culture);

		/// <summary>
		/// Grid
		/// </summary>
		public static string FillerStyle_Grid => LocaleManager.GetString("FillerStyle_Grid", Culture);

		/// <summary>
		/// Dashed
		/// </summary>
		public static string LineStyle_StopDashed => LocaleManager.GetString("LineStyle_StopDashed", Culture);

		/// <summary>
		/// Step
		/// </summary>
		public static string StyleOption_Step => LocaleManager.GetString("StyleOption_Step", Culture);

		/// <summary>
		/// Angle
		/// </summary>
		public static string StyleOption_Angle => LocaleManager.GetString("StyleOption_Angle", Culture);

		/// <summary>
		/// Click to start creating a filler
		/// </summary>
		public static string Tool_InfoFillerClickStart => LocaleManager.GetString("Tool_InfoFillerClickStart", Culture);

		/// <summary>
		/// Click to finish creating the filler
		/// </summary>
		public static string Tool_InfoFillerClickEnd => LocaleManager.GetString("Tool_InfoFillerClickEnd", Culture);

		/// <summary>
		/// Click to select the next point in the contour
		/// </summary>
		public static string Tool_InfoFillerClickNext => LocaleManager.GetString("Tool_InfoFillerClickNext", Culture);

		/// <summary>
		/// Select a point to start creating a filler
		/// </summary>
		public static string Tool_InfoFillerSelectStart => LocaleManager.GetString("Tool_InfoFillerSelectStart", Culture);

		/// <summary>
		/// Select the next point in the contour
		/// </summary>
		public static string Tool_InfoFillerSelectNext => LocaleManager.GetString("Tool_InfoFillerSelectNext", Culture);

		/// <summary>
		/// Copy
		/// </summary>
		public static string Editor_ColorCopy => LocaleManager.GetString("Editor_ColorCopy", Culture);

		/// <summary>
		/// Paste
		/// </summary>
		public static string Editor_ColorPaste => LocaleManager.GetString("Editor_ColorPaste", Culture);

		/// <summary>
		/// Default
		/// </summary>
		public static string Editor_ColorDefault => LocaleManager.GetString("Editor_ColorDefault", Culture);

		/// <summary>
		/// Solid
		/// </summary>
		public static string FillerStyle_Solid => LocaleManager.GetString("FillerStyle_Solid", Culture);

		/// <summary>
		/// [FIXED] Fix hold Alt, Ctrl and Shift buttons on Mac OS and Linux.
		/// </summary>
		public static string Mod_WhatsNewMessage1_2_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_2_1", Culture);

		/// <summary>
		/// [UPDATED] "What`s New" message will pop-up one time after an update and can be disabled in the Optio
		/// </summary>
		public static string Mod_WhatsNewMessage1_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_1", Culture);

		/// <summary>
		/// Add new filler
		/// </summary>
		public static string Settings_ShortcutAddNewFiller => LocaleManager.GetString("Settings_ShortcutAddNewFiller", Culture);

		/// <summary>
		/// Double solid
		/// </summary>
		public static string LineStyle_StopDouble => LocaleManager.GetString("LineStyle_StopDouble", Culture);

		/// <summary>
		/// Double dashed
		/// </summary>
		public static string LineStyle_StopDoubleDashed => LocaleManager.GetString("LineStyle_StopDoubleDashed", Culture);

		/// <summary>
		/// Copy
		/// </summary>
		public static string HeaderPanel_StyleCopy => LocaleManager.GetString("HeaderPanel_StyleCopy", Culture);

		/// <summary>
		/// Paste
		/// </summary>
		public static string HeaderPanel_StylePaste => LocaleManager.GetString("HeaderPanel_StylePaste", Culture);

		/// <summary>
		/// Chevron
		/// </summary>
		public static string FillerStyle_Chevron => LocaleManager.GetString("FillerStyle_Chevron", Culture);

		/// <summary>
		/// Angle between
		/// </summary>
		public static string StyleOption_AngleBetween => LocaleManager.GetString("StyleOption_AngleBetween", Culture);

		/// <summary>
		/// Turn
		/// </summary>
		public static string StyleOption_Turn => LocaleManager.GetString("StyleOption_Turn", Culture);

		/// <summary>
		/// There are no lines at this intersection yet.
		/// </summary>
		public static string LineEditor_EmptyMessage => LocaleManager.GetString("LineEditor_EmptyMessage", Culture);

		/// <summary>
		/// There are no fillers at this intersection yet.
		/// </summary>
		public static string FillerEditor_EmptyMessage => LocaleManager.GetString("FillerEditor_EmptyMessage", Culture);

		/// <summary>
		/// The list of templates is empty.
		/// </summary>
		public static string TemplateEditor_EmptyMessage => LocaleManager.GetString("TemplateEditor_EmptyMessage", Culture);

		/// <summary>
		/// Alignment
		/// </summary>
		public static string StyleOption_Alignment => LocaleManager.GetString("StyleOption_Alignment", Culture);

		/// <summary>
		/// Click to create a perpendicular line
		/// </summary>
		public static string Tool_InfoCreateNormalLine => LocaleManager.GetString("Tool_InfoCreateNormalLine", Culture);

		/// <summary>
		/// Click to delete the perpendicular line
		/// </summary>
		public static string Tool_InfoDeleteNormalLine => LocaleManager.GetString("Tool_InfoDeleteNormalLine", Culture);

		/// <summary>
		/// Zebra
		/// </summary>
		public static string CrosswalkStyle_Zebra => LocaleManager.GetString("CrosswalkStyle_Zebra", Culture);

		/// <summary>
		/// Existing crosswalk
		/// </summary>
		public static string CrosswalkStyle_Existent => LocaleManager.GetString("CrosswalkStyle_Existent", Culture);

		/// <summary>
		/// Offset between
		/// </summary>
		public static string StyleOption_OffsetBetween => LocaleManager.GetString("StyleOption_OffsetBetween", Culture);

		/// <summary>
		/// Crosswalks
		/// </summary>
		public static string CrosswalkEditor_Crosswalks => LocaleManager.GetString("CrosswalkEditor_Crosswalks", Culture);

		/// <summary>
		/// There are no crosswalks at this intersection yet.
		/// </summary>
		public static string CrosswalkEditor_EmptyMessage => LocaleManager.GetString("CrosswalkEditor_EmptyMessage", Culture);

		/// <summary>
		/// crosswalk
		/// </summary>
		public static string CrossWalkEditor_DeleteCaptionDescription => LocaleManager.GetString("CrossWalkEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// Crosswalks
		/// </summary>
		public static string CrosswalkStyle_Group => LocaleManager.GetString("CrosswalkStyle_Group", Culture);

		/// <summary>
		/// Fillers
		/// </summary>
		public static string FillerStyle_Group => LocaleManager.GetString("FillerStyle_Group", Culture);

		/// <summary>
		/// Regular lines
		/// </summary>
		public static string LineStyle_RegularLinesGroup => LocaleManager.GetString("LineStyle_RegularLinesGroup", Culture);

		/// <summary>
		/// Stop lines
		/// </summary>
		public static string LineStyle_StopLinesGroup => LocaleManager.GetString("LineStyle_StopLinesGroup", Culture);

		/// <summary>
		/// Group lines
		/// </summary>
		public static string Settings_GroupLines => LocaleManager.GetString("Settings_GroupLines", Culture);

		/// <summary>
		/// Group templates
		/// </summary>
		public static string Settings_GroupTemplates => LocaleManager.GetString("Settings_GroupTemplates", Culture);

		/// <summary>
		/// Click to create a crosswalk
		/// </summary>
		public static string Tool_InfoCreateCrosswalk => LocaleManager.GetString("Tool_InfoCreateCrosswalk", Culture);

		/// <summary>
		/// Click to delete the crosswalk
		/// </summary>
		public static string Tool_InfoDeleteCrosswalk => LocaleManager.GetString("Tool_InfoDeleteCrosswalk", Culture);

		/// <summary>
		/// By style
		/// </summary>
		public static string Settings_GroupTemplatesByStyle => LocaleManager.GetString("Settings_GroupTemplatesByStyle", Culture);

		/// <summary>
		/// By type
		/// </summary>
		public static string Settings_GroupTemplatesByType => LocaleManager.GetString("Settings_GroupTemplatesByType", Culture);

		/// <summary>
		/// Double zebra
		/// </summary>
		public static string CrosswalkStyle_DoubleZebra => LocaleManager.GetString("CrosswalkStyle_DoubleZebra", Culture);

		/// <summary>
		/// Line width
		/// </summary>
		public static string StyleOption_LineWidth => LocaleManager.GetString("StyleOption_LineWidth", Culture);

		/// <summary>
		/// Parallel solid lines
		/// </summary>
		public static string CrosswalkStyle_ParallelSolidLines => LocaleManager.GetString("CrosswalkStyle_ParallelSolidLines", Culture);

		/// <summary>
		/// Left border
		/// </summary>
		public static string CrosswalkEditor_LeftBorder => LocaleManager.GetString("CrosswalkEditor_LeftBorder", Culture);

		/// <summary>
		/// Right border
		/// </summary>
		public static string CrosswalkEditor_RightBorder => LocaleManager.GetString("CrosswalkEditor_RightBorder", Culture);

		/// <summary>
		/// Select the crosswalk's left border
		/// </summary>
		public static string CrosswalkEditor_InfoSelectLeftBorder => LocaleManager.GetString("CrosswalkEditor_InfoSelectLeftBorder", Culture);

		/// <summary>
		/// Select the crosswalk's right border
		/// </summary>
		public static string CrosswalkEditor_InfoSelectRightBorder => LocaleManager.GetString("CrosswalkEditor_InfoSelectRightBorder", Culture);

		/// <summary>
		/// Group points overlay
		/// </summary>
		public static string Settings_GroupPointsOverlay => LocaleManager.GetString("Settings_GroupPointsOverlay", Culture);

		/// <summary>
		/// Arrange in a circle
		/// </summary>
		public static string Settings_GroupPointsArrangeCircle => LocaleManager.GetString("Settings_GroupPointsArrangeCircle", Culture);

		/// <summary>
		/// Arrange in a line
		/// </summary>
		public static string Settings_GroupPointsArrangeLine => LocaleManager.GetString("Settings_GroupPointsArrangeLine", Culture);

		/// <summary>
		/// This rule is overlapped by another rule.
		/// </summary>
		public static string LineEditor_RuleOverlappedWarning => LocaleManager.GetString("LineEditor_RuleOverlappedWarning", Culture);

		/// <summary>
		/// Crosswalk's left border
		/// </summary>
		public static string LineRule_LeftBorder => LocaleManager.GetString("LineRule_LeftBorder", Culture);

		/// <summary>
		/// Crosswalk's right border
		/// </summary>
		public static string LineRule_RightBorder => LocaleManager.GetString("LineRule_RightBorder", Culture);

		/// <summary>
		/// Reset
		/// </summary>
		public static string CrosswalkStyle_ResetBorder => LocaleManager.GetString("CrosswalkStyle_ResetBorder", Culture);

		/// <summary>
		/// Select endpoint
		/// </summary>
		public static string Tool_InfoSelectCrosswalkEndPoint => LocaleManager.GetString("Tool_InfoSelectCrosswalkEndPoint", Culture);

		/// <summary>
		/// Select a point to
		/// </summary>
		public static string Tool_InfoSelectCrosswalkStartPoint => LocaleManager.GetString("Tool_InfoSelectCrosswalkStartPoint", Culture);

		/// <summary>
		/// Parallel dashed lines
		/// </summary>
		public static string CrosswalkStyle_ParallelDashedLines => LocaleManager.GetString("CrosswalkStyle_ParallelDashedLines", Culture);

		/// <summary>
		/// Ladder
		/// </summary>
		public static string CrosswalkStyle_Ladder => LocaleManager.GetString("CrosswalkStyle_Ladder", Culture);

		/// <summary>
		/// No templates
		/// </summary>
		public static string HeaderPanel_NoTemplates => LocaleManager.GetString("HeaderPanel_NoTemplates", Culture);

		/// <summary>
		/// Solid
		/// </summary>
		public static string CrosswalkStyle_Solid => LocaleManager.GetString("CrosswalkStyle_Solid", Culture);

		/// <summary>
		/// Crosswalk lines
		/// </summary>
		public static string LineStyle_CrosswalkLinesGroup => LocaleManager.GetString("LineStyle_CrosswalkLinesGroup", Culture);

		/// <summary>
		/// crosswalk
		/// </summary>
		public static string CrossWalkEditor_DeleteMessageDescription => LocaleManager.GetString("CrossWalkEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// filler
		/// </summary>
		public static string FillerEditor_DeleteMessageDescription => LocaleManager.GetString("FillerEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// line
		/// </summary>
		public static string LineEditor_DeleteMessageDescription => LocaleManager.GetString("LineEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// point
		/// </summary>
		public static string PointEditor_DeleteMessageDescription => LocaleManager.GetString("PointEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// template
		/// </summary>
		public static string TemplateEditor_DeleteMessageDescription => LocaleManager.GetString("TemplateEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// [NEW] Added new filler style - Chevron (<<<<<).
		/// </summary>
		public static string Mod_WhatsNewMessage1_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_3", Culture);

		/// <summary>
		/// Load was completed with errors.
		/// </summary>
		public static string Mod_LoadFailed => LocaleManager.GetString("Mod_LoadFailed", Culture);

		/// <summary>
		/// Chessboard
		/// </summary>
		public static string CrosswalkStyle_ChessBoard => LocaleManager.GetString("CrosswalkStyle_ChessBoard", Culture);

		/// <summary>
		/// Line count
		/// </summary>
		public static string StyleOption_LineCount => LocaleManager.GetString("StyleOption_LineCount", Culture);

		/// <summary>
		/// Triangle base
		/// </summary>
		public static string StyleOption_SharkToothBase => LocaleManager.GetString("StyleOption_SharkToothBase", Culture);

		/// <summary>
		/// Triangle height
		/// </summary>
		public static string StyleOption_SharkToothHeight => LocaleManager.GetString("StyleOption_SharkToothHeight", Culture);

		/// <summary>
		/// Space between
		/// </summary>
		public static string StyleOption_SharkToothSpace => LocaleManager.GetString("StyleOption_SharkToothSpace", Culture);

		/// <summary>
		/// Square side
		/// </summary>
		public static string StyleOption_SquareSide => LocaleManager.GetString("StyleOption_SquareSide", Culture);

		/// <summary>
		/// Shark teeth
		/// </summary>
		public static string LineStyle_SharkTeeth => LocaleManager.GetString("LineStyle_SharkTeeth", Culture);

		/// <summary>
		/// Shark teeth
		/// </summary>
		public static string LineStyle_StopSharkTeeth => LocaleManager.GetString("LineStyle_StopSharkTeeth", Culture);

		/// <summary>
		/// Solid and dashed
		/// </summary>
		public static string LineStyle_StopSolidAndDashed => LocaleManager.GetString("LineStyle_StopSolidAndDashed", Culture);

		/// <summary>
		/// Clear markings
		/// </summary>
		public static string Panel_ClearMarking => LocaleManager.GetString("Panel_ClearMarking", Culture);

		/// <summary>
		/// Copy markings
		/// </summary>
		public static string Panel_CopyMarking => LocaleManager.GetString("Panel_CopyMarking", Culture);

		/// <summary>
		/// Paste markings
		/// </summary>
		public static string Panel_PasteMarking => LocaleManager.GetString("Panel_PasteMarking", Culture);

		/// <summary>
		/// Paste markings
		/// </summary>
		public static string Tool_PasteMarkingsCaption => LocaleManager.GetString("Tool_PasteMarkingsCaption", Culture);

		/// <summary>
		/// Do you really want to paste markings?
		/// </summary>
		public static string Tool_PasteMarkingsMessage => LocaleManager.GetString("Tool_PasteMarkingsMessage", Culture);

		/// <summary>
		/// Crosswalks modifier
		/// </summary>
		public static string Settings_CrosswalksModifier => LocaleManager.GetString("Settings_CrosswalksModifier", Culture);

		/// <summary>
		/// Fillers modifier
		/// </summary>
		public static string Settings_FillersModifier => LocaleManager.GetString("Settings_FillersModifier", Culture);

		/// <summary>
		/// Regular lines modifier
		/// </summary>
		public static string Settings_RegularLinesModifier => LocaleManager.GetString("Settings_RegularLinesModifier", Culture);

		/// <summary>
		/// Stop lines modifier
		/// </summary>
		public static string Settings_StopLinesModifier => LocaleManager.GetString("Settings_StopLinesModifier", Culture);

		/// <summary>
		/// Not set
		/// </summary>
		public static string Settings_StyleModifierNotSet => LocaleManager.GetString("Settings_StyleModifierNotSet", Culture);

		/// <summary>
		/// Without modifier
		/// </summary>
		public static string Settings_StyleModifierWithout => LocaleManager.GetString("Settings_StyleModifierWithout", Culture);

		/// <summary>
		/// Always
		/// </summary>
		public static string Settings_ShowDeleteWarningsAlways => LocaleManager.GetString("Settings_ShowDeleteWarningsAlways", Culture);

		/// <summary>
		/// Only if it affects other items
		/// </summary>
		public static string Settings_ShowDeleteWarningsOnlyDependences => LocaleManager.GetString("Settings_ShowDeleteWarningsOnlyDependences", Culture);

		/// <summary>
		/// Dependent items will also be removed:
		/// </summary>
		public static string Tool_DeleteDependence => LocaleManager.GetString("Tool_DeleteDependence", Culture);

		/// <summary>
		/// Crosswalk borders - {0}
		/// </summary>
		public static string Tool_DeleteDependenceCrosswalkBorders => LocaleManager.GetString("Tool_DeleteDependenceCrosswalkBorders", Culture);

		/// <summary>
		/// Crosswalks - {0}
		/// </summary>
		public static string Tool_DeleteDependenceCrosswalks => LocaleManager.GetString("Tool_DeleteDependenceCrosswalks", Culture);

		/// <summary>
		/// Fillers - {0}
		/// </summary>
		public static string Tool_DeleteDependenceFillers => LocaleManager.GetString("Tool_DeleteDependenceFillers", Culture);

		/// <summary>
		/// Lines - {0}
		/// </summary>
		public static string Tool_DeleteDependenceLines => LocaleManager.GetString("Tool_DeleteDependenceLines", Culture);

		/// <summary>
		/// Rules - {0}
		/// </summary>
		public static string Tool_DeleteDependenceRules => LocaleManager.GetString("Tool_DeleteDependenceRules", Culture);

		/// <summary>
		/// Backup
		/// </summary>
		public static string Settings_BackupTab => LocaleManager.GetString("Settings_BackupTab", Culture);

		/// <summary>
		/// Display and usage
		/// </summary>
		public static string Settings_DisplayAndUsage => LocaleManager.GetString("Settings_DisplayAndUsage", Culture);

		/// <summary>
		/// Groupings
		/// </summary>
		public static string Settings_Groupings => LocaleManager.GetString("Settings_Groupings", Culture);

		/// <summary>
		/// Shortcuts and modifiers
		/// </summary>
		public static string Settings_ShortcutsAndModifiersTab => LocaleManager.GetString("Settings_ShortcutsAndModifiersTab", Culture);

		/// <summary>
		/// Duplicate
		/// </summary>
		public static string HeaderPanel_Duplicate => LocaleManager.GetString("HeaderPanel_Duplicate", Culture);

		/// <summary>
		/// Copy
		/// </summary>
		public static string Template_DuplicateTemplateSuffix => LocaleManager.GetString("Template_DuplicateTemplateSuffix", Culture);

		/// <summary>
		/// Edge
		/// </summary>
		public static string StyleOption_Edge => LocaleManager.GetString("StyleOption_Edge", Culture);

		/// <summary>
		/// Vertex
		/// </summary>
		public static string StyleOption_Vertex => LocaleManager.GetString("StyleOption_Vertex", Culture);

		/// <summary>
		/// Reset all points offset
		/// </summary>
		public static string Settings_ShortcutResetPointsOffset => LocaleManager.GetString("Settings_ShortcutResetPointsOffset", Culture);

		/// <summary>
		/// Reset all points offset
		/// </summary>
		public static string Tool_ResetOffsetsCaption => LocaleManager.GetString("Tool_ResetOffsetsCaption", Culture);

		/// <summary>
		/// Do you really want to reset all node #{0} points offset?
		/// </summary>
		public static string Tool_ResetOffsetsMessage => LocaleManager.GetString("Tool_ResetOffsetsMessage", Culture);

		/// <summary>
		/// Drag and drop circles to set roads order
		/// </summary>
		public static string Tool_InfoRoadsDrag => LocaleManager.GetString("Tool_InfoRoadsDrag", Culture);

		/// <summary>
		/// Drop circle inside/outside target to set/unset roads order
		/// </summary>
		public static string Tool_InfoRoadsDrop => LocaleManager.GetString("Tool_InfoRoadsDrop", Culture);

		/// <summary>
		/// Copy markings
		/// </summary>
		public static string Settings_ShortcutCopyMarking => LocaleManager.GetString("Settings_ShortcutCopyMarking", Culture);

		/// <summary>
		/// Paste markings
		/// </summary>
		public static string Settings_ShortcutPasteMarking => LocaleManager.GetString("Settings_ShortcutPasteMarking", Culture);

		/// <summary>
		/// Inverse order
		/// </summary>
		public static string Tool_InfoInverseOrder => LocaleManager.GetString("Tool_InfoInverseOrder", Culture);

		/// <summary>
		/// Turn clockwise
		/// </summary>
		public static string Tool_InfoTurnClockwise => LocaleManager.GetString("Tool_InfoTurnClockwise", Culture);

		/// <summary>
		/// Turn counter-clockwise
		/// </summary>
		public static string Tool_InfoTurnСounterClockwise => LocaleManager.GetString("Tool_InfoTurnСounterClockwise", Culture);

		/// <summary>
		/// Center
		/// </summary>
		public static string StyleOption_AlignmentCenter => LocaleManager.GetString("StyleOption_AlignmentCenter", Culture);

		/// <summary>
		/// Left
		/// </summary>
		public static string StyleOption_AlignmentLeft => LocaleManager.GetString("StyleOption_AlignmentLeft", Culture);

		/// <summary>
		/// Right
		/// </summary>
		public static string StyleOption_AlignmentRight => LocaleManager.GetString("StyleOption_AlignmentRight", Culture);

		/// <summary>
		/// Unnamed template
		/// </summary>
		public static string TemplateEditor_UnnamedTemplate => LocaleManager.GetString("TemplateEditor_UnnamedTemplate", Culture);

		/// <summary>
		/// Name is already used
		/// </summary>
		public static string TemplateEditor_NameExistCaption => LocaleManager.GetString("TemplateEditor_NameExistCaption", Culture);

		/// <summary>
		/// The name "{0}" is already used by another template.
		/// </summary>
		public static string TemplateEditor_NameExistMessage => LocaleManager.GetString("TemplateEditor_NameExistMessage", Culture);

		/// <summary>
		/// Backup markings
		/// </summary>
		public static string Settings_BackupMarking => LocaleManager.GetString("Settings_BackupMarking", Culture);

		/// <summary>
		/// Backup templates
		/// </summary>
		public static string Settings_BackupTemplates => LocaleManager.GetString("Settings_BackupTemplates", Culture);

		/// <summary>
		/// Delete all templates
		/// </summary>
		public static string Settings_DeleteTemplatesButton => LocaleManager.GetString("Settings_DeleteTemplatesButton", Culture);

		/// <summary>
		/// Delete all templates
		/// </summary>
		public static string Settings_DeleteTemplatesCaption => LocaleManager.GetString("Settings_DeleteTemplatesCaption", Culture);

		/// <summary>
		/// Do you really want to remove all templates?
		/// </summary>
		public static string Settings_DeleteTemplatesMessage => LocaleManager.GetString("Settings_DeleteTemplatesMessage", Culture);

		/// <summary>
		/// Dump templates to file
		/// </summary>
		public static string Settings_DumpTemplatesButton => LocaleManager.GetString("Settings_DumpTemplatesButton", Culture);

		/// <summary>
		/// Dump templates
		/// </summary>
		public static string Settings_DumpTemplatesCaption => LocaleManager.GetString("Settings_DumpTemplatesCaption", Culture);

		/// <summary>
		/// Restore templates from file
		/// </summary>
		public static string Settings_RestoreTemplatesButton => LocaleManager.GetString("Settings_RestoreTemplatesButton", Culture);

		/// <summary>
		/// Restore templates
		/// </summary>
		public static string Settings_RestoreTemplatesCaption => LocaleManager.GetString("Settings_RestoreTemplatesCaption", Culture);

		/// <summary>
		/// Do you really want to restore templates?
		/// </summary>
		public static string Settings_RestoreTemplatesMessage => LocaleManager.GetString("Settings_RestoreTemplatesMessage", Culture);

		/// <summary>
		/// Templates restore failed
		/// </summary>
		public static string Settings_RestoreTemplatesMessageFailed => LocaleManager.GetString("Settings_RestoreTemplatesMessageFailed", Culture);

		/// <summary>
		/// Templates successfully restored
		/// </summary>
		public static string Settings_RestoreTemplatesMessageSuccess => LocaleManager.GetString("Settings_RestoreTemplatesMessageSuccess", Culture);

		/// <summary>
		/// Apply
		/// </summary>
		public static string Tool_Apply => LocaleManager.GetString("Tool_Apply", Culture);

		/// <summary>
		/// Continue
		/// </summary>
		public static string Tool_Continue => LocaleManager.GetString("Tool_Continue", Culture);

		/// <summary>
		/// Don't apply
		/// </summary>
		public static string Tool_NotApply => LocaleManager.GetString("Tool_NotApply", Culture);

		/// <summary>
		/// Apply
		/// </summary>
		public static string Tool_InfoPasteApply => LocaleManager.GetString("Tool_InfoPasteApply", Culture);

		/// <summary>
		/// Don't apply
		/// </summary>
		public static string Tool_infoPasteNotApply => LocaleManager.GetString("Tool_infoPasteNotApply", Culture);

		/// <summary>
		/// Reset
		/// </summary>
		public static string Tool_InfoPasteReset => LocaleManager.GetString("Tool_InfoPasteReset", Culture);

		/// <summary>
		/// Edit markings order
		/// </summary>
		public static string Panel_EditMarking => LocaleManager.GetString("Panel_EditMarking", Culture);

		/// <summary>
		/// Change markings order
		/// </summary>
		public static string Settings_ShortcutEditMarking => LocaleManager.GetString("Settings_ShortcutEditMarking", Culture);

		/// <summary>
		/// Roads have changed
		/// </summary>
		public static string Tool_RoadsWasChangedCaption => LocaleManager.GetString("Tool_RoadsWasChangedCaption", Culture);

		/// <summary>
		/// The roads at this intersection have changed, therefore the existing markings cannot be applied autom
		/// </summary>
		public static string Tool_RoadsWasChangedMessage => LocaleManager.GetString("Tool_RoadsWasChangedMessage", Culture);

		/// <summary>
		/// Exit edit order mode
		/// </summary>
		public static string Tool_EndEditOrderCaption => LocaleManager.GetString("Tool_EndEditOrderCaption", Culture);

		/// <summary>
		/// Do you want to exit edit mode and apply changes?
		/// </summary>
		public static string Tool_EndEditOrderMessage => LocaleManager.GetString("Tool_EndEditOrderMessage", Culture);

		/// <summary>
		/// Drag and drop circle to set points order
		/// </summary>
		public static string Tool_InfoPointsDrag => LocaleManager.GetString("Tool_InfoPointsDrag", Culture);

		/// <summary>
		/// Drop circle inside/outside target to set/unset points order
		/// </summary>
		public static string Tool_InfoPointsDrop => LocaleManager.GetString("Tool_InfoPointsDrop", Culture);

		/// <summary>
		/// Solid in center
		/// </summary>
		public static string StyleOption_SolidInCenter => LocaleManager.GetString("StyleOption_SolidInCenter", Culture);

		/// <summary>
		/// No
		/// </summary>
		public static string StyleOption_No => LocaleManager.GetString("StyleOption_No", Culture);

		/// <summary>
		/// Yes
		/// </summary>
		public static string StyleOption_Yes => LocaleManager.GetString("StyleOption_Yes", Culture);

		/// <summary>
		/// Hold {0} to create a crosswalk
		/// </summary>
		public static string Tool_InfoStartCreateCrosswalk => LocaleManager.GetString("Tool_InfoStartCreateCrosswalk", Culture);

		/// <summary>
		/// Hold {0} to create a filler
		/// </summary>
		public static string Tool_InfoStartCreateFiller => LocaleManager.GetString("Tool_InfoStartCreateFiller", Culture);

		/// <summary>
		/// Hold {0} and drag a point to change offset
		/// </summary>
		public static string Tool_InfoStartDragPointMode => LocaleManager.GetString("Tool_InfoStartDragPointMode", Culture);

		/// <summary>
		/// [WARNING] Default templates setting will be reset.
		/// </summary>
		public static string Mod_WhatsNewMessage1_4 => LocaleManager.GetString("Mod_WhatsNewMessage1_4", Culture);

		/// <summary>
		/// This action cannot be undone.
		/// </summary>
		public static string MessageBox_CantUndone => LocaleManager.GetString("MessageBox_CantUndone", Culture);

		/// <summary>
		/// Empty
		/// </summary>
		public static string LineStyle_Empty => LocaleManager.GetString("LineStyle_Empty", Culture);

		/// <summary>
		/// Create edge lines
		/// </summary>
		public static string Settings_ShortcutCreateEdgeLines => LocaleManager.GetString("Settings_ShortcutCreateEdgeLines", Culture);

		/// <summary>
		/// Create edge lines
		/// </summary>
		public static string Panel_CreateEdgeLines => LocaleManager.GetString("Panel_CreateEdgeLines", Culture);

		/// <summary>
		/// Reset all points offset
		/// </summary>
		public static string Panel_ResetOffset => LocaleManager.GetString("Panel_ResetOffset", Culture);

		/// <summary>
		/// Quick crosswalk borders setup
		/// </summary>
		public static string Settings_QuickBorderSetup => LocaleManager.GetString("Settings_QuickBorderSetup", Culture);

		/// <summary>
		/// Markings under tracks
		/// </summary>
		public static string Settings_RailUnderMarking => LocaleManager.GetString("Settings_RailUnderMarking", Culture);

		/// <summary>
		/// Warning: this option changes game objects render order
		/// </summary>
		public static string Settings_RailUnderMarkingWarning => LocaleManager.GetString("Settings_RailUnderMarkingWarning", Culture);

		/// <summary>
		/// [NEW] Added the ability to draw marking under tracks. This can be disabled in the options. WARNING: 
		/// </summary>
		public static string Mod_WhatsNewMessage1_4_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_4_1", Culture);

		/// <summary>
		/// Save as asset
		/// </summary>
		public static string HeaderPanel_SaveAsAsset => LocaleManager.GetString("HeaderPanel_SaveAsAsset", Culture);

		/// <summary>
		/// {0}
		/// </summary>
		public static string TemplateEditor_TemplateByAuthor => LocaleManager.GetString("TemplateEditor_TemplateByAuthor", Culture);

		/// <summary>
		/// The list of presets is empty.
		/// </summary>
		public static string PresetEditor_EmptyMessage => LocaleManager.GetString("PresetEditor_EmptyMessage", Culture);

		/// <summary>
		/// Presets
		/// </summary>
		public static string PresetEditor_Presets => LocaleManager.GetString("PresetEditor_Presets", Culture);

		/// <summary>
		/// Save as preset
		/// </summary>
		public static string Panel_SaveAsPreset => LocaleManager.GetString("Panel_SaveAsPreset", Culture);

		/// <summary>
		/// Save as preset
		/// </summary>
		public static string Settings_ShortcutSaveAsPreset => LocaleManager.GetString("Settings_ShortcutSaveAsPreset", Culture);

		/// <summary>
		/// No presets
		/// </summary>
		public static string HeaderPanel_NoPresets => LocaleManager.GetString("HeaderPanel_NoPresets", Culture);

		/// <summary>
		/// Apply preset
		/// </summary>
		public static string PresetEditor_ApplyPreset => LocaleManager.GetString("PresetEditor_ApplyPreset", Culture);

		/// <summary>
		/// New preset
		/// </summary>
		public static string Preset_NewPreset => LocaleManager.GetString("Preset_NewPreset", Culture);

		/// <summary>
		/// Exit applying preset mode
		/// </summary>
		public static string Tool_EndApplyPresetCaption => LocaleManager.GetString("Tool_EndApplyPresetCaption", Culture);

		/// <summary>
		/// Do you want to apply a preset?
		/// </summary>
		public static string Tool_EndApplyPresetMessage => LocaleManager.GetString("Tool_EndApplyPresetMessage", Culture);

		/// <summary>
		/// Author
		/// </summary>
		public static string TemplateEditor_Author => LocaleManager.GetString("TemplateEditor_Author", Culture);

		/// <summary>
		/// Applies after game restart
		/// </summary>
		public static string Settings_ApplyAfterRestart => LocaleManager.GetString("Settings_ApplyAfterRestart", Culture);

		/// <summary>
		/// Load markings assets
		/// </summary>
		public static string Settings_LoadMarkingAssets => LocaleManager.GetString("Settings_LoadMarkingAssets", Culture);

		/// <summary>
		/// Crosswalks:
		/// </summary>
		public static string PresetInfo_Crosswalks => LocaleManager.GetString("PresetInfo_Crosswalks", Culture);

		/// <summary>
		/// Fillers:
		/// </summary>
		public static string PresetInfo_Fillers => LocaleManager.GetString("PresetInfo_Fillers", Culture);

		/// <summary>
		/// Lines:
		/// </summary>
		public static string PresetInfo_Lines => LocaleManager.GetString("PresetInfo_Lines", Culture);

		/// <summary>
		/// No screenshot
		/// </summary>
		public static string PresetInfo_NoScreenshot => LocaleManager.GetString("PresetInfo_NoScreenshot", Culture);

		/// <summary>
		/// Road #{0} points:
		/// </summary>
		public static string PresetInfo_RoadPoints => LocaleManager.GetString("PresetInfo_RoadPoints", Culture);

		/// <summary>
		/// Roads:
		/// </summary>
		public static string PresetInfo_Roads => LocaleManager.GetString("PresetInfo_Roads", Culture);

		/// <summary>
		/// Cut new line by crosswalks
		/// </summary>
		public static string Settings_CutLineByCrosswalk => LocaleManager.GetString("Settings_CutLineByCrosswalk", Culture);

		/// <summary>
		/// Cut lines by this crosswalk
		/// </summary>
		public static string HeaderPanel_CutLinesByCrosswalk => LocaleManager.GetString("HeaderPanel_CutLinesByCrosswalk", Culture);

		/// <summary>
		/// Backup presets
		/// </summary>
		public static string Settings_BackupPresets => LocaleManager.GetString("Settings_BackupPresets", Culture);

		/// <summary>
		/// Delete all presets
		/// </summary>
		public static string Settings_DeletePresetsButton => LocaleManager.GetString("Settings_DeletePresetsButton", Culture);

		/// <summary>
		/// Delete all presets
		/// </summary>
		public static string Settings_DeletePresetsCaption => LocaleManager.GetString("Settings_DeletePresetsCaption", Culture);

		/// <summary>
		/// Do you really want to remove all presets?
		/// </summary>
		public static string Settings_DeletePresetsMessage => LocaleManager.GetString("Settings_DeletePresetsMessage", Culture);

		/// <summary>
		/// Dump presets to file
		/// </summary>
		public static string Settings_DumpPresetsButton => LocaleManager.GetString("Settings_DumpPresetsButton", Culture);

		/// <summary>
		/// Dump presets
		/// </summary>
		public static string Settings_DumpPresetsCaption => LocaleManager.GetString("Settings_DumpPresetsCaption", Culture);

		/// <summary>
		/// Restore presets from file
		/// </summary>
		public static string Settings_RestorePresetsButton => LocaleManager.GetString("Settings_RestorePresetsButton", Culture);

		/// <summary>
		/// Restore presets
		/// </summary>
		public static string Settings_RestorePresetsCaption => LocaleManager.GetString("Settings_RestorePresetsCaption", Culture);

		/// <summary>
		/// Do you really want to restore presets?
		/// </summary>
		public static string Settings_RestorePresetsMessage => LocaleManager.GetString("Settings_RestorePresetsMessage", Culture);

		/// <summary>
		/// Presets restore failed
		/// </summary>
		public static string Settings_RestorePresetsMessageFailed => LocaleManager.GetString("Settings_RestorePresetsMessageFailed", Culture);

		/// <summary>
		/// Presets successfully restored
		/// </summary>
		public static string Settings_RestorePresetsMessageSuccess => LocaleManager.GetString("Settings_RestorePresetsMessageSuccess", Culture);

		/// <summary>
		/// Cut lines by all crosswalks
		/// </summary>
		public static string Settings_ShortcutCutLinesByCrosswalks => LocaleManager.GetString("Settings_ShortcutCutLinesByCrosswalks", Culture);

		/// <summary>
		/// Cut lines by all crosswalks
		/// </summary>
		public static string Panel_CutLinesByCrosswalks => LocaleManager.GetString("Panel_CutLinesByCrosswalks", Culture);

		/// <summary>
		/// Rewrite preset asset
		/// </summary>
		public static string PresetEditor_RewriteCaption => LocaleManager.GetString("PresetEditor_RewriteCaption", Culture);

		/// <summary>
		/// To change its name requires rewriting the asset file. Continue?
		/// </summary>
		public static string PresetEditor_RewriteMessage => LocaleManager.GetString("PresetEditor_RewriteMessage", Culture);

		/// <summary>
		/// Rewrite template asset
		/// </summary>
		public static string TemplateEditor_RewriteCaption => LocaleManager.GetString("TemplateEditor_RewriteCaption", Culture);

		/// <summary>
		/// To change its name and values requires rewriting the asset file. Continue?
		/// </summary>
		public static string TemplateEditor_RewriteMessage => LocaleManager.GetString("TemplateEditor_RewriteMessage", Culture);

		/// <summary>
		/// To be able to set the crosswalk borders, you must have lines started from the same points as the cro
		/// </summary>
		public static string CrosswalkEditor_BordersWarning => LocaleManager.GetString("CrosswalkEditor_BordersWarning", Culture);

		/// <summary>
		/// You must have lines that intersect with this line to be able to change the rule's "From" and "To" po
		/// </summary>
		public static string LineEditor_RulesWarning => LocaleManager.GetString("LineEditor_RulesWarning", Culture);

		/// <summary>
		/// Show hints on panel
		/// </summary>
		public static string Settings_ShowPaneltips => LocaleManager.GetString("Settings_ShowPaneltips", Culture);

		/// <summary>
		/// Edit
		/// </summary>
		public static string HeaderPanel_Edit => LocaleManager.GetString("HeaderPanel_Edit", Culture);

		/// <summary>
		/// Save changes
		/// </summary>
		public static string HeaderPanel_Save => LocaleManager.GetString("HeaderPanel_Save", Culture);

		/// <summary>
		/// Don't save changes
		/// </summary>
		public static string HeaderPanel_NotSave => LocaleManager.GetString("HeaderPanel_NotSave", Culture);

		/// <summary>
		/// Do you want to save preset name change?
		/// </summary>
		public static string PresetEditor_SaveChangesMessage => LocaleManager.GetString("PresetEditor_SaveChangesMessage", Culture);

		/// <summary>
		/// Save changes
		/// </summary>
		public static string TemplateEditor_SaveChanges => LocaleManager.GetString("TemplateEditor_SaveChanges", Culture);

		/// <summary>
		/// Do you want to save template values changes?
		/// </summary>
		public static string TemplateEditor_SaveChangesMessage => LocaleManager.GetString("TemplateEditor_SaveChangesMessage", Culture);

		/// <summary>
		/// The name "{0}" is already used by another preset.
		/// </summary>
		public static string PresetEditor_NameExistMessage => LocaleManager.GetString("PresetEditor_NameExistMessage", Culture);

		/// <summary>
		/// It can only be deleted from the game content manager.
		/// </summary>
		public static string PresetEditor_IsAssetWarningMessage => LocaleManager.GetString("PresetEditor_IsAssetWarningMessage", Culture);

		/// <summary>
		/// This preset can't be modified. It can only be deleted from the game content manager or unsubscribed 
		/// </summary>
		public static string PresetEditor_IsWorkshopWarningMessage => LocaleManager.GetString("PresetEditor_IsWorkshopWarningMessage", Culture);

		/// <summary>
		/// This preset is an asset.
		/// </summary>
		public static string PresetEditor_PresetIsAsset => LocaleManager.GetString("PresetEditor_PresetIsAsset", Culture);

		/// <summary>
		/// It can only be deleted from the game content manager.
		/// </summary>
		public static string TemplateEditor_IsAssetWarningMessage => LocaleManager.GetString("TemplateEditor_IsAssetWarningMessage", Culture);

		/// <summary>
		/// This template can't be modified. It can only be deleted from the game content manager or unsubscribe
		/// </summary>
		public static string TemplateEditor_IsWorkshopWarningMessage => LocaleManager.GetString("TemplateEditor_IsWorkshopWarningMessage", Culture);

		/// <summary>
		/// This template is an asset.
		/// </summary>
		public static string TemplateEditor_TemplateIsAsset => LocaleManager.GetString("TemplateEditor_TemplateIsAsset", Culture);

		/// <summary>
		/// preset
		/// </summary>
		public static string PresetEditor_DeleteCaptionDescription => LocaleManager.GetString("PresetEditor_DeleteCaptionDescription", Culture);

		/// <summary>
		/// preset
		/// </summary>
		public static string PresetEditor_DeleteMessageDescription => LocaleManager.GetString("PresetEditor_DeleteMessageDescription", Culture);

		/// <summary>
		/// [NEW] Added the ability to save whole intersection marking as preset and after apply to any other in
		/// </summary>
		public static string Mod_WhatsNewMessage1_5 => LocaleManager.GetString("Mod_WhatsNewMessage1_5", Culture);

		/// <summary>
		/// This line type doesn't support rules
		/// </summary>
		public static string LineEditor_NotSupportRules => LocaleManager.GetString("LineEditor_NotSupportRules", Culture);

		/// <summary>
		/// Don't cut line by crosswalk if it is their border
		/// </summary>
		public static string Settings_DontCutBorderByCrosswalk => LocaleManager.GetString("Settings_DontCutBorderByCrosswalk", Culture);

		/// <summary>
		/// Unknown author
		/// </summary>
		public static string Template_UnknownAuthor => LocaleManager.GetString("Template_UnknownAuthor", Culture);

		/// <summary>
		/// [UPDATED] Improve messages about load data errors.
		/// </summary>
		public static string Mod_WhatsNewMessage1_5_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_5_1", Culture);

		/// <summary>
		/// Load was completed with errors.
		/// </summary>
		public static string Mod_LoadFailedAll => LocaleManager.GetString("Mod_LoadFailedAll", Culture);

		/// <summary>
		/// Edit segment #{0} markings
		/// </summary>
		public static string Panel_SegmentCaption => LocaleManager.GetString("Panel_SegmentCaption", Culture);

		/// <summary>
		/// Grass
		/// </summary>
		public static string FillerStyle_Grass => LocaleManager.GetString("FillerStyle_Grass", Culture);

		/// <summary>
		/// Pavement
		/// </summary>
		public static string FillerStyle_Pavement => LocaleManager.GetString("FillerStyle_Pavement", Culture);

		/// <summary>
		/// Elevation
		/// </summary>
		public static string FillerStyle_Elevation => LocaleManager.GetString("FillerStyle_Elevation", Culture);

		/// <summary>
		/// Elevation
		/// </summary>
		public static string LineStyle_Elevation => LocaleManager.GetString("LineStyle_Elevation", Culture);

		/// <summary>
		/// Pavement
		/// </summary>
		public static string LineStyle_Pavement => LocaleManager.GetString("LineStyle_Pavement", Culture);

		/// <summary>
		/// LOD distance
		/// </summary>
		public static string Settings_LODDistance => LocaleManager.GetString("Settings_LODDistance", Culture);

		/// <summary>
		/// Segment #{0}
		/// </summary>
		public static string Tool_InfoHoverSegment => LocaleManager.GetString("Tool_InfoHoverSegment", Culture);

		/// <summary>
		/// Select a guide's first point or line as guide
		/// </summary>
		public static string FillerEditor_InfoSelectRailFirst => LocaleManager.GetString("FillerEditor_InfoSelectRailFirst", Culture);

		/// <summary>
		/// Select a guide's second point
		/// </summary>
		public static string FillerEditor_InfoSelectRailSecond => LocaleManager.GetString("FillerEditor_InfoSelectRailSecond", Culture);

		/// <summary>
		/// [UPDATED] Update Harmony dependence: resolving conflicts with others mods that use it
		/// </summary>
		public static string Mod_WhatsNewMessage1_5_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_5_2", Culture);

		/// <summary>
		/// [FIXED] Fixed a situation where markings could disappear from saving
		/// </summary>
		public static string Mod_WhatsNewMessage1_5_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_5_3", Culture);

		/// <summary>
		/// [NEW] Added the ability to make markings on segments.
		/// </summary>
		public static string Mod_WhatsNewMessage1_6 => LocaleManager.GetString("Mod_WhatsNewMessage1_6", Culture);

		/// <summary>
		/// Color count
		/// </summary>
		public static string StyleOption_ColorCount => LocaleManager.GetString("StyleOption_ColorCount", Culture);

		/// <summary>
		/// One
		/// </summary>
		public static string StyleOption_ColorCountOne => LocaleManager.GetString("StyleOption_ColorCountOne", Culture);

		/// <summary>
		/// Two
		/// </summary>
		public static string StyleOption_ColorCountTwo => LocaleManager.GetString("StyleOption_ColorCountTwo", Culture);

		/// <summary>
		/// From clipboard
		/// </summary>
		public static string Style_FromClipboard => LocaleManager.GetString("Style_FromClipboard", Culture);

		/// <summary>
		/// Line alignment
		/// </summary>
		public static string LineEditor_LineAlignment => LocaleManager.GetString("LineEditor_LineAlignment", Culture);

		/// <summary>
		/// Split into two
		/// </summary>
		public static string PointEditor_SplitIntoTwo => LocaleManager.GetString("PointEditor_SplitIntoTwo", Culture);

		/// <summary>
		/// Split offset
		/// </summary>
		public static string PointEditor_SplitOffset => LocaleManager.GetString("PointEditor_SplitOffset", Culture);

		/// <summary>
		/// Group points
		/// </summary>
		public static string Settings_GroupPoints => LocaleManager.GetString("Settings_GroupPoints", Culture);

		/// <summary>
		/// Apply between intersections
		/// </summary>
		public static string Settings_ShortcutApplyBetweenIntersections => LocaleManager.GetString("Settings_ShortcutApplyBetweenIntersections", Culture);

		/// <summary>
		/// Apply between intersections
		/// </summary>
		public static string Panel_ApplyBetweenIntersections => LocaleManager.GetString("Panel_ApplyBetweenIntersections", Culture);

		/// <summary>
		/// Apply to whole street
		/// </summary>
		public static string Panel_ApplyWholeStreet => LocaleManager.GetString("Panel_ApplyWholeStreet", Culture);

		/// <summary>
		/// Apply to whole street
		/// </summary>
		public static string Settings_ShortcutApplyWholeStreet => LocaleManager.GetString("Settings_ShortcutApplyWholeStreet", Culture);

		/// <summary>
		/// It will replace the existing markings.
		/// </summary>
		public static string MessageBox_ItWillReplace => LocaleManager.GetString("MessageBox_ItWillReplace", Culture);

		/// <summary>
		/// Apply marking between intersections
		/// </summary>
		public static string Tool_ApplyBetweenIntersectionsCaption => LocaleManager.GetString("Tool_ApplyBetweenIntersectionsCaption", Culture);

		/// <summary>
		/// Do you really want to apply marking between intersections?
		/// </summary>
		public static string Tool_ApplyBetweenIntersectionsMessage => LocaleManager.GetString("Tool_ApplyBetweenIntersectionsMessage", Culture);

		/// <summary>
		/// Apply marking to whole street
		/// </summary>
		public static string Tool_ApplyWholeStreetCaption => LocaleManager.GetString("Tool_ApplyWholeStreetCaption", Culture);

		/// <summary>
		/// Do you really want to apply marking to whole "{0}"?
		/// </summary>
		public static string Tool_ApplyWholeStreetMessage => LocaleManager.GetString("Tool_ApplyWholeStreetMessage", Culture);

		/// <summary>
		/// Drag behind point for perpendicular line
		/// </summary>
		public static string Tool_InfoSelectLineEndPointNormal => LocaleManager.GetString("Tool_InfoSelectLineEndPointNormal", Culture);

		/// <summary>
		/// Select endpoint for regular or stop line
		/// </summary>
		public static string Tool_InfoSelectLineEndPointStop => LocaleManager.GetString("Tool_InfoSelectLineEndPointStop", Culture);

		/// <summary>
		/// [NEW] New node/segment selection mode. The selection area exactly follows the actual shapes of the n
		/// </summary>
		public static string Mod_WhatsNewMessage1_7 => LocaleManager.GetString("Mod_WhatsNewMessage1_7", Culture);

		/// <summary>
		/// Line end alignment
		/// </summary>
		public static string LineEditor_LineEndAlignment => LocaleManager.GetString("LineEditor_LineEndAlignment", Culture);

		/// <summary>
		/// Line start alignment
		/// </summary>
		public static string LineEditor_LineStartAlignment => LocaleManager.GetString("LineEditor_LineStartAlignment", Culture);

		/// <summary>
		/// [NEW] Added the ability to set alignment for stop lines.
		/// </summary>
		public static string Mod_WhatsNewMessage1_7_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_7_1", Culture);

		/// <summary>
		/// [FIXED] Fix 3D fillers height outside available tiles.
		/// </summary>
		public static string Mod_WhatsNewMessage1_7_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_7_2", Culture);

		/// <summary>
		/// [NEW] Added missing dependencies checker.
		/// </summary>
		public static string Mod_WhatsNewMessage1_7_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_7_3", Culture);

		/// <summary>
		/// [FIXED] Fixed save and load presets' screenshot.
		/// </summary>
		public static string Mod_WhatsNewMessage1_7_4 => LocaleManager.GetString("Mod_WhatsNewMessage1_7_4", Culture);

		/// <summary>
		/// Hide street names while using tool
		/// </summary>
		public static string Settings_HideStreetName => LocaleManager.GetString("Settings_HideStreetName", Culture);

		/// <summary>
		/// Pavement
		/// </summary>
		public static string LineStyle_StopPavement => LocaleManager.GetString("LineStyle_StopPavement", Culture);

		/// <summary>
		/// Click with {0} to select the crosswalk
		/// </summary>
		public static string Tool_InfoSelectCrosswalk => LocaleManager.GetString("Tool_InfoSelectCrosswalk", Culture);

		/// <summary>
		/// Click with {0} to select the line
		/// </summary>
		public static string Tool_InfoSelectLine => LocaleManager.GetString("Tool_InfoSelectLine", Culture);

		/// <summary>
		/// Gravel
		/// </summary>
		public static string FillerStyle_Gravel => LocaleManager.GetString("FillerStyle_Gravel", Culture);

		/// <summary>
		/// Cliff
		/// </summary>
		public static string FillerStyle_Cliff => LocaleManager.GetString("FillerStyle_Cliff", Culture);

		/// <summary>
		/// Ruined
		/// </summary>
		public static string FillerStyle_Ruined => LocaleManager.GetString("FillerStyle_Ruined", Culture);

		/// <summary>
		/// Corner radius
		/// </summary>
		public static string FillerStyle_CornerRadius => LocaleManager.GetString("FillerStyle_CornerRadius", Culture);

		/// <summary>
		/// Curb size
		/// </summary>
		public static string FillerStyle_CurbSize => LocaleManager.GetString("FillerStyle_CurbSize", Culture);

		/// <summary>
		/// [NEW] Added selection step over like in MoveIt. Press Ctrl+Space to step over (you can rebind in mod
		/// </summary>
		public static string Mod_WhatsNewMessage1_8 => LocaleManager.GetString("Mod_WhatsNewMessage1_8", Culture);

		/// <summary>
		/// [UPDATED] Buttons on paste mode moved ouside of circle.
		/// </summary>
		public static string Mod_WhatsNewMessage1_8_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_8_1", Culture);

		/// <summary>
		/// Discard
		/// </summary>
		public static string HeaderPanel_Discard => LocaleManager.GetString("HeaderPanel_Discard", Culture);

		/// <summary>
		/// [NEW] Added discard button after create template/preset.
		/// </summary>
		public static string Mod_WhatsNewMessage1_8_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_8_2", Culture);

		/// <summary>
		/// Sortings
		/// </summary>
		public static string Settings_Sortings => LocaleManager.GetString("Settings_Sortings", Culture);

		/// <summary>
		/// Sort presets
		/// </summary>
		public static string Settings_SortPresetType => LocaleManager.GetString("Settings_SortPresetType", Culture);

		/// <summary>
		/// By names
		/// </summary>
		public static string Settings_SortPresetByNames => LocaleManager.GetString("Settings_SortPresetByNames", Culture);

		/// <summary>
		/// By road count, then by names
		/// </summary>
		public static string Settings_SortPresetByRoadCount => LocaleManager.GetString("Settings_SortPresetByRoadCount", Culture);

		/// <summary>
		/// By author, then by type, then by name
		/// </summary>
		public static string Settings_SortTemplateByAuthor => LocaleManager.GetString("Settings_SortTemplateByAuthor", Culture);

		/// <summary>
		/// By name, then by type
		/// </summary>
		public static string Settings_SortTemplateByNames => LocaleManager.GetString("Settings_SortTemplateByNames", Culture);

		/// <summary>
		/// By type, then by name
		/// </summary>
		public static string Settings_SortTemplateByType => LocaleManager.GetString("Settings_SortTemplateByType", Culture);

		/// <summary>
		/// Sort templates
		/// </summary>
		public static string Settings_SortTemplateType => LocaleManager.GetString("Settings_SortTemplateType", Culture);

		/// <summary>
		/// By author, then by type, then by name
		/// </summary>
		public static string Settings_SortApplyByAuthor => LocaleManager.GetString("Settings_SortApplyByAuthor", Culture);

		/// <summary>
		/// By name, then by type
		/// </summary>
		public static string Settings_SortApplyByNames => LocaleManager.GetString("Settings_SortApplyByNames", Culture);

		/// <summary>
		/// By type, then by name
		/// </summary>
		public static string Settings_SortApplyByType => LocaleManager.GetString("Settings_SortApplyByType", Culture);

		/// <summary>
		/// Show default templates at top of apply list
		/// </summary>
		public static string Settings_SortApplyDefaultFirst => LocaleManager.GetString("Settings_SortApplyDefaultFirst", Culture);

		/// <summary>
		/// Sort templates in apply list
		/// </summary>
		public static string Settings_SortApplyType => LocaleManager.GetString("Settings_SortApplyType", Culture);

		/// <summary>
		/// Illumination of editing marking at night
		/// </summary>
		public static string Settings_IlluminationAtNight => LocaleManager.GetString("Settings_IlluminationAtNight", Culture);

		/// <summary>
		/// Illumination intensity
		/// </summary>
		public static string Settings_IlluminationIntensity => LocaleManager.GetString("Settings_IlluminationIntensity", Culture);

		/// <summary>
		/// Gameplay
		/// </summary>
		public static string Settings_Gameplay => LocaleManager.GetString("Settings_Gameplay", Culture);

		/// <summary>
		/// Group presets
		/// </summary>
		public static string Settings_GroupPresets => LocaleManager.GetString("Settings_GroupPresets", Culture);

		/// <summary>
		/// Close fit
		/// </summary>
		public static string PresetEditor_PresetFit_Close => LocaleManager.GetString("PresetEditor_PresetFit_Close", Culture);

		/// <summary>
		/// Perfect fit
		/// </summary>
		public static string PresetEditor_PresetFit_Perfect => LocaleManager.GetString("PresetEditor_PresetFit_Perfect", Culture);

		/// <summary>
		/// Poor fit
		/// </summary>
		public static string PresetEditor_PresetFit_Poor => LocaleManager.GetString("PresetEditor_PresetFit_Poor", Culture);

		/// <summary>
		/// Possible fit
		/// </summary>
		public static string PresetEditor_PresetFit_Possible => LocaleManager.GetString("PresetEditor_PresetFit_Possible", Culture);

		/// <summary>
		/// Markings under level crossings
		/// </summary>
		public static string Settings_LevelCrossingUnderMarking => LocaleManager.GetString("Settings_LevelCrossingUnderMarking", Culture);

		/// <summary>
		/// Revert offsets
		/// </summary>
		public static string PointEditor_RevertOffsets => LocaleManager.GetString("PointEditor_RevertOffsets", Culture);

		/// <summary>
		/// Save offsets
		/// </summary>
		public static string PointEditor_SaveOffsets => LocaleManager.GetString("PointEditor_SaveOffsets", Culture);

		/// <summary>
		/// Road name
		/// </summary>
		public static string PointEditor_RoadName => LocaleManager.GetString("PointEditor_RoadName", Culture);

		/// <summary>
		/// [NEW] Added the ability to save points offset values for each roads.
		/// </summary>
		public static string Mod_WhatsNewMessage1_9 => LocaleManager.GetString("Mod_WhatsNewMessage1_9", Culture);

		/// <summary>
		/// Prop
		/// </summary>
		public static string LineStyle_Prop => LocaleManager.GetString("LineStyle_Prop", Culture);

		/// <summary>
		/// Tree
		/// </summary>
		public static string LineStyle_Tree => LocaleManager.GetString("LineStyle_Tree", Culture);

		/// <summary>
		/// Angle
		/// </summary>
		public static string StyleOption_ObjectAngle => LocaleManager.GetString("StyleOption_ObjectAngle", Culture);

		/// <summary>
		/// Scale
		/// </summary>
		public static string StyleOption_ObjectScale => LocaleManager.GetString("StyleOption_ObjectScale", Culture);

		/// <summary>
		/// Shift
		/// </summary>
		public static string StyleOption_ObjectShift => LocaleManager.GetString("StyleOption_ObjectShift", Culture);

		/// <summary>
		/// Step
		/// </summary>
		public static string StyleOption_ObjectStep => LocaleManager.GetString("StyleOption_ObjectStep", Culture);

		/// <summary>
		/// {0}°
		/// </summary>
		public static string NumberFormat_Degree => LocaleManager.GetString("NumberFormat_Degree", Culture);

		/// <summary>
		/// {0}m
		/// </summary>
		public static string NumberFormat_Meter => LocaleManager.GetString("NumberFormat_Meter", Culture);

		/// <summary>
		/// {0}%
		/// </summary>
		public static string NumberFormat_Percent => LocaleManager.GetString("NumberFormat_Percent", Culture);

		/// <summary>
		/// Variant #1
		/// </summary>
		public static string StyleOption_Color1 => LocaleManager.GetString("StyleOption_Color1", Culture);

		/// <summary>
		/// Variant #2
		/// </summary>
		public static string StyleOption_Color2 => LocaleManager.GetString("StyleOption_Color2", Culture);

		/// <summary>
		/// Variant #3
		/// </summary>
		public static string StyleOption_Color3 => LocaleManager.GetString("StyleOption_Color3", Culture);

		/// <summary>
		/// Variant #4
		/// </summary>
		public static string StyleOption_Color4 => LocaleManager.GetString("StyleOption_Color4", Culture);

		/// <summary>
		/// Custom
		/// </summary>
		public static string StyleOption_ColorCustom => LocaleManager.GetString("StyleOption_ColorCustom", Culture);

		/// <summary>
		/// Random
		/// </summary>
		public static string StyleOption_ColorRandom => LocaleManager.GetString("StyleOption_ColorRandom", Culture);

		/// <summary>
		/// Color variant
		/// </summary>
		public static string StyleOption_ColorOption => LocaleManager.GetString("StyleOption_ColorOption", Culture);

		/// <summary>
		/// Clip sidewalk
		/// </summary>
		public static string LineEditor_ClipSidewalk => LocaleManager.GetString("LineEditor_ClipSidewalk", Culture);

		/// <summary>
		/// Angle
		/// </summary>
		public static string StyleOption_SharkToothAngle => LocaleManager.GetString("StyleOption_SharkToothAngle", Culture);

		/// <summary>
		/// Decorative network
		/// </summary>
		public static string LineStyle_Network => LocaleManager.GetString("LineStyle_Network", Culture);

		/// <summary>
		/// Width scale
		/// </summary>
		public static string StyleOption_NetWidthScale => LocaleManager.GetString("StyleOption_NetWidthScale", Culture);

		/// <summary>
		/// Repeat distance
		/// </summary>
		public static string StyleOption_NetRepeatDistance => LocaleManager.GetString("StyleOption_NetRepeatDistance", Culture);

		/// <summary>
		/// Network asset
		/// </summary>
		public static string StyleOption_AssetNetwork => LocaleManager.GetString("StyleOption_AssetNetwork", Culture);

		/// <summary>
		/// Not set
		/// </summary>
		public static string StyleOption_AssetNotSet => LocaleManager.GetString("StyleOption_AssetNotSet", Culture);

		/// <summary>
		/// Prop asset
		/// </summary>
		public static string StyleOption_AssetProp => LocaleManager.GetString("StyleOption_AssetProp", Culture);

		/// <summary>
		/// Tree asset
		/// </summary>
		public static string StyleOption_AssetTree => LocaleManager.GetString("StyleOption_AssetTree", Culture);

		/// <summary>
		/// Nothing found
		/// </summary>
		public static string AssetPopup_NothingFound => LocaleManager.GetString("AssetPopup_NothingFound", Culture);

		/// <summary>
		/// Auto apply marking pasting if the source matches the target
		/// </summary>
		public static string Settings_AutoApplyPasting => LocaleManager.GetString("Settings_AutoApplyPasting", Culture);

		/// <summary>
		/// Probability
		/// </summary>
		public static string StyleOption_ObjectProbability => LocaleManager.GetString("StyleOption_ObjectProbability", Culture);

		/// <summary>
		/// Range
		/// </summary>
		public static string StyleOption_ObjectRange => LocaleManager.GetString("StyleOption_ObjectRange", Culture);

		/// <summary>
		/// Single
		/// </summary>
		public static string StyleOption_ObjectStatic => LocaleManager.GetString("StyleOption_ObjectStatic", Culture);

		/// <summary>
		/// [NEW] Added Prop and Tree line styles. They allow place any prop or tree on lines. Those objects are
		/// </summary>
		public static string Mod_WhatsNewMessage1_10 => LocaleManager.GetString("Mod_WhatsNewMessage1_10", Culture);

		/// <summary>
		/// Direct and Invert order
		/// </summary>
		public static string Settings_AutoApplyPastingDirectAndInvert => LocaleManager.GetString("Settings_AutoApplyPastingDirectAndInvert", Culture);

		/// <summary>
		/// Only direct order
		/// </summary>
		public static string Settings_AutoApplyPastingDirectOnly => LocaleManager.GetString("Settings_AutoApplyPastingDirectOnly", Culture);

		/// <summary>
		/// [UPDATED] Auto applying marking is enable only when there is only one posible option to paste markin
		/// </summary>
		public static string Mod_WhatsNewMessage1_10_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_10_1", Culture);

		/// <summary>
		/// [UPDATED] Added Plazas & Promenades DLC support.
		/// </summary>
		public static string Mod_WhatsNewMessage1_10_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_10_2", Culture);

		/// <summary>
		/// Slope
		/// </summary>
		public static string StyleOption_Slope => LocaleManager.GetString("StyleOption_Slope", Culture);

		/// <summary>
		/// Tilt
		/// </summary>
		public static string StyleOption_Tilt => LocaleManager.GetString("StyleOption_Tilt", Culture);

		/// <summary>
		/// Enter underground mode
		/// </summary>
		public static string Settings_ShortcutEnterUnderground => LocaleManager.GetString("Settings_ShortcutEnterUnderground", Culture);

		/// <summary>
		/// Exit underground mode
		/// </summary>
		public static string Settings_ShortcutExitUnderground => LocaleManager.GetString("Settings_ShortcutExitUnderground", Culture);

		/// <summary>
		/// Toogle underground
		/// </summary>
		public static string Settings_ToggleUnderground => LocaleManager.GetString("Settings_ToggleUnderground", Culture);

		/// <summary>
		/// Press {0} to enter and {1} to exit
		/// </summary>
		public static string Settings_ToggleUndergroundButtons => LocaleManager.GetString("Settings_ToggleUndergroundButtons", Culture);

		/// <summary>
		/// Hold {0}
		/// </summary>
		public static string Settings_ToggleUndergroundHold => LocaleManager.GetString("Settings_ToggleUndergroundHold", Culture);

		/// <summary>
		/// Press {0} to enter
		/// </summary>
		public static string Tool_EnterUnderground => LocaleManager.GetString("Tool_EnterUnderground", Culture);

		/// <summary>
		/// Press {0} to exit
		/// </summary>
		public static string Tool_ExitUnderground => LocaleManager.GetString("Tool_ExitUnderground", Culture);

		/// <summary>
		/// Hold {0} to
		/// </summary>
		public static string Tool_InfoUnderground => LocaleManager.GetString("Tool_InfoUnderground", Culture);

		/// <summary>
		/// 2D Marking LOD distance
		/// </summary>
		public static string Settings_LODDistanceMarking => LocaleManager.GetString("Settings_LODDistanceMarking", Culture);

		/// <summary>
		/// Network LOD distance
		/// </summary>
		public static string Settings_LODDistanceNetwork => LocaleManager.GetString("Settings_LODDistanceNetwork", Culture);

		/// <summary>
		/// Prop LOD distance
		/// </summary>
		public static string Settings_LODDistanceProp => LocaleManager.GetString("Settings_LODDistanceProp", Culture);

		/// <summary>
		/// Tree LOD distance
		/// </summary>
		public static string Settings_LODDistanceTree => LocaleManager.GetString("Settings_LODDistanceTree", Culture);

		/// <summary>
		/// Auto
		/// </summary>
		public static string StyleOption_ObjectAuto => LocaleManager.GetString("StyleOption_ObjectAuto", Culture);

		/// <summary>
		/// 3D Marking LOD distance
		/// </summary>
		public static string Settings_LODDistanceMesh => LocaleManager.GetString("Settings_LODDistanceMesh", Culture);

		/// <summary>
		/// Render
		/// </summary>
		public static string Settings_Render => LocaleManager.GetString("Settings_Render", Culture);

		/// <summary>
		/// Less options
		/// </summary>
		public static string Editor_LessOptions => LocaleManager.GetString("Editor_LessOptions", Culture);

		/// <summary>
		/// More options
		/// </summary>
		public static string Editor_MoreOptions => LocaleManager.GetString("Editor_MoreOptions", Culture);

		/// <summary>
		/// Collapse line rule panels
		/// </summary>
		public static string Settings_CollapseRules => LocaleManager.GetString("Settings_CollapseRules", Culture);

		/// <summary>
		/// Main color
		/// </summary>
		public static string StyleOption_MainColor => LocaleManager.GetString("StyleOption_MainColor", Culture);

		/// <summary>
		/// Second color
		/// </summary>
		public static string StyleOption_SecondColor => LocaleManager.GetString("StyleOption_SecondColor", Culture);

		/// <summary>
		/// Double dashed asym
		/// </summary>
		public static string LineStyle_DoubleDashedAsym => LocaleManager.GetString("LineStyle_DoubleDashedAsym", Culture);

		/// <summary>
		/// Each {0}
		/// </summary>
		public static string NumberFormat_Period => LocaleManager.GetString("NumberFormat_Period", Culture);

		/// <summary>
		/// Gap
		/// </summary>
		public static string StyleOption_CrosswalkGap => LocaleManager.GetString("StyleOption_CrosswalkGap", Culture);

		/// <summary>
		/// Regular
		/// </summary>
		public static string FillerStyle_CornerRadiusAbrv => LocaleManager.GetString("FillerStyle_CornerRadiusAbrv", Culture);

		/// <summary>
		/// Median
		/// </summary>
		public static string FillerStyle_CornerRadiusMedianAbrv => LocaleManager.GetString("FillerStyle_CornerRadiusMedianAbrv", Culture);

		/// <summary>
		/// Regular
		/// </summary>
		public static string FillerStyle_CurbSizeAbrv => LocaleManager.GetString("FillerStyle_CurbSizeAbrv", Culture);

		/// <summary>
		/// Median
		/// </summary>
		public static string FillerStyle_CurbSizeMedianAbrv => LocaleManager.GetString("FillerStyle_CurbSizeMedianAbrv", Culture);

		/// <summary>
		/// Dash
		/// </summary>
		public static string StyleOption_Dash => LocaleManager.GetString("StyleOption_Dash", Culture);

		/// <summary>
		/// Length
		/// </summary>
		public static string StyleOption_Length => LocaleManager.GetString("StyleOption_Length", Culture);

		/// <summary>
		/// Line
		/// </summary>
		public static string StyleOption_LineOffsetAbrv => LocaleManager.GetString("StyleOption_LineOffsetAbrv", Culture);

		/// <summary>
		/// Median
		/// </summary>
		public static string StyleOption_MedianOffsetAbrv => LocaleManager.GetString("StyleOption_MedianOffsetAbrv", Culture);

		/// <summary>
		/// After
		/// </summary>
		public static string StyleOption_OffsetAfterAbrv => LocaleManager.GetString("StyleOption_OffsetAfterAbrv", Culture);

		/// <summary>
		/// Before
		/// </summary>
		public static string StyleOption_OffsetBeforeAbrv => LocaleManager.GetString("StyleOption_OffsetBeforeAbrv", Culture);

		/// <summary>
		/// Space
		/// </summary>
		public static string StyleOption_Space => LocaleManager.GetString("StyleOption_Space", Culture);

		/// <summary>
		/// Guides
		/// </summary>
		public static string StyleOption_Rails => LocaleManager.GetString("StyleOption_Rails", Culture);

		/// <summary>
		/// Base
		/// </summary>
		public static string StyleOption_SharkToothBaseAbrv => LocaleManager.GetString("StyleOption_SharkToothBaseAbrv", Culture);

		/// <summary>
		/// Height
		/// </summary>
		public static string StyleOption_SharkToothHeightAbrv => LocaleManager.GetString("StyleOption_SharkToothHeightAbrv", Culture);

		/// <summary>
		/// [NEW] Save marking data into network asset and auto apply it when network is placed.
		/// </summary>
		public static string Mod_WhatsNewMessage1_11 => LocaleManager.GetString("Mod_WhatsNewMessage1_11", Culture);

		/// <summary>
		/// Triangle
		/// </summary>
		public static string StyleOption_Triangle => LocaleManager.GetString("StyleOption_Triangle", Culture);

		/// <summary>
		/// Text
		/// </summary>
		public static string LineStyle_Text => LocaleManager.GetString("LineStyle_Text", Culture);

		/// <summary>
		/// Default game font
		/// </summary>
		public static string StyleOption_DefaultFont => LocaleManager.GetString("StyleOption_DefaultFont", Culture);

		/// <summary>
		/// Font
		/// </summary>
		public static string StyleOption_Font => LocaleManager.GetString("StyleOption_Font", Culture);

		/// <summary>
		/// Bold
		/// </summary>
		public static string StyleOption_FontStyleBold => LocaleManager.GetString("StyleOption_FontStyleBold", Culture);

		/// <summary>
		/// Bold Italic
		/// </summary>
		public static string StyleOption_FontStyleBoldItalic => LocaleManager.GetString("StyleOption_FontStyleBoldItalic", Culture);

		/// <summary>
		/// Italic
		/// </summary>
		public static string StyleOption_FontStyleItalic => LocaleManager.GetString("StyleOption_FontStyleItalic", Culture);

		/// <summary>
		/// Regular
		/// </summary>
		public static string StyleOption_FontStyleRegular => LocaleManager.GetString("StyleOption_FontStyleRegular", Culture);

		/// <summary>
		/// Spacing
		/// </summary>
		public static string StyleOption_Spacing => LocaleManager.GetString("StyleOption_Spacing", Culture);

		/// <summary>
		/// Char
		/// </summary>
		public static string StyleOption_SpacingChar => LocaleManager.GetString("StyleOption_SpacingChar", Culture);

		/// <summary>
		/// Line
		/// </summary>
		public static string StyleOption_SpacingLine => LocaleManager.GetString("StyleOption_SpacingLine", Culture);

		/// <summary>
		/// Text
		/// </summary>
		public static string StyleOption_Text => LocaleManager.GetString("StyleOption_Text", Culture);

		/// <summary>
		/// Direction
		/// </summary>
		public static string StyleOption_TextDirection => LocaleManager.GetString("StyleOption_TextDirection", Culture);

		/// <summary>
		/// Bottom to Top
		/// </summary>
		public static string StyleOption_TextDirectionBtoT => LocaleManager.GetString("StyleOption_TextDirectionBtoT", Culture);

		/// <summary>
		/// Left to Right
		/// </summary>
		public static string StyleOption_TextDirectionLtoR => LocaleManager.GetString("StyleOption_TextDirectionLtoR", Culture);

		/// <summary>
		/// Top to Bottom
		/// </summary>
		public static string StyleOption_TextDirectionTtoB => LocaleManager.GetString("StyleOption_TextDirectionTtoB", Culture);

		/// <summary>
		/// Lane
		/// </summary>
		public static string LineStyle_LaneGroup => LocaleManager.GetString("LineStyle_LaneGroup", Culture);

		/// <summary>
		/// Click to create a lane
		/// </summary>
		public static string Tool_InfoCreateLaneLine => LocaleManager.GetString("Tool_InfoCreateLaneLine", Culture);

		/// <summary>
		/// Click to delete the lane
		/// </summary>
		public static string Tool_InfoDeleteLaneLine => LocaleManager.GetString("Tool_InfoDeleteLaneLine", Culture);

		/// <summary>
		/// Click with {0} to select the lane
		/// </summary>
		public static string Tool_InfoSelectLane => LocaleManager.GetString("Tool_InfoSelectLane", Culture);

		/// <summary>
		/// Create lane edge lines automatically
		/// </summary>
		public static string Settings_CreateLaneEdgeLines => LocaleManager.GetString("Settings_CreateLaneEdgeLines", Culture);

		/// <summary>
		/// Select endpoint to create a lane
		/// </summary>
		public static string Tool_InfoSelectLaneEndPoint => LocaleManager.GetString("Tool_InfoSelectLaneEndPoint", Culture);

		/// <summary>
		/// Distribution
		/// </summary>
		public static string StyleOption_Distribution => LocaleManager.GetString("StyleOption_Distribution", Culture);

		/// <summary>
		/// Dynamic space and fixed ends
		/// </summary>
		public static string StyleOption_DistributionDynamicFixed => LocaleManager.GetString("StyleOption_DistributionDynamicFixed", Culture);

		/// <summary>
		/// Dynamic space and free ends
		/// </summary>
		public static string StyleOption_DistributionDynamicFree => LocaleManager.GetString("StyleOption_DistributionDynamicFree", Culture);

		/// <summary>
		/// Fixed space and fixed ends
		/// </summary>
		public static string StyleOption_DistributionFixedFixed => LocaleManager.GetString("StyleOption_DistributionFixedFixed", Culture);

		/// <summary>
		/// Fixed space and free ends
		/// </summary>
		public static string StyleOption_DistributionFixedFree => LocaleManager.GetString("StyleOption_DistributionFixedFree", Culture);

		/// <summary>
		/// Zigzag
		/// </summary>
		public static string LineStyle_ZigZag => LocaleManager.GetString("LineStyle_ZigZag", Culture);

		/// <summary>
		/// Left
		/// </summary>
		public static string StyleOption_SideLeft => LocaleManager.GetString("StyleOption_SideLeft", Culture);

		/// <summary>
		/// Right
		/// </summary>
		public static string StyleOption_SideRight => LocaleManager.GetString("StyleOption_SideRight", Culture);

		/// <summary>
		/// Offset
		/// </summary>
		public static string StyleOption_ZigzagOffset => LocaleManager.GetString("StyleOption_ZigzagOffset", Culture);

		/// <summary>
		/// Side
		/// </summary>
		public static string StyleOption_ZigzagSide => LocaleManager.GetString("StyleOption_ZigzagSide", Culture);

		/// <summary>
		/// Start from
		/// </summary>
		public static string StyleOption_ZigzagStartFrom => LocaleManager.GetString("StyleOption_ZigzagStartFrom", Culture);

		/// <summary>
		/// Line
		/// </summary>
		public static string StyleOption_ZigzagStartFromLine => LocaleManager.GetString("StyleOption_ZigzagStartFromLine", Culture);

		/// <summary>
		/// Outside
		/// </summary>
		public static string StyleOption_ZigzagStartFromOutside => LocaleManager.GetString("StyleOption_ZigzagStartFromOutside", Culture);

		/// <summary>
		/// Step
		/// </summary>
		public static string StyleOption_ZigzagStep => LocaleManager.GetString("StyleOption_ZigzagStep", Culture);

		/// <summary>
		/// Alignment
		/// </summary>
		public static string StyleOption_TextAlignment => LocaleManager.GetString("StyleOption_TextAlignment", Culture);

		/// <summary>
		/// End
		/// </summary>
		public static string StyleOption_TextAlignmentEnd => LocaleManager.GetString("StyleOption_TextAlignmentEnd", Culture);

		/// <summary>
		/// Middle
		/// </summary>
		public static string StyleOption_TextAlignmentMiddle => LocaleManager.GetString("StyleOption_TextAlignmentMiddle", Culture);

		/// <summary>
		/// Start
		/// </summary>
		public static string StyleOption_TextAlignmentStart => LocaleManager.GetString("StyleOption_TextAlignmentStart", Culture);

		/// <summary>
		/// Link this preset to network asset
		/// </summary>
		public static string PresetEditor_LinkPreset => LocaleManager.GetString("PresetEditor_LinkPreset", Culture);

		/// <summary>
		/// Unlink this preset from network asset
		/// </summary>
		public static string PresetEditor_UnlinkPreset => LocaleManager.GetString("PresetEditor_UnlinkPreset", Culture);

		/// <summary>
		/// Linked
		/// </summary>
		public static string PresetEditor_PresetFit_Linked => LocaleManager.GetString("PresetEditor_PresetFit_Linked", Culture);

		/// <summary>
		/// Drag a point to change offset
		/// </summary>
		public static string Tool_InfoDragPointMode => LocaleManager.GetString("Tool_InfoDragPointMode", Culture);

		/// <summary>
		/// Hold {0} to move a point
		/// </summary>
		public static string Setting_HoldToMovePoint => LocaleManager.GetString("Setting_HoldToMovePoint", Culture);

		/// <summary>
		/// Auto apply markings from network assets
		/// </summary>
		public static string Settings_ApplyMarkingsFromAssets => LocaleManager.GetString("Settings_ApplyMarkingsFromAssets", Culture);

		/// <summary>
		/// Exit linking preset mode
		/// </summary>
		public static string Tool_EndLinkPresetCaption => LocaleManager.GetString("Tool_EndLinkPresetCaption", Culture);

		/// <summary>
		/// Do you want to exit linking preset mode and link this preset to network asset?
		/// </summary>
		public static string Tool_EndLinkPresetMessage => LocaleManager.GetString("Tool_EndLinkPresetMessage", Culture);

		/// <summary>
		/// Link
		/// </summary>
		public static string Tool_Link => LocaleManager.GetString("Tool_Link", Culture);

		/// <summary>
		/// Don't link
		/// </summary>
		public static string Tool_NotLink => LocaleManager.GetString("Tool_NotLink", Culture);

		/// <summary>
		/// [NEW] Added a new line style: Text. It allows to write custom text on road and set any font installe
		/// </summary>
		public static string Mod_WhatsNewMessage1_12 => LocaleManager.GetString("Mod_WhatsNewMessage1_12", Culture);

		/// <summary>
		/// Apply preset to all segments of this asset
		/// </summary>
		public static string PresetEditor_ApplyAllPreset => LocaleManager.GetString("PresetEditor_ApplyAllPreset", Culture);

		/// <summary>
		/// Reset to default
		/// </summary>
		public static string HeaderPanel_StyleReset => LocaleManager.GetString("HeaderPanel_StyleReset", Culture);

		/// <summary>
		/// Do you want to apply a preset to all segments of asset "{0}"?
		/// </summary>
		public static string Tool_EndApplyAllPresetMessage => LocaleManager.GetString("Tool_EndApplyAllPresetMessage", Culture);

		/// <summary>
		/// Exit paste marking mode
		/// </summary>
		public static string Tool_EndPasteMarkingCaption => LocaleManager.GetString("Tool_EndPasteMarkingCaption", Culture);

		/// <summary>
		/// Do you want to apply marking?
		/// </summary>
		public static string Tool_EndPasteMArkingMessage => LocaleManager.GetString("Tool_EndPasteMArkingMessage", Culture);

		/// <summary>
		/// Density
		/// </summary>
		public static string StyleOption_Density => LocaleManager.GetString("StyleOption_Density", Culture);

		/// <summary>
		/// Scale
		/// </summary>
		public static string StyleOption_Scale => LocaleManager.GetString("StyleOption_Scale", Culture);

		/// <summary>
		/// Cracks
		/// </summary>
		public static string StyleOption_Cracks => LocaleManager.GetString("StyleOption_Cracks", Culture);

		/// <summary>
		/// Voids
		/// </summary>
		public static string StyleOption_Voids => LocaleManager.GetString("StyleOption_Voids", Culture);

		/// <summary>
		/// Texture density
		/// </summary>
		public static string StyleOption_Texture => LocaleManager.GetString("StyleOption_Texture", Culture);

		/// <summary>
		/// Additional
		/// </summary>
		public static string StyleOptionCategory_Additional => LocaleManager.GetString("StyleOptionCategory_Additional", Culture);

		/// <summary>
		/// Graphic effects
		/// </summary>
		public static string StyleOptionCategory_Effect => LocaleManager.GetString("StyleOptionCategory_Effect", Culture);

		/// <summary>
		/// Main
		/// </summary>
		public static string StyleOptionCategory_Main => LocaleManager.GetString("StyleOptionCategory_Main", Culture);

		/// <summary>
		/// Apply to all items
		/// </summary>
		public static string HeaderPanel_ApplyAll => LocaleManager.GetString("HeaderPanel_ApplyAll", Culture);

		/// <summary>
		/// Apply to all rules
		/// </summary>
		public static string HeaderPanel_ApplyAllRules => LocaleManager.GetString("HeaderPanel_ApplyAllRules", Culture);

		/// <summary>
		/// Apply to all crosswalks
		/// </summary>
		public static string HeaderPanel_ApplyCrosswalkAll => LocaleManager.GetString("HeaderPanel_ApplyCrosswalkAll", Culture);

		/// <summary>
		/// Apply to all "{0}" crosswalks
		/// </summary>
		public static string HeaderPanel_ApplyCrosswalkType => LocaleManager.GetString("HeaderPanel_ApplyCrosswalkType", Culture);

		/// <summary>
		/// Apply to all fillers
		/// </summary>
		public static string HeaderPanel_ApplyFillerAll => LocaleManager.GetString("HeaderPanel_ApplyFillerAll", Culture);

		/// <summary>
		/// Apply to all "{0}" fillers
		/// </summary>
		public static string HeaderPanel_ApplyFillerType => LocaleManager.GetString("HeaderPanel_ApplyFillerType", Culture);

		/// <summary>
		/// Apply to all lines
		/// </summary>
		public static string HeaderPanel_ApplyRegularAll => LocaleManager.GetString("HeaderPanel_ApplyRegularAll", Culture);

		/// <summary>
		/// Apply to all "{0}" lines
		/// </summary>
		public static string HeaderPanel_ApplyRegularType => LocaleManager.GetString("HeaderPanel_ApplyRegularType", Culture);

		/// <summary>
		/// Apply to all stop lines
		/// </summary>
		public static string HeaderPanel_ApplyStopAll => LocaleManager.GetString("HeaderPanel_ApplyStopAll", Culture);

		/// <summary>
		/// Apply to all "{0}" stop lines
		/// </summary>
		public static string HeaderPanel_ApplyStopType => LocaleManager.GetString("HeaderPanel_ApplyStopType", Culture);

		/// <summary>
		/// Copy effects
		/// </summary>
		public static string HeaderPanel_CopyEffects => LocaleManager.GetString("HeaderPanel_CopyEffects", Culture);

		/// <summary>
		/// Paste effects
		/// </summary>
		public static string HeaderPanel_PasteEffects => LocaleManager.GetString("HeaderPanel_PasteEffects", Culture);

		/// <summary>
		/// Both
		/// </summary>
		public static string StyleOption_FixedEndBoth => LocaleManager.GetString("StyleOption_FixedEndBoth", Culture);

		/// <summary>
		/// End
		/// </summary>
		public static string StyleOption_FixedEndEnd => LocaleManager.GetString("StyleOption_FixedEndEnd", Culture);

		/// <summary>
		/// Start
		/// </summary>
		public static string StyleOption_FixedEndStart => LocaleManager.GetString("StyleOption_FixedEndStart", Culture);

		/// <summary>
		/// Fixed end
		/// </summary>
		public static string StyleOption_FixedEnd => LocaleManager.GetString("StyleOption_FixedEnd", Culture);

		/// <summary>
		/// Max
		/// </summary>
		public static string StyleOption_Max => LocaleManager.GetString("StyleOption_Max", Culture);

		/// <summary>
		/// Min
		/// </summary>
		public static string StyleOption_Min => LocaleManager.GetString("StyleOption_Min", Culture);

		/// <summary>
		/// Limits
		/// </summary>
		public static string StyleOption_ObjectLimits => LocaleManager.GetString("StyleOption_ObjectLimits", Culture);

		/// <summary>
		/// Not parallel
		/// </summary>
		public static string StyleOption_NotParallel => LocaleManager.GetString("StyleOption_NotParallel", Culture);

		/// <summary>
		/// Parallel to lines and slope end
		/// </summary>
		public static string StyleOption_ParallelSlope => LocaleManager.GetString("StyleOption_ParallelSlope", Culture);

		/// <summary>
		/// Parallel to lines and straight ends
		/// </summary>
		public static string StyleOption_ParallelStraight => LocaleManager.GetString("StyleOption_ParallelStraight", Culture);

		/// <summary>
		/// Dashes type
		/// </summary>
		public static string StyleOption_ZebraDashesType => LocaleManager.GetString("StyleOption_ZebraDashesType", Culture);

		/// <summary>
		/// Invert network
		/// </summary>
		public static string StyleOption_InvertNetwork => LocaleManager.GetString("StyleOption_InvertNetwork", Culture);

		/// <summary>
		/// Two different
		/// </summary>
		public static string StyleOption_ObjectTwoDifferent => LocaleManager.GetString("StyleOption_ObjectTwoDifferent", Culture);

		/// <summary>
		/// Hold {0} to collapse/expand all rules
		/// </summary>
		public static string Header_ExpandTooltip => LocaleManager.GetString("Header_ExpandTooltip", Culture);

		/// <summary>
		/// [UPDATED] Improved resolution of rendering and shape of 2D fillers and crosswalks.
		/// </summary>
		public static string Mod_WhatsNewMessage1_13 => LocaleManager.GetString("Mod_WhatsNewMessage1_13", Culture);

		/// <summary>
		/// [UPDATED] Improved solid filler rendering on slope.
		/// </summary>
		public static string Mod_WhatsNewMessage1_13_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_13_1", Culture);

		/// <summary>
		/// Decal
		/// </summary>
		public static string FillerStyle_Decal => LocaleManager.GetString("FillerStyle_Decal", Culture);

		/// <summary>
		/// Decal asset
		/// </summary>
		public static string StyleOption_AssetDecal => LocaleManager.GetString("StyleOption_AssetDecal", Culture);

		/// <summary>
		/// Tiling
		/// </summary>
		public static string StyleOption_Tiling => LocaleManager.GetString("StyleOption_Tiling", Culture);

		/// <summary>
		/// Invert all chevrons
		/// </summary>
		public static string Settings_InvertChevrons => LocaleManager.GetString("Settings_InvertChevrons", Culture);

		/// <summary>
		/// Others
		/// </summary>
		public static string Setting_Others => LocaleManager.GetString("Setting_Others", Culture);
	}
}