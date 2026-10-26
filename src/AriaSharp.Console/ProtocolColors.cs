namespace AriaSharp.App;

public static class ProtocolColors
{
    public static string For(Models.DownloadProtocol protocol) => protocol switch
    {
        Models.DownloadProtocol.Https => "green",
        Models.DownloadProtocol.Http => "cyan",
        Models.DownloadProtocol.Ftp => "blue",
        Models.DownloadProtocol.Ftps => "blue",
        Models.DownloadProtocol.Sftp => "magenta",
        Models.DownloadProtocol.Magnet => "yellow",
        _ => "grey"
    };
}