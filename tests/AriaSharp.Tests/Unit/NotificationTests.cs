using System.Text.Json;

using AriaSharp.Models;
using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class NotificationTests
{
    [Theory]
    [InlineData("aria2.onDownloadStart", EngineEventKind.Started)]
    [InlineData("aria2.onDownloadPause", EngineEventKind.Paused)]
    [InlineData("aria2.onDownloadStop", EngineEventKind.Stopped)]
    [InlineData("aria2.onDownloadComplete", EngineEventKind.Complete)]
    [InlineData("aria2.onBtDownloadComplete", EngineEventKind.Complete)]
    [InlineData("aria2.onDownloadError", EngineEventKind.Error)]
    public void TryMap_KnownMethods(string method, EngineEventKind kind)
    {
        var json = $$"""{"jsonrpc":"2.0","method":"{{method}}","params":[{"gid":"abc123"}]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.True(Aria2NotificationListener.TryMap(doc.RootElement, out var ev));
        Assert.Equal(kind, ev.Kind);
        Assert.Equal("abc123", ev.Gid);
        Assert.Equal(method, ev.Message);
    }

    [Fact]
    public void TryMap_UnknownMethod_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("""{"jsonrpc":"2.0","method":"aria2.tellActive","params":[]}""");
        Assert.False(Aria2NotificationListener.TryMap(doc.RootElement, out _));
    }

    [Fact]
    public void TryMap_NestedGidArray()
    {
        var json = """{"jsonrpc":"2.0","method":"aria2.onDownloadComplete","params":[[{"gid":"deadbeef"}]]}""";
        using var doc = JsonDocument.Parse(json);
        Assert.True(Aria2NotificationListener.TryMap(doc.RootElement, out var ev));
        Assert.Equal("deadbeef", ev.Gid);
    }
}
