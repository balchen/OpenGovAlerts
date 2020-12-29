using OpenGov.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public class LocalDisk: IStorage
    {
        private string basePath;
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        public LocalDisk(string basePath)
        {
            this.basePath = basePath;
        }

        public async Task<Uri> AddDocument(AgendaItem item, Document document, string path = "")
        {
            HttpClient http = new HttpClient();

            if (string.IsNullOrEmpty(path))
                path = Path.Combine(basePath, item.Meeting.Source.Name, item.Meeting.BoardName, item.Meeting.Date.ToString("yyyy-MM-dd"));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filename = new string(document.Title.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
            using (var output = new FileStream(Path.Combine(path, filename + ".pdf"), FileMode.OpenOrCreate, FileAccess.Write))
            {
                Stream input = await http.GetStreamAsync(document.Url);
                await input.CopyToAsync(output);
            }

            return new Uri("file:///" + path);
        }
    }
}