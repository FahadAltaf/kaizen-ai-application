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
using Azure;

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

        public async Task<GoogleSpreadSheet> CreateSheetRecord(GoogleSpreadSheet entry)
        {
            var collection = database.GetCollection<GoogleSpreadSheet>("Sheets");
            await collection.InsertOneAsync(entry);
            return entry;
        }

        public async Task<GoogleSpreadSheet> UpdateSheet(GoogleSpreadSheet entry)
        {
            var collection = database.GetCollection<GoogleSpreadSheet>("Sheets");
            var filter = Builders<GoogleSpreadSheet>.Filter.Eq(x=>x.Id,entry.Id);
            var update = Builders<GoogleSpreadSheet>.Update.Set(x => x.SheetData, entry.SheetData).Set(x => x.SheetData, entry.SheetData).Set(x=>x.UpdatedAt,entry.UpdatedAt);
            await collection.UpdateOneAsync(filter, update);  
            return entry;
        }

        public async Task<GoogleSpreadSheet> GetSheetRecord(string sid,long id)
        {
            var collection = database.GetCollection<GoogleSpreadSheet>("Sheets");
           return await collection.Find(x=>x.SpreadSheetId==sid && x.SheetId==id).FirstOrDefaultAsync();
           
        }

        public async Task<List<GoogleSpreadSheet>> GetSheets()
        {
            var collection = database.GetCollection<GoogleSpreadSheet>("Sheets");
            return await collection.Find(_=>true).ToListAsync();

        }

        public async Task<APIGeneralResponse<AvailabilityModel>> RetriveSheets(string text1)
        {
            var collection = database.GetCollection<GoogleSpreadSheet>("Sheets");
            var filter = Builders<GoogleSpreadSheet>.Filter.Regex(x => x.SheetData, new BsonRegularExpression(text1));
            var data = await collection.Find(filter).FirstOrDefaultAsync();
            APIGeneralResponse<AvailabilityModel> response = new APIGeneralResponse<AvailabilityModel>();


            if (data != null)
            {
                var text = data.SheetData;
                var lines = text.Split("\r\n");
                List<string> availability = new List<string>();
                List<string> faqs = new List<string>();

                string steps = "avail";
                foreach (var item in lines)
                {
                    if (item.StartsWith("FAQs"))
                    {
                        steps = "faq";
                        continue;
                    }
                    else if (item.StartsWith("Internal"))
                    {
                        steps = "internal";
                        continue;
                    }
                    switch (steps)
                    {
                        case "avail":
                            availability.Add(item);
                            break;
                        case "faq":
                            faqs.Add(item);
                            break;
                        case "internal":
                            break;
                        default:
                            break;
                    }
                }

                List<FAQModel> models = new List<FAQModel>();

                foreach (var item in faqs)
                {
                    var parts = item.Split(";");
                    models.Add(new FAQModel { Question = parts[0], Answer = parts[1] });
                }
                response.Data = new AvailabilityModel { Availability = string.Join("\r\n", availability.Take(2)), FAQs = models };
                response.Message = "OK";
                response.Status = true;
            }
            else
            {
                response.Message = "NOT AVAILABLE";
            }
            return response;

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

        public async Task UpdateThreadRecord(string threadId, bool aimode, string agent="")
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.AiMode, aimode).Set(x => x.Agent, agent);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateThreadHasMessages(string threadId, bool aimode)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.HasNewMessages, aimode);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateThreadRecordAssistance(string threadId, bool assistance = false)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.NeedsAssistance, assistance);
            await collection.UpdateOneAsync(filter, update);
        }

        //public async Task UpdateThreadRecordInquiry(string threadId, bool assistance = false)
        //{
        //    var collection = database.GetCollection<ThreadRecord>("Threads");
        //    var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
        //    var update = Builders<ThreadRecord>.Update.Set(x => x.IsInquiry, assistance);
        //    await collection.UpdateOneAsync(filter, update);
        //}

        public async Task UpdateThreadRecordActivity(string threadId, LastMesageBy by)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
            var update = Builders<ThreadRecord>.Update.Set(x => x.LastActivityAt, DateTime.UtcNow).Set(x => x.LastMesageBy, by);
            await collection.UpdateOneAsync(filter, update);
        }

        public async Task<ThreadRecord> UpdateThread(ThreadRecord record)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.Id, record.Id);
            var update = Builders<ThreadRecord>.Update.Set(x => x.Alias, record.Alias)
                .Set(x => x.Agent, record.Agent)
                .Set(x => x.Visible, record.Visible).Set(x => x.Deleted, record.Deleted);
            await collection.UpdateOneAsync(filter, update);
            return record;
        }

        public async Task<List<ThreadRecord>> AssistantThreads(string assistantId,string text="")
        {
            var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
            if (string.IsNullOrEmpty(assistantId))
                throw new Exception("AI assistant id is not provided");
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter =(string.IsNullOrEmpty(text))? 
                Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, assistantId) & Builders<ThreadRecord>.Filter.Eq(x => x.Deleted, false) & Builders<ThreadRecord>.Filter.Gte(x => x.LastActivityAt, twoWeeksAgo) :
                Builders<ThreadRecord>.Filter.Eq(x => x.AssistantId, assistantId) & Builders<ThreadRecord>.Filter.Eq(x => x.Deleted, false) & (Builders<ThreadRecord>.Filter.Regex(x=>x.PlatformUserId, new BsonRegularExpression(text, "i")) | Builders<ThreadRecord>.Filter.Regex(x => x.Alias, new BsonRegularExpression(text, "i")));
            var all = await collection.Find(filter).ToListAsync();
            var groups = all.GroupBy(x => x.PlatformUserId);
            foreach (var item in groups)
            {
                if (item.Count() > 1)
                {
                    foreach (var th in item.OrderByDescending(x=>x.LastActivityAt).Skip(1))
                    {
                        th.Deleted = true;
                        th.Visible = false;
                       await UpdateThread(th);
                    }
                }
            }
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

        public async Task<ThreadRecord> ThreadRecord(string threadId)
        {
            var collection = database.GetCollection<ThreadRecord>("Threads");
            var filter = Builders<ThreadRecord>.Filter.Eq(x => x.ThreadId, threadId);
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
