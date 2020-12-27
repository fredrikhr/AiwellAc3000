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
        [SuppressMessage("Design",
            "CA1031: Do not catch general exception types",
            Justification = "REPL")]
        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            bool writeIntroduction = true;
            await Task.Delay(TimeSpan.FromSeconds(0.5), cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);

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
                const StringComparison invCompare = StringComparison.InvariantCultureIgnoreCase;
                try
                {
                    switch (interactiveCommand)
                    {
                        case string helpToken
                        when helpToken.Equals("help", invCompare):
                            HandleInteractiveHelpCommand();
                            break;

                        case string exitToken
                        when exitToken.Equals("exit", invCompare) ||
                            exitToken.Equals("quit", invCompare):
                            HandleInteractiveExitCommand();
                            return;

                        case string systemCountersToken
                        when systemCountersToken.Equals("getsystemcounters", invCompare):
                            await HandleInteractiveGetSystemCountersCommand(cancelToken)
                                .ConfigureAwait(continueOnCapturedContext: false);
                            break;

                        default:
                            Console.Error.WriteLine("Unrecognized command: {0}", interactiveCommand);
                            break;
                    }
                }
                catch (Exception except)
                {
                    logger.LogCritical(except,
                        $"Unhandled exception while executing interactive command: {{{nameof(interactiveCommand)}}}",
                        interactiveCommand);
                }
            }
        }

        private void HandleInteractiveExitCommand()
        {
            var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            lifetime.StopApplication();
        }

        private void HandleInteractiveHelpCommand()
        {
            throw new NotImplementedException();
        }

        private async Task HandleInteractiveGetSystemCountersCommand(
            CancellationToken cancelToken = default)
        {
            using var serviceScope = this.serviceProvider.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;
            var client = serviceProvider.GetRequiredService<Ac3000Client>();

            await client.ConnectAsync(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            var systemCounters = await client.GetSystemCounters(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            client.Disconnect();


        }
    }
}
