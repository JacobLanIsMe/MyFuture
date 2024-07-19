using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
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
            int batchSize = 200;
            long count = await collection.CountDocumentsAsync(filter);
            int numBatches = (int)Math.Ceiling((double)count / batchSize);
            ConcurrentBag<T> data = new ConcurrentBag<T>();
            Parallel.ForEach(Partitioner.Create(0, numBatches), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var documents = collection.Find(filter)
                                              .Skip(i * batchSize)
                                              .Limit(batchSize)
                                              .ToList();

                    foreach (var doc in documents)
                    {
                        data.Add(doc);
                    }
                }
            });
            return data.ToList();
        }
        public async Task DropAndInsertManyData<T>(string collectionName, List<T> values)
        {
            MongoClient mongoClient = GetMongoClient();
            var db = mongoClient.GetDatabase("MyFuture");
            db.DropCollection(collectionName);
            var collection = db.GetCollection<T>(collectionName);
            int batchSize = 100;
            int totalBatches = (values.Count + batchSize - 1) / batchSize;
            List<List<T>> batches = new List<List<T>>();
            for (int i = 0; i < totalBatches; i++)
            {
                var batch = values.Skip(i * batchSize).Take(batchSize).ToList();
                batches.Add(batch);
            }
            Parallel.ForEach(batches, batch =>
            {
                collection.InsertMany(batch);
            });
            
        }
    }
}