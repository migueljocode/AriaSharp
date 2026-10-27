# S1: Aria2.JsonRpcClient vs in-repo RPC

**Decision:** Keep the in-repo `Aria2RpcClient` + `Aria2NotificationListener` (HTTP JSON-RPC + WebSocket notifications). Do **not** take a dependency on NuGet package `Aria2.JsonRpcClient`.

**Why**
- The published package is not declared AOT-safe; Native AOT is a hard requirement for the Console publish.
- Loopback-only RPC, owner-only conf secret, and multicall progress shape are controlled in-repo without trimming surprises.
- WebSocket notification mapping (`onDownloadComplete` / `Error` / `Stop` → session save) is wired here.

**Revisit if** a future package version ships official AOT annotations and matches our secret/conf model; then re-run spike S1 and document results here.
