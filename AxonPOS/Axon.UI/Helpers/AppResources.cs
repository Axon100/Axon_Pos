using System.Windows;

namespace Axon.UI.Helpers
{
    public static class AppResources
    {
        public static string GetString(string key, string defaultValue = "")
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Resources.Contains(key))
            {
                return System.Windows.Application.Current.Resources[key]?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}
