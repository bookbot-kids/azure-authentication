using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Authentication.Shared.Library
{
    public class HttpHelper
    {
        /// <summary>
        /// Parse to json object from http response
        /// </summary>
        /// <param name="httpResponse">http response</param>
        /// <returns>Json object</returns>
        public static async Task<JObject> GetJson(HttpResponseMessage httpResponse)
        {
            var data = await httpResponse.Content.ReadAsStringAsync();
            return JObject.Parse(data);
        }

        public static async Task<dynamic> DeserializeJson(HttpResponseMessage httpResponse)
        {
            var data = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(data);
        }

        public static string GetIpFromRequestHeaders(HttpRequest request)
        {
            return (request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "").Split(new char[] { ':' }).FirstOrDefault();
        }

    }
}
