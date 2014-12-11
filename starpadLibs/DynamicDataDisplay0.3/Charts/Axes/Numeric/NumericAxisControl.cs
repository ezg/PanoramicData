using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public class NumericAxisControl : AxisControl<double> {
        public NumericAxisControl() : this(1) { } // bcz: added
		public NumericAxisControl(double units) { // bcz: modified
            TickUnits = units; // bcz added
			LabelProvider = new ExponentialLabelProvider(TickUnits); //bcz : modified
			TicksProvider = new NumericTicksProvider(TickUnits);
			ConvertToDouble = d => d;
			Range = new Range<double>(0, 10);
		}
	}
}
