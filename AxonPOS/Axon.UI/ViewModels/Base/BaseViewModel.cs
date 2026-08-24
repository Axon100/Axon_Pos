using CommunityToolkit.Mvvm.ComponentModel;

namespace Axon.UI.ViewModels.Base
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _title;
        
        [ObservableProperty]
        private bool _hasError;
        
        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isEmpty;
    }
}
