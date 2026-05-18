using RabbitMQ.Client;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqPublisherChannel : IAsyncDisposable
{
    public IChannel Channel { get; }

    private RabbitMqPublisherChannel(IChannel channel)
    {
        Channel = channel;
    }

    public static async Task<RabbitMqPublisherChannel> CreateAsync(
        IConnection connection,
        CancellationToken cancellationToken = default)
    {
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: null);

        var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        return new RabbitMqPublisherChannel(channel);
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}
