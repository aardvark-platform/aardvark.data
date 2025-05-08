namespace Aardvark.Data.Tests.PixLoader

open Aardvark.Base
open System
open Expecto

module EmbeddedResource =
    open System
    open System.Reflection
    open System.Text.RegularExpressions

    let get (path : string) =
        let asm = Assembly.GetExecutingAssembly()
        let name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase)
        let path = Regex.Replace(path, @"(\\|\/)", ".")
        let stream = asm.GetManifestResourceStream(name + "." + path)
        if stream <> null then stream
        else failwithf "Cannot open resource stream with name '%s'" path

    let loadPixImage<'T> (loader : IPixLoader) (path : string) =
        use stream = get path
        loader.LoadFromStream(stream).AsPixImage<'T>()

    let loadPixImageMipmap (loader : IPixLoader) (path : string) =
        let loader =
            match loader with
            | :? IPixMipmapLoader as l -> l
            | _ -> raise <| NotSupportedException()

        use stream = get path
        loader.LoadMipmapFromStream(stream)

module Rnd =
    let rng = RandomSystem(1)

    let uint8 : unit -> _  = rng.UniformUInt >> uint8
    let int8 : unit -> _   = rng.UniformInt >> int8
    let uint16 : unit -> _ = rng.UniformUInt >> uint16
    let int16 : unit -> _  = rng.UniformInt >> int16
    let uint32 : unit -> _ = rng.UniformUInt
    let int32 : unit -> _  = rng.UniformInt
    let uint64 : unit -> _ = rng.UniformULong
    let int64 : unit -> _  = rng.UniformLong
    let float16() = float16 (rng.UniformFloatClosed() * 100000.0f)
    let float32() = rng.UniformFloatClosed() * 100000.0f
    let float64() = rng.UniformDoubleClosed() * 100000.0

module PixImage =
    let private randomGeneric<'T> (getValue : unit -> 'T) (format : Col.Format) (size : V2i) =
        let pi = PixImage<'T>(format, size)
        for c in pi.ChannelArray do
            c.SetByIndex(ignore >> getValue) |> ignore
        pi

    let random =
        let table : Type -> (Col.Format -> V2i -> PixImage) =
            LookupTable.lookup [
                typeof<uint8>,   fun fmt size -> randomGeneric Rnd.uint8 fmt size
                typeof<int8>,    fun fmt size -> randomGeneric Rnd.int8 fmt size
                typeof<uint16>,  fun fmt size -> randomGeneric Rnd.uint16 fmt size
                typeof<int16>,   fun fmt size -> randomGeneric Rnd.int16 fmt size
                typeof<uint32>,  fun fmt size -> randomGeneric Rnd.uint32 fmt size
                typeof<int32>,   fun fmt size -> randomGeneric Rnd.int32 fmt size
                typeof<uint64>,  fun fmt size -> randomGeneric Rnd.uint64 fmt size
                typeof<int64>,   fun fmt size -> randomGeneric Rnd.int64 fmt size
                typeof<Half>,    fun fmt size -> randomGeneric (Rnd.float32 >> Half.op_Explicit) fmt size
                typeof<float16>, fun fmt size -> randomGeneric Rnd.float16 fmt size
                typeof<float32>, fun fmt size -> randomGeneric Rnd.float32 fmt size
                typeof<float>,   fun fmt size -> randomGeneric Rnd.float64 fmt size
            ]

        fun (typ: Type) -> table typ

    let isColor (color : 'T[]) (pi : PixImage<'T>) =
        for c in 0 .. pi.ChannelCount - 1 do
            let data = pi.GetChannel(int64 c)

            for x in 0 .. pi.Size.X - 1 do
                for y in 0 .. pi.Size.Y - 1 do
                    Expect.equal data.[x, y] color.[c] "PixImage data mismatch"

    let equal (input : PixImage) (output : PixImage) =
        if input.Size <> output.Size || input.ChannelCount <> output.ChannelCount || input.PixFormat.Type <> output.PixFormat.Type then
            false
        else
            input.Visit(
                { new IPixImageVisitor<bool> with
                    member x.Visit (input: PixImage<'TData>) =
                        let mutable result = true
                        let output = output :?> PixImage<'TData>

                        for x in 0 .. output.Size.X - 1 do
                            for y in 0 .. output.Size.Y - 1 do
                                for c in 0 .. output.ChannelCount - 1 do
                                    let inputData = input.GetChannel(int64 c)
                                    let outputData = output.GetChannel(int64 c)
                                    result <- result && Unchecked.equals outputData.[x, y] inputData.[x, y]

                        result
                }
            )
module File =
    open System.IO
    let rec private getRandomName() =
        let name = Path.GetRandomFileName()
        if File.Exists name || Directory.Exists name then getRandomName()
        else name

    let temp (action: string -> 'T) =
        let name = getRandomName()
        try
            action name
        finally
            if File.Exists name then File.Delete(name)