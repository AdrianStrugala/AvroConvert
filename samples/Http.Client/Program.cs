using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using SolTechnology.Avro.Http;

namespace Http.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Http Client running!");
            string url = "https://localhost:2137/WeatherForecast";

            HttpClient httpClient = new HttpClient();
            List<WeatherForecast> avroResult = httpClient.GetAsAvro<List<WeatherForecast>>(url).Result;
            Console.WriteLine(JsonConvert.SerializeObject(avroResult));

            HttpResponseMessage postResponse = httpClient.PostAsAvro(url, avroResult.First()).Result;
            Console.WriteLine(JsonConvert.SerializeObject(postResponse));

            Console.ReadLine();
        }
    }
}
