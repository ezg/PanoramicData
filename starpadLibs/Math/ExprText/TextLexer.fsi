#light

module starPadSDK.MathExpr.TextInternals.Lexer
open System
open starPadSDK.MathExpr

val token : Lexing.LexBuffer<byte> -> Parser.token
