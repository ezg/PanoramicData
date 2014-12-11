using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public class FractionAxis : AxisBase<Fraction>
	{
        public FractionAxis() : this(1) { }
		public FractionAxis(double units)
			: base(new FractionAxisControl(units),
				d => new Fraction(0,0), //bcz: ack!
				d => d.Numerator/(double)d.Denominator)
		{
		}
	}
}
