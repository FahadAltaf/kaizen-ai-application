﻿using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kaizen.Entities
{
    public class AIAssistant
    {
        private readonly HttpClient _httpClient;

        string _baseUrl = "https://api.openai.com/v1";

        public AIAssistant()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("OpenAIKey")}");
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");
        }
        public async Task<string> Run(MessageRequest request)
        {
            //Add message to the thread
            Console.WriteLine($"user: {request.Message}");


            await AddMessageToThread(request,new Dictionary<string, string> { });

            var openAIRun = await CreateRun(request.Assistant_Id, request.Thread_Id);

            while (openAIRun.status != "completed" /*&& openAIRun.status != "requires_action"*/)
            {
                await Task.Delay(200);

                openAIRun = await RetriveRun(request.Thread_Id, openAIRun.id);
                Console.WriteLine($"Run :{openAIRun.id}, Status: {openAIRun.status}");
                if (openAIRun.status == "cancelled" || openAIRun.status == "failed" || openAIRun.status == "expired")
                {
                    throw new Exception($"Unable to process run. Run Status: {openAIRun.status}");
                }


            }

            //if (openAIRun.status == "requires_action")
            //{
            //    Console.WriteLine("Run Id: " + openAIRun.id);
            //    string toolOutput = string.Empty;
            //    string callId = string.Empty;
            //    try
            //    {

            //        var toolAction = openAIRun.required_action.submit_tool_outputs.tool_calls[0];

            //        switch (toolAction.function.name)
            //        {
            //            case "get_name":
            //                Console.WriteLine("Calling tool get_name");
            //                callId = toolAction.id;
            //                toolOutput = await GetName(JsonSerializer.Deserialize<GetNameInput>(toolAction.function.arguments));
            //                break;
            //            case "get_active_tasks":
            //                Console.WriteLine("Calling tool get_active_tasks");
            //                callId = toolAction.id;
            //                toolOutput = await GetActiveTasks();
            //                break;
            //            default:
            //                toolOutput = "";
            //                break;
            //        }
            //    }
            //    catch
            //    {

            //    }

            //    //Call output api
            //    openAIRun = await SubmitToolOutput(request.Thread_Id, openAIRun.id, callId, toolOutput);
            //    Console.WriteLine("Run Id: " + openAIRun.id);
            //    while (openAIRun.status != "completed" && openAIRun.status != "requires_action")
            //    {
            //        await Task.Delay(1000);

            //        openAIRun = await RetriveRun(request.Thread_Id, openAIRun.id);
            //        Console.WriteLine($"Run :{openAIRun.id}, Status: {openAIRun.status}");
            //        if (openAIRun.status == "cancelled" || openAIRun.status == "failed" || openAIRun.status == "expired")
            //        {
            //            throw new Exception($"Unable to process run. Run Status: {openAIRun.status}");
            //        }
            //    }
            //}
            //else if (openAIRun.status != "completed")
            //{
            //    Console.WriteLine($"Problem: ");
            //}
            //else
            //{
            //    Console.WriteLine($"Status : {openAIRun.status}");
            //}

            var assistantMessage = await RetriveMessage(request.Thread_Id, openAIRun.id);

            Console.WriteLine($"Assistant: {assistantMessage.content[0].text.value}");
            return assistantMessage.content[0].text.value;
        }

        public async Task<List<AssistantFile>> ListAllAssistantFiles(string assistantId)
        {
            var allFiles = new List<AssistantFile>();
            var hasMore = true;
            var afterId = string.Empty;

            while (hasMore)
            {
                var response = await ListAssistantFiles(assistantId, afterId);
                allFiles.AddRange(response.data);
                afterId = response.last_id;
                hasMore = response.has_more;
            }

            return allFiles;
        }

        private async Task<AssistantFilesResponse> ListAssistantFiles(string assistantId, string afterId)
        {
            var requestUrl = $"{_baseUrl}/assistants/{assistantId}/files";

            if (!string.IsNullOrEmpty(afterId))
            {
                requestUrl += $"?after={afterId}";
            }

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssistantFilesResponse>(content);
        }

        public async Task<OpenAIFile> GetFile(string fileId)
        {
            var requestUrl = $"{_baseUrl}/files/{fileId}";

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OpenAIFile>(content);
        }

        public async Task<bool> UpdateAssistantFiles(string assistantId, string[] fileIds)
        {
            // Construct the request URL
            var requestUrl = $"{_baseUrl}/assistants/{assistantId}";

            // Create the request body
            var requestBody = new
            {
                tools = new[]
        {
            new { type = "retrieval" }
        },
                file_ids = fileIds
            };

            // Serialize the request body to JSON
            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send the POST request
            var response = await _httpClient.PostAsync(requestUrl, content);
            var contentx = await response.Content.ReadAsStringAsync();
            
            return response.IsSuccessStatusCode;
        }

        public async Task<DeleteFileModel> DeleteFile(string fileId)
        {
            var requestUrl = $"{_baseUrl}/files/{fileId}";

            var response = await _httpClient.DeleteAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DeleteFileModel>(content);
        }

        public async Task<FileResponse> UploadLocalFile(string filePath,string name)
        {
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain"); 

            // Prepare the multipart/form-data request
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent("assistants"), "purpose");
            formData.Add(fileContent, "file", name);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/files", formData);

            response.EnsureSuccessStatusCode();
            return  await response.Content.ReadFromJsonAsync<FileResponse>();
        }
        public async Task<FileUploadResponse> UploadFile(string id, string url)
        {

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/files");
            request.Headers.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("OpenAIKey")}");
            var fileStream = await new HttpClient().GetStreamAsync(url);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("assistants"), "purpose");
            content.Add(new StreamContent(fileStream), "file", $"{id}.csv");
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<FileUploadResponse>();
            }
            throw new Exception(await response.Content.ReadAsStringAsync());

        }

        public async Task<OpenAIAssistant> RetrieveAssistantAsync(string assistantId)
        {
            Console.WriteLine($"Retriving assistant: {assistantId}");
            var url = $"{_baseUrl}/assistants/{assistantId}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OpenAIAssistant>();
            }
            throw new Exception($"Unable to fetch the bot details. Reason: " + response.Content);
        }

        public async Task<OpenAIMessage> AddMessageToThread(MessageRequest request,Dictionary<string,string> passedmetadata, string role = "user")
        {
        task:
            try
            {
                Dictionary<string, string> metadata = new Dictionary<string, string> { };
                metadata.Add("role", role);
                if (!string.IsNullOrEmpty(request.docId))
                {
                    metadata.Add("url", request.docId);
                }
                foreach (var m in passedmetadata)
                {
                    metadata.Add(m.Key, m.Value);
                }
                Console.WriteLine($"Adding message to thread: {request.Thread_Id}");
                var response = await _httpClient.PostAsJsonAsync<MessageBody>($"https://api.openai.com/v1/threads/{request.Thread_Id}/messages", new MessageBody { content = request.Message, metadata = metadata });
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OpenAIMessage>();
                }
                throw new Exception($"Unable to add message to the thread. Reason: {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                var runId = ex.Message.Split(" ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(x => x.StartsWith("run_"));
                var response = await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{request.Thread_Id}/runs/{runId}/cancel", null);
                if (response.IsSuccessStatusCode)
                {
                    goto task;
                }
                throw new Exception($"Problem in cancelling the run {runId}. Reason: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task<OpenAIRun> CreateRun(string assistant_id, string thread_id, string inst=null)
        {
            Console.WriteLine($"Create a run for assistant: {assistant_id}");
            var response = await _httpClient.PostAsJsonAsync<CreateRunBody>($"https://api.openai.com/v1/threads/{thread_id}/runs", new CreateRunBody { assistant_id = assistant_id, instructions=inst });
            if (response.IsSuccessStatusCode)
            {
                var run = await response.Content.ReadFromJsonAsync<OpenAIRun>();
                if (run != null && !string.IsNullOrEmpty(run.id))
                    return run;
                else
                    throw new Exception($"Unable to create run. Reason: {response.Content}");
            }
            throw new Exception($"Unable to create run. Reason: {response.Content}");
        }

        public async Task<OpenAIThread> CreateThread(string assistant_id, Dictionary<string, string> metadata)
        {
            Console.WriteLine($"Create a thread for assistant: {assistant_id}");
            var response = await _httpClient.PostAsJsonAsync<NewThreadBody>($"https://api.openai.com/v1/threads", new NewThreadBody
            {
                metadata = metadata
            });
            if (response.IsSuccessStatusCode)
            {
                var thread = await response.Content.ReadFromJsonAsync<OpenAIThread>();
                if (thread != null && !string.IsNullOrEmpty(thread.id))
                    return thread;
                else
                    throw new Exception($"Unable to create thread. Reason: {response.Content}");
            }
            throw new Exception($"Unable to create thread. Reason: {response.Content}");
        }

        async Task<OpenAIRun> SubmitToolOutput(string thread_id, string runId, string callid, string output)
        {
            Console.WriteLine($"Submiting tool output: {output}");
            var response = await _httpClient.PostAsJsonAsync<SubmitToolOutput>($"https://api.openai.com/v1/threads/{thread_id}/runs/{runId}/submit_tool_outputs", new SubmitToolOutput { tool_outputs = new List<ToolOutput> { new ToolOutput { output = output, tool_call_id = callid } } });
            if (response.IsSuccessStatusCode)
            {
                var run = await response.Content.ReadFromJsonAsync<OpenAIRun>();
                if (run != null && !string.IsNullOrEmpty(run.id))
                    return run;
                else
                    throw new Exception($"Unable to submit tool output. Reason: {response.Content}");
            }
            throw new Exception($"Unable to submit tool output. Reason: {response.Content}");
        }

        public async Task<OpenAIRun> RetriveRun(string threadId, string runId)
        {
            Console.WriteLine($"Retriving run: {runId}");
            var response = await _httpClient.GetFromJsonAsync<OpenAIRun>($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
            if (response != null && !string.IsNullOrEmpty(response.id))
            {
                return response;
            }
            throw new Exception($"Unable to retrive run details.");
        }

        public async Task<OpenAIMessage> RetriveMessage(string threadId, string runId)
        {
            Console.WriteLine($"Retriving assistant message:");
            var response = await _httpClient.GetFromJsonAsync<OpenAIMessageList>($"https://api.openai.com/v1/threads/{threadId}/messages");
            if (response != null)
            {
                var message = response.data.FirstOrDefault(x => x.role == "assistant" && runId == runId);
                if (message != null)
                {
                    return message;
                }
                else
                    throw new Exception($"Unable to retrive message details.");
            }
            throw new Exception($"Unable to retrive message details.");
        }

        //public async Task<List<OpenAIMessage>> GetMessages(string threadId, string after = "", int limit = 50)
        //{
        //    Console.WriteLine($"Retriving assistant message:");
        //    string url = $"https://api.openai.com/v1/threads/{threadId}/messages?limit={limit}";
        //    if (!string.IsNullOrEmpty(after))
        //    {
        //        url += $"&before={after}";
        //    }

        //    var response = await _httpClient.GetFromJsonAsync<OpenAIMessageList>(url);
        //    if (response != null)
        //    {
        //        return response.data;

        //    }
        //    throw new Exception($"Unable to retrive message details.");
        //}

        public async Task<List<OpenAIMessage>> GetMessages(string threadId)
        {
            List<OpenAIMessage> allMessages = new List<OpenAIMessage>();
            string after = "";
            int limit = 100; // Set the limit to the maximum value allowed by the API

            try
            {
                while (true)
                {
                    string url = $"https://api.openai.com/v1/threads/{threadId}/messages?limit={limit}";
                    if (!string.IsNullOrEmpty(after))
                    {
                        url += $"&after={after}";
                    }

                    var response = await _httpClient.GetFromJsonAsync<OpenAIMessageList>(url);
                    if (response == null || response.data == null || response.data.Count == 0)
                    {
                        break;
                    }

                    allMessages.AddRange(response.data);
                    if (response.data.Count < limit)
                    {
                        // No more messages left to fetch
                        break;
                    }

                    // Set 'after' to the last retrieved message ID for pagination
                    after = response.data.Last().id;
                }

                return allMessages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while retrieving messages: {ex.Message}");
                throw;
            }
        }



        public async Task<string> GetAIResponse(string assistantId, string threadId, string inst=null)
        {
            var openAIRun = await CreateRun(assistantId, threadId,inst);

            while (openAIRun.status != "completed")
            {
                await Task.Delay(700);

                openAIRun = await RetriveRun(threadId, openAIRun.id);
                Console.WriteLine($"Run :{openAIRun.id}, Status: {openAIRun.status}");
                if (openAIRun.status == "cancelled" || openAIRun.status == "failed" || openAIRun.status == "expired")
                {
                    throw new Exception($"Unable to process run. Run Status: {openAIRun.status}");
                }
                else if (openAIRun.status == "requires_action")
                {
                    Console.WriteLine("Calling tool");
                    string toolResponse = "";
                    switch (openAIRun.required_action.submit_tool_outputs.tool_calls[0].function.name)
                    {
                        case "handoverToHuman":
                            toolResponse = await HandOverToHuman(threadId);
                            Console.WriteLine("Tool response : " + toolResponse);
                            break;
                        default:

                            break;
                    }
                    await SubmitToolOutput(threadId, openAIRun.id, openAIRun.required_action.submit_tool_outputs.tool_calls[0].id, toolResponse);
                    //openAIRun.required_action.submit_tool_outputs.tool_calls[0].function.name will give us the function name to be called and that function will always return a text.

                }
            }

            var assistantMessage = await RetriveMessage(threadId, openAIRun.id);

            Console.WriteLine($"Assistant: {assistantMessage.content[0].text.value}");
            return assistantMessage.content[0].text.value;
        }

        public async Task<string> HandOverToHuman(string threadId)
        {
            string message = "";
            try
            {
                var response = await _httpClient.GetFromJsonAsync<APIGeneralResponse<bool>>($"https://kaizenchatbot.azurewebsites.net/api/HandoverToHuman/{threadId}?code=7Q7Uo9EsyC4jHnqocWFrwrYm-Cc94NHjHgYG4-wt2X_8AzFuHQ86RA==");
                message = JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return message;
        }
    }
}
