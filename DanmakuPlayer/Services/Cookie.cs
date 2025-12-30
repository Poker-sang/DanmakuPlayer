namespace DanmakuPlayer.Services;

public class Cookie
{
    public string Domain { get; set; } = null!;

    public double ExpirationDate { get; set; }

    public bool HostOnly { get; set; }

    public bool HttpOnly { get; set; }

    public string Name { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string? SameSite { get; set; }

    public bool Secure { get; set; }

    public bool Session { get; set; }

    public string? StoreId { get; set; }

    public string Value { get; set; } = null!;
}
