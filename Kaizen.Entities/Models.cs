using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Kaizen.Entities
{
    #region for ai


    public class AssistantRequestBody
    {
        public string Model { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Instructions { get; set; }
        public List<Tool> Tools { get; set; } = new List<Tool>();
        public List<string> FileIds { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    }

    public class Tool
    {
        public string Type { get; set; }
        //public FunctionDetails Function { get; set; }

        // Nested class for Function Tool details

    }

    public class FunctionDetails
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ParameterSchema Parameters { get; set; }
    }

    // Nested class for Function parameters schema
    public class ParameterSchema
    {
        public string Type { get; set; }
        public Dictionary<string, ParameterDetails> Properties { get; set; }
        public List<string> Required { get; set; }
    }

    // Nested class for individual parameter details
    public class ParameterDetails
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class FileUploadResponse
    {
        public string @object { get; set; }
        public string id { get; set; }
        public string purpose { get; set; }
        public string filename { get; set; }
        public int bytes { get; set; }
        public int created_at { get; set; }
        public string status { get; set; }
        public object status_details { get; set; }
    }
    public class Content
    {
        public string type { get; set; }
        public Text text { get; set; }
    }

    public class NewThreadBody
    {
        public List<OpenAIMessage>? messages { get; set; }
        public Dictionary<string, string>? metadata { get; set; }
    }

    public class OpenAIMessage
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created_at { get; set; }
        public string thread_id { get; set; }
        public string role { get; set; }
        public List<Content> content { get; set; } = new List<Content>();
        public List<string> file_ids { get; set; } = new List<string>();
        public object assistant_id { get; set; }
        public object run_id { get; set; }
        public Dictionary<string, string> metadata { get; set; }
    }

    public class Text
    {
        public string value { get; set; }
        public List<object> annotations { get; set; } = new List<object>();
    }

    public class MessageBody
    {
        public string role
        {
            get { return "user"; }
        }
        public string content { get; set; } = string.Empty;
        public Dictionary<string, string> metadata { get; set; }
    }


    public class CreateRunBody
    {
        public string assistant_id { get; set; }
    }

    public class OpenAIAssistant
    {

        public string id { get; set; }
        public string @object { get; set; }
        public int created_at { get; set; }
        public string name { get; set; }
        public object description { get; set; }
        public string model { get; set; }
        public string instructions { get; set; }
        public List<AssistantTool> tools { get; set; } = new List<AssistantTool>();
        public List<string> file_ids { get; set; } = new List<string>();
        public Metadata metadata { get; set; }
    }


    public class AssistantTool
    {
        public string type { get; set; }
    }


    public class OpenAIRun
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created_at { get; set; }
        public string assistant_id { get; set; }
        public string thread_id { get; set; }
        public string status { get; set; }
        public int? started_at { get; set; }
        public int? expires_at { get; set; }
        public int? cancelled_at { get; set; }
        public int? failed_at { get; set; }
        public int? completed_at { get; set; }
        public object last_error { get; set; }
        public string model { get; set; }
        public string instructions { get; set; }
        public List<Tool> tools { get; set; }
        public List<object> file_ids { get; set; }
        public Metadata metadata { get; set; }
        public RequiredAction required_action { get; set; }
    }
    public class RequiredAction
    {
        public string type { get; set; }
        public SubmitToolOutputs submit_tool_outputs { get; set; }
    }
    public class SubmitToolOutputs
    {
        public List<ToolCall> tool_calls { get; set; }
    }

    public class ToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public Function function { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public string arguments { get; set; }
        public string description { get; set; }
    }



    public class OpenAIMessageList
    {
        public string @object { get; set; }
        public List<OpenAIMessage> data { get; set; }
        public string first_id { get; set; }
        public string last_id { get; set; }
        public bool has_more { get; set; }
    }

    public class GetNameInput
    {
        public string alphabet { get; set; }
    }

    public class SubmitToolOutput
    {
        public List<ToolOutput> tool_outputs { get; set; }
    }

    public class ToolOutput
    {
        public string tool_call_id { get; set; }
        public string output { get; set; }
    }



    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class Due
    {
        public string date { get; set; }
        public bool is_recurring { get; set; }
        public DateTime datetime { get; set; }
        public string @string { get; set; }
        public string timezone { get; set; }
    }

    public class TodoistTask
    {
        public string creator_id { get; set; }
        public DateTime created_at { get; set; }
        public string assignee_id { get; set; }
        public string assigner_id { get; set; }
        public int comment_count { get; set; }
        public bool is_completed { get; set; }
        public string content { get; set; }
        public string description { get; set; }
        public Due due { get; set; }
        public object duration { get; set; }
        public string id { get; set; }
        public List<string> labels { get; set; } = new List<string>();
        public int order { get; set; }
        public int priority { get; set; }
        public string project_id { get; set; }
        public string section_id { get; set; }
        public string parent_id { get; set; }
        public string url { get; set; }
    }

    public class Metadata
    {
    }

    public class Assistant
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created_at { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string model { get; set; }
        public string instructions { get; set; }
        public List<object> tools { get; set; } = new List<object>();
        public List<object> file_ids { get; set; } = new List<object>();
        public Metadata metadata { get; set; }
    }

    public class Conversations
    {
        public string id { get; set; }
        public string role { get; set; }
        public long createdAt { get; set; }
        public string content { get; set; }
        public Dictionary<string, string> metadata { get; set; } = new Dictionary<string, string>();
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ThreadMessage
    {
        public string role { get; set; }
        public string content { get; set; } = "";
        public List<string> file_ids { get; set; } = new List<string>();
    }

    public class CreateThreadBody
    {
        public List<ThreadMessage> messages { get; set; }
    }

    public class OpenAIThread
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created_at { get; set; }
        public Metadata metadata { get; set; }
    }
    #endregion
    #region Meta Whatsapp
    public class Change
    {
        public Value value { get; set; }
        public string field { get; set; }
    }

    public class Contact
    {
        public Profile profile { get; set; }
        public string wa_id { get; set; }
    }

    public class Entry
    {
        public string id { get; set; }
        public List<Change> changes { get; set; } = new List<Change>();
    }
    public class Document
    {
        public string caption { get; set; }
        public string filename { get; set; }
        public string mime_type { get; set; }
        public string sha256 { get; set; }
        public string id { get; set; }
    }

    public class Message
    {
        public string from { get; set; }
        public string id { get; set; }
        public string timestamp { get; set; }
        public FbText text { get; set; }
        public string type { get; set; }
        public Document document { get; set; }
        public Document audio { get; set; }
        public Document video { get; set; }
        public Image image { get; set; }
    }

    public class Image
    {
        public string caption { get; set; }
        public string mime_type { get; set; }
        public string sha256 { get; set; }
        public string id { get; set; }
    }

    public class FbMetadata
    {
        public string display_phone_number { get; set; }
        public string phone_number_id { get; set; }
    }

    public class Profile
    {
        public string name { get; set; }
    }

    public class MetaWebhookModel
    {
        public string @object { get; set; }
        public List<Entry> entry { get; set; } = new List<Entry>();
    }

    public class FbText
    {
        public string body { get; set; }
    }

    public class Value
    {
        public string messaging_product { get; set; }
        public FbMetadata metadata { get; set; }
        public List<Contact> contacts { get; set; } = new List<Contact>();
        public List<Message> messages { get; set; } = new List<Message>();
    }

    #endregion

    #region Models
    public class AIResponse
    {
        public string ThreadId { get; set; }
        public string UserMessage { get; set; }
        public string AIMessage { get; set; }
    }
    public class ConversationModel
    {
        public string AssistantId { get; set; }
        public string ThreadId { get; set; }
        //public ConversationPlatform Platform { get; set; }
        //public string PlatformUserId { get; set; }
        //public bool AiMode { get; set; }
        public string content { get; set; }
        public string role { get; set; }
    }
    public class APIGeneralResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; }
        public T? Data { get; set; }
    }
    public class MessageRequest
    {
        public string Message { get; set; }
        public string Thread_Id { get; set; }
        public string Assistant_Id { get; set; }
        public string docId { get; set; }
    }
    public class SendMessageBody
    {
        public string To { get; set; }
        public string From { get; set; }
        public string Message { get; set; }
    }

    public class ProcessMessageModel
    {
        public string aiMessage { get; set; }
        public string aiThread { get; set; }
        public string message { get; set; }
        public string from { get; set; }
        public string name { get; set; }
        public string number { get; set; }
        public string docId { get; set; }

    }

    public class FbMediaModel
    {
        public string url { get; set; }
        public string mime_type { get; set; }
        public string sha256 { get; set; }
        public int file_size { get; set; }
        public string id { get; set; }
        public string messaging_product { get; set; }
    }


    public enum ConversationPlatform
    {
        Website,
        WhatsApp,
        Telegram
    }
    #endregion


    #region Db Entities
    public class ThreadRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string AssistantId { get; set; }
        public string ThreadId { get; set; }
        public ConversationPlatform Platform { get; set; }
        public string PlatformUserId { get; set; }
        public bool AiMode { get; set; }
        public DateTime LastActivityAt { get; set; }
        public LastMesageBy LastMesageBy { get; set; }
        public string Alias { get; set; }
        public bool NeedsAssistance { get; set; }
        public bool IsLeasing { get; set; }
        public bool HasNewMessages { get; set; }
        public bool Visible { get; set; } = true;
        public bool Deleted { get; set; }
    }
    public enum LastMesageBy
    {
        None,
        AI,
        User,
        Agent
    }

    public class AssistantRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string AssistantId { get; set; }
        public string WhatsAppNumber { get; set; }
    }
    #endregion
}
