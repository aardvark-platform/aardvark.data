﻿namespace Aardvark.Data.Opc

open System
open System.Buffers
open System.IO
open System.Text
open System.Runtime.InteropServices

open Aardvark.Base
open FSharp.Data.Adaptive

module Aara =

    /// Reads a length-prefixed (byte) string from the stream.
    let readString (encoding: Encoding) (s: Stream) =
        let count = s.ReadByte()
        let buffer = Array.zeroCreate<byte> count
        s.ReadBytes(buffer, count = count)
        encoding.GetString buffer

    let loadRaw<'T  when 'T : unmanaged> (elementCount : int) (f : Stream)  =
        let buffer = ArrayPool<byte>.Shared.Rent(1 <<< 22)

        try
            let result = Array.zeroCreate<'T> elementCount

            result |> NativePtr.pinArr (fun dst ->
                let mutable ptr = dst.Address
                let mutable remaining = sizeof<'T> * result.Length

                while remaining > 0 do
                    let s = f.Read(buffer, 0, buffer.Length)
                    Marshal.Copy(buffer, 0, ptr, s)
                    ptr <- ptr + nativeint s
                    remaining <- remaining - s
            )

            result

        finally
            ArrayPool<byte>.Shared.Return(buffer)

    let loadFromStream<'T when 'T : unmanaged> (f : Stream) =
        let binaryReader = new BinaryReader(f,Text.Encoding.ASCII, true)
        let typeName = readString Encoding.Default f
        let dimensions = f.ReadByte() |> int
        let sizes = [| for d in 0 .. dimensions - 1 do yield binaryReader.ReadInt32() |]

        let elementCount = sizes |> Array.fold ((*)) 1

        let result =
            if typeof<'T>.Name = typeName then
                loadRaw<'T> elementCount f
            else
                match typeName with
                | "V3d" -> f |> loadRaw<V3d> elementCount |> PrimitiveValueConverter.arrayConverter typeof<V3d>
                | "V3f" -> f |> loadRaw<V3f> elementCount |> PrimitiveValueConverter.arrayConverter typeof<V3f>
                | "V2d" -> f |> loadRaw<V2d> elementCount |> PrimitiveValueConverter.arrayConverter typeof<V2d>
                | "double" -> f |> loadRaw<double> elementCount |> PrimitiveValueConverter.arrayConverter typeof<double>
                | "float" -> f |> loadRaw<float32> elementCount |> PrimitiveValueConverter.arrayConverter typeof<float32>
                | _ -> failwith $"[Aara] No support for loading type {typeName}."

        let dim =
            match sizes with
            | [| x |] -> V3i(x,1,1)
            | [| x; y |] -> V3i(x,y,1)
            | [| x; y; z |] -> V3i(x,y,z)
            | _ -> failwith $"[Aara] Unsupported dimension: {sizes}."

        Volume<'T>(result, dim)

    let fromFile<'T when 'T : unmanaged> (path : string) =
        use fs = Prinziple.openRead path
        loadFromStream<'T> fs

    let createIndex (vi : Matrix<V3f>) =
        let dx = vi.Info.DX
        let dy = vi.Info.DY
        let dxy = dx + dy
        let mutable arr = Array.zeroCreate (int (vi.SX - 1L) * int (vi.SY - 1L) * 6)
        let mutable cnt = 0

        vi.SubMatrix(V2l.Zero,vi.Size-V2l.II).ForeachXYIndex(fun x y index ->
            let i00 = index
            let i10 = index + dy
            let i01 = index + dx
            let i11 = index + dxy

            arr.[cnt + 0] <- (int i00)
            arr.[cnt + 1] <- (int i10)
            arr.[cnt + 2] <- (int i11)
            arr.[cnt + 3] <- (int i00)
            arr.[cnt + 4] <- (int i11)
            arr.[cnt + 5] <- (int i01)
            cnt <- cnt + 6
        )
        Array.Resize(&arr, cnt)
        arr

    let createIndex2 (vi : Matrix<V3f>) (invalids : int64[])=

        let invalids = invalids |> Array.map (fun x -> (x, x)) |> HashMap.ofArray

        let dx = vi.Info.DX
        let dy = vi.Info.DY
        let dxy = dx + dy
        let mutable arr = Array.zeroCreate (int (vi.SX - 1L) * int (vi.SY - 1L) * 6)
        let mutable cnt = 0

        vi.SubMatrix(V2l.Zero,vi.Size-V2l.II).ForeachXYIndex(fun x y index ->

            let inv = invalids |> HashMap.tryFind index

            match inv with
                | Some _ ->
                    arr.[cnt + 0] <- 0
                    arr.[cnt + 1] <- 0
                    arr.[cnt + 2] <- 0
                    arr.[cnt + 3] <- 0
                    arr.[cnt + 4] <- 0
                    arr.[cnt + 5] <- 0
                    cnt <- cnt + 6
                | None ->
                    let i00 = index
                    let i10 = index + dy
                    let i01 = index + dx
                    let i11 = index + dxy

                    arr.[cnt + 0] <- (int i00)
                    arr.[cnt + 1] <- (int i10)
                    arr.[cnt + 2] <- (int i11)
                    arr.[cnt + 3] <- (int i00)
                    arr.[cnt + 4] <- (int i11)
                    arr.[cnt + 5] <- (int i01)
                    cnt <- cnt + 6
        )
        Array.Resize(&arr, cnt)
        arr

    // Patch Size to index array (faces with invalid points will be degenerated or skipped)
    let computeIndexArray (size : V2i) (degenerateInvalids : bool) (invalidPoints : int Set) =
        // vertex x/y to point index of face
        let getFaceIndices y x sizeX =
            let pntA = y * sizeX + x
            let pntB = (y + 1) * sizeX + x
            let pntC = pntA + 1
            let pntD = pntB + 1

            [| pntA; pntB; pntC;
               pntC; pntB; pntD |]

        // replace invalid faces with another array (invalidReplacement)
        let getFaceIndicesWReplacedInvalids invalidReplacement y x sizeX =
            let faceIndices = getFaceIndices y x sizeX
            if faceIndices |> Array.exists (fun i -> Set.contains i invalidPoints) then
                invalidReplacement
            else
                faceIndices

        // choose function to use
        let f =
            match (invalidPoints.IsEmptyOrNull(), degenerateInvalids) with
            | (true, _)      -> getFaceIndices
            // skip faces with invalid points
            | (false, false) -> getFaceIndicesWReplacedInvalids Array.empty
            // replace invalid faces with degenerated face
            | (false, true)  ->
                // find first valid point
                let p = [| 0..(size.X * size.Y - 1) |] |> Array.find (fun i -> not (Set.contains i invalidPoints))
                getFaceIndicesWReplacedInvalids [| p; p; p; p; p; p |]

        // step through all vertices to get index-array per face
        let indexArray =
            [|
                for y in [| 0..(size.Y-2) |] do
                for x in [| 0..(size.X-2) |] do
                    yield f y x size.X
            |]

        let invalidFaceCount = indexArray |> Array.filter (fun a -> a.IsEmpty()) |> Array.length
        if invalidFaceCount > 0 then
            Report.Line(5, "Invalid faces found: " + invalidFaceCount.ToString())

        indexArray |> Array.concat

    let getInvalidIndices (positions : V3d[]) =
        positions |> Array.mapi (fun i x -> if x.AnyNaN then Some i else None) |> Array.choose id

    // load triangles from aaraFile and transform them with matrix
    let loadTrianglesFromFile' (aaraFile : string) (indexComputation : V2i -> int[] -> int[]) (matrix : M44d) =
        let positions = aaraFile |> fromFile<V3f>

        let data =
            positions.Data |> Array.map (fun x ->  x.ToV3d() |> matrix.TransformPos)

        let invalidIndices = getInvalidIndices data
        let index = indexComputation (positions.Size.XY.ToV2i()) invalidIndices  //computeIndexArray (positions.Size.XY.ToV2i()) false (Set.ofArray invalidIndices)

        let triangles =
            index
                |> Seq.map(fun x -> data.[x])
                |> Seq.chunkBySize 3
                |> Seq.map(fun x -> Triangle3d(x))
                |> Seq.toArray

        triangles

    // load triangles from aaraFile and transform them with matrix
    let loadTrianglesFromFile (aaraFile : string) (matrix : M44d) =
        loadTrianglesFromFile' aaraFile (fun size invalids -> computeIndexArray size false (Set.ofArray invalids)) matrix