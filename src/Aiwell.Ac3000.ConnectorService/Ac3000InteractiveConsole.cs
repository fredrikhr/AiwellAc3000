using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using THNETII.CommandLine.Extensions;

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

        [SuppressMessage("Globalization",
            "CA1303: Do not pass literals as localized parameters",
            Justification = nameof(Console.Write))]
        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            bool writeIntroduction = true;

            static void EnsureIntroductionWritten(ref bool shouldWrite)
            {
                if (!shouldWrite)
                    return;

                Console.WriteLine("Enter help to show the help page with a list of interactive commands.");
                Console.WriteLine("Enter quit to exit the application.");
                shouldWrite = false;
            }


            while (!cancelToken.IsCancellationRequested)
            {
                EnsureIntroductionWritten(ref writeIntroduction);
                Console.Write("> ");
                var interactiveResponse = await ConsoleUtils.ReadLineAsync(cancelToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                var interactiveTokens = interactiveResponse
                    .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                if (interactiveTokens.Length < 1)
                    continue;
                var interactiveCommand = interactiveTokens[0];
            }
        }
    }
}
