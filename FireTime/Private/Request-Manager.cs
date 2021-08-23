using System;
using System.Net.Http;
using FireTime.Response;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace FireTime.Private
{
    internal class ReqManager
    {
        private readonly FireConfig Config;
        private readonly HttpClient HttpReqClient;

        internal ReqManager(FireConfig _Config)
        {
            Config = _Config;
            var FireURL = Config.FirebaseURL;
            Config.FirebaseURL = FireURL.EndsWith("/") ? FireURL : FireURL + "/";
            HttpReqClient = new HttpClient { BaseAddress = new Uri(Config.FirebaseURL) };
            if (!string.IsNullOrWhiteSpace(Config.BearerToken)) HttpReqClient.DefaultRequestHeaders
                    .Authorization = new AuthenticationHeaderValue("Bearer", Config.BearerToken);
        }

        internal StreamResponse Monitor(string ListenPath)
        {
            var SInst = new StreamResponse(GetCookedURI(ListenPath));
            SInst.StartDetection();
            return SInst;
        }

        internal async Task<HttpResponseMessage> GetAsync(string GetPath)
        {
            var Req = new HttpRequestMessage(HttpMethod.Get, GetCookedURI(GetPath));
            return await HttpReqClient.SendAsync(Req, HttpCompletionOption.ResponseHeadersRead);
        }

        internal async Task<HttpResponseMessage> PutAsync(string PutPath, object PutData)
        {
            var Req = new HttpRequestMessage(HttpMethod.Put, GetCookedURI(PutPath)) { Content = GetContent(PutData) };
            return await HttpReqClient.SendAsync(Req, HttpCompletionOption.ResponseContentRead);
        }

        internal async Task<HttpResponseMessage> PatchAsync(string PatchPath, object PatchData)
        {
            var Req = new HttpRequestMessage(new HttpMethod("PATCH"), GetCookedURI(PatchPath)) { Content = GetContent(PatchData) };
            return await HttpReqClient.SendAsync(Req, HttpCompletionOption.ResponseContentRead);
        }

        internal async Task<HttpResponseMessage> DeleteAsync(string DeletePath)
        {
            var Req = new HttpRequestMessage(HttpMethod.Delete, GetCookedURI(DeletePath));
            return await HttpReqClient.SendAsync(Req, HttpCompletionOption.ResponseContentRead);
        }

        private HttpContent GetContent(object Data)
            => new StringContent(Data as string ?? Newtonsoft.Json.JsonConvert.SerializeObject(Data));

        private Uri GetCookedURI(string Path)
        {
            var AuthData = string.IsNullOrWhiteSpace(Config.AuthToken) ? "" : $"?auth={Config.AuthToken}";
            return new Uri($"{Config.FirebaseURL}{Path}.json{AuthData}");
        }
    }
}
