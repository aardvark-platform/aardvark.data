namespace Aardvark.Data.Tests.PixLoader

open Aardvark.Base
open Aardvark.Data
open System.IO
open System

open NUnit.Framework

module TiffTests = 

    type ImageResult =
        | Fails of exn
        | NotEqual
        | CorrectFormat of PixFormat
        | WrongFormat of is : PixFormat * should : PixFormat

    module Format =
        let prettyPrint (s : Type) =
            if s = typeof<byte> then "u8"
            elif s = typeof<UInt16> then "u16"
            elif s = typeof<UInt32> then "u32"
            elif s = typeof<Single> then "f32"
            elif s = typeof<Double> then "f64"
            else s.ToString()

    module ImageResult =
        let toReadableOk (pad : string -> string) (i : ImageResult) =
            match i with
            | Fails _         -> pad "FAIL"
            | NotEqual        -> pad "NOT EQUAL"
            | CorrectFormat _ -> pad "\u2713"
            | WrongFormat(is,should) -> 
                if is.Type <> should.Type then pad $"WT ({Format.prettyPrint is.Type}, {Format.prettyPrint should.Type})"
                else
                    pad $"WF ({is.Format}, {should.Format})"

    let loaders : IPixLoader list =
        [
            PixImageSharp.Loader
            PixImageDevil.Loader
            PixImageFreeImage.Loader
            PixImageWindowsMedia.Loader
        ]

    let roundTripTest (image: PixImage) =
        let results =
            loaders |> List.map (fun loader ->
                let result =
                    try
                        File.temp (fun path ->
                            image.Save(path, PixFileFormat.Tiff, false, loader)
                            let loaded = PixImage.Load(path, loader)

                            if loaded.PixFormat.Type <> image.PixFormat.Type then
                                WrongFormat (loaded.PixFormat, image.PixFormat)
                            else
                                if PixImage.equal image loaded then
                                    CorrectFormat image.PixFormat
                                else
                                    NotEqual
                        )
                    with exn ->
                        Fails exn

                ImageResult.toReadableOk _.PadLeft(25) result
            )
            |> String.concat " | "

        let name = $"{image.PixFormat.Type} ({image.PixFormat.Format})"
        printfn $"| {name.PadRight(40)} | {results} |"

    [<OneTimeSetUp>]
    let init() =
        Aardvark.Init()

    [<Test>]
    let ``[PixImage] Save and Load TIFFs``() =

        let getRandomImage =
            let rnd = RandomSystem()

            fun (typ: Type) (format: Col.Format) ->
                let size = V2i(rnd.UniformV2d() * 100.0) + 10
                PixImage.random typ format size

        let formats = [
            Col.Format.Gray
            Col.Format.RGB
            Col.Format.RGBA
            Col.Format.BGR
            Col.Format.BGRA
        ]

        let types = [
            typeof<uint8>
            typeof<int8>
            typeof<uint16>
            typeof<int16>
            typeof<uint32>
            typeof<int32>
            typeof<float32>
            typeof<float>
        ]

        let images =
            List.allPairs types formats
            |> List.map (fun (typ, fmt) -> getRandomImage typ fmt)

        let loaderNames = loaders |> List.map (_.Name >> _.PadLeft(25)) |> String.concat " | "
        let dividerNames = loaders |> List.map (fun _ -> String.replicate 25 "-") |> String.concat " | "
        let header = $"""| {"Input".PadRight(40)} | {loaderNames} |"""
        let divider = $"""| {String.replicate 40 "-"} | {dividerNames} |"""
        printfn $"{header}"
        printfn $"{divider}"

        for img in images do
            roundTripTest img

    [<Test>]
    let ``[PixImage] Load TIFFs``() =
        let tests = 
            [ 
                "test_greyscale_16.tiff",    PixFormat.UShortGray
                "test_16.tiff",              PixFormat.UShortRGB
                "test_16_lzw.tiff",          PixFormat.UShortRGB
                "test_32.tiff",              PixFormat.UIntRGB 
                "aardvark_rgba_8_none.tiff", PixFormat.ByteRGBA
                "aardvark_rgba_8_zip.tiff",  PixFormat.ByteRGBA
                "aardvark_rgba_8_lzw.tiff",  PixFormat.ByteRGBA
                "aardvark_rgba_16_zip.tiff", PixFormat.UShortRGBA 
                "aardvark_rgba_32_lzw.tiff", PixFormat.UIntRGBA   
                "test_uncomp_rgba_32f.tiff", PixFormat.UIntRGB
            ]

        let orderIgnoringFormatCmp (a : PixFormat) (b : PixFormat) =
            if a.Type <> b.Type then false
            else
                if a = b then true
                else
                    a.Format = Col.Format.RGB && b.Format = Col.Format.BGR ||
                    a.Format = Col.Format.RGBA && b.Format = Col.Format.BGRA

        let load (loader : IPixLoader) requiredFormat (path : string) =
            try
                let pis = PixImage.Load(path, loader)
                if orderIgnoringFormatCmp pis.PixFormat requiredFormat || orderIgnoringFormatCmp requiredFormat pis.PixFormat then 
                    CorrectFormat pis.PixFormat
                else
                    WrongFormat(pis.PixFormat, requiredFormat)
            with e -> 
                Fails e

        let filePadding = 40
        let padResult (s : string) = s.PadLeft(20)
        let prettyPrint (results : list<ImageResult>) =
            results |> List.map (ImageResult.toReadableOk padResult) |> String.concat " | "

        let results =
            tests |> List.choose (fun (file, expectedFormat) -> 
                let path = Path.Combine(__SOURCE_DIRECTORY__, "data", "tiff-test-data", file)
                if File.Exists path then
                    let results = 
                        loaders |> List.map (fun loader ->
                            loader.Name, load loader expectedFormat path
                        )
                    let r = results |> List.map snd |> prettyPrint 
                    Some $"{file.PadRight(filePadding)} | {r} |"
                else
                    None
            )
        
        let file = "file".PadRight(filePadding)
        let loaderHeaders = loaders |> List.map (_.Name >> padResult) |> String.concat " | "
        Console.WriteLine("WF means wrong Format, FAIL means there was an exception, OK means Format and Type matches while channel order is ignored.")
        Console.WriteLine($"{file} | {loaderHeaders} |")
        Console.Write(results |> String.concat Environment.NewLine)
                    