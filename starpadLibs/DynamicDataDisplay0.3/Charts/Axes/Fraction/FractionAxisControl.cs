using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public class FractionAxisControl : AxisControl<Fraction> {
        public FractionAxisControl() : this(1) { } // bcz: added
		public FractionAxisControl(double units) { // bcz: modified
            TickUnits = units; // bcz added
			LabelProvider = new ExponentialFractionLabelProvider(TickUnits); //bcz : modified
			TicksProvider = new FractionTicksProvider(TickUnits);
			ConvertToDouble = d => d.Numerator/(double)d.Denominator;
			Range = new Range<Fraction>(new Fraction(0,0), new Fraction(10,1));
		}
	}
}
