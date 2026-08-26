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
                Width = 310,
                Background = Brushes.White,
                Padding = new Thickness(14),
                FlowDirection = FlowDirection.LeftToRight,
                SnapsToDevicePixels = true
            };

            var mainPanel = new StackPanel();

            // 1. CIRCULAR LOGO BRANDING (VELOURA + BUTTERFLY)
            var logoBorder = new Border
            {
                Width = 150,
                Height = 150,
                CornerRadius = new CornerRadius(75),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var logoGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // VELOURA + Butterfly Row
            var brandHeaderGrid = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Butterfly Icon Path 🦋
            var butterflyPath = new Path
            {
                Data = Geometry.Parse("M 12,2 C 10,2 8,4 8,7 C 6,4 4,4 2,6 C 0,8 1,12 4,13 C 1,15 0,18 2,20 C 4,22 8,20 10,17 C 10,19 11,21 12,21 C 13,21 14,19 14,17 C 16,20 20,22 22,20 C 24,18 23,15 20,13 C 23,12 24,8 22,6 C 20,4 18,4 16,7 C 16,4 14,2 12,2 Z"),
                Fill = Brushes.Black,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandHeaderGrid.Children.Add(butterflyPath);

            var txtBrand = new TextBlock
            {
                Text = "VELOURA",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Georgia, Times New Roman, Segoe UI"),
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            brandHeaderGrid.Children.Add(txtBrand);

            logoGrid.Children.Add(brandHeaderGrid);

            logoBorder.Child = logoGrid;
            mainPanel.Children.Add(logoBorder);

            // Address below circle
            var txtAddress1 = new TextBlock
            {
                Text = "العاشر من رمضان - مجاورة الخامسة",
                FontSize = 11,
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
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(0, 2, 0, 4)
            };
            mainPanel.Children.Add(txtAddress2);

            // Phone & Social Contacts below Address
            var txtPhone = new TextBlock
            {
                Text = "📞 01509922025   💬 01509922025",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            };
            mainPanel.Children.Add(txtPhone);

            var txtTiktok = new TextBlock
            {
                Text = "🎵 Veloura.clothing",
                FontSize = 10,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainPanel.Children.Add(txtTiktok);

            // Dotted Separator 1
            mainPanel.Children.Add(CreateDottedLine());

            // 2. BON NR BLOCK
            var bonGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            bonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtBonLabel = new TextBlock
            {
                Text = "Bon Nr:",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtBonLabel, 0);
            bonGrid.Children.Add(txtBonLabel);

            var txtBonValue = new TextBlock
            {
                Text = saleId.ToString(),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtBonValue, 1);
            bonGrid.Children.Add(txtBonValue);

            mainPanel.Children.Add(bonGrid);
            mainPanel.Children.Add(CreateDottedLine());

            // 3. RECEIPT METADATA
            mainPanel.Children.Add(CreateMetaRow("Receipt:", saleId.ToString()));
            mainPanel.Children.Add(CreateMetaRow("Date:", (sale?.Date ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm")));
            mainPanel.Children.Add(CreateMetaRow("Terminal:", "Cash-PC"));
            mainPanel.Children.Add(CreateMetaRow("Payment Method:", sale?.Payments?.FirstOrDefault()?.PaymentMethod ?? "نقداً (Cash)"));
            mainPanel.Children.Add(CreateMetaRow("Served by:", "Manager"));
            mainPanel.Children.Add(CreateDottedLine());

            // 4. ITEMS TABLE
            var headerGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });

            AddTableCell(headerGrid, "Item", 0, FontWeights.Bold, TextAlignment.Left);
            AddTableCell(headerGrid, "Price", 1, FontWeights.Bold, TextAlignment.Right);
            AddTableCell(headerGrid, "Qty", 2, FontWeights.Bold, TextAlignment.Center);
            AddTableCell(headerGrid, "Value", 3, FontWeights.Bold, TextAlignment.Right);

            mainPanel.Children.Add(headerGrid);
            mainPanel.Children.Add(CreateDottedLine());

            // Items List
            if (sale?.LineItems != null)
            {
                foreach (var item in sale.LineItems)
                {
                    var itemGrid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });

                    var productName = item.Product?.NameAR ?? item.Product?.NameEN ?? $"منتج #{item.ProductId}";
                    var lineTotal = item.Quantity * item.UnitPrice;

                    AddTableCell(itemGrid, productName, 0, FontWeights.Normal, TextAlignment.Right, isArabic: true);
                    AddTableCell(itemGrid, item.UnitPrice.ToString("0.00"), 1, FontWeights.Normal, TextAlignment.Right);
                    AddTableCell(itemGrid, $"x{item.Quantity}", 2, FontWeights.Normal, TextAlignment.Center);
                    AddTableCell(itemGrid, lineTotal.ToString("0.00"), 3, FontWeights.Normal, TextAlignment.Right);

                    mainPanel.Children.Add(itemGrid);
                }
            }

            mainPanel.Children.Add(CreateDottedLine());

            // 5. TOTALS
            var countTxt = new TextBlock
            {
                Text = $"Items count: {sale?.LineItems?.Sum(i => i.Quantity) ?? 0}",
                FontSize = 11,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 4, 0, 6)
            };
            mainPanel.Children.Add(countTxt);

            var totalRowGrid = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            totalRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtTotalLabel = new TextBlock { Text = "Total", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.Black };
            var finalTotal = sale?.Total ?? 0m;
            var txtTotalVal = new TextBlock { Text = finalTotal.ToString("0.00"), FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };

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
                Margin = new Thickness(0, 14, 0, 4)
            };
            for (int i = 0; i < 5; i++)
            {
                var circle = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5,
                    Margin = new Thickness(4, 0, 4, 0)
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
