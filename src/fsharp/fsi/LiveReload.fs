namespace Microsoft.FSharp.Compiler.Interactive

open System.IO
open System.Collections.Generic

type LiveReload() =
    let mutable disposed = false
    let watchers = dict []
    let files = List<string>()

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

        if files.Contains file then ()
        else
        let directory = Path.GetDirectoryName(file) |> Path.GetFullPath
        files.Add file
        try
            watch directory
        with e ->
            files.RemoveAt(files.Count - 1)