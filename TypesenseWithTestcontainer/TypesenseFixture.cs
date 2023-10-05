using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;
using Typesense;
using Typesense.Setup;
using Xunit;

namespace TypesenseWithTestcontainer;

public class TypesenseFixture: IAsyncLifetime
{
    private const int ContainerPort = 8108;
    private const string ApiKey = "typesense-api-key";

    public TypesenseFixture()
    {
        TypesenseContainer = new ContainerBuilder()
            .WithImage("typesense/typesense:0.25.1")
            .WithPortBinding(ContainerPort, true)
            .WithEnvironment("TYPESENSE_API_KEY", ApiKey)
            .WithEnvironment("TYPESENSE_DATA_DIR", "/tmp")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort(ContainerPort)
                    .ForPath("/health")
                    .WithHeader("TYPESENSE-API-KEY", ApiKey)
                )
            )
            .Build();
    }

    public Config ConnectionConfig { get; private set; }
    public IContainer TypesenseContainer { get; }

    public TypesenseClient GetClient()
    {
        var options = new OptionsWrapper<Config>(ConnectionConfig);
        var client = new TypesenseClient(options, new HttpClient());
        return client;
    }

    public async Task InitializeAsync()
    {
        await TypesenseContainer.StartAsync();
        
        var port = TypesenseContainer
            .GetMappedPublicPort(ContainerPort)
            .ToString();
        
        ConnectionConfig = new Config(
            new Node[] { new(TypesenseContainer.Hostname, port) },
            ApiKey
        );
    }

    public Task DisposeAsync()
        => TypesenseContainer.DisposeAsync().AsTask();
}