using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Axes
{
	public class CollectionLabelProvider<T> : LabelProviderBase<T>
	{
        private IDictionary<T, string> collection;

		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
		public IDictionary<T, string> Collection
		{
			get { return collection; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (collection != value)
				{
					DetachCollection();

					collection = value;

					AttachCollection();

					RaiseChanged();
				}
			}
		}

		#region Collection changed

		private void AttachCollection()
		{
			INotifyCollectionChanged observableCollection = collection as INotifyCollectionChanged;
			if (observableCollection != null)
			{
				observableCollection.CollectionChanged += OnCollectionChanged;
			}
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RaiseChanged();
		}

		private void DetachCollection()
		{
			INotifyCollectionChanged observableCollection = collection as INotifyCollectionChanged;
			if (observableCollection != null)
			{
				observableCollection.CollectionChanged -= OnCollectionChanged;
			}
		} 

		#endregion

	    public override UIElement[] CreateLabels(ITicksInfo<T> ticksInfo)
	    {
	        var ticks = ticksInfo.Ticks;

	        UIElement[] res = new UIElement[ticks.Length];

	        for (int i = 0; i < res.Length; i++)
	        {
	            T tick = ticks[i];

	            if (collection.ContainsKey(tick))
	            {
	                string text = collection[tick].ToString();
	                res[i] = new TextBlock
	                {
	                    Text = text,
	                    ToolTip = text
	                };
	            }
	            else
	            {
	                res[i] = null;
	            }
	        }
	        return res;
	    }
	}
}
