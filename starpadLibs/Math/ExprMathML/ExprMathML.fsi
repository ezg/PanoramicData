#light

namespace starPadSDK.MathExpr
open System.Xml

type MathML =
    new : unit -> MathML
    new : string -> MathML
    new : bool * bool -> MathML
    new : bool * bool * string * bool -> MathML
    member Convert : e:Expr -> XmlDocument
    static member Namespace : string
    static member Convert : s:string -> Expr
