using System.Windows.Controls;
using Axon.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Axon.UI.Views
{
    public partial class ProfitLossView : UserControl
    {
        public ProfitLossView()
        {
            InitializeComponent();
            // Resolve from DI since DataTemplate calls the parameterless constructor
            if (App.AppHost != null)
            {
                DataContext = App.AppHost.Services.GetRequiredService<ProfitLossViewModel>();
            }
            else
            {
                // Design-time fallback (no DB) - won't crash since ctor is parameterized
                // At design time, DataContext will simply be null - acceptable
            }
        }
    }
}
