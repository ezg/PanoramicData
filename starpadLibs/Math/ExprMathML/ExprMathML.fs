#light

namespace starPadSDK.MathExpr

open starPadSDK.UnicodeNs
open System
open System.Xml
open System.IO
open System.Diagnostics
open System.Collections.Generic
open starPadSDK.MathExpr

(* Transformation of Exprs to and from MathML. We only handle a subset of MathML for both input and output, though more would be possible even without
   extending Expr. 
   In Mathematica, use "someexpr // MathMLForm" to get mathml and "ToExpression["mathmlstring", MathMLForm]" to convert from it.
   In Maple, use "MathML[ExportPresentation](someexpr)" to get mathml and "MathML[Import]("mathmlstring")" to convert from it.
   In MS Math, right click on the displayed form in the log, go to Copy Special, and select MathML. The first attempt to copy and paste from MathML to Notepad may not work.
   *)

(********First is the code for conversion from MathML to Expr********)

/// This is the abstract syntax tree format we transform the input mathml into for easier matching (just compares string values once, in ToAST())
type internal MML = 
    | Math of MML list
    | Mi of string | Mo of string | Mn of string
    | Mrow of MML list
    | Mfenced of string * string * MML list
    | Mfrac of MML list
    | Msqrt of MML list | Mroot of MML list
    | Msub of MML list | Msup of MML list | Msubsup of MML list
    | Munder of MML list | Mover of MML list | Munderover of MML list
    | Mtable of MML list | Mtr of MML list | Mtd of MML list
    | Merror of string

/// We maintain this entire duplicate (virtually) of the Expr hierarchy because of F#'s annoying treatment of
/// return values (requiring them to be explicitly cast to the same type even if they're all subtypes of one type). FS stands for F#
type ExprFS =
    | CompositeExprFS of ExprFS * ExprFS list
    | DoubleNumberFS of float
    | IntegerNumberFS of BigInt
    | RationalNumberFS of BigRat
    | ArrayExprFS of ExprFS array array // this is wrong for expr, but matches what MML can say
    | LetterSymFS of char
    | WordSymFS of string
    | WellKnownSymFS of WKSID
    | ForceParensFS of ExprFS // start of virtual exprs
    | SubscriptFS of ExprFS * ExprFS
    | ErrorMsgExprFS of string

