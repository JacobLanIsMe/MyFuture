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
        private string? connString = null;
        private MongoClient? mongoClient = null;
        public MongoDbService(IConfiguration config)
        {
            var connString = config.GetConnectionString("Mongo");
            if (connString == null)
            {
                throw new Exception("Mongo DB connection string is missing.");    
            }
            mongoClient = new MongoClient(connString);
        }
        public MongoClient GetMongoClient()
        {
            if (this.mongoClient == null)
            {
                throw new Exception("Mongo DB connection fails.");
            }
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
                Type objectType = typeof(T);
                PropertyInfo propertyInfo = objectType.GetProperty("Id");
                if (propertyInfo != null)
                {
                    var oldId = propertyInfo.GetValue(oldStock);
                    propertyInfo.SetValue(stock, oldId);
                    await collection.ReplaceOneAsync(filter, stock);
                }
            }
        }
    }
}