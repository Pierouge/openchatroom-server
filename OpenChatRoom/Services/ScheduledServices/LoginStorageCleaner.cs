public class LoginStorageCleaner : BackgroundService
{
  private readonly PeriodicTimer timer = new(TimeSpan.FromDays(1));
  private readonly ILoginStorage loginStorage;

  private readonly TimeSpan timespan = TimeSpan.FromMinutes(2);

  public LoginStorageCleaner(ILoginStorage loginStorage)
  {
    this.loginStorage = loginStorage;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
      try
      {
        await loginStorage.cleanDictionaryAsync(timespan);
      }
      catch (Exception) { }
    }
  }
}
