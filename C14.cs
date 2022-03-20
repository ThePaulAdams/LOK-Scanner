﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;
using Object = lok_wss.Models.Object;

namespace lok_wss
{
    public static class C14
    {
        private static Timer _c14Timer;
        public static string leaveZones = "";
        public static void C14Scan()
        {
            const int thisContinent = 14;
            try
            {
                var exitEvent = new ManualResetEvent(false);
                var url = new Uri("wss://socf-lok-live.leagueofkingdoms.com:443/socket.io/?EIO=4&transport=websocket");
                using var client = new WebsocketClient(url) { ReconnectTimeout = TimeSpan.FromSeconds(30) };
                client.ReconnectionHappened.Subscribe(_ =>
                {
                    //Console.WriteLine("Reconnection happened, type: " + info.Type);
                });
                _ = client.MessageReceived.Subscribe(msg =>
                {
                    string message = msg.Text;
                    string json = "";
                    JObject parse = new();

                    if (message.Contains("{"))
                    {
                        json = Helpers.ExtractJson(message[message.IndexOf("{", StringComparison.Ordinal)..]);
                        parse = JObject.Parse(json);
                    }
                    if (!string.IsNullOrEmpty(parse["sid"]?.ToString()))
                    {
                        Console.WriteLine("Message received: " + msg);
                    }
                    if (msg.Text == "40") { }
                    else
                    {
                        var mapObjects = JsonConvert.DeserializeObject<Models.Root>(json);

                        if (mapObjects != null && mapObjects.objects != null && mapObjects.objects.Count != 0)
                        {
                            Console.WriteLine($"c{thisContinent}: " + mapObjects.objects?.Count + " Objects received");
                            List<Models.Object> crystalMines = mapObjects.objects.Where(x => x.code.ToString() == "20100105").ToList();
                            if (crystalMines.Count >= 1)
                                Helpers.ParseObjects("cmines", crystalMines, thisContinent);

                            List<Object> treasureGoblins = mapObjects.objects.Where(x => x.code.ToString() == "20200104").ToList();
                            if (treasureGoblins.Count >= 1)
                                Helpers.ParseObjects("goblins", treasureGoblins, thisContinent);
                        }
                    }

                });
                client.Start();
                _c14Timer = new Timer(
                    _ => SendRequest(client, thisContinent, _c14Timer, exitEvent),
                    null,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(2));


                exitEvent.WaitOne();
            }
            catch (Exception ex)
            {

                Discord.logError("C14", ex);
            }
        }


        private static void SendRequest(WebsocketClient client, int continent, Timer timer, ManualResetEvent exitEvent)
        {

            int count = 9;
            string zones = "";
            Random rand = new();
            if (!string.IsNullOrEmpty(leaveZones))
            {
                Task.Run(() =>
                    client.Send("42[\"/zone/leave/list\", {\"world\":" + continent + ", \"zones\":\"[" + zones +
                                "]\"}]"));
            }

            for (int i = 0; i < count; i++)
            {
                int number = rand.Next(2000, 4090);
                zones += $"{number},";
            }

            zones = zones.Substring(0, zones.Length - 1);
            leaveZones = zones;

            Task.Run(() =>
                client.Send("42[\"/zone/enter/list\", {\"world\":" + continent + ", \"zones\":\"[" + zones +
                            "]\"}]"));
            Console.WriteLine($"{continent}: Requested {zones}");



            //int count = 9;
            //int startCount = 2000;
            //int endCount = 2009;
            //int iterations = 1;
            //string zones = "";

            //if (client.IsRunning)
            //{
            //    for (int i = 0; i < 200; i++)
            //    {
            //        for (int y = startCount; y < endCount; y++)
            //        {

            //            zones += $"{y},";
            //        }

            //        zones = zones.Substring(0, zones.Length - 1);
            //        Task.Run(() =>
            //            client.Send("42[\"/zone/enter/list\", {\"world\":" + continent + ", \"zones\":\"[" + zones +
            //                        "]\"}]"));
            //        Console.WriteLine($"{continent}: Requested {startCount} to {endCount}");
            //        startCount = endCount;
            //        endCount += count;
            //        Thread.Sleep(1000);
            //    }
            //}
        }
    }
}