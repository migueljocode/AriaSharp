namespace AriaSharp.Models;

public static class ErrorCatalog
{
    public static string TextFor(int code) => code switch
    {
        1 => "File not found", 2 => "Protocol error", 3 => "HTTP error", 4 => "File exists",
        5 => "Resource not found", 6 => "Connection failed", 7 => "Network error",
        8 => "Disk full", 9 => "Protocol not supported", 10 => "Invalid option",
        11 => "Invalid parameter", 12 => "Unsupported protocol", 13 => "File already exists",
        14 => "Connection timed out", 15 => "Operation timed out", 16 => "Invalid protocol",
        17 => "Invalid file", 18 => "Invalid directory", 19 => "Invalid URL", 20 => "Invalid GID",
        21 => "Invalid session", 22 => "Invalid option value", 23 => "Invalid option name",
        24 => "Invalid option combination", 25 => "Invalid option type", 26 => "Invalid option range",
        27 => "Invalid option length", 28 => "Invalid option format", 29 => "Invalid option value range",
        30 => "Invalid option value type", 31 => "Invalid option value format",
        32 => "Invalid option value length", 33 => "Invalid option value range",
        _ => $"Unknown error (code {code})"
    };

    public static string HintFor(int code) => code switch
    {
        1 => "Check that the file path is correct and the file exists.",
        2 => "Verify the URL scheme is supported by aria2.",
        3 => "Check the server response and network connection.",
        4 => "Use --allow-overwrite or remove the existing file.",
        5 => "Verify the URL is reachable.",
        6 => "Check network connectivity and firewall settings.",
        7 => "Check your network connection and DNS settings.",
        8 => "Free up disk space or change the output directory.",
        9 => "Use a supported protocol (http, https, ftp, sftp, magnet).",
        10 => "Review the aria2 command-line options.",
        11 => "Check the value of the option you provided.",
        12 => "Use a protocol aria2 supports.",
        13 => "Use -f/--force to overwrite existing files.",
        14 => "Check your network connection and increase the timeout if needed.",
        15 => "The operation took too long; check the server and network.",
        16 => "Use a supported protocol.",
        17 => "Check the file path and permissions.",
        18 => "Check the directory path and permissions.",
        19 => "Verify the URL is correctly formatted.",
        20 => "Use a valid GID from the active or waiting list.",
        21 => "The session file may be corrupted; try resuming with a fresh start.",
        22 => "Check the value you provided for the option.",
        23 => "Check the option name is spelled correctly.",
        24 => "Some options cannot be used together.",
        25 => "Check the option type matches what aria2 expects.",
        26 => "Check the option value is within the allowed range.",
        27 => "Check the option value length.",
        28 => "Check the option value format.",
        29 => "Check the option value range.",
        30 => "Check the option value type.",
        31 => "Check the option value format.",
        32 => "Check the option value length.",
        33 => "Check the option value range.",
        _ => "Check the aria2 log for more details."
    };

    public static Aria2ErrorKind Classify(int code) => code switch
    {
        1 or 4 or 5 or 10 or 11 or 13 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 or 31 or 32 or 33 => Aria2ErrorKind.File,
        2 or 9 or 12 or 16 => Aria2ErrorKind.Protocol,
        3 => Aria2ErrorKind.Http,
        6 or 7 or 14 => Aria2ErrorKind.Network,
        8 => Aria2ErrorKind.Disk,
        15 => Aria2ErrorKind.Timeout,
        _ => Aria2ErrorKind.Unknown
    };
}