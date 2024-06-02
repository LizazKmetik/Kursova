using System.Net.Http;

namespace YoutubeExplode.Search
{
    internal class VideoSearchClient : HttpClient
    {
        private string youTubeApiKey;

        public VideoSearchClient(string youTubeApiKey)
        {
            this.youTubeApiKey = youTubeApiKey;
        }
    }
}