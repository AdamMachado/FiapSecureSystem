using RabbitMQ.Client;
using Shared.Contracts.Messaging;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqChannel : IAsyncDisposable
{
    private readonly IConnection _connection;
    public IChannel Channel { get; }

    private RabbitMqChannel(IConnection connection, IChannel channel)
    {
        _connection = connection;
        Channel = channel;
    }

    public static async Task<RabbitMqChannel> CreateAsync(
        ConnectionFactory factory,
        CancellationToken cancellationToken = default)
    {
        var connection = await factory.CreateConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: null);

        var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

        return new RabbitMqChannel(connection, channel);
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
