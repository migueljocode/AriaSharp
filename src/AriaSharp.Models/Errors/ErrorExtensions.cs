namespace AriaSharp.Models;

/// <summary>C# 14 extension members — canonical aria2 error mapping.</summary>
public static class ErrorExtensions
{
    extension(int code)
    {
        public string Aria2Text() => code switch
        {
            1 => "Unknown error",
            2 => "Timeout",
            3 => "Resource not found",
            4 => "Max file not found",
            5 => "Download too slow",
            6 => "Network problem",
            7 => "Unfinished",
            8 => "Cannot resume",
            9 => "Not enough disk space",
            10 => "Piece length changed",
            11 => "Duplicate download",
            12 => "Duplicate info hash",
            13 => "File already exists",
            14 => "File renaming failed",
            15 => "File open error",
            16 => "File create error",
            17 => "File I/O error",
            18 => "Directory create error",
            19 => "Name resolution failed",
            20 => "Metalink parse error",
            21 => "FTP protocol error",
            22 => "HTTP protocol error",
            23 => "Too many redirects",
            24 => "HTTP authentication failed",
            25 => "Bencode parse error",
            26 => "BitTorrent parse error",
            27 => "Magnet parse error",
            28 => "Option error",
            29 => "HTTP service unavailable",
            30 => "JSON parse error",
            31 => "Removed by user",
            32 => "Checksum error",
            _ => $"Unknown error (code {code})",
        };

        public string Aria2Hint() => code switch
        {
            1 => "Check the aria2 log for more details.",
            2 => "The operation timed out; check the server and network, or increase the timeout.",
            3 => "Verify the URL is reachable and the resource exists.",
            4 => "The referenced file could not be found; check the path.",
            5 => "The download is too slow; check the server and connection.",
            6 => "Check network connectivity and firewall settings.",
            7 => "The download is still in progress; wait for it to finish.",
            8 => "The download cannot be resumed; start it again from the beginning.",
            9 => "Free up disk space or change the output directory.",
            10 => "The piece length changed; restart the download.",
            11 => "This download is already queued or active.",
            12 => "This info hash is already being downloaded.",
            13 => "Use --force/--allow-overwrite to overwrite the existing file.",
            14 => "Check file permissions and available disk space.",
            15 => "Check that the file path is valid and accessible.",
            16 => "Check that the output directory exists and is writable.",
            17 => "A file I/O error occurred; check the storage device.",
            18 => "Check that the directory path is valid and accessible.",
            19 => "DNS name resolution failed; check your network and DNS settings.",
            20 => "The metalink file is malformed.",
            21 => "An FTP protocol error occurred; check the FTP server and credentials.",
            22 => "An HTTP protocol error occurred; check the HTTP server response.",
            23 => "Too many HTTP redirects; check the target URL.",
            24 => "HTTP authentication failed; check your username and password.",
            25 => "The bencode data could not be parsed.",
            26 => "The BitTorrent data could not be parsed.",
            27 => "The magnet link could not be parsed.",
            28 => "An aria2 option is invalid; check the option name and value.",
            29 => "The HTTP service is unavailable; try again later.",
            30 => "The JSON response could not be parsed.",
            31 => "The download was removed by the user.",
            32 => "The downloaded file failed the checksum verification.",
            _ => "Check the aria2 log for more details.",
        };

        public Aria2ErrorKind Aria2Kind() => code switch
        {
            2 => Aria2ErrorKind.Timeout,
            6 or 19 => Aria2ErrorKind.Network,
            3 or 23 or 24 or 29 => Aria2ErrorKind.Http,
            9 => Aria2ErrorKind.Disk,
            1 or 4 or 8 or 13 or 14 or 15 or 16 or 17 or 18 or 31 or 32 => Aria2ErrorKind.File,
            21 or 22 or 25 or 26 or 27 or 30 => Aria2ErrorKind.Protocol,
            _ => Aria2ErrorKind.Unknown,
        };

        public bool IsAria2Skipped() => code is 11 or 13;
    }
}
