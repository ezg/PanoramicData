using System;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using starPadSDK.Geom;

namespace starPadSDK.MathExpr
{
    static public class ExprOps {
        static public Expr FindFactorTerm(Expr e) {
            if (e.Annotations.Contains("Factor"))
                return e;
            if (e is CompositeExpr)
                foreach (Expr e1 in (e as CompositeExpr).Args) {
                    Expr found = FindFactorTerm(e1);
                    if (found != null)
                        return found;
                }

            return null;
        }

        static public Expr DoFactorManipulation(Expr topLevel, ref Expr target, int colorInd) {
            // find the path in the expression tree from the root to the selected factor term
            List<Expr> selPath = Expr.FindPath(topLevel, target);

            // setup the values needed to do some factoring
            Expr unfactoredExpr = selPath[0];   // the expression to be factored
            Expr factoredExpr = selPath[0];       // the resulting factored expression
            Expr factorTerm = selPath[0]; // the term to factor out
            TermColorizer.MarkFactor(factorTerm, colorInd);  // mark the term that should be factored -- there's no other way to know what to factor since the same term might be used elsewhere

            // iterate up the expression tree, factoring out the factorTerm at each step and then substituting the factored expression in the
            // next higher level expression in the tree
            foreach (Expr curTerm in selPath)
                if (curTerm != null) {
                    Expr factored = (unfactoredExpr != factoredExpr) ? Engine.Replace(curTerm, unfactoredExpr, factoredExpr) : curTerm;
                    factoredExpr = ExprTransform.FactorOut(factored, factorTerm, true);
                    for (Expr preFactoredExpr = factored; true; preFactoredExpr = factoredExpr) {
                        factoredExpr = ExprTransform.FactorOut(preFactoredExpr, factorTerm, false);
                        double dragX = 0;
                        factoredExpr = FactorIntoProduct(new Rct(100000, 100000, 10, 10), colorInd, preFactoredExpr, ref factorTerm, ref dragX, false);
                        if (preFactoredExpr == factoredExpr)
                            break;
                        if (factoredExpr.Head() == WellKnownSym.times && factoredExpr.Args()[0].Head() == WellKnownSym.divide &&
                            factoredExpr.Args()[0].Args()[0] == factorTerm) {
                                TermColorizer.ClearFactorMarks(factoredExpr, -1);
                            factorTerm = new CompositeExpr(WellKnownSym.times, 1, factoredExpr.Args()[0]);
                            factoredExpr = Engine.Replace(factoredExpr, factoredExpr.Args()[0], factorTerm);
                            TermColorizer.MarkFactor(factorTerm, colorInd);
                        }
                    }
                    unfactoredExpr = curTerm; // save original version of term for next iteration
                }
            target = factorTerm;
            return factoredExpr;
        }
        static public Expr FactorIntoProduct(Rct box, int colorInd, Expr factoredExpr, ref Expr factorTerm, ref double dragX, bool allowReorder) {
            double shift = box.Left > dragX ? 10 : -10;
            for (Expr preFactoredExpr = factoredExpr; (shift > 0 && box.Left > dragX) || (shift < 0 && box.Right < dragX); preFactoredExpr = factoredExpr) {
                factoredExpr = ExprTransform.FactorOut(preFactoredExpr, factorTerm, !allowReorder);
                if (preFactoredExpr == factoredExpr)
                    break;
                else dragX += shift;
                factorTerm = updateFactorTermIfInverted(colorInd, ref factoredExpr, factorTerm);
            }
            return factoredExpr;
        }

