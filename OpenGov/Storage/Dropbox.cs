using Dropbox.Api;
using OpenGov.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public class Dropbox
    {
        private string accessToken;
        private string baseFolder;

        public async Task<string> AddDocument(Meeting meeting, Document document)
        {
            DropboxCertHelper.InitializeCertPinning();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("accessToken");
            }

            // Specify socket level timeout which decides maximum waiting time when no bytes are
            // received by the socket.
            var httpClient = new HttpClient()
            {
                // Specify request level timeout which decides maximum time that can be spent on
                // download/upload files.
                Timeout = TimeSpan.FromMinutes(20)
            };

            try
            {
                var config = new DropboxClientConfig("OpenGovAlerts")
                {
                    HttpClient = httpClient
                };

                var client = new DropboxClient(accessToken, config);
            }
            catch (HttpException e)
            {
                Console.WriteLine("Exception reported from RPC layer");
                Console.WriteLine("    Status code: {0}", e.StatusCode);
                Console.WriteLine("    Message    : {0}", e.Message);
                if (e.RequestUri != null)
                {
                    Console.WriteLine("    Request uri: {0}", e.RequestUri);
                }
            }

            return "";
        }
    }
}