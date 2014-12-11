using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using System.Collections.ObjectModel;

namespace Microsoft.Research.DynamicDataDisplay.Charts
{
	public sealed class FractionTicksProvider : ITicksProvider<Fraction> {
        public FractionTicksProvider() : this(10) { } // bcz: added
		public FractionTicksProvider(double units)
		{
            TickUnits = units; // bcz: added
            minorProvider = new MinorFractionTicksProvider(this);
            minorProvider.Coeffs = new double[] { 0.3, 0.3, 0.3, 0.3, 0.6, 0.3, 0.3, 0.3, 0.3 };
            minorProvider.HalfCoeffs = new double[] { .3, .3, .6, .3, .3 };
		}

		public event EventHandler Changed;
		private void RaiseChangedEvent()
		{
			Changed.Raise(this);
		}

		private double minStep = 0.0;
		public double MinStep
		{
			get { return minStep; }
			set
			{
				Verify.IsTrue(value >= 0.0, "value");
				if (minStep != value)
				{
					minStep = value;
					RaiseChangedEvent();
				}
			}
		}
        public double TickUnits = 10; // bcz: added
		private double[] ticks;
		public ITicksInfo<Fraction> GetTicks(Range<Fraction> range, int ticksCount) {
            double start = range.Min.ToDouble(); // bcz: modified
            double finish = range.Max.ToDouble(); // bcz: modified

			double delta = finish - start;

			int log = (int)Math.Round(Math.Log10(delta));

			double newStart = RoundHelper.Round(start, log);
			double newFinish = RoundHelper.Round(finish, log);
			if (newStart == newFinish)
			{
				log--;
				newStart = RoundHelper.Round(start, log);
				newFinish = RoundHelper.Round(finish, log);
			}

			// calculating step between ticks
			double unroundedStep = (newFinish - newStart) / ticksCount;
			int stepLog = log;
			// trying to round step
			double step = RoundHelper.Round(unroundedStep, stepLog);
			if (step == 0)
			{
				stepLog--;
				step = RoundHelper.Round(unroundedStep, stepLog);
				if (step == 0)
				{
					// step will not be rounded if attempts to be rounded to zero.
					step = unroundedStep;
				}
			}

			if (step < minStep)
				step = minStep;

			if (step != 0.0)
			{
				ticks = CreateTicks(start, finish, step);
			}
			else
			{
				ticks = new Fraction[] { };
			}

			TicksInfo<Fraction> res = new TicksInfo<Fraction> { Info = log, Ticks = ticks };

			return res;
		}

		private static double[] CreateTicks(double start, double finish, double step)
		{
			DebugVerify.Is(step != 0.0);

			double x = step * Math.Floor(start / step);

			if (x == x + step)
			{
				return new double[0];
			}

			List<double> res = new List<double>();

			double increasedFinish = finish + step * 1.05;
			while (x <= increasedFinish)
			{
				res.Add(x);
				DebugVerify.Is(res.Count < 2000);
				x += step;
			}
			return res.ToArray();
		}

        //private static int[] tickCounts = new int[] { 20, 10, 5, 4, 2, 1 };
        private static int[] tickCounts = new int[] { 32, 16, 8, 4, 2, 1 }; // bcz: changed from above

		public const int DefaultPreferredTicksCount = 8;

		public int DecreaseTickCount(int ticksCount)
		{
			return tickCounts.FirstOrDefault(tick => tick < ticksCount);
		}

		public int IncreaseTickCount(int ticksCount)
		{
			int newTickCount = tickCounts.Reverse().FirstOrDefault(tick => tick > ticksCount);
			if (newTickCount == 0)
				newTickCount = tickCounts[0];

			return newTickCount;
		}

		#region ITicksProvider<double> Members

		private readonly MinorFractionTicksProvider minorProvider;
		public ITicksProvider<Fraction> MinorProvider
		{
			get
			{
				minorProvider.SetRanges(ticks.GetPairs());

				return minorProvider;
			}
		}

		public ITicksProvider<Fraction> MayorProvider
		{
			get { return null; }
		}

		#endregion
	}
}
