//--------------------------------------------------------------------------------------------------
// (C) 2017-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// ќписание: SmartRuntime Ц
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.RFID
{
    public class RfidMonitorService : BackgroundService
    {
        private readonly ILogger<RfidMonitorService> _logger;

        public RfidMonitorService(ILogger<RfidMonitorService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}