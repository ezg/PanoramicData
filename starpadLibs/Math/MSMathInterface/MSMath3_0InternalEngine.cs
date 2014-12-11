using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MicrosoftMath;
using System.Reflection;
using System.IO;

namespace starPadSDK.MathExpr {
    class ExtraProgramsDLLAttribute : ExtraDLLAttribute {
        public ExtraProgramsDLLAttribute(string tail) : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), tail)) {
        }
    }
    [EngineName("Microsoft Math 3.0")]
    [ExtraProgramsDLL("Microsoft Math 3.0\\MATHENGINE.DLL")]
    public class MSMath3_0InternalEngine : MarshalByRefObject, StringEngine {
        private Microsoft.MicrosoftMath.CasContext _cc = null;
        public MSMath3_0InternalEngine() {
        }

        public string Simplify(string mathml) {
            Expression mse = _cc.ParseInput(mathml);
            Expression mse2 = _cc.Execute(mse); // -- for use without solve
            //EquationRequestExpression ere = _cc.CreateEquationRequest(mse);
            //EquationSolutionSetExpression soln = _cc.SolveEquations(ere);
            string result = _cc.Serialize(_cc.Typeset(mse2, TypesetOptions.Full));
            return result;
        }

        public string Approximate(string mathml) {
            Expression mse = _cc.ParseInput(mathml);
            Expression mse2 = _cc.Execute(mse); // -- for use without solve
            //EquationRequestExpression ere = _cc.CreateEquationRequest(mse);
            //EquationSolutionSetExpression soln = _cc.SolveEquations(ere);
            Expression mse3;
            try {
                mse3 = _cc.NumericEvaluate(mse2, false);
            } catch(EvaluationException ee) {
                return "<math><merror>" + Enum.GetName(typeof(EvaluationErrorCode), ee.ErrorCode) + "</merror></math>";
            }
            string result = _cc.Serialize(_cc.Typeset(mse3, TypesetOptions.Full));
            return result;
        }

        public void Activate() {
            _cc = new CasContext();
            _cc.EvalOptions.NumberField = EvalNumberField.Complex;
            // Formula means as brackets: Equal[Exponent[x,2],4] etc
            // Linear(Input?) means conventional form as the user likely typed it in: x^2 = 4
            _cc.FormatOptions.FormatType = FormatType.MathML;
            _cc.FormatOptions.UseBoldForConstants = false;
            _cc.ParsingOptions.FormatType = FormatType.MathML;
        }

        public void Deactivate() {
            _cc = null;
        }
    }
}
