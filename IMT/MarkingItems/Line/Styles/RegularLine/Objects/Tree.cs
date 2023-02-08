using IMT.API;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public class TreeLineStyle : BaseObjectLineStyle<TreeInfo, SelectTreeProperty>
    {
        public override StyleType Type => StyleType.LineTree;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        protected override string AssetPropertyName => Localize.StyleOption_AssetTree;
        public override bool CanSlope => true;
        public override bool CanElevate => true;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(Distribution);
                yield return nameof(Probability);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Tilt);
                yield return nameof(Slope);
                yield return nameof(Shift);
                yield return nameof(Elevation);
                yield return nameof(Scale);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<TreeInfo>(nameof(Prefab), Prefab);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<DistributionType>(nameof(Distribution), Distribution);
                yield return new StylePropertyDataProvider<int>(nameof(Probability), Probability);
                yield return new StylePropertyDataProvider<float?>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Tilt), Tilt);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Slope), Slope);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public TreeLineStyle(TreeInfo tree, int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter, DistributionType distribution) : base(tree, probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter, distribution) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value, Probability, Step, Angle, Tilt, Slope, Shift, Scale, Elevation, OffsetBefore, OffsetAfter, Distribution);

        protected override void CalculateParts(TreeInfo tree, MarkingPropItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingTreeData(tree, items));
        }

        protected override bool IsValidPrefab(TreeInfo info) => info != null;
        protected override Func<TreeInfo, string> GetSortPredicate() => Utilities.Utilities.GetPrefabName;
    }
}
