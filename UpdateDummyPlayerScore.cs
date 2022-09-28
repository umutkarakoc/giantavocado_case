using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace giantavocado;

public static class UpdateDummyPlayerScore
{
    private static string Leaderboard = "Crown";
    private static string DummyPlayers = "DummyPlayers";
    
    private static PlayFabServerInstanceAPI client;
    static UpdateDummyPlayerScore()
    {
        PlayFabSettings.staticSettings.TitleId = Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID");
        PlayFabSettings.staticSettings.DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_SECRET_KEY");

        client = new PlayFabServerInstanceAPI();
    }
    
    [FunctionName("UpdateDummyPlayerScore")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        var result = await UpdateDummyPlayerScore.client.GetTitleInternalDataAsync(new GetTitleDataRequest()
        {
            Keys = new List<string>(){DummyPlayers} 
        });

        
        if (!result.Result.Data.TryGetValue(DummyPlayers, out string _playerIds))
        {
            log.LogError("Error at parsing player ids");
            return new OkObjectResult(false);
        }

        var playerIds = JsonConvert.DeserializeObject<string[]>(_playerIds);
        log.LogInformation(playerIds.Length.ToString());
        var playerScoreUpdates = playerIds
            .Select(async id =>
            {
        
                await client.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
                {
                    Statistics = new List<StatisticUpdate>()
                    {
                        new StatisticUpdate()
                        {
                            StatisticName = Leaderboard,
                            Value =  100
                        }
                    },
                    PlayFabId = id
                });
            })
            .ToArray();
        
        Task.WaitAll(playerScoreUpdates);
        
        // foreach (var id in playerIds)
        // {
        //     await client.UpdatePlayerStatisticsAsync(new UpdatePlayerStatisticsRequest()
        //     {
        //         Statistics = new List<StatisticUpdate>()
        //         {
        //             new StatisticUpdate()
        //             {
        //                 StatisticName = Leaderboard,
        //                 Value = 100
        //             }
        //         },
        //         PlayFabId = id
        //     });
        // }

        return new OkObjectResult(true);
    }
}