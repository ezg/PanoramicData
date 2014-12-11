using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public sealed class MinorFractionTicksProvider : ITicksProvider<Fraction>
	{
		private readonly ITicksProvider<Fraction> parent;
		private Range<Fraction>[] ranges;
		internal void SetRanges(IEnumerable<Range<Fraction>> ranges)
		{
			this.ranges = ranges.ToArray();
		}

		public double[] Coeffs { get; set; }
        //bcz: added
        public  double[] HalfCoeffs { get; set; }
        private double[] curCoeffs { get { return useHalfCoeffs ? HalfCoeffs : Coeffs; } }
        private bool     useHalfCoeffs = false;

        internal MinorFractionTicksProvider(ITicksProvider<Fraction> parent) {
			this.parent = parent;
			Coeffs = new double[] { };
            HalfCoeffs = new double[] { };  // bcz: 
		}

		#region ITicksProvider<double> Members

		public event EventHandler Changed;
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		private void RaiseChanged()
		{
			if (Changed != null)
			{
				Changed(this, EventArgs.Empty);
			}
		}

		public ITicksInfo<Fraction> GetTicks(Range<Fraction> range, int ticksCount)
		{
			if (curCoeffs.Length == 0) // bcz: modified
				return new TicksInfo<Fraction>();

			var minorTicks = ranges.Select(r => CreateTicks(r)).SelectMany(m => m);
			var res = new TicksInfo<Fraction>();
			res.TickSizes = minorTicks.Select(m => m.Value).ToArray();
			res.Ticks = minorTicks.Select(m => m.Tick).ToArray();

			return res;
		}

		public MinorTickInfo<Fraction>[] CreateTicks(Range<Fraction> range)
		{
			double step = (range.Max - range.Min) / (curCoeffs.Length + 1);//bcz: modified

			MinorTickInfo<Fraction>[] res = new MinorTickInfo<Fraction>[curCoeffs.Length]; // bcz:modified
			for (int i = 0; i < curCoeffs.Length; i++)  // bcz: modified
			{
				res[i] = new MinorTickInfo<double>(curCoeffs[i], range.Min + step * (i + 1)); // bcz: modified
			}
			return res;
		}

		public int DecreaseTickCount(int ticksCount)
		{
            useHalfCoeffs = ticksCount <= 10;  //bcz : modified
            return curCoeffs.Count();
		}

		public int IncreaseTickCount(int ticksCount) {
            useHalfCoeffs = false; // bcz: modified
            return ticksCount;
		}

		public ITicksProvider<Fraction> MinorProvider
		{
			get { throw new NotSupportedException(); }
		}

		public ITicksProvider<Fraction> MayorProvider
		{
			get { return parent; }
		}

		#endregion
	}
}
