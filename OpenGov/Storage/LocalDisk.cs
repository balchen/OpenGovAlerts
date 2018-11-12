﻿using OpenGov.Models;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public class LocalDisk
    {
        private string basePath;
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        public LocalDisk(string basePath)
        {
            this.basePath = basePath;
        }

        public async Task AddDocument(Meeting meeting, Document document)
        {
            HttpClient http = new HttpClient();

            string path = Path.Combine(basePath, meeting.ClientId, meeting.Phrase, meeting.Name, meeting.Date.ToString("yyyy-MM-dd"));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filename = new string(document.Name.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
            using (var output = new FileStream(Path.Combine(path, filename + ".pdf"), FileMode.OpenOrCreate, FileAccess.Write))
            {
                Stream input = await http.GetStreamAsync(document.Url);
                await input.CopyToAsync(output);
            }
        }
    }
}
