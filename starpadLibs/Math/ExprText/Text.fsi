#light

namespace starPadSDK.MathExpr
open System
open starPadSDK.MathExpr

type TextAwareExpr =
    abstract Convert : Func<Expr,string> -> string
    abstract InputConvert : Func<Expr,string> -> string

type TextParseException =
    inherit Exception
    new : int * int * int * string * Exception -> TextParseException
    member Line : int with get
    member Character : int with get
    member Column : int with get
    member Lexeme : string with get

type Text =
    new : unit -> Text
    static member Convert : e:Expr -> string
    static member InputConvert : e:Expr -> string
    static member Convert : Byte[] -> Expr
