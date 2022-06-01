namespace Tests

open Aardvark.Base
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

    module PixImage =

        let checkerboard (randomAlpha : bool) (format : Col.Format) (width : int) (height : int) =
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
                            let color = if randomAlpha then rnd.UniformC4d().ToC4b() else rnd.UniformC3d().ToC4b()
                            colors <- colors |> HashMap.add c color
                            color.[channel]
                ) |> ignore

            pi

        let compare (input : PixImage<'T>) (output : PixImage<'T>) =
            let channels = min input.ChannelCount output.ChannelCount

            for x in 0 .. output.Size.X - 1 do
                for y in 0 .. output.Size.Y - 1 do
                    for c in 0 .. channels - 1 do
                        let inputData = input.GetChannel(int64 c)
                        let outputData = output.GetChannel(int64 c)

                        let coord = V2i(x, y)

                        let ref =
                            if Vec.allGreaterOrEqual coord V2i.Zero && Vec.allSmaller coord input.Size then
                                inputData.[coord]
                            else
                                Unchecked.defaultof<'T>

                        let message =
                            let t = if c < 4 then "color" else "alpha"
                            $"PixImage {t} data mismatch at [{x}, {y}]"

                        Expect.equal outputData.[x, y] ref message

    module private Gen =

        let colorFormat =
            [
                Col.Format.RGBA
                Col.Format.BGRA
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
            ]
            |> Gen.elements

        let checkerboardPix (randomAlpha : bool) (format : Col.Format) =
            gen {
                let! w = Gen.choose (64, 513)
                let! h = Gen.choose (64, 513)
                return PixImage.checkerboard randomAlpha format w h
            }

    type SaveLoadInput =
        {
            Image       : PixImage<byte>
            SaveParams  : PixSaveParams
            UseStream   : bool
        }

    type Generator private () =

        static member PixImage =
            gen {
                let! format = Gen.colorFormat
                return! Gen.checkerboardPix true format
            }
            |> Arb.fromGen

        static member SaveLoadInput =
            gen {
                let! cf = Gen.colorFormat
                let! iff = Gen.imageFileFormat
                let! pix = Gen.checkerboardPix (iff <> PixFileFormat.Jpeg) cf
                let! useStream = Gen.elements [false; true]

                let saveParams =
                    match iff with
                    | PixFileFormat.Jpeg -> PixJpegSaveParams(quality = 100) :> PixSaveParams
                    | fmt -> PixSaveParams fmt

                return {
                    Image = pix
                    SaveParams = saveParams
                    UseStream = useStream
                }
            }
            |> Arb.fromGen


    [<SetUp>]
    let setup() =
        Aardvark.Init()
        Report.Verbosity <- 3


    [<Property(Arbitrary = [| typeof<Generator> |])>]
    let ``[FreeImage] Save and load`` (input : SaveLoadInput) =
        printfn "size = %A, color format = %A, file format = %A, use stream = %b"
                input.Image.Size input.Image.Format input.SaveParams.Format input.UseStream

        tempFile (fun file ->
            let output =
                if input.UseStream then
                    use stream = File.Open(file, FileMode.Create, FileAccess.ReadWrite)
                    input.Image.Save(stream, input.SaveParams, PixImageFreeImage.Loader)

                    stream.Position <- 0L
                    PixImage<byte>(stream, PixImageFreeImage.Loader)
                else
                    input.Image.Save(file, input.SaveParams, false, PixImageFreeImage.Loader)
                    PixImage<byte>(file, PixImageFreeImage.Loader)

            match input.SaveParams.Format with
            | PixFileFormat.Jpeg | PixFileFormat.Gif ->
                let psnr = PixImage.peakSignalToNoiseRatio input.Image output
                Expect.isGreaterThan psnr 10.0 "Bad peak-signal-to-noise ratio"

            | _ ->
                PixImage.compare input.Image output
        )


    [<Property(Arbitrary = [| typeof<Generator> |])>]
    let ``[FreeImage] JPEG quality`` (pi : PixImage<byte>) =
        printfn "size = %A, format = %A" pi.Size pi.Format

        tempFile (fun file50 ->
            tempFile (fun file90 ->
                pi.SaveAsJpeg(file50, 50, false, PixImageFreeImage.Loader)
                pi.SaveAsJpeg(file90, 90, false, PixImageFreeImage.Loader)

                // check equal
                let pi50 = PixImage<uint8>(file50, PixImageFreeImage.Loader)
                let pi90 = PixImage<uint8>(file90, PixImageFreeImage.Loader)
                let psnr = PixImage.peakSignalToNoiseRatio pi50 pi90
                psnr |> should be (greaterThan 20.0)

                // check size
                let i50 = FileInfo(file50)
                let i90 = FileInfo(file90)
                i50.Length |> should be (lessThan i90.Length)
            )
        )


    [<Property(Arbitrary = [| typeof<Generator> |])>]
    let ``[FreeImage] PNG compression level`` (pi : PixImage<byte>) =
        printfn "size = %A, format = %A" pi.Size pi.Format

        tempFile (fun file0 ->
            tempFile (fun file9 ->
                pi.SaveAsPng(file0, 0, false, PixImageFreeImage.Loader)
                pi.SaveAsPng(file9, 6, false, PixImageFreeImage.Loader)

                // check equal
                let pi0 = PixImage<uint8>(file0, PixImageFreeImage.Loader)
                let pi9 = PixImage<uint8>(file9, PixImageFreeImage.Loader)
                PixImage.compare pi0 pi9

                // check size
                let i0 = FileInfo(file0)
                let i9 = FileInfo(file9)
                i0.Length |> should be (greaterThan i9.Length)
            )
        )