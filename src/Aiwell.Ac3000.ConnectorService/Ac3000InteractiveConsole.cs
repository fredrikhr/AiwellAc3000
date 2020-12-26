using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aiwell.Ac3000
{
    public class Ac3000InteractiveConsole : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        public Ac3000InteractiveConsole(
            IServiceProvider serviceProvider
            ) : base()
        {
            this.serviceProvider = serviceProvider ??
                throw new ArgumentNullException(nameof(serviceProvider));
            logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(GetType());
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            
        }
    }
}
