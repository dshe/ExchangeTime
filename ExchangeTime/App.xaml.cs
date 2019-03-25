using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using ExchangeTime.Code;
using ExchangeTime.Utility;

#nullable enable

namespace ExchangeTime
{
	public partial class App
	{
        protected override void OnStartup(StartupEventArgs e)
        {
            Sys.Init(true);
			base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
		{
			Sys.Exit();
			base.OnExit(e);
		}
    }
}
