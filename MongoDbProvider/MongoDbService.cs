using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MongoDbProvider
{
    public class MongoDbService : IMongoDbService
    {
        private string connString = string.Empty;
        public MongoDbService(IConfiguration config)
        {
            connString = Environment.GetEnvironmentVariable("Mongodb");
            if (connString == null)
            {
                throw new Exception("Mongo DB connection string is missing.");    
            }
            
        }
        public MongoClient GetMongoClient()
        {
            MongoClient mongoClient = new MongoClient(connString);
            return mongoClient;
        }
        public async Task InsertOrUpdateStock<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, T stock)
        {
            T oldStock =  collection.Find(filter).FirstOrDefault();
            if (oldStock == null)
            {
                await collection.InsertOneAsync(stock);
            }
            else
            {
                await collection.ReplaceOneAsync(filter, stock);
            }
        }
        public async Task<List<T>> GetAllData<T>(IMongoCollection<T> collection)
        {
            var filter = Builders<T>.Filter.Empty;
            return await collection.Find(filter).ToListAsync();
        }
        public async Task DeleteAndInsertManyData<T>(string collectionName, List<T> values)
        {
            MongoClient mongoClient = GetMongoClient();
            var collection = mongoClient.GetDatabase("MyFuture").GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Empty;
            await collection.DeleteManyAsync(filter);
            await collection.InsertManyAsync(values);
        }
    }
}