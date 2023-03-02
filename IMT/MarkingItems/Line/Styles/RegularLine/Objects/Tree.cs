using IMT.API;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public class TreeLineStyle : BaseObject3DObjectStyle<TreeInfo, SelectTreeProperty>
    {
        public override StyleType Type => StyleType.LineTree;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        protected override string AssetPropertyName => Localize.StyleOption_AssetTree;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(EnableCount);
                yield return nameof(Distribution);
                yield return nameof(FixedEnd);
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
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Tilt), Tilt);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Slope), Slope);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public TreeLineStyle(TreeInfo tree, int probability, float? step, Vector2? angle, Vector2 shift, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount, Vector2 tilt, Vector2? slope, Vector2 scale, Vector2 elevation) : base(tree, probability, step, angle, shift, offsetBefore, offsetAfter, distribution, fixedEnd, minCount, maxCount, tilt, slope, scale, elevation) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value, Probability, Step, Angle, Shift, OffsetBefore, OffsetAfter, Distribution, FixedEnd, MinCount, MaxCount, Tilt, Slope, Scale, Elevation);

        protected override void AddData(TreeInfo tree, MarkingObjectItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingTreeData(tree, items));
        }

        public override bool IsValidPrefab(TreeInfo info) => info != null;
        protected override Func<TreeInfo, string> GetSortPredicate() => Utilities.Utilities.GetPrefabName;

        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            trees.Add(Prefab.RawName);
        }
    }
}
