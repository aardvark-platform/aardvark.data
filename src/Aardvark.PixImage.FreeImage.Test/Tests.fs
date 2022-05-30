namespace Aardvark.PixImage.FreeImage.Tests

open Aardvark.Base
open System
open System.IO

open NUnit.Framework
open FsUnit
open FsCheck
open FsCheck.NUnit
open Expecto

open FSharp.Data.Adaptive

module PixLoaderTests =

    let private rnd = RandomSystem(1)

    let private tempFile (f : string -> 'T) =
        let filename = Path.GetTempFileName()

        try
            f filename
        finally
            if File.Exists filename then
                File.Delete filename

    module private PixImage =

        let private desktopPath =
            Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)

        let checkerboard (format : Col.Format) (width : int) (height : int) =
            let mutable colors = HashMap.empty<V2l, C4b>

            let pi = PixImage<byte>(format, V2i(width, height))

            for channel = 0 to pi.ChannelCount - 1 do
                pi.GetChannel(int64 channel).SetByCoord(fun (c : V2l) ->
                    let c = c / 11L
                    if (c.X + c.Y) % 2L = 0L then
                        255uy
                    else
                        match colors |> HashMap.tryFind c with
                        | Some c -> c.[channel]
                        | _ ->
                            let color = rnd.UniformC4d().ToC4b()
                            colors <- colors |> HashMap.add c color
                            color.[channel]
                ) |> ignore

            pi

    module private Gen =

        let colorFormat =
            [
                //Col.Format.RGBA
                //Col.Format.BGRA
                Col.Format.RGB
                Col.Format.BGR
            ]
            |> Gen.elements

        let imageFileFormat =
            [
                PixFileFormat.Png
                PixFileFormat.Tiff
                PixFileFormat.Bmp
                PixFileFormat.Jpeg
                PixFileFormat.Gif
            ]
            |> Gen.elements

        let checkerboardPix (format : Col.Format) =
            gen {
                let! w = Gen.choose (64, 513)
                let! h = Gen.choose (64, 513)
                return PixImage.checkerboard format w h
            }

    type Generator private () =

        static member PixImage =
            gen {
                let! format = Gen.colorFormat
                return! Gen.checkerboardPix format
            }
            |> Arb.fromGen

        static member FileFormat =
            Gen.imageFileFormat
            |> Arb.fromGen


    [<SetUp>]
    let setup() =
        Aardvark.Init()
        Report.Verbosity <- 3

    [<Property(Arbitrary = [| typeof<Generator> |])>]
    let ``[FreeImage] Save`` (pi : PixImage<byte>) (format : PixFileFormat) =
        printfn "size = %A, format = %A, file = %A" pi.Size pi.Format format

        tempFile (fun file ->
            use stream = File.OpenWrite(file)
            PixImageFreeImage.SaveAsImageFreeImage(pi, stream, format, PixSaveOptions.Default, 100) |> should equal true
        )