using Microsoft.Practices.Prism.Mvvm;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view
{
    public class SchemaViewModel : BindableBase
    {
        private Vec _size = new Vec(180, 100);

        public Vec Size
        {
            get
            {
                return _size;
            }
            set
            {
                this.SetProperty(ref _size, value);
            }
        }

        private Pt _postion;

        public Pt Position
        {
            get
            {
                return _postion;
            }
            set
            {
                this.SetProperty(ref _postion, value);
            }
        }

        private TableModel _tableModel;

        public TableModel TableModel
        {
            get
            {
                return _tableModel;
            }
            set
            {
                this.SetProperty(ref _tableModel, value);
            }
        }
    }
}
