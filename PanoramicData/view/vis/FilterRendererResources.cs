using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace PanoramicData.view.vis
{
    class FilterRendererResources
    {
        private static Dictionary<string, Color> _groupColorLookup = new Dictionary<string, Color>(); 

        public static Color GetGroupingColor(string grouping)
        {
            if (!_groupColorLookup.ContainsKey(grouping))
            {
                _groupColorLookup.Add(grouping,
                    SERIES_COLORS[_groupColorLookup.Count%FilterRendererResources.SERIES_COLORS.Length]);
            }
            return _groupColorLookup[grouping];
        }

        public static Color[] SERIES_COLORS = new Color[] {
            Color.FromRgb(26, 188, 156),
            Color.FromRgb(243, 156, 18),
            Color.FromRgb(52, 152, 219),
            Color.FromRgb(52, 73, 94),
            Color.FromRgb(142, 68, 173),
            Color.FromRgb(241, 196, 15),
            Color.FromRgb(231, 76, 60),
            Color.FromRgb(149, 165, 166),
            Color.FromRgb(211, 84, 0),
            Color.FromRgb(189, 195, 199),
            Color.FromRgb(46, 204, 113),
            Color.FromRgb(155, 89, 182),
            Color.FromRgb(22, 160, 133),
            Color.FromRgb(41, 128, 185),
            Color.FromRgb(44, 62, 80),
            Color.FromRgb(230, 126, 34),
            Color.FromRgb(39, 174, 96),
            Color.FromRgb(127, 140, 141),
            Color.FromRgb(192, 57, 43)
        }.Reverse().ToArray();
    }
}
