﻿using Microsoft.Practices.Prism.Mvvm;
using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewModel : BindableBase
    {
        private Dictionary<AttributeFunction, List<AttributeViewModel>> _attributeViewModels = new Dictionary<AttributeFunction, List<AttributeViewModel>>();

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
    }
}