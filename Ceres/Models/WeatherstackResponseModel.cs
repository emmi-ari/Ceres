using Newtonsoft.Json;

namespace Ceres.Models.Apparyllis
{
    public class WeatherStackModel
    {
        [JsonProperty("request")]
        public Request Request { get; init; }

        [JsonProperty("location")]
        public Location Location { get; init; }

        [JsonProperty("current")]
        public Current Current { get; init; }
    }

    public class Request
    {
        [JsonProperty("type")]
        public string Type { get; init; }

        [JsonProperty("query")]
        public string Query { get; init; }

        [JsonProperty("language")]
        public string Language { get; init; }

        [JsonProperty("unit")]
        public string Unit { get; init; }
    }

    public class Location
    {
        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("country")]
        public string Country { get; init; }

        [JsonProperty("region")]
        public string Region { get; init; }

        [JsonProperty("lat")]
        public string Lat { get; init; }

        [JsonProperty("lon")]
        public string Lon { get; init; }

        [JsonProperty("timezone_id")]
        public string TimezoneId { get; init; }

        [JsonProperty("localtime")]
        public string Localtime { get; init; }

        [JsonProperty("localtime_epoch")]
        public int LocaltimeEpoch { get; init; }

        [JsonProperty("utc_offinit")]
        public string UtcOffinit { get; init; }
    }

    public class Current
    {
        [JsonProperty("observation_time")]
        public string ObservationTime { get; init; }

        [JsonProperty("temperature")]
        public int Temperature { get; init; }

        [JsonProperty("weather_code")]
        public int WeatherCode { get; init; }

        [JsonProperty("weather_icons")]
        public List<string> WeatherIcons { get; init; }

        [JsonProperty("weather_descriptions")]
        public List<string> WeatherDescriptions { get; init; }

        [JsonProperty("wind_speed")]
        public int WindSpeed { get; init; }

        [JsonProperty("wind_degree")]
        public int WindDegree { get; init; }

        [JsonProperty("wind_dir")]
        public string WindDir { get; init; }

        [JsonProperty("pressure")]
        public int Pressure { get; init; }

        [JsonProperty("precip")]
        public double Precip { get; init; }

        [JsonProperty("humidity")]
        public int Humidity { get; init; }

        [JsonProperty("cloudcover")]
        public int Cloudcover { get; init; }

        [JsonProperty("feelslike")]
        public int Feelslike { get; init; }

        [JsonProperty("uv_index")]
        public int UvIndex { get; init; }

        [JsonProperty("visibility")]
        public int Visibility { get; init; }

        [JsonProperty("is_day")]
        public string IsDay { get; init; }
    }
}