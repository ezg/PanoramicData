// Learn more about F# at http://fsharp.net

namespace starPadSDK.MathExpr

open starPadSDK.UnicodeNs
open System
open System.IO
open System.Diagnostics
open System.Collections.Generic
open starPadSDK.MathExpr


/// We maintain this entire duplicate (virtually) of the Expr hierarchy because of F#'s annoying treatment of
/// return values (requiring them to be explicitly cast to the same type even if they're all subtypes of one type). FS stands for F#
type ExprFS =
    | CompositeExprFS of ExprFS * ExprFS list * obj
    | DoubleNumberFS of float * obj
    | IntegerNumberFS of BigInt * obj
    | RationalNumberFS of BigRat * obj
    | ArrayExprFS of ExprFS array array // this is wrong for expr, but matches what MML can say
    | LetterSymFS of char * obj
    | WordSymFS of string * obj
    | WellKnownSymFS of WKSID
    | SubscriptFS of ExprFS * ExprFS
    | ErrorMsgExprFS of string
    | MissingValueExprFS of string

type public ExprTransform() =
     /// From Expr to ExprFS, a retrograde converter to deal with the output from various Syntax routines that return Exprs
    static let rec fromexpr (e : Expr) = // NB: Force Parentheses annotation not handled yet--but this function is so far only used to back-convert operators
        match e with
            | :? WellKnownSym as wks -> WellKnownSymFS(wks.ID)
            | :? WordSym as ws -> let wsfs = WordSymFS(ws.Word,ws.Annotations.Item("Factor")) in match ws.Subscript with | :? NullExpr -> wsfs | _ -> SubscriptFS(wsfs, fromexpr ws.Subscript)
            | :? CompositeExpr as ce -> CompositeExprFS(fromexpr ce.Head, ce.Args |> Array.map fromexpr |> Array.toList, ce.Annotations.Item("Factor"))
            | :? DoubleNumber as dn -> DoubleNumberFS(dn.Num,dn.Annotations.Item("Factor"))
            | :? IntegerNumber as ine -> IntegerNumberFS(ine.Num,ine.Annotations.Item("Factor"))
            | :? RationalNumber as rn -> RationalNumberFS(rn.Num,rn.Annotations.Item("Factor"))
            | :? ArrayExpr as ae -> IntegerNumberFS(BigInt("-9999"), ae.Annotations.Item("Factor")) //  failwith "Arrays not implemented yet in fromexpr."
            | :? LetterSym as ls -> let lsfs = LetterSymFS(ls.Letter, ls.Annotations.Item("Factor")) in match ls.Subscript with | :? NullExpr -> lsfs | _ -> SubscriptFS(lsfs, fromexpr ls.Subscript)
            | :? ErrorMsgExpr as eme -> ErrorMsgExprFS(eme.Msg)
            | _ -> ErrorMsgExprFS("")
          //  | _ -> failwith "???"
        
    /// From ExprFS to real Expr
    static let rec toexpr efs =
        let addFactor(expr:Expr,factor) =
            if (factor <> null) then TermColorizer.MarkFactorFS(expr, factor); // expr.Annotations.Add("Factor", factor)
            expr
        match efs with
            | CompositeExprFS(head, args, factor) -> addFactor(new CompositeExpr(toexpr head, args |> List.map toexpr |> List.toArray), factor)
            | DoubleNumberFS(num,factor) -> addFactor(new DoubleNumber(num), factor)
            | IntegerNumberFS(num,factor) -> addFactor(new IntegerNumber(num) , factor)
            | RationalNumberFS(num,factor) -> addFactor(new RationalNumber(num), factor)
            | ArrayExprFS(arr) -> new ArrayExpr(Array2D.init arr.Length (Array.map Array.length arr |> Array.max) (fun i j -> toexpr arr.[i].[j])) :> Expr
            | LetterSymFS(c,factor) ->  addFactor(new LetterSym(c), factor)
            | WordSymFS(w,factor) -> addFactor(new WordSym(w), factor)
            | WellKnownSymFS(wks) -> new WellKnownSym(wks) :> Expr
            | SubscriptFS(e, sub) -> match e with
                                        | LetterSymFS(c,factor) -> 
                                            let ls = new LetterSym(c, toexpr sub) 
                                            if (factor <> null) then ls.Annotations.Add("Factor", factor) 
                                            ls:> Expr
                                        | WordSymFS(w,factor) -> 
                                            let result = new WordSym(w) in
                                                addFactor(result, factor);
                                                result.Subscript <- toexpr sub;
                                                result :> Expr
                                        | _ -> new CompositeExpr(WellKnownSym.subscript, [|toexpr e; toexpr sub|]) :> Expr
            | MissingValueExprFS(msg) -> new ErrorMsgExpr(msg) :> Expr
            | ErrorMsgExprFS(msg) -> new ErrorMsgExpr(msg) :> Expr
            
            
    /// shorthands for matching different types of Exprs
    static let (|SymPlus|_|) (e) = match e with WellKnownSymFS(WKSID.plus) -> Some() | _ -> None
    static let (|SymMinus|_|) (e) = match e with WellKnownSymFS(WKSID.minus) -> Some() | _ -> None
    static let (|SymEq|_|) (e) = match e with WellKnownSymFS(WKSID.equals) -> Some() | _ -> None
    static let (|SymTimes|_|) (e) = match e with WellKnownSymFS(WKSID.times) -> Some() | _ -> None
    static let (|SymDiv|_|) (e) = match e with WellKnownSymFS(WKSID.divide) -> Some() | _ -> None
    static let (|SymPower|_|) (e) = match e with WellKnownSymFS(WKSID.power) -> Some() | _ -> None
    static let (|SymRoot|_|) (e) = match e with WellKnownSymFS(WKSID.root) -> Some() | _ -> None
    static let (|CEXP|_|) (e) = match e with CompositeExprFS(h,a,f) -> Some(h,a,f) | _ -> None
    static let (|BINT|_|) (e) = match e with IntegerNumberFS(d,f) -> Some(d,f) | _ -> None
    static let (|DINT|_|) (e) = match e with DoubleNumberFS(d,f) -> Some(d,f) | _ -> None
    static let (|LSYM|_|)(e) = match e with LetterSymFS(l,f) -> Some(l,f) | _ -> None
    
    static let clearFactor(e) = fromexpr(TermColorizer.ClearFactorMarks(toexpr(e),-1))
    static let rec setFactor(b,fac) = 
        match b with
            | CEXP(op, args, f) -> CompositeExprFS(op, args, fac)
            | IntegerNumberFS(v,f) -> IntegerNumberFS(v, fac)
            | DoubleNumberFS(v,f) -> DoubleNumberFS(v, fac)
            | RationalNumberFS(arg, f) -> RationalNumberFS(arg, fac)
            | LetterSymFS(arg, f) -> LetterSymFS(arg, fac)
            | WordSymFS(arg, f) -> WordSymFS(arg, fac)
            | _ as other -> other
    
    static let remove(args, expr) = 
        let  found = ref false
        List.filter (fun arg -> if (!found) then true else found  := (arg = expr); !found = false) args
        
    // shorthands for creating Exprs
    static let rec bint(a) = IntegerNumberFS(BigInt(a.ToString()), null)
    static let MatchInt(factor, num) =
        match factor with
            | IntegerNumberFS(value, f) when value.ToString()=num.ToString() -> true
            | _ -> false
    static let rec times(args') = 
        let args = remove(args', bint(1))
        if (List.length(args) > 1) then CompositeExprFS(WellKnownSymFS(WKSID.times), args, null)
        else if (args.IsEmpty) then  bint(1) 
        else   List.head args
    static let rec plus(args) = 
        if (List.length(args) > 1) then  CompositeExprFS(WellKnownSymFS(WKSID.plus), args, null)
        else if (List.length(args) = 1) then args.[0]
        else if (args.IsEmpty) then  bint(0) 
        else   List.head args
    static let rec eq(args) =  CompositeExprFS(WellKnownSymFS(WKSID.equals), args, null)
    static let rec power(a,b) =  CompositeExprFS(WellKnownSymFS(WKSID.power), a::[b], null)
    static let rec inverse(b) = CompositeExprFS(WellKnownSymFS(WKSID.divide), [b], null)
    static let rec inverseN(b) = CompositeExprFS(WellKnownSymFS(WKSID.divide), b, null)
    static let rec minusF(b,fac) = 
        match b with
            | CEXP(SymMinus, [CompositeExprFS(a, arg, f2)], f) -> CompositeExprFS(a, arg, if (fac<>null) then fac else if (f2<> null) then f2 else f)
            | CEXP(SymMinus, [IntegerNumberFS(a, f2)], f) -> IntegerNumberFS(a, if (fac<>null) then fac else if (f2<> null) then f2 else f)
            | CEXP(SymMinus, [DoubleNumberFS(a, f2)], f) -> DoubleNumberFS(a, if (fac<>null) then fac else if (f2<> null) then f2 else f)
            | CEXP(SymMinus, [LetterSymFS(a, f2)], f) -> LetterSymFS(a, if (fac<>null) then fac else if (f2<> null) then f2 else f)
            | CEXP(SymMinus, [WordSymFS(a, f2)], f) -> WordSymFS(a, if (fac<>null) then fac else if (f2<> null) then f2 else f)
            | CEXP(SymMinus, [arg], f) -> arg
            | BINT(value, f) -> IntegerNumberFS(-value, if (fac<>null) then fac else f)
            | CompositeExprFS(a, args, f) -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [CompositeExprFS(a,args,null)], if (fac<>null) then fac else f)
            | LetterSymFS(arg, f) -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [LetterSymFS(arg,null)], if (fac<>null) then fac else f)
            | _ -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [b], if (fac<>null) then fac else null)
    static let rec minus(b) = minusF(b,null)
    
    // some utilties
        
    
    static let rec _FixNegatives (inExpr : ExprFS) =
        match inExpr with
            | BINT(v, f) when v < BigInt("0") -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [bint(-v)], f)
            | DINT(v,f) when v < double(0) -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [DoubleNumberFS(-v,null)], f)
            | CEXP(SymTimes, neg::args, f) ->
                match neg with 
                    | CEXP(SymMinus, [BINT(n,f2)], f) -> minus(CompositeExprFS(WellKnownSymFS(WKSID.times), (bint(n)::args), f))
                    | CEXP(SymMinus, [DINT(n,f2)], f) -> minus(times(DoubleNumberFS(n, null)::args))
                    | _ -> CompositeExprFS(WellKnownSymFS(WKSID.times), List.map _FixNegatives (neg::args), f)
            | CEXP(func, args, f) -> CompositeExprFS(func, List.map _FixNegatives args, f)
            | _ as other -> other
    static let FromExpr(inexpr) = _FixNegatives(fromexpr(inexpr))
    static let ToExpr(inexp) = toexpr(inexp) // _FixNegatives(inexp))
    
    static let  divisor(arg) =
            match arg with
                | CEXP(SymDiv, dividend, f) when f = null -> dividend
                | _ -> []
    
    static let rec flat(args) = 
        let rec flattened  terms  arg  =
                match arg with
                    | CEXP(SymTimes, moreterms, f) when f = null ->  terms @ (flat moreterms)
                    | _  as other -> List.append terms [other]
        List.fold (flattened) [] args
        
    static let rec flatSum(args) = 
        let rec flattened  terms  arg  =
                match arg with
                    | CEXP(SymPlus, moreterms, f) when f = null ->  terms @ (flatSum moreterms)
                    | _  as other -> List.append terms [other]
        List.fold (flattened) [] args
        
    static let containsTop(inList, expr)      =  List.tryFind (fun arg -> arg=expr) inList <> None
    static let containsDivision(inList) =  
        let isDiv(expr) =
            match expr with
                | CEXP(SymDiv, args, f) -> true
                | _ -> false
        List.tryFind isDiv inList <> None
    
    static let rec equivMatch(expr1, expr2) =
        let rec equivLists flag arg1 arg2 = equivMatch(arg1, arg2) && flag
        let num1 = 
            match expr1 with
                | CompositeExprFS(head, args, f) -> args.Length
                | _ -> 0
        let num2 =
            match expr2 with
                | CompositeExprFS(head, args, f) -> args.Length
                | _ -> 0
        if (num1 = num2) then
            let res = match (expr1, expr2) with
                | (CompositeExprFS(head, args, f), CompositeExprFS(head2, args2, f2)) when f=f2 -> equivMatch(head, head2) && List.fold2 (equivLists) true args  args2
                | (DoubleNumberFS(valF, f), DoubleNumberFS(valF2, f2))when f=f2  ->  valF = valF2
                | (IntegerNumberFS(valF, f), IntegerNumberFS(valF2, f2)) when f=f2 ->  valF = valF2
                | (RationalNumberFS(valF, f), RationalNumberFS(valF2, f2)) when f=f2 ->  valF = valF2
                | (LetterSymFS(valF, f), LetterSymFS(valF2, f2)) when f=f2 ->  valF = valF2
                | (WordSymFS(valF, f), WordSymFS(valF2, f2)) when f=f2 ->  valF = valF2
                | _ as c -> expr1 = expr2
            res
        else
            false
            
    static let powMatchEquiv(term,target) =
        match term with
            | CEXP(SymPower, bas::[power], f) when equivMatch(bas,target) -> true
            | _ -> false
    
        
    static let removeAny(args, expr) = 
        let  found = ref false
        List.filter (fun arg -> if (!found) then true else found  := equivMatch(arg, expr); !found = false) args
        
    static let reduceFirstPower(args, expr) = 
        let  found = ref false
        List.collect( (fun arg -> 
            match arg with
                | CEXP(SymPower, bas::[pow], f) when !found = false  -> found := true; [FromExpr(Engine.Simplify(toexpr(power(bas, plus(pow::[bint(-1)])))))]
                | _ -> [arg]
                )) args   
                   
    static let multiplyAll(args, expr) = 
        let exprDiv =
            match expr with
                | CEXP(SymDiv, [divisor], f) -> [divisor]
                | _ -> []
        let multipler(arg) =
            let isDiv = (fun arg -> match arg with | CEXP(SymDiv, a, f2) -> true | _ -> false)
            match arg with
                | CEXP(SymTimes, args, f) when List.tryFind isDiv args <> None && exprDiv <> [] -> 
                    let divTerm = List.find isDiv args
                    let divisor =  match divTerm with | CEXP(SymDiv, [div], f2) -> div
                    [times(remove(args, divTerm)@[CompositeExprFS(WellKnownSymFS(WKSID.divide), [times(divisor::exprDiv)], f)])]
                | CEXP(SymDiv, [arg2], f) when exprDiv <> [] -> [CompositeExprFS(WellKnownSymFS(WKSID.divide), [times(arg2::exprDiv)], f)]
                | CEXP(SymMinus, arg2, f) -> [minusF(times(arg2 @ [expr]), f)]
                | _ -> [times(arg::[expr])]
        List.collect multipler args
        
    static let containsTopAny(inList, expr:ExprFS) =  List.tryFind (fun arg -> equivMatch(arg, expr) ) inList <> None
    
    static let containsTopAnyPower(inList, expr:ExprFS) =  
        List.tryFind (fun arg -> powMatchEquiv(arg,expr) ||equivMatch(arg, expr) ) inList <> None
        
        // looks for an exact match for 'expr' in the list of additive terms 'inList'
    static let containsInSumExact(inList, expr) = 
        let contains(inExpr,  expr) =
            match inExpr with
                | _ as other when other = expr -> true
                | _  -> false
        List.length( List.filter (fun arg -> contains(arg,expr)) inList) <> 0
        
        // looks for a match for 'expr' in the list of additive terms 'inList' where 'expr' may be multiplied by something else
    static let containsInSum(inList, expr) = 
        let contains(inExpr,  expr) =
            match inExpr with
               // | _ as other when other = expr -> true
                | CompositeExprFS(SymTimes, args, f)  when containsTopAny(flat(args),expr) -> true // -> let found = false in (args |> List.map (fun arg -> found = (found || contains(arg, thing)))); found
                | CompositeExprFS(SymMinus, args, f) when containsTopAny(flat(args),expr) ->true
                | _  when equivMatch(inExpr, expr) -> true // -> let found = false in (args |> List.map (fun arg -> found = (found || contains(arg, thing)))); found
                | _  -> false
        List.length( List.filter (fun arg -> contains(arg,expr)) inList) <> 0
        
    static let containsAnywhere(inList, expr) = 
        let rec contains(inExpr,  expr) =
            match inExpr with
                | _ as other when other = expr  -> true
                | CompositeExprFS(head, args, f)  -> List.tryFind  (fun arg -> contains(arg, expr) ) args <> None // -> let found = false in (args |> List.map (fun arg -> found = (found || contains(arg, thing)))); found
                | _  -> false
        List.length( List.filter (fun arg -> contains(arg,expr)) inList) <> 0
    
    static let rec getFactor(b) =
        match b with 
            | CEXP(func, list, f) -> f
            | BINT(v, f) -> f
            | DINT(v, f) -> f
            | LSYM(v, f) -> f
            | _ -> null
            
    static let rec divide(a,b) = 
        let rec isDiv = (fun arg -> match arg with | CEXP(SymDiv, a, f2) -> true | _ -> false)
        let rec isMatch = (fun arg ->  match arg with | any when any=b -> true | _ -> false )
        let rec bInv = 
            match b with
                | CEXP(SymDiv, [arg], f) -> setFactor(arg,f)
                | CEXP(SymTimes, mult::[CEXP(SymDiv, [arg], f)], f2) when mult=bint(1) -> setFactor(arg,f2)
                | CEXP(SymTimes, args, f) -> 
                    let divs = List.filter isDiv args
                    let muls = List.filter (fun arg -> isDiv(arg) <> true) args
                    let newMuls = List.collect (fun arg -> match arg with | CEXP(SymDiv, [arg], f) -> [arg] |_ -> [arg]) divs
                    let newDivs = times(muls)
                    let newDiv = CompositeExprFS(WellKnownSymFS(WKSID.divide), [newDivs], null)
                    if (newMuls.Length > 0) then
                        CompositeExprFS(WellKnownSymFS(WKSID.times), newMuls @ [newDiv], f)
                    else 
                        newDiv
                | _ -> inverse(b)
                // CompositeExprFS(WellKnownSymFS(WKSID.times), bint(1)::[inverse(b)], getFactor(b))   // bcz: this makes divisions look like: a * (1/b)
                  //  inverse(b)                 // bcz: this makes divisions look like a/b
        match a with
        // bcz: this "simplifies" divisions when the numerator and the denominator contain the exact, labeled factor term
            | CEXP(SymTimes, args, f) when List.tryFind isMatch (args) <> None -> 
                let matchTerm = List.find isMatch (args)
                times(remove(args, matchTerm))
            | CEXP(SymTimes, args, f) when List.tryFind isDiv (flat(args)) <> None && bInv=b -> 
                let divTerm = List.find isDiv (flat(args))
                times(inverse(times(b :: divisor(divTerm))) :: remove(flat(args), divTerm))
            | CEXP(SymTimes, args, f) when bInv<>b ->
                times(bInv::args)
            | _ -> times(a :: [bInv])
            
    static let rec divideSimplify(a', b') =
        let a = clearFactor(a')
        let b = clearFactor(b')
        match a with
        // bcz: turn off all simplification of divisions for now...
            | CEXP(SymTimes, args, f) when containsTopAny(args, b) -> times(removeAny(args,b))
            | CEXP(SymTimes, args, f) when containsTopAny(flat(args), b) -> times(removeAny(flat(args),b))
            | CEXP(SymTimes, args, f) when containsTopAnyPower(args, b) -> times(reduceFirstPower(args,b))
            | CEXP(SymTimes, args, f) when containsTopAnyPower(flat(args), b) -> times(reduceFirstPower(flat(args),b))
            | CEXP(SymMinus, [arg], f) -> CompositeExprFS(WellKnownSymFS(WKSID.minus), [divideSimplify(arg, b')], f)
            | _ when a = b -> bint(1)
            | _ -> divide(a, b')
            
    static let simplify(expr) = FromExpr(Engine.Simplify(toexpr(expr)))
       
            
    // Math Functions
    static let _FactorOut (inExpr : ExprFS, factor, factorMinus) =
        let isInvFactor(a) =
            match a with 
                | CEXP(SymDivide, [args], f) when args=factor -> true
                | CEXP(SymTimes, a::[CEXP(SymDivide, [args],fs)], f) when args=factor && a=bint(1) -> true
                | _ -> false
        Engine.ClearAll()
        match inExpr with
            | _ as other when inExpr = factor -> factor
                        // num/root(x) -> num*root(x)/x
            | CEXP(SymTimes, num::[CEXP(SymDiv, [CEXP(SymRoot, root::[bas], f)], f2)], f3)   when MatchInt(root,2) -> times(CompositeExprFS(WellKnownSymFS(WKSID.root), root::[bas], f)::[inverse(bas)])
                       // 1/root(x) -> root(x)/x
            | CEXP(SymDiv, [CEXP(SymRoot, root::[bas], f)], f2)                       when MatchInt(root,2) -> times(CompositeExprFS(WellKnownSymFS(WKSID.root), root::[bas], f)::[inverse(bas)])
                       // (x + b + c) / m -> x/m + (b + c)/m   
            | CEXP(SymTimes, CEXP(SymPlus, args, f) :: [CEXP(SymDiv, [args2], f2)], f3) when containsInSumExact(flat(args), factor) ->  plus(divide(factor,args2) :: [simplify(divide(plus(remove(flatSum(args),factor)),args2))] )
                       // (x + b + c) / m -> x/m + (b + c)/m   
            | CEXP(SymTimes, CEXP(SymDiv, [args2], f2) :: [CEXP(SymPlus, args, f)], f3) when containsInSumExact(flat(args), factor) ->  plus(divide(factor,args2) :: [simplify(divide(plus(remove(flatSum(args),factor)),args2))] )
                            // c(a+b+dx) -> cx(a/ + b/x + d/x)
            | CEXP(SymTimes,CEXP(SymPlus, args2, f)::args, f2)                          when containsInSum(args2, factor) -> times(factor::[plus(List.map (fun arg -> divideSimplify(arg,factor)) args2)]@args)
                             // (a+b+d/x) -> 1/x*(ax+bx+d)
            | CEXP(SymPlus, args2, f)                                                   when containsInSum(args2, inverse(factor)) -> times(divide(bint(1),factor)::[plus(List.map (fun arg -> simplify(times(arg::[factor]))) args2)])
                            // (a+b+dx)c -> x(a/x + b/x + d/x)c
            | CEXP(SymTimes,[args; CEXP(SymPlus, args2, f)], f2)                        when containsInSum(args2, factor) && args<>factor && isInvFactor(factor) <> false -> times(factor::args::[plus(List.map (fun arg -> divideSimplify(arg,factor)) args2)])
                            // ax + b + c -> x(a + b/x + c/x)
            | CEXP(SymPlus, args, f)                                                    when containsInSum(args, factor) -> times(factor::[plus(List.map (fun arg -> divideSimplify(arg,factor)) args)])
                                                                                                            // if we don't want to "simplify" the ax term, use --  times(factor::[plus(List.map (fun arg -> times(times(bint(1)::[inverse(factor)])::[arg])) args)])
                        // abx  -> xab
            | CEXP(SymTimes, args, f)                when containsDivision(flat(args)) && containsTop(flat(args), factor) ->  times(factor::[times(remove((flat)args,factor))])
                        // abx  -> xab
            //| CEXP(SymTimes, args, f)                                                   when containsTop(flat(args), factor) ->  times(factor::remove((flat)args,factor))
                       // -x -> x(-1)
            | CEXP(SymMinus, [arg], f)                                                  when containsTop(flat([arg]), factor) ->  times(factor :: [minus(times(remove(flat([arg]),factor)))] )
                        // a/(bx)  -> 1/x a / b
            | CEXP(SymTimes, numerator ::[CEXP(SymDiv, [CEXP(SymTimes, args, f)], f2)], f3)  when containsTop(flat(args), factor) ->  times(inverse(factor) :: [divide(numerator,  times(remove(flat(args),factor))) ])
                        // 1/(ax)  -> 1/x*1/a
            //| CEXP(SymDiv, [CEXP(SymTimes, args, f)], f2)                               when containsTop(flat(args), factor) ->  times(inverse(factor) :: [inverse(times( remove(flat(args),factor))) ] )
                       // a^n -> a * a^(n-1)
            | CEXP(SymPower, bas :: (pow :: rem), f)                                    when bas = factor ->  times(factor:: [FromExpr(Engine.Simplify(toexpr(power(bas, plus(pow :: [minus(bint(1))])))))] )
                        // leave it
            | _ as other -> other
            
    static let rec _SplitFractionalSum (inExpr : ExprFS, factor) =
        Engine.ClearAll()
        match inExpr with
                        // c+(a + x)/ n -> c + a/n + x/n
            | CEXP(SymTimes, CEXP(SymPlus, args, f)::[CEXP(SymDiv, [div],f3)], f2)  when containsInSumExact(args,factor) -> 
                let  found = ref false
                let start = List.filter (fun arg -> if (!found) then false else found  := (arg = factor); !found = false) args
                found.Value <- false
                let rest  = List.filter (fun arg -> if (!found) then true else found  := (arg = factor); !found = true) args
                if (start.Length = 0 || (start.Length = 1 && start.[0] = bint(1))) then
                    inExpr
                else
                    plus(times(plus(start)::[inverse(div)])::[times(plus(rest)::[inverse(div)])])
            | _ as other -> other
            
    static let rec _SplitProductIntoSum (inExpr : ExprFS, factor, numLevels) =
        Engine.ClearAll()
        match inExpr with
                        // n -> (n+numLevels)-numLevels
            | BINT(v,f) when inExpr=factor -> 
                plus(bint(v+BigInt(numLevels.ToString()))::[bint(-numLevels)])
                        // x -> (n+numLevels)*x-numLevels*x
            | _ when inExpr=factor -> 
                if (numLevels > 0) then
                    plus(times(bint(numLevels)::[inExpr])::[minus(times(bint(numLevels)::[inExpr]))])
                else
                    plus(minus(times(bint(-numLevels)::[inExpr]))::[times(bint(-numLevels)::[inExpr])])
                        // nx-> (n+numLevels)*x-numLevels*x
                        // bcz: want to have the sum that's created here get merged w/ the sum that contains this term if it exists!!
            | CEXP(SymTimes, args, f) when containsTop(args,factor) -> 
                let found = ref false
                let start = List.filter (fun arg -> if (found.Value) then false else found  := (arg = factor); found.Value = false) args
                found.Value <- false
                let rest  = List.filter (fun arg -> if (found.Value) then true else found  := (arg = factor); found.Value = true) args
                if (start.Length = 1 && start.[0] = bint(1)) then
                    inExpr
                else 
                    let numLeft = simplify(plus(times(start@remove(rest,factor))::[bint(numLevels)]))
                    match numLeft with
                        | BINT(v,f) when v < BigInt("0")-> if (numLevels < 0) then  plus(minus(times(bint(-v)::[factor]))::[times(bint(-numLevels)::[factor])]) else plus(minus(times(bint(-v)::[factor]))::[minus(times(bint(numLevels)::[factor]))]) 
                        | CEXP(SymMinus, [args], f) -> if (numLevels < 0) then  plus(minus(times(args::[factor]))::[times(bint(-numLevels)::[factor])]) else plus(minus(times(args::[factor]))::[minus(times(bint(numLevels)::[factor]))]) 
                        | _ -> if (numLevels < 0) then  plus(times(numLeft::[factor])::[times(bint(-numLevels)::[factor])]) else plus(times(numLeft::[factor])::[minus(times(bint(numLevels)::[factor]))]) 
                        // leave it
            | _ as other -> other
            
    static let rec _SplitProductIntoFraction (inExpr : ExprFS, factor, numLevels) =
        Engine.ClearAll()
        match inExpr with
                        // n -> (n* numlevels)/numLevels
            | BINT(v,f) when inExpr=factor -> 
                times(IntegerNumberFS(BigInt(numLevels.ToString())*v,null)::[ inverse(bint(numLevels))])
                        // x -> (n*numLevels)*x-numLevels*x
            | CEXP(SymPower, [bas; pow], f) ->
                times(power(bas,times(pow::[bint(numLevels+1)]))::[ inverse(power(bas,times(pow::[bint(numLevels)])))])
            | _ when inExpr=factor -> 
                if (numLevels <> 0) then
                    times(power(inExpr,bint(numLevels+1))::[inverse(power(inExpr,bint(numLevels)))])
                else inExpr 
            | CEXP(SymTimes, args, f) when containsTop(args,factor) -> 
                if (numLevels <> 0) then
                    match factor with 
                        | CEXP(SymPower, [bas; pow], f) ->
                            times([power(bas,times(pow::[bint(numLevels+1)]))]@remove(args,factor)@[ inverse(power(bas,times(pow::[bint(numLevels)])))])
                        | _ -> times([power(factor,bint(numLevels+1))]@remove(args,factor)@[ inverse(power(factor,bint(numLevels)))])
                else inExpr 
                     // leave it
            | _ as other -> other
            
    static let rec _FlattenSums (inExpr : ExprFS) =
        let rec flattenSum(args) = 
            let rec flattened  terms  arg  =
                    match arg with
                        | CEXP(SymPlus, moreterms, f) ->  terms @ (flattenSum moreterms)
                        | CEXP(func, args, f) -> List.append terms [CompositeExprFS(func, List.map _FlattenSums args, f)]
                        | _  as other -> List.append terms [other]
            List.fold (flattened) [] args
        match inExpr with
            | CEXP(SymPlus, args, f) -> CompositeExprFS(WellKnownSymFS(WKSID.plus), flattenSum(args), f)
            | CEXP(func, args, f) -> CompositeExprFS(func, List.map _FlattenSums args, f)
            | _ as other -> other
            
            
    static let rec _FlattenMults (inExpr : ExprFS) =
        let rec flat(args) = 
            let rec flattened  terms  arg  =
                    match arg with
                        | CEXP(SymTimes, moreterms, f) ->  terms @ (flat moreterms)
                        | CEXP(func, args, f) -> List.append terms [CompositeExprFS(func, List.map _FlattenMults args, f)]
                        | _  as other -> List.append terms [other]
            List.fold (flattened) [] args
        
        match inExpr with
            | CEXP(SymTimes, args, f) -> CompositeExprFS(WellKnownSymFS(WKSID.times), flat(args), f)
            | CEXP(func, args, f) -> CompositeExprFS(func, List.map _FlattenMults args, f)
            | _ as other -> other
            
    static let rec _RemoveTerm (inExpr : ExprFS, term : ExprFS) =
        match inExpr with
            | CEXP(SymPlus, args, f)  when containsInSumExact(args, term) -> plus(remove(args, term))
            | CEXP(SymTimes, args, f) when containsInSumExact(args, term) -> times(remove(args, term))
            | CEXP(head, args, f) -> CompositeExprFS(head, List.map (fun arg -> _RemoveTerm(arg, term)) args, f)
            | _ as other -> other
            
    static let _DivideAcross (lhs : ExprFS, rhs : ExprFS, factor) =
        Engine.ClearAll()
        match (lhs, rhs) with
            | (lhs, rhs)                     when MatchInt(lhs,0) -> eq(lhs::[rhs])
            | (lhs, rhs)                     when lhs = factor -> eq(bint(1) :: [divideSimplify(rhs,factor)])
                        // a + b + x  = z  -> a/x + b/x + 1 = z / x
            | (CEXP(SymPlus, args, f), rhs)   when containsInSumExact(args,factor) ->  eq(plus(bint(1) :: multiplyAll(remove(args,factor),inverse(factor))) :: [ times(flat(rhs :: [inverse(factor)] )) ] )
                        // -ab/x = z -> -ab = xz
            | (CEXP(SymMinus,[CEXP(SymTimes, args, f)],f2), rhs)  when containsTop(args, inverse(factor)) -> eq(minus(times(remove(args,inverse(factor)))) :: [ times(flat(rhs::[factor])) ]) 
                        // 1/x*ab = z -> ab = xz
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, divide(bint(1),factor)) -> eq(times(remove(args,divide(bint(1),factor))) :: [ times(rhs::[factor]) ]) 
                         // ab/x = z -> ab = xz
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, inverse(factor)) -> eq(times(remove(args,inverse(factor))) :: [ times(rhs::[factor]) ]) 
                        // abx = 0 -> ab = 0
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, factor) && MatchInt(rhs,0) = true -> eq(times(remove(args,factor)) :: [ rhs ]) 
                        // abx = z -> ab = z/x
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, factor) && MatchInt(factor,0) = false -> eq(times(remove(args,factor)) :: [ divide(rhs,factor) ]) 
                        // a + b + x  = z  -> a + b  = z - x
            | (CEXP(SymPlus, args, f), rhs)     when containsInSumExact(args,factor) ->  eq(plus(remove(args, factor)) :: [ plus (minus(factor) :: [rhs]) ] )
                        // leave it
            | (lhs, rhs) as other -> eq(lhs::[rhs])
            
    static let _DivideAcrossLtoR(inExpr : ExprFS, factor) =
        Engine.ClearAll()
        match inExpr with
            | CEXP(SymEq, lhs :: [rhs], f)  -> _DivideAcross(lhs, rhs, factor)
                        // leave it
            | _ as other -> other
            
    static let rec _DivideAcrossRtoL(inExpr : ExprFS, factor) =
        match inExpr with
            | CEXP(SymEq, lhs :: [rhs], f)  -> 
                match _DivideAcross(rhs, lhs, factor) with
                    | CEXP(SymEq, left::[right], f) -> eq(right::[left])
                    | _ as other -> other
                        // leave it
            | _ as other -> other
            
    static let _MoveAcross (lhs: ExprFS, rhs: ExprFS, factor) =
        Engine.ClearAll()
        match (lhs, rhs) with
                        // x'th root(n) = z -> n = z^x
            | (CEXP(SymRoot, root::[bas], f),rhs) when factor=root -> eq(bas::[CompositeExprFS(WellKnownSymFS(WKSID.power), rhs::[root],null)])
                        // n^x = z -> n = x'th root(z)
            | (CEXP(SymPower, bas::[exp], f), rhs) when factor=exp -> eq(bas::[CompositeExprFS(WellKnownSymFS(WKSID.root), exp::[rhs],null)])
                        // -ab/x = z -> -ab = xz
//            | (CEXP(SymMinus,[CEXP(SymTimes, args, f)],f2), rhs)  when containsTop(args, inverse(factor)) -> eq(minus(times(remove(args,inverse(factor)))) :: [ times(flat(rhs::[factor])) ]) 
                        // 1/x = z -> 1 = xz
            | (CEXP(SymDiv, [arg], f), rhs)  when arg=factor -> eq(bint(1) :: [ times(flat(rhs::[factor])) ]) 
                        // ab/x = z -> ab = xz
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, inverse(factor)) -> eq(times(remove(args,inverse(factor))) :: [ times(flat(rhs::[factor])) ]) 
                         // 1/x*ab = z -> ab = xz
            | (CEXP(SymTimes, args, f), rhs)  when containsTop(args, divide(bint(1),factor)) -> eq(times(remove(args,divide(bint(1),factor))) :: [ times(flat(rhs::[factor])) ]) 
                        // a + b + 0  = z  -> a + b  = z 
            | (CEXP(SymPlus, args, f), rhs)   when containsInSumExact(args,factor)&& MatchInt(factor,0) ->  eq(plus(remove(args, factor)) :: [ rhs ] )
                       // a + b + x  = z + d -> a + b  = z + d - x
            | (CEXP(SymPlus, args, f), CEXP(SymPlus, args2, f2))   when containsInSumExact(args,factor) ->  eq(plus(remove(args, factor)) :: [ plus (minus(factor) :: args2) ] )
                        // a + b + x  = 0  -> a + b  = - x
            | (CEXP(SymPlus, args, f), IntegerNumberFS(num,f2))   when containsInSumExact(args,factor) && num=BigInt("0") ->  eq(plus(remove(args, factor)) :: [ minus(factor) ] )
                        // a + b + x  = z  -> a + b  = z - x
            | (CEXP(SymPlus, args, f), rhs)   when containsInSumExact(args,factor) ->  eq(plus(remove(args, factor)) :: [ plus (minus(factor) :: [rhs]) ] )
                        // x(a+b...)  = z  -> a+b... = z/x
 //           | (CEXP(SymTimes, args, f), rhs)  when containsTop(args,factor) ->  eq(times(remove(args, factor)) :: [ divide(rhs, factor)  ] )
                        // lhs  = a+b  -> 0  = a + b - lhs
            | (lhs,CEXP(SymPlus, args,f))     when lhs=factor && MatchInt(lhs,0) = false  ->  eq(bint(0) :: [ plus (minus(factor) :: args) ] )
                       // lhs  = z  -> 0  = z - lhs
            | (lhs,rhs)                       when lhs=factor && MatchInt(lhs,0)=false  ->  eq(bint(0) :: [ plus (minus(factor) :: [rhs]) ] )
                        // leave it
            | (lhs,rhs) -> eq(lhs::[rhs])
            
    static let _MoveAcrossLtoR(inExpr : ExprFS, factor) =
        Engine.ClearAll()
        match inExpr with
            | CEXP(SymEq, lhs :: [rhs], f)  -> _MoveAcross(lhs, rhs, factor)
                        // leave it
            | _ as other -> other
            
    static let rec _MoveAcrossRtoL(inExpr : ExprFS, factor) =
        match inExpr with
            | CEXP(SymEq, lhs :: [rhs], f)  -> 
                match _MoveAcross(rhs, lhs, factor) with
                    | CEXP(SymEq, left::[right], f) -> eq(right::[left])
                    | _ as other -> other
                        // leave it
            | _ as other -> other
            
    static member public FactorOut(inexpr, inLet, factorMinus) = ToExpr(_FactorOut(fromexpr(inexpr), fromexpr(inLet), factorMinus))
    static member public MoveAcross(inexpr, inLet, lhs) = (ToExpr(if (lhs) then _MoveAcrossLtoR(fromexpr(inexpr),fromexpr(inLet)) else _MoveAcrossRtoL(fromexpr(inexpr), fromexpr(inLet))))
    static member public DivideAcross(inexpr, inLet, lhs) = (ToExpr(if (lhs) then _DivideAcrossLtoR(fromexpr(inexpr),fromexpr(inLet)) else _DivideAcrossRtoL(fromexpr(inexpr), fromexpr(inLet))))
    static member public SplitFractionalSum(inexpr, inLet) = (ToExpr(_SplitFractionalSum(FromExpr(inexpr), FromExpr(inLet))))
    static member public SplitProductIntoSum(inexpr, inLet, numLevels) = (ToExpr(_SplitProductIntoSum(FromExpr(inexpr), FromExpr(inLet), numLevels)))
    static member public SplitProductIntoFraction(inexpr, inLet, numLevels) = (ToExpr(_SplitProductIntoFraction(FromExpr(inexpr), FromExpr(inLet), numLevels)))
    static member public FlattenSums(inexpr) = (ToExpr(_FlattenMults(_FlattenSums(FromExpr(inexpr)))))
    static member public FlattenMults(inexpr) = (ToExpr(_FlattenMults(FromExpr(inexpr))))
    static member public FixNegatives(inexpr) = (ToExpr(_FixNegatives(FromExpr(inexpr))))
    static member public RemoveTerm(inexpr, term) = (ToExpr(_RemoveTerm(FromExpr(inexpr), FromExpr(term))))