        static Expr updateFactorTermIfInverted(int colorInd, ref Expr factoredExpr, Expr factorTerm) {
            Expr newFactorTerm = factorTerm;
            if (factoredExpr.Head() == WellKnownSym.times && factoredExpr.Args()[0].Head() == WellKnownSym.divide && factoredExpr.Args()[0].Args()[0] == factorTerm) {
                TermColorizer.ClearFactorMarks(factoredExpr, -1);
                newFactorTerm = new CompositeExpr(WellKnownSym.times, 1, factoredExpr.Args()[0]);
                factoredExpr = Engine.Replace(factoredExpr, factoredExpr.Args()[0], newFactorTerm);
                TermColorizer.MarkFactor(newFactorTerm, colorInd);
            }
            if (factoredExpr.Head() == WellKnownSym.times && factoredExpr.Args()[0].Head() == WellKnownSym.root && factoredExpr.Args()[0].Args()[1] == factorTerm && factoredExpr.Args()[1].Head() == WellKnownSym.divide && factoredExpr.Args()[1].Args()[0] == factorTerm) {
                TermColorizer.ClearFactorMarks(factoredExpr, -1);
                newFactorTerm = factoredExpr.Args()[1];
                factoredExpr = Engine.Replace(factoredExpr, factoredExpr.Args()[1], newFactorTerm);
                TermColorizer.MarkFactor(newFactorTerm, colorInd);
            }
            return newFactorTerm;
        }
        /// <summary>
        /// Joins all the terms between start and end (or end and start if they're reversed) and simplifies the result
        /// e.g.,  Join( 2x+3x+4x+5x+6x  ,  3x, 5x) => 2x + 12x + 6x
        /// e.g.,  Join(a* x*x^2*y,  x, x^2) =>a*x^3*y
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        static public Expr JoinRange(Expr expr, Expr start, Expr end, int colorInd, bool expand) {
            Expr foo = ExprTransform.FlattenSums(expr);
            CompositeExpr compExpr = expr as CompositeExpr;
            try {
                if (compExpr == null)
                    return expr;

                int ind1 = 0;
                int ind2 = 0;
                CompositeExpr ancestor = null;

                if (!object.ReferenceEquals(start, end))
                    ancestor = Expr.FindCommonAncestor(compExpr, start, end, out ind1, out ind2);
                else if (start is CompositeExpr) {
                    ancestor = start as CompositeExpr;
                    ind1 = 0;
                    ind2 = ancestor.Args.Length - 1;
                }
                if (ancestor == null)
                    return compExpr;

                if (ind1 > ind2) { // swap start and end if end comes first
                    int temp = ind1; ind1 = ind2; ind2 = temp;
                    Expr temp2 = start; start = end; end = temp2;
                }

                List<Expr> args = new List<Expr>();
                for (int i = ind1; i <= ind2; i++)
                    args.Add(ancestor.Args()[i]);

                Expr[] chosenTerms = new Expr[] { start, end };
                for (int i = 0; i < chosenTerms.Length; i++) {
                    foreach (Expr plusTerm in ancestor.Args())
                        if (Expr.FindPath(plusTerm, chosenTerms[i]).Count != 0)
                            TermColorizer.MarkFactor(plusTerm, colorInd);
                }
                Expr subst = Join(new CompositeExpr(ancestor.Head, args.ToArray()), chosenTerms, colorInd, expand);
                TermColorizer.MarkFactor(subst, colorInd);

                // make a list of all the new terms (having replaced the joined terms into a single term)
                if (ind1 != 0 || ind2 != ancestor.Args().Length - 1) {
                    List<Expr> modifiedArgs = new List<Expr>();
                    for (int i = 0; i < ind1; i++) modifiedArgs.Add(ancestor.Args()[i]);                                      // unchanged initial terms
                    modifiedArgs.Add(subst);                                                                                                 // the result of joining terms
                    for (int i = ind2 + 1; i < ancestor.Args().Length; i++) modifiedArgs.Add(ancestor.Args()[i]); // unchanged final terms
                    subst = new CompositeExpr(ancestor.Head, modifiedArgs.ToArray());
                }
                Expr replaced = ExprTransform.FlattenMults(Engine.Replace(compExpr, ancestor, subst));
                replaced = ExprTransform.FlattenSums(replaced);
                return replaced;  // finally, replace the modified ancestor expression in the top level expression
            }
            catch {
                return compExpr;
            }
        }

        static public Expr Join(Expr preSubst, Expr[] chosenTerms, int colorInd, bool expand) {
            if (preSubst.Head() == WellKnownSym.minus) {
                if (preSubst.Args()[0].Head() == WellKnownSym.plus) {
                    List<Expr> newArgs = new List<Expr>();
                    foreach (Expr e in preSubst.Args()[0].Args())
                        if (e.Head() == WellKnownSym.minus)
                            newArgs.Add(e.Args()[0]);
                        else newArgs.Add(new CompositeExpr(WellKnownSym.minus, e));
                    return new CompositeExpr(WellKnownSym.plus, newArgs.ToArray());
                }
            }
            foreach (Expr e in chosenTerms)
                if (e == preSubst)
                    return Engine.Simplify(preSubst);
            if (preSubst.Head() == WellKnownSym.times && preSubst.Args()[1].Head() == WellKnownSym.divide) {
                return Engine.Simplify(preSubst);
            }
            if (preSubst.Head() == WellKnownSym.times) return DistributeTimes(preSubst, expand);
            if (preSubst.Head() == WellKnownSym.plus) return JoinAddition(preSubst, chosenTerms, colorInd);
            if (preSubst.Head() == WellKnownSym.power &&
                preSubst.Args()[1] is IntegerNumber && (int)preSubst.Args()[1] > 0 && (int)preSubst.Args()[1] < 5 &&
                (expand || !(preSubst.Args()[0] is Number))) {
                Expr[] args = new Expr[(int)preSubst.Args()[1]];
                for (int i = 0; i < args.Length; i++)
                    args[i] = preSubst.Args()[0].Clone();
                return new CompositeExpr(WellKnownSym.times, args);
            }
            return Engine.Simplify(preSubst);
        }

