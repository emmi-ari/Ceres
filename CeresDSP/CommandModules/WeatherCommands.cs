using CeresDSP.Models;
using CeresDSP.Models.Apparyllis;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Newtonsoft.Json;

using System.Text;
using System.Text.RegularExpressions;

namespace CeresDSP.CommandModules
{
    public class WeatherCommands : BaseCommandModule
    {
        private readonly Configuration _config;
        private readonly HttpClient _weatherStackApi;
        private readonly string _userWeatherConfigPath = Path.Combine(AppContext.BaseDirectory, "user_weather_defaults.conf");

        public WeatherCommands(Configuration config)
        {
            _config = config;
            HttpClient weatherStackApi = new()
            {
                BaseAddress = new("http://api.weatherstack.com/"),
                Timeout = new(0, 0, 10)
            };
            weatherStackApi.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _weatherStackApi = weatherStackApi;
        }

        private static string ParseWindDirection(string abbreviation)
        {
            if (abbreviation.Length > 3)
                throw new ArgumentException($"{nameof(abbreviation)} can not contain less than 1 or more than 3 chars");

            StringBuilder parsedWindDirection = new(4, 15);

            for (int i = 0; i < abbreviation.Length; i++)
            {
                switch (abbreviation[i])
                {
                    case 'N':
                        parsedWindDirection.Append("Nord-");
                        break;
                    case 'E':
                        parsedWindDirection.Append("Ost-");
                        break;
                    case 'S':
                        parsedWindDirection.Append("Süd-");
                        break;
                    case 'W':
                        parsedWindDirection.Append("West-");
                        break;
                }
            }
            return parsedWindDirection.ToString().TrimEnd('-');
        }

        private static string ParseUvIndex(int uvIndex) =>
            uvIndex switch
            {
                1 or 2       => $"{uvIndex} (Niedrig)",
                3 or 4 or 5  => $"{uvIndex} (Mittel)",
                6 or 7       => $"```ANSI\n\u001b[0;31m{uvIndex} (Hoch)\u001b[0;00m```",
                8 or 9 or 10 => $"```ANSI\n\u001b[0;35m{uvIndex} (Sehr hoch)\u001b[0;00m```",
                >= 11        => $"```ANSI\n\u001b[4;35m{uvIndex} (Extrem)\u001b[0;00m```",
                _            => $"{uvIndex}"
            };

        private async Task<string> GetDefaultPlaceForUserId(ulong uid)
        {
            string defaultLocationForUser = null;
            List<Dictionary<ulong, string>> defaultWeatherConfig = await GetDefaultWeatherConfig();
            Dictionary<ulong, string> userSpecificLocationConfig = defaultWeatherConfig?.Where(entry => entry.ContainsKey(uid)).FirstOrDefault();
            userSpecificLocationConfig?.TryGetValue(uid, out defaultLocationForUser);
            return defaultLocationForUser;
        }

        private void ChangeDefaultWeatherConfigForUser(ulong uid, string place, bool modify)
        {
            if (!File.Exists(_userWeatherConfigPath)) throw new FileLoadException("File doesn't exist or is inaccessible", _userWeatherConfigPath);
            if (modify)
            {
                string file = File.ReadAllText(_userWeatherConfigPath);
                string[] replacementLine = Regex.Replace(file, @$"({uid},)[\w ]+", $"$1{place}", RegexOptions.Multiline)
                    .Split(Environment.NewLine);
                File.WriteAllLines(_userWeatherConfigPath, replacementLine.Where(line => !string.IsNullOrEmpty(line)));
            }
            else // Append
            {
                List<string> file = File.ReadAllLines(_userWeatherConfigPath).ToList();
                file.Add($"{uid},{place}");
                File.WriteAllLines(_userWeatherConfigPath, file.Where(line => !string.IsNullOrEmpty(line)));
            }
        }

