using System.Windows;
using System.Windows.Controls;
using Axon.UI.ViewModels;

namespace Axon.UI.Views
{
    public partial class ProductManagementView : UserControl
    {
        public ProductManagementView()
        {
            InitializeComponent();
        }

        private void ProductsTab_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductManagementViewModel vm)
            {
                vm.SelectedTab = 0;
            }
        }

        private void CategoriesTab_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductManagementViewModel vm)
            {
                vm.SelectedTab = 1;
            }
        }
    }
}
