using OpenGov.Models;
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

        public async Task<string> AddDocument(Meeting meeting, Document document)
        {
            HttpClient http = new HttpClient();

            string path = Path.Combine(basePath, meeting.Source.Name, meeting.BoardName, meeting.Date.ToString("yyyy-MM-dd"));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filename = new string(document.Title.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
            using (var output = new FileStream(Path.Combine(path, filename + ".pdf"), FileMode.OpenOrCreate, FileAccess.Write))
            {
                Stream input = await http.GetStreamAsync(document.Url);
                await input.CopyToAsync(output);
            }

            return path;
        }
    }
}