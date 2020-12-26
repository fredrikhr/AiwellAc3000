using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using THNETII.CommandLine.Hosting;

namespace Aiwell.Ac3000
{
    public static class Program
    {
        internal static readonly string TcpConfigurationPath = ConfigurationPath
            .Combine(
                nameof(Aiwell),
                nameof(Ac3000),
                "Tcp"
            );
        internal static readonly string SerialPortConfigurationPath = ConfigurationPath
            .Combine(
                nameof(Aiwell),
                nameof(Ac3000),
                "Serial"
            );

        private static readonly MethodInfo RunInvocationMethodInfo =
            typeof(Program).GetMethod(nameof(RunCommandLineInvocation),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public static RootCommand RootCommand { get; }
        public static Command TcpCommand { get; }
        public static Option<string> TcpHostOption { get; }
        public static Option<int> TcpPortOption { get; }
        public static Command SerialPortCommand { get; }
        public static Option<string> SerialPortPortOption { get; }

        static Program()
        {
            RootCommand = new RootCommand
            {
                Description = CommandLineHost.GetEntryAssemblyDescription(),
                Handler = CommandHandler.Create(RunInvocationMethodInfo),
                TreatUnmatchedTokensAsErrors = true,
            };

            TcpCommand = new Command("tcp")
            {
                Description = "Connect using a TCP/IP connection",
                Handler = RootCommand.Handler
            };
            TcpHostOption = new Option<string>("--host")
            {
                Description = "Hostname or IP-Address to connect to",
                Argument =
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Name = nameof(DnsEndPoint.Host)
                }
            };
            TcpHostOption.AddAlias("-r");
            TcpCommand.AddOption(TcpHostOption);
            TcpPortOption = new Option<int>("--port", () => 3000)
            {
                Description = "Port to connect to",
                Argument =
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Name = nameof(DnsEndPoint.Port)
                },
            };
            TcpPortOption.AddAlias("-p");
            TcpCommand.AddOption(TcpPortOption);

            SerialPortCommand = new Command("serial")
            {
                Description = "Connect using a serial port connection",
                Handler = RootCommand.Handler,
            };
            SerialPortPortOption = new Option<string>("--port")
            {
                Description = "Serial port name",
                Argument =
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Name = nameof(SerialPort.PortName),
                },
            };
            SerialPortPortOption.AddAlias("-p");
            SerialPortPortOption.AddSuggestions(SerialPort.GetPortNames());
            SerialPortCommand.AddOption(SerialPortPortOption);

            RootCommand.AddCommand(TcpCommand);
            RootCommand.AddCommand(SerialPortCommand);
        }

        public static Task<int> Main(string[]? args)
        {
            var parser = new CommandLineBuilder(RootCommand)
                .UseDefaults()
                .UseHost(CreateHostBuilder, ConfigureHostBuilder)
                .Build();
            return parser.InvokeAsync(args ?? Array.Empty<string>());
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = CommandLineHost.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);

            return hostBuilder;
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            if (hostContext.Properties.TryGetValue(typeof(InvocationContext), out var invocationObject) &&
                invocationObject is InvocationContext invocationContext)
            {
                switch (invocationContext.ParseResult.CommandResult.Command)
                {
                    case Command c when c == TcpCommand:
                        services.AddOptions<Ac3000TcpConnectOptions>()
                            .BindConfiguration(TcpConfigurationPath)
                            .BindCommandLine()
                            ;
                        services.AddScoped(serviceProvider =>
                        {
                            var tcpClient = new TcpClient();

                            var configuration = serviceProvider
                                .GetRequiredService<IConfiguration>();
                            configuration.Bind(TcpConfigurationPath, tcpClient);

                            var bindingContext = serviceProvider
                                .GetRequiredService<BindingContext>();
                            var modelBinder = ActivatorUtilities
                                .GetServiceOrCreateInstance<ModelBinder<TcpClient>>(serviceProvider);
                            modelBinder.UpdateInstance(tcpClient, bindingContext);

                            return tcpClient;
                        });
                        services.AddScoped<Ac3000BaseConnector, Ac3000TcpConnector>();
                        break;

                    case Command c when c == SerialPortCommand:
                        services.AddScoped(serviceProvider =>
                        {
                            var serialPort = new SerialPort();

                            var configuration = serviceProvider
                                .GetRequiredService<IConfiguration>();
                            configuration.Bind(SerialPortConfigurationPath, serialPort);

                            var bindingContext = serviceProvider
                                .GetRequiredService<BindingContext>();
                            var modelBinder = ActivatorUtilities
                                .GetServiceOrCreateInstance<ModelBinder<SerialPort>>(serviceProvider);
                            modelBinder.UpdateInstance(serialPort, bindingContext);

                            return serialPort;
                        });
                        services.AddScoped<Ac3000BaseConnector, Ac3000SerialPortConnector>();
                        break;
                }
            }
            services.AddScoped<Ac3000Client>();
            services.AddHostedService<Ac3000InteractiveConsole>();
        }

        private static void ConfigureHostBuilder(IHostBuilder hostBuilder)
        {
            hostBuilder.UseWindowsService();
        }

        private static async Task RunCommandLineInvocation(IHost host,
            CancellationToken cancelToken = default)
        {
            var serviceProvider = host.Services;
            var appLifetime = serviceProvider
                .GetRequiredService<IHostApplicationLifetime>();
            using var consoleCancellation = cancelToken.Register(state =>
            {
                if (state is not IHostApplicationLifetime lf)
                    return;
                lf.StopApplication();
            }, appLifetime);

            await host.WaitForShutdownAsync(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
