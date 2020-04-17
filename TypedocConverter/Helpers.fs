﻿module Helpers

open Definitions
open Entity

let printWarning (warn: string) = 
    let backup = System.Console.ForegroundColor
    System.Console.ForegroundColor <- System.ConsoleColor.Yellow
    System.Console.Error.WriteLine ("[Warning] " + warn)
    System.Console.ForegroundColor <- backup
    
let printError (err: string) = 
    let backup = System.Console.ForegroundColor
    System.Console.ForegroundColor <- System.ConsoleColor.Red
    System.Console.Error.WriteLine ("[Error] " + err)
    System.Console.ForegroundColor <- backup

let rec toPascalCase (str: string) =
    if str.Length = 0
    then str
    else if str.Contains "." then 
         str.Split "." |> Array.map toPascalCase |> Array.reduce (fun a n -> a + "." + n)
         else 
            str.Split([| "-"; "_" |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun x -> x.Substring(0, 1).ToUpper() + x.Substring 1)
            |> Array.reduce (fun accu next -> accu + next)

let escapeSymbols (text: string) = 
    if isNull text then ""
    else text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")

let toCommentText (text: string) = 
    if isNull text then ""
    else text.Split "\n" |> Array.map (fun t -> "/// " + escapeSymbols t) |> Array.reduce(fun accu next -> accu + "\n" + next)

let getXmlDocComment (comment: Comment) =
    let prefix = "/// <summary>\n"
    let suffix = "\n/// </summary>"
    let summary = 
        match comment.Text with
        | Some text -> prefix + toCommentText comment.ShortText + toCommentText text + suffix
        | _ -> 
            match comment.ShortText with
            | "" -> ""
            | _ -> prefix + toCommentText comment.ShortText + suffix
    let returns = 
        match comment.Returns with
        | Some text -> "\n/// <returns>\n" + toCommentText text + "\n/// </returns>"
        | _ -> ""
    summary + returns

let getCommentFromSignature (node: Reflection) =
    let signature = 
        match node.Comment with
        | Some comment -> [getXmlDocComment comment]
        | _ -> []
    match signature with
    | [] -> ""
    | _ -> signature |> List.reduce(fun accu next -> accu + "\n" + next)

let getComment (node: Reflection) =
    match node.Comment with
    | Some comment -> getXmlDocComment comment
    | _ -> ""

let rec getType (config: Config) (typeInfo: Type): Entity = 
    let containerType =
        match typeInfo.Type with
        | "intrinsic" -> 
            match typeInfo.Name with
            | Some name -> 
                match name with
                | "number" -> TypeEntity(typeInfo.Id, config.NumberType, typeInfo.Type, [])
                | "boolean" -> TypeEntity(typeInfo.Id, "bool", typeInfo.Type, [])
                | "string" -> TypeEntity(typeInfo.Id, "string", typeInfo.Type, [])
                | "void" -> TypeEntity(typeInfo.Id, "void", typeInfo.Type, [])
                | "any" -> TypeEntity(typeInfo.Id, config.AnyType, typeInfo.Type, [])
                | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
            | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
        | "reference" | "typeParameter" -> 
            match typeInfo.Name with
            | Some name -> 
                match name with
                | "Promise" -> TypeEntity(typeInfo.Id, "System.Threading.Tasks.Task", typeInfo.Type, [])
                | "Set" -> TypeEntity(typeInfo.Id, "System.Collections.Generic.ISet", typeInfo.Type, [])
                | "Map" -> TypeEntity(typeInfo.Id, "System.Collections.Generic.IDictionary", typeInfo.Type, [])
                | "Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [])
                | "Date" -> TypeEntity(typeInfo.Id, "System.DateTime", typeInfo.Type, [])
                | "BigUint64Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "ulong", "intrinsic", [])])
                | "Uint32Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "uint", "intrinsic", [])])
                | "Uint16Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "ushort", "intrinsic", [])])
                | "Uint8Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "byte", "intrinsic", [])])
                | "BigInt64Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "long", "intrinsic", [])])
                | "Int32Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "int", "intrinsic", [])])
                | "Int16Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "short", "intrinsic", [])])
                | "Int8Array" -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "char", "intrinsic", [])])
                | "RegExp" -> TypeEntity(typeInfo.Id, "string", typeInfo.Type, []);
                | x -> TypeEntity(typeInfo.Id, x, typeInfo.Type, []);
            | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
        | "array" -> 
            match typeInfo.ElementType with
            | Some elementType -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [getType config elementType])
            | _ -> TypeEntity(typeInfo.Id, "System.Array", typeInfo.Type, [TypeEntity(0, "object", "intrinsic", [])])
        | "stringLiteral" -> TypeEntity(typeInfo.Id, "string", typeInfo.Type, [])
        | "tuple" ->
            match typeInfo.Types with
            | Some innerTypes -> 
                match innerTypes with
                | [] -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
                | _ -> TypeEntity(typeInfo.Id, "System.ValueTuple", typeInfo.Type, innerTypes |> List.map (getType config))
            | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
        | "union" -> 
            match typeInfo.Types with
            | Some innerTypes -> 
                match innerTypes with
                | [] -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
                | _ -> UnionTypeEntity(typeInfo.Id, typeInfo.Type, innerTypes |> List.map (getType config))
            | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
        | "intersection" -> 
            let types = 
                match typeInfo.Types with
                | Some innerTypes -> 
                    innerTypes 
                    |> List.map (getType config) 
                    |> List.map (fun x ->
                        match x with 
                        | TypeEntity(_, name, _, _) -> name 
                        | UnionTypeEntity _ -> "union"
                        | _ -> "object"
                    )
                | _ -> []

            printWarning ("Intersection type " + System.String.Join(" & ", types) + " is not supported.")
            TypeEntity(typeInfo.Id, "object", typeInfo.Type, []) // TODO: generate intersections
        | "reflection" -> 
            match typeInfo.Declaration with
            | Some dec -> 
                match dec.Signatures with
                | Some [signature] -> 
                    let paras = 
                        match signature.Parameters with
                        | Some p -> 
                            p 
                            |> List.map
                                (fun pi -> 
                                    match pi.Type with 
                                    | Some pt -> Some (getType config pt)
                                    | _ -> None
                                )
                            |> List.collect
                                (fun x -> 
                                    match x with
                                    | Some s -> [s]
                                    | _ -> []
                                )
                        | _ -> []
                    let rec getDelegateParas (paras: Entity list): Entity list =
                        match paras with
                        | [x] -> [x]
                        | (front::tails) -> [front] @ getDelegateParas tails
                        | _ -> []
                    let returnsType = 
                        match signature.Type with
                        | Some t -> getType config t
                        | _ -> TypeEntity(0, "void", "intrinsic", [])
                    let typeParas = getDelegateParas paras
                    match typeParas with
                    | [] -> TypeEntity(typeInfo.Id, "System.Action", typeInfo.Type, [])
                    | _ -> 
                        match returnsType with
                        | TypeEntity(_, "void", _, _) -> TypeEntity(typeInfo.Id, "System.Action", typeInfo.Type, typeParas) 
                        | _ -> TypeEntity(typeInfo.Id, "System.Func", typeInfo.Type, typeParas @ [returnsType])
                | _ -> 
                    match dec.Children with
                    | None | Some [] -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
                    | Some children -> 
                        printWarning ("Type literal { " + System.String.Join(", ", children |> List.map(fun c -> c.Name)) + " } is not supported.")
                        TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
            | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
        | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
    handlePromiseType containerType typeInfo config
