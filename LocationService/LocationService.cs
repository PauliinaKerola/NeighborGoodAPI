using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Service.Models;

namespace Service
{
    public class LocationService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://maps.googleapis.com/maps/api/geocode/json";
        public LocationService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<LocationObject?> GetLocationObject(string street, string city, string zipcode)
        {
            string address = $"{street} {zipcode} {city}";
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = client.GetAsync($"{_baseUrl}?address={address}&key={_apiKey}").Result;
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LocationObject>();
            }
            else
            {
                return null;
            }
        }

        public async Task<(double, double)?> GetLatitudeLongitudeAsync(string street, string city, string zipcode)
        {
            var location = await GetLocationObject(street, city, zipcode);
            if (location != null)
            {
                return (location.results[0].geometry.location.lat, location.results[0].geometry.location.lng);
            }
            else
            {
                return null;
            }
        }
    }
}
