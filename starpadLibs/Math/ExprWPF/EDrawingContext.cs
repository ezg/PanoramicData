using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using starPadSDK.UnicodeNs;
using starPadSDK.MathExpr;
using starPadSDK;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Runtime.InteropServices;
using System.Globalization;

namespace starPadSDK.MathExpr.ExprWPF {
    public class EDrawingContext {
        // the STIX fonts
        private enum F { General, GeneralB, GeneralI, GeneralBI, Variant, VariantB, Nonuni, NonuniB, NonuniI, NonuniBI,
            IntegralsSmall, /*IntegralsRegular,*/ IntegralsDisplay, IntegralsSmallB, /*IntegralsRegularB,*/ IntegralsDisplayB,
            IntegralsUprightSmall, IntegralsUprightRegular, IntegralsUprightDisplay,
            IntegralsUprightSmallB, IntegralsUprightRegularB, IntegralsUprightDisplayB,
            Sz1, Sz2, Sz3, Sz4, Sz5, Sz1B, Sz2B, Sz3B, Sz4B, NumFonts };
        private static string[] STIXFontNameCores = { "General", "GeneralBol", "GeneralItalic", "GeneralBolIta",
                                                    "Var", "VarBol", "NonUni", "NonUniBol", "NonUniIta", "NonUniBolIta",
                                                    "IntSma", "IntDis", "IntSmaBol", "IntDisBol", "IntUpSma", "IntUp", "IntUpDis",
                                                    "IntUpSmaBol", "IntUpBol", "IntUpDisBol",
                                                    "Siz1Sym", "Siz2Sym", "Siz3Sym", "Siz4Sym", "Siz5Sym",
                                                    "Siz1SymBol", "Siz2SymBol", "Siz3SymBol", "Siz4SymBol" };
        private static string EscapePathForURI(string path) {
            int start = 0;
            string uri = "";
            char[] seps = new [] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar };
            while(start < path.Length) {
                int end = path.IndexOfAny(seps, start);
                if(end == -1) {
                    uri += Uri.EscapeDataString(path.Substring(start));
                    break;
                } else {
                    uri += Uri.EscapeDataString(path.Substring(start, end-start));
                    uri += path[end];
                    start = end+1;
                }
            }
            return uri;
        }
        private static string STIXFontDir = "file:///" + EscapePathForURI(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "\\STIXfonts\\";
        private static string STIXFontName(F ix) { return "STIX" + STIXFontNameCores[(int)ix] + ".otf"; }
        private static GlyphTypeface[] _gts = new GlyphTypeface[(int)F.NumFonts];
        private static GlyphTypeface _f(F ix) {
            if(_gts[(int)ix] == null) {
                _gts[(int)ix] = new GlyphTypeface(new Uri(STIXFontDir + STIXFontName(ix)));
            }
            return _gts[(int)ix];
        }
        public static Uri FontFamilyURIBase { get { return new Uri(STIXFontDir.Replace('\\', '/')); } }
        public static string FontFamilyURIRel { get { return "./#STIXGeneral, ./#STIXNonUnicode, Arial Unicode MS, Lucida Sans Unicode"; } }
        private static string[] FontFallbacks = { "Arial Unicode MS", "Lucida Sans Unicode" };

        private Color _colr; public Color Colr { get { return _colr; } set { _colr = value; Brush = new SolidColorBrush(_colr); } }
        public Brush Brush { get; private set; }
        public DrawingContext DC { get; private set; }
        double emSize = 0;
        public double EmSize { get { return emSize; } 
            private set { emSize = value; } }
        public bool UprightIntegrals { get; private set; }
        private bool _display; public bool Display { get { return _display; } }
        private bool _nonscript; public bool Nonscript { get { return _nonscript; } }

        // For OTF (OpenType Font) stuff, especially documentation, see http://www.microsoft.com/typography/otspec/otff.htm
        #region OTFstructs
        public class OTFMetrics {
            public OTFhead head;
            public OTFOS2 OS2;
            public OTFhhea hhea;
            public OTFMATHHeader MATHhead;
            public OTFMATHconstants MATHconstants;
            public OTFMATHglyphinfo MATHglyphinfo;
            public OTFMATHvariants MATHvariants;
        }
        private static uint OTFTag(string tag) {
            uint val = 0;
            for(int i = 0; i < 4; i++) val = (val << 8) | (uint)tag[i];
            return val;
        }
        private static string OTFTag(uint tag) {
            string s = "";
            for(int i = 0; i < 4; i++) {
                s = (char)(tag & 0xff) + s;
                tag >>= 8;
            }
            return s;
        }
        private class OTFHeader { public int version; public ushort numTables, searchRange, entrySelector, rangeShift; }
        private class OTFTableDirent { public uint tag, checkSum, offset, length; }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFhead {
            public int version;
            public int fontRevision;
            public uint checkSumAdjustment;
            public uint magicNumber;
            public ushort flags;
            public ushort unitsPerEm;
            public ulong created;
            public ulong modified;
            public short xMin;
            public short yMin;
            public short xMax;
            public short yMax;
            public ushort macStyle;
            public ushort lowestRecPPEM;
            public short fontDirectionHint;
            public short indexToLocFormat;
            public short glyphDataFormat;
        }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFOS2 {
            public ushort version; // the beta fonts use version 2, though MS' current web page defines up to version 4 (v4 struct = v2 struct)
            public short xAvgCharWidth;
            public ushort usWeightClass;
            public ushort usWidthClass;
            public ushort fsType;
            public short ySubscriptXSize;
            public short ySubscriptYSize;
            public short ySubscriptXOffset;
            public short ySubscriptYOffset;
            public short ySuperscriptXSize;
            public short ySuperscriptYSize;
            public short ySuperscriptXOffset;
            public short ySuperscriptYOffset;
            public short yStrikeoutSize;
            public short yStrikeoutPosition;
            public short sFamilyClass;
            public byte panoseFamilyType;
            public byte panoseSerifStyle;
            public byte panoseWeight;
            public byte panoseProportion;
            public byte panoseContrast;
            public byte panoseStrokeVariation;
            public byte panoseArmStyle;
            public byte panoseLetterform;
            public byte panoseMidline;
            public byte panoseXHeight;
            public uint ulUnicodeRange1;
            public uint ulUnicodeRange2;
            public uint ulUnicodeRange3;
            public uint ulUnicodeRange4;
            public uint achVendID; // really a char[4]
            public ushort fsSelection;
            public ushort usFirstCharIndex;
            public ushort usLastCharIndex;
            public short sTypoAscender;
            public short sTypoDescender;
            public short sTypoLineGap;
            public ushort usWinAscent;
            public ushort usWinDescent;
            public uint ulCodePageRange1;
            public uint ulCodePageRange2;
            public short sxHeight;
            public short sCapHeight;
            public ushort usDefaultChar;
            public ushort usBreakChar;
            public ushort usMaxContext;
        }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFhhea {
            public int version;
            public short Ascender;
            public short Descender;
            public short LineGap;
            public ushort advanceWidthMax;
            public short minLeftSideBearing;
            public short minRightSideBearing;
            public short xMaxExtent;
            public short caretSlopeRise;
            public short caretSlopeRun;
            public short caretOffset;
            public short reserved1;
            public short reserved2;
            public short reserved3;
            public short reserved4;
            public short metricDataFormat;
            public ushort numberOfHMetrics;
        }
        // All the MATH table stuff comes from examining the code to the open source Fontforge program (see parsettfatt.c, mathconstants.c, and splinefont.h).
        // The STIX fonts were produced with Fontforge, so it seems reasonable.
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFMATHHeader {
            public int version;
            public ushort constants;
            public ushort glyphinfo;
            public ushort variants;
        }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFMATHconstants {
            /// <summary>Percentage scale down for script level 1</summary>
            public short ScriptPercentScaleDown; // 80
            /// <summary>Percentage scale down for script level 2</summary>
            public short ScriptScriptPercentScaleDown; // 60
            /// <summary>Minimum height at which to treat a delimited expression as a subformula</summary>
            public ushort DelimitedSubFormulaMinHeight; // 1500
            /// <summary>Minimum height of n-ary operators (integration, summation, etc.)</summary>
            public ushort DisplayOperatorMinHeight; // 0
            /// <summary>White space to be left between math formulae to ensure proper line spacing.</summary>
            public short MathLeading; // 0
            public ushort MathLeading_taboffs;
            /// <summary>Axis height of the font</summary>
            public short AxisHeight; // 0
            public ushort AxisHeight_taboffs;
            /// <summary>Maximum (ink) height of accent base that does not require raising the accents.</summary>
            public short AccentBaseHeight; // 450
            public ushort AccentBaseHeight_taboffs;
            /// <summary>Maximum (ink) height of accent base that does not require flattening the accents.</summary>
            public short FlattenedAccentBaseHeight; // 662
            public ushort FlattenedAccentBaseHeight_taboffs;
            /// <summary>The standard shift down applied to subscript elements. Positive for moving downward.</summary>
            public short SubscriptShiftDown; // 250
            public ushort SubscriptShiftDown_taboffs;
            /// <summary>Maximum height of the (ink) top of subscripts that does not require moving ubscripts further down.</summary>
            public short SubscriptTopMax; // 450
            public ushort SubscriptTopMax_taboffs;
            /// <summary>Maximum allowed drop of the baseline of subscripts realtive to the bottom of the base. Used for bases that are treated as a box or extended shape. Positive for subscript baseline dropped below base bottom.</summary>
            public short SubscriptBaselineDropMin; // 0
            public ushort SubscriptBaselineDropMin_taboffs;
            /// <summary>Standard shift up applied to superscript elements.</summary>
            public short SuperscriptShiftUp; // 500
            public ushort SuperscriptShiftUp_taboffs;
            /// <summary>Standard shift of superscript relative to base in cramped mode.</summary>
            public short SuperscriptShiftUpCramped; // 0
            public ushort SuperscriptShiftUpCramped_taboffs;
            /// <summary>Minimum allowed hieght of the bottom of superscripts that does not require moving them further up.</summary>
            public short SuperscriptBottomMin; // 450
            public ushort SuperscriptBottomMin_taboffs;
            /// <summary>Maximum allowed drop of the baseline of superscripts realtive to the top of the base. Used for bases that are treated as a box or extended shape. Positive for superscript baseline below base top.</summary>
            public short SuperscriptBaselineDropMax; // 0
            public ushort SuperscriptBaselineDropMax_taboffs;
            /// <summary>Minimum gap between the supersecript and subscript ink.</summary>
            public short SubSuperscriptGapMin; // 200
            public ushort SubSuperscriptGapMin_taboffs;
            /// <summary>The maximum level to which the (ink) bottom of superscript can be pushed to increase the gap between superscript and subscript, before subscript starts being moved down.</summary>
            public short SuperscriptBottomMaxWithSubscript; // 450
            public ushort SuperscriptBottomMaxWithSubscript_taboffs;
            /// <summary>Extra white space to be added after each ub/superscript.</summary>
            public short SpaceAfterScript; // 41
            public ushort SpaceAfterScript_taboffs;
            /// <summary>Minimum gap between the bottom of the upper limit, and the top of the base operator.</summary>
            public short UpperLimitGapMin; // 0
            public ushort UpperLimitGapMin_taboffs;
            /// <summary>Minimum distance between the baseline of an upper limit and the bottom of the base operator.</summary>
            public short UpperLimitBaselineRiseMin; // 0
            public ushort UpperLimitBaselineRiseMin_taboffs;
            /// <summary>Minimum gap between (ink) top of the lower limit, and (ink) bottom of the base operator.</summary>
            public short LowerLimitGapMin; // 0
            public ushort LowerLimitGapMin_taboffs;
            /// <summary>Minimum distance between the baseline of the lower limit and bottom of the base operator.</summary>
            public short LowerLimitBaselineDropMin; // 0
            public ushort LowerLimitBaselineDropMin_taboffs;
            /// <summary>Standard shift up applied to the top element of a stack.</summary>
            public short StackTopShiftUp; // 0
            public ushort StackTopShiftUp_taboffs;
            /// <summary>Standard shift up applied to the top element of a stack in display style.</summary>
            public short StackTopDisplayStyleShiftUp; // 0
            public ushort StackTopDisplayStyleShiftUp_taboffs;
            /// <summary>Standard shift down applied to the bottom element of a stack. Positive values indicate downward motion.</summary>
            public short StackBottomShiftDown; // 0
            public ushort StackBottomShiftDown_taboffs;
            /// <summary>Standard shift down applied to the bottom element of a stack in display style. Positive values indicate downward motion.</summary>
            public short StackBottomDisplayStyleShiftDown; // 0
            public ushort StackBottomDisplayStyleShiftDown_taboffs;
            /// <summary>Minimum gap between bottom of the top element of a stack, and the top of the bottom element.</summary>
            public short StackGapMin; // 150
            public ushort StackGapMin_taboffs;
            /// <summary>Minimum gap between bottom of the top element of a stack and the top of the bottom element in display style.</summary>
            public short StackDisplayStyleGapMin; // 350
            public ushort StackDisplayStyleGapMin_taboffs;
            /// <summary>Standard shift up applied to the top element of the stretch stack.</summary>
            public short StretchStackTopShiftUp; // 0
            public ushort StretchStackTopShiftUp_taboffs;
            /// <summary>Standard shift down applied to the bottom element of the stretch stack. Positive values indicate downward motion.</summary>
            public short StretchStackBottomShiftDown; // 0
            public ushort StretchStackBottomShiftDown_taboffs;
            /// <summary>Minimum gap between the ink of the stretched element and the ink bottom of the element above..</summary>
            public short StretchStackGapAboveMin; // 0
            public ushort StretchStackGapAboveMin_taboffs;
            /// <summary>Minimum gap between the ink of the stretched element and the ink top of the element below.</summary>
            public short StretchStackGapBelowMin; // 0
            public ushort StretchStackGapBelowMin_taboffs;
            /// <summary>Standard shift up applied to the numerator.</summary>
            public short FractionNumeratorShiftUp; // 0
            public ushort FractionNumeratorShiftUp_taboffs;
            /// <summary>Standard shift up applied to the numerator in display style.</summary>
            public short FractionNumeratorDisplayStyleShiftUp; // 0
            public ushort FractionNumeratorDisplayStyleShiftUp_taboffs;
            /// <summary>Standard shift down applied to the denominator. Postive values indicate downward motion.</summary>
            public short FractionDenominatorShiftDown; // 0
            public ushort FractionDenominatorShiftDown_taboffs;
            /// <summary>Standard shift down applied to the denominator in display style. Postive values indicate downward motion.</summary>
            public short FractionDenominatorDisplayStyleShiftDown; // 0
            public ushort FractionDenominatorDisplayStyleShiftDown_taboffs;
            /// <summary>Minimum tolerated gap between the ink bottom of the numerator and the ink of the fraction bar.</summary>
            public short FractionNumeratorGapMin; // 50
            public ushort FractionNumeratorGapMin_taboffs;
            /// <summary>Minimum tolerated gap between the ink bottom of the numerator and the ink of the fraction bar in display style.</summary>
            public short FractionNumeratorDisplayStyleGapMin; // 150
            public ushort FractionNumeratorDisplayStyleGapMin_taboffs;
            /// <summary>Thickness of the fraction bar.</summary>
            public short FractionRuleThickness; // 50
            public ushort FractionRuleThickness_taboffs;
            /// <summary>Minimum tolerated gap between the ink top of the denominator and the ink of the fraction bar..</summary>
            public short FractionDenominatorGapMin; // 50
            public ushort FractionDenominatorGapMin_taboffs;
            /// <summary>Minimum tolerated gap between the ink top of the denominator and the ink of the fraction bar in display style.</summary>
            public short FractionDenominatorDisplayStyleGapMin; // 150
            public ushort FractionDenominatorDisplayStyleGapMin_taboffs;
            /// <summary>Horizontal distance between the top and bottom elemnts of a skewed fraction.</summary>
            public short SkewedFractionHorizontalGap; // 0
            public ushort SkewedFractionHorizontalGap_taboffs;
            /// <summary>Vertical distance between the ink of the top and bottom elements of a skewed fraction.</summary>
            public short SkewedFractionVerticalGap; // 0
            public ushort SkewedFractionVerticalGap_taboffs;
            /// <summary>Distance between the overbar and the ink top of the base.</summary>
            public short OverbarVerticalGap; // 150
            public ushort OverbarVerticalGap_taboffs;
            /// <summary>Thickness of the overbar.</summary>
            public short OverbarRuleThickness; // 50
            public ushort OverbarRuleThickness_taboffs;
            /// <summary>Extra white space reserved above the overbar.</summary>
            public short OverbarExtraAscender; // 50
            public ushort OverbarExtraAscender_taboffs;
            /// <summary>Distance between underbar and the (ink) bottom of the base.</summary>
            public short UnderbarVerticalGap; // 150
            public ushort UnderbarVerticalGap_taboffs;
            /// <summary>Thickness of the underbar.</summary>
            public short UnderbarRuleThickness; // 50
            public ushort UnderbarRuleThickness_taboffs;
            /// <summary>Extra white space resevered below the underbar.</summary>
            public short UnderbarExtraDescender; // 50
            public ushort UnderbarExtraDescender_taboffs;
            /// <summary>Space between the ink to of the expression and the bar over it.</summary>
            public short RadicalVerticalGap; // 50
            public ushort RadicalVerticalGap_taboffs;
            /// <summary>Space between the ink top of the expression and the bar over it in display style.</summary>
            public short RadicalDisplayStyleVerticalGap; // 0
            public ushort RadicalDisplayStyleVerticalGap_taboffs;
            /// <summary>Thickness of the radical rule in designed or constructed radical signs.</summary>
            public short RadicalRuleThickness; // 0
            public ushort RadicalRuleThickness_taboffs;
            /// <summary>Extra white space reserved above the radical.</summary>
            public short RadicalExtraAscender; // 50
            public ushort RadicalExtraAscender_taboffs;
            /// <summary>Extra horizontal kern before the degree of a radical if such be present.</summary>
            public short RadicalKernBeforeDegree; // 277
            public ushort RadicalKernBeforeDegree_taboffs;
            /// <summary>Negative horizontal kern after the degree of a radical if such be present.</summary>
            public short RadicalKernAfterDegree; // -555
            public ushort RadicalKernAfterDegree_taboffs;
            /// <summary>Height of the bottom of the radical degree, if such be present, in proportion to the ascender of the radical sign.</summary>
            public ushort RadicalDegreeBottomRaisePercent; // 60
        }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFMATHglyphinfo { public ushort icoff, taoff, esoff, kioff; }
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public class OTFMATHvariants {
            /// <summary>Minimum overlap of connecting glyphs during glyph construction</summary>
            public ushort MinConnectorOverlap; // 20
            public ushort vcoverage, hcoverage, vcnt, hcnt;
        }
        #endregion
        private static void SlurpStruct(object o, Stream s) {
            Type t = o.GetType();
            FieldInfo[] fields = t.GetFields();
            foreach(FieldInfo fi in fields) {
                ulong num = 0;
                for(int i = 0; i < Marshal.SizeOf(fi.FieldType); i++) {
                    num = (num << 8) | (byte) s.ReadByte();
                }
                object val;
                val = fi.FieldType.InvokeMember("Parse", BindingFlags.Public|BindingFlags.Static|BindingFlags.InvokeMethod, null, null, new object[] { num.ToString("x"), System.Globalization.NumberStyles.HexNumber });
                fi.SetValue(o, val);
            }
        }
        private static Dictionary<GlyphTypeface, OTFMetrics> _metrics = new Dictionary<GlyphTypeface, OTFMetrics>();
        private static OTFMetrics _Metrics(F f) { return _Metrics(_f(f), true); }
        private static OTFMetrics _Metrics(GlyphTypeface gt, bool ours) { // checkmissing is false for non-stix/math fonts
            OTFMetrics metrics;
            if(!_metrics.TryGetValue(gt, out metrics)) {
                metrics = new OTFMetrics();
                Stream ffile = gt.GetFontStream();
                ffile.Seek(0, SeekOrigin.Begin);
                OTFHeader otfhdr = new OTFHeader();
                SlurpStruct(otfhdr, ffile);
                Trace.Assert(otfhdr.version == OTFTag("OTTO") || otfhdr.version == 0x10000); // is OTTO for the beta stix fonts
                Dictionary<string, OTFTableDirent> tables = new Dictionary<string, OTFTableDirent>();
                OTFTableDirent td;
                for(int i = 0; i < otfhdr.numTables; i++) {
                    td = new OTFTableDirent();
                    SlurpStruct(td, ffile);
                    tables[OTFTag(td.tag)] = td;
                }

                bool found;

                found = tables.TryGetValue("head", out td);
                if(found) {
                    ffile.Seek(td.offset, SeekOrigin.Begin);
                    Trace.Assert(td.length == Marshal.SizeOf(typeof(OTFhead)));
                    metrics.head = new OTFhead();
                    SlurpStruct(metrics.head, ffile);
                    Trace.Assert(metrics.head.version == 0x10000);
                    Trace.Assert(metrics.head.magicNumber == 0x5F0F3CF5);
                } else if(ours) throw new Exception("Built-in font " + gt.FaceNames[CultureInfo.CurrentCulture] + " missing 'head' table!");

                found = tables.TryGetValue("OS/2", out td);
                if(found) {
                    ffile.Seek(td.offset, SeekOrigin.Begin);
                    Trace.Assert(ours ? (td.length == Marshal.SizeOf(typeof(OTFOS2))) : (td.length <= Marshal.SizeOf(typeof(OTFOS2))));
                    metrics.OS2 = new OTFOS2();
                    SlurpStruct(metrics.OS2, ffile);
                    Trace.Assert(ours? (metrics.OS2.version == 0x0002) : (metrics.OS2.version >= 0 && metrics.OS2.version <= 4));
                } else if(ours) throw new Exception("Built-in font " + gt.FaceNames[CultureInfo.CurrentCulture] + " missing 'OS/2' table!");

                found = tables.TryGetValue("hhea", out td);
                if(found) {
                    ffile.Seek(td.offset, SeekOrigin.Begin);
                    Trace.Assert(td.length == Marshal.SizeOf(typeof(OTFhhea)));
                    metrics.hhea = new OTFhhea();
                    SlurpStruct(metrics.hhea, ffile);
                    Trace.Assert(metrics.hhea.version == 0x10000);
                } else if(ours) throw new Exception("Built-in font " + gt.FaceNames[CultureInfo.CurrentCulture] + " missing 'hhea' table!");

                if(ours) {
                    ffile.Seek(tables["MATH"].offset, SeekOrigin.Begin);
                    Trace.Assert(tables["MATH"].length >= Marshal.SizeOf(typeof(OTFMATHHeader)) + Marshal.SizeOf(typeof(OTFMATHconstants)));
                    metrics.MATHhead = new OTFMATHHeader();
                    SlurpStruct(metrics.MATHhead, ffile);
                    Trace.Assert(metrics.MATHhead.version == 0x10000);
                    ffile.Seek(tables["MATH"].offset + metrics.MATHhead.constants, SeekOrigin.Begin);
                    metrics.MATHconstants = new OTFMATHconstants();
                    SlurpStruct(metrics.MATHconstants, ffile);
                    ffile.Seek(tables["MATH"].offset + metrics.MATHhead.glyphinfo, SeekOrigin.Begin);
                    metrics.MATHglyphinfo = new OTFMATHglyphinfo();
                    SlurpStruct(metrics.MATHglyphinfo, ffile);
                    /* we don't actually load in any of the rest of glyphinfo because none of the STIX fonts have any */
                    ffile.Seek(tables["MATH"].offset + metrics.MATHhead.variants, SeekOrigin.Begin);
                    metrics.MATHvariants = new OTFMATHvariants();
                    SlurpStruct(metrics.MATHvariants, ffile);
                    /* we don't actually load in any of the rest of variants because none of the STIX fonts have any */
                }

                _metrics[gt] = metrics;
            }
            return metrics;
        }
        public static OTFMetrics Metrics { get { return _Metrics(F.General); } }
        public static double FDesignUnits { get { return Metrics.head.unitsPerEm; } }
        public double FScaleFactor { get { return EmSize/FDesignUnits; } }
        public double Ascent { get { return Metrics.OS2.sTypoAscender*FScaleFactor; } }
        public double Descent { get { return -Metrics.OS2.sTypoDescender*FScaleFactor; } }
        public double XHeight { get { return EmSize*_f(F.General).XHeight; } }
        public double Midpt { get { return XHeight/2; } }

        public double Mu { get { return EmSize/18; } }
        public double Thin { get { return 3*Mu; } }
        public double Med { get { return 4*Mu; } }
        public double Thick { get { return 5*Mu; } }
        public double NegThin { get { return -3*Mu; } }

        public EDrawingContext(double size, DrawingContext dc, Color c) {
            EmSize = size;
            Colr = c;
            DC = dc;
            _display = true;
            _nonscript = true;
            UprightIntegrals = true;
        }
        private EDrawingContext(double size, DrawingContext dc, Color c, bool display, bool nonscript) {
            EmSize = size;
            Colr = c;
            DC = dc;
            _display = display;
            _nonscript = nonscript;
            UprightIntegrals = true;
        }

        public Color EmphColor {
            get {
                byte max = Math.Max(Colr.R, Colr.G);
                max = Math.Max(max, Colr.B);
                if(max == 0) {
                    return Color.FromArgb(Colr.A, 128, 128, 128);
                } else {
                    double scale;
                    if(max > 127) scale = 0.5f;
                    else scale = 1 + (255f-max)/2/max;
                    return Color.FromArgb(Colr.A, (byte)(Colr.R*scale), (byte)(Colr.G*scale), (byte)(Colr.B*scale));
                }
            }
        }

        public static Vec ScreenDPI {
            get {
                // idea from http://blogs.msdn.com/jaimer/archive/2007/03/07/getting-system-dpi-in-wpf-app.aspx
                Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
                return new Vec(m.M11*96, m.M22*96);
            }
        }
        public virtual EDrawingContext Script() {
            double newsize = Metrics.MATHconstants.ScriptPercentScaleDown/100.0*EmSize;
            newsize = Math.Max(newsize, Metrics.head.lowestRecPPEM/96.0*ScreenDPI.X);
            newsize = Math.Min(newsize, EmSize);
            return new EDrawingContext(newsize, DC, Colr, false, _display);
        }
        public virtual EDrawingContext Atop() {
            if(_display) return new EDrawingContext(EmSize, DC, Colr, false, _display);
            else return Script();
        }
        public struct Glyph {
            public GlyphTypeface GT;
            public ushort Index;
            public bool Ours;
            public Glyph(GlyphTypeface gt, ushort index, bool ours) {
                GT = gt;
                Index = index;
                Ours = ours;
            }
        }
        public Glyph GetGlyph(char c, bool bold, bool italic) {
            GlyphTypeface gt = _f(bold ? (italic ? F.GeneralBI : F.GeneralB) : (italic ? F.GeneralI : F.General));
            ushort ix;
            Typeface tf; // arg, stupid c# rules
            if(gt.CharacterToGlyphMap.TryGetValue(c, out ix)) return new Glyph(gt, ix, true);
            gt = _f(bold ? (italic ? F.NonuniBI : F.NonuniB) : (italic ? F.NonuniI : F.Nonuni));
            if(gt.CharacterToGlyphMap.TryGetValue(c, out ix)) return new Glyph(gt, ix, true);
            // (note in case desired in future: FormattedText can be used to get the same thing by parsing the output of drawing it, looking for GlyphRunDrawings
            //  see http://social.msdn.microsoft.com/forums/en-US/wpf/thread/ddd9c850-25a6-4b99-8a43-5816a0d329a1)
            foreach(string fam in FontFallbacks) {
                tf = new Typeface(new FontFamily(fam), italic ? FontStyles.Italic : FontStyles.Normal,
                        bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
                if(tf.TryGetGlyphTypeface(out gt)) {
                    if(gt.CharacterToGlyphMap.TryGetValue(c, out ix)) return new Glyph(gt, ix, false);
                }
            }
            // return .notdef/missing char--is blank in stix fonts! so we get real box from arial.
            tf = new Typeface("Arial");
            Trace.Assert(tf.TryGetGlyphTypeface(out gt));
            return new Glyph(gt, 0, false);
        }
        // for use by BigC only
        private double Draw(char c, F f, double scale, Pt where) {
            GlyphTypeface gt = _f(f);
            ushort ix = gt.CharacterToGlyphMap[c]; // FIXME handle missing chars, or would that be trapped by the composite-finding code?
            return Draw(new Glyph(gt, ix, true), where, scale);
        }
        // for use by BigC only
        private double GetGeometry(char c, F f, double scale, Pt where, out Geometry g) {
            GlyphTypeface gt = _f(f);
            ushort ix = gt.CharacterToGlyphMap[c]; // FIXME handle missing chars, or would that be trapped by the composite-finding code?
            return GetGeometry(new Glyph(gt, ix, true), where, scale, out g);
        }
        public double Draw(string s, Pt where, bool bold, bool italic) {
            Pt pos = where;
            foreach(char c in s) {
                pos.X += Draw(c, pos, bold, italic);
            }
            return pos.X - where.X;
        }
        public double GetGeometry(string s, Pt where, bool bold, bool italic, out Geometry g) {
            GeometryGroup gg = new GeometryGroup();
            Pt pos = where;
            foreach(char c in s) {
                Geometry g1;
                pos.X += GetGeometry(c, pos, bold, italic, out g1);
                gg.Children.Add(g1);
            }
            g = gg;
            return pos.X - where.X;
        }
        public virtual double Draw(char c, Pt where, bool bold, bool italic) {
            return Draw(GetGlyph(c, bold, italic), where);
        }
        public virtual double GetGeometry(char c, Pt where, bool bold, bool italic, out Geometry g) {
            return GetGeometry(GetGlyph(c, bold, italic), where, out g);
        }
        public double Draw(Glyph g, Pt where) { return Draw(g, where + Midpt*Vec.Down, 1); }
        public double GetGeometry(Glyph g, Pt where, out Geometry geo) { return GetGeometry(g, where + Midpt*Vec.Down, 1, out geo); }
        private double Draw(Glyph g, Pt where, double scale) {
            GlyphRun gr = new GlyphRun(g.GT, 0, false, EmSize*scale, new ushort[] { g.Index }, where,
                new double[] { EmSize*scale*g.GT.AdvanceWidths[g.Index] }, null, null, null, null, null, null);
            DC.DrawGlyphRun(Brush, gr);
            return EmSize*scale*g.GT.AdvanceWidths[g.Index];
        }
        private double GetGeometry(Glyph g, Pt where, double scale, out Geometry geo) {
            GlyphRun gr = new GlyphRun(g.GT, 0, false, EmSize*scale, new ushort[] { g.Index }, where,
                new double[] { EmSize*scale*g.GT.AdvanceWidths[g.Index] }, null, null, null, null, null, null);
            geo = gr.BuildGeometry();
            return EmSize*scale*g.GT.AdvanceWidths[g.Index];
        }
        /// <summary>
        /// Returns the nominal bbox; third parm gets the tight bbox.
        /// </summary>
        /// <param name="bbox">the tight bbox of where ink is drawn.</param>
        /// <returns>the nominal bbox (right is amount to advance after drawing).</returns>
        public Rct Measure(char c, bool bold, bool italic, out Rct bbox) {
            return Measure(GetGlyph(c, bold, italic), out bbox);
        }
        // for use by BigC only
        private bool Measure(char c, F f, out Rct bbox, out Rct nombbox) {
            GlyphTypeface gt = _f(f);
            ushort ix;
            if(!gt.CharacterToGlyphMap.TryGetValue(c, out ix)) {
                bbox = nombbox = Rct.Null;
                return false;
            }
            nombbox = Measure(new Glyph(gt, ix, true), out bbox);
            return true;
        }
        private Rct Measure(Glyph g, out Rct bbox) {
            bbox = new Rct(EmSize*g.GT.LeftSideBearings[g.Index],
                -(_Metrics(g.GT, g.Ours).OS2.sTypoAscender*EmSize/_Metrics(g.GT, g.Ours).head.unitsPerEm - EmSize*g.GT.TopSideBearings[g.Index] - Midpt),
                EmSize*(g.GT.AdvanceWidths[g.Index] - g.GT.RightSideBearings[g.Index]),
                EmSize*g.GT.DistancesFromHorizontalBaselineToBlackBoxBottom[g.Index]+Midpt);
            return new Rct(0, Math.Min(0.0f, bbox.Top), EmSize*g.GT.AdvanceWidths[g.Index], Math.Max(Midpt, bbox.Bottom));
        }

        public class BigC {
            public char C { get; private set; }
            public bool Bold { get; private set; }
            public bool Italic { get; private set; }
            public bool Emph { get; private set; }
            public EDrawingContext EDC { get; private set; }
            public double Ascent { get; private set; }
            public double Descent { get; private set; }

            private Rct _bbox; public Rct BBox { get { return _bbox; } }
            private Rct _nombbox; public Rct NomBBox { get { return _nombbox; } }
            private double _uloffset;
            private double _repeatsize;
            private double _scale;
            private double _scaledEmSize;
            private Composite _comp;
            private List<Rct> _bboxes, _nombboxes; // bounds of pieces that go together to make a composed character
            private List<double> _offsets; // for each of the pieces, if you want it to show up so that the top of the ink is at a given Y value, add this to get the value to pass to the drawing routines (for WPF, the baseline; used to be the top for GDI)
            private enum Variant { Plain, Sized, Composed, Scaled }
            private Variant _variant;
            public bool IsScaled { get { return _variant != Variant.Plain; } } // FIXME just to keep existing code in Boxes working, then rename later
            private F _f;
            public BigC(char c, bool bold, bool italic, bool emph, double ascent, double descent, EDrawingContext edc) {
                C = c;
                Bold = bold;
                Italic = italic;
                Emph = emph;
                EDC = edc;

                if(c == Unicode.S.SQUARE_ROOT) {
                    // see rule 11 from TeXBook appendix G
                    Ascent = ascent;
                    Descent = descent;
                } else {
                    // rule 19 from TeXBook appendix G
                    double delta = Math.Max(ascent, descent);
                    double delimeterfactor = 901;
                    double delimetershortfall = 5.0*65536; // convert plain TeX pts to "scaled pts"
                    double deltasp = delta/96*72.27*65536;
                    deltasp = Math.Max(Math.Floor(deltasp/500)*delimeterfactor, 2*deltasp - delimetershortfall)/2;
                    delta = deltasp/65536/72.27*96;
                    Ascent = Descent = delta;
                }

                if(!FindSizedChar() && !FindComposition()) ScaleChar();
                CenterShift();
            }
            private const char STIX_RADICAL_SYMBOL_VERTICAL_EXTENDER = (char)0xe000;
            private const char STIX_RADICAL_SYMBOL_TOP_CORNER_PIECE = (char)0xe001;
            private bool IsIntegral(char c) {
                return (c >= Unicode.I.INTEGRAL && c <= Unicode.A.ANTICLOCKWISE_CONTOUR_INTEGRAL)
                    || (c >= Unicode.Q.QUADRUPLE_INTEGRAL_OPERATOR && c <= Unicode.I.INTEGRAL_WITH_UNDERBAR);
            }
            private static F[] IntegralFPath = { F.IntegralsSmall, F.General, F.IntegralsDisplay };
            private static F[] IntegralFPathB = { F.IntegralsSmallB, F.GeneralB, F.IntegralsDisplayB };
            private static F[] IntegralFPathUp = { F.IntegralsUprightSmall, F.IntegralsUprightRegular, F.IntegralsUprightDisplay };
            private static F[] IntegralFPathUpB = { F.IntegralsUprightSmallB, F.IntegralsUprightRegularB, F.IntegralsUprightDisplayB };
            private bool FindSizedChar() {
                F f;
                Rct bbox, nombbox;
                if(IsIntegral(C)) {
                    F[] fpath = EDC.UprightIntegrals ? (Bold ? IntegralFPathUpB : IntegralFPathUp) : (Bold ? IntegralFPathB : IntegralFPath);
                    int i = EDC.Nonscript ? 1 : 0;
                    for(; i < fpath.Length; i++) {
                        f = fpath[i];
                        if(EDC.Measure(C, f, out bbox, out nombbox)) {
                            _f = f; // we keep the biggest valid font found stored in _f,_bbox,_nombbox so ScaleChar can scale from the closest match if necessary
                            _bbox = bbox;
                            _nombbox = nombbox;
                            if(bbox.Height >= Ascent + Descent) {
                                _variant = Variant.Sized;
                                _uloffset = EDC.Midpt;
                                return true;
                            }
                        }
                    }
                } else {
                    f = Bold ? (Italic ? F.GeneralBI : F.GeneralB) : (Italic ? F.GeneralI : F.General);
                    if(EDC.Measure(C, f, out bbox, out nombbox)) {
                        _f = f; // we keep the biggest valid font found stored in _f,_bbox,_nombbox so ScaleChar can scale from the closest match if necessary
                        _bbox = bbox;
                        _nombbox = nombbox;
                        if(bbox.Height >= Ascent + Descent) {
                            _variant = Variant.Plain;
                            _uloffset = EDC.Midpt;
                            return true;
                        }
                    }
                    for(f = (Bold ? F.Sz1B : F.Sz1); f <= (Bold ? F.Sz4B : F.Sz5); f++) {
                        if(EDC.Measure(C, f, out bbox, out nombbox)) {
                            _f = f; // we keep the biggest valid font found stored in _f,_bbox,_nombbox so ScaleChar can scale from the closest match if necessary
                            _bbox = bbox;
                            _nombbox = nombbox; 
                            if (bbox.Height >= Ascent + Descent) {
                                _variant = Variant.Sized;
                                _uloffset = EDC.Midpt;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            private void ScaleChar() {
                // The font with the biggest valid version of this character has been kept in _f,_bbox,_nombbox so we can use it here.
                _variant = Variant.Scaled;
                _scale = (Ascent+Descent)/BBox.Height;
                _scaledEmSize = EDC.EmSize*_scale;
                _uloffset = EDC.Midpt*_scale;
                _bbox.Top *= _scale;
                _bbox.Left *= _scale;
                _bbox.Bottom *= _scale;
                _bbox.Right *= _scale;
                _nombbox.Top *= _scale;
                _nombbox.Left *= _scale;
                _nombbox.Bottom *= _scale;
                _nombbox.Right *= _scale;
            }
            private bool FindComposition() {
                if(!_compositions.TryGetValue(C, out _comp)) return false;
                _variant = Variant.Composed;
                double desiredH = Ascent + Descent;
                _comp.Measure(EDC, this, out _bboxes, out _nombboxes, out _offsets);
                if(_comp.Components.Count == 1) {
                    /* repeats to scale */
                    _repeatsize = desiredH;
                    _uloffset = 0;
                    _bbox = _bboxes[0] + new Vec(0, -_repeatsize/2);
                    _bbox.Bottom = _bbox.Top + _repeatsize;
                    _nombbox = _nombboxes[0] + new Vec(0, -_repeatsize/2);
                    _nombbox.Bottom = _bbox.Bottom + (_nombboxes[0].Bottom - _bboxes[0].Bottom);
                    return true;
                } else if(_comp.Components.Count == 2) {
                    /* Non-extensible; simply larger by using multiple nonextendable pieces */
                    double hgt = _bboxes[0].Height + _bboxes[1].Height;
                    if(desiredH < hgt - EDrawingContext.Metrics.OS2.sTypoLineGap*EDC.FScaleFactor) return false;
                    _scale = desiredH > hgt ? desiredH/hgt : 1;
                    _scaledEmSize = EDC.EmSize*_scale;
                    _uloffset = 0;
                    for(int i = 0; i < 2; i++) {
                        _bboxes[0] *= _scale;
                        _nombboxes[0] *= _scale;
                        _offsets[0] *= _scale;
                    }
                    _bbox = (_bboxes[0] + _bboxes[0].Height*Vec.Up).Union(_bboxes[1]);
                    _nombbox = (_nombboxes[0] + _bboxes[0].Height*Vec.Up).Union(_nombboxes[1]);
                    return true;
                } else if(_comp.Components.Count == 3) {
                    /* middle section repeats to scale */
                    double hgt = _bboxes[0].Height + _bboxes[2].Height;
                    if(desiredH < hgt - EDrawingContext.Metrics.OS2.sTypoLineGap*EDC.FScaleFactor) return false;
                    if(desiredH <= hgt) _repeatsize = 0;
                    else _repeatsize = desiredH - hgt;
                    _uloffset = 0;
                    _bbox = (_bboxes[0] + new Vec(0, -_bboxes[0].Height - _repeatsize/2)).Union(
                        _bboxes[2] + new Vec(0, _repeatsize/2));
                    _nombbox = (_nombboxes[0] + new Vec(0, -_bboxes[0].Height - _repeatsize/2)).Union(
                        _nombboxes[2] + new Vec(0, _repeatsize/2));
                    return true;
                } else if(_comp.Components.Count == 5) {
                    /* indexes 1 and 3 each repeat (this is used for braces only)*/
                    double hgt = _bboxes[0].Height + _bboxes[2].Height + _bboxes[4].Height;
                    if(desiredH < hgt - EDrawingContext.Metrics.OS2.sTypoLineGap*EDC.FScaleFactor) return false;
                    if(desiredH <= hgt) _repeatsize = 0;
                    else _repeatsize = desiredH - hgt; // divide by two for repeat side of each extender
                    _uloffset = 0;
                    _bbox = (_bboxes[0] + new Vec(0, -_bboxes[0].Height - _repeatsize/2 - _bboxes[2].Height/2)).Union(
                        _bboxes[2] + new Vec(0, -_bboxes[2].Height/2)).Union(
                        _bboxes[4] + new Vec(0, _bboxes[2].Height/2 + _repeatsize/2));
                    _nombbox = (_nombboxes[0] + new Vec(0, -_bboxes[0].Height - _repeatsize/2 - _bboxes[2].Height/2)).Union(
                        _nombboxes[2] + new Vec(0, -_bboxes[2].Height/2)).Union(
                        _nombboxes[4] + new Vec(0, _bboxes[2].Height/2 + _repeatsize/2));
                    return true;
                } else throw new Exception("This shouldn't have happened: composite char found with unhandled # of components");
            }

            private void CenterShift() {
                // see TeXBook appendix G rule 11 for sqrt handling, rule 19 for other delimiter handling
                double cctr = (_bbox.Top + _bbox.Bottom)/2;
                double contentsctr = (-Ascent + Descent)/2.0f;
                double shift = contentsctr - cctr;
                _uloffset += shift;
                Vec sh = new Vec(0, shift);
                _bbox += sh;
                _nombbox += sh;
            }

            public void Draw(EDrawingContext edc, Pt refpt) {
                switch(_variant) {
                    case Variant.Plain:
                    case Variant.Sized:
                        edc.Draw(C, _f, 1.0, refpt + _uloffset*Vec.Down);
                        break;
                    case Variant.Composed:
                        double overlap = Math.Max(1, EDrawingContext.Metrics.MATHvariants.MinConnectorOverlap*edc.FScaleFactor);
                        switch(_comp.Components.Count) {
                            case 1:
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 + _offsets[0] + _uloffset);
                                    double lenleft = _repeatsize + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    double olap = 0;
                                    while(lenleft > _bboxes[0].Height - olap) {
                                        edc.Draw(_comp.Components[0], _comp.F(0, this), 1, l + olap*Vec.Up);
                                        lenleft -= _bboxes[0].Height - olap;
                                        l.Y += _bboxes[0].Height - olap;
                                        olap = overlap;
                                    }
                                    l += olap*Vec.Up;
                                    Rct clip = _bboxes[0] + (Vec)l - new Vec(0, _offsets[0]);
                                    clip.Bottom = clip.Top + lenleft + olap;
                                    try {
                                        edc.DC.PushClip(new RectangleGeometry(clip.Inflate(5f, 0f)));
                                        edc.Draw(_comp.Components[0], _comp.F(0, this), 1, l);
                                    } finally {
                                        edc.DC.Pop();
                                    }
                                }
                                break;
                            case 2:
                                edc.Draw(_comp.Components[0], _comp.F(0, this), _scale, refpt + (-_bboxes[0].Height + _offsets[0] + _uloffset)*Vec.Down);
                                edc.Draw(_comp.Components[1], _comp.F(1, this), _scale, refpt + (_offsets[1] + _uloffset)*Vec.Down);
                                break;
                            case 3:
                                edc.Draw(_comp.Components[0], _comp.F(0, this), 1, refpt + new Vec(0, -_bboxes[0].Height - _repeatsize/2 + _offsets[0] + _uloffset));
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[1].Height - overlap) {
                                        edc.Draw(_comp.Components[1], _comp.F(1, this), 1, l + overlap*Vec.Up);
                                        lenleft -= _bboxes[1].Height - overlap;
                                        l.Y += _bboxes[1].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[1] + (Vec)l - new Vec(0, _offsets[1]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    try {
                                        edc.DC.PushClip(new RectangleGeometry(clip.Inflate(5f, 0f)));
                                        edc.Draw(_comp.Components[1], _comp.F(1, this), 1, l);
                                    } finally {
                                        edc.DC.Pop();
                                    }
                                }
                                edc.Draw(_comp.Components[2], _comp.F(2, this), 1, refpt + new Vec(0, _repeatsize/2 + _offsets[2] + _uloffset));
                                break;
                            case 5:
                                edc.Draw(_comp.Components[0], _comp.F(0, this), 1, refpt + new Vec(0, -_bboxes[0].Height - _repeatsize/2 - _bboxes[2].Height/2 + _offsets[0] + _uloffset));
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 - _bboxes[2].Height/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize/2 + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[1].Height - overlap) {
                                        edc.Draw(_comp.Components[1], _comp.F(1, this), 1, l + overlap*Vec.Up);
                                        lenleft -= _bboxes[1].Height - overlap;
                                        l.Y += _bboxes[1].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[1] + (Vec)l - new Vec(0, _offsets[1]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    try {
                                        edc.DC.PushClip(new RectangleGeometry(clip.Inflate(5f, 0f)));
                                        edc.Draw(_comp.Components[1], _comp.F(1, this), 1, l);
                                    } finally {
                                        edc.DC.Pop();
                                    }
                                }
                                edc.Draw(_comp.Components[2], _comp.F(2, this), 1, refpt + new Vec(0, -_bboxes[2].Height/2 + _offsets[2] + _uloffset));
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, _bboxes[2].Height/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize/2 + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[3].Height - overlap) {
                                        edc.Draw(_comp.Components[3], _comp.F(3, this), 1, l + overlap*Vec.Up);
                                        lenleft -= _bboxes[3].Height - overlap;
                                        l.Y += _bboxes[3].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[3] + (Vec)l - new Vec(0, _offsets[3]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    try {
                                        edc.DC.PushClip(new RectangleGeometry(clip.Inflate(5f, 0f)));
                                        edc.Draw(_comp.Components[3], _comp.F(3, this), 1, l);
                                    } finally {
                                        edc.DC.Pop();
                                    }
                                }
                                edc.Draw(_comp.Components[4], _comp.F(4, this), 1, refpt + new Vec(0, _bboxes[2].Height/2 + _repeatsize/2 + _offsets[4] + _uloffset));
                                break;
                            default:
                                throw new Exception("This shouldn't happen: unhandled number of components for composite character");
                        }
                        break;
                    case Variant.Scaled:
                        edc.Draw(C, _f, _scale, refpt + _uloffset*Vec.Down);
                        break;
                }
            }
            public Geometry GetGeometry(EDrawingContext edc, Pt refpt) {
                Geometry g;
                switch(_variant) {
                    case Variant.Plain:
                    case Variant.Sized:
                        edc.GetGeometry(C, _f, 1.0, refpt + _uloffset*Vec.Down, out g);
                        return g;
                    case Variant.Composed:
                        List<Geometry> gg = new List<Geometry>();
                        double overlap = Math.Max(1, EDrawingContext.Metrics.MATHvariants.MinConnectorOverlap*edc.FScaleFactor);
                        switch(_comp.Components.Count) {
                            case 1:
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 + _offsets[0] + _uloffset);
                                    double lenleft = _repeatsize + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    double olap = 0;
                                    while(lenleft > _bboxes[0].Height - olap) {
                                        edc.GetGeometry(_comp.Components[0], _comp.F(0, this), 1, l + olap*Vec.Up, out g);
                                        gg.Add(g);
                                        lenleft -= _bboxes[0].Height - olap;
                                        l.Y += _bboxes[0].Height - olap;
                                        olap = overlap;
                                    }
                                    l += olap*Vec.Up;
                                    Rct clip = _bboxes[0] + (Vec)l - new Vec(0, _offsets[0]);
                                    clip.Bottom = clip.Top + lenleft + olap;
                                    RectangleGeometry rc = new RectangleGeometry(clip.Inflate(5f, 0f));
                                    edc.GetGeometry(_comp.Components[0], _comp.F(0, this), 1, l, out g);
                                    gg.Add(new CombinedGeometry(GeometryCombineMode.Intersect, rc, g));
                                }
                                break;
                            case 2:
                                edc.GetGeometry(_comp.Components[0], _comp.F(0, this), _scale, refpt + (-_bboxes[0].Height + _offsets[0] + _uloffset)*Vec.Down, out g);
                                gg.Add(g);
                                edc.GetGeometry(_comp.Components[1], _comp.F(1, this), _scale, refpt + (_offsets[1] + _uloffset)*Vec.Down, out g);
                                gg.Add(g);
                                break;
                            case 3:
                                edc.GetGeometry(_comp.Components[0], _comp.F(0, this), 1, refpt + new Vec(0, -_bboxes[0].Height - _repeatsize/2 + _offsets[0] + _uloffset), out g);
                                gg.Add(g);
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[1].Height - overlap) {
                                        edc.GetGeometry(_comp.Components[1], _comp.F(1, this), 1, l + overlap*Vec.Up, out g);
                                        gg.Add(g);
                                        lenleft -= _bboxes[1].Height - overlap;
                                        l.Y += _bboxes[1].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[1] + (Vec)l - new Vec(0, _offsets[1]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    RectangleGeometry rc = new RectangleGeometry(clip.Inflate(5f, 0f));
                                    edc.GetGeometry(_comp.Components[1], _comp.F(1, this), 1, l, out g);
                                    gg.Add(new CombinedGeometry(GeometryCombineMode.Intersect, rc, g));
                                }
                                edc.GetGeometry(_comp.Components[2], _comp.F(2, this), 1, refpt + new Vec(0, _repeatsize/2 + _offsets[2] + _uloffset), out g);
                                gg.Add(g);
                                break;
                            case 5:
                                edc.GetGeometry(_comp.Components[0], _comp.F(0, this), 1, refpt + new Vec(0, -_bboxes[0].Height - _repeatsize/2 - _bboxes[2].Height/2 + _offsets[0] + _uloffset), out g);
                                gg.Add(g);
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, -_repeatsize/2 - _bboxes[2].Height/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize/2 + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[1].Height - overlap) {
                                        edc.GetGeometry(_comp.Components[1], _comp.F(1, this), 1, l + overlap*Vec.Up, out g);
                                        gg.Add(g);
                                        lenleft -= _bboxes[1].Height - overlap;
                                        l.Y += _bboxes[1].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[1] + (Vec)l - new Vec(0, _offsets[1]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    RectangleGeometry rc = new RectangleGeometry(clip.Inflate(5f, 0f));
                                    edc.GetGeometry(_comp.Components[1], _comp.F(1, this), 1, l, out g);
                                    gg.Add(new CombinedGeometry(GeometryCombineMode.Intersect, rc, g));
                                }
                                edc.GetGeometry(_comp.Components[2], _comp.F(2, this), 1, refpt + new Vec(0, -_bboxes[2].Height/2 + _offsets[2] + _uloffset), out g);
                                gg.Add(g);
                                if(_repeatsize > 0) {
                                    Pt l = refpt + new Vec(0, _bboxes[2].Height/2 + _offsets[1] + _uloffset);
                                    double lenleft = _repeatsize/2 + overlap; // consider the overlap bit at the bottom as part of length to draw
                                    // each time, overlap with previous glyph and consider distance done to be that much less
                                    while(lenleft > _bboxes[3].Height - overlap) {
                                        edc.GetGeometry(_comp.Components[3], _comp.F(3, this), 1, l + overlap*Vec.Up, out g);
                                        gg.Add(g);
                                        lenleft -= _bboxes[3].Height - overlap;
                                        l.Y += _bboxes[3].Height - overlap;
                                    }
                                    l += overlap*Vec.Up;
                                    Rct clip = _bboxes[3] + (Vec)l - new Vec(0, _offsets[3]);
                                    clip.Bottom = clip.Top + lenleft + 2*overlap;
                                    RectangleGeometry rc = new RectangleGeometry(clip.Inflate(5f, 0f));
                                    edc.GetGeometry(_comp.Components[3], _comp.F(3, this), 1, l, out g);
                                    gg.Add(new CombinedGeometry(GeometryCombineMode.Intersect, rc, g));
                                }
                                edc.GetGeometry(_comp.Components[4], _comp.F(4, this), 1, refpt + new Vec(0, _bboxes[2].Height/2 + _repeatsize/2 + _offsets[4] + _uloffset), out g);
                                gg.Add(g);
                                break;
                            default:
                                throw new Exception("This shouldn't happen: unhandled number of components for composite character");
                        }
                        if(gg.Count == 1) return gg[0];
                        else return gg.Aggregate((g1, g2) => new CombinedGeometry(GeometryCombineMode.Union, g1, g2));
                    case Variant.Scaled:
                        edc.GetGeometry(C, _f, _scale, refpt + _uloffset*Vec.Down, out g);
                        return g;
                    default:
                        throw new Exception("This should not have happened: BigC with unknown variant");
                }
            }
            private class Composite {
                public List<char> Components;
                public F[] ExtensionF;
                public Composite(F extF, params char[] components) { ExtensionF = new F[] { extF }; Components = new List<char>(components); }
                public Composite(F[] extF, params char[] components) { ExtensionF = extF; Components = new List<char>(components); }
                // bboxes and nombboxes get the boxes with top moved to Y=0; offsets gets val to add to where you want top to be to where you should draw the char
                public void Measure(EDrawingContext edc, BigC c, out List<Rct> bboxes, out List<Rct> nombboxes, out List<double> offsets) {
                    bboxes = new List<Rct>(Components.Count);
                    nombboxes = new List<Rct>(Components.Count);
                    offsets = new List<double>(Components.Count);
                    for(int i = 0; i < Components.Count; i++) {
                        Rct bbox, nombbox;
                        edc.Measure(Components[i], F(i, c), out bbox, out nombbox);
                        bboxes.Add(bbox + bbox.Top*Vec.Up);
                        nombboxes.Add(nombbox + bbox.Top*Vec.Up);
                        offsets.Add(-bbox.Top + edc.Midpt);
                    }
                }
                public F F(int i, BigC c) {
                    F f = ExtensionF[Math.Min(i, ExtensionF.Length - 1)];
                    if(f == EDrawingContext.F.General) f += (c.Bold?1:0) + (c.Italic?2:0);
                    return f;
                }
            }
            private static Dictionary<char, Composite> _compositions; private Dictionary<char, Composite> Compositions { get { return _compositions; } }
            static private void AddComposition(char c, F extF, params char[] composites) { _compositions[c] = new Composite(extF, composites); }
            static private void AddComposition(char c, F[] extF, params char[] composites) { _compositions[c] = new Composite(extF, composites); }
            static BigC() {
                _compositions = new Dictionary<char, Composite>();
                AddComposition('|', F.General, '|');
                AddComposition(Unicode.D.DOUBLE_VERTICAL_LINE, F.General, Unicode.D.DOUBLE_VERTICAL_LINE);
                AddComposition(Unicode.S.SQUARE_ROOT, new F[] { F.Nonuni, F.Nonuni, F.Sz1 }, STIX_RADICAL_SYMBOL_TOP_CORNER_PIECE, STIX_RADICAL_SYMBOL_VERTICAL_EXTENDER, Unicode.R.RADICAL_SYMBOL_BOTTOM);
                AddComposition(Unicode.N.N_ARY_SUMMATION, F.Sz1, Unicode.S.SUMMATION_TOP, Unicode.S.SUMMATION_BOTTOM);
                AddComposition(Unicode.I.INTEGRAL, F.Sz1, Unicode.T.TOP_HALF_INTEGRAL, Unicode.I.INTEGRAL_EXTENSION, Unicode.B.BOTTOM_HALF_INTEGRAL);
                AddComposition(Unicode.L.LEFT_PARENTHESIS, F.Sz1, Unicode.L.LEFT_PARENTHESIS_UPPER_HOOK, Unicode.L.LEFT_PARENTHESIS_EXTENSION, Unicode.L.LEFT_PARENTHESIS_LOWER_HOOK);
                AddComposition(Unicode.R.RIGHT_PARENTHESIS, F.Sz1, Unicode.R.RIGHT_PARENTHESIS_UPPER_HOOK, Unicode.R.RIGHT_PARENTHESIS_EXTENSION, Unicode.R.RIGHT_PARENTHESIS_LOWER_HOOK);
                AddComposition(Unicode.L.LEFT_SQUARE_BRACKET, F.Sz1, Unicode.L.LEFT_SQUARE_BRACKET_UPPER_CORNER, Unicode.L.LEFT_SQUARE_BRACKET_EXTENSION, Unicode.L.LEFT_SQUARE_BRACKET_LOWER_CORNER);
                AddComposition(Unicode.R.RIGHT_SQUARE_BRACKET, F.Sz1, Unicode.R.RIGHT_SQUARE_BRACKET_UPPER_CORNER, Unicode.R.RIGHT_SQUARE_BRACKET_EXTENSION, Unicode.R.RIGHT_SQUARE_BRACKET_LOWER_CORNER);
                AddComposition(Unicode.L.LEFT_CURLY_BRACKET, F.Sz1, Unicode.L.LEFT_CURLY_BRACKET_UPPER_HOOK, Unicode.C.CURLY_BRACKET_EXTENSION, Unicode.L.LEFT_CURLY_BRACKET_MIDDLE_PIECE, Unicode.C.CURLY_BRACKET_EXTENSION, Unicode.L.LEFT_CURLY_BRACKET_LOWER_HOOK);
                AddComposition(Unicode.R.RIGHT_CURLY_BRACKET, F.Sz1, Unicode.R.RIGHT_CURLY_BRACKET_UPPER_HOOK, Unicode.C.CURLY_BRACKET_EXTENSION, Unicode.R.RIGHT_CURLY_BRACKET_MIDDLE_PIECE, Unicode.C.CURLY_BRACKET_EXTENSION, Unicode.R.RIGHT_CURLY_BRACKET_LOWER_HOOK);
            }
        }
        public BigC Big(char c, double ascent, double descent) {
            return Big(c, false, false, false, ascent, descent);
        }
        public BigC Big(char c, bool emph, double ascent, double descent) {
            return Big(c, false, false, emph, ascent, descent);
        }
        public BigC Big(char c, bool bold, bool italic, bool emph, double ascent, double descent) {
            return new BigC(c, bold, italic, emph, ascent, descent, this);
        }
        public double Draw(BigC big, Pt refpt) {
            big.Draw(this, refpt);
            return big.NomBBox.Right;
        }
        public double GetGeometry(BigC big, Pt refpt, out Geometry g) {
            g = big.GetGeometry(this, refpt);
            return big.NomBBox.Right;
        }
    }
}
