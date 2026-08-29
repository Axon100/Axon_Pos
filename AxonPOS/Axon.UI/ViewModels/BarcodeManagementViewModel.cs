using Axon.Application.Interfaces.Repositories;
using Axon.UI.Views;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using Axon.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Axon.UI.ViewModels
{
    public partial class BarcodeManagementViewModel : BaseViewModel
    {
        private readonly IBarcodeService _barcodeService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<SystemSetting> _systemSettingRepository;

        // ===== Filter & Selection Properties =====
        [ObservableProperty]
        private string _searchBarcodeOrCode = string.Empty;

        [ObservableProperty]
        private CategoryOptionModel? _selectedCategory;

        [ObservableProperty]
        private ProductOptionModel? _selectedProduct;

        [ObservableProperty]
        private decimal _costPrice;

        [ObservableProperty]
        private decimal _sellingPrice;

        [ObservableProperty]
        private int _labelsCount = 12;

        [ObservableProperty]
        private string _storeName = "Axon POS";

        [ObservableProperty]
        private string _labelSize = "38mm × 25mm (ملصق قياسي)";

        // ===== Display Checkboxes =====
        [ObservableProperty]
        private bool _showStoreName = true;

        [ObservableProperty]
        private bool _showProductName = true;

        [ObservableProperty]
        private bool _showPrice = true;

        [ObservableProperty]
        private bool _showBarcodeNumber = true;

        [ObservableProperty]
        private bool _showCategoryName = false;

        // ===== Report & Toolbar State =====
        [ObservableProperty]
        private bool _hasGenerated = false;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private double _zoomScale = 1.0;

        [ObservableProperty]
        private string _zoomText = "100%";

        [ObservableProperty]
        private string _statusInfo = "جاهز لتنفيذ وإنشاء ملصقات الباركود";

        // ===== Collections =====
        public ObservableCollection<CategoryOptionModel> Categories { get; } = new();
        public ObservableCollection<ProductOptionModel> Products { get; } = new();
        private readonly List<ProductOptionModel> _allProducts = new();
        public ObservableCollection<BarcodeLabelItemModel> GeneratedLabels { get; } = new();
        public ObservableCollection<string> LabelSizes { get; } = new()
        {
            "38mm × 25mm (ملصق قياسي)"
        };

        private bool _isInitializing = false;

        
        public void SelectProductById(int productId)
        {
            var match = _allProducts.FirstOrDefault(p => p.Id == productId);
            if (match != null)
            {
                SelectedProduct = match;
                _ = ExecuteGenerateCommand.ExecuteAsync(null);
            }
        }

        public BarcodeManagementViewModel(
            IBarcodeService barcodeService,
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<SystemSetting> systemSettingRepository)
        {
            _barcodeService = barcodeService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _systemSettingRepository = systemSettingRepository;

            Title = "إدارة وطباعة الباركود";
            _ = LoadInitialDataAsync();
        }

        [RelayCommand]
        public async Task LoadInitialDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            _isInitializing = true;
            try
            {
                // Fetch dynamic store name from Settings
                var settings = await _systemSettingRepository.GetAllAsync();
                var storeSetting = settings.FirstOrDefault(s => s.Key == "LegalStoreName");
                if (storeSetting != null && !string.IsNullOrWhiteSpace(storeSetting.Value))
                {
                    StoreName = storeSetting.Value;
                }

                Categories.Clear();
                Products.Clear();
                _allProducts.Clear();

                // Add "All Categories" option
                Categories.Add(new CategoryOptionModel { Id = 0, Name = "جميع الأقسام والتصنيفات" });

                var catList = await _categoryRepository.GetAllAsync();
                foreach (var c in catList)
                {
                    Categories.Add(new CategoryOptionModel
                    {
                        Id = c.Id,
                        Name = string.IsNullOrEmpty(c.NameAR) ? c.NameEN : c.NameAR
                    });
                }

                var prodList = await _productRepository.GetAllAsync();
                foreach (var p in prodList)
                {
                    var item = new ProductOptionModel
                    {
                        Id = p.Id,
                        Name = string.IsNullOrEmpty(p.NameAR) ? p.NameEN : p.NameAR,
                        Sku = p.SKU ?? string.Empty,
                        Barcode = p.Barcode ?? string.Empty,
                        SellingPrice = p.SellingPrice,
                        CostPrice = p.CostPrice,
                        CategoryId = p.CategoryId,
                        CurrentStock = (int)p.CurrentStock
                    };
                    _allProducts.Add(item);
                    Products.Add(item);
                }

                _isInitializing = false;

                SelectedCategory = Categories.FirstOrDefault();
                if (Products.Count > 0)
                {
                    SelectedProduct = Products[0];
                }
                StatusInfo = $"تم تحميل {_allProducts.Count} منتج بنجاح.";
            }
            catch (Exception ex)
            {
                StatusInfo = $"خطأ في تحميل البيانات: {ex.Message}";
            }
            finally
            {
                _isInitializing = false;
                IsBusy = false;
            }
        }

        partial void OnSelectedCategoryChanged(CategoryOptionModel? value)
        {
            if (_isInitializing) return;

            Products.Clear();
            if (value == null || value.Id == 0)
            {
                foreach (var p in _allProducts) Products.Add(p);
            }
            else
            {
                var filtered = _allProducts.Where(p => p.CategoryId == value.Id).ToList();
                foreach (var p in filtered) Products.Add(p);
            }

            if (Products.Count > 0)
            {
                if (SelectedProduct == null || !Products.Contains(SelectedProduct))
                {
                    SelectedProduct = Products[0];
                }
            }
            else
            {
                SelectedProduct = null;
                SellingPrice = 0;
                CostPrice = 0;
                SearchBarcodeOrCode = string.Empty;
            }
        }

        partial void OnSelectedProductChanged(ProductOptionModel? value)
        {
            if (_isInitializing) return;

            if (value != null)
            {
                SellingPrice = value.SellingPrice;
                CostPrice = value.CostPrice;
            }
        }

        partial void OnSearchBarcodeOrCodeChanged(string value)
        {
            if (_isInitializing || string.IsNullOrWhiteSpace(value)) return;
            var q = value.Trim();
            var matched = _allProducts.FirstOrDefault(p => 
                p.Sku.Equals(q, StringComparison.OrdinalIgnoreCase) || 
                (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(q, StringComparison.OrdinalIgnoreCase)) ||
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase));

            if (matched != null && matched != SelectedProduct)
            {
                if (!Products.Contains(matched))
                {
                    // Reset category to All
                    SelectedCategory = Categories.FirstOrDefault();
                }
                SelectedProduct = matched;
            }
        }

        // ==================== COMMANDS ====================

        [RelayCommand]
        private async Task ExecuteGenerateAsync()
        {
            if (SelectedProduct == null && _allProducts.Count == 0 && string.IsNullOrWhiteSpace(SearchBarcodeOrCode))
            {
                AxonMessageBox.Show("يرجى اختيار صنف أو إدخال كود الباركود أولاً لإنشاء الملصقات!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = await _systemSettingRepository.GetAllAsync();
                var storeSetting = settings.FirstOrDefault(s => s.Key == "LegalStoreName");
                if (storeSetting != null && !string.IsNullOrWhiteSpace(storeSetting.Value))
                {
                    StoreName = storeSetting.Value;
                }
            }
            catch { }

            var prod = SelectedProduct ?? _allProducts.FirstOrDefault();

            if (LabelsCount <= 0) LabelsCount = 1;

            StatusInfo = "جاري إنشاء وتصميم ملصقات الباركود...";
            GeneratedLabels.Clear();

            try
            {
                string barcodeValue;
                if (!string.IsNullOrWhiteSpace(SearchBarcodeOrCode))
                {
                    barcodeValue = SearchBarcodeOrCode.Trim();
                }
                else if (prod != null)
                {
                    barcodeValue = !string.IsNullOrWhiteSpace(prod.Barcode) ? prod.Barcode : (!string.IsNullOrWhiteSpace(prod.Sku) ? prod.Sku : $"PRD-{prod.Id:D6}");
                }
                else
                {
                    barcodeValue = "00000000";
                }

                var productName = prod != null ? prod.Name : "منتج مخصص";
                var catName = SelectedCategory?.Id > 0 ? SelectedCategory.Name : "عام";

                // Generate real Code-128 Barcode Image
                byte[] barcodeBytes = _barcodeService.GenerateBarcode(barcodeValue);
                BitmapImage? barcodeBitmap = ConvertByteArrayToBitmap(barcodeBytes);

                var displayPrice = SellingPrice > 0 ? SellingPrice : (prod != null ? prod.SellingPrice : 0);
                var formattedPrice = (displayPrice % 1 == 0) ? $"{displayPrice:0} ج.م" : $"{displayPrice:0.##} ج.م";

                for (int i = 0; i < LabelsCount; i++)
                {
                    GeneratedLabels.Add(new BarcodeLabelItemModel
                    {
                        StoreName = StoreName,
                        ProductName = productName,
                        SkuOrBarcode = barcodeValue,
                        Price = displayPrice,
                        FormattedPrice = formattedPrice,
                        CategoryName = catName,
                        BarcodeImage = barcodeBitmap,
                        ShowStoreName = ShowStoreName,
                        ShowProductName = ShowProductName,
                        ShowPrice = ShowPrice,
                        ShowBarcodeNumber = ShowBarcodeNumber,
                        ShowCategoryName = ShowCategoryName
                    });
                }

                HasGenerated = true;
                CurrentPage = 1;
                TotalPages = Math.Max(1, (int)Math.Ceiling(GeneratedLabels.Count / 24.0));
                StatusInfo = $"تم إنشاء ({GeneratedLabels.Count}) ملصق بنجاح جاهز للطباعة.";
            }
            catch (Exception ex)
            {
                StatusInfo = $"فشل إنشاء الباركود: {ex.Message}";
                AxonMessageBox.Show($"حدث خطأ أثناء توليد الباركود: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void PrintLabels(FrameworkElement? visualElement)
        {
            if (GeneratedLabels.Count == 0)
            {
                AxonMessageBox.Show("يرجى تنفيذ التقرير وتوليد الملصقات أولاً قبل إرسال أمر الطباعة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();

                // 1. Auto-select Barcode Label Printer (XP-233B / Label / Barcode)
                try
                {
                    var printServer = new System.Printing.LocalPrintServer();
                    var printQueues = printServer.GetPrintQueues();
                    var labelQueue = printQueues.FirstOrDefault(q => 
                        q.Name.Contains("233", StringComparison.OrdinalIgnoreCase) || 
                        q.Name.Contains("Label", StringComparison.OrdinalIgnoreCase) || 
                        q.Name.Contains("Barcode", StringComparison.OrdinalIgnoreCase) ||
                        (q.Name.Contains("XP-", StringComparison.OrdinalIgnoreCase) && !q.Name.Contains("80", StringComparison.OrdinalIgnoreCase)));

                    if (labelQueue != null)
                    {
                        printDialog.PrintQueue = labelQueue;
                    }
                }
                catch { }

                // Print individual stickers directly one-by-one so thermal print head renders each label on 38x25 stock
                int count = 0;
                foreach (var labelData in GeneratedLabels)
                {
                    var visual = CreateDirectPrintVisual(labelData);
                    printDialog.PrintVisual(visual, $"Label_{++count}_{labelData.SkuOrBarcode}");
                }

                AxonMessageBox.Show($"تمت طباعة ({GeneratedLabels.Count}) ملصق بنجاح على طابعة الباركود ({printDialog.PrintQueue?.Name ?? "XP-233B"}).", "تمت الطباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AxonMessageBox.Show($"تعذر إتمام عملية الطباعة: {ex.Message}", "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreateDirectPrintVisual(BarcodeLabelItemModel label)
        {
            // Standard 38mm x 25mm in 96 DPI points: 38mm = ~144 points, 25mm = ~95 points
            var container = new System.Windows.Controls.Border
            {
                Width = 144,
                Height = 95,
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(4, 2, 4, 2),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // 0: Store Name
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // 1: Product Name
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) }); // 2: Barcode
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // 3: Digits & Price

            // Store Name
            if (label.ShowStoreName && !string.IsNullOrWhiteSpace(label.StoreName))
            {
                var txtStore = new System.Windows.Controls.TextBlock
                {
                    Text = label.StoreName,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                System.Windows.Controls.Grid.SetRow(txtStore, 0);
                grid.Children.Add(txtStore);
            }

            // Product Name
            if (label.ShowProductName && !string.IsNullOrWhiteSpace(label.ProductName))
            {
                var txtProd = new System.Windows.Controls.TextBlock
                {
                    Text = label.ProductName,
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 1)
                };
                System.Windows.Controls.Grid.SetRow(txtProd, 1);
                grid.Children.Add(txtProd);
            }

            // High Contrast Barcode Image
            if (label.BarcodeImage != null)
            {
                var img = new System.Windows.Controls.Image
                {
                    Source = label.BarcodeImage,
                    Stretch = System.Windows.Media.Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(1, 0, 1, 1)
                };
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Grid.SetRow(img, 2);
                grid.Children.Add(img);
            }

            // Digits & Price Footer
            var footer = new System.Windows.Controls.Grid();
            footer.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

            if (label.ShowBarcodeNumber && !string.IsNullOrWhiteSpace(label.SkuOrBarcode))
            {
                var txtCode = new System.Windows.Controls.TextBlock
                {
                    Text = label.SkuOrBarcode,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                System.Windows.Controls.Grid.SetColumn(txtCode, 0);
                footer.Children.Add(txtCode);
            }

            if (label.ShowPrice && label.Price > 0)
            {
                var txtPrice = new System.Windows.Controls.TextBlock
                {
                    Text = label.FormattedPrice,
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                System.Windows.Controls.Grid.SetColumn(txtPrice, 1);
                footer.Children.Add(txtPrice);
            }

            System.Windows.Controls.Grid.SetRow(footer, 3);
            grid.Children.Add(footer);

            container.Child = grid;

            // Measure & arrange to force instant rendering
            container.Measure(new Size(144, 95));
            container.Arrange(new Rect(0, 0, 144, 95));
            container.UpdateLayout();

            return container;
        }

        private FrameworkElement CreateSingleThermalLabelVisual(BarcodeLabelItemModel label)
        {
            // Exact 38mm x 25mm in 96 DPI points (~144px width x 95px height)
            double targetWidth = 144;
            double targetHeight = 95;

            if (LabelSize.Contains("50mm"))
            {
                targetWidth = 190;
                targetHeight = 95;
            }
            else if (LabelSize.Contains("70mm"))
            {
                targetWidth = 265;
                targetHeight = 132;
            }

            var border = new System.Windows.Controls.Border
            {
                Width = targetWidth,
                Height = targetHeight,
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(4, 2, 4, 2),
                SnapsToDevicePixels = true
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // Store
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // Name
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) }); // Barcode Image
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // Code & Price

            // Store Name
            if (label.ShowStoreName && !string.IsNullOrWhiteSpace(label.StoreName))
            {
                var txtStore = new System.Windows.Controls.TextBlock
                {
                    Text = label.StoreName,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                System.Windows.Controls.Grid.SetRow(txtStore, 0);
                grid.Children.Add(txtStore);
            }

            // Product Name
            if (label.ShowProductName && !string.IsNullOrWhiteSpace(label.ProductName))
            {
                var txtProd = new System.Windows.Controls.TextBlock
                {
                    Text = label.ProductName,
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 1)
                };
                System.Windows.Controls.Grid.SetRow(txtProd, 1);
                grid.Children.Add(txtProd);
            }

            // Barcode Image
            if (label.BarcodeImage != null)
            {
                var img = new System.Windows.Controls.Image
                {
                    Source = label.BarcodeImage,
                    Stretch = System.Windows.Media.Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 2, 1)
                };
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Grid.SetRow(img, 2);
                grid.Children.Add(img);
            }

            // Footer (Barcode digits + Price)
            var footerGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 0) };
            footerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

            if (label.ShowBarcodeNumber && !string.IsNullOrWhiteSpace(label.SkuOrBarcode))
            {
                var txtCode = new System.Windows.Controls.TextBlock
                {
                    Text = label.SkuOrBarcode,
                    FontSize = 8,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                System.Windows.Controls.Grid.SetColumn(txtCode, 0);
                footerGrid.Children.Add(txtCode);
            }

            if (label.ShowPrice && label.Price > 0)
            {
                var txtPrice = new System.Windows.Controls.TextBlock
                {
                    Text = label.FormattedPrice,
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                System.Windows.Controls.Grid.SetColumn(txtPrice, 1);
                footerGrid.Children.Add(txtPrice);
            }

            System.Windows.Controls.Grid.SetRow(footerGrid, 3);
            grid.Children.Add(footerGrid);

            border.Child = grid;
            return border;
        }

        [RelayCommand]
        private void ZoomIn()
        {
            if (ZoomScale < 2.0)
            {
                ZoomScale = Math.Round(ZoomScale + 0.15, 2);
                ZoomText = $"{(int)(ZoomScale * 100)}%";
            }
        }

        [RelayCommand]
        private void ZoomOut()
        {
            if (ZoomScale > 0.5)
            {
                ZoomScale = Math.Round(ZoomScale - 0.15, 2);
                ZoomText = $"{(int)(ZoomScale * 100)}%";
            }
        }

        [RelayCommand]
        private void ResetZoom()
        {
            ZoomScale = 1.0;
            ZoomText = "100%";
        }

        [RelayCommand]
        private void IncreaseCount()
        {
            LabelsCount += 1;
        }

        [RelayCommand]
        private void DecreaseCount()
        {
            if (LabelsCount > 1) LabelsCount -= 1;
        }

        [RelayCommand]
        private void FirstPage() => CurrentPage = 1;

        [RelayCommand]
        private void PrevPage()
        {
            if (CurrentPage > 1) CurrentPage--;
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages) CurrentPage++;
        }

        [RelayCommand]
        private void LastPage() => CurrentPage = TotalPages;

        private static BitmapImage? ConvertByteArrayToBitmap(byte[]? byteArray)
        {
            if (byteArray == null || byteArray.Length == 0) return null;
            try
            {
                using var stream = new MemoryStream(byteArray);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }

    public class BarcodeLabelItemModel
    {
        public string StoreName { get; set; } = "AXON STORE";
        public string ProductName { get; set; } = string.Empty;
        public string SkuOrBarcode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string FormattedPrice { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public BitmapImage? BarcodeImage { get; set; }

        public bool ShowStoreName { get; set; } = true;
        public bool ShowProductName { get; set; } = true;
        public bool ShowPrice { get; set; } = true;
        public bool ShowBarcodeNumber { get; set; } = true;
        public bool ShowCategoryName { get; set; } = false;
    }

    public class CategoryOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductOptionModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int CategoryId { get; set; }
        public int CurrentStock { get; set; }
    }
}
