using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Logging
{
    public class UserLogBackgroundService : BackgroundService
    {
        private readonly IUserLogQueue _logQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserLogBackgroundService> _logger;

        public UserLogBackgroundService(
            IUserLogQueue logQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<UserLogBackgroundService> logger)
        {
            _logQueue = logQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("UserLogBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _logQueue.DequeueAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<SearchToolDBContext>();

                    var log = new Log
                    {
                        UserEmail = item.UserEmail ?? item.UserId.ToString(),
                        Date = item.Date,
                        Action = item.Action,
                        Description = item.Description,
                        IpAddress = item.IpAddress,
                        DeviceInfo = item.DeviceInfo
                    };

                    dbContext.Logs.Add(log);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while saving user log from queue.");
                }
            }

            _logger.LogInformation("UserLogBackgroundService stopped.");
        }
    }
}