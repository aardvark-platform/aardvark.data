namespace Tests

open Aardvark.Base

open Expecto

module EmbeddedResource =
    open System.Reflection
    open System.Text.RegularExpressions

    let get (path : string) =
        let asm = Assembly.GetExecutingAssembly()
        let name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase)
        let path = Regex.Replace(path, @"(\\|\/)", ".")
        let stream = asm.GetManifestResourceStream(name + "." + path)
        if stream <> null then stream
        else failwithf "Cannot open resource stream with name '%s'" path

    let loadPixImage<'T> (path : string) =
        use stream = get path
        PixImagePfim.Load(stream).AsPixImage<'T>()

    let loadPixImageMipmap (path : string) =
        use stream = get path
        PixImagePfim.LoadWithMipmap(stream)

module PixImage =

    let isColor (color : 'T[]) (pi : PixImage<'T>) =
        for c in 0 .. pi.ChannelCount - 1 do
            let data = pi.GetChannel(int64 c)

            for x in 0 .. pi.Size.X - 1 do
                for y in 0 .. pi.Size.Y - 1 do
                    Expect.equal data.[x, y] color.[c] "PixImage data mismatch"