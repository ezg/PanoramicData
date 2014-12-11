using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media;

namespace starPadSDK.MathExpr
{
    /// <summary>
    /// Summary description for Engine.
    /// </summary>
    static public class TermColorizer
    {
        static Color[] FactorColors = new Color[] { Colors.Red, Colors.Blue, Colors.Orange, Colors.Green, Colors.Brown, Colors.Cyan };
        static public Color FactorCol(int which)
        {
            Color baseline = FactorColors[which % FactorColors.Length];
            int mod = which / FactorColors.Length;
            if (mod > 0)
            {
                baseline = Color.FromRgb((byte)(baseline.R + mod < 255 ? baseline.R + mod : baseline.R - mod),
                    (byte)(baseline.G + mod < 255 ? baseline.G + mod : baseline.G - mod),
                    (byte)(baseline.B + mod < 255 ? baseline.B + mod : baseline.B - mod));
            }
            return baseline;
        }
        static public int FactorColInd(Color col)
        {
            for (int i = 0; i < 100; i++)
                if (FactorCol(i) == col)
                    return i;
            return 0;
        }
        /// <summary>
        /// clears out the Factor color flags for all terms that match the 'colorInd' color index or for all terms if 'colorInd' is -1
        /// </summary>
        /// <param name="e"></param>
        /// <param name="justThis"></param>
        /// <returns></returns>
        static public Expr ClearFactorMarks(Expr e, int colorInd)
        {
            if (e.Head() == WellKnownSym.divide)
                ClearFactorMarks(e.Args()[0], colorInd);
            if (e.Annotations.Contains("Factor") && (colorInd == -1 || e.Annotations["Factor"].Equals(FactorCol(colorInd))))
                e.Annotations.Remove("Factor");
            if (e.Annotations.Contains("FactorList"))
            {
                if (colorInd != -1)
                {
                    Dictionary<int, Color> colors = (Dictionary<int, Color>)e.Annotations["FactorList"];
                    colors.Remove(colorInd);
                    int maxInd = -1;
                    foreach (KeyValuePair<int, Color> pair in colors)
                    {
                        if (pair.Key > maxInd)
                        {
                            maxInd = pair.Key;
                            e.Annotations["Factor"] = pair.Value;
                        }
                    }
                }
                else
                    e.Annotations.Remove("FactorList");
            }
            if (e is CompositeExpr)
                foreach (Expr e1 in (e as CompositeExpr).Args)
                    ClearFactorMarks(e1, colorInd);

            return e;
        }
        static public void MarkFactorFS(Expr expr, object color)
        {
            MarkFactor(expr, FactorColInd((Color)color));
        }
        static public void MarkFactor(Expr expr, int colorInd)
        {
            if (expr.Head() == WellKnownSym.divide)
                MarkFactor(expr.Args()[0], colorInd);
            Dictionary<int, Color> factorList = null;
            if (expr.Annotations.Contains("FactorList"))
                factorList = (Dictionary<int, Color>)expr.Annotations["FactorList"];
            else
            {
                factorList = new Dictionary<int, Color>();
                expr.Annotations.Add("FactorList", factorList);
            }
            if (!factorList.ContainsKey(colorInd))
                factorList.Add(colorInd, FactorCol(colorInd));
            if (expr.Annotations.Contains("Factor"))
                expr.Annotations["Factor"] = FactorCol(colorInd);
            else expr.Annotations.Add("Factor", FactorCol(colorInd));
        }
        static public Expr FindFactorTerm(Expr e)
        {
            if (e.Annotations.Contains("Factor"))
                return e;
            if (e is CompositeExpr)
                foreach (Expr e1 in (e as CompositeExpr).Args)
                {
                    Expr found = FindFactorTerm(e1);
                    if (found != null)
                        return found;
                }

            return null;
        }
    }
}
