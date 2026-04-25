using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace CIS.DataAcces.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDb");
        var databaseName = configuration["MongoDb:DatabaseName"] ?? "CisDb";
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }
    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database  = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
        => _database.GetCollection<T>(collectionName);
}