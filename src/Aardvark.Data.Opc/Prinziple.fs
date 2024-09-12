namespace Aardvark.Data.Opc

open System
open System.IO
open System.Collections.Concurrent
open System.Collections.Generic
open System.Xml
open ICSharpCode.SharpZipLib.Zip
open Aardvark.Base

/// API for transparently writing and reading from ZIP archives.
module Prinziple =

    [<AutoOpen>]
    module private Utilities =

        type StreamWithDispose(inner: Stream, onDisposed: unit -> unit) =
            inherit Stream()

            let mutable isDisposed = false

            override x.Dispose(_) =
                if not isDisposed then
                    inner.Dispose()
                    onDisposed()
                    isDisposed <- true

            override _.CanRead = inner.CanRead
            override _.CanSeek = inner.CanSeek
            override _.CanWrite = inner.CanWrite
            override _.Length = inner.Length
            override _.Position with get() = inner.Position and set(v) = inner.Position <- v
            override _.Flush() = inner.Flush()
            override _.Seek(offset, origin) = inner.Seek(offset, origin)
            override _.SetLength(value) = inner.SetLength(value)
            override _.Read(buffer, offset, count) = inner.Read(buffer, offset, count)
            override _.Write(buffer, offset, count) = inner.Write(buffer, offset, count)


    type private ZippedOpc(zipPath: string) =
        let zip = new ZipFile(zipPath)
        let dirSeparator = Path.DirectorySeparatorChar
        let rootPath = Path.ChangeExtension(zipPath, null).TrimEnd(dirSeparator) + string dirSeparator;

        let additionalEntries = ConcurrentDictionary<string, string>()

        let findAdditonalEntries() =
            if Directory.Exists rootPath then
                let files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                for path in files do
                    let entry = path.Substring(rootPath.Length).Replace(dirSeparator, '/').ToLowerInvariant()
                    additionalEntries.[entry] <- path

        let tryGetAdditionalEntry(name: string) =
            match additionalEntries.TryGetValue <| name.ToLowerInvariant() with
            | true, path -> ValueSome path
            | _ -> ValueNone

        do findAdditonalEntries()

        member x.ReadEntry(name: string) : Stream =
            match tryGetAdditionalEntry name with
            | ValueSome path ->
                File.OpenRead path

            | _ ->
                let entry = zip.FindEntry(name, true)

                if entry < 0 then
                    raise <| FileNotFoundException($"[Prinziple] Entry {name} in {zipPath} not found.")
                else
                    zip.GetInputStream(int64 entry)

        member x.WriteEntry(name: string) : Stream =
            let path = Path.Combine(rootPath, name.Replace('/', dirSeparator))
            let dir = Path.GetDirectoryName path

            if not <| Directory.Exists dir then
                Directory.CreateDirectory dir |> ignore

            if File.Exists path then File.Delete path // When writing to the ZIP, files get replaced instead of overwritten
            let f = File.OpenWrite path

            new StreamWithDispose(f, fun _ ->
                additionalEntries.[name] <- path
            )

        member x.HasEntry(name: string) =
            match tryGetAdditionalEntry name with
            | ValueSome _ -> true
            | _ -> zip.FindEntry(name, true) >= 0

        member x.HasEntry(predicate: string -> bool) =
            if additionalEntries.Keys |> Seq.exists predicate then
                true
            else
                let count = int zip.Count

                let rec check (index: int) =
                    if index >= count then false
                    else
                        let e = zip.EntryByIndex index
                        if predicate e.Name then true
                        else check (index + 1)

                check 0

        member x.GetFiles(path: string, recursive: bool) =
            let path = path.ToLowerInvariant()

            let isFileInPath (n: string) =
                n |> String.endsWith "/" |> not &&
                n |> String.startsWith path &&
                (recursive || n.LastIndexOf('/') < path.Length)

            let result = ResizeArray<string>()

            for i = 0 to int zip.Count - 1 do
                let n = (zip.EntryByIndex i).Name

                if isFileInPath <| n.ToLowerInvariant() then
                    result.Add (Path.Combine(rootPath, n.Replace('/', dirSeparator)))

            for KeyValue(n, p) in additionalEntries do
                if not <| result.Contains p && isFileInPath n then
                    result.Add p

            result.ToArray()

        member x.GetDirectories(path: string) =
            let path = path.ToLowerInvariant()

            let tryGetFolder (n: string) =
                if n.ToLowerInvariant().StartsWith(path) && n.Length > path.Length then
                    let name = n.Substring path.Length
                    let s = name.IndexOf '/'

                    if s > -1 then
                        ValueSome <| n.Substring(0, path.Length + s + 1)
                    else
                        ValueNone
                else
                    ValueNone

            let result = HashSet<string>()

            let addEntry (n: string) =
                match tryGetFolder n with
                | ValueSome n ->
                    let p = Path.Combine(rootPath, n.Replace('/', dirSeparator))
                    result.Add p |> ignore

                | _ -> ()

            for i = 0 to int zip.Count - 1 do
                let n = (zip.EntryByIndex i).Name
                addEntry n

            for KeyValue(n, _) in additionalEntries do
                addEntry n

            result.ToArray(result.Count)

        member x.Commit(compress: bool) =
            if not additionalEntries.IsEmpty then
                Report.BeginTimed(3, $"[Prinziple] Updating {zipPath}")

                try
                    zip.BeginUpdate(DiskArchiveStorage(zip))

                    for KeyValue(name, path) in additionalEntries do
                        Report.Line(3, path)

                        let source = StaticDiskDataSource(path)
                        let method = if compress then CompressionMethod.Deflated else CompressionMethod.Stored
                        zip.Add(source, name, method)

                    zip.CommitUpdate()
                    additionalEntries.Clear()

                with e ->
                    if zip.IsUpdating then zip.AbortUpdate()
                    Report.Error($"[Priniziple] Failed to update {zipPath}: {e.Message}")
                    Report.EndTimed(3, ": failed") |> ignore
                    reraise()

                try
                    Directory.Delete(rootPath, true)
                with e ->
                    Report.Warn($"[Priniziple] Failed to delete {rootPath}: {e.Message}")

        member x.Dispose() =
            zip.Close()

        interface IDisposable with
            member x.Dispose() = x.Dispose()


    let private zipTable = Dictionary<string, ZippedOpc>()

    let private tryGetZip (path: string) =
        let path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)

        lock zipTable (fun _ ->
            zipTable |> Seq.tryPickV (fun (KeyValue(root, zip)) ->
                if path.StartsWith root then
                    let index = min path.Length (root.Length + 1)
                    let entry = path.Substring(index).Replace(Path.DirectorySeparatorChar, '/')
                    ValueSome struct (zip, entry)
                else
                    ValueNone
            )
        )

    let mutable private allowedExtensions =
        [| ".opc"; ".opcz"; ".zip"|]

    /// Sets the extensions that are considered for subsequent calls of register and tryRegister.
    /// By default .opc, .opcz, and .zip extensions are recognized.
    let setAllowedExtensions (extensions: string seq) =
        allowedExtensions <- Array.ofSeq extensions

    /// Tries to register a path as a ZIP archive by looking for corresponding files with a supported extension.
    /// Upon successful registration the content of the archive is available via the other functions of this module.
    /// Returns true if an archive has been registered successfully, false otherwise.
    let tryRegister (path: string) : bool =
        let path = path |> Path.GetFullPath |> Path.withoutTrailingSlash

        lock zipTable (fun _ ->
            if zipTable.ContainsKey path then true
            else
                allowedExtensions |> Array.exists (fun ext ->
                    let zipPath = path + ext

                    if File.Exists zipPath then
                        Report.BeginTimed(3, $"[Prinziple] Registering archive {zipPath}")

                        try
                            zipTable.[path] <- new ZippedOpc(zipPath)
                            Report.EndTimed(3, $": done") |> ignore
                            true

                        with e ->
                            Report.Warn($"[Prinziple] Failed to open {zipPath}: {e.Message}")
                            Report.EndTimed(3, $": failed") |> ignore
                            false
                    else
                        false
                )
        )

    /// Registers a path as a ZIP archive by looking for corresponding files with a supported extension.
    /// Upon successful registration the content of the archive is available via the other functions of this module.
    /// Returns the input path.
    let register (path: string) =
        tryRegister path |> ignore
        path

    /// Returns a list of all registered paths
    let getRegistered() : string list =
        lock zipTable (fun _ ->
            zipTable.Keys |> List.ofSeq
        )

    /// Returns if the given path is registered.
    let isRegistered (path: string) =
        lock zipTable (fun _ ->
            zipTable.ContainsKey <| Path.GetFullPath(path)
        )

    /// Opens a stream for reading from the given file.
    let openRead (path: string) : Stream =
        match tryGetZip path with
        | ValueSome (zip, entry) ->
            zip.ReadEntry entry

        | ValueNone ->
            File.OpenRead(path)

    /// Opens a stream for writing to the given file.
    /// If the path refers to a registered ZIP archive, the file will be written next to the archive rather than modifying the archive itself.
    /// Modified and new files are added to their corresponding archives when Prinziple.commit is called.
    let openWrite (path: string) : Stream =
        match tryGetZip path with
        | ValueSome (zip, entry) ->
            zip.WriteEntry entry

        | ValueNone ->
            File.OpenWrite(path)

    /// Returns whether a file in the given path exists.
    let fileExists (path: string) =
        match tryGetZip path with
        | ValueSome (zip, entry) ->
            zip.HasEntry entry

        | ValueNone ->
            File.Exists path

    /// Returns whether a directory in the given path exists.
    let directoryExists (path: string) =
        match tryGetZip path with
        | ValueSome (zip, entry) ->
            let entry = if entry.EndsWith "/" then entry else entry + "/"
            if zip.HasEntry entry then
                true
            else
                let entry = entry.ToLowerInvariant()
                zip.HasEntry(fun e -> e.ToLowerInvariant() |> String.startsWith entry)

        | ValueNone ->
            Directory.Exists path

    /// Returns the full path of files in the given directory.
    /// If recursive is true, files of subdirectories are returned as well.
    let getFiles (recursive: bool) (directoryPath: string) : string[] =
        match tryGetZip directoryPath with
        | ValueSome (zip, entry) ->
            let entry = if entry = "" || entry.EndsWith "/" then entry else entry + "/"
            zip.GetFiles(entry, recursive)

        | ValueNone ->
            let option = if recursive then SearchOption.AllDirectories else SearchOption.TopDirectoryOnly
            Directory.GetFiles(directoryPath, "*.*", option)

    /// Returns the full path of directories in the given directory.
    let getDirectories (directoryPath: string) : string[] =
        match tryGetZip directoryPath with
        | ValueSome (zip, entry) ->
            let entry = if entry = "" ||  entry.EndsWith "/" then entry else entry + "/"
            zip.GetDirectories(entry)

        | ValueNone ->
            Directory.GetDirectories(directoryPath)

    /// Reads the content of the given file into a byte array.
    let readAllBytes (path: string) : byte[] =
        use s = openRead path
        Stream.readAllBytes s

    /// Reads the content of the given file as a string.
    let readAllText (path: string) : string =
        use s = openRead path
        use r = new StreamReader(s, detectEncodingFromByteOrderMarks = true)
        r.ReadToEnd();

    /// Reads the lines of the given file as a string array.
    let readAllLines (path: string) : string[] =
        use s = openRead path
        use r = new StreamReader(s, detectEncodingFromByteOrderMarks = true)

        let result = ResizeArray<string>()

        let mutable line = r.ReadLine()
        while line <> null do
            result.Add line
            line <- r.ReadLine()

        result.ToArray()

    /// Writes data to the given file.
    let writeAllBytes (path: string) (data: byte[]) =
        use f = openWrite path
        f.Write(data, 0, data.Length)

    /// Writes a string to the given file.
    let writeAllText (path: string) (content: string) =
        let data = Text.Encoding.UTF8.GetBytes content
        writeAllBytes path data

    /// Writes a string array to the given file.
    let writeAllLines (path: string) (lines: string[]) =
        use f = openWrite path
        use w = new StreamWriter(f)
        for l in lines do w.WriteLine l

    /// Reads the XML document in the given path.
    let readXmlDoc (path: string) =
        use s = openRead path
        let doc = XmlDocument()
        doc.Load s
        doc

    /// Adds all modified and new of the given path to the corresponding archive.
    /// Note: This call must be externally synchronized with other Prinziple function calls.
    let commit (compress: bool) (path: string) =
        let path = Path.GetFullPath path

        lock zipTable (fun _ ->
            match zipTable.TryFindV path with
            | ValueSome zip -> zip.Commit(compress)
            | _ -> ()
        )

    /// Adds all modified and new files to their corresponding archives.
    /// Note: This call must be externally synchronized with other Prinziple function calls.
    let commitAll (compress: bool) =
        lock zipTable (fun _ ->
            for KeyValue(_, zip) in zipTable do
                zip.Commit(compress)
        )

    /// Closes and unregisters the ZIP archive corresponding to the given path if it exists.
    /// Note: This call must be externally synchronized with other Prinziple function calls.
    let close (path: string) =
        let path = Path.GetFullPath(path)

        lock zipTable (fun _ ->
            match zipTable.TryFindV path with
            | ValueSome zip ->
                zipTable.Remove path |> ignore
                zip.Dispose()

            | _ -> ()
        )

    /// Closes and unregisters all ZIP archives.
    /// Note: This call must be externally synchronized with other Prinziple function calls.
    let closeAll() =
        lock zipTable (fun _ ->
            for zip in zipTable.Values do
                zip.Dispose()

            zipTable.Clear()
        )