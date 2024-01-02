using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.BouncyCastle.Utilities.IO.Pem;
using System.Net.NetworkInformation;

namespace Kaizen.Entities
{
    public class DataService
    {
        private static readonly IMongoClient client = new MongoClient(Environment.GetEnvironmentVariable("DbConnectionString"));
        private static readonly IMongoDatabase database = client.GetDatabase("ChatbotLeads");

        #region Threads/Conversations

        public async Task<string> GetAssistantLinkedWithPlatform(string id, ConversationPlatform platform = ConversationPlatform.WhatsApp)
        {
            var assistants = await GetAllAssistants();
            switch (platform)
            {
                case ConversationPlatform.Website:
                    return string.Empty;
                case ConversationPlatform.WhatsApp:
                    return assistants.FirstOrDefault(x => x.WhatsAppNumber == id).AssistantId;
                case ConversationPlatform.Telegram:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }
        public async Task<List<AssistantRecord>> GetAllAssistants()
        {
            var collection = database.GetCollection<AssistantRecord>("Assistants");
            return await collection.Find(_ => true).ToListAsync();
        }

        public async Task<AssistantRecord> CreateAssistantRecord(AssistantRecord entry)
        {
            var collection = database.GetCollection<AssistantRecord>("Assistants");
            await collection.InsertOneAsync(entry);
            return entry;
        }

        public async Task<ThreadRecord> CreateThreadRecord(ThreadRecord record)
        {
            if (string.IsNullOrEmpty(record.AssistantId) || string.IsNullOrEmpty(record.ThreadId))
                throw new Exception("AI assistant id and thread id are required");
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, record.AssistantId) & Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, record.ThreadId);
            var entry = await collection.Find(filter).FirstOrDefaultAsync();
            if (entry == null)
            {
                await collection.InsertOneAsync(record);
                return record;
            }
            else
                return entry;
        }

        public async Task UpdateThreadRecord(string threadId, bool aimode)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.AiMode, aimode);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateThreadRecordAssistance(string threadId, bool assistance = false)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.NeedsAssistance, assistance);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateThreadRecordActivity(string threadId)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.LastActivityAt, DateTime.UtcNow);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task<ThreadRecord> UpdateThread(ThreadRecord record)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.Id, record.Id);
            var update = Builders<ThreadRecord>.Update.Set(x => x.Alias, record.Alias);
            await collection.UpdateOneAsync(filter, update);
            return record;
        }

        public async Task<List<ThreadRecord>> AssistantThreads(string assistantId)
        {
            if (string.IsNullOrEmpty(assistantId))
                throw new Exception("AI assistant id is not provided");
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, assistantId);
            return await collection.Find(filter).ToListAsync();
        }

        public async Task<List<ThreadRecord>> AllThreads()
        {
           
            var collection = database.GetCollection<ThreadRecord>("Threads");

            return await collection.Find(_=>true).ToListAsync();
        }

        public async Task<ThreadRecord> ThreadRecord(string assistantId, string threadId)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, assistantId) & Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<ThreadRecord> ThreadExistsInDatabaseAgainstIdAndPlatform(string assistantId, string id)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, assistantId) & Builders<ThreadRecord>.Filter.Eq(x => x.PlatformUserId, id);
            return await collection.Find(filter).FirstOrDefaultAsync();
        }

        #endregion

    }


}
