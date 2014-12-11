/**
 * The $N Multistroke Recognizer (C# version)
 *
 *	    Lisa Anthony, Ph.D.
 *		UMBC
 *		Information Systems Department
 * 		1000 Hilltop Circle
 *		Baltimore, MD 21250
 * 		lanthony@umbc.edu
 * 
 *      Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 * The Protractor enhancement was published by Yang Li and programmed
 * here by Lisa Anthony and Jacob O. Wobbrock.
 *
 *	Li, Y. (2010). Protractor: A fast and accurate gesture 
 *	  recognizer. Proceedings of the ACM Conference on Human 
 *	  Factors in Computing Systems (CHI '10). Atlanta, Georgia
 *	  (April 10-15, 2010). New York: ACM Press, pp. 2169-2172.
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2011, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington nor UMBC,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Lisa Anthony 
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
**/
using System;
using System.Drawing;

namespace Recognizer.NDollar
{
	public struct RectangleR
	{
		private const int Digits = 4;
		private double _x;
		private double _y;
		private double _width;
		private double _height;
		public static readonly RectangleR Empty = new RectangleR();

		public RectangleR(double x, double y, double width, double height)
		{
			_x = x;
			_y = y;
			_width = width;
			_height = height;
		}

		// copy constructor
		public RectangleR(RectangleR r)
		{
			_x = r.X;
			_y = r.Y;
			_width = r.Width;
			_height = r.Height;
		}

		public double X
		{
			get
			{
				return Math.Round(_x, Digits);
			}
			set
			{
				_x = value;
			}
		}

		public double Y
		{
			get
			{
				return Math.Round(_y, Digits);
			}
			set
			{
				_y = value;
			}
		}

		public double Width
		{
			get
			{
				return Math.Round(_width, Digits);
			}
			set
			{
				_width = value;
			}
		}

		public double Height
		{
			get
			{
				return Math.Round(_height, Digits);
			}
			set
			{
				_height = value;
			}
		}

		public PointR TopLeft
		{
			get
			{
				return new PointR(X, Y);
			}
		}

		public PointR BottomRight
		{
			get
			{
				return new PointR(X + Width, Y + Height);
			}
		}

		public PointR Center
		{
			get
			{
				return new PointR(X + Width / 2d, Y + Height / 2d);
			}
		}

		public double MaxSide
		{
			get
			{
				return Math.Max(_width, _height);
			}
		}

		public double MinSide
		{
			get
			{
				return Math.Min(_width, _height);
			}
		}

		public double Diagonal
		{
			get
			{
				return Utils.Distance(TopLeft, BottomRight);
			}
		}

		public static explicit operator RectangleF(RectangleR r)
		{
			return new RectangleF((float) r.X, (float) r.Y, (float) r.Width, (float) r.Height);
		}

		public override bool Equals(object obj)
		{
			if (obj is RectangleR)
			{
				RectangleR r = (RectangleR) obj;
				return (X == r.X && Y == r.Y && Width == r.Width && Height == r.Height);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((RectangleF) this).GetHashCode();
		}


	}
}
