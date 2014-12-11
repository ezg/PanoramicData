using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public sealed class MinorNumericTicksProvider : ITicksProvider<double>
	{
		private readonly ITicksProvider<double> parent;
		private Range<double>[] ranges;
		internal void SetRanges(IEnumerable<Range<double>> ranges)
		{
			this.ranges = ranges.ToArray();
		}

		public double[] Coeffs { get; set; }
        //bcz: added
        public  double[] HalfCoeffs { get; set; }
        private double[] curCoeffs { get { return useHalfCoeffs ? HalfCoeffs : Coeffs; } }
        private bool     useHalfCoeffs = false;

        internal MinorNumericTicksProvider(ITicksProvider<double> parent) {
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

		public ITicksInfo<double> GetTicks(Range<double> range, int ticksCount)
		{
			if (curCoeffs.Length == 0) // bcz: modified
				return new TicksInfo<double>();

			var minorTicks = ranges.Select(r => CreateTicks(r)).SelectMany(m => m);
			var res = new TicksInfo<double>();
			res.TickSizes = minorTicks.Select(m => m.Value).ToArray();
			res.Ticks = minorTicks.Select(m => m.Tick).ToArray();

			return res;
		}

		public MinorTickInfo<double>[] CreateTicks(Range<double> range)
		{
			double step = (range.Max - range.Min) / (curCoeffs.Length + 1);//bcz: modified

			MinorTickInfo<double>[] res = new MinorTickInfo<double>[curCoeffs.Length]; // bcz:modified
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

		public ITicksProvider<double> MinorProvider
		{
			get { throw new NotSupportedException(); }
		}

		public ITicksProvider<double> MayorProvider
		{
			get { return parent; }
		}

		#endregion
	}
}
