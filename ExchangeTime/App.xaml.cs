using System.Windows;

namespace ExchangeTime
{
	public partial class App : Application
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
