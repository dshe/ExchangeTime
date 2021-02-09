using System.Net;
using System.Runtime.CompilerServices;

namespace ExchangeTime.Utility
{
    class Initializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ServicePointManager.UseNagleAlgorithm = false;
        }
    }
}
