namespace Aardvark.Data.Opc

open System.Xml
open System.Xml.Linq
open Aardvark.Base

module internal XmlHelpers =

    let xname str = XName.op_Implicit str
    let xattr (elem: XElement) sname = elem.Attribute(xname sname).Value
    let elem name (v : XElement) =
        match v.Elements(xname name) |> Seq.toList with
            | []-> failwithf "could not get value: %s" name
            | [x] -> x
            | _ -> failwithf "could not get value (not unique): %s" name
    let elem' name (v : XElement) =
        match v.Elements(xname name) |> Seq.toList with
            | []-> None
            | [x] -> Some x
            | _ -> None
    let elems name (p : XElement) =
        p.Elements (xname name)

    let children name (p : XElement) =
        p.Descendants (xname name)

    let xvalue (p : XElement) = p.Value.Trim()

    let prop name (p : XElement) =
        (elem name p).Value.Trim()


// in the old namespace to enable deserialization in (pickler) caches without hurdles.
namespace Aardvark.SceneGraph.Opc

open Aardvark.Base

type Texture = { fileName : string; weights : string }

[<Struct; RequireQualifiedAccess>]
type VertexOrder = RowMajor | ColumnMajor

[<Struct; RequireQualifiedAccess>]
type GeometryType = QuadList

type DiffuseColorCoordinates = string

type PatchFileInfo =
    {
        Name                : string
        TagList             : list<string>
        GeometryType        : GeometryType
        QuadVertexSortOrder : VertexOrder

        Local2Global        : Trafo3d
        GlobalBoundingBox   : Box3d
        LocalBoundingBox    : Box3d

        Local2Global2d      : Trafo3d
        GlobalBoundingBox2d : Box3d
        LocalBoundingBox2d  : Box3d

        Positions           : string
        Positions2d         : option<string>
        Normals             : string
        Offsets             : string

        Coordinates         : list<DiffuseColorCoordinates>
        Textures            : list<Texture>
        Attributes          : list<string>
    }

namespace Aardvark.Data.Opc

