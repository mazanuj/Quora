using System.Windows;
using Quora.Properties;
using QuoraLib.Utilities;

namespace Quora
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnExit(ExitEventArgs e)
        {
            Utils.DisposeWebDrivers();
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}