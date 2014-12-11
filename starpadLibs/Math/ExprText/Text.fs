#light

namespace starPadSDK.MathExpr

open starPadSDK.UnicodeNs
open System
open System.IO
open System.Diagnostics
open System.Reflection
open System.Collections.Generic
open starPadSDK.MathExpr
open starPadSDK.MathExpr.TextInternals

(* Transformation of Exprs to and from simple text representations. Originally, this was just for debugging, so don't expect it to be
   very robust. Round-trip integrity is not guaranteed, for instance. *)

/// This only applies to ErrorExprs.
type public TextAwareExpr =
    abstract Convert : Func<Expr,string> -> string
    abstract InputConvert : Func<Expr,string> -> string

type public TextParseException(line, char, col, lexeme, inner:Exception) =
    inherit Exception(sprintf "Parse error \"%s\" near line %d, character %d, column %d possibly concerning %s" inner.Message line char col lexeme, inner)
    member public t.Line with get() = line
    member public t.Character with get() = char
    member public t.Column with get() = col
    member public t.Lexeme with get() = lexeme

type public Text() =
    static let (|ErrorMsgExpr|_|) (e:Expr) = match e with :? ErrorMsgExpr as eme -> Some(eme.Msg) | _ -> None
    static let  (|ErrorExpr|_|) (e:Expr) = match e with :? ErrorExpr -> Some() | _ -> None
    static let (|NullExpr|_|) (e:Expr) = match e with :? NullExpr -> Some() | _ -> None
    static let (|CompositeExpr|_|) (e:Expr) = match e with :? CompositeExpr as ce -> Some(ce.Head, ce.Args) | _ -> None
    static let (|DoubleNumber|_|) (e:Expr) = match e with :? DoubleNumber as d -> Some(d.Num) | _ -> None
    static let (|IntegerNumber|_|) (e:Expr) = match e with :? IntegerNumber as i -> Some(i.Num) | _ -> None
    static let (|RationalNumber|_|) (e:Expr) = match e with :? RationalNumber as r -> Some(r.Num) | _ -> None
    static let (|ComplexNumber|_|) (e:Expr) = match e with :? ComplexNumber as c -> Some(c.Re, c.Im) | _ -> None
    static let (|ArrayExpr|_|) (e:Expr) = match e with :? ArrayExpr as ae -> Some(ae.Elts.Rank) | _ -> None
    static let (|LetterSym|_|) (e:Expr) = match e with :? LetterSym as ls -> Some(ls.Letter, ls.Subscript) | _ -> None
    static let (|GroupedLetterSym|_|) (e:Expr) = match e with :? GroupedLetterSym as gls -> Some(gls.Letters) | _ -> None
    static let (|WordSym|_|) (e:Expr) = match e with :? WordSym as ws -> Some(ws.Word, ws.Subscript) | _ -> None
    static let (|WellKnownSym|_|) (e:Expr) = match e with :? WellKnownSym as wks -> Some(wks.ID) | _ -> None
    /// Convert to a simple notation likely to be read back properly
    static member Convert(e:Expr) =
        let Convert(e:Expr) = Text.Convert e
        let subtail sub = match sub with | NullExpr -> "" | _ -> "," + Convert(sub)
        match e with
            | ErrorMsgExpr(msg) -> "<<Error msg " + msg + ">>"
            | ErrorExpr -> match e:>obj with | :? TextAwareExpr as tae -> tae.Convert(new Func<Expr,string>(Text.Convert))
                                             | _ -> "<<Error " + e.ToString() + ">>"
            | NullExpr -> ""
            | CompositeExpr(head, args) -> Convert(head) + "[" + (args |> Seq.map (fun a -> Convert(a)) |> String.concat ",") + "]"
            | DoubleNumber(num) -> if Double.IsNaN(num) then "NaN"
                                   elif Double.IsNegativeInfinity(num) then "-infinity"
                                   elif Double.IsPositiveInfinity(num) then "infinity"
                                   else num.ToString("R")
            | IntegerNumber(num) -> num.ToString();
            | ComplexNumber(re, im) -> "<" + Convert(re) + "," + Convert(im) + ">"
            | :? ArrayExpr as ae -> if ae.Elts.Rank = 2 then
                                        let h = ae.Elts.GetLength(0)
                                        let w = ae.Elts.GetLength(1)
                                        "{" + String.concat " " (*"\n"*) [ for i in 0 .. h-1 ->
                                                                               "{" + String.concat ", " [ for j in 0 .. w-1 -> Convert(ae.[[|i;j|]]) ]
                                                                                   + "}" ] + "}"
                                    elif ae.Elts.Rank = 1 then
                                        "{" + String.concat ", " [ for i in 0 .. ae.Elts.GetLength(0)-1 -> Convert(ae.[[|i|]]) ] + "}"
                                    else failwithf "Array transformations to text not supported for rank %d > 2" ae.Elts.Rank
            | LetterSym(ltr, sub) -> "LetterSym[\"" + ltr.ToString() + "\"" + subtail sub + "]" // TODO: need to handle accent, format
            | GroupedLetterSym(ltrs) -> "GroupedLetterSym[" + (ltrs |> Seq.map Convert |> String.concat ",") + "]" // TODO: need to handle accent
            | WordSym(word, sub) -> "WordSym[\"" + word + "\"" + subtail sub + "]" // TODO: need to handle accent, format
            | WellKnownSym(id) -> id.ToString()
            | _ -> failwithf "attempting to convert %s to text" (e.GetType().Name)
    //--------------------------------------
    /// Convert to a representation that a human might enter&mdash;infix notation, etc. May not be parsed, though.
    static member InputConvert(e:Expr) =
        let InputConvert e = Text.InputConvert e
        let rec Input_binop args op =
            Trace.Assert(Array.length args = 2)
            InputConvert(args.[0]) + " " + op.ToString() + " " + InputConvert(args.[1])
        and Input_multibin args op =
            Trace.Assert(Array.length args >= 2)
            args |> Seq.map InputConvert |> String.concat (" " + op.ToString() + " ")
        and Input_arglist args =
            args |> Seq.map InputConvert |> String.concat ","
        and Input_maybe_paren (e:Expr) lowerprec =
            match e with
                | CompositeExpr(WellKnownSym(id), _) when List.exists (fun x -> x = id) lowerprec -> "(" + InputConvert(e) + ")"
                | _ -> InputConvert(e)
        let subtail sub = match sub with | NullExpr -> "" | _ -> "," + InputConvert(sub)
        /// these cannot appear in bare letter or word syms
        let specialchars = "[]{}()<>,+-*/^\" \n0123456789"
        match e with
        | ErrorMsgExpr(msg) -> "<<Error msg " + msg + ">>"
        | ErrorExpr -> match e:>obj with | :? TextAwareExpr as tae -> tae.Convert(new Func<Expr,string>(Text.InputConvert))
                                         | _ -> "<<Error " + e.ToString() + ">>"
        | NullExpr -> ""
        | CompositeExpr(WellKnownSym(WKSID.assignment), args)      -> Input_binop args Unicode.L.LEFTWARDS_ARROW
        | CompositeExpr(WellKnownSym(WKSID.cross), args)              -> Input_binop args Unicode.M.MULTIPLICATION_SIGN
        | CompositeExpr(WellKnownSym(WKSID.dot), args)                 -> Input_binop args Unicode.D.DOT_OPERATOR
        | CompositeExpr(WellKnownSym(WKSID.equals), args)            -> Input_binop args Unicode.E.EQUALS_SIGN
        | CompositeExpr(WellKnownSym(WKSID.greaterequals), args) -> Input_binop args Unicode.G.GREATER_THAN_OR_EQUAL_TO
        | CompositeExpr(WellKnownSym(WKSID.greaterthan), args)    -> Input_binop args Unicode.G.GREATER_THAN_SIGN
        | CompositeExpr(WellKnownSym(WKSID.im), args)                  -> Unicode.B.BLACK_LETTER_CAPITAL_I.ToString() + "[" + Input_arglist(args) + "]"
        | CompositeExpr(WellKnownSym(WKSID.lessequals), args)      -> Input_binop args Unicode.L.LESS_THAN_OR_EQUAL_TO
        | CompositeExpr(WellKnownSym(WKSID.lessthan), args)         -> Input_binop args Unicode.L.LESS_THAN_SIGN
        | CompositeExpr(WellKnownSym(WKSID.logand), args)           -> Input_multibin args Unicode.L.LOGICAL_AND
        | CompositeExpr(WellKnownSym(WKSID.lognot), [|arg|])        -> Unicode.N.NOT_SIGN.ToString() + InputConvert(arg)
        | CompositeExpr(WellKnownSym(WKSID.logor), args)             -> Input_multibin args Unicode.L.LOGICAL_OR
        | CompositeExpr(WellKnownSym(WKSID.magnitude), [|arg|])  -> "|" + InputConvert(arg) + "|"
        | CompositeExpr(WellKnownSym(WKSID.minus), [|arg|])         -> Unicode.M.MINUS_SIGN.ToString() + Input_maybe_paren arg [WKSID.plus; WKSID.times; WKSID.power]
        | CompositeExpr(WellKnownSym(WKSID.plus), [||])                -> failwithf "plus applied to no arguments? in Text.InputConvert"
        | CompositeExpr(WellKnownSym(WKSID.plus), [|arg|])           -> "(" + InputConvert arg + ")"
        | CompositeExpr(WellKnownSym(WKSID.plus), args) ->
            let xf (a:Expr) =
                match a with
                | IntegerNumber(num) when num < BigInt.Zero -> InputConvert(a)
                | DoubleNumber(num) when num < 0. -> InputConvert(a)
                | CompositeExpr(WellKnownSym(WKSID.times), args) when args.Length > 1 &&
                                                                        match args.[0] with 
                                                                        | IntegerNumber(num) when num < BigInt.Zero -> true
                                                                        | DoubleNumber(num) when num < 0. -> true
                                                                        | _ -> false
                                                                    -> InputConvert(a)
                | CompositeExpr(WellKnownSym(WKSID.minus), [|arg|]) -> Unicode.M.MINUS_SIGN.ToString() + " " + InputConvert(arg)
                | _ -> "+ " + InputConvert(a)
            InputConvert(args.[0]) + (args.[1..] |> Seq.map xf |> String.concat "")
        | CompositeExpr(WellKnownSym(WKSID.power), [|bas; exp|]) -> Input_maybe_paren bas [WKSID.plus; WKSID.times] + "^"+ Input_maybe_paren exp [WKSID.plus; WKSID.times]
        | CompositeExpr(WellKnownSym(WKSID.re), args)                  -> Unicode.B.BLACK_LETTER_CAPITAL_R.ToString() + "[" + Input_arglist args + "]"
        | CompositeExpr(WellKnownSym(WKSID.divide), [|arg|])        -> "1/" + Input_maybe_paren arg []
        | CompositeExpr(WellKnownSym(WKSID.times), [||])               -> failwithf "times applied to no arguments? in Text.InputConvert"
        | CompositeExpr(WellKnownSym(WKSID.times), [|arg|])          -> "(" + InputConvert arg + ")"
        | CompositeExpr(WellKnownSym(WKSID.times), args) ->
            let xf (a:Expr) =
                match a with
                | CompositeExpr(WellKnownSym(WKSID.divide), [|arg|]) -> "/" + Input_maybe_paren arg [WKSID.plus; WKSID.minus] // WKSID.minus is here only because will look a-b rather than a(-b)
                | _ -> " " + Input_maybe_paren a [WKSID.plus; WKSID.minus] // Or use "*" or THIN_SPACE instead of " "?
            Input_maybe_paren args.[0] [WKSID.plus; WKSID.minus] + (args.[1..] |> Seq.map xf |> String.concat "")
        | CompositeExpr(head, args) -> Input_maybe_paren head [WKSID.plus; WKSID.times; WKSID.power; WKSID.minus] + "[" + Input_arglist args + "]"
        | DoubleNumber(num) -> if Double.IsNaN(num) then "NaN"
                               elif Double.IsNegativeInfinity(num) then Unicode.M.MINUS_SIGN.ToString() + Unicode.I.INFINITY.ToString()
                               elif Double.IsPositiveInfinity(num) then Unicode.I.INFINITY.ToString()
                               else num.ToString("R")
        | IntegerNumber(num) -> num.ToString()
        | RationalNumber(num) -> "(" + num.ToString() + ")"
        | ComplexNumber(re, im) -> let ims = InputConvert(im)
                                   "(" + InputConvert(re) + (if ims.[0] = '-' then ims else "+" + ims) + "i)"
        | :? ArrayExpr as ae -> if ae.Elts.Rank = 2 then
                                    let h = ae.Elts.GetLength(0)
                                    let w = ae.Elts.GetLength(1)
                                    "{" + String.concat " " (*"\n"*) [ for i in 0 .. h-1 ->
                                                                           "{" + String.concat ", " [ for j in 0 .. w-1 -> InputConvert(ae.[[|i;j|]]) ]
                                                                               + "}" ] + "}"
                                elif ae.Elts.Rank = 1 then
                                    "{" + String.concat ", " [ for i in 0 .. ae.Elts.GetLength(0)-1 -> InputConvert(ae.[[|i|]]) ] + "}"
                                else raise(new NotImplementedException("converting 0 or > 2 dimensional arrays to text not supported"))
        | LetterSym(ltr,sub) -> // TODO: need to handle accent, format
            let str = ltr.ToString()
            match sub with
                | NullExpr when not(Array.exists (fun x -> x = str) (Enum.GetNames(typeof<WKSID>))) &&
                                    not(String.exists (fun c -> c = ltr) specialchars) -> str
                | _ -> "LetterSym[\"" + str + "\"" + subtail sub + "]"
        | GroupedLetterSym(lets) -> "GroupedLetterSym[" + (lets |> Seq.map InputConvert |> String.concat ",") + "]" // TODO: need to handle accent
        | WordSym(word,sub) -> // TODO: need to handle accent, format
            match sub with
                | NullExpr when word.Length > 1 && not(Array.exists (fun x -> x = word) (Enum.GetNames(typeof<WKSID>))) &&
                    not(List.exists (fun x -> x = word) ["LetterSym"; "GroupedLetterSym"; "WordSym"; "infinity"; "NaN"]) &&
                    not(String.exists (fun c -> String.exists (fun d -> d = c) specialchars) word) -> word
                | _ -> "WordSym[\"" + word + "\"" + subtail sub + "]"
        | WellKnownSym(WKSID.del) -> Unicode.N.NABLA.ToString()
        | WellKnownSym(WKSID.differentiald) -> "d"
        | WellKnownSym(WKSID.partiald) -> Unicode.P.PARTIAL_DIFFERENTIAL.ToString()
        | WellKnownSym(WKSID.pi) -> Unicode.G.GREEK_SMALL_LETTER_PI.ToString()
        | WellKnownSym(WKSID.infinity) -> Unicode.I.INFINITY.ToString()
        | WellKnownSym(WKSID.integral) -> Unicode.I.INTEGRAL.ToString()
        | WellKnownSym(WKSID.summation) -> Unicode.N.N_ARY_SUMMATION.ToString()
        | WellKnownSym(id) -> id.ToString()
        | _ -> failwithf "attempting to convert %s to text" (e.GetType().Name)
    //------------------------
    static member Convert(s:Byte[]) =
        let lexbuf = Lexing.LexBuffer<byte>.FromBytes s
        try
            Parser.expr Lexer.token lexbuf
        with e ->
            let pos = lexbuf.EndPos
            raise(new TextParseException(pos.Line, pos.AbsoluteOffset, pos.Column, lexbuf.Lexeme.ToString()(*_utf8*), e))
