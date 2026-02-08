using System;
using System.Threading;
using System.Threading.Tasks;

public enum SendType
{
    All = 0,
    AccountExpireMail = 1,
    AccountExpireSms = 2,
    PermissionExpireMail = 3
}

public class ExpireNoticeService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public async Task RunAsync(SendType type)
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
            SendType.AccountExpireMail => SafeExecute(SendAccountExpireMail),
            SendType.AccountExpireSms => SafeExecute(SendAccountExpireSms),
            SendType.PermissionExpireMail => SafeExecute(SendPermissionExpireMail),
            _ => Task.CompletedTask
        };

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