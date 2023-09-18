using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MongoFactory.Factorys
{
    public class Factory
    {
        private string connString = null;
        public Factory(IConfiguration config)
        {
            connString = config.GetConnectionString("Mongo");
            if (connString == null)
            {
                throw new Exception("Monogo DB connection string is missing.");
            }
            MongoClient client = new MongoClient(connString);
            

        }
    }
}