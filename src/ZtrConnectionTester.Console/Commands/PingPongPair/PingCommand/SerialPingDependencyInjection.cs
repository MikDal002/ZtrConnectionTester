using Microsoft.Extensions.DependencyInjection;
using ZtrConnectionTester.Console.Commands.PingPongPair.Commons;

namespace ZtrConnectionTester.Console.Commands.PingPongPair.SerialPing;

public static class SerialPingDependencyInjection
{
    public static IServiceCollection RegisterSerialPingCommand(this IServiceCollection services)
    {
        services.AddSingleton<ISerialPortEnumerator, DefaultSerialPortEnumerator>();
        services.AddTransient<IPingDataCollector, InMemoryPingDataCollector>();

        return services;
    }
}
