namespace Swd392.Api.Infrastructure.Email;

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;
    public bool UseStartTls { get; set; } = false;
}
