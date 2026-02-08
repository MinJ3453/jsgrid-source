using System;
using System.Threading;
using System.Threading.Tasks;

public class ExpireNoticeService : IExpireNoticeService
{
    private readonly IMailSendService _mailService;
    private readonly ISmsSendService _smsService;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public ExpireNoticeService(
        IMailSendService mailService,
        ISmsSendService smsService)
    {
        _mailService = mailService;
        _smsService = smsService;
    }

    public Task SendAccountExpireMailAsync()
    {
        return ExecuteAsync(_mailService.SendAccountExpireMailAsync);
    }

    public Task SendAccountExpireSmsAsync()
    {
        return ExecuteAsync(_smsService.SendAccountExpireSmsAsync);
    }

    public Task SendPermissionExpireMailAsync()
    {
        return ExecuteAsync(_mailService.SendPermissionExpireMailAsync);
    }

    public Task SendAllAsync()
    {
        return ExecuteAllAsync();
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        await _lock.WaitAsync();
        try
        {
            await SafeExecute(action);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task ExecuteAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await SafeExecute(_mailService.SendAccountExpireMailAsync);
            await SafeExecute(_smsService.SendAccountExpireSmsAsync);
            await SafeExecute(_mailService.SendPermissionExpireMailAsync);
        }
        finally
        {
            _lock.Release();
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

    private void Log(Exception ex)
    {
    }
}