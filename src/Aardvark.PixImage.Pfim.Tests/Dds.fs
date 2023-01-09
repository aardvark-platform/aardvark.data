namespace Tests

open Aardvark.Base

open FsUnit
open Expecto

module DdsTests =

    module Cases =

        let private testMipmapped(format : Col.Format) (colors : 'T[][]) (file : string) =
            let mip = EmbeddedResource.loadPixImageMipmap file
            let pi = mip.ImageArray |> Array.map (fun pi -> pi.AsPixImage<'T>())

            for i = 0 to pi.Length - 1 do
                pi.[i].Format |> should equal format
                pi.[i].Size |> should equal (Fun.MipmapLevelSize(V2i(8, 8), i))
                pi.[i] |> PixImage.isColor colors.[i]

        module private MipmapColors =

            let rgba8 =
                [|
                    [| 0; 29; 72; 149 |]
                    [| 188; 0; 19; 160 |]
                    [| 19; 173; 0; 202 |]
                    [| 173; 163; 0; 202 |]
                |]
                |> Array.map (Array.map uint8)

            let r3g3b2 =
                [|
                    [| 0; 36; 85 |]
                    [| 182; 0; 0 |]
                    [| 36; 182; 0 |]
                    [| 182; 145; 0 |]
                |]
                |> Array.map (Array.map uint8)

            let r5g6b5 =
                [|
                    [| 0; 28; 74 |]
                    [| 189; 0; 16 |]
                    [| 16; 174; 0 |]
                    [| 172; 161; 0 |]
                |]
                |> Array.map (Array.map uint8)

            let rgb5a1 =
                [|
                    [| 0; 32; 74; 255 |]
                    [| 189; 0; 16; 255 |]
                    [| 16; 172; 0; 255 |]
                    [| 172; 164; 0; 255 |]
                |]
                |> Array.map (Array.map uint8)

            let rgba4 =
                [|
                    [| 0; 34; 68; 153 |]
                    [| 187; 0; 17; 153 |]
                    [| 17; 170; 0; 204 |]
                    [| 170; 170; 0; 204 |]
                |]
                |> Array.map (Array.map uint8)

            let l8a8 =
                [|
                    [| 26; 149 |]
                    [| 41; 160 |]
                    [| 127; 202 |]
                    [| 152; 202 |]
                |]
                |> Array.map (Array.map uint8)

            let a8 =
                [|
                    [| 149 |]
                    [| 160 |]
                    [| 202 |]
                    [| 202 |]
                |]
                |> Array.map (Array.map uint8)

        let rgb8Mipmapped() =
            testMipmapped Col.Format.BGR MipmapColors.rgba8 "data/mipmap-rgb8.dds"

        let rgba8Mipmapped() =
            testMipmapped Col.Format.BGRA MipmapColors.rgba8 "data/mipmap-rgba8.dds"

        let bgr8Mipmapped() =
            testMipmapped Col.Format.BGR MipmapColors.rgba8 "data/mipmap-bgr8.dds"

        let abgr8Mipmapped() =
            testMipmapped Col.Format.BGRA MipmapColors.rgba8 "data/mipmap-abgr8.dds"

        let r3g3b2Mipmapped() =
            testMipmapped Col.Format.BGR MipmapColors.r3g3b2 "data/mipmap-r3g3b2.dds"

        let r5g6b5Mipmapped() =
            testMipmapped Col.Format.BGR MipmapColors.r5g6b5 "data/mipmap-r5g6b5.dds"

        let rgb5a1Mipmapped() =
            testMipmapped Col.Format.BGRA MipmapColors.rgb5a1 "data/mipmap-rgb5a1.dds"

        let rgba4Mipmapped() =
            testMipmapped Col.Format.BGRA MipmapColors.rgba4 "data/mipmap-rgba4.dds"

        let l8a8Mipmapped() =
            testMipmapped Col.Format.GrayAlpha MipmapColors.l8a8 "data/mipmap-l8a8.dds"

        let l8Mipmapped() =
            testMipmapped Col.Format.Gray MipmapColors.l8a8 "data/mipmap-l8.dds"

        let a8Mipmapped() =
            testMipmapped Col.Format.Alpha MipmapColors.a8 "data/mipmap-a8.dds"

        let uncompressed32bit() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/32-bit-uncompressed.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 127uy; 0uy; 0uy; 255uy |]

        let uncompressed32bitOdd() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/32-bit-uncompressed-odd.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(5, 9))
            pi |> PixImage.isColor [| 128uy; 0uy; 0uy; 255uy |]

        let uncompressed24bitOdd() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/24-bit-uncompressed-odd.dds"
            pi.Format |> should equal Col.Format.BGR
            pi.Size |> should equal (V2i(1, 3))
            pi |> PixImage.isColor [| 128uy; 0uy; 0uy |]

        let compressedDxt1() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/dxt1-simple.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 128uy; 0uy; 0uy; 255uy |]

        let compressedDxt1AlphaMipmapped() =
            let pi = EmbeddedResource.loadPixImageMipmap "data/dxt1-alpha.dds"
            pi.PixFormat.Format |> should equal Col.Format.BGRA

            let expectedSizes =
                [|
                    V2i(128, 64)
                    V2i(64, 32)
                    V2i(32, 16)
                    V2i(16, 8)
                    V2i(8, 4)
                    V2i(4, 2)
                    V2i(2, 1)
                    V2i(1, 1)
                |]

            pi.ImageCount |> should equal expectedSizes.Length

            for i = 0 to expectedSizes.Length - 1 do
                pi.ImageArray.[i].Size |> should equal expectedSizes.[i]

            pi.ImageArray.[0].AsPixImage<uint8>().GetMatrix<C4b>().[69, 33] |> should equal (C4b(16, 186, 58))
            pi.ImageArray.[1].AsPixImage<uint8>().GetMatrix<C4b>().[28, 2] |> should equal (C4b(0, 77, 90))

        let compressedDxt3() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/dxt3-simple.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 129uy; 0uy; 0uy; 255uy |]

        let compressedDxt5() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/dxt5-simple.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 129uy; 0uy; 0uy; 255uy |]

        let compressedDxt5_1x1() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/dxt5-simple-1x1.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(1, 1))
            pi |> PixImage.isColor [| 129uy; 0uy; 0uy; 255uy |]

        let compressedDxt5Odd() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/dxt5-simple-odd.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(5, 9))
            pi |> PixImage.isColor [| 129uy; 0uy; 0uy; 255uy |]

        let compressedBc1() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/BC1_UNORM_SRGB-47.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(256, 256))
            pi |> PixImage.isColor [| 47uy; 47uy; 47uy; 255uy |]

        let compressedBc2() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc2-simple-srgb.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 189uy; 186uy; 255uy; 255uy |]

        let compressedBc3() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc3-simple-srgb.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 189uy; 186uy; 255uy; 255uy |]

        let compressedBc4() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc4-simple.dds"
            pi.Format |> should equal Col.Format.Gray
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 128uy; 128uy; 128uy; 128uy |]

        let compressedBc5u() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc5-simple.dds"
            pi.Format |> should equal Col.Format.BGR
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 128uy; 128uy; 0uy; |]

        // These tests are weird and broken?
        // Color doesn't match what NVIDIA texture tools or GIMP reports?
        let compressedBc5s() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc5-simple-snorm.dds"
            pi.Format |> should equal Col.Format.BGR
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 128uy; 64uy; 0uy; |]

        let compressedBc6h() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc6h-simple.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 128uy; 128uy; 255uy; 255uy |]

        let compressedBc7() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/bc7-simple.dds"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 129uy; 128uy; 255uy; 255uy |]

    [<Tests>]
    let tests =
        testList "PixLoader" [
            testCase "Dds.RGB8 mipmapped"                   Cases.rgb8Mipmapped
            testCase "Dds.RGBA8 mipmapped"                  Cases.rgba8Mipmapped

            // Broken: Pfim throws "Do not know how to swap Rgb24"
            //testCase "Dds.BGR8 mipmapped"                   Cases.bgr8Mipmapped

            testCase "Dds.ABGR8 mipmapped"                  Cases.abgr8Mipmapped

            // Broken: Pfim parses as grayscale
            // testCase "Dds.R3G3B2 mipmapped"                 Cases.r3g3b2Mipmapped

            // TODO: Implement us!
            //testCase "Dds.R5G6B5 mipmapped"                 Cases.r5g6b5Mipmapped
            //testCase "Dds.RGB5A1 mipmapped"                 Cases.rgb5a1Mipmapped
            //testCase "Dds.RGBA4 mipmapped"                  Cases.rgba4Mipmapped

            testCase "Dds.L8 mipmapped"                     Cases.l8Mipmapped

            // Broken: Pfim parses as r5g5b5a1
            //testCase "Dds.L8A8 mipmapped"                   Cases.l8a8Mipmapped

            // Broken: Pfim parses as grayscale
            //testCase "Dds.A8 mipmapped"                     Cases.a8Mipmapped

            testCase "Dds.Compressed DXT1"                  Cases.compressedDxt1
            testCase "Dds.Compressed DXT1 alpha mipmapped"  Cases.compressedDxt1AlphaMipmapped
            testCase "Dds.Compressed DXT3"                  Cases.compressedDxt3
            testCase "Dds.Compressed DXT5"                  Cases.compressedDxt5
            testCase "Dds.Compressed DXT5 1x1"              Cases.compressedDxt5_1x1
            testCase "Dds.Compressed DXT5 odd"              Cases.compressedDxt5Odd
            testCase "Dds.Compressed BC1"                   Cases.compressedBc1
            testCase "Dds.Compressed BC2"                   Cases.compressedBc2
            testCase "Dds.Compressed BC3"                   Cases.compressedBc3
            testCase "Dds.Compressed BC4"                   Cases.compressedBc4
            testCase "Dds.Compressed BC5u"                  Cases.compressedBc5u
            testCase "Dds.Compressed BC5s"                  Cases.compressedBc5s
            testCase "Dds.Compressed BC6h"                  Cases.compressedBc6h
            testCase "Dds.Compressed BC7"                   Cases.compressedBc7
        ]