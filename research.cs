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
			// log.LogInformation("C# HTTP trigger function processed a request.");

			// 


			// name = name ?? data?.name;

			// string responseMessage = string.IsNullOrEmpty(name)
			//     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
			//     : $"Hello, {name}. This HTTP triggered function executed successfully.";

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			dynamic data = JsonConvert.DeserializeObject(requestBody);
			string userId = data.userId;

			var getConfigParams = (dynamic)new JsonObject();
			getConfigParams.Keys = new string[] { "ResearchConfig" };

			using (var httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Add("X-SecretKey", Environment.GetEnvironmentVariable("PLAYFAB_SECRET"));
				HttpResponseMessage res = await httpClient.PostAsync("https://ECEE9.playfabapi.com/Server/GetTitleInternalData", getConfigParams);

				if (!res.IsSuccessStatusCode)
				{
					return new InternalServerErrorResult();
				}

				var result = JsonConvert.DeserializeObject<ResearchConfig>(res.Content.ReadAsStringAsync().Result);

				var userData = new ResearchUserData();
				userData.Id = result.Id;
				userData.StartDate = DateTime.Now;
				userData.EndDate = userData.StartDate.AddSeconds(result.Duration);


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
