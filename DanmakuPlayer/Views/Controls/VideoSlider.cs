using System;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DanmakuPlayer.Views.Controls;

/// <summary>
/// 方便追踪用户鼠标操作的<see cref="Slider"/>
/// </summary>
public partial class VideoSlider : Slider
{
    [GeneratedDependencyProperty]
    public partial double UserValue { get; set; }

    public VideoSlider() => Style = Application.Current.Resources["DefaultSliderStyle"] as Style;

    public event EventHandler? SliderManipulationStarted;
    public event EventHandler? SliderManipulationCompleted;
    public event EventHandler? SliderManipulationDelta;
    public event EventHandler? UserValueChangedByManipulation;

    private bool IsSliderBeingManipulated => _isContainerHeld || _isThumbHeld;
    private bool _isThumbHeld;
    private bool _isContainerHeld;

    partial void OnUserValuePropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (!IsSliderBeingManipulated)
            Value = UserValue;
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
        // 拖动过程中触发
        // 此时IsSliderBeingManipulated一定为true
        SliderManipulationDelta?.Invoke(this, EventArgs.Empty);
        UserValue = Value;
        UserValueChangedByManipulation?.Invoke(this, EventArgs.Empty);
    }

    private void InvokeStateChange(bool wasBeingManipulated)
    {
        if (wasBeingManipulated != IsSliderBeingManipulated)
            if (IsSliderBeingManipulated)
                // 点击进度条时触发
                SliderManipulationStarted?.Invoke(this, EventArgs.Empty);
            else
            {
                // 松开时触发
                UserValue = Value;
                SliderManipulationCompleted?.Invoke(this, EventArgs.Empty);
                UserValueChangedByManipulation?.Invoke(this, EventArgs.Empty);
            }
    }
}
