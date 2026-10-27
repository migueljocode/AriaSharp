namespace AriaSharp.Models;

public enum DownloadState { Waiting, Active, Paused, Complete, Failed, Skipped, Interrupted }
public enum DownloadProtocol { Unknown, Http, Https, Ftp, Ftps, Sftp, Magnet }
public enum ExitCode { Ok = 0, Failure = 1, UsageError = 2, Interrupted = 130 }
public enum EngineEventKind { Started, Paused, Stopped, Complete, Error, Progress }
public enum Confidence { Exact, AtLeast, Estimated, Unknown }
public enum Aria2ErrorKind { None, Timeout, Network, Http, File, Disk, Protocol, Unknown }