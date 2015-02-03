using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data
{
    public class LinkModel : ExtendedBindableBase
    {
        private QueryModel _fromQueryModel = null;
        public QueryModel FromQueryModel
        {
            get
            {
                return _fromQueryModel;
            }
            set
            {
                this.SetProperty(ref _fromQueryModel, value);
            }
        }
        
        private QueryModel _toQueryModel = null;
        public QueryModel ToQueryModel
        {
            get
            {
                return _toQueryModel;
            }
            set
            {
                this.SetProperty(ref _toQueryModel, value);
            }
        }

        private LinkType _linkType = LinkType.Filter;
        public LinkType LinkType
        {
            get
            {
                return _linkType;
            }
            set
            {
                this.SetProperty(ref _linkType, value);
            }
        }
    }

    public enum LinkType { Filter, Brush }
}
