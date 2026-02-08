
    using System;
using System.Threading;
using System.Threading.Tasks;

public class ExpireNoticeService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public Task SendAllAsync()
    {
        return ExecuteAsync(SendType.All);
    }

    public Task SendAccountExpireMailAsync()
    {
        return ExecuteAsync(SendType.AccountExpireMail);
    }

    public Task SendAccountExpireSmsAsync()
    {
        return ExecuteAsync(SendType.AccountExpireSms);
    }

    public Task SendPermissionExpireMailAsync()
    {
        return ExecuteAsync(SendType.PermissionExpireMail);
    }

    private async Task ExecuteAsync(SendType type)
    {
        await _lock.WaitAsync();
        try
        {
            if (type == SendType.All)
            {
                await SafeExecute(SendAccountExpireMail);
                await SafeExecute(SendAccountExpireSms);
                await SafeExecute(SendPermissionExpireMail);
                return;
            }

            await ExecuteByType(type);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task ExecuteByType(SendType type)
    {
        switch (type)
        {
            case SendType.AccountExpireMail:
                return SafeExecute(SendAccountExpireMail);
            case SendType.AccountExpireSms:
                return SafeExecute(SendAccountExpireSms);
            case SendType.PermissionExpireMail:
                return SafeExecute(SendPermissionExpireMail);
            default:
                return Task.CompletedTask;
        }
    }

    private async Task SafeExecute(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }

    private Task SendAccountExpireMail()
    {
        return Task.CompletedTask;
    }

    private Task SendAccountExpireSms()
    {
        return Task.CompletedTask;
    }

    private Task SendPermissionExpireMail()
    {
        return Task.CompletedTask;
    }

    private void Log(Exception ex)
    {
    }
}

public enum SendType
{
    All = 0,
    AccountExpireMail = 1,
    AccountExpireSms = 2,
    PermissionExpireMail = 3
}
    