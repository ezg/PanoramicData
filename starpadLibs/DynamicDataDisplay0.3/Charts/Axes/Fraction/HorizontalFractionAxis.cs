using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public class HorizontalFractionAxis : FractionAxis {
        public HorizontalFractionAxis() : this(1) { } // bcz: added
		public HorizontalFractionAxis(double units) : base(units) // bcz: modified
		{
			Placement = AxisPlacement.Bottom;
		}

		protected override void ValidatePlacement(AxisPlacement newPlacement)
		{
			if (newPlacement == AxisPlacement.Left || newPlacement == AxisPlacement.Right)
				throw new ArgumentException(Properties.Resources.HorizontalAxisCannotBeVertical);
		}
	}
}
