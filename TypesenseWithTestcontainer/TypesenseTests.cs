using System.Text.Json.Serialization;
using Typesense;
using Xunit;
using Xunit.Abstractions;

namespace TypesenseWithTestcontainer;

public class TypesenseTests(TypesenseFixture fixture, ITestOutputHelper output) :
    IClassFixture<TypesenseFixture>, IAsyncLifetime
{
    [Fact]
    public async Task CanQueryTypesenseForHealth()
    {
        var client = fixture.GetClient();
        var result = await client.RetrieveHealth();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CanSearchForProducts()
    {
        var client = fixture.GetClient();

        var results = await client
            .Search<Product>(
                nameof(Product),
                new("Sony", nameof(Product.Manufacturer))
            );
        
        Assert.Equal(1, results.Hits.Count);
        
        var product = results.Hits[0].Document;
        Assert.Equal("Sony", product.Manufacturer);
        
        output.WriteLine($"Found {product.Manufacturer} {product.Name} ({product.Price:C})");
    }

    public class Product(string id, string name, string manufacturer, double price)
    {
        [JsonPropertyName(nameof(Id))]
        public string Id { get; set; } = id;
        [JsonPropertyName(nameof(Name))]
        public string Name { get; set; } = name;
        [JsonPropertyName(nameof(Manufacturer))]
        public string Manufacturer { get; set; } = manufacturer;
        [JsonPropertyName(nameof(Price))]
        public double Price { get; set; } = price;
        
        public static Product[] Samples { get; } = {
            new("1", "iPhone 15", "Apple", 1500),
            new("2", "Pixel 8 Pro", "Google", 1300),
            new("3", "Playstation 5", "Sony", 500),
            new("4", "XBox Series X", "Xbox", 500),
            new("5", "Switch", "Nintendo", 300)
        };
    }

    public async Task InitializeAsync()
    {
        var client = fixture.GetClient();

        var schema = new Schema(nameof(Product), new Field[]
        {
            new(nameof(Product.Id), FieldType.String),
            new(nameof(Product.Name), FieldType.String, false),
            new(nameof(Product.Manufacturer), FieldType.String, true, false, true),
            new(nameof(Product.Price), FieldType.Float, false)
        });

        await client.CreateCollection(schema);

        foreach (var product in Product.Samples) {
            await client.CreateDocument(nameof(Product), product);
        }
    }

    public async Task DisposeAsync()
    {
        var client = fixture.GetClient();
        await client.DeleteCollection(nameof(Product));
    }
}