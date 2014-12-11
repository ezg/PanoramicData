using PanoramicData.view.filter;
using System.Linq;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for PivotFilterRenderer.xaml
    /// </summary>
    public partial class PivotFilterRenderer : FilterRenderer
    {
        public PivotFilterRenderer()
            : this(false)
        {
        }

        public PivotFilterRenderer(bool showSettings)
        {
            InitializeComponent();
        }


        protected override void Init(bool resetViewport)
        {
            if (FilterModel == null)
            {
                return;
            }
            wrapPanel.Children.Clear();
            bool anySelected = FilterModel.Pivots.Any(p => p.Selected);
            foreach (var pivot in FilterModel.Pivots)
            {
                PivotFilterItemRenderer r = new PivotFilterItemRenderer();
                r.DataContext = pivot;
                r.IsAnySelected = anySelected;
                wrapPanel.Children.Add(r);
            }
        }
    }
}
