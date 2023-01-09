namespace Aardvark.Base

open System
open System.IO
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

    // Note: The default allocator of Pfim just allocates a new array, so
    // we can just use it without worrying about it being reused when the original
    // IImage is disposed.
    let private of8Bit (format : Col.Format) (info : MipMapOffset) (src : uint8[]) =
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

        PixImage<uint8>(format, volume) :> PixImage

    // TODO: Implement missing formats
    let private levelToPixImageTable : (ImageFormat -> (MipMapOffset -> uint8[] -> PixImage) option) =
        LookupTable.lookupTable' [
            ImageFormat.Rgb8,     of8Bit Col.Format.Gray
            ImageFormat.Rgb24,    of8Bit Col.Format.BGR
            ImageFormat.Rgba32,   of8Bit Col.Format.BGRA
            //ImageFormat.R5g5b5
            //ImageFormat.R5g6b5
            //ImageFormat.R5g5b5a1
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