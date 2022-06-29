namespace Aardvark.Data.DGM

open System
open Aardvark.Base
open Aardvark.Rendering

module DGM = 

    let inline isNan (v : V3f) = v.AnyNaN

    let slowSystemParseDouble (c : char[], start : int, endIndex : int) =
        let s = String(c,start,endIndex-start)
        let mutable r = 0.0
        if Double.TryParse(s,Globalization.NumberStyles.Float,Globalization.CultureInfo.InvariantCulture, &r) then r
        else failwithf "could not parse int double: %s" s

    let mutable doubleParse : char[] * int * int -> float = slowSystemParseDouble

    let createIndex (vi : Matrix<V3f>) =
        let dx = vi.Info.DX
        let dy = vi.Info.DY
        let dxy = dx + dy
        let mutable arr = Array.zeroCreate (int (vi.SX - 1L) * int (vi.SY - 1L) * 6)
        let mutable cnt = 0
        
        vi.SubMatrix(V2l.Zero,vi.Size-V2l.II).ForeachXYIndex(fun x y index -> 
            let i00 = index
            let i10 = index + dy
            let i01 = index + dx
            let i11 = index + dxy

            let p00 = vi.[i00] |> isNan
            let p01 = vi.[i10] |> isNan
            let p10 = vi.[i01] |> isNan
            let p11 = vi.[i11] |> isNan

            if p00 || p01 || p10 || p11 then ()
            else 
                arr.[cnt + 0] <- (int i00)
                arr.[cnt + 1] <- (int i10)
                arr.[cnt + 2] <- (int i11)
                arr.[cnt + 3] <- (int i00)
                arr.[cnt + 4] <- (int i11)
                arr.[cnt + 5] <- (int i01)
                cnt <- cnt + 6
        )
        Array.Resize(&arr, cnt)
        arr

    type DGM = 
        {
            ncols : int
            nrows : int
            corner : V2d
            cellSize : float
            noDataValue : float
        }

    let loadDgm (fileName : string) = 
        let extract (tag : string)  (s : String) =
            let tokens = s.SplitOnWhitespace()
            if tokens.Length <> 2 || tokens.[0].ToUpperInvariant() <> tag then 
                failwithf "could not parse tag: %s in file: %s" tag fileName
            tokens.[1]                

        let inline pint (s : string) = 
            let mutable r = 0
            if Int32.TryParse(s,&r) then r else failwithf "could not parse int from: %s in file: %s" s fileName

        let inline pfloat (s : String) =
            let mutable r = 0.0
            if Double.TryParse(s,Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, &r) then r
            else failwithf "could not parse int double: %s in file: %s" s fileName

        use txt        = IO.File.OpenText(fileName) 
        let ncols      = txt.ReadLine() |> extract "NCOLS"     |> pint
        let nrows      = txt.ReadLine() |> extract "NROWS"     |> pint
        let xllcorner  = txt.ReadLine() |> extract "XLLCORNER" |> pfloat
        let yllcorner  = txt.ReadLine() |> extract "YLLCORNER" |> pfloat
        let cellSize   = txt.ReadLine() |> extract "CELLSIZE"  |> pfloat
        let noData     = txt.ReadLine() |> extract "NODATA_VALUE" |> pfloat
        let bufferSize = IO.FileInfo(fileName).Length |> int
        let buffer     = Array.zeroCreate bufferSize 
        let elementCnt = ncols * nrows
        let result     = Array.zeroCreate elementCnt
        let mutable cursor = 0
        while not txt.EndOfStream do
            let cnt = txt.ReadBlock(buffer,0,bufferSize)
            let mutable last = 0
            let mutable i = 0
            while i < cnt do
                let current = buffer.[i]
                if current = ' ' || current = '\n' || current = '\r' then
                    let cnt = i - last
                    if cnt > 0 then
                        let f = doubleParse(buffer,last,i)
                        result.[cursor] <- f
                        cursor <- cursor + 1
                    last <- i + 1
                    i <- i + 1
                else i <- i + 1
        if cursor <> elementCnt then failwithf "element count: %d does not match expected: %d in file: %s" cursor elementCnt fileName
        {
            ncols         = ncols
            nrows         = nrows
            corner        = V2d(xllcorner,yllcorner)
            cellSize      = cellSize
            noDataValue   = noData
        }, result

    let computeVertices (dgm : DGM) (arr : float[]) =
        Matrix<V3f>(V2i(dgm.ncols,dgm.nrows)).SetByCoord(fun (c : V2l) ->
            let height = arr.[int c.X + int c.Y * dgm.ncols]
            if height = dgm.noDataValue then V3f.NaN
            else V3f(float c.X * dgm.cellSize, (float dgm.nrows - float c.Y) * dgm.cellSize, height)
        )

    let normals (dgm : DGM) (o : Matrix<V3f>) =
        let m = o.SubMatrix(V2l.II,o.Size-V2l.II)
        o.Copy().SubMatrix(V2l.II,m.Size-V2l.II).SetByCoord(fun (l:V2l)-> 
            let v = m.[l]
            let mutable W = m.[l.X-1L,l.Y]
            let mutable O = m.[l.X+1L,l.Y]
            let mutable N = m.[l.X,l.Y+1L]
            let mutable S = m.[l.X,l.Y-1L]
//            let n1 = (W-v).Cross(N-v)
//            let n2 = (O-v).Cross(S-v)
//            (n1+n2).Normalized

            if W |> isNan then W <- v
            if O |> isNan then O <- v
            if N |> isNan then N <- v
            if S |> isNan then S <- v
//            (W-O).Cross(N-S).Normalized

            (V3f(W.Z - O.Z, N.Z - S.Z, float32 dgm.cellSize * 2.0f)).Normalized // 2*10m cell size
        )

    let createUVs (dgm : DGM) (o : Matrix<V3f>) =
        Matrix<V2f>(o.Size).SetByCoord(fun (l:V2l)-> 
            V2f(float l.X / float o.Size.X, 1.0 - float l.Y / float o.Size.Y)
        )


    let dgm2IndexGeometry (fileName : string) =

        let sw = System.Diagnostics.Stopwatch()
        Log.startTimed "Parsing"
        sw.Start()
        let dgm,heightfield = loadDgm fileName
        sw.Stop()
        Log.stop()

        Log.startTimed "Computing vertices"
        let vertices = computeVertices dgm heightfield
        Log.stop()

        Log.startTimed "Computing normals"
        let normals = normals dgm vertices
        Log.stop()

        Log.startTimed "Creating Mesh"
        let index = createIndex vertices
        Log.stop()

        Log.startTimed "Creating UVS"
        let uvs = createUVs dgm vertices
        Log.stop()

        printfn "ns/double: %f" (float sw.Elapsed.TotalMilliseconds * 1000000.0 / float heightfield.Length)

        let ig = 
            IndexedGeometry(
                Mode = IndexedGeometryMode.TriangleList,
                IndexArray = index,
                IndexedAttributes =
                    SymDict.ofList [
                        DefaultSemantic.Normals, normals.Data :> Array
                        DefaultSemantic.Positions, vertices.Data :> Array
                        DefaultSemantic.DiffuseColorCoordinates, uvs.Data :> Array
                    ]
            )
        ig,dgm