// Sadly, it appears we have to put this in its own type to be able to keep its related definitions together in F#.
type private MathMLInput() =
    static let squashspace str =
        let sqs = System.Text.RegularExpressions.Regex.Replace(str, "[ \t\n\r]+", " ")
        if sqs = " " then " " else sqs.Trim()
    /// converts an XmlElement (tree) to our Abstract Syntax Tree representation (type MML above) for faster parsing/matching
    static let rec ToAST (elt : XmlElement) =
        let cnlist () = [ for e in elt.ChildNodes -> ToAST (e :?> XmlElement) ]
        match elt.Name with
            | "math" -> Math(cnlist())
            | "mi" -> let it = squashspace elt.InnerText in if it = "" then Mo(Unicode.I.INVISIBLE_TIMES.ToString()) else Mi(it)
            | "mo" -> Mo(squashspace elt.InnerText) | "mn" -> Mn(squashspace elt.InnerText) //ms and mtext get squashspace too
            | "mrow" -> Mrow(cnlist())
            | "mfenced" -> Mfenced(elt.GetAttribute("open"), elt.GetAttribute("close"), cnlist())
            | "mfrac" -> Mfrac(cnlist())
            | "msqrt" -> Msqrt(cnlist()) | "mroot" -> Mroot(cnlist())
            | "msub" -> Msub(cnlist()) | "msup" -> Msup(cnlist()) | "msubsup" -> Msubsup(cnlist())
            | "munder" -> Munder(cnlist()) | "mover" -> Mover(cnlist()) | "munderover" -> Munderover(cnlist())
            | "mtable" -> Mtable(cnlist()) | "mtr" -> Mtr(cnlist()) | "mtd" -> Mtd(cnlist())
            | "merror" -> Merror(squashspace elt.InnerText)
            | _ -> failwithf "Unable to parse MathML: unknown element name \"%s\"." elt.Name
    /// Is the string just a single character?
    static let (|Char|_|) (s : string) = if s.Length = 1 then Some(s.[0]) else None
    /// given a mathml identifier, convert it to our corresponding symbol
    static let mitosym (id : string) =
        // lettersym, wordsym, Ord char op (?--see fixme), log/sin/cos/etc, infinity
        if id.Length > 1 then
            try // FIXME not quite right--should only accept those WKS that are written that way!
                WellKnownSymFS(System.Enum.Parse(typeof<WKSID>, id) :?> WKSID)
            with | :? ArgumentException ->
                WordSymFS(if id.[0] = '"' && id.[id.Length-1] = '"' then id.Substring(1, id.Length-2) else id) // is this "" stuff ok with mathml?
        elif id.Length = 0 then failwith "Unable to parse MathML blank <mi/>."
        else
            match id.[0] with
                | Unicode.B.BLACK_LETTER_CAPITAL_R -> WellKnownSymFS(WKSID.re)
                | Unicode.B.BLACK_LETTER_CAPITAL_I -> WellKnownSymFS(WKSID.im)
                | Unicode.N.NABLA -> WellKnownSymFS(WKSID.del)
                | Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_D -> WellKnownSymFS(WKSID.differentiald)
                | Unicode.P.PARTIAL_DIFFERENTIAL -> WellKnownSymFS(WKSID.partiald)
                | Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_I -> WellKnownSymFS(WKSID.i)
                | Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_E -> WellKnownSymFS(WKSID.e)
                | _ -> let wks = Syntax.CharWKSMap.[new Syntax.WOrC(id.[0])] in
                        if wks = null || WellKnownSym.integral = wks then LetterSymFS(id.[0]) else WellKnownSymFS(wks.ID)
    /// can this string be parsed as an integer?
    static let _integerRE = new System.Text.RegularExpressions.Regex("^[+---]?[0-9]+$")
    /// given a mathml representation of a number, convert it to a corresponding ExprFS
    static let mntonum (num : string) = // double (including NaN), integer
        match num with
            | Char(Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_I) -> WellKnownSymFS(WKSID.i)
            | Char(Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_E) -> WellKnownSymFS(WKSID.e)
            | Char('π') -> WellKnownSymFS(WKSID.pi)
            | Char('∞') -> WellKnownSymFS(WKSID.infinity)
            | "NaN" -> DoubleNumberFS(Double.NaN)
            | _ -> if _integerRE.IsMatch(num) then IntegerNumberFS(new BigInt(num)) else DoubleNumberFS(Double.Parse(num))
    /// From Expr to ExprFS, a retrograde converter to deal with the output from various Syntax routines that return Exprs
    static let rec fromexpr (e : Expr) = // NB: Force Parentheses annotation not handled yet--but this function is so far only used to back-convert operators
        match e with
            | :? WellKnownSym as wks -> WellKnownSymFS(wks.ID)
            | :? WordSym as ws -> let wsfs = WordSymFS(ws.Word) in match ws.Subscript with | :? NullExpr -> wsfs | _ -> SubscriptFS(wsfs, fromexpr ws.Subscript)
            | :? CompositeExpr as ce -> CompositeExprFS(fromexpr ce.Head, ce.Args |> Array.map fromexpr |> Array.toList)
            | :? DoubleNumber as dn -> DoubleNumberFS(dn.Num)
            | :? IntegerNumber as ine -> IntegerNumberFS(ine.Num)
            | :? RationalNumber as rn -> RationalNumberFS(rn.Num)
            | :? ArrayExpr as ae -> failwith "Arrays not implemented yet in fromexpr."
            | :? LetterSym as ls -> let lsfs = LetterSymFS(ls.Letter) in match ls.Subscript with | :? NullExpr -> lsfs | _ -> SubscriptFS(lsfs, fromexpr ls.Subscript)
            | :? ErrorMsgExpr as eme -> ErrorMsgExprFS(eme.Msg)
            | _ -> failwith "???"
    /// The main routine to convert MathML (AST) to ExprFS. This along with the functions it calls make up the workhorse of the translator/parser.
    static let rec toexprfs (ast : MML) =
        match ast with
            | Math(_) -> failwith "Unable to parse MathML: <math> element occurs other than at top level."
            | Mi(name) -> mitosym name
            | Mo(op) -> failwith "<mo> not handled on its own"
            | Mn(num) -> mntonum num
            | Mrow(elts) -> convertmrow elts
            | Mfenced(opendel, closedel, contents) -> convertmfenced opendel closedel contents
            | Mfrac([num; denom]) -> CompositeExprFS(WellKnownSymFS(WKSID.times), [toexprfs num;
                                                                                    CompositeExprFS(WellKnownSymFS(WKSID.divide), [toexprfs denom])])
            | Msqrt(children) -> CompositeExprFS(WellKnownSymFS(WKSID.root), [IntegerNumberFS(new BigInt("2")); convertmrow children])
            | Mroot([baseelt; ixelt]) -> CompositeExprFS(WellKnownSymFS(WKSID.root), [toexprfs ixelt; toexprfs baseelt])
            | Msub([elt; sub]) -> SubscriptFS(toexprfs elt, toexprfs sub)
            | Msup([elt; sup]) -> CompositeExprFS(WellKnownSymFS(WKSID.power), [toexprfs elt; toexprfs sup])
            | Msubsup([elt; sub; sup]) -> CompositeExprFS(WellKnownSymFS(WKSID.power), [SubscriptFS(toexprfs elt, toexprfs sub); toexprfs sup])
            | Munder([elt; sub]) -> failwith "<munder> not handled on its own"
            | Mover([elt; sup]) -> failwith "<mover> not handled on its own"
            | Munderover([elt; sub; sup]) -> failwith "<munderover> not handled on its own"
            | Mtable(elts) -> converttable elts
            | Mtr(_) -> failwith "<mtr> not handled on its own"
            | Mtd(_) -> failwith "<mtd> not handled on its own"
            | Merror(msg) -> ErrorMsgExprFS(msg)
            | _ -> failwithf "MathML not understood: %A" ast
    and OperFor op = match Syntax.Fixes.Translate(match op with | Char(c) -> new Syntax.WOrC(c) | _ -> new Syntax.WOrC(op)) with
                        | null -> failwithf "Unknown operator \"%s\"%s" op (if op.Length = 1 then sprintf " (0x%X)" (int op.[0]) else "")
                        | _ as oper -> oper
    and PrecOf op = (Syntax.Fixes.Find(OperFor op)).Precedence
    and MainOpOf2 opix = Syntax.Fixes.Table.[opix].Heads.[0]
    and MainOpOf op = MainOpOf2(PrecOf op)
    and SyntaxKindOf prec = Syntax.Fixes.Table.[prec].Kind
    and OpSyntaxIs synlist prec = List.contains (SyntaxKindOf prec) synlist
    and convertop op rightfix =
        let prec = PrecOf op
        if prec = -1 || not (rightfix prec) then failwithf "Operator \"%s\" is not correct fixedness" op
        else fromexpr(OperFor op)
    and (|MultiBinTail|_|) elts =
        match elts with
            | [Mo(op); arg] -> Some(PrecOf op, [OperFor op, toexprfs arg])
            | Mo(op) :: arg :: MultiBinTail(prec, tailfs) when prec = PrecOf op -> Some(prec, (OperFor op, toexprfs arg) :: tailfs)
            | _ -> None
    and (|MRowWithDifferential|_|) elts =
        match elts with
            | [Mo(Char(Unicode.I.INVISIBLE_TIMES)); Mrow([Mo(Char(Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_D)); diff])] -> Some([], diff)
            | [Mrow([Mo("d"); diff])] -> Some([], diff)   // for MS Math
            | elt :: MRowWithDifferential(tail, diff) -> Some(elt :: tail, diff)
            | _ -> None
    and convertmrow elts = // the pattern matching necessary in this specific function is the primary reason I'm doing this in F# rather than C#
        match elts with
            | [elt] -> toexprfs elt
            | [arg; Mo(op)] -> CompositeExprFS(convertop op (OpSyntaxIs [Syntax.K.Postfix]), [toexprfs arg])
            | [Mo(op); arg] -> match op with
                                | "-" | Char(Unicode.M.MINUS_SIGN) -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [toexprfs arg])
                                | _ -> CompositeExprFS(convertop op (OpSyntaxIs [Syntax.K.Prefix]), [toexprfs arg])
            | [Mi(fn); Mfenced("", "", args)] -> CompositeExprFS(mitosym fn, List.map toexprfs args)
            | [fn; Mo(Char(Unicode.F.FUNCTION_APPLICATION)); args] ->
                CompositeExprFS(toexprfs fn, match toexprfs args with
                                                | CompositeExprFS(WordSymFS("comma"), arglist) -> arglist
                                                | ForceParensFS(CompositeExprFS(WordSymFS("comma"), arglist)) -> arglist
                                                | elt -> [elt]
                                                )
            | Msub([Mo(Char(Unicode.I.INTEGRAL)); lower]) :: MRowWithDifferential(mrowcont, diff)
            | [Msub([Mo(Char(Unicode.I.INTEGRAL)); lower]); Mrow(MRowWithDifferential(mrowcont, diff))]
            | Munder([Mo(Char(Unicode.I.INTEGRAL)); lower]) :: MRowWithDifferential(mrowcont, diff)
            | [Munder([Mo(Char(Unicode.I.INTEGRAL)); lower]); Mrow(MRowWithDifferential(mrowcont, diff))] ->
                CompositeExprFS(WellKnownSymFS(WKSID.integral), [convertmrow mrowcont; CompositeExprFS(WellKnownSymFS(WKSID.differentiald), [toexprfs diff]);
                                                                    toexprfs lower])
            | Msubsup([Mo(Char(Unicode.I.INTEGRAL)); lower; upper]) :: MRowWithDifferential(mrowcont, diff)
            | [Msubsup([Mo(Char(Unicode.I.INTEGRAL)); lower; upper]); Mrow(MRowWithDifferential(mrowcont, diff))]
            | Munderover([Mo(Char(Unicode.I.INTEGRAL)); lower; upper]) :: MRowWithDifferential(mrowcont, diff)
            | [Munderover([Mo(Char(Unicode.I.INTEGRAL)); lower; upper]); Mrow(MRowWithDifferential(mrowcont, diff))] ->
                CompositeExprFS(WellKnownSymFS(WKSID.integral), [convertmrow mrowcont; CompositeExprFS(WellKnownSymFS(WKSID.differentiald), [toexprfs diff]);
                                                                    toexprfs lower; toexprfs upper])
            | Mo(Char(Unicode.I.INTEGRAL)) :: MRowWithDifferential(mrowcont, diff)
            | [Mo(Char(Unicode.I.INTEGRAL)); Mrow(MRowWithDifferential(mrowcont, diff))] ->
                CompositeExprFS(WellKnownSymFS(WKSID.integral), [convertmrow mrowcont; CompositeExprFS(WellKnownSymFS(WKSID.differentiald), [toexprfs diff])])
            | MultiBinTail(prec, tail) when OpSyntaxIs [Syntax.K.BinPrimaryAndSecondary2] prec ->
                CompositeExprFS(MainOpOf2 prec |> fromexpr, List.map (fun (oper, arg) -> if oper = MainOpOf2 prec then arg else
                                                                                            CompositeExprFS(fromexpr oper, [arg])) tail)
            | arg1 :: MultiBinTail(prec, tail) when OpSyntaxIs [Syntax.K.BinLeft; Syntax.K.BinRight; Syntax.K.BinAllOfLike; Syntax.K.BinPrimaryAndSecondary;
                                                                    Syntax.K.BinPrimaryAndSecondary2] prec -> convertbin (toexprfs arg1) prec tail
            | [arg1; Mo(op); arg2] -> CompositeExprFS(convertop op (OpSyntaxIs [Syntax.K.BinAlone]), [toexprfs arg1; toexprfs arg2])
            | [Mo(o1); elt; Mo(o2)] -> convertmfenced o1 o2 [elt]
            | _ -> failwithf "unhandled <mrow> %A" elts
    and converttable elts =
        match elts with
            | MTrows(rows) -> ArrayExprFS(List.toArray rows)
            | _ -> failwithf "unhandled <mtable> %A" elts
    and (|MTrows|_|) elts =
        match elts with
            | Mtr(MTcells(cells)) :: MTrows(rows) -> Some(List.toArray cells :: rows)
            | [Mtr(MTcells(cells))] -> Some([List.toArray cells])
            | _ -> None
    and (|MTcells|_|) elts =
        match elts with
            | Mtd(cell) :: MTcells(cells) -> Some(convertmrow cell :: cells)
            | [Mtd(cell)] -> Some([convertmrow cell])
            | _ -> None
    and convertbin arg1 prec tail =
        match SyntaxKindOf prec with
            | Syntax.K.BinLeft -> List.fold (fun accum (oper, efs) -> CompositeExprFS(fromexpr oper, [accum; efs])) arg1 tail
            | Syntax.K.BinRight ->
                let (opers, efss) = List.unzip tail
                let efss = Array.ofList (arg1 :: efss)
                let (efss, argn) = efss.[0 .. (efss.Length-2)], efss.[efss.Length-1]
                List.foldBack (fun (oper, efs) accum -> CompositeExprFS(fromexpr oper, [efs; accum])) (List.zip opers (List.ofArray efss)) argn
            | Syntax.K.BinAllOfLike ->
                let accumlastargs lastoper curargs relns = CompositeExprFS(fromexpr lastoper, List.rev curargs)::relns
                let (relns, curargs, lastoper, _) =
                    List.fold (fun (relns, curargs, lastoper, lastefs) (oper, efs) ->
                                        if oper = lastoper then (relns, efs::curargs, lastoper, efs)
                                            else (accumlastargs lastoper curargs relns, [efs; lastefs], oper, efs))
                                    ([], [arg1], fst tail.Head, arg1) tail
                let relns = accumlastargs lastoper curargs relns
                if relns.Length > 1 then CompositeExprFS(WellKnownSymFS(WKSID.logand), List.rev relns) else relns.[0]
            | Syntax.K.BinPrimaryAndSecondary | Syntax.K.BinPrimaryAndSecondary2 ->
                let mainoper = MainOpOf2 prec
                CompositeExprFS(fromexpr mainoper,
                    arg1 :: (List.map (fun (oper, efs) -> if mainoper = oper then efs else CompositeExprFS(fromexpr oper, [efs])) tail))
    and convertmfenced opendel closedel contents =
        let children = List.map toexprfs contents
        let child() = match children with | [efs] -> efs | args -> CompositeExprFS(WordSymFS("comma"), args)
        match opendel, closedel with
            | "(", ")" | "", "" -> ForceParensFS(child())
            | "[", "]" -> CompositeExprFS(WordSymFS("bracket"), children)
            | "{", "}" -> CompositeExprFS(WordSymFS("brace"), children)
            | Char(Unicode.L.LEFT_CEILING), Char(Unicode.R.RIGHT_CEILING) -> CompositeExprFS(WellKnownSymFS(WKSID.ceiling), [child()])
            | Char(Unicode.L.LEFT_FLOOR), Char(Unicode.R.RIGHT_FLOOR) -> CompositeExprFS(WellKnownSymFS(WKSID.floor), [child()])
            | "|", "|" -> CompositeExprFS(WellKnownSymFS(WKSID.magnitude), [child()])
            | _ -> failwithf "Unknown delimeters (\"fences\") '%s', '%s'" opendel closedel
    /// From ExprFS to real Expr
    static let rec toexpr efs =
        match efs with
            | CompositeExprFS(head, args) -> new CompositeExpr(toexpr head, args |> List.map toexpr |> List.toArray) :> Expr
            | DoubleNumberFS(num) -> new DoubleNumber(num) :> Expr
            | IntegerNumberFS(num) -> new IntegerNumber(num) :> Expr
            | RationalNumberFS(num) -> new RationalNumber(num) :> Expr
            | ArrayExprFS(arr) -> new ArrayExpr(Array2D.init arr.Length (Array.map Array.length arr |> Array.max) (fun i j -> toexpr arr.[i].[j])) :> Expr
            | LetterSymFS(c) -> new LetterSym(c) :> Expr
            | WordSymFS(w) -> new WordSym(w) :> Expr
            | WellKnownSymFS(wks) -> new WellKnownSym(wks) :> Expr
            | ForceParensFS(e) -> let expr = toexpr e
                                  expr.Annotations.["Force Parentheses"] <- match expr.Annotations.["Force Parentheses"] with :? int as num -> num+1 | _ -> 1
                                  expr
            | SubscriptFS(e, sub) -> match e with
                                        | LetterSymFS(c) -> new LetterSym(c, toexpr sub) :> Expr
                                        | WordSymFS(w) -> let result = new WordSym(w) in result.Subscript <- toexpr sub; result :> Expr
                                        | _ -> new CompositeExpr(WellKnownSym.subscript, [|toexpr e; toexpr sub|]) :> Expr
            | ErrorMsgExprFS(msg) -> new ErrorMsgExpr(msg) :> Expr
    /// Convert MathML to Expr
    static member public Convert(s) =
        let doc = new XmlDocument()
        let dtdloc =
            if File.Exists("MathML dtd\\mathml2.dtd") then "MathML dtd\\mathml2.dtd" else "http://www.w3.org/TR/MathML2/dtd/mathml2.dtd"
        doc.LoadXml("<?xml version=\"1.0\" encoding=\"iso8859-1\"?>"
            + "<!DOCTYPE math PUBLIC \"-//W3C//DTD MathML 2.0//EN\" \"" + dtdloc + "\">"
            + s);
        let mmlast = ToAST(doc.DocumentElement)
        match mmlast with
            | Math(inner) -> inner |> convertmrow |> toexpr
            | _ -> failwith "Unable to parse MathML: top-level element is not <math>."


