using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.Messaging.RabbitMq.Internals;

public sealed class RabbitMqChannel : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private bool _disposed;

    public RabbitMqChannel(IOptions<RabbitMqOptions> options)
    {
        var settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.Username,
            Password = settings.Password,
            DispatchConsumersAsync = true,
            ClientProvidedName = settings.ClientProvidedName,
            Ssl = { Enabled = settings.UseSsl }
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, settings.PrefetchCount, false);
    }

    public IModel Model => _channel;

    public void Dispose()
    {
        if (_disposed)
            return;

        _channel.Dispose();
        _connection.Dispose();
        _disposed = true;
    }
}
