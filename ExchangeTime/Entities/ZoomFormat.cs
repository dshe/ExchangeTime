using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;

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
        private int index = 3;
        internal int Index { get => index; set
            {
                if (value < 0 || value >= FormatList.Count)
                    throw new IndexOutOfRangeException();
                index = value;
            }
        }
        private Format Format => FormatList[index];
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

        internal ZoomFormat()
        {
            using FileStream fs = File.OpenRead(DataFileName);
            FormatList = JsonDocument
                .Parse(fs)
                .RootElement
                .EnumerateArray()
                .Select(j => new Format(j))
                .ToList();
        }
    }
}
