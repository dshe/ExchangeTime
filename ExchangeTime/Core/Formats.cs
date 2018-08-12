using System.Collections.Generic;

namespace ExchangeTime.Code
{
    internal class Format
    {
        internal int SecondsPerPixel, Major, Minor;
        internal string MajorFormat;
    }

    internal class Formats : List<Format>
    {
        private void Add(int secondsPerPixel, int major, int minor, string majorFormat)
        {
            Add(new Format
            {
                SecondsPerPixel = secondsPerPixel,
                Major = major,
                Minor = minor,
                MajorFormat = majorFormat
            });
        }
        internal Formats()
        {
            Add(   1, 60          , 15          , "H:mm");
            Add(   2, 60 *  5     , 60          , "H:mm");
            Add(   3, 60 *  5     , 60          , "H:mm");
            Add(   5, 60 *  5     , 60          , "H:mm");
            Add(  10, 60 * 15     , 60 * 5      , "H:mm");
            Add(  15, 60 * 15     , 60 * 5      , "H:mm");
            Add(  30, 60 * 30     , 50 * 15     , "H:mm");      // 30 seconds
            Add(  60, 60 * 60     , 60 * 15     , "H:mm");      //  1 minute
            Add( 120, 60 * 60     , 60 * 30     , "%H");         //  2 minutes
            Add( 180, 60 * 60     , 60 * 30     , "%H");         //  3 minutes
            Add( 300, 60 * 60 *  6, 60 * 60     , "H:mm");      //  5 minutes
            Add( 600, 60 * 60 * 24, 60 * 60 *  3, "ddd MMM d"); // 10 minutes
            Add( 900, 60 * 60 * 24, 60 * 60 *  3, "ddd MMM d"); // 15 minutes
            Add(1800, 60 * 60 * 24, 60 * 60 *  6, "MMM d");     // 30 minutes
            Add(3600, 60 * 60 * 24, 60 * 60 * 12, "ddd MMM d"); //  1 hour
            Add(7200, 60 * 60 * 24, 60 * 60 * 12, "MMM d");     //  2 hours
        }
    }



}
