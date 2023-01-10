namespace Aardvark.Base

open System
open System.IO
open FSharp.NativeInterop
open Pfim

#nowarn "9"

[<AutoOpen>]
module private PixImagePfimImpl =

    type IImage with
        member x.TopLevel =
            MipMapOffset(x.Width, x.Height, x.Stride, 0, x.DataLen)

        member x.GetLevel(level : int) =
            if level = 0 then x.TopLevel
            else x.MipMaps.[level - 1]

        member x.Levels =
            if isNull x.MipMaps then 1
            else x.MipMaps.Length + 1

    [<AutoOpen>]
    module private ColorUtilities =

        module Bits =

            module Tables =

                let expand1 =
                    Array.init 2 (fun i -> uint8 (i * 255))

                let expand4 =
                    Array.init 16 (fun i -> uint8 (i * 17))

                let expand5 =
                    Array.init 32 (fun i -> uint8 ((i <<< 3) ||| (i >>> 2)))

                let expand6 =
                    Array.init 64 (fun i -> uint8 ((i <<< 2) ||| (i >>> 4)))

            let inline expand1 x =
                Tables.expand1.[int x]

            let inline expand4 x =
                Tables.expand4.[int x]

            let inline expand5 x =
                Tables.expand5.[int x]

            let inline expand6 x =
                Tables.expand6.[int x]

    // Note: The default allocator of Pfim just allocates a new array, so
    // we can just use it without worrying about it being reused when the original
    // IImage is disposed.
    let private of8Bit (format : Col.Format) (info : MipMapOffset) (src : uint8[]) : PixImage =
        let channels =
            format.ChannelCount()

        let volume =
            Volume<uint8>(
                src,
                VolumeInfo(
                    int64 info.DataOffset,
                    V3l(info.Width, info.Height, channels),
                    V3l(channels, info.Stride, 1)
                )
            )

        PixImage<uint8>(format, volume)

    let private of16Bit (format : Col.Format) (decode : uint16 -> C4b) (info : MipMapOffset) (src : uint8[]) : PixImage =
        if info.Stride % 2 <> 0 then
            failwith $"Stride is odd ({info.Stride})."

        let pi = PixImage<uint8>(format, int64 info.Width, int64 info.Height)

        pinned src (fun src ->
            let srcVolume =
                NativeVolume<uint16>(
                    NativePtr.ofNativeInt (src + nativeint info.DataOffset),
                    VolumeInfo(
                        0L,
                        V3l(info.Width, info.Height, 1),
                        V3l(1, info.Stride / 2, 1)
                    )
                )

            PixImage.pin pi (fun dst ->
                let dstVolume =
                    NativeVolume<uint8>(
                        dst.Pointer,
                        VolumeInfo(dst.Origin, dst.Size.XYI, dst.Delta)
                    )

                (srcVolume, dstVolume) ||> NativeVolume.iterPtr2 (fun _ src dst ->
                    let color = decode <| NativePtr.read src
                    NativePtr.set dst 0 color.B
                    NativePtr.set dst 1 color.G
                    NativePtr.set dst 2 color.R
                    NativePtr.set dst 3 color.A
                )
            )
        )

        pi

    let private ofRGBA16 =
        of16Bit Col.Format.BGRA (fun pixel ->
            let b = pixel &&& 0xFus
            let g = (pixel >>> 4) &&& 0xFus
            let r = (pixel >>> 8) &&& 0xFus
            let a = (pixel >>> 12) &&& 0xFus
            C4b(
                Bits.expand4 r,
                Bits.expand4 g,
                Bits.expand4 b,
                Bits.expand4 a
            )
        )

    let private ofR5G5B5A1 (withAlpha : bool) =
        of16Bit Col.Format.BGRA (fun pixel ->
            let b = pixel &&& 0x1Fus
            let g = (pixel >>> 5) &&& 0x1Fus
            let r = (pixel >>> 10) &&& 0x1Fus
            let a = (pixel >>> 15) &&& 0x1us
            C4b(
                Bits.expand5 r,
                Bits.expand5 g,
                Bits.expand5 b,
                if withAlpha then Bits.expand1 a else 255uy
            )
        )

    let private ofR5G6B5 =
        of16Bit Col.Format.BGR (fun pixel ->
            let b = pixel &&& 0x1Fus
            let g = (pixel >>> 5) &&& 0x3Fus
            let r = (pixel >>> 11) &&& 0x1Fus
            C4b(
                Bits.expand5 r,
                Bits.expand6 g,
                Bits.expand5 b
            )
        )

    let private levelToPixImageTable : (ImageFormat -> (MipMapOffset -> uint8[] -> PixImage) option) =
        LookupTable.lookupTable' [
            ImageFormat.Rgb8,     of8Bit Col.Format.Gray
            ImageFormat.Rgb24,    of8Bit Col.Format.BGR
            ImageFormat.Rgba32,   of8Bit Col.Format.BGRA
            ImageFormat.Rgba16,   ofRGBA16
            ImageFormat.R5g5b5,   ofR5G5B5A1 false
            ImageFormat.R5g6b5,   ofR5G6B5
            ImageFormat.R5g5b5a1, ofR5G5B5A1 true
        ]

    let private levelToPixImage (format : ImageFormat) =
        match levelToPixImageTable format with
        | Some pi -> pi
        | _ ->
            raise <| NotSupportedException($"Format {format} not supported.")

    let toPixImage (image : IImage) =
        levelToPixImage image.Format image.TopLevel image.Data

    let toPixImageMipmap (image : IImage) =
        let create = levelToPixImage image.Format

        let levels =
            Array.init image.Levels (fun i ->
                let level = image.GetLevel i
                create level image.Data
            )

        PixImageMipMap(levels)


[<AbstractClass; Sealed>]
type PixImagePfim private() =

    static let loader = PfimPixLoader() :> IPixLoader

    static member Loader = loader

    /// Installs the Pfim loader in PixImage.
    [<OnAardvarkInit>]
    static member Init() =
        PixImage.AddLoader(loader)

    /// Loads a PixImage from the given file.
    static member Load(filename : string) : PixImage =
        use image = Pfimage.FromFile filename
        toPixImage image

    /// Loads a PixImageMipMap from the given file.
    static member LoadWithMipmap(filename : string) : PixImageMipMap =
        use image = Pfimage.FromFile filename
        toPixImageMipmap image

    /// Loads a PixImage from the given stream.
    static member Load(stream : Stream) : PixImage =
        use image = Pfimage.FromStream stream
        toPixImage image

    /// Loads a PixImageMipMap from the given stream.
    static member LoadWithMipmap(stream : Stream) : PixImageMipMap =
        use image = Pfimage.FromStream stream
        toPixImageMipmap image


and private PfimPixLoader() =
    member x.Name = "Pfim"

    interface IPixLoader with
        member x.Name = x.Name

        member x.LoadFromFile(filename) =
            PixImagePfim.Load(filename)

        member x.LoadFromStream(stream) =
            PixImagePfim.Load(stream)

        member x.SaveToFile(filename, image, saveParams) =
            raise <| NotSupportedException($"{x.Name} loader does not support saving.")

        member x.SaveToStream(stream, image, saveParams) =
            raise <| NotSupportedException($"{x.Name} loader does not support saving.")

        member x.GetInfoFromFile(filename) =
            raise <| NotSupportedException($"{x.Name} loader does not support getting info.")

        member x.GetInfoFromStream(stream) =
            raise <| NotSupportedException($"{x.Name} loader does not support getting info.")