// Some systems we might want to interface with: mathematica, maple, (matlab), mathomatic, maxima, macsyma, singular
// See also http://en.wikipedia.org/wiki/Comparison_of_computer_algebra_systems
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace starPadSDK.MathExpr
{
    /* NB: to force parentheses around any expression, set the Annotation "Force Parentheses" on the Expr node you want
     * parentheses around to an integer indicating how many parentheses you want. */

	/* We need Expr subclasses to override Equals (if necessary) so that we can tell whether two
	 * variables are equal, since variables can have subscripts which can be full expressions. */
    /* NB: every nonabstract subclass of Expr must have an equals and gethashcode defined that
     * ignores _annotations */
#pragma warning disable 660, 661 // don't warn about not implementing Equals or GetHashCode
    [Serializable]
	public abstract class Expr {
		public static bool operator==(Expr a, Expr b) { return (Object.ReferenceEquals(a,b) || (!Object.ReferenceEquals(null, b) && !Object.ReferenceEquals(a, null) && a.Equals(b))); }
		public static bool operator!=(Expr a, Expr b) { return !(Object.ReferenceEquals(a, b) || (!Object.ReferenceEquals(null, b) && !Object.ReferenceEquals(a, null) && a.Equals(b))); }
		// this should return a copy such that it is not possible to modify either copy by modifying the other. that is, whatever structure
		// is shared should not be modifiable
		public abstract Expr Clone();
		// pattern matching. returns hash from symbols to values to be bound to to match the literal
		public virtual Hashtable Match(Expr literal) {
			return Equals(literal) ? new Hashtable() : null;
        }
        // Annotations are defined to be ignored by the Expr system, for instance by copying, and since we're really making a copy when serializing
        // we ignore them then too.
        [NonSerialized] private Hashtable _annotations = new Hashtable();
        /// <summary>
        /// Annotations are ignored by the Expr system. Clone() returns a copy with annotations set to a new hashtable.
        /// </summary>
        public Hashtable Annotations { get { if (_annotations == null) _annotations = new Hashtable(); return _annotations; }
                                       set { _annotations = value; } }
        public static implicit operator Expr(long c) { return new IntegerNumber(c); }
        public static implicit operator Expr(BigInt c) { return new IntegerNumber(c); }
        public static implicit operator Expr(BigRat c) { return new RationalNumber(c); }
        public static implicit operator Expr(double c) { return new DoubleNumber(c); }
        public static explicit operator int(Expr e) { if (e is IntegerNumber) return (int)(e as IntegerNumber).Num; throw new ArgumentException(); }
        public static explicit operator double(Expr e) { if (e is IntegerNumber) return (int)e; if (e is DoubleNumber) return (double)(e as DoubleNumber).Num; if (e == WellKnownSym.infinity) return double.PositiveInfinity; return double.NaN; }
        public static explicit operator BigInt(Expr e) { if(e is IntegerNumber) return (e as IntegerNumber).Num; throw new ArgumentException(); }
        public static explicit operator BigRat(Expr e) { if(e is RationalNumber) return (e as RationalNumber).Num; throw new ArgumentException(); }

        /// <summary>
        /// Collects all the terms in the expression tree that contain the specified target Expr (uses Object reference test, not value test)
        /// </summary>
        /// <param name="e">expression to search through</param>
        /// <param name="target">expression to search for</param>
        /// <returns></returns>
        public static List<Expr> FindPath(Expr e, Expr target) {
            List<Expr> selPath = new List<Expr>();
            if (collectExprPath(e, selPath, target))
                return selPath;
            return new List<Expr>();
        }
       static  bool collectExprPath(Expr e, List<Expr> path, Expr target) {
            if (object.ReferenceEquals(e, target)) {
                path.Add(e);
                return true;
            }
            if (e is CompositeExpr) {
                foreach (Expr e1 in (e as CompositeExpr).Args)
                    if (collectExprPath(e1, path, target)) {
                        path.Add(e);
                        return true;
                    }
                if (collectExprPath((e as CompositeExpr).Head, path, target)) {
                    path.Add(e);
                    return true;
                }
            }
           if (e is LetterSym)
               if (collectExprPath((e as LetterSym).Subscript, path, target))
               {
                   path.Add(e);
                   return true;
               }
            return false;
        }

       static  int FindIndexExact(CompositeExpr expr, Expr node) {
           for (int i = 0; i < expr.Args().Length; i++)
               if (Object.ReferenceEquals(expr.Args()[i], node))
                   return i;
           return -1;
       }
       static  public bool ContainsExact(IEnumerable<Expr> list, Expr expr) {
           foreach (Expr e in list)
               if (Object.ReferenceEquals(e, expr))
                   return true;
           return false;
       }
       static  int IndexOfExact(List<Expr> list, Expr expr) {
           for (int i = 0; i < list.Count; i++)
               if (Object.ReferenceEquals(list[i], expr))
                   return i;
           return -1;
       }

       public delegate string ConvertTo(Expr e);
       static public ConvertTo Converter = null;

        /// <summary>
        /// Finds the lowest node in the exprssion tree that contains both the start and end exprs.
        /// </summary>
        /// <param name="expr">the expression to search over</param>
        /// <param name="start">the first expression to search for</param>
        /// <param name="end">the second expression to search for</param>
        /// <param name="ind1">the index of the first expression in the node that contains both first and second</param>
       /// <param name="ind2">the index of the second expression in the node that contains both first and second</param>
        /// <returns></returns>
       public static  CompositeExpr FindCommonAncestor(Expr expr, Expr start, Expr end, out int ind1, out int ind2) {
           CompositeExpr ancestor = null;
           ind1 = ind2 = 0;
           List<Expr> path1 = new List<Expr>();
           List<Expr> path2 = new List<Expr>();
           if (!collectExprPath(expr, path1, start) || !collectExprPath(expr, path2, end))
               return ancestor;
           if (Expr.FindPath(start, end).Count != 0) {
               ancestor = start as CompositeExpr;
               if (ancestor != null) {
                   ind1 = 0;
                   ind2 = ancestor.Args.Length - 1;
               }
               return ancestor;
           }
           if (Expr.FindPath(end, start).Count != 0)
           {
               ancestor = end as CompositeExpr;
               if (ancestor != null) {
                   ind1 = 0;
                   ind2 = ancestor.Args.Length - 1;
               }
               return ancestor;
           }

           ancestor = null;
           Expr par1 = null;
           Expr par2 = null;
           for (int i = 0; i < path1.Count; i++)
               if (ContainsExact(path2, path1[i])) {
                   par1 = path1[i - 1];
                   par2 = path2[IndexOfExact(path2, path1[i]) - 1];
                   ancestor = path1[i] as CompositeExpr;
                   break;
               }

           if (ancestor == null)
               return ancestor;

           ind1 = FindIndexExact(ancestor, par1);
           ind2 = FindIndexExact(ancestor, par2);
           return ancestor;
       }


        [OnDeserialized]
        private void FixUpDeserialization(StreamingContext ctxt) {
            _annotations = new Hashtable();
        }
    }
#pragma warning restore 660, 661

	/// <summary>
	/// This should really only appear in places like subscripts where nothing is to be displayed
	/// </summary>
    [Serializable]
	public sealed class NullExpr : Expr {
		public NullExpr() {}
		public override bool Equals(Object obj) {
			return obj.GetType() == this.GetType();
		}
		public override int GetHashCode() {
			return 0;
		}
		public override Expr Clone() { return this; }
    }

    /// <summary>
    /// This is a base class for parsers to make classes that inherit from so that compute engines etc can recognize errors and not get hung up on them (no
    /// missing method exceptions, in particular).
    /// </summary>
    [Serializable]
    public abstract class ErrorExpr : Expr {
        protected ErrorExpr() { }
    }

    /// <summary>
    /// An error message returned from a computation engine.
    /// </summary>
    [Serializable]
    public class ErrorMsgExpr : ErrorExpr {
        public string Msg { get; private set; }
        public ErrorMsgExpr(string msg) {
            Msg = msg;
        }
        public override bool Equals(object obj) { // should this always return false?
            if(obj.GetType() != this.GetType()) return false;
            ErrorMsgExpr eme = (ErrorMsgExpr)obj;
            return eme.Msg == Msg;
        }
        public override int GetHashCode() {
            return Msg.GetHashCode();
        }
        public override Expr Clone() {
            return new ErrorMsgExpr(Msg);
        }
    }

    [Serializable]
    public class CompositeExpr : Expr {
		private Expr _head;
		public Expr Head { get { return _head; } set { _head = value; } }
		private Expr[] _args;
		public Expr[] Args { get { return _args; } set { _args = value; } }

        public CompositeExpr(Expr head, params Expr[] args) { _head = head; _args = args; }
        public CompositeExpr(Expr head, Hashtable annotations, params Expr[] args) { _head = head; Annotations = annotations; _args = args; }
        
		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			CompositeExpr ce = (CompositeExpr)obj;
			if(!_head.Equals(ce._head)) return false;
			if(_args.Length != ce._args.Length) return false;
			for(int i = 0; i < _args.Length; i++) {
				if(!_args[i].Equals(ce._args[i])) return false;
			}
			return true;
		}
		public override int GetHashCode() {
			int hash = _head.GetHashCode();
			hash ^= _args.Length;
			for(int i = 0; i < _args.Length; i++) {
				hash ^= _args[i].GetHashCode();
			}
			return hash;
		}

        public override Expr Clone() {
            Expr[] args = new Expr[_args.Length];
            for (int i = 0; i < _args.Length; i++) args[i] = _args[i].Clone();
            CompositeExpr ce = new CompositeExpr(_head.Clone(), args);
            ce.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return ce;
        }

        public override string ToString() {
           if (Converter == null)
               return base.ToString();
           return Converter(this);
        }
	}
    static public class CompositeExprExtensions {
        static public Expr   Head(this Expr e) { return e is CompositeExpr ? (e as CompositeExpr).Head : null; }
        static public Expr[] Args(this Expr e) { return e is CompositeExpr ? (e as CompositeExpr).Args : null; }
    }

    [Serializable]
    public class OneArgExpr  {
        CompositeExpr _oneArg;
        public OneArgExpr(Expr head) { _oneArg = (head != null && head is CompositeExpr && (head as CompositeExpr).Args.Length == 1) ? head as CompositeExpr : null; }
        public bool    OK  { get { return _oneArg != null; } }
        public Expr    Arg { get { return _oneArg.Args[0]; } }
        public bool    Int  { get { return Arg is IntegerNumber; } }
        public BigInt IArg { get { return (Arg as IntegerNumber).Num; } }
    }
    [Serializable]
    public class TwoArgExpr {
        CompositeExpr _twoArg;
        public TwoArgExpr(Expr head) { _twoArg = (head != null && head is CompositeExpr && (head as CompositeExpr).Args.Length == 2) ? head as CompositeExpr : null; }
        public bool OK { get { return _twoArg != null && Int1 && Int2;  } }
        public Expr Arg1 { get { return _twoArg.Args[0]; } }
        public Expr Arg2 { get { return _twoArg.Args[1]; } }
        public bool Int1 { get { return Arg1 is IntegerNumber; } }
        public bool Int2 { get { return Arg2 is IntegerNumber; } }
        public BigInt IArg1 { get { return (Arg1 as IntegerNumber).Num; } }
        public BigInt IArg2 { get { return (Arg2 as IntegerNumber).Num; } }
    }
    [Serializable]
    public class PowerExpr : TwoArgExpr {
        public PowerExpr(Expr pow) : base(pow is CompositeExpr && (pow as CompositeExpr).Head == WellKnownSym.power ? pow : null) { }
        public Expr   Base   { get { return Arg1; } }
        public bool   BaseInt { get { return Int1; } }
        public BigInt IBase  { get { return IArg1; } }
        public bool   PowerInt { get { return Int1; } }
        public Expr   Power  { get { return Arg2; } }
        public BigInt IPower { get { return IArg2; } }
    }
    [Serializable]
    public class DivideExpr : OneArgExpr {
        public DivideExpr(Expr divisor) : base(divisor is CompositeExpr && (divisor as CompositeExpr).Head == WellKnownSym.divide ? divisor : null) { }
        public Expr   Divisor  { get { return Arg; } }
        public bool   DivisorInt {get { return Int; } }
        public BigInt IDivisor { get { return IArg; } }
    }
    [Serializable]
    public class MinusExpr : OneArgExpr {
        public MinusExpr(Expr divisor) : base(divisor is CompositeExpr && (divisor as CompositeExpr).Head == WellKnownSym.minus ? divisor : null) { }
        public Expr   Term     { get { return Arg; } }
        public bool   TermInt  { get { return Int; } }
        public BigInt ITerm    { get { return IArg; } }
    }

    [Serializable]
    public abstract class Sym : Expr {
    }
    [Serializable]
	public abstract class Number : Expr {
    }
    [Serializable]
	public abstract class RealNumber : Number {
    }
	/// <summary>
	/// This is basically to hold doubles. Subclasses might use floats instead of doubles, or
	/// even other things.
    /// </summary>
    [Serializable]
	public abstract class MachinePrecisionNumber : RealNumber {
        public abstract double Num { get; set; } // SIGH: would have liked to not include "set" here, but then descendants couldn't add it...feel free to throw an UnimplementedException from it
    }
    [Serializable]
	public class DoubleNumber : MachinePrecisionNumber {
        private double _num;
        static public int DisplayPrecision = 12;
		public override double Num { get { return _num; } set { _num = value; } }
		public DoubleNumber(double num) { _num = num; }
		public override Expr Clone() { DoubleNumber ds = new DoubleNumber(_num); 
            ds.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return ds;
        }
		public override bool Equals(object obj) {
			if(obj.GetType() != this.GetType()) return false;
			DoubleNumber dn = (DoubleNumber)obj;
			return _num == dn._num;
		}
		public override int GetHashCode() {
			return _num.GetHashCode();
		}

        public static implicit operator DoubleNumber(double num) { return new DoubleNumber(num); }

        public override string ToString() { return "{" + GetType().Name + " " + _num + "}"; }
    }
	/// <summary>
	/// This is to hold bignums, not machine-precision ints.
    /// </summary>
    [Serializable]
	public class IntegerNumber : RealNumber {
		private BigInt _num;
		public BigInt Num { get { return _num; } set { _num = value; } }
        public IntegerNumber(BigInt num) { _num = num; }
        public IntegerNumber(string digits) { _num = new BigInt(digits); }
        public override Expr Clone() { 
            IntegerNumber id =new IntegerNumber(_num); 
            id.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return id;
        }
		public override bool Equals(object obj) {
			if(obj.GetType() != this.GetType()) return false;
			IntegerNumber intn = (IntegerNumber)obj;
			return _num.Equals(intn._num);
		}
		public override int GetHashCode() {
			return _num.GetHashCode();
		}

		private static BigInt _zero = 0;
		private static BigInt _one = 1;
		public static IntegerNumber Zero { get { return new IntegerNumber(_zero); } }
		public static IntegerNumber One { get { return new IntegerNumber(_one); } }

        public static implicit operator IntegerNumber(BigInt num) { return new IntegerNumber(num); }
        public static implicit operator IntegerNumber(long num) { return new IntegerNumber(num); }

        public override string ToString() { return _num.CanBeInt() ? "{" + GetType().Name + " " + _num + "}" : base.ToString(); }
    }
	/// <summary>
	/// This is to hold quotients of bignums, not machine-precision ints.
    /// </summary>
    [Serializable]
	public class RationalNumber : RealNumber {
		private BigRat _num;
		public BigRat Num { get { return _num; } set { _num = value; } }
		public RationalNumber(BigRat num) { _num = num; }
        public override Expr Clone() {
            RationalNumber rs = new RationalNumber(_num);
            rs.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return rs;
        }
		public override bool Equals(object obj) {
			if(obj.GetType() != this.GetType()) return false;
			RationalNumber rn = (RationalNumber)obj;
			return _num.Equals(rn._num);
		}
		public override int GetHashCode() {
			return _num.GetHashCode();
		}

		private static BigRat _zero = 0;
		private static BigRat _one = 1;
		public static RationalNumber Zero { get { return new RationalNumber(_zero); } }
		public static RationalNumber One { get { return new RationalNumber(_one); } }

        public static implicit operator RationalNumber(BigRat num) { return new RationalNumber(num); }

        public override string ToString() { return _num.Num.CanBeInt() && _num.Denom.CanBeInt() ? "{" + GetType().Name + " " + _num + "}" : base.ToString(); }
    }
	/// <summary>
	/// This is to hold variable-precision floating-point numbers, not machine-precision floats
	/// or doubles.
    /// </summary>
    [Serializable]
	public abstract class FloatingPointNumber : RealNumber {
	}
	/// <summary>
	/// Some math engines won't support Re and Im being different kinds of real number, so keep them the same.
    /// </summary>
    [Serializable]
	public class ComplexNumber : Number {
		private RealNumber _re, _im;
		public RealNumber Re { get { return _re; } set { _re = value; } }
		public RealNumber Im { get { return _im; } set { _im = value; } }
        public ComplexNumber(RealNumber re, RealNumber im) { _re = re; _im = im; }
        /* next two constructors are just here so you can use autoconversions */
        public ComplexNumber(DoubleNumber re, DoubleNumber im) { _re = re; _im = im; }
        public ComplexNumber(IntegerNumber re, IntegerNumber im) { _re = re; _im = im; }
        public override Expr Clone() {
            ComplexNumber cn = new ComplexNumber(_re, _im);
            cn.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return cn;
        }
		public override bool Equals(object obj) {
			if(obj.GetType() != this.GetType()) return false;
			ComplexNumber dn = (ComplexNumber)obj;
			return _re == dn._re && _im == dn._im;
		}
		public override int GetHashCode() {
			return _re.GetHashCode() ^ _im.GetHashCode();
		}

        public static implicit operator ComplexNumber(double r) { return new ComplexNumber(r, 0.0); }
        public static implicit operator ComplexNumber(long r) { return new ComplexNumber(r, (IntegerNumber)0); }
    }
    [Serializable]
    public class ArrayExpr : Expr, IEnumerable<Expr> {
		/// <summary>
		/// This is an array of Expr.
		/// </summary>
		private Array _elts;
		public Array Elts { get { return _elts; } set { _elts = value; } }
        /// <summary>
        /// It's strange that the Array class doesn't provide this on its own.
        /// </summary>
		public int[] Dims {
			get {
				int[] d = new int[_elts.Rank];
				for(int i = 0; i < _elts.Rank; i++) d[i] = _elts.GetLength(i);
				return d;
			}
		}
        /// <summary>
        /// ***NOTE***: although c# internal indexing into the array is standard c# 0-based, indices used as arguments to WKS index
        /// are 1-based, for easier matching with Mathematica. Thus, don't forget to convert!
        /// </summary>
        /// <param name="indices">These are 0-based although Expr code itself is 1-based.</param>
		public Expr this[params int[] indices] {
			get {
				return (Expr)_elts.GetValue(indices);
			}
			set {
				_elts.SetValue(value, indices);
			}
		}

		public ArrayExpr(Array elts) { _elts = elts; }
        
		private bool bumpindexer(int[] indexer, int[] dims) {
			return bumpforward(indexer, dims, 0);
		}
		private bool bumpforward(int[] indexer, int[] dims, int ix) {
			if(ix == dims.Length) return false;
			indexer[ix] = (indexer[ix] + 1)%dims[ix];
			if(indexer[ix] == 0) return bumpforward(indexer, dims, ix+1);
			return true;
		}
        public IEnumerable<int[]> Indices {
            get {
                int[] dims = Dims;
                int[] indexer = new int[_elts.Rank]; // relying on default value for int being 0
                do {
                    yield return indexer;
                } while(bumpindexer(indexer, dims));
            }
        }
		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			ArrayExpr ae = (ArrayExpr)obj;
			if(_elts.Rank != ae._elts.Rank) return false;
			int[] mydims = Dims;
			int[] odims = ae.Dims;
			int i;
			for(i = 0; i < _elts.Rank; i++) if(mydims[i] != odims[i]) return false;
            foreach(int[] indexer in Indices) if(this[indexer] != ae[indexer]) return false;
			return true;
		}
		public override int GetHashCode() {
			int hash = 0;
            foreach(int[] ix in Indices) hash ^= this[ix].GetHashCode();
			return hash;
		}
		public override Expr Clone() {
            Array a = (Array) Elts.Clone();
            ArrayExpr ae = new ArrayExpr(a);
            foreach (int[] ix in ae.Indices) ae[ix] = ae[ix].Clone();
            ae.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return ae;
		}

        public IEnumerator<Expr> GetEnumerator() {
            if(Elts.Rank == 1) {
                foreach(Expr e in Elts) yield return e;
            } else {
                foreach(int[] ix in Indices) yield return this[ix];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    [Serializable]
    public class LetterSym : Sym {
        private char _letter;
        public char Letter { get { return _letter; } set { _letter = value; } }
        private AccentKind _accent;
        public AccentKind Accent { get { return _accent; } set { _accent = value; } }
        private Expr _subscript;
        public Expr Subscript { get { return _subscript; } set { _subscript = value; } }
        private Format _format;
        public Format Format { get { return _format; } set { _format = value; } }
        private object _tag = null;
        /// <summary>
        /// Tags must be equal for LetterSyms to be equal, and are copied in Clone(), but are otherwise unused by Expr
        /// </summary>
        public object Tag { get { return _tag; } set { _tag = value; } }

        public LetterSym(char letter) : this(letter, new NoAccent(), new NullExpr(), Format.Normal, null) { }
        public LetterSym(char letter, Expr subscript) : this(letter, new NoAccent(), subscript, Format.Normal, null) { }
        public LetterSym(char letter, AccentKind accent, Expr subscript, Format format, object tag) {
            _letter = letter; _accent = accent; _subscript = subscript; _format = format; _tag = tag;
		}

		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			LetterSym ls = (LetterSym)obj;
			return _letter.Equals(ls._letter) && _accent.Equals(ls._accent) && _subscript.Equals(ls._subscript) && _format.Equals(ls._format) && ((_tag == null && ls._tag == null) || _tag.Equals(ls._tag));
		}
		public override int GetHashCode() {
			return _letter.GetHashCode() ^ _accent.GetHashCode() ^ _subscript.GetHashCode() ^ _format.GetHashCode() ^ (_tag == null ? 0 : _tag.GetHashCode());
		}
		public override Expr Clone() {
			LetterSym ls = new LetterSym(_letter, _accent, _subscript.Clone(), _format, _tag);
            ls.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return ls;
		}

        public override string ToString() {
            return "{" + GetType().Name + " '" + Letter + "'"
                + (_accent is NoAccent ? "" : " acc " + _accent)
                + (_subscript is NullExpr ? "" : " sub " + _subscript)
                + (_format == Format.Normal ? "" : " fmt " + _format)
                + (_tag == null ? "" : " tag " + _tag)
            + "}";
        }
    }
	/// <summary>
	/// This is for things like [angle]ABC. The individual elements take accents and such to represent [angle]A'BA, and the group takes an accent
	/// to represent line A-B as AB with a bar over it.
    /// </summary>
    [Serializable]
    public class GroupedLetterSym : Sym {
        private LetterSym[] _letters;
        public LetterSym[] Letters { get { return _letters; } set { _letters = value; } }
        private AccentKind _accent;
        public AccentKind Accent { get { return _accent; } set { _accent = value; } }

		public GroupedLetterSym(params LetterSym[] letters) { _letters = letters; _accent = new NoAccent(); }
		public GroupedLetterSym(LetterSym[] letters, AccentKind accent) { _letters = letters; _accent = accent; }

		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			GroupedLetterSym gls = (GroupedLetterSym)obj;
			if(_letters.Length != gls._letters.Length) return false;
			for(int i = 0; i < _letters.Length; i++) if(!_letters[i].Equals(gls._letters[i])) return false;
			if(!_accent.Equals(gls._accent)) return false;
			return true;
		}
		public override int GetHashCode() {
			int hash = _letters.Length;
			for(int i = 0; i < _letters.Length; i++) hash ^= _letters[i].GetHashCode();
			hash ^= _accent.GetHashCode();
			return 0;
		}
		public override Expr Clone() {
			LetterSym[] letters = new LetterSym[_letters.Length];
            for (int i = 0; i < _letters.Length; i++) letters[i] = (LetterSym)_letters[i].Clone();
            GroupedLetterSym gs = new GroupedLetterSym(letters, _accent);
            gs.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return gs;
		}
    }
	/// <summary>
	/// This is to represent variables named with whole words rather than just single letters. It's different from GroupedLetterSym
	/// because it's not composed of symbols which can be meaningfully treated individually.
	/// But if the user writes a well-known function name such as "sin", that should be made a WellKnownSym.
    /// </summary>
    [Serializable]
	public class WordSym : Sym {
		private string _word;
		public string Word { get { return _word; } set { _word = value; } }
		private AccentKind _accent;
		public AccentKind Accent { get { return _accent; } set { _accent = value; } }
		private Expr _subscript;
		public Expr Subscript { get { return _subscript; } set { _subscript = value; } }
		private Format _format;
		public Format Format { get { return _format; } set { _format = value; } }
        private object _tag = null;
        /// <summary>
        /// Tags must be equal for WordSyms to be equal, and are copied in Clone(), but are otherwise unused by Expr
        /// </summary>
        public object Tag { get { return _tag; } set { _tag = value; } }

        public WordSym(string word) : this(word, new NoAccent(), new NullExpr(), Format.Normal, null) { }
		public WordSym(string word, AccentKind accent, Expr subscript, Format format, object tag) {
			_word = word; _accent = accent; _subscript = subscript; _format = format; _tag = Tag;
		}

		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			WordSym ws = (WordSym)obj;
            return _word != null && ws._word != null && _word.Equals(ws._word) && _accent.Equals(ws._accent) && _subscript.Equals(ws._subscript) && _format.Equals(ws._format) && ((_tag == null && ws._tag == null) || _tag.Equals(ws._tag));
		}
		public override int GetHashCode() {
            return _word == null ? 0 : _word.GetHashCode() ^ _accent.GetHashCode() ^ _subscript.GetHashCode() ^ _format.GetHashCode() ^ (_tag == null ? 0 : _tag.GetHashCode());
		}
		public override Expr Clone() {
            WordSym ws = new WordSym(_word, _accent, _subscript.Clone(), _format, _tag);
            ws.Annotations = (Hashtable)Annotations.Clone(); // bcz: I added this
            return ws;
		}

        public override string ToString() {
            if (Annotations.ContainsKey("literal"))
                return _word;
            return "{" + GetType().Name + " \"" + _word + "\""
                + (_accent is NoAccent ? "" : " acc " + _accent)
                + (_subscript is NullExpr ? "" : " sub " + _subscript)
                + (_format == Format.Normal ? "" : " fmt " + _format)
                + (_tag == null ? "" : " tag " + _tag)
            + "}";
        }
    }
    [Serializable]
	public abstract class AccentKind {
	}
    [Serializable]
	public sealed class NoAccent : AccentKind {
		public NoAccent() {}
		public override bool Equals(object obj) {
			return obj.GetType() == this.GetType();
		}
		public override int GetHashCode() {
			return 0;
		}
	}
    // These follow the order given in the Mathematical Alphanumeric Symbols section of unicode, starting at 1D400.
    // Not all of these will be available for display, let alone all of these for all characters!
    [Serializable]
	public enum Format {
        /// <summary>
        /// Standard math formatting: uses Italic for A-Za-z and Roman for everything else.
        /// </summary>
        Normal,
        Roman, Bold, Italic, BoldItalic, Script, BoldScript, Fractur, Doublestruck, BoldFractur,
        SansSerif, BoldSansSerif, ItalicSansSerif, BoldItalicSansSerif,
        Monospace, 
	}
	/// <summary>
	/// This list is only for symbols which are (or need to be) known by the underlying engine. Things like the symbols
	/// for angles or triangles to make composite symbols with shouldn't go here.
	/// </summary>
    [Serializable]
	public enum WKSID {
		i, e, pi, infinity,
        naturals, integers, rationals, reals, complexes,
		del,
		re, im, arg, magnitude, /* use magnitude also for vectors and for matrix determinants */
		ln, log, /* log takes base then value to take the logarithm of; or base 10 if only one argument */
		limit, integral, summation, differentiald, partiald, derivative, /* needs further thought */
        root, /* takes root number (eg 2 for square root), value to take the root of */
        sin, cos, tan, sec, csc, cot, arcsin, arccos, arctan, asec, acsc, acot,
		sinh, cosh, tanh, sech, csch, coth, asinh, acosh, atanh, asech, acsch, acoth,
		plus, minus, plusminus, minusplus, times, divide, mod, power, factorial,/* minus and divide are unary operators only */
		assignment, definition,
		equals, greaterthan, lessthan, greaterequals, lessequals, notequals,
        True, False,
		lognot, logand, logor,
        floor, ceiling,
		dot, cross,  
		index, /* takes thing to be indexed then a one-dimensional array of however many indices -- ***NOTE*** that the indices here are 1-based even though indices from c# into the array are 0-based. */
        subscript, /* takes object and then its subscript */
        elementof, subsetof, supersetof, subseteq, superseteq, setexplicit, setdef,
        sum, avg, // ez: spreadsheet functions
        none
    }
    [Serializable]
	public class WellKnownSym : Sym {
		private WKSID _id;
        public WKSID ID { get { return _id; } }

		public WellKnownSym(WKSID id) { _id = id; }

		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			WellKnownSym wks = (WellKnownSym)obj;
			return _id.Equals(wks._id);
		}
		public override int GetHashCode() {
			return _id.GetHashCode();
		}
		public override Expr Clone() { return new WellKnownSym(_id); }

        public override string ToString() {
            return "{WellKnownSym: " + _id.ToString() + "}";
        }


		/* Gack, this is a pain. I sure wish there was a way to get abbreviated references to the WKSs without going through all this,
		 * but implicit conversions don't work if, say, you tried to pass WKSID.pi in as an argument to something expecting an
		 * Expr.
		 */

        public static WellKnownSym i { get { return new WellKnownSym(WKSID.i); } }
        public static WellKnownSym e { get { return new WellKnownSym(WKSID.e); } }
		public static WellKnownSym pi { get { return new WellKnownSym(WKSID.pi); } }
        public static WellKnownSym infinity { get { return new WellKnownSym(WKSID.infinity); } }
        public static WellKnownSym naturals { get { return new WellKnownSym(WKSID.naturals); } }
        public static WellKnownSym integers { get { return new WellKnownSym(WKSID.integers); } }
        public static WellKnownSym rationals { get { return new WellKnownSym(WKSID.rationals); } }
        public static WellKnownSym reals { get { return new WellKnownSym(WKSID.reals); } }
        public static WellKnownSym complexes { get { return new WellKnownSym(WKSID.complexes); } }
        public static WellKnownSym del { get { return new WellKnownSym(WKSID.del); } }
		public static WellKnownSym re { get { return new WellKnownSym(WKSID.re); } }
		public static WellKnownSym im { get { return new WellKnownSym(WKSID.im); } }
		public static WellKnownSym arg { get { return new WellKnownSym(WKSID.arg); } }
		public static WellKnownSym magnitude { get { return new WellKnownSym(WKSID.magnitude); } }
		public static WellKnownSym ln { get { return new WellKnownSym(WKSID.ln); } }
		public static WellKnownSym log { get { return new WellKnownSym(WKSID.log); } }
        public static WellKnownSym limit { get { return new WellKnownSym(WKSID.limit); } }
        public static WellKnownSym integral { get { return new WellKnownSym(WKSID.integral); } }
        public static WellKnownSym summation { get { return new WellKnownSym(WKSID.summation); } }
        public static WellKnownSym differentiald { get { return new WellKnownSym(WKSID.differentiald); } }
        public static WellKnownSym derivative { get { return new WellKnownSym(WKSID.derivative); } }
		public static WellKnownSym partiald { get { return new WellKnownSym(WKSID.partiald); } }
		public static WellKnownSym root { get { return new WellKnownSym(WKSID.root); } }
		public static WellKnownSym sin { get { return new WellKnownSym(WKSID.sin); } }
		public static WellKnownSym cos { get { return new WellKnownSym(WKSID.cos); } }
		public static WellKnownSym tan { get { return new WellKnownSym(WKSID.tan); } }
		public static WellKnownSym sec { get { return new WellKnownSym(WKSID.sec); } }
		public static WellKnownSym csc { get { return new WellKnownSym(WKSID.csc); } }
        public static WellKnownSym cot { get { return new WellKnownSym(WKSID.cot); } }
        public static WellKnownSym arcsin { get { return new WellKnownSym(WKSID.arcsin); } }
        public static WellKnownSym arccos { get { return new WellKnownSym(WKSID.arccos); } }
        public static WellKnownSym arctan { get { return new WellKnownSym(WKSID.arctan); } }
		public static WellKnownSym asec { get { return new WellKnownSym(WKSID.asec); } }
		public static WellKnownSym acsc { get { return new WellKnownSym(WKSID.acsc); } }
		public static WellKnownSym acot { get { return new WellKnownSym(WKSID.acot); } }
		public static WellKnownSym sinh { get { return new WellKnownSym(WKSID.sinh); } }
		public static WellKnownSym cosh { get { return new WellKnownSym(WKSID.cosh); } }
		public static WellKnownSym tanh { get { return new WellKnownSym(WKSID.tanh); } }
		public static WellKnownSym sech { get { return new WellKnownSym(WKSID.sech); } }
		public static WellKnownSym csch { get { return new WellKnownSym(WKSID.csch); } }
		public static WellKnownSym coth { get { return new WellKnownSym(WKSID.coth); } }
		public static WellKnownSym asinh { get { return new WellKnownSym(WKSID.asinh); } }
		public static WellKnownSym acosh { get { return new WellKnownSym(WKSID.acosh); } }
		public static WellKnownSym atanh { get { return new WellKnownSym(WKSID.atanh); } }
		public static WellKnownSym asech { get { return new WellKnownSym(WKSID.asech); } }
		public static WellKnownSym acsch { get { return new WellKnownSym(WKSID.acsch); } }
		public static WellKnownSym acoth { get { return new WellKnownSym(WKSID.acoth); } }
		public static WellKnownSym plus { get { return new WellKnownSym(WKSID.plus); } }
		public static WellKnownSym minus { get { return new WellKnownSym(WKSID.minus); } }
        public static WellKnownSym plusminus { get { return new WellKnownSym(WKSID.plusminus); } }
        public static WellKnownSym minusplus { get { return new WellKnownSym(WKSID.minusplus); } }
        public static WellKnownSym times { get { return new WellKnownSym(WKSID.times); } }
        public static WellKnownSym divide { get { return new WellKnownSym(WKSID.divide); } }
        public static WellKnownSym mod { get { return new WellKnownSym(WKSID.mod); } }
        public static WellKnownSym power { get { return new WellKnownSym(WKSID.power); } }
        public static WellKnownSym factorial { get { return new WellKnownSym(WKSID.factorial); } }
        public static WellKnownSym assignment { get { return new WellKnownSym(WKSID.assignment); } }
        public static WellKnownSym definition { get { return new WellKnownSym(WKSID.definition); } }
		public static WellKnownSym equals { get { return new WellKnownSym(WKSID.equals); } }
		public static WellKnownSym greaterthan { get { return new WellKnownSym(WKSID.greaterthan); } }
		public static WellKnownSym lessthan { get { return new WellKnownSym(WKSID.lessthan); } }
		public static WellKnownSym greaterequals { get { return new WellKnownSym(WKSID.greaterequals); } }
        public static WellKnownSym lessequals { get { return new WellKnownSym(WKSID.lessequals); } }
        public static WellKnownSym notequals { get { return new WellKnownSym(WKSID.notequals); } }
        public static WellKnownSym True { get { return new WellKnownSym(WKSID.True); } }
        public static WellKnownSym False { get { return new WellKnownSym(WKSID.False); } }
        public static WellKnownSym lognot { get { return new WellKnownSym(WKSID.lognot); } }
		public static WellKnownSym logand { get { return new WellKnownSym(WKSID.logand); } }
        public static WellKnownSym logor { get { return new WellKnownSym(WKSID.logor); } }
        public static WellKnownSym floor { get { return new WellKnownSym(WKSID.floor); } }
        public static WellKnownSym ceiling { get { return new WellKnownSym(WKSID.ceiling); } }
        public static WellKnownSym dot { get { return new WellKnownSym(WKSID.dot); } }
		public static WellKnownSym cross { get { return new WellKnownSym(WKSID.cross); } }
        public static WellKnownSym index { get { return new WellKnownSym(WKSID.index); } }
        public static WellKnownSym subscript { get { return new WellKnownSym(WKSID.subscript); } }
        public static WellKnownSym elementof { get { return new WellKnownSym(WKSID.elementof); } }
        public static WellKnownSym subsetof { get { return new WellKnownSym(WKSID.subsetof); } }
        public static WellKnownSym supersetof { get { return new WellKnownSym(WKSID.supersetof); } }
        public static WellKnownSym subseteq { get { return new WellKnownSym(WKSID.subseteq); } }
        public static WellKnownSym superseteq { get { return new WellKnownSym(WKSID.superseteq); } }
        public static WellKnownSym setexplicit { get { return new WellKnownSym(WKSID.setexplicit); } }
        public static WellKnownSym setdef { get { return new WellKnownSym(WKSID.setdef); } }
        public static WellKnownSym none { get { return new WellKnownSym(WKSID.none); } }
        public static WellKnownSym sum { get { return new WellKnownSym(WKSID.sum); } }
        public static WellKnownSym avg { get { return new WellKnownSym(WKSID.avg); } }
	}
    /// <summary>
	/// Only some Engines, converters, etc will support this.
    /// </summary>
    [Serializable]
	public class Pattern : Expr {
		private Sym _var;
		public Sym Var { get { return _var; } set { _var = value; } }

		public Pattern(Sym var) { _var = var; }
        
		public override bool Equals(Object obj) {
			if(obj.GetType() != this.GetType()) return false;
			Pattern p = (Pattern)obj;
			if(!_var.Equals(p._var)) return false;
			return true;
		}
		public override int GetHashCode() {
			int hash = _var.GetHashCode();
			return hash;
		}

		public override Expr Clone() {
			return new CompositeExpr(_var.Clone());
		}

		public override Hashtable Match(Expr literal) {
			return base.Match(literal);
		}

	}

	/* XXX open issues:
	 * * c# operators make new node (eg == but conflict with testing for
	 * equality, also conflict with things like sin: can't do Math.Sin so wd have to do Expr.Sin).
	 * *+ times, divide (etc?) may have different interps in diff engines for matrices & vectors
	 * *+ variances in, eg, interpretation (or not) of del, del^2, del bold vs del w arrow overhead vs del plain, del as grad, etc.
	 * * should cases ("{") be represented as special node type or as WKS which may need significant translation to generate back again
	 * when reading back from the engine?
	 * */
	/* rfc1019 may be of a little interest */
}
