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
                
                appResources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(15, 15, 15));             // #0F0F0F (Login Window Pure Deep Black)
                appResources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(15, 15, 15));        // #0F0F0F
                appResources["SidebarBorderBrush"] = new SolidColorBrush(Color.FromRgb(38, 38, 38));    // #262626
                appResources["HeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(20, 20, 20));         // #141414
                appResources["HeaderBorderBrush"] = new SolidColorBrush(Color.FromRgb(38, 38, 38));     // #262626
                appResources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 18));           // #121212
                appResources["CardInnerBgBrush"] = new SolidColorBrush(Color.FromRgb(22, 22, 22));      // #161616
                appResources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(38, 38, 38));       // #262626
                appResources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));    // White
                appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(142, 142, 147));  // #8E8E93
                appResources["HeaderPillBgBrush"] = new SolidColorBrush(Color.FromRgb(18, 18, 18));
                appResources["ChartTrackBgBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));     // #1A1A1A
                appResources["GridLineBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));        // #1A1A1A
                appResources["TableHeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(20, 20, 20));    // #141414
                appResources["TableAltRowBrush"] = new SolidColorBrush(Color.FromRgb(15, 15, 15));      // #0F0F0F
                appResources["InputBgBrush"] = new SolidColorBrush(Color.FromRgb(22, 22, 22));          // #161616
                appResources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(44, 44, 44));       // #2C2C2C
                appResources["SidebarTextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
                appResources["SidebarIconBrush"] = new SolidColorBrush(Color.FromRgb(180, 181, 181));   // Gray/White
                appResources["SidebarLogoutBgBrush"] = new SolidColorBrush(Color.FromRgb(31, 17, 19));   // #1F1F13
                appResources["SidebarLogoutBorderBrush"] = new SolidColorBrush(Color.FromRgb(78, 22, 28));// #4E161C
                appResources["PosBrandTextBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // White
                appResources["TopControlIconBrush"] = new SolidColorBrush(Color.FromRgb(180, 181, 181)); // Gray
                appResources["TopCloseBtnBgBrush"] = new SolidColorBrush(Color.FromRgb(43, 20, 22));    // #2B1416
                appResources["TopCloseBtnBorderBrush"] = new SolidColorBrush(Color.FromRgb(78, 22, 28));// #4E161C
                appResources["TopCloseBtnTextBrush"] = new SolidColorBrush(Color.FromRgb(239, 68, 68));  // #EF4444
            }
            else
            {
                theme.SetBaseTheme(BaseTheme.Light);

                appResources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));         // #F8F9FA Clean Light
                appResources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // Pure White Sidebar
                appResources["SidebarBorderBrush"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));  // #E5E7EB Light Border
                appResources["HeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // Pure White Header
                appResources["HeaderBorderBrush"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));  // #E5E7EB Light Border
                appResources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));        // Pure White Cards
                appResources["CardInnerBgBrush"] = new SolidColorBrush(Color.FromRgb(244, 245, 247));   // Soft Light Inner
                appResources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));    // #E5E7EB Light Border
                appResources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));      // #1E1E1E Dark Text
                appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(107, 114, 128)); // #6B7280 Subtext
                appResources["HeaderPillBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                appResources["ChartTrackBgBrush"] = new SolidColorBrush(Color.FromRgb(243, 244, 246));   // #F3F4F6 Light Track
                appResources["GridLineBrush"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));      // #E5E7EB Grid Lines
                appResources["TableHeaderBgBrush"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));  // #F8F9FA Table Header
                appResources["TableAltRowBrush"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));   // #FAFAFA Alt Row
                appResources["InputBgBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));       // Pure White Input
                appResources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));    // #E5E7EB Input Border
                appResources["SidebarTextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(31, 41, 55)); // #1F2937 Dark Slate Text
                appResources["SidebarIconBrush"] = new SolidColorBrush(Color.FromRgb(75, 85, 99));      // #4B5563 Dark Icon
                appResources["SidebarLogoutBgBrush"] = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Soft red tint
                appResources["SidebarLogoutBorderBrush"] = new SolidColorBrush(Color.FromRgb(252, 165, 165));
                appResources["PosBrandTextBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));     // #1E1E1E Dark POS Text
                appResources["TopControlIconBrush"] = new SolidColorBrush(Color.FromRgb(75, 85, 99));   // #4B5563
                appResources["TopCloseBtnBgBrush"] = new SolidColorBrush(Color.FromRgb(254, 242, 242));  // #FEE2E2
                appResources["TopCloseBtnBorderBrush"] = new SolidColorBrush(Color.FromRgb(252, 165, 165));// #FCA5A5
                appResources["TopCloseBtnTextBrush"] = new SolidColorBrush(Color.FromRgb(217, 4, 41));    // #D90429
            }

            paletteHelper.SetTheme(theme);
        }
    }
}
