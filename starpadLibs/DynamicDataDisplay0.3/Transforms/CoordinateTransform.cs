using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Research.DynamicDataDisplay.Common;
using System.Diagnostics;

namespace Microsoft.Research.DynamicDataDisplay
{
	/// <summary>
	/// A central class in 2d coordinate transformation in DynamicDataDisplay.
	/// Provides methods to transform point from one coordinate system to another.
	/// </summary>
	public sealed class CoordinateTransform
	{
		private CoordinateTransform(Rect visibleRect, Rect screenRect)
		{
			this.visibleRect = visibleRect;
			this.screenRect = screenRect;

			rxToScreen = screenRect.Width / visibleRect.Width;
			ryToScreen = screenRect.Height / visibleRect.Height;
			cxToScreen = visibleRect.Left * rxToScreen - screenRect.Left;
			cyToScreen = screenRect.Height + screenRect.Top + visibleRect.Top * ryToScreen;

			rxToData = visibleRect.Width / screenRect.Width;
			ryToData = visibleRect.Height / screenRect.Height;
			cxToData = screenRect.Left * rxToData - visibleRect.Left;
			cyToData = visibleRect.Height + visibleRect.Top + screenRect.Top * ryToData;
		}

		#region Coeffs
		double rxToScreen;
		double ryToScreen;
		double cxToScreen;
		double cyToScreen;

		double rxToData;
		double ryToData;
		double cxToData;
		double cyToData;
		#endregion

		#region Creation methods

		internal CoordinateTransform WithRects(Rect visibleRect, Rect screenRect)
		{
			CoordinateTransform copy = new CoordinateTransform(visibleRect, screenRect);
			copy.dataTransform = dataTransform;
			return copy;
		}

		/// <summary>
		/// Creates a new instance of CoordinateTransform with the given data transform.
		/// </summary>
		/// <param name="dataTransform">The data transform.</param>
		/// <returns></returns>
		public CoordinateTransform WithDataTransform(DataTransform dataTransform)
		{
			if (dataTransform == null)
				throw new ArgumentNullException("dataTransform");

			CoordinateTransform copy = new CoordinateTransform(visibleRect, screenRect);
			copy.dataTransform = dataTransform;
			return copy;
		}

		internal static CoordinateTransform CreateDefault()
		{
			CoordinateTransform transform = new CoordinateTransform(new Rect(0, 0, 1, 1), new Rect(0, 0, 1, 1));

			return transform;
		}

        #endregion

        // bcz: added region
        #region discontinuities
        public  const double NumToPosInfinityDiscontinuity = double.MaxValue-1e293;
        public  const double NumToNegInfinityDiscontinuity = double.MinValue+2e293;
        public  const double PosToNumInfinityDiscontinuity = double.MinValue-3e293;
        public  const double NegToNumInfinityDiscontinuity = double.MinValue+3e293;
        public  const double PosToNegDiscontinuity = double.MaxValue;
        public  const double NegToPosDiscontinuity = double.MinValue;
        public  const double PosToPosDiscontinuity = double.MaxValue - 1e293;
        public  const double NegToNegDiscontinuity = double.MinValue + 1e293;
        public static double DiscontinuityToNumber(double d, bool leftSide) {
            if (leftSide) {
                if (d == PosToPosDiscontinuity || d == PosToNumInfinityDiscontinuity || d == PosToNegDiscontinuity)
                    return 100000;
                if (d == NegToPosDiscontinuity || d == NegToNumInfinityDiscontinuity || d == NegToNegDiscontinuity)
                    return -100000;
                return d;
            }
            else {
                if (d == PosToPosDiscontinuity || d == NumToPosInfinityDiscontinuity || d == NegToPosDiscontinuity)
                    return 100000;
                if (d == NegToNegDiscontinuity || d == NumToNegInfinityDiscontinuity || d == PosToNegDiscontinuity)
                    return -100000;
                return d;
            }
        }
        public static Point DiscontinuityToPoint(Point p, bool leftSide) {
            return new Point(p.X, DiscontinuityToNumber(p.Y, leftSide));
        }
        public static bool IsDiscontinuity(double d) {
            return d == PosToNumInfinityDiscontinuity || d == NegToNumInfinityDiscontinuity ||
                d == NumToPosInfinityDiscontinuity || d == NumToNegInfinityDiscontinuity ||
                d == PosToNegDiscontinuity || d == NegToPosDiscontinuity ||
                d == PosToPosDiscontinuity || d == NegToNegDiscontinuity;
        }
        public static double NegateDiscontinuity(double d) {
            if (d == PosToPosDiscontinuity)
                return NegToNegDiscontinuity;
            if (d == NegToNegDiscontinuity)
                return PosToPosDiscontinuity;
            if (d == PosToNegDiscontinuity)
                return NegToPosDiscontinuity;
            if (d == NegToPosDiscontinuity)
                return PosToNegDiscontinuity;
            if (d == NumToPosInfinityDiscontinuity)
                return NumToNegInfinityDiscontinuity;
            if (d == NumToNegInfinityDiscontinuity)
                return NumToPosInfinityDiscontinuity;
            if (d == NegToNumInfinityDiscontinuity)
                return PosToNumInfinityDiscontinuity;
            if (d == PosToNumInfinityDiscontinuity)
                return NegToNumInfinityDiscontinuity;
            return d;
        }
        #endregion

