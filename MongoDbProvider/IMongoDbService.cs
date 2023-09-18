using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDbProvider
{
    public interface IMongoDbService
    {
        MongoClient GetMongoClient();
        Task InsertOrUpdateStock<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, T stock);
    }
}