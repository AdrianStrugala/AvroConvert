using System;
using System.Collections.Generic;
using System.Net.Http;
using SolTechnology.Avro.Http;

namespace Http.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Http Client running!");

            HttpClient httpClient = new HttpClient();
            var avroResult = httpClient.GetAsAvro<List<WeatherForecast>>("https://localhost:2137/WeatherForecast").Result;

            Console.WriteLine(avroResult.ToString());

            Console.ReadLine();
        }
    }
}
