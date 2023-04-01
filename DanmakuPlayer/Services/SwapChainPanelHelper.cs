using Microsoft.UI.Xaml.Controls;
using SharpGen.Runtime;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using WinUI3Utilities;

namespace DanmakuPlayer.Services;

public static class SwapChainPanelHelper
{
    private static ID3D12Device _d3D12Device = null!;
    private static IDXGIAdapter1 _dxgiAdapter1 = null!;
    private static IDXGIFactory5 _dxgiFactory5 = null!;
    private static IDXGISwapChain1 _dxgiSwapChain1 = null!;

    public static void SetSwapChainPanel(SwapChainPanel swapChainPanel, nint hWnd)
    {
        _d3D12Device = CreateDevice(out _dxgiFactory5, out _dxgiAdapter1);
        _dxgiSwapChain1 = CreateSwapChain(0, _d3D12Device, _dxgiFactory5);

        var panelNative = ComObject.As<Vortice.WinUI.ISwapChainPanelNative>(swapChainPanel);
        _ = panelNative.SetSwapChain(_dxgiSwapChain1);

        var exStyle = (User32.WindowStylesEx)User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);
        // var style = (User32.WindowStyles)User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);
        if (!exStyle.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED))
        {
            // _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE, (int)style.SetFlags(User32.WindowStyles.WS_THICKFRAME));
            _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, (int)exStyle.SetFlags(User32.WindowStylesEx.WS_EX_LAYERED));
        }
    }

    private static ID3D12Device CreateDevice(out IDXGIFactory5 dxgiFactory5, out IDXGIAdapter1 dxgiAdapter1)
    {
        dxgiFactory5 = DXGI.CreateDXGIFactory2<IDXGIFactory5>(false);

        for (var adapterIndex = 0; dxgiFactory5.EnumAdapters1(adapterIndex, out dxgiAdapter1) == Result.Ok; ++adapterIndex)
        {
            var desc = dxgiAdapter1.Description1;
            if ((desc.Flags & AdapterFlags.Software) != 0)
            {
                dxgiAdapter1.Dispose();
                continue;
            }
            return D3D12.D3D12CreateDevice<ID3D12Device>(dxgiAdapter1, FeatureLevel.Level_12_1);
        }

        return ThrowHelper.NotSupported<ID3D12Device>("No available adapter.");
    }

    private static IDXGISwapChain1 CreateSwapChain(nint hWnd, ID3D12Device d3D12Device, IDXGIFactory5 dxgiFactory5)
    {
        var queueDesc = new CommandQueueDescription { Type = CommandListType.Direct };
        var queue = d3D12Device.CreateCommandQueue(queueDesc);
        var swapChainDesc = new SwapChainDescription1
        {
            Width = 1024,
            Height = 768,
            // this is the most common swap chain format
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            // don't use multi-sampling
            SampleDescription = new()
            {
                Count = 1,
                Quality = 0
            },
            BufferUsage = Usage.RenderTargetOutput,
            // use double buffering to enable flip
            BufferCount = 2,
            Scaling = (hWnd is 0) ? Scaling.Stretch : Scaling.None,
            // all apps must use this SwapEffect
            SwapEffect = SwapEffect.FlipSequential,
            Flags = 0
        };

        var dxgiSwapChain1 = hWnd != 0
              ? dxgiFactory5.CreateSwapChainForHwnd(queue, hWnd, swapChainDesc)
              : dxgiFactory5.CreateSwapChainForComposition(queue, swapChainDesc);

        return dxgiSwapChain1;
    }

    private static bool SetPictureToLayeredWindow(HWND hWnd, nint hBitmap, SIZE sizeBitmap, POINT pos)
    {
        var hDcScreen = User32.GetDC(HWND.NULL);
        var hDcMem = Gdi32.CreateCompatibleDC(hDcScreen);
        var hBitmapOld = Gdi32.SelectObject(hDcMem, hBitmap);

        var bf = new Gdi32.BLENDFUNCTION
        {
            BlendOp = 0, // AcSrcOver
            SourceConstantAlpha = 255,
            AlphaFormat = 1 // AcSrcAlpha
        };

        var bRet = User32.UpdateLayeredWindow(hWnd, hDcScreen, pos, sizeBitmap, hDcMem, new(), 0, bf, User32.UpdateLayeredWindowFlags.ULW_ALPHA);

        _ = Gdi32.SelectObject(hDcMem, hBitmapOld);
        _ = Gdi32.DeleteDC(hDcMem);
        _ = User32.ReleaseDC(HWND.NULL, hDcScreen);

        return bRet;
    }

    public static void Dispose()
    {
        _dxgiFactory5.Dispose();
        _dxgiAdapter1.Dispose();
        _d3D12Device.Dispose();
        _dxgiSwapChain1.Dispose();
    }
}
