using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using WinUI3Utilities;
using WinUI3Utilities.Attributes;

namespace DanmakuPlayer.Views.Controls;

[DependencyProperty<double>("UserValue", "0d", nameof(OnUserValuePropertyChanged))]
public partial class VideoSlider : Slider
{
    public VideoSlider() => Style = Application.Current.Resources["DefaultSliderStyle"] as Style;

    public event EventHandler? SliderManipulationStarted;
    public event EventHandler? SliderManipulationCompleted;
    public event EventHandler? SliderManipulationMoved;
    public event EventHandler? UserValueChangedByManipulation;

    private bool IsSliderBeingManipulated => _isContainerHeld || _isThumbHeld;
    private bool _isThumbHeld;
    private bool _isContainerHeld;

    private static void OnUserValuePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var sliderEx = o.To<VideoSlider>();
        if (!sliderEx.IsSliderBeingManipulated)
            sliderEx.Value = sliderEx.UserValue;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if ((GetTemplateChild("HorizontalThumb") ?? GetTemplateChild("VerticalThumb")) is Thumb thumb)
        {
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragCompleted += Thumb_DragCompleted;
            thumb.DragDelta += Thumb_DragDelta;
        }

        if (GetTemplateChild("SliderContainer") is Grid sliderContainer)
        {
            sliderContainer.AddHandler(PointerPressedEvent, new PointerEventHandler(SliderContainer_PointerPressed), true);
            sliderContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(SliderContainer_PointerReleased), true);
            // sliderContainer.AddHandler(PointerMovedEvent, new PointerEventHandler(SliderContainer_PointerMoved), true);
        }
    }

    private void SliderContainer_PointerReleased(object sender, PointerRoutedEventArgs e) => SetContainerHeld(false);

    private void SliderContainer_PointerPressed(object sender, PointerRoutedEventArgs e) => SetContainerHeld(true);

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) => InvokeMove();

    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) => SetThumbHeld(false);

    private void Thumb_DragStarted(object sender, DragStartedEventArgs e) => SetThumbHeld(true);

    private void SetThumbHeld(bool held)
    {
        var wasManipulated = IsSliderBeingManipulated;
        _isThumbHeld = held;
        InvokeStateChange(wasManipulated);
    }

    private void SetContainerHeld(bool held)
    {
        var wasManipulated = IsSliderBeingManipulated;
        _isContainerHeld = held;
        InvokeStateChange(wasManipulated);
    }

    private void InvokeMove()
    {
        // 此时IsSliderBeingManipulated一定为true
        SliderManipulationMoved?.Invoke(this, EventArgs.Empty);
        UserValue = Value;
        UserValueChangedByManipulation?.Invoke(this, EventArgs.Empty);
    }

    private void InvokeStateChange(bool wasBeingManipulated)
    {
        if (wasBeingManipulated != IsSliderBeingManipulated)
        {
            if (IsSliderBeingManipulated)
            {
                SliderManipulationStarted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SliderManipulationCompleted?.Invoke(this, EventArgs.Empty);
                UserValue = Value;
                UserValueChangedByManipulation?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
