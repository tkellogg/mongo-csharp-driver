﻿/* Copyright 2010-2012 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests
{
    /// <summary>
    /// A static class to handle online test configuration.
    /// </summary>
    public static class Configuration
    {
        // private static fields
        private static MongoClient __testClient;
        private static MongoServer __testServer;
        private static MongoDatabase __testDatabase;
        private static MongoCollection<BsonDocument> __testCollection;

        // static constructor
        static Configuration()
        {
            var connectionString = "mongodb://localhost/?w=1"; // TODO: make this configurable

            var mongoUrl = new MongoUrl(connectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            if (!clientSettings.WriteConcern.Enabled)
            {
                clientSettings.WriteConcern.W = 1; // ensure WriteConcern is enabled regardless of what the URL says
            }

            __testClient = new MongoClient(clientSettings);
            __testServer = __testClient.GetServer();
            __testDatabase = __testServer.GetDatabase("csharpdriverunittests");
            __testCollection = __testDatabase.GetCollection("testcollection");
        }

        // public static properties
        /// <summary>
        /// Gets the test client.
        /// </summary>
        public static MongoClient TestClient
        {
            get { return __testClient; }
        }

        /// <summary>
        /// Gets the test collection.
        /// </summary>
        public static MongoCollection<BsonDocument> TestCollection
        {
            get { return __testCollection; }
        }

        /// <summary>
        /// Gets the test database.
        /// </summary>
        public static MongoDatabase TestDatabase
        {
            get { return __testDatabase; }
        }

        /// <summary>
        /// Gets the test server.
        /// </summary>
        public static MongoServer TestServer
        {
            get { return __testServer; }
        }

        // public static methods
        /// <summary>
        /// Gets the test collection with a default document type of T.
        /// </summary>
        /// <typeparam name="T">The default document type.</typeparam>
        /// <returns>The collection.</returns>
        public static MongoCollection<T> GetTestCollection<T>()
        {
            return __testDatabase.GetCollection<T>(__testCollection.Name);
        }
    }
}
