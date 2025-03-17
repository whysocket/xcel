namespace Xcel.Services.Email;

public class EmailOptions
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public required bool EnableSsl { get; set; }
}