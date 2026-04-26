using RabbitMQ.Client;
using Shared.Contracts.Messaging;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqChannel : IAsyncDisposable
{
    private readonly IConnection _connection;

    private RabbitMqChannel(IConnection connection, IChannel channel)
    {
        _connection = connection;
        Channel = channel;
    }

    public IChannel Channel { get; }

    public static async Task<RabbitMqChannel> CreateAsync(
        ConnectionFactory factory,
        CancellationToken cancellationToken = default)
    {
        var connection = await factory.CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            ExchangeNames.Analysis,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        return new RabbitMqChannel(connection, channel);
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
