using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.DynamicDataDisplay.Charts {
    public class Fraction {
        private int numerator;

        public int Numerator {
            get { return numerator; }
            set { numerator = value; }
        }
        private int denominator;

        public int Denominator {
            get { return denominator; }
            set { denominator = value; }
        }
        public Fraction(int num, int den) {
            numerator = num;
            denominator = den;
        }
        public double ToDouble() {
            return numerator / (double)denominator;
        }
        public override string ToString() {
            return numerator.ToString() + "/" + denominator.ToString();
        }
    }
}
