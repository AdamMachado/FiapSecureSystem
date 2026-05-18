using RabbitMQ.Client;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqConsumerChannel : IAsyncDisposable
{
    public IChannel Channel { get; }

    private RabbitMqConsumerChannel(IChannel channel)
    {
        Channel = channel;
    }

    public static async Task<RabbitMqConsumerChannel> CreateAsync(
        IConnection connection,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken = default)
    {
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: false,
            publisherConfirmationTrackingEnabled: false,
            outstandingPublisherConfirmationsRateLimiter: null,
            consumerDispatchConcurrency: consumerDispatchConcurrency);

        var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        return new RabbitMqConsumerChannel(channel);
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}
