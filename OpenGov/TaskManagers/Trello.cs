using Newtonsoft.Json;
using OpenGov.Models;
using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.TaskManagers
{
    public class Trello : ITaskManager
    {
        private readonly HttpClient http;
        private readonly string taskBoardId;
        private readonly string taskListId;
        private readonly string apiKey;
        private readonly string apiToken;

        public Trello(string apiKey, string apiToken, string taskBoardId, string taskListId)
        {
            this.apiKey = apiKey;
            this.apiToken = apiToken;
            this.taskBoardId = taskBoardId;
            this.taskListId = taskListId;

            http = new HttpClient();
            http.BaseAddress = new Uri("https://api.trello.com/1/");
            http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<Uri> AddTask(Meeting meeting)
        {
            dynamic request = new ExpandoObject();

            request.name = meeting.Title;
            request.idList = taskListId;
            request.pos = "top";

            var response = await http.PostAsync("cards", new StringContent(JsonConvert.SerializeObject(request)));

            string responseJson = await response.Content.ReadAsStringAsync();

            dynamic responseData = JsonConvert.DeserializeObject(responseJson);

            return new Uri(responseData.url);
        }

        private string GetUrl(string method)
        {
            return method + "?key=" + apiKey + "&token=" + apiToken;
        }
    }
}