open System.Xml
open System.Xml.Linq
open Aardvark.Base
open Aardvark.SceneGraph.Opc

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PatchFileInfo =

    [<AutoOpen>]
    module private Parsers =
        let majority = function
            | "RowMajor" -> VertexOrder.RowMajor
            | "ColumnMajor" -> VertexOrder.ColumnMajor
            | s -> failwithf "could not parse majority: %s" s

        let geomtryType = function
            | "QuadList" -> GeometryType.QuadList
            | s -> failwithf "could not parse geometry type: %s" s

    let node (name : string) (node : XmlNode) =
        node.SelectSingleNode(name)

    let nodes (name : string) (node : XmlNode) =
        node.SelectNodes(name) |> Seq.cast<XmlNode>

    let childNodes (name : string) (node : XmlNode) =
        node.SelectSingleNode(name).ChildNodes |> Seq.cast<XmlNode>

    let childNodes' (name : string) (node : XmlNode) =
        let n = node.SelectSingleNode(name)
        if (n = null) then
            [] |> Seq.cast<XmlNode>
        else
            n.ChildNodes |> Seq.cast<XmlNode>

    let get (name : string) (node : XmlNode)=
        node.SelectSingleNode(name).InnerText.Trim()

    let tryGet (name : string) (node : XmlNode)=
        let n = node.SelectSingleNode(name)
        if (n = null) then
            None
        else
            Some (n.InnerText.Trim())

    let xvalue (p : XmlNode) = p.Value.Trim()

    let inner (node : XmlNode) =
        node.InnerText.Trim()

    let private ofXDoc (doc : XmlDocument) (name : string) (hasDeviations : bool) =

        let trafo (m : M44d) =
            Trafo3d(m, m.Inverse)

        let mkTexture t =
            let name = (inner t).Replace('\\',System.IO.Path.DirectorySeparatorChar)
            { fileName = name; weights = "" }

        let patch =
            doc.SelectSingleNode "/Aardvark"
            |> Seq.cast<XmlNode>
            |> Seq.head

        let textures =
            patch
              |> childNodes "Textures"
              |> Seq.map mkTexture
              |> Seq.toList

        let coords =
            patch
              |> nodes "Coordinates"
              |> Seq.toList
              |> List.map (string << get "DiffuseColor1Coordinates")

        let attributes =
          //if hasDeviations then
            patch
              |> childNodes' "Attributes"
              |> Seq.map inner
              |> Seq.toList

        let pos2d = patch |> tryGet "Positions2D"
        let attributes =
            match pos2d with
            | Some _ ->  "Positions2d.aara" :: attributes
            | None   -> attributes

        let local2Global2d = patch |> tryGet "Local2Global2D"
        let local2Global2d =
            match local2Global2d with
            | Some x -> x |> M44d.Parse |> trafo
            | None -> Trafo3d.Identity

        let globalBoundingBox2d = patch |> tryGet "GlobalBoundingBox2D"
        let globalBoundingBox2d =
            match globalBoundingBox2d with
            | Some x -> x |> Box3d.Parse
            | None -> Box3d.Invalid

        let localBoundingBox2d = patch |> tryGet "LocalBoundingBox2d"
        let localBoundingBox2d =
            match localBoundingBox2d with
            | Some x -> x |> Box3d.Parse
            | None -> Box3d.Invalid

        let split (s:string) =
            (s.Split ' ') |> Array.toList

        {
            Name                = name
            TagList             = patch |> get "TagList" |> split

            GeometryType        = patch |> get "GeometryType"           |> geomtryType
            QuadVertexSortOrder = patch |> tryGet "QuadVertexSortOrder" |> Option.map majority |> Option.defaultValue VertexOrder.ColumnMajor

            Local2Global        = patch |> get "Local2Global"           |> M44d.Parse |> trafo
            GlobalBoundingBox   = patch |> get "GlobalBoundingBox"      |> Box3d.Parse
            LocalBoundingBox    = patch |> get "LocalBoundingBox"       |> Box3d.Parse

            Local2Global2d      = local2Global2d
            GlobalBoundingBox2d = globalBoundingBox2d
            LocalBoundingBox2d  = localBoundingBox2d

            Positions           = patch |> get "Positions"
            Positions2d         = pos2d
            Normals             = "" //patch |> get "Normals"
            Offsets             = ""
            Textures            = textures
            Coordinates         = coords
            Attributes          = attributes
        }

    let load (opcPaths : OpcPaths) (patchName : string) =
        let path = opcPaths.Patches_DirAbsPath +/ patchName +/ OpcPaths.PatchFileInfo_FileName
        let doc = Prinziple.readXmlDoc path
        ofXDoc doc patchName false

    /// <summary>
    /// Loads patchfileinfo with positions2d.aara as attribute
    /// </summary>
    let load' (opcPaths : OpcPaths) (patchName : string) =
        let path = opcPaths.Patches_DirAbsPath +/ patchName +/ OpcPaths.PatchFileInfo_FileName
        let doc = Prinziple.readXmlDoc path
        ofXDoc doc patchName true

namespace Aardvark.SceneGraph.Opc

// in the old namespace to enable deserialization in (pickler) caches without hurdles.
[<RequireQualifiedAccess>]
type QTree<'a> =
    | Node of 'a * QTree<'a>[]
    | Leaf of 'a

namespace Aardvark.Data.Opc

open Aardvark.SceneGraph.Opc

type QTree<'a> = Aardvark.SceneGraph.Opc.QTree<'a>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module QTree =
    let rec map (f : 'T -> 'U) (tree : QTree<'T>) =
        match tree with
        | QTree.Leaf v -> QTree.Leaf (f v)
        | QTree.Node(v, children) ->
            QTree.Node(f v, children |> Array.map (map f))

    let mapLevel (f : int -> 'T -> 'U) (tree : QTree<'T>) =
        let rec mapLevel lvl f t =
            match t with
            | QTree.Leaf v -> QTree.Leaf (f lvl v)
            | QTree.Node(v, children) ->
                QTree.Node(f lvl v, children |> Array.map (mapLevel (lvl + 1) f))

        mapLevel 0 f tree

    let rec getLeaves (tree : QTree<'T>) =
        match tree with
        | QTree.Leaf v -> Seq.singleton v
        | QTree.Node(v, children) -> children |> Seq.collect getLeaves

    let getRoot (tree : QTree<'T>) =
        match tree with
        | QTree.Leaf v -> v
        | QTree.Node(v, _) -> v

    let rec flatten (tree : QTree<'T>) =
        match tree with
        | QTree.Leaf v -> [|v|]
        | QTree.Node(v, children) ->
            let cs = children |> Array.map flatten |> Array.concat
            Array.append [|v|] cs

    let height (tree : QTree<'T>) =
        let rec height lvl tree =
            match tree with
            | QTree.Leaf _ -> lvl
            | QTree.Node (_, children) ->
                children
                |> Array.map (height (lvl+1))
                |> Array.max

        height 0 tree