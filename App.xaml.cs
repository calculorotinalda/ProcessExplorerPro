using System;
using System.IO;
using System.Windows;

namespace ProcessExplorerPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) => 
                LogCrash(args.ExceptionObject as Exception, "AppDomain.UnhandledException");

            DispatcherUnhandledException += (s, args) => {
                LogCrash(args.Exception, "DispatcherUnhandledException");
                args.Handled = true;
            };

            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogCrash(ex, "OnStartup Exception");
                throw;
            }
        }

        private void LogCrash(Exception? ex, string source)
        {
            if (ex == null) return;
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.txt");
            string errorText = $"=== CRASH LOG ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===\n" +
                               $"Source: {source}\n" +
                               $"Message: {ex.Message}\n" +
                               $"Stack Trace:\n{ex.StackTrace}\n";
            if (ex.InnerException != null)
            {
                errorText += $"Inner Exception: {ex.InnerException.Message}\n" +
                             $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
            }
            errorText += "======================================\n\n";

            try
            {
                File.AppendAllText(logPath, errorText);
                MessageBox.Show($"Ocorreu um erro crítico no arranque da aplicação ({source}):\n\n{ex.Message}\n\nConsulte crash.txt para mais detalhes.", "Erro Crítico - Process Explorer Pro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Ignore fallback failure
            }
        }
    }
}