        #region Transform methods

        /// <summary>
		/// Transforms point from data coordinates to screen.
		/// </summary>
		/// <param name="dataPoint">The point in data coordinates.</param>
		/// <returns></returns>
		public Point DataToScreen(Point dataPoint)
		{
			Point viewportPoint = dataTransform.DataToViewport(dataPoint);
            
            // bcz: modified
			Point screenPoint = new Point(
                IsDiscontinuity(dataPoint.X) ? dataPoint.X :  viewportPoint.X * rxToScreen - cxToScreen,
				IsDiscontinuity(dataPoint.Y) ? NegateDiscontinuity(dataPoint.Y) :  cyToScreen - viewportPoint.Y * ryToScreen);

			return screenPoint;
		}

		/// <summary>
		/// Transforms point from screen coordinates to data coordinates.
		/// </summary>
		/// <param name="screenPoint">The point in screen coordinates.</param>
		/// <returns></returns>
		public Point ScreenToData(Point screenPoint)
		{
			Point viewportPoint = new Point(screenPoint.X * rxToData - cxToData,
				cyToData - screenPoint.Y * ryToData);

			Point dataPoint = dataTransform.ViewportToData(viewportPoint);

			return dataPoint;
		}

		/// <summary>
		/// Transforms point from viewport coordinates to screen coordinates.
		/// </summary>
		/// <param name="viewportPoint">The point in viewport coordinates.</param>
		/// <returns></returns>
		public Point ViewportToScreen(Point viewportPoint)
		{
			Point screenPoint = new Point(viewportPoint.X * rxToScreen - cxToScreen,
				cyToScreen - viewportPoint.Y * ryToScreen);

			return screenPoint;
		}

		/// <summary>
		/// Transforms point from screen coordinates to viewport coordinates.
		/// </summary>
		/// <param name="screenPoint">The point in screen coordinates.</param>
		/// <returns></returns>
		public Point ScreenToViewport(Point screenPoint)
		{
			Point viewportPoint = new Point(screenPoint.X * rxToData - cxToData,
				cyToData - screenPoint.Y * ryToData);

			return viewportPoint;
		}

		#endregion

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Rect visibleRect;
		/// <summary>
		/// Gets the viewport rectangle.
		/// </summary>
		/// <value>The viewport rect.</value>
		public Rect ViewportRect
		{
			get { return visibleRect; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Rect screenRect;
		/// <summary>
		/// Gets the screen rectangle.
		/// </summary>
		/// <value>The screen rect.</value>
		public Rect ScreenRect
		{
			get { return screenRect; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private DataTransform dataTransform = DataTransforms.Identity;
		/// <summary>
		/// Gets the data transform.
		/// </summary>
		/// <value>The data transform.</value>
		public DataTransform DataTransform
		{
			get { return dataTransform; }
		}
	}
}
