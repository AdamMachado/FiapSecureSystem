using RabbitMQ.Client;

namespace ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    public IConnection Connection { get; }

    private RabbitMqConnection(IConnection connection)
    {
        Connection = connection;
    }

    public static async Task<RabbitMqConnection> CreateAsync(
        ConnectionFactory factory,
        CancellationToken cancellationToken = default)
    {
        var connection = await factory.CreateConnectionAsync(cancellationToken);
        return new RabbitMqConnection(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
