using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Utilities;

namespace DanmakuPlayer.Views.Controls;

public partial class RadioMenuFlyout : MenuFlyout
{
    [GeneratedDependencyProperty]
    public partial object? SelectedItem { get; set; }

    [GeneratedDependencyProperty]
    public partial object ItemsSource { get; set; }

    public string Formatter { get; set; } = "";

    public event Action<RadioMenuFlyout>? SelectionChanged;

    partial void OnSelectedItemPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (_suppressPropertyChanged)
            _suppressPropertyChanged = false;
        else
            foreach (var item in Items)
                item.To<RadioMenuFlyoutItem>().IsChecked = Equals(item.Tag, SelectedItem);

        SelectionChanged?.Invoke(this);
    }

    partial void OnItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged ncc)
            ncc.CollectionChanged += (_, _) => UpdateSource();
        UpdateSource();
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