and handlePromiseType (containerType: Entity) (typeInfo: Type) (config: Config): Entity =
    let mutable container = containerType
    let mutable innerTypes = 
        match typeInfo.TypeArguments with
        | Some args -> getGenericTypeArguments config args
        | _ -> []
    match container with
    | TypeEntity(id, "System.Threading.Tasks.Task", typeId, _) ->
        match innerTypes with
        | [front] | (front::_) -> 
            match front with
            | TypeEntity(_, "void", _, _) ->
                innerTypes <- []
                if config.UseWinRTPromise
                then 
                    container <- TypeEntity(id, "Windows.Foundation.IAsyncAction", typeId, [])
                else 
                    container <- TypeEntity(id, "System.Threading.Tasks.Task", typeId, [])
            | TypeEntity(_, _, _, inner) ->
                if config.UseWinRTPromise
                then 
                    container <- TypeEntity(id, "Windows.Foundation.IAsyncOperation", typeId, inner)
                else 
                    container <- TypeEntity(id, "System.Threading.Tasks.Task", typeId, inner)
            | _ -> ()
        | _ -> ()
    | UnionTypeEntity(id, typeId, inner) -> 
        container <- UnionTypeEntity(id, typeId, inner |> List.map(fun x -> handlePromiseType x typeInfo config))
    | _ -> ()
    match container with
    | TypeEntity(id, name, typeId, inner) -> TypeEntity(id, name, typeId, if innerTypes = [] then inner else innerTypes)
    | UnionTypeEntity(id, typeId, inner) -> UnionTypeEntity(id, typeId, inner)
    | _ -> TypeEntity(typeInfo.Id, "object", typeInfo.Type, [])
and getGenericTypeArguments (config: Config) (typeInfos: Type list): Entity list = 
    typeInfos |> List.map (getType config)
and getGenericTypeParameters (nodes: Reflection list) = // TODO: generate constaints
    let types = 
        nodes 
        |> List.where(fun x -> x.Kind = ReflectionKind.TypeParameter)
    types |> List.map (fun x -> TypeParameterEntity(x.Id, x.Name))

let getMethodParameters (config: Config) (parameters: Reflection list) = 
    parameters
    |> List.where(fun x -> x.Kind = ReflectionKind.Parameter)
    |> List.map(fun x ->
        let name = if isNull x.Name then "" else x.Name
        match x.Type with
        | Some typeInfo -> 
            let typeMeta = getType config typeInfo
            ParameterEntity(x.Id, name, typeMeta)
        | _ -> ParameterEntity(x.Id, name, TypeEntity(0, "object", "intrinsic", []))
    )

let getModifier (flags: ReflectionFlags) = 
    let mutable modifier = []
    match flags.IsPublic with
    | Some flag -> if flag then modifier <- modifier |> List.append [ "public" ] else ()
    | _ -> ()
    match flags.IsAbstract with
    | Some flag -> if flag then modifier <- modifier |> List.append [ "abstract" ] else ()
    | _ -> ()
    match flags.IsPrivate with
    | Some flag -> if flag then modifier <- modifier |> List.append [ "private" ] else ()
    | _ -> ()
    match flags.IsProtected with
    | Some flag -> if flag then modifier <- modifier |> List.append [ "protected" ] else ()
    | _ -> ()
    match flags.IsStatic with
    | Some flag -> if flag then modifier <- modifier |> List.append [ "static" ] else ()
    | _ -> ()
    modifier
