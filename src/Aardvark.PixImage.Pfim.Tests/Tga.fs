namespace Tests

open Aardvark.Base

open FsUnit
open Expecto

module TgaTests =

    module Cases =

        let true24() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/true-24.tga"
            pi.Format |> should equal Col.Format.BGR
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 0uy; 176uy; 255uy; |]

        let true24rle() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/true-24-rle.tga"
            pi.Format |> should equal Col.Format.BGR
            pi.Size |> should equal (V2i(16, 4))

            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 0), V2i(16, 2))) |> PixImage.isColor [| 255uy; 216uy; 0uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 2), V2i(16, 1))) |> PixImage.isColor [| 0uy; 148uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 3), V2i(8, 1)))  |> PixImage.isColor [| 76uy; 255uy; 0uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(8, 3), V2i(8, 1)))  |> PixImage.isColor [| 255uy; 0uy; 0uy |]

        let true32() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/true-32.tga"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(64, 64))
            pi |> PixImage.isColor [| 127uy; 0uy; 0uy; 255uy |]

        let true32rle() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/true-32-rle.tga"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(16, 4))
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 0), V2i(16, 2))) |> PixImage.isColor [| 255uy; 216uy; 0uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 2), V2i(16, 1))) |> PixImage.isColor [| 0uy; 148uy; 255uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 3), V2i(8, 1)))  |> PixImage.isColor [| 76uy; 255uy; 0uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(8, 3), V2i(8, 1)))  |> PixImage.isColor [| 255uy; 0uy; 0uy; 255uy |]

        let nonSquare() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/tiny-rect.tga"
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(20, 12))
            pi |> PixImage.isColor [| 255uy; 216uy; 0uy; 255uy |]

        let true32MixedEncoding() =
            let pi = EmbeddedResource.loadPixImage<uint8> "data/true-32-mixed.tga"
            let mat = pi.GetMatrix<C4b>()
            pi.Format |> should equal Col.Format.BGRA
            pi.Size |> should equal (V2i(16, 4))
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 0), V2i(16, 1))) |> PixImage.isColor [| 255uy; 216uy; 0uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 2), V2i(16, 1))) |> PixImage.isColor [| 0uy; 148uy; 255uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(0, 3), V2i(8, 1))) |> PixImage.isColor [| 76uy; 255uy; 0uy; 255uy |]
            pi.SubImage(Box2i.FromMinAndSize(V2i(8, 3), V2i(8, 1))) |> PixImage.isColor [| 255uy; 0uy; 0uy; 255uy |]
            mat.[0, 1] |> should equal (C4b(0, 0, 0, 255))
            mat.[1, 1] |> should equal (C4b(64, 64, 64, 255))
            mat.[2, 1] |> should equal (C4b(255, 0, 0, 255))
            mat.[3, 1] |> should equal (C4b(255, 106, 0, 255))
            mat.[4, 1] |> should equal (C4b(255, 216, 0, 255))
            mat.[5, 1] |> should equal (C4b(182, 255, 0, 255))
            mat.[6, 1] |> should equal (C4b(76, 255, 0, 255))
            mat.[7, 1] |> should equal (C4b(0, 255, 33, 255))
            mat.[8, 1] |> should equal (C4b(0, 255, 144, 255))
            mat.[9, 1] |> should equal (C4b(0, 255, 255, 255))
            mat.[10, 1] |> should equal (C4b(0, 148, 255, 255))
            mat.[11, 1] |> should equal (C4b(0, 38, 255, 255))
            mat.[12, 1] |> should equal (C4b(72, 0, 255, 255))
            mat.[13, 1] |> should equal (C4b(178, 0, 255, 255))
            mat.[14, 1] |> should equal (C4b(255, 0, 220, 255))
            mat.[15, 1] |> should equal (C4b(255, 0, 110, 255))

    [<Tests>]
    let tests =
        testList "PixLoader" [
            testCase "Tga.True 24bit"                   Cases.true24
            testCase "Tga.True 24bit RLE"               Cases.true24rle
            testCase "Tga.True 32bit"                   Cases.true32
            testCase "Tga.True 32bit mixed encoding"    Cases.true32MixedEncoding
            testCase "Tga.True 32bit RLE"               Cases.true32rle
            testCase "Tga.Non-square"                   Cases.nonSquare
        ]