//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SmartRuntimeService –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    #endregion Using

    public sealed class SmartRuntimeService : BackgroundService
    {
        private readonly ILogger<SmartRuntimeService> _logger;

        public SmartRuntimeService(ILogger<SmartRuntimeService> logger)
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
