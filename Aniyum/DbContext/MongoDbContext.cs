using Aniyum.Utils;
using MongoDB.Driver;

namespace Aniyum.DbContext;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private MongoClient MongoClient { get; set; }
    public IClientSessionHandle Session { get; set; }
    private readonly List<Func<Task>> _commands;


    public MongoDbContext()
    {
        MongoClient = new MongoClient("");
        _database = MongoClient.GetDatabase(AppSettingConfig.Configuration["MongoDBSettings:DatabaseName"]);
        _commands = new List<Func<Task>>();
    }

    public IMongoCollection<TEntity> GetCollection<TEntity>(string name)
    {
        return _database.GetCollection<TEntity>(name);
    }
    
    public void Dispose()
    {
        Session?.Dispose();
        GC.SuppressFinalize(this);    
    }
    
    public async Task<int> SaveChanges()
    {
        using (Session = await MongoClient.StartSessionAsync())
        {
            Session.StartTransaction();
            var commandTasks = _commands.Select(c => c());
            await Task.WhenAll(commandTasks);

            await Session.CommitTransactionAsync();
        }
        
        return _commands.Count;
    }
}