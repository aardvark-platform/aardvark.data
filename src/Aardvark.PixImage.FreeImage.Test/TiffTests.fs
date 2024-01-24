namespace Tests

open Aardvark.Base
open System.IO
open System
open System.IO
open System.IO.Compression

open NUnit.Framework
open FsUnit
open FsCheck
open FsCheck.NUnit
open Expecto

open FSharp.Data.Adaptive


module TiffTests = 

    type ImageResult =
        | Fails of exn
        | CorrectFormat of PixFormat
        | WrongFormat of is : PixFormat * should : PixFormat

    module Format =
        let prettyPrint (s : Type) =
            if s = typeof<byte> then "b"
            elif s = typeof<UInt16> then "u16"
            elif s = typeof<UInt32> then "u32"
            elif s = typeof<Single> then "f32"
            elif s = typeof<Double> then "f64"
            else s.ToString()

    module ImageResult =
        let toReadableOk (pad : string -> string) (i : ImageResult) =
            match i with
            | Fails _         -> pad "FAIL"
            | CorrectFormat _ -> pad "-OK-"
            | WrongFormat(is,should) -> 
                if is.Type <> should.Type then pad $"WT ({Format.prettyPrint is.Type}, {Format.prettyPrint should.Type})"
                else
                    pad $"WF- ({is.Format}, {should.Format})"

    [<Test>]
    let ``[PixImage] Load TIFFs``() =

        Aardvark.Init()

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

        let loaders : list<string * (string -> PixImage)> = 
            [
                "ImageSharp", PixImageSharp.Loader.LoadFromFile
                "DevIL", PixImageDevil.Loader.LoadFromFile
                "FreeImage", PixImageFreeImage.Loader.LoadFromFile
            ]

        let orderIgnoringFormatCmp (a : PixFormat) (b : PixFormat) =
            if a.Type <> b.Type then false
            else
                if a = b then true
                else
                    a.Format = Col.Format.RGB && b.Format = Col.Format.BGR ||
                    a.Format = Col.Format.RGBA && b.Format = Col.Format.BGRA

        let load (loader : string -> PixImage) requiredFormat path =
            try
                let pis = loader path
                if orderIgnoringFormatCmp pis.PixFormat requiredFormat || orderIgnoringFormatCmp requiredFormat pis.PixFormat then 
                    CorrectFormat pis.PixFormat
                else
                    WrongFormat(pis.PixFormat, requiredFormat)
            with e -> 
                Fails e

        let filePadding = 40
        let padResult (s : string) = s.PadLeft(15)
        let prettyPrint (results : list<ImageResult>) =
            results |> List.map (ImageResult.toReadableOk padResult) |> String.concat " | "

        let results =
            tests |> List.choose (fun (file, expectedFormat) -> 
                let path = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "data", "image-test-data", file)
                if File.Exists path then
                    let results = 
                        loaders |> List.map (fun (name, loader) -> 
                            name, load loader expectedFormat path
                        )
                    let r = results |> List.map snd |> prettyPrint 
                    Some $"{file.PadRight(filePadding)} | {r} |"
                else
                    None
            )
        
        let file = "file".PadRight(filePadding)
        let loaderHeaders = loaders |> List.map (fst >> padResult) |> String.concat " | "
        Console.WriteLine("WF means wrong Format, FAIL means there was an exception, OK means Format and Type matches while channel order is ignored.")
        Console.WriteLine($"{file} | {loaderHeaders} |")
        Console.Write(results |> String.concat Environment.NewLine)
                    