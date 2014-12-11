using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FSBigInt = System.Numerics.BigInteger;

namespace starPadSDK.MathExpr {
    /// <summary>
    /// Convenience wrapper around F#'s arbitrary-precision integer class, Microsoft.FSharp.Math.BigInt.
    /// Includes overloaded operators.
    /// </summary>
    [Serializable]
    public struct BigInt : IComparable<BigInt>, IComparable {
        private FSBigInt _num;
        public FSBigInt Num { get { return _num; } }
        public BigInt(FSBigInt num) { _num = num; }
        public BigInt(string digits) { _num = FSBigInt.Parse(digits); }
        public override string ToString() { return _num.ToString(); }

        public BigInt abs() { return FSBigInt.Abs(_num); }
        public int CompareTo(BigInt x) { return (_num - x._num).Sign; }
        public int CompareTo(object obj) { return (_num - ((BigInt)obj)._num).Sign; }
        public override bool Equals(object obj) {
            if(obj.GetType() != this.GetType()) return false;
            BigInt bi = (BigInt)obj;
            return _num.Equals(bi._num);
        }
        public BigInt gcd(BigInt x) { return FSBigInt.GreatestCommonDivisor(_num, x._num); }
        public override int GetHashCode() {
            return _num.GetHashCode();
        }

        public static implicit operator BigInt(FSBigInt i) { return new BigInt(i); }
        public static implicit operator BigInt(long i) { return new BigInt(new FSBigInt(i)); }

        public static BigInt operator+(BigInt a) { return a; }
        public static BigInt operator-(BigInt a) { return -a.Num; }
        public static BigInt operator+(BigInt a, BigInt b) { return a.Num + b.Num; }
        public static BigInt operator-(BigInt a, BigInt b) { return a.Num - b.Num; }
        public static BigInt operator*(BigInt a, BigInt b) { return a.Num*b.Num; }
        public static BigInt operator/(BigInt a, BigInt b) { return a.Num/b.Num; }
        public static BigInt operator%(BigInt a, BigInt b) { return a.Num%b.Num; }

        public static bool operator==(BigInt a, BigInt b) { return a.Num == b.Num; }
        public static bool operator!=(BigInt a, BigInt b) { return a.Num != b.Num; }
        public static bool operator<(BigInt a, BigInt b) { return a.Num < b.Num; }
        public static bool operator>(BigInt a, BigInt b) { return a.Num > b.Num; }
        public static bool operator<=(BigInt a, BigInt b) { return a.Num <= b.Num; }
        public static bool operator>=(BigInt a, BigInt b) { return a.Num >= b.Num; }

        private static FSBigInt _maxint = new FSBigInt(int.MaxValue);
        private static FSBigInt _minint = new FSBigInt(int.MinValue);
        public bool CanBeInt() { return _num <= _maxint && _num >= _minint; }
        public static explicit operator int(BigInt i) {
            if(!i.CanBeInt()) throw new ArithmeticException("BigInt is too large to convert to an integer: " + i.ToString());
            return (int)(i.Num);
        }
        private static FSBigInt _maxlong = new FSBigInt(long.MaxValue);
        private static FSBigInt _minlong = new FSBigInt(long.MinValue);
        public bool CanBeLong() { return _num <= _maxlong && _num >= _minlong; }
        public static explicit operator long(BigInt i) {
            if(!i.CanBeLong()) throw new ArithmeticException("BigInt is too large to convert to a long: " + i.ToString());
            return (int)(i.Num);
        }
        public static explicit operator double(BigInt i) { return i.AsDouble(); }
        /// <summary>
        ///  WARNING: THIS WILL LOSE INFORMATION
        /// </summary>
        /// <returns></returns>
        public double AsDouble() { return (double)(_num); }

