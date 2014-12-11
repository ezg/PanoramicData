using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Input;
using starPadSDK.CharRecognizer;
using starPadSDK.MathRecognizer;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using starPadSDK.MathExpr;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;

namespace starPadSDK.AppLib
{
    public static class InqFont
    {
        static Dictionary<object, StroqCollection> handChars = new Dictionary<object, StroqCollection>();
        static private Guid InkFontGuid = new Guid("{69628b7c-ac15-4cde-a3c8-4232abcbfe14}");

        static public bool LoadInkFont(string filename)
        {
            if (!System.IO.File.Exists(filename))
                return false;

            using (FileStream thestream = File.OpenRead(filename))
            {
                System.Windows.Ink.StrokeCollection col = new System.Windows.Ink.StrokeCollection(thestream);
                Dictionary<object, StroqCollection> inkfont = new Dictionary<object, StroqCollection>();
                foreach (System.Windows.Ink.Stroke sk in col)
                {
                    if (sk.GetPropertyData(InkFontGuid) != null)
                    {
                        object key = sk.GetPropertyData(InkFontGuid);
                        if (key != null)
                        {
                            if (key is ushort) key = Convert.ToChar(key); // Sigh...MS Ink silently and undocumentedly converts chars to ushorts when sticking in extended properties...
                            else if (key is int) key = Enum.ToObject(typeof(Recognition.Result.Special), key);
                            StroqCollection ss;
                            if (!inkfont.TryGetValue(key, out ss))
                            {
                                ss = new StroqCollection();
                                inkfont[key] = ss;
                            }
                            ss.Add(new Stroq(sk));
                        }
                    }
                }
                handChars = inkfont;
            }

            return true;
        }

        static public bool CheckSymbolExists(object o) { StroqCollection tmp; return handChars.TryGetValue(o, out tmp); }
        static public StroqCollection GetSymbol(object o) {
            StroqCollection c;
            if (handChars.TryGetValue(o, out c))
                return c;
            else
                return null;
        }

        static public StroqCollection NumberAsInk(double num, double fontsize)
        {
            StroqCollection col = new StroqCollection();

            Expr expr = new DoubleNumber(num);

            StringBox box = EWPF.Measure(expr, fontsize) as StringBox;

            for (int i = 0; i < box.CBoxes.Length; i++) {
                CharBox b = box.CBoxes[i];

                if (i == box.CBoxes.Length - 1 && b.C == '.')
                    continue;  // don't draw numbers that look like "xxxx."

                StroqCollection sym = InqFont.GetSymbol(b.C).Clone();

                Rct old_rct = sym.GetBounds();
                Rct new_rct = b.BBoxRefOrigin;
                if (b.C == '1') {
                    double k = 0.05*new_rct.Width;
                    new_rct = new Rct(new_rct.Center.X - k, new_rct.Top, new_rct.Center.X + k, new_rct.Bottom);
                } else if (b.C == '.') {
                    double k = 0.05 * new_rct.Width;
                    new_rct = new Rct(new_rct.Center.X - k, new_rct.Bottom - 2*k, new_rct.Center.X + k, new_rct.Bottom);
                }

                Mat mat = Mat.Rect(old_rct).Inverse() * Mat.Rect(new_rct);

                foreach (Stroq s in sym)
                    s.XformBy(mat);

                col.Add(sym);
            }

            return col;
        }
    }
}
