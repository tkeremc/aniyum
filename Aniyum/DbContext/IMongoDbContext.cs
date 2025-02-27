using MongoDB.Driver;

namespace Aniyum.DbContext;

public interface IMongoDbContext
{
    IMongoCollection<TEntity> GetCollection<TEntity>(string name);
    Task<int> SaveChanges();
}