        static public Expr JoinAddition(Expr plusExpression, Expr[] chosenTerms, int colorInd) {
            Expr accumulator = null;
            List<Expr> rootLevelChosenTerms = new List<Expr>();
            for (int i = 0; i < chosenTerms.Length; i++) {
                foreach (Expr plusTerm in plusExpression.Args())
                    if (Expr.FindPath(plusTerm, chosenTerms[i]).Count != 0) {
                        TermColorizer.MarkFactor(plusTerm, colorInd);
                        rootLevelChosenTerms.Add(plusTerm);
                    }
            }
            List<Expr> factors = getPossibleFactors(rootLevelChosenTerms[0]);

            bool commonFactor = checkFactors(factors.ToArray(), rootLevelChosenTerms.ToArray());
            List<Expr> startArgs = new List<Expr>();
            List<Expr> endArgs = new List<Expr>();
            List<Expr> midArgs = new List<Expr>();
            int termsFound = 0;
            foreach (Expr e in plusExpression.Args()) {
                if (rootLevelChosenTerms.Contains(e)) {
                    termsFound++;
                    midArgs.Add(commonFactor ? removeFactor(e) : e);
                }
                else if (termsFound == 0)
                    startArgs.Add(e);
                else if (termsFound == chosenTerms.Length)
                    endArgs.Add(e);
                else midArgs.Add(e);
            }
            Expr joinedTerm = new CompositeExpr(WellKnownSym.plus, midArgs.ToArray());
            factors.Insert(0, !commonFactor ? ExprTransform.FixNegatives(Engine.Simplify(joinedTerm)) : joinedTerm);
            startArgs.Add(commonFactor && factors.Count > 1 ? (Expr)new CompositeExpr(WellKnownSym.times, factors.ToArray()) : factors[0]);
            startArgs.AddRange(endArgs.ToArray());
            if (startArgs.Count > 1)
                return new CompositeExpr(WellKnownSym.plus, startArgs.ToArray());
            if (startArgs.Count == 1)
                return startArgs[0];

            if (!commonFactor)
                accumulator = Engine.Simplify(plusExpression);
            return accumulator;
        }

