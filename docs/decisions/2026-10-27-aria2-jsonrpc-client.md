# S1: Aria2.JsonRpcClient vs in-repo RPC

**Decision (provisional):** Keep the in-repo `Aria2RpcClient` + `Aria2NotificationListener` (HTTP JSON-RPC + WebSocket notifications). Do **not** take a dependency on NuGet package `Aria2.JsonRpcClient` until a PublishAot spike proves it safe.

**Evidence so far**
- The published package does not advertise AOT compatibility in its metadata (not the same as “fails under AOT”).
- Loopback-only RPC, owner-only conf secret, multicall progress, and terminal-event → `saveSession` are controlled in-repo.

**Required to promote this to a final decision**
- Spike: reference `Aria2.JsonRpcClient` in a PublishAot console, call a minimal method, record IL2026/IL3050 and runtime result.
- If green and secret/conf model fits, prefer the package and delete the in-repo client; document the spike log here.

**Owner preference:** Brief preferred the package for notification events; this note overrides only until the spike is done.
