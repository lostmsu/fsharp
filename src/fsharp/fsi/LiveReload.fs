namespace Microsoft.FSharp.Compiler.Interactive

open System.IO
open System.Collections.Generic

type private LiveFile(path) =
    let mutable isDirty = false
    member this.Path = path
    member this.IsDirty with get() = isDirty
                        and internal set value = isDirty <- value

[<Sealed>]
type LiveReload() =
    let mutable disposed = false
    let watchers = dict []
    let files = List<LiveFile>()

    let watch directory =
        if watchers.ContainsKey directory then ()
        else
        let watcher = new FileSystemWatcher(directory, "*.fs;*.fsx")
        // TODO: add event handlers
        watcher.EnableRaisingEvents <- true
        watchers.Add(directory, watcher)

    let notDisposed() = if disposed then raise <| System.ObjectDisposedException("LiveReload")

    member this.Track file =
        notDisposed()

        if files.Exists(fun liveFile -> liveFile.Path = file) then ()
        else
        let directory = Path.GetDirectoryName(file) |> Path.GetFullPath
        files.Add <| LiveFile(file)
        try
            watch directory
        with e ->
            files.RemoveAt(files.Count - 1)

    member this.Reload loader =
        let dirtyStart = files.FindIndex(fun liveFile -> liveFile.IsDirty)
        if dirtyStart < 0 then ()
        else
        let dirtyList = Array.init (files.Count - dirtyStart) (fun i -> files.[i + dirtyStart].Path)
        let remainder = loader dirtyList
        if Array.length remainder < dirtyList.Length then
            raise <| System.ArgumentException("loader")

        let di = dirtyList.Length - Array.length remainder
        for i = Array.length remainder - 1 downto 0 do
            if not <| dirtyList.[di + i].Equals(remainder.[i], System.StringComparison.OrdinalIgnoreCase) then
                raise <| System.ArgumentException "loader"
        
        for i = dirtyStart to files.Count - 1 - remainder.Length do
            files.[i].IsDirty <- false

    interface System.IDisposable with
        member this.Dispose() =
            if disposed then ()
            else
            disposed <- true
            files.Clear()
            for watcher in watchers.Values do watcher.Dispose()
            watchers.Clear()
