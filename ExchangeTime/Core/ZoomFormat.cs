using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ExchangeTime
{
    internal class Format
    {
        internal readonly int SecondsPerPixel, Major, Minor;
        internal readonly string MajorFormat;
        internal Format(JsonElement json)
        {
            SecondsPerPixel = json.GetProperty("secondsPerPixel").GetInt32();
            Major = json.GetProperty("major").GetInt32();
            Minor = json.GetProperty("minor").GetInt32();
            MajorFormat = json.GetProperty("majorFormat").GetString()
                ?? throw new InvalidDataException("Missing property: 'majorFormat'");
        }
    }

    internal class ZoomFormat
    {
        private const string DataFileName = "zooms.json";
        private readonly List<Format> FormatList;
        internal int Index { get; private set; }
        private Format Format => FormatList[Index];
        internal int SecondsPerPixel => Format.SecondsPerPixel;
        internal int Major => Format.Major;
        internal int Minor => Format.Minor;
        internal string MajorFormat => Format.MajorFormat;
        internal bool Zoom(bool expand)
        {
            int newIndex = Index + (expand ? -1 : +1);
            if (newIndex >= 0 && newIndex < FormatList.Count)
            {
                Index = newIndex;
                return true;
            }
            return false;
        }

        internal ZoomFormat(int initialIndex)
        {
            using FileStream fs = File.OpenRead(DataFileName);
            FormatList = JsonDocument
                .Parse(fs)
                .RootElement
                .EnumerateArray()
                .Select(j => new Format(j))
                .ToList();

            if (initialIndex >= 0 && initialIndex < FormatList.Count)
                Index = initialIndex;
            else
            {
                var logger = App.MyLoggerFactory.CreateLogger("ZoomFormat");
                logger.LogWarning("Invalid zoom index.");
                //throw new IndexOutOfRangeException();
            }
        }
    }
}
