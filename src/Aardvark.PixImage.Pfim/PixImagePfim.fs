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

    let private create<'T when 'T : unmanaged> (copy : MipMapOffset -> nativeint -> PixImage<'T> -> unit)
                                               (format : Col.Format) (info : MipMapOffset) (data : nativeint) : PixImage =
        let pi = PixImage<'T>(format, int64 info.Width, int64 info.Height)
        copy info data pi
        pi

    let private copyDirect<'T when 'T : unmanaged> (info : MipMapOffset) (src : nativeint) (dst : PixImage<'T>) =
        let srcVolume =
            NativeVolume<'T>(
                NativePtr.ofNativeInt (src + nativeint info.DataOffset),
                VolumeInfo(
                    0L,
                    V3l(info.Width, info.Height, dst.ChannelCount),
                    V3l(dst.ChannelCount, info.Stride, 1)
                )
            )

        PixImage.copyFromNativeVolume srcVolume dst

    // TODO: Implement missing formats
    let private levelToPixImageTable : (ImageFormat -> (MipMapOffset -> nativeint -> PixImage) option) =
        LookupTable.lookupTable' [
            ImageFormat.Rgb8,     create copyDirect<uint8> Col.Format.Gray
            ImageFormat.Rgb24,    create copyDirect<uint8> Col.Format.BGR
            ImageFormat.Rgba32,   create copyDirect<uint8> Col.Format.BGRA
            //ImageFormat.R5g5b5
            //ImageFormat.R5g6b5
            //ImageFormat.R5g5b5a1
            //ImageFormat.Rgba16
        ]

    let private levelToPixImage (format : ImageFormat) =
        match levelToPixImageTable format with
        | Some pi -> pi
        | _ ->
            raise <| NotSupportedException($"Format {format} not supported.")

    let toPixImage (image : IImage) =
        pinned image.Data (
            levelToPixImage image.Format image.TopLevel
        )

    let toPixImageMipmap (image : IImage) =
        let create = levelToPixImage image.Format

        let levels =
            pinned image.Data (fun data ->
                Array.init image.Levels (fun i ->
                    let level = image.GetLevel i
                    create level data
                )
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