        static public Expr DistributeTimes(Expr timesExpression, bool expand) {
            Expr accumulator = timesExpression.Args()[0];
            for (int i = 1; i < timesExpression.Args().Length; i++)
                accumulator = Distribute(accumulator, timesExpression.Args()[i], expand);
            return accumulator;
        }
        static  List<Expr> getPossibleFactors(Expr expr) {
            List<Expr> factors = new List<Expr>(new Expr[] {  });
            if (expr.Head() == WellKnownSym.times)
                foreach (Expr arg in expr.Args())
                    if (arg.Head() == WellKnownSym.divide)
                        factors.Add(arg);
            return factors;
        }
        static bool checkFactor(Expr factor, Expr term) {
            if (getPossibleFactors(term).Contains(factor))
                return true;
            return false;
        }
        static bool checkFactors(Expr[] factors, Expr[] terms) {
            foreach (Expr factor in factors)
                foreach (Expr term in terms)
                    if (!checkFactor(factor, term))
                        return false;
            return factors.Length > 0;
        }
        static Expr removeFactor(Expr expr) {
            if (expr.Head() != WellKnownSym.times)
                return expr;
            List<Expr> stripped = new List<Expr>();
            foreach (Expr arg in expr.Args())
                if (arg.Head() != WellKnownSym.divide)
                    stripped.Add(arg);
            if (stripped.Count == 0)
                return 1;
            if (stripped.Count == 1)
                return stripped[0];
            return new CompositeExpr(WellKnownSym.times, stripped.ToArray());
        }
        static public Expr Distribute(Expr terma, Expr termb, bool expand) {
            if (terma == termb && !expand)
                return new CompositeExpr(WellKnownSym.power, terma, 2);
            if (termb.Head() == WellKnownSym.power && termb.Args()[0] == terma)
                return new CompositeExpr(WellKnownSym.power, terma, Engine.Simplify(new CompositeExpr(WellKnownSym.plus, termb.Args()[1], 1)));
            if (terma.Head() == WellKnownSym.power && terma.Args()[0] == termb)
                return new CompositeExpr(WellKnownSym.power, termb, Engine.Simplify(new CompositeExpr(WellKnownSym.plus, terma.Args()[1], 1)));
            if (terma.Head() == WellKnownSym.minus) { // transforms -1*x -> -x     -a(bc) -> -(abc)   (-a)(b+c) -> -(a(b+c))
                if (terma.Args()[0] == 1) return new CompositeExpr(WellKnownSym.minus, termb);
                if (terma.Args()[0].Head() == WellKnownSym.times) {
                    List<Expr> combinedTimes = new List<Expr>(terma.Args()[0].Args());
                    combinedTimes.Add(termb);
                    return new CompositeExpr(WellKnownSym.minus, new CompositeExpr(WellKnownSym.times, combinedTimes.ToArray()));
                } else 
                    return new CompositeExpr(WellKnownSym.minus, new CompositeExpr(WellKnownSym.times,terma.Args()[0], termb));
            }
            if (termb.Head() == WellKnownSym.minus) {
                if (termb.Args()[0] == 1) return new CompositeExpr(WellKnownSym.minus, terma);
                if (termb.Args()[0].Head() == WellKnownSym.times) {
                    List<Expr> combinedTimes = new List<Expr>(termb.Args()[0].Args());
                    combinedTimes.Insert(0, terma);
                    return new CompositeExpr(WellKnownSym.minus, new CompositeExpr(WellKnownSym.times, combinedTimes.ToArray()));
                } else
                    return new CompositeExpr(WellKnownSym.minus, new CompositeExpr(WellKnownSym.times, terma, termb.Args()[0]));
            }
            if (terma.Head() == WellKnownSym.plus && termb.Head() != WellKnownSym.plus)
                return DistributePlus(termb, terma);
            if (terma.Head() != WellKnownSym.plus && termb.Head() == WellKnownSym.plus)
                return DistributePlus(terma, termb);
            if (terma.Head() == WellKnownSym.plus && termb.Head() == WellKnownSym.plus) {
                List<Expr> newTermAArgs = new List<Expr>();
                foreach (Expr plusTerm in terma.Args())
                    newTermAArgs.Add(DistributePlus(plusTerm, termb));
                return new CompositeExpr(WellKnownSym.plus, newTermAArgs.ToArray());
            }
            if (terma.Head() == WellKnownSym.power && terma.Args()[1] is IntegerNumber && (int)terma.Args()[1] == 2) {
                Expr ret = new CompositeExpr(WellKnownSym.times, terma.Args()[0], terma.Args()[0].Clone(), termb);
                return expand ? ret : Engine.Simplify(ret);
            }
            if (termb.Head() == WellKnownSym.power && termb.Args()[1] is IntegerNumber && (int)termb.Args()[1] == 2) {
                Expr ret = new CompositeExpr(WellKnownSym.times, terma, termb.Args()[0], termb.Args()[0].Clone());
                return expand ? ret : Engine.Simplify(ret);
            }
            Expr ret2 = new CompositeExpr(WellKnownSym.times, terma, termb);
            return expand ? ret2 : Engine.Simplify(ret2);
        }
        static public Expr DistributePlus(Expr terma, Expr plusTerm) {
            List<Expr> newPlusTerms = new List<Expr>();
            foreach (Expr pterm in plusTerm.Args())
                newPlusTerms.Add(Engine.Simplify(new CompositeExpr(WellKnownSym.times, terma, pterm)));
            return new CompositeExpr(WellKnownSym.plus, newPlusTerms.ToArray());
        }
        static public Expr DragTermAcrossEquality(Expr factorTerm, Expr topLevelExpr, bool divideAcross, bool lhs, int colorInd) {
            Expr factoredExpr = null;
            Expr origFactorTerm = factorTerm;
            if (divideAcross) {
                factoredExpr = ExprTransform.DivideAcross(topLevelExpr, factorTerm, lhs);
                if (factoredExpr == topLevelExpr) {
                    factoredExpr = DoFactorManipulation(topLevelExpr, ref factorTerm, colorInd);
                    if (origFactorTerm != factorTerm)
                        factoredExpr = ExprTransform.MoveAcross(factoredExpr, factorTerm, lhs);
                    else
                        factoredExpr = ExprTransform.DivideAcross(factoredExpr, factorTerm, lhs);
                }
            }
            else {
                factoredExpr = ExprTransform.MoveAcross(topLevelExpr, factorTerm, lhs);
                if (factoredExpr == topLevelExpr) {
                    Expr newFactoredExpr = DoFactorManipulation(topLevelExpr, ref factorTerm, colorInd);
                    if (origFactorTerm != factorTerm)
                        factoredExpr = ExprTransform.DivideAcross(newFactoredExpr, factorTerm, lhs);
                    else 
                        factoredExpr = ExprTransform.MoveAcross(newFactoredExpr, factorTerm, lhs);
                    if (newFactoredExpr == factoredExpr)
                        factoredExpr = topLevelExpr;
                }
            }
            return factoredExpr;
        }
    }
}
