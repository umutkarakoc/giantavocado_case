using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Web.Http;
using Microsoft.VisualBasic;

namespace giantavocado
{
	public static class research
	{
		[FunctionName("research")]
		public static async Task<IActionResult> Run(
				[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
				ILogger log)
		{

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			dynamic data = JsonConvert.DeserializeObject(requestBody);
			string userId = data.userId;

			using (var httpClient = new HttpClient())
			{
				// playfab secretkey'i azure settings'den geliyor
				httpClient.DefaultRequestHeaders.Add("X-SecretKey", Environment.GetEnvironmentVariable("PLAYFAB_SECRET"));

				HttpResponseMessage res = await httpClient.PostAsync(
					"https://ECEE9.playfabapi.com/Server/GetTitleInternalData", 
					new StringContent("{\"Keys\": [\"ResearchConfig\"]}", Encoding.UTF8, "application/json")
				);

				if (!res.IsSuccessStatusCode)
				{
					return new InternalServerErrorResult();
				}

				dynamic result = JsonConvert.DeserializeObject(
					res.Content.ReadAsStringAsync().Result
				);
			
				// response icerisinde data.Data.ResearchConfig bir string deger ve bizim config ayarlarimizi tutuyor.
				ResearchConfig config = JsonConvert.DeserializeObject<ResearchConfig>((string)result.data.Data.ResearchConfig);

				var userData = new ResearchUserData();
				userData.Id = config.Id;
				userData.StartDate = DateTime.Now;
				userData.EndDate = userData.StartDate.AddSeconds(config.Duration);


				var userDataReq = new UserDataReq {
					PlayFabId = userId,
					Data = new UserData {
						Research = JsonConvert.SerializeObject(userData)
					}
				};
				var userDataReqJSON = JsonConvert.SerializeObject(userDataReq);

				HttpResponseMessage saveRes = await httpClient.PostAsync(
						"https://ECEE9.playfabapi.com/Server/UpdateUserData", 
						new StringContent(userDataReqJSON, Encoding.UTF8, "application/json")
					);

				if (!saveRes.IsSuccessStatusCode)
					return new InternalServerErrorResult();

				return new OkObjectResult(true);
			}
		}
	}

	class ResearchConfig
	{
		public string Id { get; set; }
		public int Duration { get; set; }
	}

	class ResearchUserData
	{
		public string Id { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
	}

	class UserDataReq {
		public string PlayFabId { get; set; }
		public UserData Data { get; set; }
	}

	class UserData {
		public string Research { get; set; }
	}
}
