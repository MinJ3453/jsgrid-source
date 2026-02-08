public class ExpireNoticeService
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task RunAsync(SendType type)
    {
        await _lock.WaitAsync();
        try
        {
            await SendByType(type);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task SendByType(SendType type)
        => type switch
        {
            SendType.AccountExpireMail => SendAccountExpireMail(),
            SendType.AccountExpireSms => SendAccountExpireSms(),
            SendType.PermissionExpireMail => SendPermissionExpireMail(),
            _ => Task.CompletedTask
        };

    private Task SendAccountExpireMail() { }
    private Task SendAccountExpireSms() { }
    private Task SendPermissionExpireMail() { }
}