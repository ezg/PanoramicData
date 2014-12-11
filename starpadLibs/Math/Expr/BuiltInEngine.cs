using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using FSBigInt = System.Numerics.BigInteger;

namespace starPadSDK.MathExpr {
    /// <summary>
    /// Summary description for BuiltInEngine.
    /// </summary>
    public class BuiltInEngine : Engine {
        public BuiltInEngine() {
        }

        private string[] simplifyrules = new string[] {
			"sin[asin[x_]]", "x_",
			"cos[acos[x_]]", "x_",
			"tan[atan[x_]]", "x_",
		};
        int? ExprToInd(Expr e) {
            /* Expr arrays are 1-based to match Mathematica easier, so convert to 0-based c# arrays here. */
            if(e is IntegerNumber)
                return (int)((e as IntegerNumber).Num) - 1;
            else if(e is DoubleNumber) // FIXME -- shouldn't need to understand noninteger array indices
                return (int)((e as DoubleNumber).Num + 0.5) - 1;
            else return null;
        }
        public Expr Reformat(Expr e) {
            if (e.Annotations.Contains("Factor"))
                return e;
            if(e is DoubleNumber) {
                double truncated = Math.Truncate((e as DoubleNumber).Num);
                double decimals = Math.Round(((e as DoubleNumber).Num - truncated) * Math.Pow(10, 15)) / Math.Pow(10, 15);
                return truncated + decimals;
            }
            if(e is ArrayExpr) {
                Array a = (Array)((ArrayExpr)e).Elts.Clone();
                ArrayExpr ae = new ArrayExpr(a);
                foreach(int[] ix in ae.Indices) ae[ix] = Reformat(ae[ix]);
                if(!e.Annotations.Contains("Force Parentheses") || !e.Annotations["Force Parentheses"].Equals(0)) ae.Annotations["Force Parentheses"] = 1;
                return ae;
            }
            if(e is CompositeExpr) {
                List<Expr> args = new List<Expr>();
                foreach(Expr a in Args(e))
                    args.Add(Reformat(a));
                if(head(e) == WKSID.power && IsNum(args[1])) {
                    Expr n = Num(args[1]);
                    if(n is IntegerNumber && (int)n < 0) {
                        if((int)n == -1)
                            return Divide(args[0]);
                        return Divide(Power(args[0], -(int)n));
                    }
                }
                if(head(e) == WKSID.power) {
                    if(Ag(e, 1) is CompositeExpr && head(Ag(e, 1)) == WKSID.divide && Ag(Ag(e, 1), 0) is IntegerNumber &&
                        (Ag(Ag(e, 1), 0) as IntegerNumber).Num == 2)
                        return new CompositeExpr(WellKnownSym.root, 2, Reformat(Ag(e, 0)));
                    if(head(Ag(e, 1)) == WKSID.divide && Ag(Ag(e, 1), 0) is IntegerNumber &&
                        (Ag(Ag(e, 1), 0) as IntegerNumber).Num < 0)
                        if((Ag(Ag(e, 1), 0) as IntegerNumber).Num == -2)
                            return new CompositeExpr(WellKnownSym.root, 2, Reformat(Divide(Ag(e, 0))));
                        else return Divide(Power(Ag(e, 0), new IntegerNumber(-(Ag(Ag(e, 1), 0) as IntegerNumber).Num)));
                } else if(head(e) == WKSID.plus) {
                    bool reorder = false;
                    List<Expr> negs = new List<Expr>();
                    List<Expr> pluses = new List<Expr>();
                    foreach(Expr t in args) {
                        if(head(t) == WKSID.minus || head(t) == WKSID.plusminus || ((t is DoubleNumber && (double)t < 0) || (t is IntegerNumber && (int)t < 0)))
                            negs.Add(t);
                        else if(head(t) == WKSID.times && head(Ag(t, 0)) == WKSID.minus) {
                            Expr[] remainder = new Expr[(t as CompositeExpr).Args.Length];
                            for(int i = 1; i < (t as CompositeExpr).Args.Length; i++)
                                remainder[i] = (t as CompositeExpr).Args[i];
                            remainder[0] = Ag(Ag(t, 0), 0);
                            negs.Add(Minus(Mult(remainder)));
                            reorder = true;
                        } else if(head(t) == WKSID.times && (Ag(t, 0) is IntegerNumber && (int)Ag(t, 0) < 0)) {
                            Expr[] remainder = new Expr[(t as CompositeExpr).Args.Length];
                            for(int i = 0; i < (t as CompositeExpr).Args.Length; i++)
                                remainder[i] = (t as CompositeExpr).Args[i];
                            remainder[0] = -(int)(Ag(t, 0) as IntegerNumber).Num;
                            negs.Add(Minus(Mult(remainder)));
                            reorder = true;
                        } else {
                            pluses.Add(t);
                            if(negs.Count > 0)
                                reorder = true;
                        }
                    }
                    if(reorder) {
                        foreach(Expr t in negs)
                            pluses.Add(t);
                        return Reformat(new CompositeExpr(WellKnownSym.plus, pluses.ToArray()));
                    }
                } else if(head(e) == WKSID.times) {
                    bool reorder = false;
                    List<Expr> divs = new List<Expr>();
                    List<Expr> ndivs = new List<Expr>();
                    List<Expr> nargs = new List<Expr>();
                    foreach(Expr t in args)
                        if(head(t) == WKSID.times)
                            foreach(Expr tt in Args(t))
                                nargs.Add(tt);
                        else nargs.Add(t);
                    foreach(Expr t in nargs) {
                        if(head(t) == WKSID.divide || (head(t) == WKSID.minus && head(Ag(t, 0)) == WKSID.divide))
                            divs.Add(t);
                        else {
                            ndivs.Add(t);
                            if(divs.Count > 0)
                                reorder = true;
                        }
                    }
                    if(divs.Count > 1 || reorder) {
                        if(divs.Count > 1) {
                            for(int i = 0; i < divs.Count; i++)
                                divs[i] = Ag(divs[i], 0);
                            ndivs.Add(new CompositeExpr(WellKnownSym.divide, new CompositeExpr(WellKnownSym.times, divs.ToArray())));
                        } else ndivs.Add(divs[0]);
                        return Reformat(new CompositeExpr(WellKnownSym.times, ndivs.ToArray()));
                    }
                }
                if(head(e) == WKSID.divide && args[0] is IntegerNumber && (int)args[0] < 0) {
                    return Mult(-1, Divide(-(int)args[0]));
                }
                if (head(e) == WKSID.times && args[0] is IntegerNumber && (int)args[0] == 1) {
                    List<Expr> terms = new List<Expr>();
                    terms.Add(args[1]);
                    for (int i = 2; i < args.Count; i++)
                        terms.Add(args[i]);
                    if (terms.Count == 1)
                        return terms[0];
                    else return Reformat(new CompositeExpr(WellKnownSym.times, terms.ToArray()));
                }
                if(head(e) == WKSID.times && args[0] is IntegerNumber && (int)args[0] == -1) {
                    args.RemoveAt(0);
                    return Reformat(new CompositeExpr(WellKnownSym.minus, args.Count < 2 ? args[0] : new CompositeExpr(WellKnownSym.times, args.ToArray())));
                }
                return new CompositeExpr((e as CompositeExpr).Head.Clone(), args.ToArray());
            }
            return e;
        }
        public override Expr _Simplify(Expr e) {
            Expr ret = Canonicalize(e);
            return Reformat(ret);
            /* simplifyrules. integer evaluation. trig of special expressions. half/double angle formulas; formulas with pi etc. */
            /* sin2+cos2 ? in middle of addition of other things? etc? */
#if false // Grr, this code was for the magnitude case of the CompositeExpr switch for Numericize--but numericize does only doubles: this should become part of simplify instead.
			Trace.Assert(args.Length == 1);
			if(args[0] is RealNumber) {
				if(args[0] is DoubleNumber) return new DoubleNumber(Math.Abs(((DoubleNumber)args[0]).Num));
				else if(args[0] is IntegerNumber) return new IntegerNumber(((IntegerNumber)args[0]).Num.abs());
				else if(args[0] is RationalNumber) return new RationalNumber(((RationalNumber)args[0]).Num.abs());
			} else if(args[0] is ComplexNumber) {
				ComplexNumber cn = (ComplexNumber)args[0];
				DoubleNumber red = cn.Re as DoubleNumber;
				DoubleNumber imd = cn.Im as DoubleNumber;
				if(red != null && imd != null) return new DoubleNumber(Math.Sqrt(red.Num*red.Num + imd.Num*imd.Num));
				IntegerNumber rei = cn.Re as IntegerNumber;
				IntegerNumber imi = cn.Im as IntegerNumber;
				IntegerNumber rer = cn.Re as RationalNumber;
				IntegerNumber imr = cn.Im as RationalNumber;
				BigRat re, im;
				bool gotem = false;
				if(rei != null && imi != null) { re = rei.Num; im = imi.Num; gotem = true; }
				if(rer != null && imr != null) { re = rer.Num; im = imr.Num; gotem = true; }
				if(gotem) {
					/* This is a hack. There *are* integer square root algorithms (thus working for larger values than doubles
								   represent). See http://www.embedded.com/98/9802fe2.htm, for instance. */
					BigRat ss = re*re + im*im;
					BigInt sqssnum = (long)Math.Round(Math.Sqrt(ss.Num.Num.doubleValue()));
					BigInt sqssdenom = (long)Math.Round(Math.Sqrt(ss.Denom.Num.doubleValue()));
					bool havenum, havedenom;
					havenum = (ss.Num == sqssnum*sqssnum);
					havedenom = (ss.Denom == sqssdenom*sqssdenom);
#if false // grr, wrote code here for what should be in simplify. Actually, this whole case of the switch can be simplified dramatically since we know we won't get any numbers but doubles (or machine precision) here
								if(havenum && havedenom) {
									BigRat val = new BigRat(sqssnum, sqssdenom);
									if(val.Denom == 1) return new IntegerNumber(val.Denom);
									else return new RationalNumber(val);
								} else if(!havenum && !havedenom) {
									return new CompositeExpr(WellKnownSym.root, new IntegerNumber(2), new RationalNumber(ss));
								} else {
									return new CompositeExpr(WellKnownSym.divide,
										havenum ? new IntegerNumber(sqssnum) : new CompositeExpr(WellKnownSym.root, new IntegerNumber(2), ss.Num),
										havedenom ? new IntegerNumber(sqssdenom) : new CompositeExpr(WellKnownSym.root, new IntegerNumber(2), ss.Denom));
								}
#endif
					if(havenum && havedenom) {
						BigRat val = new BigRat(sqssnum, sqssdenom);
						if(val.Denom == 1) return new IntegerNumber(val.Denom);
						else return new DoubleNumber(val.Num.Num.AsDouble()/val.Denom.Num.AsDouble());
					} else {
						/* Not as accurate as it could be if we have one as an integer (with an actual int sqrt function) but
									 * not the other */
						return new DoubleNumber(Math.Sqrt(ss.Num.Num.AsDouble()/ss.Denom.Num.AsDouble()));
					}
				}
			}
#endif
        }
        public override Expr _Approximate(Expr e) {
            Expr ret = Numericize(e);
            return Reformat(ret);
        }
        public override Expr _Substitute(Expr e, Expr orig, Expr replacement) {
            Substituter s = new Substituter(orig, replacement, false);
            return s.Substitute(e);
        }
        public override Expr _Substitute(Expr e, MathConstant[] consts) {
            return new Substituter(consts).Substitute(e);
        }
        public override Expr _Replace(Expr e, Expr orig, Expr replacement) {
            Substituter s = new Substituter(orig, replacement, true);
            return s.Substitute(e);
        }

