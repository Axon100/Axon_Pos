using Axon.Application.Interfaces.Repositories;
using Axon.Application.Interfaces.Services;
using Axon.Domain.Entities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Axon.UI.Services
{
    public class PrintService : IPrintService
    {
        private readonly IRepository<Sale> _saleRepository;

        public PrintService(IRepository<Sale> saleRepository)
        {
            _saleRepository = saleRepository;
        }

        public async Task PrintReceiptAsync(int saleId)
        {
            Sale? sale = null;
            try
            {
                sale = await _saleRepository.GetByIdAsync(saleId);
            }
            catch
            {
                // Fallback
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        var receiptElement = CreateReceiptVisualElement(sale, saleId);
                        
                        // Measure and arrange element for visual rendering (Thermal paper standard ~80mm width)
                        receiptElement.Measure(new Size(printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 300, double.PositiveInfinity));
                        receiptElement.Arrange(new Rect(new Point(0, 0), receiptElement.DesiredSize));
                        receiptElement.UpdateLayout();

                        printDialog.PrintVisual(receiptElement, $"Receipt_#{saleId}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Printing failed: {ex.Message}");
                    Axon.UI.Views.AxonMessageBox.Show($"تعذر التوصيل بالطابعة أو حدث خطأ أثناء إرسال أمر الطباعة:\n{ex.Message}", "تنبيه الطباعة", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });
        }

        public FrameworkElement CreateReceiptVisualElement(Sale? sale, int saleId)
        {
            var container = new Border
            {
                Width = 290,
                Background = Brushes.White,
                Padding = new Thickness(12, 10, 12, 10),
                FlowDirection = FlowDirection.LeftToRight,
                SnapsToDevicePixels = true
            };

            var mainPanel = new StackPanel();

            // 1. VELOURA LOGO IMAGE
            try
            {
                var logoUri = new Uri("pack://application:,,,/Assets/veloura_logo.png", UriKind.Absolute);
                var logoImg = new Image
                {
                    Source = new BitmapImage(logoUri),
                    Height = 46,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                mainPanel.Children.Add(logoImg);
            }
            catch
            {
                // Fallback text if image pack fails
                var txtBrand = new TextBlock
                {
                    Text = "VELOURA",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Georgia, Times New Roman, Segoe UI"),
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                mainPanel.Children.Add(txtBrand);
            }

            // Address below logo
            var txtAddress1 = new TextBlock
            {
                Text = "العاشر من رمضان - مجاورة الخامسة",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
            mainPanel.Children.Add(txtAddress1);

            var txtAddress2 = new TextBlock
            {
                Text = "مول أبو الوفا، البدروم",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 1, 0, 2)
            };
            mainPanel.Children.Add(txtAddress2);

            // Phone & Social Contacts below Address
            var txtPhone = new TextBlock
            {
                Text = "📞 01509924025   💬 01509923025",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1)
            };
            mainPanel.Children.Add(txtPhone);

            var txtTiktok = new TextBlock
            {
                Text = "🎵 Veloura.clothing",
                FontSize = 9.5,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            mainPanel.Children.Add(txtTiktok);

            // Dotted Separator 1
            mainPanel.Children.Add(CreateDottedLine());

            // 2. BON NR BLOCK
            var bonGrid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            bonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtBonLabel = new TextBlock
            {
                Text = "Bon Nr:",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtBonLabel, 0);
            bonGrid.Children.Add(txtBonLabel);

            var txtBonValue = new TextBlock
            {
                Text = saleId.ToString(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtBonValue, 1);
            bonGrid.Children.Add(txtBonValue);

            mainPanel.Children.Add(bonGrid);
            mainPanel.Children.Add(CreateDottedLine());

            // 3. RECEIPT METADATA (Compact, without Terminal)
            mainPanel.Children.Add(CreateMetaRow("Receipt:", saleId.ToString()));
            mainPanel.Children.Add(CreateMetaRow("Date:", (sale?.Date ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm")));
            mainPanel.Children.Add(CreateMetaRow("Payment Method:", sale?.Payments?.FirstOrDefault()?.PaymentMethod ?? "نقداً (Cash)"));
            mainPanel.Children.Add(CreateMetaRow("Served by:", "Manager"));
            mainPanel.Children.Add(CreateDottedLine());

            // 4. ITEMS TABLE (Clean 3 columns: Item, Qty, Price)
            var headerGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });

            AddTableCell(headerGrid, "Item", 0, FontWeights.Bold, TextAlignment.Left);
            AddTableCell(headerGrid, "Qty", 1, FontWeights.Bold, TextAlignment.Center);
            AddTableCell(headerGrid, "Price", 2, FontWeights.Bold, TextAlignment.Right);

            mainPanel.Children.Add(headerGrid);
            mainPanel.Children.Add(CreateDottedLine());

            // Items List
            if (sale?.LineItems != null)
            {
                foreach (var item in sale.LineItems)
                {
                    var itemGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });

                    var productName = item.Product?.NameAR ?? item.Product?.NameEN ?? $"منتج #{item.ProductId}";
                    var lineTotal = item.Quantity * item.UnitPrice;

                    AddTableCell(itemGrid, productName, 0, FontWeights.Normal, TextAlignment.Right, isArabic: true);
                    AddTableCell(itemGrid, $"x{item.Quantity}", 1, FontWeights.Normal, TextAlignment.Center);
                    AddTableCell(itemGrid, lineTotal.ToString("0.00"), 2, FontWeights.SemiBold, TextAlignment.Right);

                    mainPanel.Children.Add(itemGrid);
                }
            }

            mainPanel.Children.Add(CreateDottedLine());

            // 5. TOTALS
            var countTxt = new TextBlock
            {
                Text = $"Items count: {sale?.LineItems?.Sum(i => i.Quantity) ?? 0}",
                FontSize = 10,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 2, 0, 3)
            };
            mainPanel.Children.Add(countTxt);

            var totalRowGrid = new Grid { Margin = new Thickness(0, 2, 0, 4) };
            totalRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtTotalLabel = new TextBlock { Text = "Total", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.Black };
            var finalTotal = sale?.Total ?? 0m;
            var txtTotalVal = new TextBlock { Text = $"{finalTotal:0.00} ج.م", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };

            Grid.SetColumn(txtTotalLabel, 0); totalRowGrid.Children.Add(txtTotalLabel);
            Grid.SetColumn(txtTotalVal, 1); totalRowGrid.Children.Add(txtTotalVal);

            mainPanel.Children.Add(totalRowGrid);

            // Payment Breakdown
            mainPanel.Children.Add(CreateMetaRow("Cash", finalTotal.ToString("0.00")));
            mainPanel.Children.Add(CreateMetaRow("Tendered:", finalTotal.ToString("0.00")));
            mainPanel.Children.Add(CreateMetaRow("Change:", "0.00"));

            // 5 Small Bottom Hollow Circles
            var circlesStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 2)
            };
            for (int i = 0; i < 5; i++)
            {
                var circle = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Margin = new Thickness(3, 0, 3, 0)
                };
                circlesStack.Children.Add(circle);
            }
            mainPanel.Children.Add(circlesStack);

            container.Child = mainPanel;
            return container;
        }

        private UIElement CreateDottedLine()
        {
            var line = new Line
            {
                X1 = 0,
                X2 = 280,
                Y1 = 0,
                Y2 = 0,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                Margin = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return line;
        }

        private UIElement CreateMetaRow(string key, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtKey = new TextBlock { Text = key, FontSize = 11, Foreground = Brushes.Black };
            var txtVal = new TextBlock { Text = value, FontSize = 11, Foreground = Brushes.Black, FontWeight = FontWeights.Medium };

            Grid.SetColumn(txtKey, 0); grid.Children.Add(txtKey);
            Grid.SetColumn(txtVal, 1); grid.Children.Add(txtVal);

            return grid;
        }

        private void AddTableCell(Grid grid, string text, int col, FontWeight weight, TextAlignment align, bool isArabic = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = weight,
                Foreground = Brushes.Black,
                TextAlignment = align,
                FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(tb, col);
            Grid.SetRow(tb, 0);
            grid.Children.Add(tb);
        }

        public Task PrintReportAsync(string reportName, object data)
        {
            Debug.WriteLine($"Printing report: {reportName}");
            return Task.CompletedTask;
        }
    }
}
