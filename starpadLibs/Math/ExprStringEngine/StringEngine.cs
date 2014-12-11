using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace starPadSDK.MathExpr {
    /// <summary>
    /// This is for certain engines that need to be loaded with special search paths and can use MathML to interoperate. Don't use or worry about it otherwise.
    /// </summary>
    public interface StringEngine {
        string Simplify(string mathml);
        string Approximate(string mathml);
        /// <summary>
        /// Perform any startup activation needed (connecting to an external program, etc.). Use Deactivate to deactivate.
        /// </summary>
        void Activate();
        void Deactivate();
    }
    [global::System.AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public class EngineNameAttribute : Attribute {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _engineName;

        // This is a positional argument
        public EngineNameAttribute(string engineName) {
            _engineName = engineName;
        }

        public string EngineName {
            get { return _engineName; }
        }
    }
    [global::System.AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=true)]
    public class ExtraDLLAttribute : Attribute {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _dllName;

        // This is a positional argument
        public ExtraDLLAttribute(string dllName) {
            _dllName = dllName;
        }

        public string DLLName {
            get { return _dllName; }
        }
    }
}
