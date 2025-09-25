using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DanmakuPlayer.Services;

public static class HttpClientHelper
{
    public static bool ShouldRefreshHeader { get; set; } = true;

    public static HttpClient Client { get; } =
        new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate,
            // ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        {
            Timeout = new(0, 0, 0, 8000),
            MaxResponseContentBufferSize = ((long)2 << 30) - 1
        };

    public static void Initialize() { }

    public static HttpClient InitializeHeader(this HttpClient client, Dictionary<string, string>? tempHeader = null)
    {
        if (!ShouldRefreshHeader)
            return client;

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new("Mozilla", "5.0"));
        client.DefaultRequestHeaders.UserAgent.Add(new("DanmakuPlayer", "0"));
        if (AppContext.AppConfig.DanmakuCookie is { Count: > 0 } cookies)
            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Select(t => $"{t.Key}={t.Value}")));
        ShouldRefreshHeader = false;
        if (tempHeader is not null)
        {
            ShouldRefreshHeader = true;
            foreach (var (k, v) in tempHeader)
                client.DefaultRequestHeaders.Add(k, v);
        }

        Debug.WriteLine($"[{nameof(HttpClientHelper)}]::{nameof(InitializeHeader)}(): Header: [");
        foreach (var i in client.DefaultRequestHeaders)
            Debug.WriteLine($"  {i.Key}:{string.Join(';', i.Value)},");

        Debug.WriteLine("]");
        return client;
    }

    public static Task<string> DownloadStringAsync(this string uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            return Client.InitializeHeader(header).GetStringAsync(uri, token);
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static Task<string> DownloadStringAsync(this Uri uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            return Client.InitializeHeader(header).GetStringAsync(uri, token);
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static Task<Stream> DownloadStreamAsync(this string uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            return Client.InitializeHeader(header).GetStreamAsync(uri, token);
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static Task<Stream> DownloadStreamAsync(this Uri uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            return Client.InitializeHeader(header).GetStreamAsync(uri, token);
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static async Task<Stream?> TryDownloadStreamAsync(this string uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            var response = await Client.InitializeHeader(header).GetAsync(uri, token);
            return response.IsSuccessStatusCode ? await Client.InitializeHeader(header).GetStreamAsync(uri, token) : null;
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static Task<byte[]> DownloadBytesAsync(this string uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        Debug.WriteLine("Requesting uri: " + uri);
        try
        {
            return Client.InitializeHeader(header).GetByteArrayAsync(uri, token);
        }
        catch (Exception e)
        {
            HttpClient.DefaultProxy = CurrentSystemProxy;
            throw;
        }
    }

    public static async Task<JsonDocument> DownloadJsonAsync(this string uri, CancellationToken token, Dictionary<string, string>? header = null)
    {
        await using var download = await uri.DownloadStreamAsync(token, header);
        return await JsonDocument.ParseAsync(download, default, token);
    }

    [DynamicDependency("ConstructSystemProxy", "SystemProxyInfo", "System.Net.Http")]
    static HttpClientHelper()
    {
        var type = typeof(HttpClient).Assembly.GetType("System.Net.Http.SystemProxyInfo");
        var method = type?.GetMethod("ConstructSystemProxy");
        var @delegate = method?.CreateDelegate<Func<IWebProxy>>();

        _GetCurrentSystemProxy = @delegate ?? throw new MissingMethodException("Unable to find proxy functions");
        HttpClient.DefaultProxy = _GetCurrentSystemProxy();
    }

    private static readonly Func<IWebProxy> _GetCurrentSystemProxy;

    public static IWebProxy CurrentSystemProxy
    {
        get
        {
            var now = DateTime.UtcNow;
            if (now < CoolDown)
                return HttpClient.DefaultProxy;
            CoolDown = now.AddSeconds(2);
            return _GetCurrentSystemProxy();
        }
    }

    private static DateTime CoolDown { get; set; } = DateTime.UtcNow.AddSeconds(2);
}
