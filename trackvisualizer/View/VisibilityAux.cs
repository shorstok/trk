using System.Windows;

namespace trackvisualizer.View
{
    public static class VisibilityAux
    {
        public static readonly DependencyProperty IsVisibleProperty
            = DependencyProperty.RegisterAttached(
                "IsVisible",
                typeof(bool?),
                typeof(VisibilityAux),
                new FrameworkPropertyMetadata(default(bool?),
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    IsVisibleChangedCallback));

        public static readonly DependencyProperty IsCollapsedProperty
            = DependencyProperty.RegisterAttached(
                "IsCollapsed",
                typeof(bool?),
                typeof(VisibilityAux),
                new FrameworkPropertyMetadata(default(bool?),
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    IsCollapsedChangedCallback));

        public static readonly DependencyProperty IsHiddenProperty
            = DependencyProperty.RegisterAttached(
                "IsHidden",
                typeof(bool?),
                typeof(VisibilityAux),
                new FrameworkPropertyMetadata(default(bool?),
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    IsHiddenChangedCallback));

        private static void IsVisibleChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe))
                return;

            fe.Visibility = (bool?) e.NewValue == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public static void SetIsVisible(DependencyObject element, bool? value) => element.SetValue(IsVisibleProperty, value);

        public static bool? GetIsVisible(DependencyObject element) => (bool?) element.GetValue(IsVisibleProperty);

        private static void IsCollapsedChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe))
                return;

            fe.Visibility = (bool?) e.NewValue == true
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public static void SetIsCollapsed(DependencyObject element, bool? value) => element.SetValue(IsCollapsedProperty, value);

        public static bool? GetIsCollapsed(DependencyObject element) => (bool?) element.GetValue(IsCollapsedProperty);

        private static void IsHiddenChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe))
                return;

            fe.Visibility = (bool?) e.NewValue == true
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        public static void SetIsHidden(DependencyObject element, bool? value) => element.SetValue(IsHiddenProperty, value);

        public static bool? GetIsHidden(DependencyObject element) => (bool?) element.GetValue(IsHiddenProperty);
    }
}