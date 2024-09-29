using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class TreeLineStyle : BaseObject3DObjectStyle<TreeInfo, SelectTreeProperty>
    {
        public override StyleType Type => StyleType.LineTree;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        public PropertyBoolValue Wind { get; }
        protected override string AssetPropertyName => Localize.StyleOption_AssetTree;
        protected override IComparer<TreeInfo> Comparer => new Utilities.Utilities.TreeComparer();

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
                yield return nameof(Wind);
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

        public TreeLineStyle(TreeInfo tree, int probability, float? step, Vector2? angle, Spread angleSpread, Vector2 shift, Spread shiftSpread, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount, Vector2 tilt, Spread tiltSpread, Vector2? slope, Spread slopeSpread, Vector2 scale, Spread scaleSpread, Vector2 elevation, Spread elevationSpread, bool wind) : base(tree, probability, step, angle, angleSpread, shift, shiftSpread, offsetBefore, offsetAfter, distribution, fixedEnd, minCount, maxCount, tilt, tiltSpread, slope, slopeSpread, scale, scaleSpread, elevation, elevationSpread) 
        {
            Wind = new PropertyBoolValue("WN", StyleChanged, wind);
        }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value, Probability, Step, Angle, AngleSpread, Shift, ShiftSpread, OffsetBefore, OffsetAfter, Distribution, FixedEnd, MinCount, MaxCount, Tilt, TiltSpread, Slope, SlopeSpread, Scale, ScaleSpread, Elevation, ElevationSpread, Wind);

        protected override void CalculateItem(ITrajectory trajectory, float t, float p, TreeInfo prefab, ref MarkingObjectItemData item)
        {
            base.CalculateItem(trajectory, t, p, prefab, ref item);
            item.wind = Wind;
        }
        protected override void AddData(TreeInfo tree, MarkingObjectItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingTreeData(tree, items));
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<BoolPropertyPanel>(this, nameof(Wind), AdditionalCategory, AddWindProperty, RefreshWindProperty));
        }

        private void AddWindProperty(BoolPropertyPanel windProperty, EditorProvider provider)
        {
            windProperty.Label = Localize.StyleOption_Wind;
            windProperty.Init();
            windProperty.Value = Wind;
            windProperty.OnValueChanged += (value) => Wind.Value = value;
        }
        private void RefreshWindProperty(BoolPropertyPanel windProperty, EditorProvider provider)
        {
            windProperty.IsHidden = !IsValid;
        }

        public override bool IsValidPrefab(TreeInfo info) => info != null;

        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            trees.Add(Prefab.RawName);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Wind.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Wind.FromXml(config, true);
        }
    }
}
