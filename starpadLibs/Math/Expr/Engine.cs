using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace starPadSDK.MathExpr
{
    [Serializable()]
    public class MathConstant
    {
        public Expr Name;
        public Expr Value;
        public MathConstant(Expr name, Expr value) { Name = name; Value = value; }
    }
    public abstract class Engine
    {
        static Engine[]      _engines;
        static Engine        _current;
        static Stack<Engine> _engineStack = new Stack<Engine>();

        public  static Engine[] Engines { get { return _engines; } set { _engines = value; } }
        public  static Engine   Current
        {
            get { EngineLoader.Init();  return _current; }
            set { if (_current != null) _current.Deactivate(); _current = value; _current.Activate(); }
        }
        public  static void     PushEngine(Engine e)
        {
            _engineStack.Push(_current);
            _current = e;
            _current.Activate();
        }
        public  static void     PopEngine()
        {
            if (_engineStack.Count != 0)
            {
                _current.Deactivate();
                _current = _engineStack.Pop();
            }
        }
        /// <summary>
        /// clears out any cached data for Math engines that have limited stacks, etc
        /// </summary>
        public  static void     ClearAll() { }
        public  static Expr     Simplify(Expr e) { return Current._Simplify(e); }
        public  static Expr     Approximate(Expr e) { return Current._Approximate(e); }
        public  static Expr     Substitute(Expr e, Expr orig, Expr replacement) { return Current._Substitute(e, orig, replacement); }
        public  static Expr     Substitute(Expr e, MathConstant[] consts) { return Current._Substitute(e, consts); }
        public  static Expr     Replace(Expr e, Expr orig, Expr replacement) { return Current._Replace(e, orig, replacement); }

        /// <summary>
        /// Try to simplify the expression. (For some meaning of "simplify".)
        /// The original expression should not be modified.
        /// </summary>
        public abstract Expr   _Simplify(Expr e);
        /// <summary>
        /// Attempt to perform numeric conversion and evaluation similar to Mathematica's N[] function.
        /// The original expression should not be modified.
        /// </summary>
        public abstract Expr   _Approximate(Expr e);
        /// <summary>
        /// Precise semantics of this are up to the Engine, but it should at least support literal replacement with orig a Sym.
        /// The original expression should not be modified.
        /// </summary>
        public abstract Expr   _Substitute(Expr e, Expr orig, Expr replacement);
        public abstract Expr   _Substitute(Expr e, MathConstant[] consts);
        /// <summary>
        /// Precise semantics of this are up to the Engine, but it should at least support literal replacement with orig a Sym.
        /// The original expression should not be modified.
        /// Replaces the expression using an Object refernce test
        /// </summary>
        public abstract Expr   _Replace(Expr e, Expr orig, Expr replacement);
        /// <summary>
        /// Names of this engine's variants, for display to the user, or null for no variants.
        /// </summary>
        public virtual string[] Names { get { return null; } }
        /// <summary>
        /// Name of this engine, for display to the user.
        /// </summary>
        public abstract string  Name { get; }
        /// <summary>
        /// Pick which variant to use, from the set named in Name
        /// </summary>
        public virtual int      Variant { get { return 0; } set { Deactivate(); } }
        /// <summary>
        /// Perform any startup activation needed (connecting to an external program, etc.). Use Deactivate to deactivate.
        /// </summary>
        public abstract void    Activate();
        public abstract void    Deactivate();
        /// <summary>
        /// Does the underlying program or platform for this engine exist? If this returns false, the engine will be omitted from the Engines list.
        /// </summary>
        public virtual bool     Exists { get { return true; } }
    }
}
