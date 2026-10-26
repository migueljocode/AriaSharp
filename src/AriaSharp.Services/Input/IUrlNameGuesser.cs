namespace AriaSharp.Services;

public interface IUrlNameGuesser
{
    string GuessName(Uri url);
}
