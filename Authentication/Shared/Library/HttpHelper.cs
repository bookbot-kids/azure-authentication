using System.Net.Http;
using System.Threading.Tasks;
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
    }
}
