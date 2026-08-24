using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Axon.UI.Services
{
    public static class ThemeService
    {
        public static bool IsDarkMode { get; private set; } = true;

        public static void SetTheme(bool isDark)
        {
            IsDarkMode = isDark;
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            var appResources = System.Windows.Application.Current.Resources;

            if (isDark)
            {
                theme.SetBaseTheme(BaseTheme.Dark);
                
                appResources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(9, 9, 13));             // #09090D
                appResources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(12, 12, 16));        // #0C0C10
                appResources["HeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(14, 14, 18));         // #0E0E12
                appResources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 26));           // #12121A
                appResources["CardInnerBgBrush"] = new SolidColorBrush(Color.FromRgb(24, 24, 36));      // #181824
                appResources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(34, 34, 48));       // #222230
                appResources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));    // White
                appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9CA3AF
                appResources["HeaderPillBgBrush"] = new SolidColorBrush(Color.FromRgb(22, 22, 32));
            }
            else
            {
                theme.SetBaseTheme(BaseTheme.Light);

                appResources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(243, 244, 246));         // #F3F4F6 Soft Light Gray
                appResources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(12, 12, 16));        // Sidebar stays dark/sleek
                appResources["HeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // Pure White Header
                appResources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));        // Crisp White Cards
                appResources["CardInnerBgBrush"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));   // Slate 50 Soft Inner
                appResources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));    // Slate 200 Border
                appResources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));      // Slate 900 Dark Text
                appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // Slate 500 Subtext
                appResources["HeaderPillBgBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            }

            paletteHelper.SetTheme(theme);
        }
    }
}
