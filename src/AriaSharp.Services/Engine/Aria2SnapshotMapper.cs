namespace AriaSharp.Services;

/// <summary>
/// Maps aria2 JSON-RPC tell* payloads into <see cref="DownloadItem"/> lists.
/// </summary>
internal static class Aria2SnapshotMapper
{
    public static List<DownloadItem> MapItems(JsonElement array)
    {
        var list = new List<DownloadItem>();
        if (array.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var el in array.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object)
                continue;

            var status = ReadString(el, "status");
            var state = status switch
            {
                "active" => DownloadState.Active,
                "waiting" => DownloadState.Waiting,
                "paused" => DownloadState.Paused,
                "complete" => DownloadState.Complete,
                "error" => DownloadState.Failed,
                "removed" => DownloadState.Interrupted,
                _ => DownloadState.Waiting,
            };

            var name = "";
            if (el.TryGetProperty("files", out var files)
                && files.ValueKind == JsonValueKind.Array
                && files.GetArrayLength() > 0)
            {
                var f0 = files[0];
                if (f0.TryGetProperty("path", out var pathEl))
                    name = Path.GetFileName(pathEl.GetString() ?? "") ?? "";
            }

            list.Add(new DownloadItem
            {
                Gid = ReadString(el, "gid"),
                Name = name,
                Status = status,
                State = state,
                Directory = ReadString(el, "dir"),
                CompletedLength = ReadLong(el, "completedLength"),
                TotalLength = ReadLong(el, "totalLength"),
                DownloadSpeed = ReadLong(el, "downloadSpeed"),
                Connections = (int)ReadLong(el, "connections"),
                ErrorCode = (int)ReadLong(el, "errorCode"),
                ErrorMessage = string.IsNullOrEmpty(ReadString(el, "errorMessage"))
                    ? null
                    : ReadString(el, "errorMessage"),
            });
        }

        return list;
    }

    public static IEnumerable<string> ExtractGids(JsonElement items)
    {
        if (items.ValueKind != JsonValueKind.Array)
            yield break;
        foreach (var el in items.EnumerateArray())
        {
            var gid = ReadString(el, "gid");
            if (gid.Length > 0)
                yield return gid;
        }
    }

    public static JsonElement ExtractMulticallResult(JsonElement multicall, int index)
    {
        if (multicall.ValueKind != JsonValueKind.Array || index >= multicall.GetArrayLength())
            return default;
        var entry = multicall[index];
        if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() > 0)
            return entry[0];
        return entry;
    }

    private static string ReadString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? ""
            : "";

    private static long ReadLong(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return 0;
        if (p.ValueKind == JsonValueKind.String
            && long.TryParse(p.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            return v;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n))
            return n;
        return 0;
    }
}
