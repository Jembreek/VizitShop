using System;
using System.Windows;

namespace VizitShop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Показываем окно входа
            new LoginWindow().Show();
        }
    }
}