        public override string Name { get { return "Built-in mathematics engine"; } }

        public override void Activate() {
            // TODO:  Add BuiltInEngine.Activate implementation
        }

        public override void Deactivate() {
            // TODO:  Add BuiltInEngine.Deactivate implementation
        }

        /* Canonicalization stuff */
        Expr          MaybeMult(params Expr[] terms) {
            if(terms.Length == 0) throw new ArgumentException();
            else if(terms.Length == 1) return terms[0];
            else return new CompositeExpr(WellKnownSym.times, terms);
        }
        CompositeExpr Mult(params Expr[] terms) { if (terms.Length < 2) throw new ArgumentException(); return new CompositeExpr(WellKnownSym.times, terms); }
        CompositeExpr Plus(params Expr[] terms) { return new CompositeExpr(WellKnownSym.plus, terms); }
        CompositeExpr Minus(Expr a) { return new CompositeExpr(WellKnownSym.minus, a); }
        CompositeExpr Divide(Expr a) { return new CompositeExpr(WellKnownSym.divide, a); }
        CompositeExpr Power(Expr a, Expr b) { return new CompositeExpr(WellKnownSym.power, a, b); }
        WKSID         head(Expr e) { return (e is CompositeExpr) && (e as CompositeExpr).Head is WellKnownSym ? ((e as CompositeExpr).Head as WellKnownSym).ID : WellKnownSym.none.ID; }
        Expr[]        Args(Expr e) { return (e is CompositeExpr) ? (e as CompositeExpr).Args : null; }
        Expr          Ag(Expr e, int n) { return Args(e)[n]; }
        bool          IsArrayIndex(Expr e, ref ArrayExpr ae, ref ArrayExpr ai) {
            if(Args(e).Length != 2) return false;
            if(!(Ag(e, 0) is ArrayExpr)) return false;
            if(!(Ag(e, 1) is ArrayExpr && ((ArrayExpr)Ag(e, 1)).Elts.Rank == 1)) return false;
            ae = (ArrayExpr)Ag(e, 0);
            ai = (ArrayExpr)Ag(e, 1);
            if(ai.Dims[0] != ae.Elts.Rank) return false;
            return true;
        }
        /// <summary>
        /// This converts from an Expr 1-dimensional array of indices (1-based) to a c# 1-dimensional array of indices (0-based),
        /// or returns null if not all the indices are known.
        /// </summary>
        /// <param name="ae"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        int[]         ConvertToInd(ArrayExpr ae, Converter<Expr, int?> converter) {
            Debug.Assert(ae.Elts.Rank == 1);
            int[] inds = new int[ae.Dims[0]];
            for(int i = 0; i < inds.Length; i++) {
                int? ix = converter(ae[i]);
                if(ix == null) return null;
                else inds[i] = ix.Value;
            }
            return inds;
        }
        public bool   IsNum(Expr e) {
            if(e == WellKnownSym.pi || e == WellKnownSym.e)
                return true;
            if(e is IntegerNumber || e is DoubleNumber)
                return true;
            switch(head(e)) {
                case WKSID.power: return IsNum(Ag(e, 0)) && IsNum(Ag(e, 1));
                case WKSID.divide:
                case WKSID.plusminus:
                case WKSID.minus: return IsNum(Ag(e, 0));
                case WKSID.times:
                case WKSID.mod:
                case WKSID.plus: foreach(Expr t in Args(e)) if(!IsNum(t)) return false;
                    return true;
                case WKSID.floor:
                case WKSID.ceiling:
                case WKSID.magnitude:
                case WKSID.factorial:
                case WKSID.arccos:
                case WKSID.arcsin:
                case WKSID.arctan:
                case WKSID.asec:
                case WKSID.acsc:
                case WKSID.acot:
                case WKSID.sec:
                case WKSID.csc:
                case WKSID.cot:
                case WKSID.tan:
                case WKSID.cos:
                case WKSID.sin: return IsNum(Ag(e, 0));
                case WKSID.index:
                    ArrayExpr ae = null, ai = null;
                    if(!IsArrayIndex(e, ref ae, ref ai)) return false;
                    int[] inds = ConvertToInd(ai, delegate(Expr ee) { return ExprToInd(_Approximate(ee)); });
                    if(inds == null) return false;
                    return IsNum(ae[inds]);
            }
            return false;
        }
        List<Expr>    flattenMults(CompositeExpr e) { // flatten out all multiplications
            List<Expr> termList = new List<Expr>();
            if(head(e) != WKSID.times)
                return null;
            foreach(Expr term in e.Args) {
                if(head(term) == WKSID.times) {
                    foreach(Expr t in Args(term))
                        termList.Add(t);
                } else if(head(term) == WKSID.power && (head(Ag(term, 0)) == WKSID.times)) {
                    foreach(Expr mterm in Args(Ag(term, 0)))
                        termList.Add(Power(mterm, Ag(term, 1)));
                } else if(head(term) == WKSID.divide) {
                    if(head(Ag(term, 0)) == WKSID.times) {
                        foreach(Expr t in Args(Ag(term, 0)))
                            termList.Add(Power(t, -1));
                    } else
                        termList.Add(Power(Ag(term, 0), -1));
                } else if(head(term) == WKSID.minus && head(Ag(term, 0)) == WKSID.times) {
                    bool first = true;
                    foreach(Expr t in Args(Ag(term, 0)))
                        if(first) {
                            first = false;
                            if(t is IntegerNumber)
                                termList.Add(-(t as IntegerNumber).Num);
                            else if(t is RationalNumber)
                                termList.Add(-(t as RationalNumber).Num);
                            else if(t is DoubleNumber)
                                termList.Add(-(t as DoubleNumber).Num);
                            else termList.Add(Minus(t));
                        } else termList.Add(t);
                } else
                    termList.Add(term);
            }
            if(termList.Count > e.Args.Length)
                return flattenMults(Mult(termList.ToArray()));
            return termList;
        }
        public Expr   Num(Expr e) {
            if(e is IntegerNumber)
                return e;
            IntegerNumber num = 0;
            IntegerNumber den = 1;
            Expr accum = null;
            switch(head(e)) {
                case WKSID.times:
                    bool numIsNum = false;
                    List<Expr> fargs1 = flattenMults(e as CompositeExpr);
                    List<Expr> fargs = new List<Expr>();
                    for(int i = 0; i < fargs1.Count; i++)
                        fargs.Add(Num(fargs1[i]));
                    List<Expr> additions = new List<Expr>();
                    List<Expr> others = new List<Expr>();
                    foreach(Expr t in fargs) {
                        if(head(t) == WKSID.plus)
                            additions.Add(t);
                        else others.Add(t);
                    }
                    Expr additiveAccum = null;
                    foreach(Expr t in fargs) { // compute fraction terms
                        Expr tval = Num(t);
                        if(tval == WellKnownSym.infinity) // mult by inf -> inf
                            return WellKnownSym.infinity;
                        DivideExpr div = new DivideExpr(tval);
                        PowerExpr pow = new PowerExpr(tval);
                        if(tval is IntegerNumber) { num.Num = (numIsNum ? num.Num : 1) * (tval as IntegerNumber).Num; numIsNum = true; } else if(pow.OK && pow.PowerInt && pow.IPower < 0) den.Num *= pow.IPower;
                        else if(div.OK && div.DivisorInt) den.Num *= div.IDivisor;
                        else if(head(tval) == WKSID.times && (head(Ag(tval, 0)) == WKSID.plusminus || head(Ag(tval, 1)) == WKSID.plusminus))
                            if(additiveAccum == null)
                                additiveAccum = tval;
                            else additiveAccum = Mult(additiveAccum, tval);
                        else if(head(tval) == WKSID.times && Ag(tval, 0) is IntegerNumber) {
                            DivideExpr dexp = new DivideExpr(Ag(tval, 1));
                            if(dexp.OK && dexp.DivisorInt) {                   // keep integer fractions, leave others unsimplified
                                if(den.Num == dexp.IDivisor) {
                                    num.Num *= (Ag(tval, 0) as IntegerNumber).Num;
                                    numIsNum = true;
                                } else {
                                    num.Num = (Ag(tval, 0) as IntegerNumber).Num * num.Num;
                                    den.Num *= dexp.IDivisor;
                                    numIsNum = true;
                                }
                            } else if(accum == null)
                                accum = tval;
                            else accum = Mult(accum, tval);
                        } else if(accum == null)
                            accum = tval;
                        else accum = Mult(accum, tval);
                    }
                    if(den.Num == 0 && num.Num == 0) return double.NaN;
                    if(den.Num == 0) return WellKnownSym.infinity; // divide by zero -> infinity
                    if(num.Num == 0 && numIsNum) return 0; // terminate early if we're multiplying by 0
                    Expr fraction = null;
                    if(numIsNum && (den.Num != 1 || num.Num != 1)) {
                        if ((double)num.Num / (double)den.Num == Math.Floor((double)num.Num / (double)den.Num)) // convert frac to integer if possible
                            fraction = (int)Math.Floor((double)num.Num / (double)den.Num);
                        else if ((double)den.Num / (double)num.Num == Math.Floor((double)den.Num / (double)num.Num)) // reduce numerator if possible
                            fraction = Divide((int)Math.Floor((double)den.Num / (double)num.Num));
                        else fraction = Mult(num, Divide(den)); // fraction
                    } else if(den.Num != 1)
                        fraction = Divide(den);
                    if((fraction == null || (fraction is IntegerNumber && (fraction as IntegerNumber).Num == 1)))
                        if(accum != null && additiveAccum != null)
                            return Mult(accum, additiveAccum);
                        else if(accum != null)
                            return accum;
                        else if(additiveAccum != null)
                            return additiveAccum;
                    if(fraction != null && accum == null && additiveAccum == null)
                        return fraction;
                    if(fraction == null && accum == null && additiveAccum == null)
                        return 1;
                    if(accum == null)
                        return Plus(fraction, additiveAccum);
                    else if(additiveAccum == null) {
                        if(head(accum) == WKSID.plus) {
                            List<Expr> plusterms = new List<Expr>();
                            for(int i = 0; i < Args(accum).Length; i++)
                                plusterms.Add(Num(Mult(Args(accum)[i], fraction)));
                            return Plus(plusterms.ToArray());
                        }
                        return Mult(fraction, accum);
                    }
                    return Plus(Mult(fraction, accum), additiveAccum);
                case WKSID.factorial: {
                        Expr arg = Num(Ag(e, 0));
                        if(arg is IntegerNumber && ((IntegerNumber)arg).Num > 0) return Factorial(((IntegerNumber)arg).Num);
                        else return new CompositeExpr(WellKnownSym.factorial, arg);
                    }
                case WKSID.divide:
                    return Num(Power(Ag(e, 0), -1));
                case WKSID.minus:
                    Expr term = Num(Ag(e, 0));
                    if(term == WellKnownSym.infinity) return WellKnownSym.infinity;
                    if(term is IntegerNumber) return (int)-(term as IntegerNumber).Num;
                    if(term is DoubleNumber) return (double)-(term as DoubleNumber).Num;
                    switch(head(term)) {
                        case WKSID.times: // - (a 4 c) ->  a -4 c
                            List<Expr> rem = new List<Expr>();
                            bool flipped = false;
                            foreach(Expr m in Args(term))
                                if(!flipped && m is IntegerNumber) {
                                    flipped = true;
                                    rem.Add((int)-(m as IntegerNumber).Num);
                                } else rem.Add(m);
                            if(flipped)
                                return Mult(rem.ToArray());
                            break;
                        case WKSID.divide:
                            DivideExpr dexp = new DivideExpr(term);
                            if(dexp.OK && dexp.DivisorInt)
                                return Divide((int)-dexp.IDivisor);
                            break;
                        case WKSID.plus:
                            return Num(Mult(-1, term));
                    }
                    return Minus(term);
                case WKSID.mod: {
                        Expr a = Num(Ag(e, 0));
                        Expr b = Num(Ag(e, 1));
                        IntegerNumber ia = a as IntegerNumber;
                        IntegerNumber ib = b as IntegerNumber;
                        DoubleNumber da = a as DoubleNumber;
                        DoubleNumber db = b as DoubleNumber;
                        if(ia != null && ib != null) return ia.Num % ib.Num;
                        else if(ia != null && db != null) return Math.IEEERemainder(ia.Num.AsDouble(), db.Num);
                        else if(da != null && ib != null) return Math.IEEERemainder(da.Num, ib.Num.AsDouble());
                        else if(da != null && db != null) return Math.IEEERemainder(da.Num, db.Num);
                        else return new CompositeExpr(WellKnownSym.mod, a, b);
                    }
                case WKSID.power: {
                        Expr bas = Num(Ag(e, 0));
                        Expr pow = Num(Ag(e, 1));
                        if(head(bas) == WKSID.times) { // distribute (ab)^x -> a^x b^x
                            List<Expr> nargs = new List<Expr>();
                            foreach(Expr t in Args(bas))
                                nargs.Add(Power(t, pow));
                            return Num(Mult(nargs.ToArray()));
                        }
                        if(bas is IntegerNumber && pow is IntegerNumber) { // compute a^b
                            BigInt basI = (bas as IntegerNumber).Num;
                            BigInt powI = (pow as IntegerNumber).Num;
                            Expr powNum = new NullExpr();
                            try {
                                if(powI == -1)
                                    powNum = bas;
                                else powNum = Math.Pow((int)(bas as IntegerNumber), Math.Abs((int)(pow as IntegerNumber)));
                            } catch(Exception) {
                                powNum = new IntegerNumber((int)FSBigInt.Pow(basI.Num, (int)FSBigInt.Abs(powI.Num)));
                            }
                            if((powNum is DoubleNumber) && (int)(powNum as DoubleNumber).Num == (powNum as DoubleNumber).Num)
                                powNum = (int)((powNum as DoubleNumber).Num);
                            if(powI < 0)
                                return Divide(powNum);
                            return powNum;
                        }
                        if(pow is IntegerNumber) {
                            BigInt powI = (pow as IntegerNumber).Num;
                            if(powI == 1) return bas;             // x^1 -> x
                            if(powI == 0) return 1;               // x^0 -> 1
                        }
                        DivideExpr dexp = new DivideExpr(pow);  // XXX- hack - x^(a/b) sometimes is an int need symbolic test
                        if(bas is IntegerNumber && dexp.OK && dexp.DivisorInt) {
                            int neg = ((int)(dexp.IDivisor / 2) == ((int)dexp.IDivisor) / 2 && (bas as IntegerNumber).Num < 0) ? -1 : 1;
                            double pd = Math.Pow(neg * (int)(bas as IntegerNumber).Num, 1.0 / (int)dexp.IDivisor);
                            if (Math.Abs(pd-Math.Round(pd)) < 1e-15) {
                                int p = (int)Math.Round(pd);
                                if (neg < 0) {
                                    int n = (int)dexp.IDivisor;
                                    // (-256)^(1/8) -> -2i
                                    // (-128)^(1/7)-> -2
                                    // (-64)^(1/6) -> +-2i
                                    // (-32)^(1/5) -> -2
                                    // (-16)^(1/4) -> 2+2i
                                    // (-8)^(1/3) -> -2
                                    // (-4)^(1/2) -> 2i
                                    if ((n+1)/2 == (n+1)/2.0) // odd fraction power is -p
                                        return -p;
                                    if (n == 2)               // sqrt is p*imaginary
                                        return p == 1 ? (Expr)WellKnownSym.i : Mult(p, WellKnownSym.i);
                                    Expr re = Num(Mult(p,Num(new CompositeExpr(WellKnownSym.cos, Mult(WellKnownSym.pi, Divide(n))))));
                                    Expr im = Num(Mult(p,new CompositeExpr(WellKnownSym.sin, Mult(WellKnownSym.pi, Divide(n)))));
                                    if (re is RealNumber && im is RealNumber)
                                        return new ComplexNumber(re as RealNumber,im as RealNumber);
                                    return Mult(Num(Power(Minus(bas),Divide(n))), Power(-1, Divide(n)));
                                }
                                return (int)p;
                            }
                            int mult = 1;
                            BigInt basVal = (bas as IntegerNumber).Num;
                            while(dexp.IDivisor == 2 && (basVal >= 4 || basVal <= -4) && ((int)basVal / 4.0) == (double)(basVal / 4)) {
                                mult *= 2;
                                basVal = basVal / 4;
                            }
                            while(dexp.IDivisor == 2 && (basVal >= 9 || basVal <= -9) && ((int)basVal / 9.0) == (double)(basVal / 9)) {
                                mult *= 3;
                                basVal = basVal / 9;
                            }
                            while(dexp.IDivisor == 2 && (basVal >= 25 || basVal <= -25) && ((int)basVal / 25.0) == (double)(basVal / 25)) {
                                mult *= 5;
                                basVal = basVal / 25;
                            }
                            while(dexp.IDivisor == 2 && (basVal >= 49 || basVal <= -49) && ((int)basVal / 49.0) == (double)(basVal / 49)) {
                                mult *= 7;
                                basVal = basVal / 49;
                            }
                            if(mult != 1)
                                if(basVal != 1)
                                    return Mult(mult, Power(new IntegerNumber(basVal), pow));
                                else return new IntegerNumber(basVal);
                        }
                        if(head(bas) == WKSID.divide) return Num(Power(Ag(bas, 0), Minus(pow))); // (1/x)^a -> x^(-a)bas
                        if(head(bas) == WKSID.power) return Num(Power(Ag(bas, 0), Mult(Ag(bas, 1), pow))); // (x^a)^b ->  x^(a*b)
                        RealNumber bnum = bas as RealNumber, pnum = pow as RealNumber;
                        if (bnum != null && pnum != null) {
                            double b = (bnum is IntegerNumber) ? (double)((bnum as IntegerNumber).Num) : (bnum as DoubleNumber).Num;
                            double p = (pnum is IntegerNumber) ? (double)((pnum as IntegerNumber).Num) : (pnum as DoubleNumber).Num;
                            double r =  Math.Pow(b, p);
                            if (Math.Abs(r -Math.Round(r)) < 1e-15)
                                return (int)Math.Round(r);
                            return r;
                        }
                        return Power(bas, pow);
                    }
                case WKSID.root: // nth root (x) -> x ^(1/n)
                    return Num(Power(Ag(e, 0), Divide(Ag(e, 1))));
                case WKSID.arccos: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.arccos, val);
                    }
                case WKSID.magnitude : {
                        Expr val = Num(Ag(e,0));
                        if (val is IntegerNumber)
                            return (val as IntegerNumber).Num.abs();
                        else if (val is DoubleNumber)
                            return Math.Abs((val as DoubleNumber).Num);
                        else return new CompositeExpr(WellKnownSym.magnitude, val);
                    }
                case WKSID.arcsin: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.arcsin, val);
                    }
                case WKSID.arctan: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.arctan, val);
                    }
                case WKSID.asec: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.asec, val);
                    }
                case WKSID.acsc: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.acsc, val);
                    }
                case WKSID.acot: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.acot, val);
                    }
                case WKSID.sec: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.sec, val);
                    }
                case WKSID.csc: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.csc, val);
                    }
                case WKSID.cot: {
                        Expr val = Num(Ag(e, 0));
                        return new CompositeExpr(WellKnownSym.cot, val);
                    }
                case WKSID.cos: {
                        Expr val = Num(Ag(e, 0));
                        if(val is IntegerNumber && (val as IntegerNumber).Num == 0) //cos(0) -> 1
                            return 1;
                        return new CompositeExpr(WellKnownSym.cos, val);
                    }
                case WKSID.index: {
                        ArrayExpr ae = null, ai = null;
                        if(!IsArrayIndex(e, ref ae, ref ai)) return e;
                        int[] inds = ConvertToInd(ai, delegate(Expr ee) { return ExprToInd(_Approximate(ee)); });
                        if(inds == null) return e;
                        return Num(ae[inds]);
                    }
                case WKSID.sin: {
                        Expr val = Num(Ag(e, 0));
                        if((val is IntegerNumber && (val as IntegerNumber).Num == 0)) //sin(0) -> 0
                            return 0;
                        return new CompositeExpr(WellKnownSym.sin, val);
                    }
                case WKSID.tan: {
                        Expr val = Num(Ag(e, 0));
                        if((val is IntegerNumber && (val as IntegerNumber).Num == 0)) //tan(0) -> 0
                            return 0;
                        return new CompositeExpr(WellKnownSym.tan, val);
                    }
                case WKSID.plus:
                    num.Num = 0;
                    foreach(Expr t in Args(e)) {
                        Expr tval = Num(t);
                        if(tval is WellKnownSym && (tval as WellKnownSym).ID == WKSID.infinity) // add infinity -> infinity
                            return WellKnownSym.infinity;
                        if(tval is IntegerNumber) {
                            num.Num += den.Num * (BigInt)tval; // accumulate numerator
                            continue;
                        }
                        if(head(tval) == WKSID.divide && Ag(tval, 0) is IntegerNumber) { // accumulate divisor (& numerator)
                            BigInt oldden = den.Num;
                            BigInt newden = (BigInt)Ag(tval, 0);
                            num.Num *= newden;
                            den.Num *= newden;
                            num.Num += oldden;
                            continue;
                        }
                        if(head(tval) == WKSID.times && Ag(tval, 0) is IntegerNumber) {  // accumulate fraction
                            DivideExpr dexp = new DivideExpr(Ag(tval, 1));
                            if(dexp.OK && dexp.DivisorInt) {                   // keep integer fractions, leave others unsimplified
                                BigInt oldden = den.Num;
                                BigInt newnum = (BigInt)Ag(tval, 0);
                                BigInt newden = dexp.IDivisor;
                                if(oldden == newden) {
                                    num.Num += newnum;
                                } else {
                                    num.Num *= newden;
                                    den.Num *= newden;
                                    num.Num += oldden * newnum;
                                }
                                continue;
                            }
                        }
                        if(accum == null)
                            accum = tval;
                        else
                            accum = Plus(accum, tval);
                    }
                    if(num.Num == 0) {
                        if(accum != null)
                            return accum;
                        return 0;
                    }
                    Expr frac = ((double)num.Num / (double)den.Num == (int)(num.Num / den.Num)) ? (Expr)(int)(num.Num / den.Num) :
                                                                                              (Expr)Mult(num, Divide(den));
                    return accum == null ? frac : Plus(frac, accum);
                default:
                    if(e is CompositeExpr) {
                        List<Expr> args = new List<Expr>();
                        foreach(Expr a in Args(e))
                            args.Add(Num(a));
                        return new CompositeExpr((e as CompositeExpr).Head.Clone(), args.ToArray());
                    }
                    break;
            }
            return e;
        }

        static LetterSym _degree = new LetterSym('°');
        BigInt        Factorial(BigInt x) {
            BigInt accum = 1;
            for(BigInt i = 2; i <= x; i = i + 1) accum *= i;
            return accum;
        }
        Expr          CanonicalizeTimes(CompositeExpr e) {
            Hashtable bases = new Hashtable();
            Hashtable baseCounts = new Hashtable();
            SortedList<int, Expr> baseOrders = new SortedList<int, Expr>();
            bool degrees = false;
            List<Expr> arrays = new List<Expr>();
            int index = 0;
            foreach(Expr term in e.Args) {
                if(term == _degree) {
                    degrees = true;
                    continue;
                }
                if(IsNum(term)) {
                    if(!baseCounts.Contains("unitless"))
                        baseCounts.Add("unitless", Num(term));
                    else baseCounts["unitless"] = Num(Mult((Expr)baseCounts["unitless"], term));
                } else if(term is ArrayExpr) {
                    arrays.Add(term);
                } else
                    switch(head(term)) {
                        case WKSID.power:
                            Expr baseTerm = Ag(term, 0);
                            Expr baseExp = Ag(term, 1);
                            if(bases.Contains(baseTerm)) { // a^x a^y -> a^(x+y)
                                Expr pow = (Expr)bases[baseTerm];
                                bases[baseTerm] = Canonicalize(Plus(pow, baseExp));
                            }
                            else {
                                baseOrders.Add(index++, baseTerm);
                                bases.Add(baseTerm, baseExp);
                                baseCounts.Add(baseTerm, (Expr)1);
                            }
                            break;
                        default:
                            if (!bases.Contains(term)) {
                                baseOrders.Add(index++, term);
                                bases.Add(term, (Expr)1);  // a^1
                                baseCounts.Add(term, (Expr)1); // 1 * a
                            } else {  // a ^ x * a -> a ^ (x + 1)
                                bases[term] = Canonicalize(Plus((Expr)bases[term], 1));
                            }
                            break;
                    }
            }
            List<Expr> mterms = new List<Expr>();
            if(baseCounts.Contains("unitless")) {
                if((baseCounts["unitless"] is IntegerNumber)) {
                    BigInt unitless = (baseCounts["unitless"] as IntegerNumber).Num;
                    if(unitless == 0)
                        if(degrees) {
                            arrays.Insert(0, 0);
                            arrays.Insert(1, _degree);
                            return Mult(arrays.ToArray());
                        } else
                            return 0;
                    if(unitless != 1)
                        mterms.Add((Expr)baseCounts["unitless"]);
                } else mterms.Add((Expr)baseCounts["unitless"]);
            }
            foreach(KeyValuePair<int,Expr> pair in baseOrders) {
                KeyValuePair<Expr, Expr> de = new KeyValuePair<Expr, Expr>(pair.Value, (Expr)bases[pair.Value]);
                Expr count = (baseCounts[de.Key] as Expr);
                Expr pow = (de.Value as Expr);
                Expr t = !(pow is IntegerNumber) || (int)pow != 1 ? Canonicalize(Power((Expr)de.Key, pow)) : (Expr)de.Key;
                if(count is IntegerNumber && (int)count == 1)
                    count = null;
                if(t is IntegerNumber && (int)t == 1)
                    t = null;
                if(t != null && count != null)
                    t = Mult(count, t);
                else if(t == null)
                    t = count;
                if(t is ComplexNumber && mterms.Count == 1 && mterms[0] is IntegerNumber) {
                    IntegerNumber rei = (t as ComplexNumber).Re as IntegerNumber;
                    DoubleNumber red = (t as ComplexNumber).Re as DoubleNumber;
                    IntegerNumber iei = (t as ComplexNumber).Im as IntegerNumber;
                    DoubleNumber ied = (t as ComplexNumber).Im as DoubleNumber;
                    IntegerNumber mi = mterms[0] as IntegerNumber;
                    if((rei != null || red != null) && (ied != null || iei != null))
                        mterms[0] = new ComplexNumber((RealNumber)(rei != null ? (Expr)(rei.Num * mi.Num) : (Expr)new DoubleNumber(red.Num * (double)mi.Num)),
                                                      (RealNumber)(iei != null ? (Expr)(iei.Num * mi.Num) : (Expr)new DoubleNumber(ied.Num * (double)mi.Num)));
                    else mterms.Add(t);
                } else if(t != null)
                    mterms.Add(t);
            }
            Expr ret = null;
            if(mterms.Count == 0)
                ret = 1;
            if(mterms.Count == 1)
                ret = mterms[0];
            if(ret != null) {
                if(degrees || arrays.Count == 0 || ret != 1) {
                    arrays.Insert(0, ret);
                    if(degrees) arrays.Insert(1, _degree);
                }
                return MaybeMult(arrays.ToArray());
            }
            ret = Mult(mterms.ToArray());
            foreach(Expr term in e.Args)
                if(head(term) == WKSID.plus) { // distribute addition terms: 
                    List<Expr> mults = new List<Expr>();
                    mults.Add(null);
                    foreach(Expr t in e.Args)
                        if(!Object.ReferenceEquals(t, term))
                            mults.Add(t);
                    List<Expr> adds = new List<Expr>();
                    foreach(Expr t in Args(term)) {
                        mults[0] = t;
                        adds.Add(Canonicalize(Mult(mults.ToArray())));
                    }
                    Expr ret2 = Canonicalize(Plus(adds.ToArray()));
                    //if(Text.InputConvert(ret2).Length < Text.InputConvert(ret).Length)
                    //    ret = ret2;
                    break;
                }
            if(arrays.Count == 0 || ret != 1) arrays.Insert(0, ret);
            return MaybeMult(arrays.ToArray());
        }

        public  Expr Canonicalize(Expr e) {
            if (e is CompositeExpr)
                return _Canonicalize(e as CompositeExpr);
            return e;
        }
        private Expr _Canonicalize(NullExpr e) {
            return e;
        }
        private Expr _Canonicalize(ErrorExpr e) {
            return e;
        }
        private Expr _Canonicalize(CompositeExpr e) {
            if(IsNum(e))
                return Num(e);
            List<Expr> termList = new List<Expr>();
            if(head(e) == WKSID.times)
                termList = flattenMults(e as CompositeExpr); // don't canonicalize args to preserve factors
            else if(head(e) == WKSID.plus) {
                bool recurse = false;
                foreach(Expr term in e.Args) { // flatten out all additions
                    if(head(term) == WKSID.plus) {
                        foreach(Expr t in Args(term)) {
                            termList.Add(t);
                            recurse = true;
                        }
                    } else {
                        Expr canon = Canonicalize(term);
                        if(canon != term)
                            recurse = true;
                        termList.Add(canon);
                    }
                }
                if(recurse)
                    return Canonicalize(Plus(termList.ToArray()));
            } else foreach(Expr term in e.Args)
                    termList.Add(Canonicalize(term));

            Expr[] args = termList.ToArray();
            switch(head(e)) {
                case WKSID.times: Expr r = CanonicalizeTimes(Mult(args));
                    if(head(r) == WKSID.times) { // after simplifying, we now want to canonicalize any remaining terms to see if we can simplify further
                        termList.Clear();
                        foreach(Expr term in Args(r))
                            termList.Add(Canonicalize(term));
                        r = CanonicalizeTimes(Mult(termList.ToArray()));
                    } else
                        r = Canonicalize(r);
                    return r;
                case WKSID.minus:
                    if(head(args[0]) == WKSID.minus)
                        return args[0]; // convert - - (expr) to (expr)
                    if(args[0] is IntegerNumber && (int)args[0] < 0)
                        return -(int)args[0];
                    return Canonicalize(Mult((Expr)(-1), args[0]));
                case WKSID.divide:
                    if(head(args[0]) == WKSID.power)
                        return Power(Ag(args[0], 0), Canonicalize(Mult(Ag(args[0], 1), -1)));
                    else return Power(args[0], -1);
                case WKSID.power:
                    if(args[0] == WellKnownSym.i) {
                        if(IsNum(args[1])) {
                            Expr n = Num(args[1]);
                            if(n is IntegerNumber) {
                                if((int)((n as IntegerNumber).Num + 2) / 4.0 == (int)((n as IntegerNumber).Num + 2) / 4)
                                    return -1;
                                if((int)((n as IntegerNumber).Num + 1) / 4.0 == (int)((n as IntegerNumber).Num + 1) / 4)
                                    return new ComplexNumber(0, new IntegerNumber(-1));
                                if((int)(n as IntegerNumber).Num / 4.0 == (int)(n as IntegerNumber).Num / 4)
                                    return 1;
                                return new ComplexNumber(0, new IntegerNumber(1));
                            }
                        }
                    }
                    if(args[0] == WellKnownSym.e && head(args[1]) == WKSID.ln)
                        return Ag(args[1], 0);
                    if(head(args[0]) == WKSID.power && (
                        (args[1] is IntegerNumber) ||
                        (head(Ag(args[0],1)) == WKSID.divide && head(args[1]) == WKSID.divide))) // can't
                        return Canonicalize(Power(Ag(args[0], 0), Canonicalize(Mult(Ag(args[0], 1), args[1]))));
                    IntegerNumber qex1 = null;
                    if(IsNum(args[1])) { // simplify ^0 && ^
                        Expr qex = Num(args[1]);
                        if(qex is IntegerNumber) {
                            qex1 = (qex as IntegerNumber);
                            if(qex1.Num == 1)
                                return args[0];
                            else if(qex1.Num == 0)
                                return 1;
                        }
                    }
                    IntegerNumber qex0 = null;
                    if(IsNum(args[0])) { // simplify 0^ && 1^
                        Expr qex = Num(args[0]);
                        if(qex is IntegerNumber) {
                            qex0 = (qex as IntegerNumber);
                            if(qex0.Num == 0)
                                return 0;
                            if(qex0.Num == 1)
                                return 1;
                        }
                    }
                    if(qex0 != null && qex1 != null && qex1.Num >= 0)
                        return new BigInt(FSBigInt.Pow(qex0.Num.Num, (int)qex1.Num.Num));
                    if(head(args[0]) == WKSID.times) { // expand (ab)^x  to a^x b^x
                        List<Expr> tterms = new List<Expr>();
                        foreach(Expr t in Args(args[0]))
                            tterms.Add(Canonicalize(Power(t, args[1])));
                        return Canonicalize(Mult(tterms.ToArray()));
                    }
                    return new CompositeExpr(WellKnownSym.power, args);
                case WKSID.root:
                    return Canonicalize(Power(args[1], Divide(args[0])));
                case WKSID.cos: {
                        Expr val = args[0];
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree && Ag(val, 0) is IntegerNumber) {
                            int n = (int)Ag(val, 0);
                            if((double)(n - 90) / 180 == (n - 90) / 180)
                                return 0;
                            if((double)n / 360 == n / 360)
                                return 1;
                            if((double)(n - 180) / 360 == (n - 180) / 360)
                                return -1;
                            if((double)(n - 60) / 360 == (n - 60) / 360)
                                return Mult(1, Divide(2));
                            if((double)(n - 120) / 360 == (n - 120) / 360)
                                return Mult(-1, Divide(2));
                            if((double)(n - 240) / 360 == (n - 240) / 360)
                                return Mult(-1, Divide(2));
                            if((double)(n - 300) / 360 == (n - 300) / 360)
                                return Mult(1, Divide(2));
                        }
                        if(val == WellKnownSym.pi) // cos(pi) ->  -1
                            return -1;
                        if(val == Mult(Divide(2), WellKnownSym.pi)) // cos(pi/2) -> 0
                            return 0;
                        if(head(val) == WKSID.times && Ag(val, 1) == WellKnownSym.pi) { // cos(x * pi) ...
                            Expr m = Ag(val, 0);
                            int mval = (int)m;
                            if(m is IntegerNumber)                                     // cos(int * pi) ...
                                if(mval / 2.0 == mval / 2)                                 // cos(evenInt *pi)-> 1
                                    return 1;
                                else return -1;                      // cos(oddInt *pi) -> -1
                            if(head(m) == WKSID.times && (Ag(m, 0) is IntegerNumber) && Ag(m, 1) == Divide(2)) {  // cos(int *pi / 2) -> 0
                                return 0;
                            }
                            if(head(m) == WKSID.times && (Ag(m, 0) is IntegerNumber) && Ag(m, 1) == Divide(3)) { // sin(int *pi / 2) ...
                                int n = (int)Ag(m, 0);
                                if((n - 1) / 3.0 == (n - 1) / 3 || ((n - 5) / 3.0 == (n - 5) / 3))
                                    return Mult(1, Divide(2));
                                if((n - 2) / 3.0 == (n - 2) / 3 || ((n - 4) / 3.0 == (n - 4) / 3))
                                    return Mult(-1, Divide(2));
                            }
                        }
                        break;
                    }
                case WKSID.ln: {
                        if(args[0] == WellKnownSym.e)
                            return 1;
                        break;
                    }
                case WKSID.tan: {
                        Expr val = args[0];
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Canonicalize(new CompositeExpr(WellKnownSym.tan, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        break;
                    }
                case WKSID.index: {
                        Expr[] canoned = Array.ConvertAll<Expr, Expr>(args, Canonicalize);
                        return new CompositeExpr(WellKnownSym.index, canoned);
                    }
                case WKSID.sin: {
                        Expr val = args[0];
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree && Ag(val, 0) is IntegerNumber) {
                            int n = (int)Ag(val, 0);
                            if((double)n / 180 == n / 180)
                                return 0;
                            if((double)(n - 90) / 360 == (n - 90) / 360)
                                return 1;
                            if((double)(n + 90) / 360 == (n + 90) / 360)
                                return -1;
                            if((double)(n - 30) / 360 == (n - 30) / 360)
                                return Mult(1, Divide(2));
                            if((double)(n - 150) / 360 == (n - 150) / 360)
                                return Mult(1, Divide(2));
                            if((double)(n - 210) / 360 == (n - 210) / 360)
                                return Mult(-1, Divide(2));
                            if((double)(n - 330) / 360 == (n - 330) / 360)
                                return Mult(-1, Divide(2));
                        }
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Canonicalize(new CompositeExpr(WellKnownSym.sin, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(val == WellKnownSym.pi) //sin(pi)  -> 0
                            return 0;
                        if(val == Mult(Divide(2), WellKnownSym.pi)) // sin(pi/2) -> 1
                            return 1;
                        if(head(val) == WKSID.times && Ag(val, 1) == WellKnownSym.pi) { // sin(x * pi) ...
                            Expr m = Ag(val, 0);
                            if(m is IntegerNumber)                                     // sin(int * pi) -> 0
                                return 0;
                            if(head(m) == WKSID.times && (Ag(m, 0) is IntegerNumber) && Ag(m, 1) == Divide(2)) { // sin(int *pi / 2) ...
                                int n = (int)Ag(m, 0);
                                if((n - 1) / 4.0 == (n - 1) / 4)
                                    return 1;                // sin(1, 5, 9...*pi/2) -> 1
                                else return -1;              // sin(3, 7, 11 ...*pi/2) -> -1
                            }
                            if(head(m) == WKSID.times && (Ag(m, 0) is IntegerNumber) && Ag(m, 1) == Divide(6)) { // sin(int *pi / 2) ...
                                int n = (int)Ag(m, 0);
                                if((n - 1) / 6.0 == (n - 1) / 6 || ((n - 5) / 6.0 == (n - 5) / 6))
                                    return Mult(1, Divide(2));
                                if((n - 7) / 6.0 == (n - 7) / 6 || ((n - 11) / 6.0 == (n - 11) / 6))
                                    return Mult(-1, Divide(2));
                            }
                        }
                        break;
                    }
                case WKSID.plus:
                    Hashtable terms = new Hashtable();
                    SortedList<int, object> termOrder = new SortedList<int, object>();
                    int index = 0;
                    foreach(Expr t in args) {
                        if(t is ComplexNumber) {
                            if (terms.Contains("unitless"))
                                terms["unitless"] = Num(Plus((Expr)terms["unitless"], (t as ComplexNumber).Re));
                            else {
                                terms.Add("unitless", (t as ComplexNumber).Re);
                                termOrder.Add(index++, "unitless");
                            }
                        }
                        if(IsNum(t)) {
                            if(terms.Contains("unitless"))
                                terms["unitless"] = Num(Plus((Expr)terms["unitless"], t));
                            else {
                                terms.Add("unitless", Num(t));
                                termOrder.Add(index++, "unitless");
                            }
                        } else if(t is LetterSym || t is WellKnownSym || head(t) == WKSID.plusminus) {
                            if(terms.Contains(t))
                                terms[t] = Canonicalize(Plus((Expr)terms[t], 1));
                            else {
                                terms.Add(t, (Expr)1);
                                termOrder.Add(index++, t);
                            }
                        } else if(t is ArrayExpr) {
                            terms.Add(t, (Expr)1);
                            termOrder.Add(index++, t);
                        } else {
                            Expr swVal = t is ComplexNumber ? Mult((t as ComplexNumber).Im, WellKnownSym.i) : t;
                            switch(head(swVal)) {
                                case WKSID.times:
                                    List<Expr> syms = new List<Expr>();
                                    List<Expr> nums = new List<Expr>();
                                    foreach(Expr tt in Args(swVal))
                                        if(IsNum(tt))
                                            nums.Add(tt);
                                        else syms.Add(tt);
                                    Expr baseTerm = syms.Count == 1 ? syms[0] : Mult(syms.ToArray());
                                    Expr baseNum = 1;
                                    if(nums.Count == 1)
                                        baseNum = nums[0];
                                    else if(nums.Count > 1)
                                        baseNum = Num(Mult(nums.ToArray()));
                                    if (terms.Contains(baseTerm))
                                        terms[baseTerm] = Canonicalize(Plus((Expr)terms[baseTerm], baseNum));
                                    else {
                                        bool foundBase = false;
                                        foreach (Expr s in syms)
                                            if (terms.Contains(s))
                                            {
                                                syms.Remove(s);
                                                syms.Insert(0, (Expr)terms[s]);
                                                terms[s] = Canonicalize(Plus(syms.ToArray()));
                                                foundBase = true;
                                                break;
                                            }
                                        foreach (Expr s in syms)
                                            if (s.Head() == WellKnownSym.power && s.Args()[1] == -1)
                                            {
                                                syms.Remove(s);
                                                terms.Add(s, Canonicalize(Plus(syms.ToArray())));
                                                termOrder.Add(index++, s);
                                                foundBase = true;
                                                break;
                                            }
                                        if (!foundBase)
                                        {
                                            terms.Add(baseTerm, baseNum);
                                            termOrder.Add(index++, baseTerm);
                                        }
                                    }
                                    break;
                                case WKSID.magnitude:
                                case WKSID.factorial:
                                case WKSID.summation:
                                case WKSID.power:
                                    if (terms.Contains(swVal))
                                        terms[swVal] = Canonicalize(Plus((Expr)terms[swVal], 1));
                                    else {
                                        terms.Add(swVal, (Expr)1);
                                        termOrder.Add(index++, swVal);
                                    }
                                    break;
                            }
                        }
                    }

                    List<Expr> plTerms = new List<Expr>();
                    List<Expr> plNumTerms = new List<Expr>();
                    foreach(KeyValuePair<int,object> next in termOrder) {
                        KeyValuePair<object, object> de = new KeyValuePair<object, object>(next.Value, terms[next.Value]);
                        if(de.Key is string && (string)de.Key == "unitless") {
                            if(!(de.Value is IntegerNumber) || (int)(Expr)de.Value != 0)
                                plNumTerms.Add((Expr)de.Value);
                        } else {
                            Expr baseCount = (de.Value as Expr);
                            if(baseCount is IntegerNumber && (int)baseCount == 0)
                                continue;
                            if(baseCount is IntegerNumber && (int)baseCount == 1)
                                plTerms.Add((Expr)de.Key);
                            else plTerms.Add(Mult((Expr)de.Value, (Expr)de.Key));
                        }
                    }
                    Expr plNum = null;
                    if(plNumTerms.Count == 1)
                        plNum = plNumTerms[0];
                    else if(plNumTerms.Count > 1)
                        plNum = Num(Plus(plNumTerms.ToArray()));
                    if(plNum != null)
                        plTerms.Add(plNum);
                    if(plTerms.Count == 0)
                        return 0;
                    else if(plTerms.Count == 1)
                        return plTerms[0];
                    else return Plus(plTerms.ToArray());
                case WKSID.floor:
                    if(args.Length == 1) {
                        if(args[0] is IntegerNumber) return args[0];
                        else if(args[0] is RationalNumber) {
                            RationalNumber rn = (RationalNumber)args[0];
                            FSBigInt rr = new FSBigInt(0);
                            FSBigInt qq = FSBigInt.DivRem(rn.Num.Num.Num, rn.Num.Denom.Num, out rr);
                            return new BigInt(qq - (qq.Sign*rr.Sign == -1 ? FSBigInt.One : FSBigInt.Zero));
                        } else if(args[0] is DoubleNumber) {
                            DoubleNumber dn = (DoubleNumber)args[0];
                            return Math.Floor(dn.Num);
                        }
                    }
                    break;
                case WKSID.magnitude:
                    if (args.Length == 1) {
                        if (args[0] is IntegerNumber) return (args[0] as IntegerNumber).Num.abs();
                        else if (args[0] is DoubleNumber) return Math.Abs(((DoubleNumber)args[0]).Num);
                        else return new CompositeExpr(WellKnownSym.magnitude, args);
                    }
                    break;
                case WKSID.ceiling:
                    if(args.Length == 1) {
                        if(args[0] is IntegerNumber) return args[0];
                        else if(args[0] is RationalNumber) {
                            RationalNumber rn = (RationalNumber)args[0];
                            FSBigInt rr = new FSBigInt(0);
                            FSBigInt qq = FSBigInt.DivRem(rn.Num.Num.Num, rn.Num.Denom.Num, out rr);
                            return new BigInt(qq + (qq.Sign*rr.Sign == 1 ? FSBigInt.One : FSBigInt.Zero));
                        } else if(args[0] is DoubleNumber) {
                            DoubleNumber dn = (DoubleNumber)args[0];
                            return Math.Floor(dn.Num);
                        }
                    }
                    break;
            }
            return new CompositeExpr(Canonicalize(e.Head), args);
        }
        private Expr _Canonicalize(DoubleNumber n) {
            return n;
        }
        private Expr _Canonicalize(IntegerNumber n) {
            return n;
        }
        private Expr _Canonicalize(RationalNumber n) {
            return n;
        }
        private Expr _Canonicalize(ComplexNumber n) {
            return n;
        }
        private Expr _Canonicalize(ArrayExpr e) {
            return e;
        }
        private Expr _Canonicalize(LetterSym s) {
            return s;
        }
        private Expr _Canonicalize(GroupedLetterSym s) {
            return s;
        }
        private Expr _Canonicalize(WordSym s) {
            return s;
        }
        private Expr _Canonicalize(WellKnownSym s) {
            return s;
        }

        /* Stuff for numeric evaluation */
        public Expr Numericize(Expr e) {
            try {
                if (e is CompositeExpr)
                    return _Numericize(e as CompositeExpr);
                if (e is WellKnownSym)
                    return _Numericize(e as WellKnownSym);
                if (e is WordSym)
                    return e;
                if (e is DoubleNumber)
                    return e;
                if (e is IntegerNumber)
                    return e;
                if (e is LetterSym)
                    return e;
                return (Expr)this.GetType().InvokeMember("_Numericize", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, this, new object[] { e });
            }
            catch (System.MissingMethodException) {
                return e;
            }
            catch (System.Reflection.TargetException) {
                return e;
            }
        }
        private Expr _Numericize(NullExpr e) {
            return e;
        }
        private Expr _Numericize(ErrorExpr e) {
            return e;
        }
        private Expr _Numericize(CompositeExpr e) {
            Expr[] args = new Expr[e.Args.Length];
            for(int i = 0; i < e.Args.Length; i++)
                if((i != 0 || head(e) != WKSID.index) && !(i == 0 && e.Head == WellKnownSym.summation && e.Args.Length > 1))
                    args[i] = Numericize(e.Args[i]);
                else args[i] = e.Args[i];
            Expr val = Ag(e, 0);
            if(e.Head is WellKnownSym) {
                DoubleNumber dn1 = null;
                DoubleNumber dn2 = null;
                IntegerNumber in1 = null;
                IntegerNumber in2 = null;
                ComplexNumber cn1 = null;
                ComplexNumber cn2 = null;
                if(args.Length > 0) {
                    dn1 = args[0] as DoubleNumber;
                    cn1 = args[0] as ComplexNumber;
                    in1 = args[0] as IntegerNumber;
                }
                if(args.Length > 1) {
                    dn2 = args[1] as DoubleNumber;
                    cn2 = args[1] as ComplexNumber;
                    in2 = args[1] as IntegerNumber;
                }
                double d1 = 0;
                double d2 = 0;
                if(in2 != null) d2 = (double)in2.Num;
                if(in1 != null) d1 = (double)in1.Num;
                if(dn2 != null) d2 = dn2.Num;
                if(dn1 != null) d1 = dn1.Num;
                bool num1 = dn1 != null || in1 != null;
                bool num2 = dn2 != null || in2 != null;
                WKSID id = ((WellKnownSym)e.Head).ID;
                switch(id) {
                    case WKSID.im:
                        Trace.Assert(args.Length == 1);
                        if(args[0] is RealNumber) return 0.0;
                        else if(args[0] is ComplexNumber) return ((ComplexNumber)args[0]).Im;
                        break;
                    case WKSID.magnitude:
                        if(in1 != null) return in1.Num.abs();
                        if(dn1 != null) return Math.Abs(dn1.Num);
                        if(cn1 != null) return Numericize(new CompositeExpr(WellKnownSym.root, 2, new CompositeExpr(WellKnownSym.plus,
                            new CompositeExpr(WellKnownSym.power, cn1.Re, 2), new CompositeExpr(WellKnownSym.power, cn1.Im, 2))));
                        break;
                    case WKSID.minus:
                        Trace.Assert(args.Length == 1);
                        if(num1)
                            if(in1 != null) return new IntegerNumber(-in1.Num);
                            else return -d1;
                        else if(args[0] is ComplexNumber) return NMinus((ComplexNumber)args[0]);
                        else if(args[0] is ArrayExpr) {
                            ArrayExpr ae = new ArrayExpr((Array)((ArrayExpr)args[0]).Elts.Clone());
                            foreach(int[] i in ae.Indices) ae[i] = Numericize(Minus(ae[i]));
                            if(!args[0].Annotations.Contains("Force Parentheses") || !args[0].Annotations["Force Parentheses"].Equals(0)) ae.Annotations["Force Parentheses"] = 1;
                            return ae;
                        }
                        break;
                    case WKSID.plus: {
                            Trace.Assert(args.Length > 0);
                            if(args.Length == 1) {
                                return args[0];
                            }
                            if(Array.TrueForAll(args, delegate(Expr a) { return a is ArrayExpr; })) {
                                ArrayExpr[] aes = Array.ConvertAll<Expr, ArrayExpr>(args, delegate(Expr a) { return (ArrayExpr)a; });
                                int[] dims = aes[0].Dims;
                                bool isok = true;
                                for(int i = 1; isok && i < args.Length; i++) {
                                    int[] dd = aes[i].Dims;
                                    if(dd.Length != dims.Length) isok = false;
                                    for(int j = 0; isok && j < dims.Length; j++) if(dims[j] != dd[j]) isok = false;
                                }
                                if(isok) {
                                    ArrayExpr newae = new ArrayExpr((Array)aes[0].Elts.Clone());
                                    foreach(int[] ix in newae.Indices) {
                                        newae[ix] = Numericize(Plus(Array.ConvertAll<ArrayExpr, Expr>(aes, delegate(ArrayExpr ae) { return ae[ix]; })));
                                    }
                                    newae.Annotations["Force Parentheses"] = 1;
                                    return newae;
                                }
                            }
                            List<Expr> leftover = new List<Expr>();
                            double rsum = 0;
                            double isum = 0;
                            IntegerNumber risum = 0;
                            bool anyd = false, anyc = false, anyi = false;
                            foreach(Expr p in args) {
                                DoubleNumber dn = p as DoubleNumber;
                                IntegerNumber inn = p as IntegerNumber;
                                ComplexNumber cn = p as ComplexNumber;
                                if(dn != null) {
                                    if(anyi) {
                                        rsum = dn.Num + (double)risum.Num;
                                        anyi = false;
                                    } else
                                        rsum += dn.Num;
                                    anyd = true;
                                } else if(inn != null) {
                                    if(anyd)
                                        rsum += (double)inn.Num;
                                    else {
                                        risum = risum.Num + inn.Num;
                                        anyi = true;
                                    }
                                } else if(cn != null) {
                                    DoubleNumber red = cn.Re as DoubleNumber;
                                    DoubleNumber imd = cn.Im as DoubleNumber;
                                    if(red != null && imd != null) {
                                        if(anyi)
                                            rsum = red.Num + (double)risum.Num;
                                        else rsum += red.Num;
                                        isum += imd.Num;
                                        anyd = true;
                                        anyc = true;
                                        anyi = false;
                                    } else leftover.Add(p);
                                } else leftover.Add(p);
                            }
                            Number rn = anyd ? (Number)new DoubleNumber(rsum) : (Number)risum;
                            Number n = anyc ? (Number)new ComplexNumber(rsum, isum) : (Number)rn;
                            if(leftover.Count == 0) return n;
                            else {
                                if(anyd || anyi || anyc) leftover.Add(n);
                                return new CompositeExpr(e.Head, leftover.ToArray());
                            }
                        }
                    case WKSID.mod:
                        if(args.Length != 2) break;
                        if(in1 != null && in2 != null) return new IntegerNumber(in1.Num % in2.Num);
                        else if(in1 != null && dn2 != null) return Math.IEEERemainder(in1.Num.AsDouble(), dn2.Num);
                        else if(dn1 != null && in2 != null) return Math.IEEERemainder(dn1.Num, in2.Num.AsDouble());
                        else if(dn1 != null && dn2 != null) return Math.IEEERemainder(dn1.Num, dn2.Num);
                        break;
                    case WKSID.power:
                        if(args.Length < 2)
                            break;
                        Trace.Assert(args.Length == 2);
                        if(num1 && (d1 >= 0 || in2 != null) && num2) {
                            double pow = Math.Pow(d1, d2);
                            return in1 != null && in2 != null && d2 > 0 ? (Expr)new IntegerNumber((int)pow) : (Expr)new DoubleNumber(pow);
                        } else {
                            if(num1) cn1 = new ComplexNumber(d1, 0.0);
                            if(num2) cn2 = new ComplexNumber(d2, 0.0);
                            if(cn1 != null && cn2 != null) return NPower(cn1, cn2);
                        }
                        if(num2 && d2 == 0)
                            return new DoubleNumber(1);
                        if(num2 && d2 == 1)
                            return args[0];
                        if(args[0] is ArrayExpr && args[1] == new LetterSym('T')) {
                            // matrix transpose
                            // FIXME: actually, this should probably be turned into a wellknownsym "transpose" by parse2. Issue there is how to know T isn't being
                            // used as a variable?
                            ArrayExpr ae = (ArrayExpr)args[0];
                            if(ae.Elts.Rank == 1) {
                                Expr[,] n = new Expr[ae.Elts.Length, 1];
                                for(int i = 0; i < ae.Elts.Length; i++) {
                                    n[i, 0] = ae[i];
                                }
                                ae = new ArrayExpr(n);
                                ae.Annotations["Force Parentheses"] = 1;
                            } else if(ae.Elts.Rank == 2) {
                                int h = ae.Elts.GetLength(0);
                                int w = ae.Elts.GetLength(1);
                                Expr[,] n = new Expr[w, h];
                                for(int i = 0; i < h; i++) {
                                    for(int j = 0; j < w; j++) {
                                        n[j, i] = ae[i, j];
                                    }
                                }
                                ae = new ArrayExpr(n);
                                ae.Annotations["Force Parentheses"] = 1;
                                return ae;
                            }
                        }
                        break;
                    case WKSID.re:
                        Trace.Assert(args.Length == 1);
                        if(num1) return args[0];
                        else if(cn1 != null) return cn1.Re;
                        break;
                    case WKSID.times:
                        Trace.Assert(args.Length > 0);
                        if(args.Length == 1) {
                            return args[0];
                        } else {
                            List<Expr> leftover = new List<Expr>();
                            int start = 0;
                            while(start < args.Length) {
                                int end;
                                bool isarray = args[start] is ArrayExpr;
                                for(end = start + 1; end < args.Length; end++) {
                                    if(isarray != (args[end] is ArrayExpr)) break;
                                }
                                leftover.AddRange(isarray ? NMultiplyArrays(args, start, end) : NMultiplyScalars(args, start, end));
                                start = end;
                            }
                            if(leftover.Count == 1) {
                                return leftover[0];
                            } else {
                                leftover = this.NMultipyAll(args, e);
                                if(leftover.Count == 1)
                                    return leftover[0];
                                else
                                    return new CompositeExpr(e.Head, leftover.ToArray());
                            }
                            //original code before matrix scalar computations
                            //if(leftover.Count == 1) return leftover[0];
                            //else
                            //return new CompositeExpr(e.Head, leftover.ToArray());
                        }

                    case WKSID.divide:
                        Trace.Assert(args.Length == 1);
                        if(num1) return 1 / d1;
                        else if(args[0] is ComplexNumber) return NReciprocal((ComplexNumber)args[0]);
                        break;
                    case WKSID.ln:
                        Trace.Assert(args.Length == 1);
                        if(num1) {
                            if(d1 >= 0) return Math.Log(d1);
                            return NLog(d1);
                        } else if(args[0] is ComplexNumber) return NLog((ComplexNumber)args[0]);
                        break;
                    case WKSID.log: /* log takes base then value to take the logarithm of */
                        if(args.Length == 1) {
                            dn2 = dn1;
                            in2 = in1;
                            cn2 = cn1;
                            d2 = d1;
                            num2 = num1;
                            d1 = 10;
                            dn1 = 10;
                            in1 = null;
                            cn1 = null;
                            num1 = true;
                        }
                        if(num1 && num2) {
                            if(d1 >= 0 && num2 && d2 >= 0)
                                return Math.Log(d2, d1);
                            else return double.NaN;
                        } else {
                            if(num1) cn1 = new ComplexNumber(d1, 0.0);
                            if(num2) cn2 = new ComplexNumber(d2, 0.0);
                            if(cn1 != null && cn2 != null) return NTimes(NLog(cn2), NReciprocal(NLog(cn1)));
                        }
                        break;
                    case WKSID.root: /* takes root number (eg 2 for square root), value to take the root of */
                        Trace.Assert(args.Length == 2);
                        if(num1 && num2 && d2 >= 0) return Math.Pow(d2, 1 / d1);
                        else {
                            if(num1) cn1 = new ComplexNumber(d1, 0.0);
                            if(num2) cn2 = new ComplexNumber(d2, 0.0);
                            if(cn1 != null && cn2 != null) return NPower(cn2, NReciprocal(cn1));
                        }
                        if(head(args[1]) == WKSID.power)
                            return Numericize(Power(Ag(args[1], 0), Mult(Ag(args[1], 1), Divide(args[0]))));
                        break;
                    case WKSID.index: {
                            bool didnumericize = false;
                            if(!(args[0] is ArrayExpr)) {
                                args[0] = Numericize(args[0]);
                                didnumericize = true;
                            }
                            ArrayExpr aex = null, aix = null;
                            if(!IsArrayIndex(e, ref aex, ref aix)) {
                                if(!didnumericize) args[0] = Numericize(args[0]);
                                break;
                            }
                            int[] ix = ConvertToInd(aix, ExprToInd);
                            if(ix == null) {
                                if(!didnumericize) args[0] = Numericize(args[0]);
                                break;
                            }
                            return Numericize(aex[ix]);
                        }
                    case WKSID.sin:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.sin, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return Math.Sin(d1);
                        else if(cn1 != null) return NSin(cn1);
                        break;
                    case WKSID.cos:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.cos, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return Math.Cos(d1);
                        else if(cn1 != null) return NCos(cn1);
                        break;
                    case WKSID.tan:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.tan, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return Math.Tan(d1);
                        else if(cn1 != null) return NTan(cn1);
                        break;
                    case WKSID.sec:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.sec, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return 1 / Math.Cos(d1);
                        else if(cn1 != null) return NReciprocal(NCos(cn1));
                        break;
                    case WKSID.csc:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.csc, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return 1 / Math.Sin(d1);
                        else if(args[0] is ComplexNumber) return NReciprocal(NSin(cn1));
                        break;
                    case WKSID.cot:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.cot, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return 1 / Math.Tan(d1);
                        if(cn1 != null) return NReciprocal(NTan(cn1));
                        break;
                    case WKSID.arcsin:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Asin(d1);
                        if(cn1 != null) return NArcSin(cn1);
                        break;
                    case WKSID.arccos:
                        Trace.Assert(args.Length == 1);
                        if(head(val) == WKSID.times && Ag(val, 1) == _degree)
                            return Numericize(new CompositeExpr(WellKnownSym.arccos, Mult(WellKnownSym.pi, Num(Mult(Ag(val, 0), Divide(180))))));
                        if(num1) return Math.Acos(d1);
                        if(cn1 != null) return NArcCos(cn1);
                        break;
                    case WKSID.arctan:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Atan(d1);
                        if(cn1 != null) return NArcTan(cn1);
                        break;
                    case WKSID.asec:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Acos(1 / d1);
                        if(cn1 != null) return NArcCos(NReciprocal(cn1));
                        break;
                    case WKSID.acsc:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Asin(1 / d1);
                        if(cn1 != null) return NArcSin(NReciprocal(cn1));
                        break;
                    case WKSID.acot:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Atan(1 / d1);
                        if(cn1 != null) return NArcTan(NReciprocal(cn1));
                        break;
                    case WKSID.sinh:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Sinh(d1);
                        if(cn1 != null) return NSinH(cn1);
                        break;
                    case WKSID.cosh:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Cosh(d1);
                        if(cn1 != null) return NCosH(cn1);
                        break;
                    case WKSID.tanh:
                        Trace.Assert(args.Length == 1);
                        if(num1) return Math.Tanh(d1);
                        if(cn1 != null) return NTanH(cn1);
                        break;
                    case WKSID.sech:
                        Trace.Assert(args.Length == 1);
                        if(num1) return 1 / Math.Cosh(d1);
                        if(cn1 != null) return NReciprocal(NCosH(cn1));
                        break;
                    case WKSID.csch:
                        Trace.Assert(args.Length == 1);
                        if(num1) return 1 / Math.Sinh(d1);
                        if(cn1 != null) return NReciprocal(NSinH(cn1));
                        break;
                    case WKSID.coth:
                        Trace.Assert(args.Length == 1);
                        if(num1) return 1 / Math.Tanh(d1);
                        if(cn1 != null) return NReciprocal(NTanH(cn1));
                        break;
                    case WKSID.asinh:
                    case WKSID.acosh:
                    case WKSID.atanh:
                    case WKSID.asech:
                    case WKSID.acsch:
                    case WKSID.acoth:
                        /* C# library doesn't contain these functions */
                        break;
                    case WKSID.factorial:
                        Trace.Assert(args.Length == 1);
                        if(in1 != null && in1.Num >= 0) return Factorial(in1.Num);
                        break;
                    case WKSID.summation:
                        Trace.Assert(args.Length > 0 && args.Length < 4);
                        if(args.Length == 3 && num2 && e.Args[0] is CompositeExpr && !(e.Args[2] is NullExpr)) {
                            CompositeExpr ce = e.Args[0] as CompositeExpr;
                            Expr key = new NullExpr();
                            d1 = 0;
                            if(ce != null && ce.Head == WellKnownSym.equals && (ce.Args[0] is LetterSym || ce.Args[0] is WellKnownSym)) {
                                key = ce.Args[0].Clone();
                                Expr start = Numericize(ce.Args[1]);
                                IntegerNumber sinn = start as IntegerNumber;
                                DoubleNumber sdn = start as DoubleNumber;
                                d1 = (sinn != null) ? (double)sinn.Num : sdn != null ? sdn.Num : double.NaN;
                            }
                            Expr res = new IntegerNumber(0);
                            for(int d = (int)d1; d1 <= d2 && d <= (int)d2; d++) {
                                Expr newB = (Expr)e.Args[2].Clone();
                                newB = Numericize(_Substitute(newB, key, d));
                                res = Plus(res, newB);
                                res = Numericize(_Simplify(res));
                            }
                            return res;
                        }
                        else if (args.Length == 1)
                        {
                            return args[0];
                        }
                        break;
                }
            }
            return new CompositeExpr(Numericize(e.Head), args);
        }

        private ArrayExpr TempHackNewAE(Array a) {
            ArrayExpr ae = new ArrayExpr(a);
            ae.Annotations["Force Parentheses"] = 1;
            return ae;
        }
        private List<Expr> NMultiplyArrays(Expr[] args, int start, int end) {
#if false
            Expr[] a2 = new Expr[end-start];
            Array.Copy(args, start, a2, 0, end-start);
            return new List<Expr>(a2);
#endif
            List<Expr> leftover = new List<Expr>();
            int i;
            Expr[,] curmat = null;
            for(i = start; i < end; i++) {
                ArrayExpr ae = (ArrayExpr)args[i];
                if(ae.Elts.Rank == 2) {
                    if(curmat == null) {
                        curmat = (Expr[,])ae.Elts;
                    } else {
                        Expr[,] m2 = (Expr[,])ae.Elts;
                        if(curmat.GetUpperBound(1) != m2.GetUpperBound(0)) {
                            leftover.Add(TempHackNewAE(curmat));
                            curmat = m2;
                        } else {
                            int m = curmat.GetUpperBound(0) + 1;
                            int n = m2.GetUpperBound(1) + 1;
                            int o = curmat.GetUpperBound(1) + 1;
                            Expr[,] result = new Expr[m, n];
                            for(int j = 0; j < m; j++) {
                                for(int k = 0; k < n; k++) {
                                    Expr[] sum = new Expr[o];
                                    for(int l = 0; l < o; l++) {
                                        sum[l] = new CompositeExpr(WellKnownSym.times, curmat[j, l], m2[l, k]);
                                    }
                                    result[j, k] = Numericize(Canonicalize(new CompositeExpr(WellKnownSym.plus, sum)));
                                }
                            }
                            curmat = result;
                        }
                    }
                } else {
                    if(curmat != null) {
                        leftover.Add(TempHackNewAE(curmat));
                        curmat = null;
                    }
                    leftover.Add(ae);
                }
            }
            if(curmat != null) leftover.Add(TempHackNewAE(curmat));
            return leftover;
        }
        private List<Expr> NMultiplyScalars(Expr[] args, int start, int end) {
            List<Expr> leftover = new List<Expr>();
            double product = 1;
            IntegerNumber iproduct = 1;
            ComplexNumber cproduct = null; // stupid c#--shouldn't need to init this
            bool anyd = false, anyc = false, anyi = false;
            for(int i = start; i < end; i++) {
                Expr p = args[i];
                DoubleNumber dn = p as DoubleNumber;
                IntegerNumber inn = p as IntegerNumber;
                ComplexNumber cn = p as ComplexNumber;
                if(dn != null && (double.IsInfinity(dn.Num) || double.IsNaN(dn.Num))) {
                    leftover.Add(dn.Num);
                    return leftover;
                }
                if(dn != null) {
                    if(anyc) {
                        cproduct = NTimes(cproduct, dn.Num);
                        anyd = false;
                    } else if(anyi) {
                        product = (double)iproduct.Num * dn.Num;
                        anyi = false;
                        anyd = true;
                    } else {
                        product *= dn.Num;
                        anyd = true;
                    }
                } else if(inn != null) {
                    if(anyd) product *= (double)inn.Num;
                    else if(anyc) cproduct = NTimes(cproduct, (double)inn.Num);
                    else {
                        iproduct = iproduct.Num * inn.Num;
                        anyi = true;
                    }
                } else if(cn != null) {
                    if(!anyc) {
                        cproduct = anyd ? product : (double)iproduct.Num;
                        anyc = false;
                        anyd = false;
                    }
                    cproduct = NTimes(cproduct, cn);
                    anyc = true;
                } else leftover.Add(p);
            }
            Number n = anyc ? (Number)cproduct : anyd ? (Number)new DoubleNumber(product) : (Number)iproduct;
            if(leftover.Count == 0) leftover.Add(n);
            else {
                if(anyd || anyc || anyi) leftover.Insert(0, n);
            }
            return leftover;
        }

        private List<Expr> NMultipyAll(Expr[] leftover, CompositeExpr e) {
            List<Expr> result = new List<Expr>();
            int start = 0;
            int end = 1;
            while(end < leftover.Length) {
                if(leftover[start] is ArrayExpr && leftover[end] is ArrayExpr) {
                    List<Expr> product = new List<Expr>();
                    Expr[] args = new Expr[2];
                    args[0] = leftover[start];
                    args[1] = leftover[end];
                    product = this.NMultiplyArrays(args, 0, 2);
                    if(product.Count == 1) {
                        leftover[start] = product[0];
                        if(end == leftover.Length - 1)
                            result.Insert(0, leftover[start]);
                    } else {
                        if(end != leftover.Length - 1 || result.Count == 0)
                            result.AddRange(product);
                        else
                            result.Add(leftover[end]);
                        start = end + 1;
                        if(start == leftover.Length - 1)
                            result.Add(leftover[start]);
                        end++;
                    }
                } else if(leftover[start] is ArrayExpr != true && leftover[end] is ArrayExpr != true) {
                    List<Expr> product = new List<Expr>();
                    Expr[] args = new Expr[2];
                    args[0] = leftover[start];
                    args[1] = leftover[end];
                    product = this.NMultiplyScalars(args, 0, 2);

                    if(product.Count == 1) {
                        leftover[start] = product[0];
                    } else {
                        leftover[start] = new CompositeExpr(e.Head, product.ToArray());
                    }
                    if(end == leftover.Length - 1)
                        result.Insert(0, leftover[start]);

                    if(product.Count == 2)
                        result = product;
                    else {
                        leftover[start] = product[0];
                        if(end == leftover.Length - 1)
                            result.Insert(0, leftover[start]);
                    }

                } else {
                    List<Expr> product = new List<Expr>();
                    product = this.NMultiplyScalarMatrix(leftover, start, end, e);
                    leftover[start] = product[0];
                    if(end == leftover.Length - 1)
                        result.Insert(0, leftover[start]);
                }
                end++;
            }

            return result;
        }

        private List<Expr> NMultiplyScalarMatrix(Expr[] leftover, int start, int end, CompositeExpr e) {
            Expr[,] curmat = null;
            Expr[] args = new Expr[2];
            List<Expr> product = new List<Expr>();
            List<Expr> result = new List<Expr>();

            if(leftover[start] is ArrayExpr) {
                ArrayExpr array = (ArrayExpr)leftover[start];
                curmat = (Expr[,])array.Elts;
                args[0] = leftover[end];
            } else if(leftover[end] is ArrayExpr) {
                ArrayExpr array = (ArrayExpr)leftover[end];
                curmat = (Expr[,])array.Elts;
                args[0] = leftover[start];
            }

            int x = curmat.GetUpperBound(0);
            int y = curmat.GetUpperBound(1);

            for(int i = 0; i <= x; i++) {
                for(int j = 0; j <= y; j++) {
                    args[1] = curmat[i, j];
                    product = this.NMultiplyScalars(args, 0, 2);
                    if(product.Count == 1) {
                        curmat[i, j] = product[0];
                    } else {
                        curmat[i, j] = new CompositeExpr(e.Head, product.ToArray());
                    }
                }
            }
            if(curmat != null) result.Add(TempHackNewAE(curmat));
            return result;
        }

        private ComplexNumber NI { get { return new ComplexNumber(0.0, 1.0); } }
        private ComplexNumber NMinusI { get { return new ComplexNumber(0.0, -1.0); } }
        private double NRe(ComplexNumber z) { return z.Re is DoubleNumber ? ((DoubleNumber)z.Re).Num : (double)((IntegerNumber)z.Re).Num; }
        private double NIm(ComplexNumber z) { return z.Im is DoubleNumber ? ((DoubleNumber)z.Im).Num : (double)((IntegerNumber)z.Im).Num; }
        private ComplexNumber NPlus(ComplexNumber a, ComplexNumber b) {
            double ar = NRe(a);
            double ai = NIm(a);
            double br = NRe(b);
            double bi = NIm(b);
            return new ComplexNumber(ar + br, ai + bi);
        }
        private ComplexNumber NMinus(ComplexNumber a) {
            double ar = NRe(a);
            double ai = NIm(a);
            return new ComplexNumber(-ar, -ai);
        }
        private ComplexNumber NTimes(ComplexNumber a, ComplexNumber b) {
            double ar = NRe(a);
            double ai = NIm(a);
            double br = NRe(b);
            double bi = NIm(b);
            return new ComplexNumber(ar * br - ai * bi, ar * bi + ai * br);
        }
        private ComplexNumber NReciprocal(ComplexNumber a) {
            double ar = NRe(a);
            double ai = NIm(a);
            if(ai == 0 && ar == 0) throw new DivideByZeroException("Division by complex 0!");
            double denom = ar * ar + ai * ai;
            return new ComplexNumber(ar / denom, -ai / denom);
        }
        private ComplexNumber NLog(ComplexNumber a) {
            double ar = NRe(a);
            double ai = NIm(a);
            if(ar == 0 && ai == 0) throw new DivideByZeroException("Logarithm of complex 0!");
            double arg = Math.Atan2(ai, ar);
            double mag = Math.Sqrt(ar * ar + ai * ai);
            return new ComplexNumber(Math.Log(mag), arg);
        }
        private ComplexNumber NExp(ComplexNumber a) {
            double ar = NRe(a);
            double ai = NIm(a);
            double er = Math.Exp(ar);
            return new ComplexNumber(er * Math.Cos(ai), er * Math.Sin(ai));
        }
        private ComplexNumber NPower(ComplexNumber b, ComplexNumber x) {
            if(NRe(b) == 0 && NIm(b) == 0) {
                if(NRe(x) == 0 && NIm(x) == 0) return (ComplexNumber)1.0;
                else if(NRe(x) > 0) return (ComplexNumber)0.0;
                else throw new DivideByZeroException("Complex power of 0!");
            }
            return NExp(NTimes(x, NLog(b)));
        }
        private ComplexNumber NSqrt(ComplexNumber z) { return NPower(z, 0.5); }
        private ComplexNumber NSin(ComplexNumber z) {
            return NTimes(NPlus(NExp(NTimes(NI, z)), NMinus(NExp(NMinus(NTimes(NI, z))))), new ComplexNumber(0.0, -0.5));
        }
        private ComplexNumber NCos(ComplexNumber z) { return NTimes(NPlus(NExp(NTimes(NI, z)), NMinus(NExp(NMinus(NTimes(NI, z))))), 0.5); }
        private ComplexNumber NTan(ComplexNumber z) { return NTimes(NSin(z), NReciprocal(NCos(z))); }
        private ComplexNumber NSinH(ComplexNumber z) { return NTimes(NPlus(NExp(z), NMinus(NExp(NMinus(z)))), 0.5); }
        private ComplexNumber NCosH(ComplexNumber z) { return NTimes(NPlus(NExp(z), NExp(NMinus(z))), 0.5); }
        private ComplexNumber NTanH(ComplexNumber z) { return NTimes(NSinH(z), NReciprocal(NCosH(z))); }
        private ComplexNumber NArcSin(ComplexNumber z) { return NTimes(NArcSinH(NTimes(NI, z)), NMinusI); }
        private ComplexNumber NArcCos(ComplexNumber z) { return NPlus(Math.PI / 2, NMinus(NArcSin(z))); }
        private ComplexNumber NArcTan(ComplexNumber z) { return NTimes(NArcTanH(NTimes(NI, z)), NMinusI); }
        private ComplexNumber NArcSinH(ComplexNumber z) {
            return NLog(NPlus(z, NSqrt(NPlus(1.0, NTimes(z, z)))));
        }
        private ComplexNumber NArcCosH(ComplexNumber z) {
            return NTimes(2.0, NLog(NPlus(NSqrt(NTimes(NPlus(z, 1.0), 0.5)), NSqrt(NTimes(NPlus(z, -1.0), 0.5)))));
        }
        private ComplexNumber NArcTanH(ComplexNumber z) {
            return NTimes(NPlus(NLog(NPlus(1.0, z)), NLog(NPlus(1.0, NMinus(z)))), 0.5);
        }


        public Expr SubstFunctions(Expr expression, out bool changed, ref int maxRecursion) {
            changed = false;
            return expression;
        }
        private Expr _Numericize(DoubleNumber n) {
            return n;
        }
        private Expr _Numericize(IntegerNumber n) {
            return n; //  new DoubleNumber(n.Num.AsDouble());
        }
        private Expr _Numericize(RationalNumber n) {
            return new DoubleNumber(n.Num.AsDouble());
        }
        private Expr _Numericize(ComplexNumber n) {
            if(n.Im is IntegerNumber && ((IntegerNumber)n.Im).Num == 0) return Numericize(n.Re);
            if(n.Im is RationalNumber && ((RationalNumber)n.Im).Num == 0) return Numericize(n.Re);
            return new ComplexNumber((RealNumber)Numericize(n.Re), (RealNumber)Numericize(n.Im));
        }
        private Expr _Numericize(ArrayExpr e) {
            Array a = (Array)e.Elts.Clone();
            ArrayExpr ae = new ArrayExpr(a);
            foreach(int[] ix in ae.Indices) ae[ix] = Numericize(ae[ix]);
            if(!e.Annotations.Contains("Force Parentheses") || !e.Annotations["Force Parentheses"].Equals(0)) ae.Annotations["Force Parentheses"] = 1;
            return ae;
        }
        private Expr _Numericize(LetterSym s) {
            return s;
        }
        private Expr _Numericize(GroupedLetterSym s) {
            return s;
        }
        private Expr _Numericize(WordSym s) {
            return s;
        }
        private Expr _Numericize(WellKnownSym s) {
            switch(s.ID) {
                case WKSID.pi:
                    return new DoubleNumber(Math.PI);
                case WKSID.e:
                    return new DoubleNumber(Math.E);
                case WKSID.i:
                    return new ComplexNumber(new DoubleNumber(0), new DoubleNumber(1));
                default:
                    return s;
            }
        }

        private class Substituter
        {
            private Expr _orig;
            private Expr _replacement;
            private MathConstant[] _constants = null;
            private bool _pattern;
            public Substituter(Expr orig, Expr replacement) { _orig = orig; _replacement = replacement; }
            public Substituter(Expr orig, Expr replacement, bool pattern) { _orig = orig; _replacement = replacement; _pattern = pattern; }
            public Substituter(MathConstant[] consts) { _constants = consts; }
            public Expr Substitute(Expr e) {
                if ((_pattern && Object.ReferenceEquals(e, _orig)) || (!_pattern && e == _orig))
                    return _replacement;
                try {
                    if (e is CompositeExpr)
                        return _Substitute(e as CompositeExpr);
                    else if (e is Number)
                        return e;
                    return (Expr)this.GetType().InvokeMember("_Substitute", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                        null, this, new object[] { e });
                }
                catch (System.MissingMethodException) {
                    return e;
                }
                catch (System.Reflection.TargetInvocationException) {
                    return e;
                }
            }
            private Expr _Substitute(NullExpr e) { return e; }
            private Expr _Substitute(ErrorExpr e) { return e; }
            private Expr _Substitute(CompositeExpr e) {
                Expr[] args = new Expr[e.Args.Length];
                for (int i = 0; i < e.Args.Length; i++) args[i] = Substitute(e.Args[i]);
                return new CompositeExpr(Substitute(e.Head), args);
            }
            private Expr _Substitute(Number n) {
                return n;
            }
            private Expr _Substitute(ArrayExpr e) {
                Array a = (Array)e.Elts.Clone();
                ArrayExpr ae = new ArrayExpr(a);
                foreach (int[] ix in ae.Indices) ae[ix] = Substitute(ae[ix]);
                if (!e.Annotations.Contains("Force Parentheses") || !e.Annotations["Force Parentheses"].Equals(0)) ae.Annotations["Force Parentheses"] = 1;
                return ae;
            }
            private Expr _Substitute(Sym s) {
                if (s == _orig) return _replacement;
                if (_constants != null)
                    foreach (MathConstant c in _constants)
                        if (c != null && c.Name == s)
                            return c.Value;
                return s;
            }
        }
    }
}