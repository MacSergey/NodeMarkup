using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class AsphaltFillerStyle : BaseFillerStyle, IColorStyle, IThemeFiller
    {
        public override StyleType Type => StyleType.FillerAsphalt;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        public bool KeepColor => false;

        public PropertyThemeValue Theme { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Offset);
#if DEBUG

#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield break;
            }
        }

        public AsphaltFillerStyle(ThemeHelper.IThemeData theme, Color32 color, Vector2 offset) : base(color, default, offset)
        {
            Theme = new PropertyThemeValue("THM", StyleChanged, theme);
        }

        public override BaseFillerStyle CopyStyle() => new AsphaltFillerStyle(Theme.Value, Color, Offset);
        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is AsphaltFillerStyle asphaltTarger)
            {
                asphaltTarger.Theme.Value = Theme.Value;
            }
        }

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                var theme = (Theme.Value is ThemeHelper.IThemeData themeData ? themeData : ThemeHelper.DefaultTheme).GetTexture(ThemeHelper.TextureType.Asphalt);
                var textureData = new DecalData.TextureData(theme.texture, null, theme.tiling, 0f);

                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    var datas = DecalData.GetData(DecalData.DecalType.Filler, lod, trajectories, SplitParams, Color, textureData, DecalData.EffectData.Default);
                    foreach (var data in datas)
                    {
                        addData(data);
                    }
                }
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<SelectThemeProperty>(this, nameof(Theme), MainCategory, AddThemeProperty));
        }
        protected void AddThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.Label = Localize.StyleOption_Theme;
            themeProperty.Init();
            themeProperty.RawName = Theme.RawName;
            themeProperty.TextureType = ThemeHelper.TextureType.Asphalt;
            themeProperty.Theme = Theme.Value;
            themeProperty.OnValueChanged += (value) => Theme.Value = value;
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Theme.FromXml(config, null);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Theme.ToXml(config);
            return config;
        }
    }
}
