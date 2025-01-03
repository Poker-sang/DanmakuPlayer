using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.Controls;

[DependencyProperty<object>("SelectedItem", DependencyPropertyDefaultValue.Default, nameof(OnSelectedItemChanged), IsNullable = true)]
[DependencyProperty<object>("ItemsSource", DependencyPropertyDefaultValue.Default, nameof(OnItemsSourceChanged))]
public partial class RadioMenuFlyout : MenuFlyout
{
    public string Formatter { get; set; } = "";

    public event Action<RadioMenuFlyout>? SelectionChanged;

    public static void OnSelectedItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var flyout = o.To<RadioMenuFlyout>();
        if (flyout._suppressPropertyChanged)
            flyout._suppressPropertyChanged = false;
        else
            foreach (var item in flyout.Items)
                item.To<RadioMenuFlyoutItem>().IsChecked = Equals(item.Tag, flyout.SelectedItem);

        flyout.SelectionChanged?.Invoke(flyout);
    }

    public static void OnItemsSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var flyout = o.To<RadioMenuFlyout>();
        if (flyout.ItemsSource is INotifyCollectionChanged ncc)
            ncc.CollectionChanged += (_, _) => flyout.UpdateSource();
        flyout.UpdateSource();
    }

    private void UpdateSource()
    {
        Items.Clear();
        if (ItemsSource is not IEnumerable source)
            return;
        foreach (var item in source)
        {
            var flyoutItem = new RadioMenuFlyoutItem
            {
                Text = string.IsNullOrEmpty(Formatter)
                    ? item.ToString()
                    : string.Format(Formatter, item),
                Tag = item
            };
            flyoutItem.Click += RadioMenuFlyoutItem_Click;
            Items.Add(flyoutItem);
        }

        if (SelectedItem is { } selectedItem)
            foreach (var item in Items)
                item.To<RadioMenuFlyoutItem>().IsChecked = Equals(item.Tag, selectedItem);
    }

    private async void RadioMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        await Task.Yield();
        _suppressPropertyChanged = true;
        SelectedItem = sender.To<FrameworkElement>().Tag;
    }

    private bool _suppressPropertyChanged;
}