        [Command("weather")]
        [Aliases("wetter", "w")]
        public async Task Weather(CommandContext ctx, [RemainingText] string place = "")
            => await WeatherDynamic(ctx, place);
        internal async Task Weather(InteractionContext ctx, [RemainingText] string place = "")
            => await WeatherDynamic(ctx, place);
        private async Task WeatherDynamic(dynamic ctx, [RemainingText] string place = "")
        {
            string defaultPlaceForUser = await GetDefaultPlaceForUserId(ctx.User.Id);

            if (string.IsNullOrEmpty(place) && string.IsNullOrEmpty(defaultPlaceForUser))
            {
                await Helper.RespondToCommand(ctx, $"You have to provide a place name if you haven't set a default place with {_config.Ceres.Prefix}defaultWeather yet. That will be saved in an unencrypted CSV file, where the Discord User ID and the provided location will be saved.");
                return;
            }

            if (place == string.Empty && !string.IsNullOrWhiteSpace(defaultPlaceForUser))
                place = defaultPlaceForUser;

            if (place == "frankfurt".ToLower() && ctx.User.Id == 346295434546774016)
                place = "Frankfurt an der Oder";

            HttpResponseMessage response = await _weatherStackApi.GetAsync($"current?access_key={_config.Weatherstack.Token}&query={place}");
            WeatherStackModel serializedResponse = JsonConvert.DeserializeObject<WeatherStackModel>(await response.Content.ReadAsStringAsync());
            string location = place.ToLower() == "frankfurt" ? "Frankfurt am Main" : serializedResponse.Location.Name;

            DiscordEmbedBuilder embed = new()
            {
                Title = $"Wetter für {location}, {serializedResponse.Location.Region}, {serializedResponse.Location.Country}",
                Color = new DiscordColor(45, 122, 185),
                Description = $"# {serializedResponse.Current.WeatherDescriptions[0]} {serializedResponse.Current.Temperature} °C",
                Footer = new()
                {
                    Text = "Data provided by WeatherStack API",
                    IconUrl = "https://rapidapi-prod-apis.s3.amazonaws.com/c2139e70-bb7e-4aaa-81e9-b8f70cdb77d4.png"
                }
            };

            embed.AddField("Luftdruck", $"{serializedResponse.Current.Pressure} mBar");
            embed.AddField("Wind", $"{serializedResponse.Current.WindSpeed} km/h {ParseWindDirection(serializedResponse.Current.WindDir)} ({serializedResponse.Current.WindDegree} °)");
            embed.AddField("Relative Luftfeuchte", $"{serializedResponse.Current.Humidity} %");
            embed.AddField("Feels like", $"{serializedResponse.Current.Feelslike} °C");
            embed.AddField("Bedeckung", $"{serializedResponse.Current.Cloudcover} %");
            embed.AddField("Niederschlagswahrscheinlichkeit", $"{serializedResponse.Current.Precip} %");
            embed.AddField("UV Index", $"{ParseUvIndex(serializedResponse.Current.UvIndex)}");
            embed.AddField("Uhrzeit", $"{DateTime.Parse(serializedResponse.Current.ObservationTime).ToLocalTime():t}");
            embed.WithThumbnail(serializedResponse.Current.WeatherIcons[0]);

            await Helper.RespondToCommand(ctx, embed.Build());
        }

        [Command("defaultWeather")]
        [Aliases("dw")]
        public async Task UserSetDefaultWeatherLocation(CommandContext ctx, [RemainingText] string place = "")
            => await UserSetDefaultWeatherLocationDynamic(ctx, place);
        internal async Task UserSetDefaultWeatherLocation(InteractionContext ctx, [RemainingText] string place = "")
            => await UserSetDefaultWeatherLocationDynamic(ctx, place);
        private async Task UserSetDefaultWeatherLocationDynamic(dynamic ctx, [RemainingText] string place = "")
        {
            if (string.IsNullOrEmpty(place))
                throw new ArgumentException($"'{nameof(place)}' cannot be null or empty.", nameof(place));

            HttpResponseMessage response = await _weatherStackApi.GetAsync($"current?access_key={_config.Weatherstack.Token}&query={place}");
            WeatherStackModel serializedResponse = JsonConvert.DeserializeObject<WeatherStackModel>(await response.Content.ReadAsStringAsync());

            if (serializedResponse.Current is null && serializedResponse.Location is null)
            {
                await Helper.RespondToCommand(ctx, $"{place} was not found by the WeatherStack API.");
                return;
            }

            List<Dictionary<ulong, string>> defaultWeatherConfig = await GetDefaultWeatherConfig();
            bool modifyExistentDefaultValue = defaultWeatherConfig.Where(entry => entry.ContainsKey(ctx.User.Id)).Any();
            try
            {
                ChangeDefaultWeatherConfigForUser(ctx.User.Id, place, modifyExistentDefaultValue);
                await Helper.RespondToCommand(ctx, $"Changed your default location to {place}.");
            }
            catch (Exception ex)
            {
                await Helper.RespondToCommand(ctx, $"Something went horribly wrong. Maybe this text will help: {ex.Message} (0x{ex.HResult:X8})");
            }
        }

        private async Task<List<Dictionary<ulong, string>>> GetDefaultWeatherConfig()
        {
            List<Dictionary<ulong, string>> userWeatherConfig = new();
            if (File.Exists(_userWeatherConfigPath))
            {
                string[] configLines = await File.ReadAllLinesAsync(_userWeatherConfigPath);
                foreach (string line in configLines)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                        continue;
                    Dictionary<ulong, string> entry = new();
                    dynamic[] lineData = line.Split(',');
                    entry.Add(ulong.Parse(lineData[0]), lineData[1]);
                    userWeatherConfig.Add(entry);
                }
            }
            else
                await File.WriteAllTextAsync(_userWeatherConfigPath, null);

            return userWeatherConfig;
        }
    }

    [SlashCommandGroup("Weather", "Weather commands")]
    public class WeatherCommandsSlash : ApplicationCommandModule
    {
        WeatherCommands WeatherCommands { get; init; }

        public WeatherCommandsSlash(Configuration config)
        {
            WeatherCommands = new(config);
        }

        [SlashCommand("Show", "Returns weather of either your default place or a provided place")]
        public async Task Weather(InteractionContext ctx,
            [Option("Place", "The place for where you want to look the weather up")] string place = "")
            => await WeatherCommands.Weather(ctx, place);

        [SlashCommand("DefaultLocation", "Sets the default location for you, so that you can use the weather command without a place")]
        public async Task UserSetDefaultWeatherLocation(InteractionContext ctx,
            [Option("Place", "Default place")] string place)
            => await WeatherCommands.UserSetDefaultWeatherLocation(ctx, place);
    }
}
