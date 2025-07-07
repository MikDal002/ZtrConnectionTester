using Microsoft.Extensions.DependencyInjection;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.PongCommand;

public static class SerialPongDependencyInjection
{
    public static IServiceCollection RegisterSerialPongCommand(this IServiceCollection services)
    {
        services.AddSingleton<ISerialPortEnumerator, DefaultSerialPortEnumerator>();
        services.AddTransient<IPongReturnService, PongReturnService>();
        services.AddLogging();

        return services;
    }
}
