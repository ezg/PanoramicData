using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;

namespace starPadSDK.MathExpr {
    public class MapleEngine : Engine {
        // These DllImport, type, and constant declarations all come from OpenMaple include files extern/include/maplec.h and .../mplshlib.h
        /* typedef int ***ALGEB, M_BOOL */
        /* MarshalAs attribute for ANSI/ASCII string callback args */
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        private struct MCallBackVectorDesc {
            public delegate void textCBType(IntPtr data, int tag, string output);
            public textCBType textCallBack;
            public delegate void errorCBType(IntPtr data, int offset, string msg);
            public errorCBType errorCallBack;
            public delegate void statusCBType(IntPtr data, long kilobytesUsed, long kilobytesAlloc, double cpuTime);
            public statusCBType statusCallBack;
            public delegate string readLineCBType(IntPtr data, bool debug);
            public readLineCBType readLineCallBack;
            public delegate bool redirectCBType(IntPtr data, string name, string mode);
            public redirectCBType redirectCallBack;
            public delegate string streamCBType(IntPtr data, string stream, int nargs, /* char** */string[] args);
            public streamCBType streamCallBack;
            public delegate bool queryInterruptType(IntPtr data);
            public queryInterruptType queryInterrupt;
            public delegate string callBackCBType(IntPtr data, string output);
            public callBackCBType callBackCallBack;
        }
        /* Possible values for the tag parameter to the textCallBack function. */
        private const int MAPLE_TEXT_DIAG = 1;
        private const int MAPLE_TEXT_MISC = 2;
        private const int MAPLE_TEXT_OUTPUT = 3;
        private const int MAPLE_TEXT_QUIT = 4;
        private const int MAPLE_TEXT_WARNING = 5;
        private const int MAPLE_TEXT_ERROR = 6;
        private const int MAPLE_TEXT_STATUS = 7;
        private const int MAPLE_TEXT_PRETTY = 8;
        private const int MAPLE_TEXT_HELP = 9;
        private const int MAPLE_TEXT_DEBUG = 10;
        /* Maple object ids */
        enum MapleID {
            MAPLE_INTNEG=1,
            MAPLE_INTPOS,
            MAPLE_RATIONAL,
            MAPLE_FLOAT,
            MAPLE_HFLOAT,
            MAPLE_COMPLEX,
            MAPLE_STRING,
            MAPLE_NAME,
            MAPLE_MEMBER,
            MAPLE_TABLEREF,
            MAPLE_DCOLON,
            MAPLE_CATENATE,
            MAPLE_POWER,
            MAPLE_PROD,
            MAPLE_SERIES,
            MAPLE_SUM,
            MAPLE_ZPPOLY,
            MAPLE_SDPOLY,
            MAPLE_FUNCTION,
            MAPLE_UNEVAL,
            MAPLE_EQUATION,
            MAPLE_INEQUAT,
            MAPLE_LESSEQ,
            MAPLE_LESSTHAN,
            MAPLE_AND,
            MAPLE_NOT,
            MAPLE_OR,
            MAPLE_XOR,
            MAPLE_IMPLIES,
            MAPLE_EXPSEQ,
            MAPLE_LIST,
            MAPLE_LOCAL,
            MAPLE_PARAM,
            MAPLE_LEXICAL,
            MAPLE_PROC,
            MAPLE_RANGE,
            MAPLE_SET,
            MAPLE_TABLE,
            MAPLE_RTABLE,
            MAPLE_MODDEF,
            MAPLE_MODULE,
            MAPLE_ASSIGN,
            MAPLE_FOR,
            MAPLE_IF,
            MAPLE_READ,
            MAPLE_SAVE,
            MAPLE_STATSEQ,
            MAPLE_STOP,
            MAPLE_ERROR,
            MAPLE_TRY,
            MAPLE_RETURN,
            MAPLE_BREAK,
            MAPLE_NEXT,
            MAPLE_USE
        }
        /* extern EXT_DECL MKernelVector  StartMaple( int argc, char *argv[], MCallBackVector cb, void *user_data, void *info, char *errstr );
         * extern EXT_DECL void  StopMaple( MKernelVector kv );
         * extern EXT_DECL M_BOOL  RestartMaple( MKernelVector kv, char *errstr );*/
        /* EXT_DECL ALGEB  EvalMapleStatement( MKernelVector kv, char *statement ); */
        /* MKernelVector (kv) is a pointer to a struct which is actually defined in the header files, but we don't need any of its contents, so represent by IntPtr */
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern IntPtr StartMaple(int argc, string[] argv, ref MCallBackVectorDesc cb, IntPtr user_data, IntPtr info, StringBuilder errstr);
        [DllImport("maplec.dll")]
        private static extern void StopMaple(IntPtr kv);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern bool RestartMaple(IntPtr kv, StringBuilder errstr);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi, BestFitMapping=false, ThrowOnUnmappableChar=true)]
        private static extern IntPtr EvalMapleStatement(IntPtr kv, string statement);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern IntPtr/*ALGEB*/ MapleEval(IntPtr kv, IntPtr/*ALGEB*/ s);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern bool IsMapleString(IntPtr kv, IntPtr/*ALGEB*/ s);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern string MapleToString(IntPtr kv, IntPtr/*ALGEB*/ s);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi, BestFitMapping=false, ThrowOnUnmappableChar=true)]
        private static extern IntPtr/*ALGEB*/ ToMapleString(IntPtr kv, string s);
        // FIXME: p/invoke doesn't support varargs stuff here?
        [DllImport("maplec.dll", CharSet=CharSet.Ansi, EntryPoint="ToMapleFunction", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr/*ALGEB*/ ToMapleFunction1(IntPtr kv, IntPtr/*ALGEB*/ fname, int nargs, IntPtr/*ALGEB*/ arg1);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi, BestFitMapping=false, ThrowOnUnmappableChar=true)]
        private static extern IntPtr/*ALGEB*/ ToMapleName(IntPtr kv, string n, bool is_global);
        [DllImport("maplec.dll", CharSet=CharSet.Ansi)]
        private static extern IntPtr/*ALGEB*/ MapleNew(IntPtr kv, MapleID id, int len);


        private string QuoteAsMapleString(XmlDocument input) {
            // Quote the string; also must convert to the ASCII format Maple's function needs, alas...
            // FIXME: should quote double quotes and anything else needing it
            string str = "\"" + input.OuterXml + "\"";
            StringBuilder sb = new StringBuilder(str);
            for(int i = 0; i < sb.Length; i++) {
                int c = (int)sb[i];
                if(c > 0x7f) {
                    sb.Remove(i, 1);
                    sb.Insert(i, String.Format("&#{0};", c));
                }
            }
            return sb.ToString();
        }
        public Expr Run(Expr e, string evalfn) {
            // ./OpenMapleCHook.exe 'MathML[ExportPresentation](MathML[Import]("<math><mrow><mn>3</mn><mo>+</mo><mn>2</mn></mrow></math>"));'
            XmlDocument input = (new MathML(true, true)).Convert(e);
            IntPtr ALGEB = EvalMapleStatement(_kv, "MathML[ExportPresentation](" + evalfn + "(MathML[Import](" + QuoteAsMapleString(input) + "))):");
            //IntPtr ALGEB = MapleEval(_kv, ToMapleFunction1(_kv, ToMapleName(_kv, "MathML[ExportPresentation]", true), 1,
            //    ToMapleFunction1(_kv, ToMapleName(_kv, "MathML[Import]", true), 1, ToMapleString(_kv, expr))));
            if(IsMapleString(_kv, ALGEB)) {
                string output = MapleToString(_kv, ALGEB);
                return MathML.Convert(output);
            } else {
                // FIXME if _curOutput is null, must convert ALGEB to a string and indicate to user don't know why didn't do right thing
                throw new Exception(_curOutput);
            }
        }

        public override Expr _Simplify(Expr e) {
            return Run(e, "eval");
        }
        public override Expr _Approximate(Expr e) {
            return Run(e, "evalf");
        }

        public override Expr _Substitute(Expr e, Expr orig, Expr replacement) {
            /* Maybe we can use Maple here instead? */
            return (new BuiltInEngine())._Substitute(e, orig, replacement);
        }
        public override Expr _Substitute(Expr e, MathConstant[] consts) {
            /* Maybe we can use Maple here instead? */
            return (new BuiltInEngine())._Substitute(e, consts);
        }
        public override Expr _Replace(Expr e, Expr orig, Expr replacement) {
            /* Maybe we can use Maple here instead? */
            return (new BuiltInEngine())._Replace(e, orig, replacement);
        }

        public override string Name {
            get { return "Maple"; }
        }

        private string _curOutput = null, _command = null;
        private IntPtr _kv = IntPtr.Zero; /* MKernelVector */
        private MCallBackVectorDesc _cb;
        private bool _exists;
        public override bool Exists { get { return _exists; } }

        public MapleEngine() {
            _cb = new MCallBackVectorDesc();
            _cb.textCallBack = textCallback;
            _cb.errorCallBack = errorCallback;
            //cb.queryInterrupt = queryInterrupt;

            _exists = System.IO.File.Exists(@"C:\WINDOWS\system32\maplec.dll");
        }

        private void errorCallback(IntPtr data, int offset, string errmsg) {
            _curOutput = "MAPLE ERROR @" + offset + "\n" + errmsg;
        }
        private void textCallback(IntPtr data, int tag, string output) {
            _curOutput = output;
        }

        public override void Activate() {
            _curOutput = null;
            _command = null;
            StringBuilder errbuf = new StringBuilder(new string((char)0, 4096));
            _kv = StartMaple(0, null, ref _cb, IntPtr.Zero, IntPtr.Zero, errbuf);
            if(_kv == IntPtr.Zero) throw new ApplicationException("Error starting Maple: " + errbuf.ToString());
        }

        public override void Deactivate() {
            if(_kv != IntPtr.Zero) {
                StopMaple(_kv);
                _kv = IntPtr.Zero;
            }
        }
    }
}
