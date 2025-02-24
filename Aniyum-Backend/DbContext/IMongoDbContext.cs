using MongoDB.Driver;

namespace Aniyum_Backend.DbContext;

public interface IMongoDbContext
{
    IMongoCollection<TEntity> GetCollection<TEntity>(string name);
    Task<int> SaveChanges();
}