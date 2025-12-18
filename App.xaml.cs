using System.Windows;

namespace Advent_calendar_Deambrogio_Barthod
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Désactiver le shutdown automatique
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Afficher la fenêtre de connexion
            LoginWindow loginWindow = new LoginWindow();
            bool? loginResult = loginWindow.ShowDialog();

            if (loginResult == true && !string.IsNullOrEmpty(loginWindow.UserPrenom))
            {
                // L'utilisateur a validé, ouvrir le calendrier
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                MainWindow mainWindow = new MainWindow(loginWindow.UserPrenom);
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                // L'utilisateur a annulé, fermer l'application
                Shutdown();
            }
        }
    }
}