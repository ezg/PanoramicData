using PanoramicData.model.view_new;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.controller.data
{
    public abstract class QueryExecuter
    {
        public abstract void ExecuteQuery(VisualizationViewModel visualizationViewModel);
    }
}
