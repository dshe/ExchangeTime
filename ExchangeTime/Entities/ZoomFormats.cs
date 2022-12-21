using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace ExchangeTime;

internal sealed class ZoomFormats
{
    private readonly List<ZoomFormat> Formats;
    internal ZoomFormats()
    {
        using FileStream fs = File.OpenRead("zooms.json");
        Formats = JsonDocument
            .Parse(fs)
            .RootElement
            .EnumerateArray()
            .Select(j => new ZoomFormat(j))
            .ToList();
    }

    private int index = 3;
    internal int Index
    {
        get => index;
        set
        {
            if (value < 0 || value >= Formats.Count)
                throw new InvalidOperationException("Index");
            index = value;
        }
    }

    private ZoomFormat Format => Formats[index];
    internal int SecondsPerPixel => Format.SecondsPerPixel;
    internal int Major => Format.Major;
    internal int Minor => Format.Minor;
    internal string MajorFormat => Format.MajorFormat;
    internal bool Zoom(bool expand)
    {
        int newIndex = Index + (expand ? -1 : +1);
        if (newIndex < 0 || newIndex >= Formats.Count)
            return false;
        Index = newIndex;
        return true;
    }
}

internal sealed class ZoomFormat
{
    internal readonly int SecondsPerPixel, Major, Minor;
    internal readonly string MajorFormat;
    internal ZoomFormat(JsonElement json)
    {
        SecondsPerPixel = json.GetProperty("secondsPerPixel").GetInt32();
        Major = json.GetProperty("major").GetInt32();
        Minor = json.GetProperty("minor").GetInt32();
        MajorFormat = json.GetProperty("majorFormat").GetString()
            ?? throw new InvalidDataException("Missing property: 'majorFormat'.");
    }
}
