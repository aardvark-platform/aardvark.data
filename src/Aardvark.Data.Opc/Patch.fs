namespace Aardvark.Data.Opc

open System
open Aardvark.Base
open Aardvark.Rendering

[<Struct; RequireQualifiedAccess>]
type ViewerModality = XYZ | SvBR

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ViewerModality =

  let matchy threeD twoD (modality : ViewerModality) =
      match modality with
      | ViewerModality.XYZ  -> threeD
      | ViewerModality.SvBR -> twoD


namespace Aardvark.SceneGraph.Opc

// in the old namespace to enable deserialization in (pickler) caches without hurdles.
type Patch =
    {
        level           : int
        info            : PatchFileInfo
        triangleSize    : float
    }

namespace Aardvark.Data.Opc

open System
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph.Opc

type Patch = Aardvark.SceneGraph.Opc.Patch

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Patch =
    let ofInfo (level : int) (size: float) (p : PatchFileInfo) = { level = level; info = p; triangleSize = size }

    let load (opcPaths : OpcPaths) (mode : ViewerModality) (p : PatchFileInfo)  =
        let sw = System.Diagnostics.Stopwatch()
        sw.Start()
        let patch_DirAbsPath = opcPaths.Patches_DirAbsPath +/ p.Name

        let pos =
          match mode, p.Positions2d with
          | ViewerModality.SvBR, Some p2 -> p2
          | _ -> p.Positions

        let positions   = patch_DirAbsPath +/ pos |> Aara.fromFile<V3f>
        let coordinates = patch_DirAbsPath +/ (List.head p.Coordinates) |> Aara.fromFile<V2f>

        sw.Stop()

        let coordinates = coordinates.Data |> Array.map (fun v -> V2f(v.X, 1.0f-v.Y))

        let index = Aara.createIndex (positions.AsMatrix())

        let indexAttributes =
            let def = [
                DefaultSemantic.Positions, positions.Data :> Array
                DefaultSemantic.DiffuseColorCoordinates, coordinates :> Array
            ]

            def |> SymDict.ofList

        let geometry =
            IndexedGeometry(
                Mode              = IndexedGeometryMode.TriangleList,
                IndexArray        = index,
                IndexedAttributes = indexAttributes
            )

        geometry, sw.MicroTime

    let extractTexturePath (opcPaths : OpcPaths) (patchInfo : PatchFileInfo) (texNumber : int) =
        let t = patchInfo.Textures |> List.item texNumber
        let fn = t.fileName.Replace('\\',System.IO.Path.DirectorySeparatorChar)
        let sourcePath = opcPaths.Images_DirAbsPath +/ fn
        let extensions = [ ".dds"; ".tif"; ".tiff"]

        let rec tryFindTex exts path =
            match exts with
            | x::xs ->
                let current = System.IO.Path.ChangeExtension(path,x)
                if Prinziple.fileExists current then current else tryFindTex xs path

            | [] ->
                failwithf "texture not found: %s" path

        tryFindTex extensions sourcePath

