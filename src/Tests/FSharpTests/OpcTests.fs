namespace Aardvark.Data.Tests.Opc

open Aardvark.Data.Opc
open Aardvark.Base
open System.IO
open System.Threading
open System.Threading.Tasks

open NUnit.Framework
open FsUnit

module Prinziple =

    let private rootPath = Path.Combine(__SOURCE_DIRECTORY__, "data", "opc", "test")

    [<Test>]
    let fileExists() =
        Prinziple.tryRegister rootPath |> should be True
        [rootPath; "files"; "nested"; "g.txt"] |> Path.combine |> Prinziple.fileExists |> should be True
        [rootPath; "files"; "nested"] |> Path.combine |> Prinziple.fileExists |> should be False

    [<Test>]
    let directoryExists() =
        Prinziple.tryRegister rootPath |> should be True
        [rootPath; "files"; "nested"; "g.txt"] |> Path.combine |> Prinziple.directoryExists |> should be False
        [rootPath; "files"; "nested"] |> Path.combine |> Prinziple.directoryExists |> should be True

    [<TestCase(true)>]
    [<TestCase(false)>]
    let getFiles (recursive: bool) =
        Prinziple.tryRegister rootPath |> should be True

        let expected =
            [|
                "files/a.txt"
                "files/b.txt"
                "files/c.txt"
                "files/d.txt"
                "files/e.txt"
                if recursive then "files/more/f.txt"
                if recursive then "files/nested/g.txt"
            |]
            |> Array.map (fun f -> Path.Combine(rootPath, f) |> Path.GetFullPath)
            |> Array.sort

        let files =
            Path.combine [rootPath; "files"]
            |> Prinziple.getFiles recursive
            |> Array.sort

        files |> should equal expected

    [<Test>]
    let getDirectories() =
        Prinziple.tryRegister rootPath |> should be True

        let expected =
            [|
                "files/nested"
                "files/more"
            |]
            |> Array.map (fun f -> Path.Combine(rootPath, f) |> Path.GetFullPath |> Path.withTrailingSlash)
            |> Array.sort

        let dirs =
            Path.combine [rootPath; "files"]
            |> Prinziple.getDirectories
            |> Array.sort

        dirs |> should equal expected

    [<Test>]
    let reading() =
        Prinziple.tryRegister rootPath |> should be True

        let path = Path.combine [rootPath; "files"; "a.txt"]
        Prinziple.readAllText path |> should equal "TEST"

    [<Test>]
    let ``concurrent I/O``() =
        Prinziple.tryRegister rootPath |> should be True

        try
            let mutable running = true

            let writers : Task list =
                List.init 2 (fun _ ->
                    task {
                        do! Async.SwitchToThreadPool()

                        let rnd = RandomSystem()

                        while running do
                            do! Async.Sleep (200 + rnd.UniformInt 300)

                            let path = Path.combine [rootPath; Path.GetRandomFileName()]
                            Prinziple.writeAllText path "TEST"
                    }
                )

            let readers : Task list =
                List.init 4 (fun _ ->
                    task {
                        do! Async.SwitchToThreadPool()

                        let rnd = RandomSystem()

                        while running do
                            do! Async.Sleep (50 + rnd.UniformInt 100)

                            let files = Prinziple.getFiles true rootPath
                            let file = files.[rnd.UniformInt files.Length]

                            let result = Prinziple.readAllText file
                            if result <> "TEST" then
                                failwithf "Got: %s" result
                    } :> Task
                )

            Thread.Sleep(System.TimeSpan.FromSeconds 5.0)
            running <- false

            Task.WhenAll(readers @ writers).Wait()

        finally
            if Directory.Exists rootPath then
                Directory.Delete(rootPath, true)

    [<Test>]
    let writing() =
        Prinziple.tryRegister rootPath |> should be True

        let path = Path.combine [rootPath; "files"; "new.txt"]
        Prinziple.fileExists path |> should be False

        try
            Prinziple.writeAllText path "HEY"
            Prinziple.readAllText path |> should equal "HEY"

            Prinziple.writeAllText path "K"
            Prinziple.readAllText path |> should equal "K"

        finally
            if Directory.Exists rootPath then
                Directory.Delete(rootPath, true)

    [<Test>]
    let commit() =
        let tempRootPath = rootPath + "_temp"
        let tempRootZip = Path.ChangeExtension(tempRootPath, ".zip")

        if File.Exists tempRootZip then File.Delete tempRootZip
        File.Copy (Path.ChangeExtension(rootPath, ".zip"), tempRootZip)

        try
            Prinziple.tryRegister tempRootPath |> should be True

            let p1 = Path.combine [tempRootPath; "files"; "new.txt"]
            Prinziple.writeAllText p1 "HEY"

            let p2 = Path.combine [tempRootPath; "files"; "a.txt"]
            Prinziple.writeAllText p2 "B"

            Prinziple.commitAll false

            Directory.Exists tempRootPath |> should be False
            Prinziple.readAllText p1 |> should equal "HEY"
            Prinziple.readAllText p2 |> should equal "B"

        finally
            Prinziple.closeAll()
            if File.Exists tempRootZip then File.Delete tempRootZip
            if Directory.Exists tempRootPath then Directory.Delete(tempRootPath, true)