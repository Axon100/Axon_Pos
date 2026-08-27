using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Axon.UI.Helpers
{
    public static class SmoothScrollHelper
    {
        public static readonly DependencyProperty IsSmoothScrollEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSmoothScrollEnabled",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnIsSmoothScrollEnabledChanged));

        public static bool GetIsSmoothScrollEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSmoothScrollEnabledProperty);
        public static void SetIsSmoothScrollEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSmoothScrollEnabledProperty, value);

        private static readonly DependencyProperty CurrentVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "CurrentVerticalOffset",
                typeof(double),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(0.0, OnCurrentVerticalOffsetChanged));

        private static double GetCurrentVerticalOffset(DependencyObject obj) => (double)obj.GetValue(CurrentVerticalOffsetProperty);
        private static void SetCurrentVerticalOffset(DependencyObject obj, double value) => obj.SetValue(CurrentVerticalOffsetProperty, value);

        private static void OnIsSmoothScrollEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                }
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && scrollViewer.ScrollableHeight > 0)
            {
                double currentTarget = GetCurrentVerticalOffset(scrollViewer);
                if (Math.Abs(currentTarget - scrollViewer.VerticalOffset) > 300 || currentTarget == 0)
                {
                    currentTarget = scrollViewer.VerticalOffset;
                }

                double newTarget = currentTarget - (e.Delta * 2.6);
                newTarget = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, newTarget));

                SetCurrentVerticalOffset(scrollViewer, newTarget);

                var animation = new DoubleAnimation
                {
                    From = scrollViewer.VerticalOffset,
                    To = newTarget,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                scrollViewer.BeginAnimation(CurrentVerticalOffsetProperty, animation);
                e.Handled = true;
            }
        }

        private static void OnCurrentVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
