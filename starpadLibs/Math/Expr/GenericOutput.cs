using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using starPadSDK.UnicodeNs;

namespace starPadSDK.MathExpr {
    public abstract class GenericOutput<T> 
    where T: class
    {
        // Really, I think the show invisibles stuff (and emph1paren, etc) ought to be handled in some cleaner way than by making all output routines
        // need to know about it.
        private bool _showInvisibles; public bool ShowInvisibles { get { return _showInvisibles; } set { _showInvisibles = value; } }
        protected GenericOutput(bool showinvisibles) { _showInvisibles = showinvisibles; }

        private bool _emph1paren = false;

        protected virtual T Translate(Expr e) {
            bool emph1paren = _emph1paren;
            _emph1paren = false;
            T t = (T)typeof(GenericOutput<T>).InvokeMember("_Translate", BindingFlags.InvokeMethod|BindingFlags.NonPublic|BindingFlags.Instance,
                null, this, new object[] { e });
            t = ParenAround(e, emph1paren, t);
            return t;
        }

        private T ParenAround(Expr e, bool emph1paren, T t) {
            if(e.Annotations.ContainsKey("Force Parentheses")) {
                int parens = (int)e.Annotations["Force Parentheses"];
                while(parens > 0) {
                    t = Translate_paren(t, e, parens-1, parens == 1 ? emph1paren : false);
                    parens--;
                }
            }
            return t;
        }
        public delegate T Translator(Expr e);
        public interface GenericOutputAwareExpr {
            T Translate(Translator xlator);
        }
        private T _Translate(GenericOutputAwareExpr e) {
            return e.Translate(Translate);
        }
        protected abstract T __Translate(NullExpr e);
        private T _Translate(NullExpr e) {
            return __Translate(e);
        }
        protected abstract T __Translate(ErrorMsgExpr e);
        private T _Translate(ErrorMsgExpr e) {
            return __Translate(e);
        }
        protected virtual bool __TakesInvisibleTimes { get { return false; } }
        protected T __TranslateOperator(Expr expr, Syntax.WOrC op, Syntax.TType type) { return __TranslateOperator(expr, null, op, type); }
        protected abstract T __TranslateOperator(Expr expr, object exprix, Syntax.WOrC op, Syntax.TType type);
        protected abstract T __TranslateWord(Expr expr, string op, Syntax.TType type);
        /// <summary>Handles Syntax.K.{BinLeft,BinRight,BinAlone}</summary>
        /// <param name="prec">precedence of operator</param>
        /// <param name="assoc">BinLeft, BinRight, or BinAlone</param>
        private List<T> Translate_thing(Expr head, Expr[] args, Syntax.WOrC op, Syntax.TType type, int prec, Syntax.K assoc) {
            Trace.Assert(args.Length == 2);
            List<T> lt = new List<T>();
            lt.Add(Translate_maybe_paren(args[0], prec - ((assoc == Syntax.K.BinLeft) ? 1 : 0)));
            lt.Add(__TranslateOperator(head, op, type));
            lt.Add(Translate_maybe_paren(args[1], prec - ((assoc == Syntax.K.BinRight) ? 1 : 0)));
            return lt;
        }
        /// <summary>
        /// Handles Syntax.K.{Prefix,Postfix}
        /// </summary>
        /// <param name="side">Prefix or Postfix</param>
        /// <returns></returns>
        private List<T> Translate_prepostfix(Expr head, Expr arg, Syntax.WOrC op, Syntax.TType type, int prec, Syntax.K side) {
            List<T> lt = new List<T>();
            if(side == Syntax.K.Prefix || side == Syntax.K.PrefixOpt) lt.Add(__TranslateOperator(head, op, type));
            lt.Add(Translate_maybe_paren(arg, prec - 1));
            if(side == Syntax.K.Postfix) lt.Add(__TranslateOperator(head, op, type));
            return lt;
        }
        /// <summary>Handles Syntax.K.BinAllOfLike</summary>
        /// <param name="prec">precedence of operator</param>
        private List<T> Translate_multithing(Expr bin, Expr[] args, Syntax.WOrC op, Syntax.TType type, int prec) {
            Trace.Assert(args.Length >= 2);
            List<T> lt = new List<T>();
            lt.Add(Translate_maybe_paren(args[0], prec));
            for(int i = 1; i < args.Length; i++) {
                lt.Add(__TranslateOperator(bin, i-1, op, type));
                lt.Add(Translate_maybe_paren(args[i], prec));
            }
            return lt;
        }
        private List<T> Translate_arglist(Expr ce, Expr[] args) {
            List<T> lt = new List<T>();
            for(int i = 0; i < args.Length; i++) {
                lt.Add(Translate(args[i]));
                if(i != args.Length-1) {
                    lt.Add(__TranslateOperator(ce, i, ',', Syntax.TType.Punct));
                }
            }
            return lt;
        }
        private T Translate_paren(T t, Expr e, int level) {
            return Translate_paren(t, e, level, false);
        }
        protected abstract T __TranslateDelims(Expr e, bool emph, object lexprix, char l, T t, object rexprix, char r);
        private T Translate_paren(T t, Expr e, int level, bool emph) {
            return __TranslateDelims(e, emph, "l"+level, '(', t, "r"+level, ')');
        }
        private T Translate_definite_paren(Expr e) {
            return Translate_definite_paren(e, false);
        }
        private T Translate_definite_paren(Expr e, bool emph) {
            if(e.Annotations.ContainsKey("Force Parentheses")) {
                _emph1paren = emph;
                return Translate(e);
            } else return Translate_paren(Translate(e), e, 0, emph);
        }
        private T Translate_maybe_parenfn(Expr e) {
            return Translate_maybe_parenfn(e, false);
        }
        /// <summary>
        /// Parenthesize for something like a function's single argument
        /// </summary>
        private T Translate_maybe_parenfn(Expr e, bool emph) {
            CompositeExpr ce = e as CompositeExpr;
            if(ce != null && ce.Head != WellKnownSym.root && ce.Head != WellKnownSym.subscript) {
                return Translate_definite_paren(e, emph);
            } else {
                if(e.Annotations.ContainsKey("Force Parentheses")) _emph1paren = emph;
                return Translate(e);
            }
        }
        /// <param name="prectoparen">parenthesize stuff w prec ≤ prectoparen</param>
        private bool Needs_paren(Expr e, int prectoparen) {
            CompositeExpr ce = e as CompositeExpr;
            int prec;
            if(ce != null) {
                prec = Syntax.Fixes.Find(ce.Head).Precedence;
                if(prec == -1) prec = int.MaxValue; // function calls, power, subscript, and root have highest precedence
            } else {
                prec = (e is ComplexNumber) ? Syntax.Fixes.Find(WellKnownSym.plus).Precedence
                : (e is IntegerNumber) && ((IntegerNumber)e).Num < 0 ? Syntax.Fixes.Find(WellKnownSym.minus).Precedence
                : (e is DoubleNumber) && ((DoubleNumber)e).Num < 0 ? Syntax.Fixes.Find(WellKnownSym.minus).Precedence
                : (e is DoubleNumber) && ((DoubleNumber)e).Num.ToString("R").IndexOfAny(new char[] { 'e', 'E' }) >= 0 ? Syntax.Fixes.Find(WellKnownSym.plus).Precedence
                : int.MaxValue; // assuming everything else is an atom of one sort or another. Possibly wrong, for user-defined Exprs
            }
            if(prec <= prectoparen) return true;
            if(ce != null) {
                Syntax.OpRelPos orp = Syntax.Fixes.Find(ce.Head);
                if(orp.Precedence != -1 && ce.Args.Length > 0) {
                    Syntax.K k = Syntax.Fixes.Table[orp.Precedence].Kind;
                    if(k == Syntax.K.Prefix || k == Syntax.K.PrefixOpt) return false;
                    if(k == Syntax.K.BinPrimaryAndSecondary2) {
                        // might be an operator at the start
                        if(ce.Args[0].Annotations["initial op"] != null) return false;
                        CompositeExpr ce2 = ce.Args[0] as CompositeExpr;
                        if(ce2 != null && Syntax.Fixes.Find(ce2.Head).Precedence == orp.Precedence) return false;
                    }
                    return Needs_paren(ce.Args[0], prectoparen);
                }
            }
            return false;
        }
        /// <summary>Parenthesize for precedence</summary>
        /// <param name="prectoparen">parenthesize stuff w prec ≤ prectoparen</param>
        private T Translate_maybe_paren(Expr e, int prectoparen) {
            return Needs_paren(e, prectoparen) ? Translate_definite_paren(e) : Translate(e);
        }
        private bool NumberStartsWithMinus(Expr e) {
            if (e.Head() == WellKnownSym.minus)
                return true;
            if (e.Head() == WellKnownSym.plus && !e.Annotations.ContainsKey("Force Parentheses"))
                return NumberStartsWithMinus(e.Args()[0]);
            ComplexNumber cn = e as ComplexNumber;
            if((e is IntegerNumber && (e as IntegerNumber).Num < 0)
                || (e is DoubleNumber && (e as DoubleNumber).Num < 0))
                return true;
            else if(cn != null && ((cn.Re is IntegerNumber && (cn.Re as IntegerNumber).Num < 0)
                                   || (cn.Re is DoubleNumber && (cn.Re as DoubleNumber).Num < 0)))
                return true;
            else return false;
        }
        protected abstract T __WrapTranslatedExpr(Expr expr, List<T> lt);
        protected T __WrapTranslatedExpr(Expr expr, params T[] ts) { return __WrapTranslatedExpr(expr, new List<T>(ts)); }
        protected T __TranslateVerticalFraction(Expr e, T num, T den) { return __TranslateVerticalFraction(e, null, num, den); }
        protected abstract T __TranslateVerticalFraction(Expr e, Expr divlineexpr, T num, T den);
        protected abstract T __TranslateBigOp(Expr wholeexpr, Expr opexpr, char op, T lowerlimit, T upperlimit, T contents);
        protected abstract T __TranslateFunctionApplication(Expr e, T fn, T args);
        protected abstract T __TranslateOperatorApplication(Expr e, T op, T args);
        protected abstract T __AddSuperscript(Expr e, T nuc, T sup);
        protected abstract T __AddSubscript(Expr e, T nuc, T sub);
        protected abstract T __TranslateRadical(Expr e, T radicand, T index);
        protected abstract T __TranslateIntegralInternals(T integrand, T dxthing);
        private T _Translate(CompositeExpr e) {
            List<T> lt; // for local use because, sigh, c# doesn't handle this case of definitions used or duplicated across switch cases well
            T t; // this too, sigh
            Syntax.OpRelPos orp = Syntax.Fixes.Find(e.Head);
            if(orp.Precedence != -1) {
                Syntax.OpRel or = Syntax.Fixes.Table[orp.Precedence];
                Syntax.WOrC op = or.Ops[orp.Position];
                Syntax.TType tt = or.Types[orp.Position];
                switch(or.Kind) {
                    case Syntax.K.BinLeft:
                    case Syntax.K.BinRight:
                    case Syntax.K.BinAlone:
                        Trace.Assert(e.Args.Length == 2);
                        return __WrapTranslatedExpr(e, Translate_thing(e.Head, e.Args, op, tt, orp.Precedence, or.Kind));
                    case Syntax.K.BinAllOfLike:
                        Trace.Assert(e.Args.Length >= 2);
                        return __WrapTranslatedExpr(e, Translate_multithing(e, e.Args, op, tt, orp.Precedence));
                    case Syntax.K.BinPrimaryAndSecondary:
                        if(e.Head != or.Heads[0]) {
                            Trace.Assert(e.Args.Length == 1);
                            // Division is a special case because it can be represented as the vertical fraction representation, which is not dealt with
                            // in our syntax table.
                            if(e.Head == WellKnownSym.divide && !e.Annotations.ContainsKey("inline")) {
                                Expr int1 = IntegerNumber.One;
                                int1.Annotations.Add("implicit", true);
                                return __TranslateVerticalFraction(e, Translate(int1), Translate(e.Args[0]));
                            } else {
                                lt = new List<T>();
                                if(or.Kind == Syntax.K.BinPrimaryAndSecondary) lt.Add(Translate_maybe_paren(or.Identity, orp.Precedence));
                                lt.Add(__TranslateOperator(e.Head, op, tt));
                                lt.Add(Translate_maybe_paren(e.Args[0], orp.Precedence));
                                return __WrapTranslatedExpr(e, lt);
                            }
                        } else {
                            // FIXME!!!! currently this only works for times and divide; ignores Syntax! Ideally ought to be merged w BinPriAndSec2
                            Trace.Assert(e.Args.Length > 0);
                            if(e.Args.Length == 1) {
                                return Translate(e.Args[0]);
                            } else {
                                List<Expr> num = new List<Expr>();
                                List<CompositeExpr> denom = new List<CompositeExpr>();
                                List<CompositeExpr> denominline = new List<CompositeExpr>();
                                foreach(Expr ee in e.Args) {
                                    CompositeExpr ce = ee as CompositeExpr;
                                    if(ce != null && ce.Head == WellKnownSym.divide) {
                                        Trace.Assert(ce.Args.Length == 1);
                                        if(ce.Annotations.ContainsKey("inline")) {
                                            denominline.Add(ce);
                                        } else denom.Add(ce);
                                    } else num.Add(ee);
                                }
                                lt = new List<T>();
                                if (num.Count > 1 || (denom.Count == 0 && denominline.Count > 0)) AddTimes(lt, num);
                                else {
                                    if (num.Count > 0) lt.Add(Translate(num[0]));
                                }
                                if (denom.Count != 0) {
                                    Expr int1 = IntegerNumber.One;
                                    int1.Annotations.Add("implicit", true);
                                    if(num.Count == 0) lt.Add(Translate(int1));
                                    T nt = __WrapTranslatedExpr(null, lt);
                                    lt = new List<T>();
                                    if(denom.Count > 1) {
                                        List<Expr> denomraw = new List<Expr>();
                                        foreach(CompositeExpr ce in denom) denomraw.Add(ce.Args[0]);
                                        AddTimes(lt, denomraw);
                                    } else lt.Add(Translate(denom[0].Args[0]));
                                    T dt = __WrapTranslatedExpr(null, lt);
                                    t = __TranslateVerticalFraction(e, denom[0].Head, nt, dt);
                                } else {
                                    Expr int1 = IntegerNumber.One;
                                    int1.Annotations.Add("implicit", true);
                                    if(num.Count == 0) t = Translate(int1);
                                    else t = __WrapTranslatedExpr(e, lt);
                                }
                                if(denominline.Count > 0) {
                                    lt = new List<T>();
                                    lt.Add(t);
                                    foreach(CompositeExpr division in denominline) {
                                        lt.Add(__TranslateOperator(division.Head, '/', Syntax.TType.Ord));
                                        lt.Add(Translate(division.Args[0]));
                                    }
                                    return __WrapTranslatedExpr(e, lt);
                                } else return t;
                            }
                        }
                    case Syntax.K.BinPrimaryAndSecondary2:
                        if(e.Head != or.Heads[0]) {
                            Trace.Assert(e.Args.Length == 1);
                            return __TranslateOperatorApplication(e, __TranslateOperator(e.Head, op, tt), Translate_maybe_paren(e.Args[0], orp.Precedence));
                        } else {
                            Trace.Assert(e.Args.Length > 0);
                            lt = new List<T>();
                            if(e.Annotations["initial op"] != null) lt.Add(__TranslateOperator(e, 0, (char)e.Annotations["initial op"], tt));
                            bool checkminus = (Syntax.Fixes.Find(WellKnownSym.minus).Precedence == orp.Precedence);
                            if (checkminus && !(e.Args[0] is CompositeExpr) && NumberStartsWithMinus(e.Args[0])) { // bcz: turn negative numbers into '-' signs and positive numbers
                                var mt = new List<T>();
                                mt.Add(__TranslateOperator(e, 0, Unicode.M.MINUS_SIGN, Syntax.TType.Op));
                                _suppressDominus = true;
                                mt.Add(Translate(e.Args[0] as Number));
                                _suppressDominus = false;
                                lt.Add(__WrapTranslatedExpr(null, mt));
                            } else
                                lt.Add(Translate_maybe_paren(e.Args[0], orp.Precedence - 1));
                            for(int i = 1; i < e.Args.Length; i++) {
                                CompositeExpr ce = e.Args[i] as CompositeExpr;
                                Syntax.OpRelPos orp2 = new Syntax.OpRelPos(-1, -1);
                                if(ce != null) orp2 = Syntax.Fixes.Find(ce.Head);
                                if (checkminus && NumberStartsWithMinus(e.Args[i])) {
                                    if (e.Args[i].Head() == WellKnownSym.minus) {
                                        var mt = new List<T>();
                                        mt.Add(__TranslateOperator(e, i, Unicode.M.MINUS_SIGN, Syntax.TType.Op));
                                        mt.Add(Translate_maybe_paren(e.Args[i].Args()[0], orp.Precedence));
                                        lt.Add(__WrapTranslatedExpr(null, mt));
                                    } else if (e.Args[i] is CompositeExpr) {
                                        lt.Add(Translate(e.Args[i]));
                                    }
                                    else {  // bcz: turn negative numbers into '-' signs and positive numbers
                                        var mt = new List<T>();
                                        mt.Add(__TranslateOperator(e, i, Unicode.M.MINUS_SIGN, Syntax.TType.Op));
                                        _suppressDominus = true;
                                        mt.Add(Translate(e.Args[i] as Number));
                                        _suppressDominus = false;
                                        lt.Add(__WrapTranslatedExpr(null, mt));
                                    }
                                    //else lt.Add(Translate(e.Args[i]));
                                } 
                                else if (ce != null && orp2.Precedence == orp.Precedence
                                    && or.Heads[orp.Position] != or.Heads[orp2.Position]) {
                                    Trace.Assert(ce.Args.Length == 1);
                                    var mt = new List<T>();  // bcz: I wrapped these so that minus terms are encapsulated as thing
                                    mt.Add(__TranslateOperator(e, i, or.Ops[orp2.Position], tt));
                                    mt.Add(Translate_maybe_paren(ce.Args[0], orp.Precedence));
                                    lt.Add(__WrapTranslatedExpr(null, mt));
                                }
                                else {
                                    var mt = new List<T>();   // bcz: I wrapped these so that each term of an addition is encapsulated as thing
                                    mt.Add(__TranslateOperator(e, i, op, tt));
                                    mt.Add(Translate_maybe_paren(e.Args[i], orp.Precedence - 1));
                                    lt.Add(__WrapTranslatedExpr(null, mt));
                                }
                            }
                            return __WrapTranslatedExpr(e, lt);
                        }
                    case Syntax.K.PrefixOpt:
                    case Syntax.K.Prefix:
                        if(tt == Syntax.TType.LargeOp && op.Word == null) { // Only summation? This seems oddly hard-coded.
                            Trace.Assert(0 < e.Args.Length && e.Args.Length < 4);
                            return __TranslateBigOp(e, e.Head, op.Character, e.Args.Length > 1 ? Translate(e.Args[0]) : null, e.Args.Length > 2 ? Translate(e.Args[1]) : null,
                                Translate_maybe_paren(e.Args[e.Args.Length-1], orp.Precedence - 1));
                        } else if(or.Kind == Syntax.K.PrefixOpt && e.Args.Length == 0) {
                            return __TranslateOperator(e.Head, op, tt);
                        } else {
                            //Trace.Assert(e.Args.Length == 1);//CJ disabled temporarily for site visit demo
                            return __WrapTranslatedExpr(e, Translate_prepostfix(e.Head, e.Args[0], op, tt, orp.Precedence, or.Kind));
                        }
                    case Syntax.K.Postfix:
                        Trace.Assert(e.Args.Length == 1);
                        return __WrapTranslatedExpr(e, Translate_prepostfix(e.Head, e.Args[0], op, tt, orp.Precedence, or.Kind));
                }
            }
            if(e.Head is WellKnownSym) {
                WellKnownSym wks = (WellKnownSym)e.Head;
                WKSID id = wks.ID;
                switch(id) {
                    case WKSID.im:
                        Trace.Assert(e.Args.Length == 1);
                        return __TranslateFunctionApplication(e, __TranslateOperator(e.Head, Unicode.B.BLACK_LETTER_CAPITAL_I, Syntax.TType.Ord),
                            Translate_maybe_parenfn(e.Args[0], _showInvisibles));
#if false
                    case WKSID.magnitude:
                        Trace.Assert(e.Args.Length == 1);
                        return __TranslateDelims(e, false, 0, '|', Translate(e.Args[0]), 1, '|');
#endif
                    case WKSID.magnitude:
                        if(e.Args.Length == 1) {
                            return __TranslateDelims(e, false, "|l", '|',
                                __WrapTranslatedExpr(null, Translate(e.Args[0])),
                                "|r", '|');
                        }
                        break;
                    case WKSID.power:
                        Trace.Assert(e.Args.Length == 2);
                        if(e.Args[0] is CompositeExpr && ((CompositeExpr)e.Args[0]).Head == WellKnownSym.power) t = Translate_definite_paren(e.Args[0]);
                        else t = Translate_maybe_parenfn(e.Args[0]);
                        return __AddSuperscript(e, t, Translate(e.Args[1]));
                    case WKSID.subscript:
                        Trace.Assert(e.Args.Length == 2);
                        if(e.Args[0] is CompositeExpr && (((CompositeExpr)e.Args[0]).Head == WellKnownSym.power
                            || ((CompositeExpr)e.Args[0]).Head == WellKnownSym.subscript)) t = Translate_definite_paren(e.Args[0]);
                        else t = Translate_maybe_parenfn(e.Args[0]);
                        return __AddSubscript(e, t, Translate(e.Args[1]));
                    case WKSID.re:
                        Trace.Assert(e.Args.Length == 1);
                        return __TranslateFunctionApplication(e, __TranslateOperator(e.Head, Unicode.B.BLACK_LETTER_CAPITAL_R, Syntax.TType.Ord),
                            Translate_maybe_parenfn(e.Args[0], _showInvisibles));
                    case WKSID.root:
                        if(e.Args[0] is IntegerNumber && (e.Args[0] as IntegerNumber).Num == 2) {
                            return __TranslateRadical(e, Translate(e.Args[1]), null);
                        } else return __TranslateRadical(e, Translate(e.Args[1]), Translate(e.Args[0]));
                    case WKSID.differentiald:
                    case WKSID.partiald:
                        Trace.Assert(e.Args.Length == 1);
                        if(e.Args[0] is Sym) return __TranslateOperatorApplication(e, Translate(e.Head), Translate(e.Args[0]));
                        else return __TranslateOperatorApplication(e, Translate(e.Head), Translate_definite_paren(e.Args[0]));
                    case WKSID.integral:
                        Trace.Assert(e.Args.Length >= 2 && e.Args.Length <= 4);
                        return __TranslateBigOp(e, e.Head, Unicode.I.INTEGRAL, e.Args.Length > 2 ? Translate(e.Args[2]) : null,
                            e.Args.Length > 3 ? Translate(e.Args[3]) : null,
                            __TranslateIntegralInternals(Translate(e.Args[0]), Translate(e.Args[1])));
                    case WKSID.log:
                        if(e.Args.Length == 2) {
                            T f = __TranslateWord(wks, wks.ID.ToString(), Syntax.TType.LargeOp);
                            f = __AddSubscript(null, f, Translate(e.Args[0]));
                            f = ParenAround(e.Head, false, f);
                            return __TranslateFunctionApplication(e, f, Translate_maybe_parenfn(e.Args[1], _showInvisibles));
                        }
                        break;
                    case WKSID.floor:
                        if(e.Args.Length == 1) {
                            return __TranslateDelims(e, false, Unicode.L.LEFT_FLOOR.ToString(), Unicode.L.LEFT_FLOOR,
                                __WrapTranslatedExpr(null, Translate(e.Args[0])),
                                Unicode.R.RIGHT_FLOOR.ToString(), Unicode.R.RIGHT_FLOOR);
                        }
                        break;
                    case WKSID.ceiling:
                        if(e.Args.Length == 1) {
                            return __TranslateDelims(e, false, Unicode.L.LEFT_CEILING.ToString(), Unicode.L.LEFT_CEILING,
                                __WrapTranslatedExpr(null, Translate(e.Args[0])),
                                Unicode.R.RIGHT_CEILING.ToString(), Unicode.R.RIGHT_CEILING);
                        }
                        break;
                }
            } else if(e.Head == new LetterSym('→') || e.Head == new LetterSym(Unicode.R.RIGHTWARDS_DOUBLE_ARROW)) {
                T arrow = __TranslateOperator(e.Head, ((LetterSym)e.Head).Letter, Syntax.TType.Rel);
                if(e.Args.Length == 1) {
                    return __WrapTranslatedExpr(e, Translate(e.Args[0]), arrow);
                } else {
                    Trace.Assert(e.Args.Length == 2);
                    return __WrapTranslatedExpr(e, Translate(e.Args[0]), arrow, Translate(e.Args[1]));
                }
            } else if(e.Head == new WordSym("brace")) {
                return __TranslateDelims(e, false, "{", '{', __WrapTranslatedExpr(null, Translate_arglist(e, e.Args)), "}", '}');
            } else if(e.Head == new WordSym("bracket")) {
                return __TranslateDelims(e, false, "[", '[', __WrapTranslatedExpr(null, Translate_arglist(e, e.Args)), "]", ']');
            }
            // FIXME: probably both branches here should use la and ra...
            if(e.Args.Length == 1) {
                if(e.Head is WellKnownSym) {
                    return __TranslateFunctionApplication(e, Translate(e.Head), Translate_maybe_parenfn(e.Args[0], _showInvisibles));
                } else {
                    return __TranslateFunctionApplication(e, Translate_maybe_parenfn(e.Head), Translate_definite_paren(e.Args[0], _showInvisibles));
                }
            } else return __TranslateFunctionApplication(e, Translate_maybe_parenfn(e.Head),
                __TranslateDelims(e, _showInvisibles, "la", '(', __WrapTranslatedExpr(null, Translate_arglist(e, e.Args)), "ra", ')'));
        }

        private void AddTimes(List<T> lt, List<Expr> exprs) {
            bool wasnum = false;
            foreach(Expr ee in exprs) {
                // we expand Translate_maybe_paren here
                bool needsparen = Needs_paren(ee, Syntax.Fixes.Find(WellKnownSym.times).Precedence - 1);
                if(ee.Annotations.Contains("dot before")) lt.Add(__TranslateOperator(ee, "dot before", Unicode.D.DOT_OPERATOR, Syntax.TType.Op));
                else if(wasnum && !needsparen && StartsWithNumber(ee)) lt.Add(__TranslateOperator(null, Unicode.D.DOT_OPERATOR, Syntax.TType.Op));
                else if(__TakesInvisibleTimes && lt.Count > 0) lt.Add(__TranslateOperator(null, Unicode.I.INVISIBLE_TIMES, Syntax.TType.Op));
                lt.Add(needsparen ? Translate_definite_paren(ee) : Translate(ee));
                wasnum = ee is Number && !ee.Annotations.ContainsKey("Force Parentheses") && !needsparen;
            }
        }
        private bool StartsWithNumber(Expr ee) {
            if(ee.Annotations.ContainsKey("Force Parentheses")) return false;
            if(ee is Number) return true;
            CompositeExpr ce = ee as CompositeExpr;
            if(ce == null) return false;
            WellKnownSym wks = ce.Head as WellKnownSym;
            if(wks == null) return false;
            if(wks == WellKnownSym.power || wks == WellKnownSym.subscript
                || Syntax.Fixes.Find(wks).Precedence >= Syntax.Fixes.Find(WellKnownSym.times).Precedence) return StartsWithNumber(ce.Args[0]);
            else return false;
        }
        private bool _suppressDominus = false;
        /// <summary>
        /// determines whether the dominus ('-') should be rendered when a Number is output.
        /// when a number is part of a sum, then the dominus should not be displayed because the sum operator will draw it.
        /// this is a hack, and should be replaced by some more general mechanism for formatting numbers & expressions
        /// </summary>
        public bool SuppressDominus { get { return _suppressDominus; } set { _suppressDominus = value; } }
        protected abstract T __Translate(DoubleNumber n);
        protected abstract T __TranslateNumber(Expr e, string n);
        private T _Translate(DoubleNumber n) {
            string chars = n.Annotations["chars"] as string;
            if(chars != null) {
                return __TranslateNumber(n, chars);
            } else return __Translate(n);
        }
        private T _Translate(IntegerNumber n) {
            string chars = n.Annotations["chars"] as string;
            bool dominus = false;
            if(chars == null) {
                chars = n.Num.ToString();
                dominus = chars[0] == '-';
                if(dominus) chars = chars.Substring(1, chars.Length-1);
            }
            if (_suppressDominus)
                dominus = false;
            T t = __TranslateNumber(dominus ? null : n, chars);
            if(dominus) return __WrapTranslatedExpr(n, __TranslateOperator(null, Unicode.M.MINUS_SIGN, Syntax.TType.Op), t);
            else return t;
        }
        private T _Translate(ComplexNumber n) {
            IntegerNumber ri = n.Re as IntegerNumber;
            RationalNumber rr = n.Re as RationalNumber;
            IntegerNumber ii = n.Im as IntegerNumber;
            RationalNumber ir = n.Im as RationalNumber;
            List<T> ts = new List<T>();
            if(!((ri != null && ri.Num == 0) || (rr != null && rr.Num == 0))) {
                ts.Add(Translate(n.Re));
            }
            int? isgn = null;
            bool? isone = null;
            bool special = false;
            if(ii != null) {
                isgn = ii.Num > 0 ? 1 : ii.Num < 0 ? -1 : 0;
                isone = ii.Num == 1 || ii.Num == -1;
                special = true;
            } else if(ir != null) {
                isgn = ir.Num > 0 ? 1 : ir.Num < 0 ? -1 : 0;
                isone = ir.Num == 1 || ir.Num == -1;
                special = true;
            }
            if(special) {
                if(isgn == 0) {
                    if(ts.Count == 0) ts.Add(Translate(n.Re));
                } else {
                    if(isone.Value) {
                        if(isgn == 1) {
                            if(ts.Count != 0) ts.Add(__TranslateOperator(null, '+', Syntax.TType.Op));
                        } else ts.Add(__TranslateOperator(null, Unicode.M.MINUS_SIGN, Syntax.TType.Op));
                    } else {
                        if(isgn == 1 && ts.Count != 0) ts.Add(__TranslateOperator(null, '+', Syntax.TType.Op));
                        ts.Add(Translate(n.Im));
                    }
                    ts.Add(Translate(WellKnownSym.i));
                }
            } else {
                DoubleNumber dn = (DoubleNumber)n.Im;
                string im = dn.ToString();
                if(im[0] != '-' && ts.Count != 0) ts.Add(__TranslateOperator(null, '+', Syntax.TType.Op));
                ts.Add(Translate(n.Im));
                ts.Add(Translate(WellKnownSym.i));
            }
            return __WrapTranslatedExpr(n, ts);
        }
        private T _Translate(RationalNumber n) {
            T num = __TranslateNumber(null, n.Num.Num.ToString());
            T den = __TranslateNumber(null, n.Num.Denom.ToString());
            if(n.Annotations.ContainsKey("inline")) return __WrapTranslatedExpr(n, num, __TranslateOperator(null, '/', Syntax.TType.Ord), den);
            else return __TranslateVerticalFraction(n, num, den);
        }
        protected abstract T __Translate(ArrayExpr e);
        private T _Translate(ArrayExpr e) {
            return __Translate(e);
        }
        protected abstract T __Translate(LetterSym s);
        private T _Translate(LetterSym s) {
            return __Translate(s);
        }
        private T _Translate(GroupedLetterSym s) {
            /* TODO: need to handle accent */
            List<T> lt = new List<T>();
            foreach(LetterSym ls in s.Letters) lt.Add(Translate(ls));
            return __WrapTranslatedExpr(s, lt);
        }
        protected abstract T __Translate(WordSym s);
        private T _Translate(WordSym s) {
            return __Translate(s);
        }
        private T _Translate(WellKnownSym s) {
            switch(s.ID) {
                case WKSID.del:
                    return __TranslateOperator(s, Unicode.N.NABLA, Syntax.TType.Ord);
                case WKSID.differentiald:
                    return __TranslateOperator(s, Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_D, Syntax.TType.Ord);
                case WKSID.partiald:
                    return __TranslateOperator(s, Unicode.P.PARTIAL_DIFFERENTIAL, Syntax.TType.Ord);
                case WKSID.integral:
                    return __TranslateOperator(s, Unicode.I.INTEGRAL, Syntax.TType.LargeOp);
                case WKSID.i:
                    return __TranslateOperator(s, Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_I, Syntax.TType.Ord);
                case WKSID.e:
                    return __TranslateOperator(s, Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_E, Syntax.TType.Ord);
                default:
                    KeyValuePair<Syntax.WOrC, Syntax.TType>? val;
                    val = Syntax.CharWKSMap[s];
                    if(val == null) {
                        Syntax.OpRelPos orp = Syntax.Fixes.Find(s);
                        if(orp.Precedence == -1) return __TranslateWord(s, s.ID.ToString(), Syntax.TType.LargeOp);
                        else {
                            Syntax.OpRel or = Syntax.Fixes.Table[orp.Precedence];
                            Syntax.WOrC op = or.Ops[orp.Position];
                            Syntax.TType tt = or.Types[orp.Position];
                            return __TranslateOperator(s, op, tt);
                        }
                    } else return __TranslateOperator(s, val.Value.Key, val.Value.Value);
            }
        }
    }
}
