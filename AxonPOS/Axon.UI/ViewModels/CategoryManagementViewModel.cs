using Axon.UI.ViewModels.Base;
using Axon.Application.Interfaces.Repositories;
using Axon.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace Axon.UI.ViewModels
{
    public partial class CategoryManagementViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _totalCategoriesCount = 6;

        [ObservableProperty]
        private int _activeCategoriesCount = 6;

        private readonly IRepository<Category> _categoryRepository;

        public ObservableCollection<CategoryItemModel> Categories { get; } = new();

        public CategoryManagementViewModel(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
            Title = "قائمة الأقسام";

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Categories.Clear();
                var categoryEntities = (await _categoryRepository.GetAllAsync()).ToList();

                if (categoryEntities.Count == 0)
                {
                    Categories.Add(new CategoryItemModel { Id = 1, Name = "إكسسوار وكابلات", Description = "كابلات الشبكات والإكسسوارات", ProductsCount = 45, IsActive = true });
                    Categories.Add(new CategoryItemModel { Id = 2, Name = "UPS", Description = "مزودات الطاقة غير المنقطعة", ProductsCount = 12, IsActive = true });
                    Categories.Add(new CategoryItemModel { Id = 3, Name = "سويتشات", Description = "سويتشات الشبكة ومعدات التوجيه", ProductsCount = 28, IsActive = true });
                    Categories.Add(new CategoryItemModel { Id = 4, Name = "راوترات", Description = "أجهزة الراوتر والإنترنت", ProductsCount = 15, IsActive = true });
                    Categories.Add(new CategoryItemModel { Id = 5, Name = "كاميرات IP", Description = "كاميرات المراقبة الشبكية", ProductsCount = 60, IsActive = true });
                    Categories.Add(new CategoryItemModel { Id = 6, Name = "Analog", Description = "كاميرات المراقبة التماثلية", ProductsCount = 30, IsActive = true });
                }
                else
                {
                    foreach (var c in categoryEntities)
                    {
                        Categories.Add(new CategoryItemModel
                        {
                            Id = c.Id,
                            Name = string.IsNullOrEmpty(c.NameAR) ? (c.NameEN ?? "قسم") : c.NameAR,
                            Description = c.NameEN ?? "قسم فرعي",
                            ProductsCount = 10,
                            IsActive = true
                        });
                    }
                }

                TotalCategoriesCount = Categories.Count;
                ActiveCategoriesCount = Categories.Count(c => c.IsActive);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenAddCategoryDialog()
        {
            // Placeholder for Add Category Dialog
        }
    }

    public class CategoryItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProductsCount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