(********Second is the code for converting from Expr to MathML, which defines the type outside users will call and references the conversion from MathML*******)

/// useUnderOver: use munderover rather than msubsup on operators. Maple 10 doesn't interpret msubsup on integrals properly (it fails on an example from the spec).
/// useMapleFunnyIntegral: Maple 10 wants integrals etc as <int><args><invistimes><mrow <diff d><var>> rather than the example from the spec, <int><mrow <args><invistimes><mrow <diff d><var>>>
/// useNamespace: some programs want no namespace (though I forget which); some like Word 2007 want the namespace. It's normally http://www.w3.org/1998/Math/MathML
type private MathMLOutput(_useUnderOver:bool, _useMapleFunnyIntegral:bool, _useNamespace:string, _forMSMath:bool) =
    // This was converted pretty directly from existing C# code, so does not necessarily show proper design etc for F#
    inherit GenericOutput<XmlDocumentFragment>(false)
    let mutable _doc = (null :> XmlDocument)
    let MaybeMrow(df:XmlDocumentFragment) =
        if df.ChildNodes.Count = 1 then df
        else
            let df2 = _doc.CreateDocumentFragment()
            let mrow = _doc.CreateElement("mrow", _useNamespace)
            df2.AppendChild(mrow) |> ignore
            mrow.AppendChild(df) |> ignore
            df2
    let SimpleElement(kind:string, contents:string) =
        let el = _doc.CreateElement(kind, _useNamespace)
        el.AppendChild(_doc.CreateTextNode(contents)) |> ignore
        el
    member public this.Convert(e) =
        Trace.Assert((_doc = null))
        _doc <- new XmlDocument();
        _doc.LoadXml(if _useNamespace = "" then "<math/>" else "<math xmlns='" + _useNamespace + "'/>")
        let result = this.Translate(e)
        let doc = _doc
        doc.DocumentElement.AppendChild(result) |> ignore
        _doc <- null;
        doc
    override this.__Translate(e:NullExpr) =
        //raise(new NotSupportedException("NullExprs should not exist outside of subscripts and such")) : XmlDocumentFragment
        let df = _doc.CreateDocumentFragment()
        df.AppendChild(SimpleElement("merror", "Null expression!")) |> ignore
        df
    override this.__Translate(e:ErrorMsgExpr) =
        let df = _doc.CreateDocumentFragment()
        df.AppendChild(SimpleElement("merror", e.Msg)) |> ignore
        df
    override this.__TakesInvisibleTimes with get() = true
    override this.__TranslateOperator(expr:Expr, exprix:obj, op:Syntax.WOrC, ttype:Syntax.TType) =
        let df = _doc.CreateDocumentFragment()
        let elt = if _forMSMath && op.Character = Unicode.I.INVISIBLE_TIMES then SimpleElement("mi", " ")
                  else SimpleElement((if ttype = Syntax.TType.Ord && Syntax.Fixes.Translate(op) = null && op <> Syntax.WOrC(Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_D) then "mi" else "mo"), op.ToString())
        // FIXME: type is ord for blackletter R, I (for re and im) but also for /...all come through here, need distinguishing
        df.AppendChild(elt) |> ignore
        df
    override this.__TranslateWord(expr:Expr, op:string, ttype:Syntax.TType) =
        let df = _doc.CreateDocumentFragment()
        df.AppendChild(SimpleElement("mi", op)) |> ignore
        df
    override this.__TranslateDelims(e:Expr, emph:bool, lexprix:obj, l:char, t:XmlDocumentFragment, rexprix:obj, r:char) =
        let df = _doc.CreateDocumentFragment()
        let row = _doc.CreateElement("mrow", _useNamespace)
        df.AppendChild(row) |> ignore
        row.AppendChild(SimpleElement("mo", l.ToString())) |> ignore
        row.AppendChild(t) |> ignore
        row.AppendChild(SimpleElement("mo", r.ToString())) |> ignore
        df
    override this.__WrapTranslatedExpr(expr:Expr, lt:List<XmlDocumentFragment>) =
        let df = _doc.CreateDocumentFragment()
        let row = _doc.CreateElement("mrow", _useNamespace)
        df.AppendChild(row) |> ignore
        for c:XmlDocumentFragment in lt do row.AppendChild(c) |> ignore
        df
    override this.__TranslateVerticalFraction(e:Expr, divlineexpr:Expr, num:XmlDocumentFragment, den:XmlDocumentFragment) =
        let df = _doc.CreateDocumentFragment()
        let mfrac = _doc.CreateElement("mfrac", _useNamespace)
        df.AppendChild(mfrac) |> ignore
        mfrac.AppendChild(MaybeMrow(num)) |> ignore
        mfrac.AppendChild(MaybeMrow(den)) |> ignore
        df
    override this.__TranslateBigOp(wholeexpr:Expr, opexpr:Expr, op:char, lowerlimit:XmlDocumentFragment, upperlimit:XmlDocumentFragment, contents:XmlDocumentFragment) =
        let df = _doc.CreateDocumentFragment()
        let mrow = _doc.CreateElement("mrow", _useNamespace)
        df.AppendChild(mrow) |> ignore
        if lowerlimit <> null && upperlimit <> null then
            let mss = _doc.CreateElement((if _useUnderOver then "munderover" else "msubsup"), _useNamespace)
            mrow.AppendChild(mss) |> ignore
            mss.AppendChild(SimpleElement("mo", op.ToString())) |> ignore
            mss.AppendChild(MaybeMrow(lowerlimit)) |> ignore
            mss.AppendChild(MaybeMrow(upperlimit)) |> ignore
        elif lowerlimit <> null then
            let mss = _doc.CreateElement((if _useUnderOver then "munder" else "msub"), _useNamespace)
            mrow.AppendChild(mss) |> ignore
            mss.AppendChild(SimpleElement("mo", op.ToString())) |> ignore
            mss.AppendChild(MaybeMrow(lowerlimit)) |> ignore
        elif upperlimit <> null then
            let mss = _doc.CreateElement((if _useUnderOver then "mover" else "msup"), _useNamespace)
            mrow.AppendChild(mss) |> ignore
            mss.AppendChild(SimpleElement("mo", op.ToString())) |> ignore
            mss.AppendChild(MaybeMrow(upperlimit)) |> ignore
        else
            mrow.AppendChild(SimpleElement("mo", op.ToString())) |> ignore
        mrow.AppendChild(if _useMapleFunnyIntegral then contents else MaybeMrow(contents)) |> ignore
        df
    override this.__TranslateFunctionApplication(e:Expr, fn:XmlDocumentFragment, args:XmlDocumentFragment) =
        Trace.Assert(fn.ChildNodes.Count = 1 && args.ChildNodes.Count = 1)
        let df = _doc.CreateDocumentFragment()
        let mrow = _doc.CreateElement("mrow", _useNamespace)
        df.AppendChild(mrow) |> ignore
        mrow.AppendChild(fn) |> ignore
        if not _forMSMath then mrow.AppendChild(SimpleElement("mo", Unicode.F.FUNCTION_APPLICATION.ToString())) |> ignore
        mrow.AppendChild(args) |> ignore
        df
    override this.__TranslateOperatorApplication(e:Expr, op:XmlDocumentFragment, args:XmlDocumentFragment) =
        Trace.Assert(op.ChildNodes.Count = 1 && args.ChildNodes.Count = 1)
        let df = _doc.CreateDocumentFragment()
        let mrow = _doc.CreateElement("mrow", _useNamespace)
        df.AppendChild(mrow) |> ignore
        mrow.AppendChild(op) |> ignore
        mrow.AppendChild(args) |> ignore
        df
    override this.__AddSuperscript(e:Expr, nuc:XmlDocumentFragment, sup:XmlDocumentFragment) =
        // FIXME check if already has subscript so do msubsup?
        let df = _doc.CreateDocumentFragment()
        let msup = _doc.CreateElement("msup", _useNamespace)
        df.AppendChild(msup) |> ignore
        msup.AppendChild(MaybeMrow(nuc)) |> ignore
        msup.AppendChild(MaybeMrow(sup)) |> ignore
        df
    override this.__AddSubscript(e:Expr, nuc:XmlDocumentFragment, sub:XmlDocumentFragment) =
        let df = _doc.CreateDocumentFragment()
        let msub = _doc.CreateElement("msub", _useNamespace)
        df.AppendChild(msub) |> ignore
        msub.AppendChild(MaybeMrow(nuc)) |> ignore
        msub.AppendChild(MaybeMrow(sub)) |> ignore
        df
    override this.__TranslateRadical(e:Expr, radicand:XmlDocumentFragment, index:XmlDocumentFragment) =
        let df = _doc.CreateDocumentFragment()
        let root = _doc.CreateElement((if index = null then "msqrt" else "mroot"), _useNamespace)
        df.AppendChild(root) |> ignore
        root.AppendChild(radicand) |> ignore
        if index <> null then root.AppendChild(index) |> ignore
        df
    override this.__TranslateIntegralInternals(integrand:XmlDocumentFragment, dxthing:XmlDocumentFragment) =
        let df = _doc.CreateDocumentFragment()
        let parent = if _useMapleFunnyIntegral then df :> XmlNode
                        else let mrow = _doc.CreateElement("mrow", _useNamespace)
                             df.AppendChild(mrow) |> ignore
                             mrow :> XmlNode
        parent.AppendChild(MaybeMrow(integrand)) |> ignore
        if not _forMSMath then parent.AppendChild(SimpleElement("mo", Unicode.I.INVISIBLE_TIMES.ToString())) |> ignore
        parent.AppendChild(MaybeMrow(dxthing)) |> ignore
        df
    override this.__Translate(n:DoubleNumber) =
        let df = _doc.CreateDocumentFragment()
        if Double.IsNaN(n.Num) then
            df.AppendChild(SimpleElement("mn", "NaN")) |> ignore
        elif Double.IsNegativeInfinity(n.Num) then
            let el = _doc.CreateElement("mrow", _useNamespace)
            df.AppendChild(el) |> ignore
            el.AppendChild(SimpleElement("mo", Unicode.M.MINUS_SIGN.ToString())) |> ignore
            el.AppendChild(SimpleElement("mi", Unicode.I.INFINITY.ToString())) |> ignore
        elif Double.IsPositiveInfinity(n.Num) then
            df.AppendChild(SimpleElement("mi", Unicode.I.INFINITY.ToString())) |> ignore
        else
            df.AppendChild(SimpleElement("mn", n.Num.ToString("R"))) |> ignore
        df
    override this.__TranslateNumber(e:Expr, n:string) =
        let df = _doc.CreateDocumentFragment()
        df.AppendChild(SimpleElement("mn", n)) |> ignore
        df
    override this.__Translate(e:ArrayExpr) =
        let df = _doc.CreateDocumentFragment()
        let mtable = _doc.CreateElement("mtable", _useNamespace)
        df.AppendChild(mtable) |> ignore
        if e.Elts.Rank = 2 then
            let h = e.Elts.GetLength(0)
            let w = e.Elts.GetLength(1)
            for i = 0 to h-1 do
                let mtr = _doc.CreateElement("mtr", _useNamespace)
                mtable.AppendChild(mtr) |> ignore
                for j = 0 to w-1 do
                    let mtd = _doc.CreateElement("mtd", _useNamespace)
                    mtr.AppendChild(mtd) |> ignore
                    mtd.AppendChild(MaybeMrow(this.Translate(e.[[|i; j|]]))) |> ignore
        else
            raise (new NotImplementedException())
        df
    override this.__Translate(s:LetterSym) =
        let df = _doc.CreateDocumentFragment()
        let mi = SimpleElement("mi", s.Letter.ToString())
        match s.Subscript with
            | :? NullExpr -> df.AppendChild(mi) |> ignore
            | _ -> let msub = _doc.CreateElement("msub", _useNamespace)
                   msub.AppendChild(mi) |> ignore
                   let subdf = this.Translate(s.Subscript)
                   msub.AppendChild(MaybeMrow(subdf)) |> ignore
                   df.AppendChild(msub) |> ignore
        df
    override this.__Translate(s:WordSym) =
        let df = _doc.CreateDocumentFragment()
        let name = if Array.IndexOf((Enum.GetNames(typeof<WKSID>):>Array), s.Word) = -1 && s.Word.Length > 1 && s.Word <> "NaN" then s.Word
                   else "\"" + s.Word + "\""
        let mi = SimpleElement("mi", name)
        match s.Subscript with
            | :? NullExpr -> df.AppendChild(mi) |> ignore
            | _ -> let msub = _doc.CreateElement("msub", _useNamespace)
                   msub.AppendChild(mi) |> ignore
                   let subdf = this.Translate(s.Subscript)
                   msub.AppendChild(MaybeMrow(subdf)) |> ignore
                   df.AppendChild(msub) |> ignore
        df

type public MathML(_useUnderOver:bool, _useMapleFunnyIntegral:bool, _useNamespace:string, _forMSMath:bool) =
    let mmlout = new MathMLOutput(_useUnderOver, _useMapleFunnyIntegral, _useNamespace, _forMSMath)
    new() = MathML(false, false, "", false)
    new(ns) = MathML(false, false, ns, false)
    new(useunderover, usemaplefunny) = MathML(useunderover, usemaplefunny, "", false)
    member public mml.Convert(e) = mmlout.Convert(e)
    static member public Convert(s) = MathMLInput.Convert(s)
    static member public Namespace = "http://www.w3.org/1998/Math/MathML"
