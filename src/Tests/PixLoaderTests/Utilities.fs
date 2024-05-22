namespace Aardvark.Data.Tests.PixLoader

open Aardvark.Base
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

module PixImage =

    let isColor (color : 'T[]) (pi : PixImage<'T>) =
        for c in 0 .. pi.ChannelCount - 1 do
            let data = pi.GetChannel(int64 c)

            for x in 0 .. pi.Size.X - 1 do
                for y in 0 .. pi.Size.Y - 1 do
                    Expect.equal data.[x, y] color.[c] "PixImage data mismatch"