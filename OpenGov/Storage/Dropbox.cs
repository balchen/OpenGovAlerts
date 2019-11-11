using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using OpenGov.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenGov.Storage
{
    public class Dropbox: IStorage
    {
        private string accessToken;
        private string baseFolder;
        static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

        public Dropbox(string accessToken, string baseFolder)
        {
            this.accessToken = accessToken;
            this.baseFolder = baseFolder;
        }

        public async Task<Uri> AddDocument(Meeting meeting, Document document, string path = "")
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

            var config = new DropboxClientConfig("OpenGovAlerts")
            {
                HttpClient = httpClient
            };

            var client = new DropboxClient(accessToken, config);

            if (string.IsNullOrEmpty(path))
                path = Path.Combine(baseFolder, meeting.Source.Name, meeting.Title, meeting.Date.ToString("yyyy-MM-dd") + "-" + meeting.BoardName);

            var filename = new string(document.Title.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());

            try
            {
                CreateFolderResult result = await client.Files.CreateFolderV2Async(path, false);
            }
            catch (ApiException<CreateFolderError> ex)
            {
                if (ex.ErrorResponse.AsPath.Value.IsConflict)
                {
                    // The folder already exists. Fine. I feel like I need to shower now.
                }
                else
                {
                    // Fuck it, I'm not looking at any of those other properties.
                    throw new ApplicationException(string.Format("Could not create/verify Dropbox folder {1} for document {0} because of error {2}", document.Url, path, ex.Message));
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("Could not create/verify Dropbox folder {1} for document {0} because of error {2}", document.Url, path, e.Message));
            }

            string filePath = Path.Combine(path, filename);

            try
            {
                Stream input = await httpClient.GetStreamAsync(document.Url);

                FileMetadata file = await client.Files.UploadAsync(filePath, body: input);

                SharedLinkMetadata link = await client.Sharing.CreateSharedLinkWithSettingsAsync(new CreateSharedLinkWithSettingsArg(file.PathLower));
                return new Uri(link.Url);
            }
            catch (ApiException<UploadError> ex)
            {
                throw new ApplicationException(string.Format("Could not upload document {0} to path {1} because of error {2}", document.Url, filePath, ex.Message));
            }
            catch (Exception e)
            {
                throw new ApplicationException(string.Format("Could not upload document {0} to path {1} because of error {2}", document.Url, filePath, e.Message));
            }
        }
    }
}