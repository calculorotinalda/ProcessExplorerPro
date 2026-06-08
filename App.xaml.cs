using System;
using System.IO;
using System.Windows;

namespace ProcessExplorerPro
{
    public partial class App : Application
    {
        private static readonly string ThemeConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.config");

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
                bool isDark = LoadThemePreference();
                ApplyTheme(isDark);
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogCrash(ex, "OnStartup Exception");
                throw;
            }
        }

        public static bool LoadThemePreference()
        {
            try
            {
                if (File.Exists(ThemeConfigPath))
                {
                    string content = File.ReadAllText(ThemeConfigPath).Trim();
                    if (bool.TryParse(content, out bool isDark))
                    {
                        return isDark;
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return true; // Default to dark theme
        }

        public static void SaveThemePreference(bool isDark)
        {
            try
            {
                File.WriteAllText(ThemeConfigPath, isDark.ToString());
            }
            catch
            {
                // Ignore
            }
        }

        public static void ApplyTheme(bool isDark)
        {
            var app = Application.Current;
            if (app == null) return;

            void SetBrush(string key, string hexColor)
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
                app.Resources[key] = new System.Windows.Media.SolidColorBrush(color);
            }

            if (isDark)
            {
                SetBrush("BackgroundBrush", "#0F1015");
                SetBrush("CardBrush", "#161821");
                SetBrush("BorderBrush", "#232530");
                SetBrush("AccentBrush", "#5C62D6");
                SetBrush("AccentHoverBrush", "#4E53C4");
                SetBrush("TextBrush", "#E1E3E6");
                SetBrush("TextMutedBrush", "#848694");
                SetBrush("SelectedRowBrush", "#205C62D6");
                SetBrush("HoverRowBrush", "#1E202F");
                SetBrush("TitleBarBrush", "#0A0B10");
                SetBrush("SidebarBrush", "#0C0D13");
                SetBrush("AlternatingRowBrush", "#191B26");
                SetBrush("ButtonBackgroundBrush", "#1E202F");
                SetBrush("ButtonHoverBackgroundBrush", "#2A2D3C");
                SetBrush("TextBoxBackgroundBrush", "#0D0E15");
                SetBrush("ScrollThumbBrush", "#30FFFFFF");
            }
            else
            {
                SetBrush("BackgroundBrush", "#F5F6F8");
                SetBrush("CardBrush", "#FFFFFF");
                SetBrush("BorderBrush", "#E2E8F0");
                SetBrush("AccentBrush", "#5C62D6");
                SetBrush("AccentHoverBrush", "#4E53C4");
                SetBrush("TextBrush", "#1E293B");
                SetBrush("TextMutedBrush", "#64748B");
                SetBrush("SelectedRowBrush", "#155C62D6");
                SetBrush("HoverRowBrush", "#F1F5F9");
                SetBrush("TitleBarBrush", "#ECEFF1");
                SetBrush("SidebarBrush", "#E2E8F0");
                SetBrush("AlternatingRowBrush", "#F8FAFC");
                SetBrush("ButtonBackgroundBrush", "#E2E8F0");
                SetBrush("ButtonHoverBackgroundBrush", "#CBD5E1");
                SetBrush("TextBoxBackgroundBrush", "#FFFFFF");
                SetBrush("ScrollThumbBrush", "#30000000");
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