        static private readonly BigInt _zero = (BigInt)0;
        static public BigInt Zero { get { return _zero; } }
        public bool IsZero { get { return _num.IsZero; } }
        static private readonly BigInt _one = (BigInt)1;
        static public BigInt One { get { return _one; } }
        public bool IsOne { get { return _num.IsOne; } }
    }
    /// <summary>
    /// Arbitrary-precision rational numbers, built out of BigInts.
    /// </summary>
    [Serializable]
    public struct BigRat : IComparable<BigRat>, IComparable {
        private BigInt _num, _denom;
        public BigInt Num { get { return _num; } }
        public BigInt Denom { get { return _denom; } }
        public BigRat(BigInt num) { _num = num; _denom = BigInt.One; }
        public BigRat(BigInt num, BigInt denom) {
            _num = num; _denom = denom;
            Normalize();
        }
        private BigRat(BigInt num, BigInt denom, bool normalizedummy) {
            _num = num; _denom = denom;
        }
        public override string ToString() { return _num.ToString()+"/"+_denom.ToString(); }

        public BigRat abs() { return new BigRat(_num.abs(), _denom.abs(), true); }
        public int CompareTo(BigRat x) { return (_num*x._denom).CompareTo(x._num*_denom); }
        public int CompareTo(object o) { BigRat x = (BigRat)o; return (_num*x._denom).CompareTo(x._num*_denom); }
        public override bool Equals(object obj) {
            if(obj.GetType() != this.GetType()) return false;
            BigRat br = (BigRat)obj;
            return _num.Equals(br._num) && _denom.Equals(br._denom);
        }
        public override int GetHashCode() {
            return _num.GetHashCode()^_denom.GetHashCode();
        }

        public static implicit operator BigRat(BigInt i) { return new BigRat(i); }
        public static implicit operator BigRat(long i) { return new BigRat(i); }

        public static BigRat operator+(BigRat a) { return a; }
        public static BigRat operator-(BigRat a) { return new BigRat(-a.Num, a.Denom); }
        public static BigRat operator+(BigRat a, BigRat b) { return new BigRat(a.Num*b.Denom+b.Num*a.Denom, a.Denom*b.Denom).Normalize(); }
        public static BigRat operator-(BigRat a, BigRat b) { return new BigRat(a.Num*b.Denom-b.Num*a.Denom, a.Denom*b.Denom).Normalize(); }
        public static BigRat operator*(BigRat a, BigRat b) { return new BigRat(a.Num*b.Num, a.Denom*b.Denom).Normalize(); }
        public static BigRat operator/(BigRat a, BigRat b) { return new BigRat(a.Num*b.Denom, a.Denom*b.Num).Normalize(); }

        public static bool operator==(BigRat a, BigRat b) { return a.CompareTo(b) == 0; }
        public static bool operator!=(BigRat a, BigRat b) { return a.CompareTo(b) != 0; }
        public static bool operator<(BigRat a, BigRat b) { return a.CompareTo(b) < 0; }
        public static bool operator>(BigRat a, BigRat b) { return a.CompareTo(b) > 0; }
        public static bool operator<=(BigRat a, BigRat b) { return a.CompareTo(b) <= 0; }
        public static bool operator>=(BigRat a, BigRat b) { return a.CompareTo(b) >= 0; }

        private BigRat Normalize() {
            if(_denom.IsZero) throw new DivideByZeroException("Denominator of BigRat was 0");
            if(_denom < BigInt.Zero) { _num = -_num; _denom = -_denom; }
            if(!_num.IsZero) {
                BigInt gcd = _num.gcd(_denom);
                if(!gcd.IsOne) { _num /= gcd; _denom /= gcd; }
            } else _denom = BigInt.One;
            return this;
        }

        /// <summary>
        ///  WARNING: THIS WILL LOSE INFORMATION
        /// </summary>
        /// <returns></returns>
        public double AsDouble() {
            return _num.AsDouble()/_denom.AsDouble();
        }

        static private readonly BigRat _infinity = new BigRat(BigInt.One, BigInt.Zero, false);
        /// <summary>
        /// This is just for use as a loop bound! Don't try to compute with it; just compare to it.
        /// </summary>
        static public BigRat Infinity { get { return _infinity; } }
        static private readonly BigRat _zero = new BigRat(BigInt.Zero);
        static public BigRat Zero { get { return _zero; } }
        static private readonly BigRat _one = new BigRat(BigInt.One);
        static public BigRat One { get { return _one; } }
    }
}