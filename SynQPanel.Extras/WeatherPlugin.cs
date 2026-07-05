using SynQPanel.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SynQPanel.Extras
{
    /// <summary>
    /// Weather Plugin for SynQPanel
    /// Provides comprehensive weather data including current conditions and today's
    /// forecast using Open-Meteo (https://open-meteo.com), a free service that
    /// requires no API key. Location is resolved via IP geolocation (ip-api.com) or
    /// Open-Meteo's geocoding API when a specific place name is configured.
    /// </summary>
    public sealed class WeatherPlugin : BasePlugin
    {
        // Configuration
        private const string CONFIG_FILE = "weather_config.ini";
        private string? _apiKey; // No longer required (Open-Meteo is free/keyless); kept for backward-compat ini parsing.
        private string? _location;
        private bool _useFahrenheit = false;
        private string _language = "pt";

        // Resolved location (cached after first successful lookup)
        private bool _locationResolved = false;
        private double? _resolvedLatitude;
        private double? _resolvedLongitude;

        // HTTP client for API requests
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Status tracking
        private DateTime _lastSuccessfulUpdate = DateTime.MinValue;
        private string _lastError = string.Empty;

        // ===================================================================
        // LOCATION & STATUS (Text + Numeric)
        // ===================================================================
        private readonly PluginText _cityName;
        private readonly PluginText _region;
        private readonly PluginText _country;
        private readonly PluginSensor _latitude;
        private readonly PluginSensor _longitude;
        private readonly PluginText _timezone;
        private readonly PluginText _localTime;
        private readonly PluginText _lastUpdate;
        private readonly PluginText _updateStatus;

        // ===================================================================
        // CURRENT WEATHER (Text + Numeric)
        // ===================================================================
        private readonly PluginSensor _temperature;
        private readonly PluginSensor _feelsLike;
        private readonly PluginText _condition;
        private readonly PluginSensor _conditionCode;
        private readonly PluginText _iconUrl;

        private readonly PluginSensor _humidity;
        private readonly PluginSensor _cloudCover;
        private readonly PluginSensor _dewPoint;

        private readonly PluginSensor _windSpeed;
        private readonly PluginSensor _windDirection;
        private readonly PluginText _windDirectionText;
        private readonly PluginSensor _windGust;

        private readonly PluginSensor _pressure;
        private readonly PluginSensor _precipitation;
        private readonly PluginSensor _visibility;
        private readonly PluginSensor _uvIndex;

        private readonly PluginSensor _isDay;

        // ===================================================================
        // AIR QUALITY
        // ===================================================================
        private readonly PluginSensor _aqiUS;
        private readonly PluginSensor _aqiUK;
        private readonly PluginSensor _pm2_5;
        private readonly PluginSensor _pm10;
        private readonly PluginSensor _co;
        private readonly PluginSensor _no2;
        private readonly PluginSensor _o3;
        private readonly PluginSensor _so2;

        // ===================================================================
        // ASTRONOMY (Text + Numeric)
        // ===================================================================
        private readonly PluginText _sunrise;
        private readonly PluginText _sunset;
        private readonly PluginText _moonrise;
        private readonly PluginText _moonset;
        private readonly PluginText _moonPhase;
        private readonly PluginSensor _moonIllumination;

        // ===================================================================
        // TODAY'S FORECAST
        // ===================================================================
        private readonly PluginSensor _maxTemp;
        private readonly PluginSensor _minTemp;
        private readonly PluginSensor _avgTemp;
        private readonly PluginSensor _maxWind;
        private readonly PluginSensor _totalPrecipitation;
        private readonly PluginSensor _totalSnow;
        private readonly PluginSensor _avgVisibility;
        private readonly PluginSensor _avgHumidity;
        private readonly PluginSensor _chanceOfRain;
        private readonly PluginSensor _chanceOfSnow;
        private readonly PluginSensor _uvIndexForecast;

        // ===================================================================
        // TOMORROW'S FORECAST
        // ===================================================================
        private readonly PluginText _tomorrowDate;
        private readonly PluginSensor _tomorrowMaxTemp;
        private readonly PluginSensor _tomorrowMinTemp;
        private readonly PluginSensor _tomorrowAvgTemp;
        private readonly PluginText _tomorrowCondition;
        private readonly PluginText _tomorrowIconUrl;
        private readonly PluginSensor _tomorrowMaxWind;
        private readonly PluginSensor _tomorrowPrecipitation;
        private readonly PluginSensor _tomorrowChanceOfRain;
        private readonly PluginSensor _tomorrowChanceOfSnow;

        // ===================================================================
        // EXTENDED FORECAST (Days 3–5)
        // ===================================================================
        private readonly PluginText _day3Date;
        private readonly PluginText _day3Condition;
        private readonly PluginText _day3IconUrl;
        private readonly PluginSensor _day3AvgTemp;
        private readonly PluginSensor _day3ChanceOfRain;

        private readonly PluginText _day4Date;
        private readonly PluginText _day4Condition;
        private readonly PluginText _day4IconUrl;
        private readonly PluginSensor _day4AvgTemp;
        private readonly PluginSensor _day4ChanceOfRain;

        private readonly PluginText _day5Date;
        private readonly PluginText _day5Condition;
        private readonly PluginText _day5IconUrl;
        private readonly PluginSensor _day5AvgTemp;
        private readonly PluginSensor _day5ChanceOfRain;


        // ===================================================================
        // ICONS
        // ===================================================================
        private readonly PluginText _todayIconUrl;

        // Hourly forecast icons (next 6 hours)
        private readonly PluginText _hour1IconUrl;
        private readonly PluginText _hour1Time;
        private readonly PluginText _hour2IconUrl;
        private readonly PluginText _hour2Time;
        private readonly PluginText _hour3IconUrl;
        private readonly PluginText _hour3Time;
        private readonly PluginText _hour4IconUrl;
        private readonly PluginText _hour4Time;
        private readonly PluginText _hour5IconUrl;
        private readonly PluginText _hour5Time;
        private readonly PluginText _hour6IconUrl;
        private readonly PluginText _hour6Time;
        private readonly PluginSensor _hour1Temp;
        private readonly PluginSensor _hour1ChanceOfRain;
        private readonly PluginSensor _hour2Temp;
        private readonly PluginSensor _hour2ChanceOfRain;
        private readonly PluginSensor _hour3Temp;
        private readonly PluginSensor _hour3ChanceOfRain;
        private readonly PluginSensor _hour4Temp;
        private readonly PluginSensor _hour4ChanceOfRain;
        private readonly PluginSensor _hour5Temp;
        private readonly PluginSensor _hour5ChanceOfRain;
        private readonly PluginSensor _hour6Temp;
        private readonly PluginSensor _hour6ChanceOfRain;




        public override string? ConfigFilePath => Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
            CONFIG_FILE
        );

        public override TimeSpan UpdateInterval => TimeSpan.FromMinutes(10);

        public WeatherPlugin()
            : base(
                "weather",
                "Weather",
                "Comprehensive weather information powered by Open-Meteo including current conditions, forecasts, air quality, and astronomy data."
            )
        {
            // Initialize Location & Status
            _cityName = new PluginText("city_name", "City Name", "-");
            _region = new PluginText("region", "Region", "-");
            _country = new PluginText("country", "Country", "-");
            _latitude = new PluginSensor("Latitude", 0, "°");
            _longitude = new PluginSensor("Longitude", 0, "°");
            _timezone = new PluginText("timezone", "Timezone", "-");
            _localTime = new PluginText("local_time", "Local Time", "-");
            _lastUpdate = new PluginText("last_update", "Last Update", "-");
            _updateStatus = new PluginText("status", "Status", "Not updated");

            // Initialize Current Weather
            _temperature = new PluginSensor("Temperature", 0, "°C");
            _feelsLike = new PluginSensor("Feels Like", 0, "°C");
            _condition = new PluginText("condition", "Condition", "-");
            _conditionCode = new PluginSensor("Condition Code", 0, "");
            _iconUrl = new PluginText("icon_url", "Icon URL", "-");

            _humidity = new PluginSensor("Humidity", 0, "%");
            _cloudCover = new PluginSensor("Cloud Cover", 0, "%");
            _dewPoint = new PluginSensor("Dew Point", 0, "°C");

            _windSpeed = new PluginSensor("Wind Speed", 0, "km/h");
            _windDirection = new PluginSensor("Wind Direction", 0, "°");
            _windDirectionText = new PluginText("wind_dir_text", "Wind Direction", "-");
            _windGust = new PluginSensor("Wind Gust", 0, "km/h");

            _pressure = new PluginSensor("Pressure", 0, "mb");
            _precipitation = new PluginSensor("Precipitation", 0, "mm");
            _visibility = new PluginSensor("Visibility", 0, "km");
            _uvIndex = new PluginSensor("UV Index", 0, "");

            _isDay = new PluginSensor("Is Day", 0, "");

            // Initialize Air Quality
            _aqiUS = new PluginSensor("AQI (US EPA)", 0, "");
            _aqiUK = new PluginSensor("AQI (UK DEFRA)", 0, "");
            _pm2_5 = new PluginSensor("PM2.5", 0, "μg/m³");
            _pm10 = new PluginSensor("PM10", 0, "μg/m³");
            _co = new PluginSensor("CO", 0, "μg/m³");
            _no2 = new PluginSensor("NO2", 0, "μg/m³");
            _o3 = new PluginSensor("O3", 0, "μg/m³");
            _so2 = new PluginSensor("SO2", 0, "μg/m³");

            // Initialize Astronomy
            _sunrise = new PluginText("sunrise", "Sunrise", "-");
            _sunset = new PluginText("sunset", "Sunset", "-");
            _moonrise = new PluginText("moonrise", "Moonrise", "-");
            _moonset = new PluginText("moonset", "Moonset", "-");
            _moonPhase = new PluginText("moon_phase", "Moon Phase", "-");
            _moonIllumination = new PluginSensor("Moon Illumination", 0, "%");

            // Initialize Today's Forecast
            _maxTemp = new PluginSensor("Max Temperature", 0, "°C");
            _minTemp = new PluginSensor("Min Temperature", 0, "°C");
            _avgTemp = new PluginSensor("Avg Temperature", 0, "°C");
            _maxWind = new PluginSensor("Max Wind", 0, "km/h");
            _totalPrecipitation = new PluginSensor("Total Precipitation", 0, "mm");
            _totalSnow = new PluginSensor("Total Snow", 0, "cm");
            _avgVisibility = new PluginSensor("Avg Visibility", 0, "km");
            _avgHumidity = new PluginSensor("Avg Humidity", 0, "%");
            _chanceOfRain = new PluginSensor("Chance of Rain", 0, "%");
            _chanceOfSnow = new PluginSensor("Chance of Snow", 0, "%");
            _uvIndexForecast = new PluginSensor("UV Index", 0, "");

            // Initialize Tomorrow's Forecast
            _tomorrowDate = new PluginText("day2_date", "Tomorrow Date", "-");
            _tomorrowMaxTemp = new PluginSensor("Max Temperature", 0, "°C");
            _tomorrowMinTemp = new PluginSensor("Min Temperature", 0, "°C");
            _tomorrowAvgTemp = new PluginSensor("Avg Temperature", 0, "°C");
            _tomorrowCondition = new PluginText("tomorrow_condition", "Condition", "-");
            _tomorrowIconUrl = new PluginText("tomorrow_icon_url", "Icon URL", "-");
            _tomorrowMaxWind = new PluginSensor("Max Wind", 0, "km/h");
            _tomorrowPrecipitation = new PluginSensor("Total Precipitation", 0, "mm");
            _tomorrowChanceOfRain = new PluginSensor("Chance of Rain", 0, "%");
            _tomorrowChanceOfSnow = new PluginSensor("Chance of Snow", 0, "%");
            _todayIconUrl = new PluginText("today_icon_url", "Today Icon URL", "-");

            // Extended Forecast (Days 3–5)
            _day3Date = new PluginText("day3_date", "Day 3 Date", "-");
            _day3Condition = new PluginText("day3_condition", "Day 3 Condition", "-");
            _day3IconUrl = new PluginText("day3_icon_url", "Day 3 Icon URL", "-");
            _day3AvgTemp = new PluginSensor("Day 3 Avg Temperature", 0, "°C");
            _day3ChanceOfRain = new PluginSensor("Day 3 Chance of Rain", 0, "%");

            _day4Date = new PluginText("day4_date", "Day 4 Date", "-");
            _day4Condition = new PluginText("day4_condition", "Day 4 Condition", "-");
            _day4IconUrl = new PluginText("day4_icon_url", "Day 4 Icon URL", "-");
            _day4AvgTemp = new PluginSensor("Day 4 Avg Temperature", 0, "°C");
            _day4ChanceOfRain = new PluginSensor("Day 4 Chance of Rain", 0, "%");

            _day5Date = new PluginText("day5_date", "Day 5 Date", "-");
            _day5Condition = new PluginText("day5_condition", "Day 5 Condition", "-");
            _day5IconUrl = new PluginText("day5_icon_url", "Day 5 Icon URL", "-");
            _day5AvgTemp = new PluginSensor("Day 5 Avg Temperature", 0, "°C");
            _day5ChanceOfRain = new PluginSensor("Day 5 Chance of Rain", 0, "%");

            // Hourly icons
            _hour1IconUrl = new PluginText("hour1_icon_url", "Hour 1 Icon", "-");
            _hour1Time = new PluginText("hour1_time", "Hour 1 Time", "-");
            _hour2IconUrl = new PluginText("hour2_icon_url", "Hour 2 Icon", "-");
            _hour2Time = new PluginText("hour2_time", "Hour 2 Time", "-");
            _hour3IconUrl = new PluginText("hour3_icon_url", "Hour 3 Icon", "-");
            _hour3Time = new PluginText("hour3_time", "Hour 3 Time", "-");
            _hour4IconUrl = new PluginText("hour4_icon_url", "Hour 4 Icon", "-");
            _hour4Time = new PluginText("hour4_time", "Hour 4 Time", "-");
            _hour5IconUrl = new PluginText("hour5_icon_url", "Hour 5 Icon", "-");
            _hour5Time = new PluginText("hour5_time", "Hour 5 Time", "-");
            _hour6IconUrl = new PluginText("hour6_icon_url", "Hour 6 Icon", "-");
            _hour6Time = new PluginText("hour6_time", "Hour 6 Time", "-");

            //Hourly extended
            _hour1Temp = new PluginSensor("Hour 1 Temperature", 0, "°C");
            _hour1ChanceOfRain = new PluginSensor("Hour 1 Chance of Rain", 0, "%");

            _hour2Temp = new PluginSensor("Hour 2 Temperature", 0, "°C");
            _hour2ChanceOfRain = new PluginSensor("Hour 2 Chance of Rain", 0, "%");

            _hour3Temp = new PluginSensor("Hour 3 Temperature", 0, "°C");
            _hour3ChanceOfRain = new PluginSensor("Hour 3 Chance of Rain", 0, "%");

            _hour4Temp = new PluginSensor("Hour 4 Temperature", 0, "°C");
            _hour4ChanceOfRain = new PluginSensor("Hour 4 Chance of Rain", 0, "%");

            _hour5Temp = new PluginSensor("Hour 5 Temperature", 0, "°C");
            _hour5ChanceOfRain = new PluginSensor("Hour 5 Chance of Rain", 0, "%");

            _hour6Temp = new PluginSensor("Hour 6 Temperature", 0, "°C");
            _hour6ChanceOfRain = new PluginSensor("Hour 6 Chance of Rain", 0, "%");

        }

        public override void Initialize()
        {
            LoadConfiguration();
        }

        public override void Load(List<IPluginContainer> containers)
        {
            // Location & Status
            var location = new PluginContainer("weather-location", "Weather · Location");
            location.Entries.Add(_cityName);
            location.Entries.Add(_region);
            location.Entries.Add(_country);
            location.Entries.Add(_latitude);
            location.Entries.Add(_longitude);
            location.Entries.Add(_timezone);
            location.Entries.Add(_localTime);
            location.Entries.Add(_lastUpdate);
            location.Entries.Add(_updateStatus);

            // Current Conditions
            var current = new PluginContainer("weather-current", "Weather · Current");
            current.Entries.Add(_temperature);
            current.Entries.Add(_feelsLike);
            current.Entries.Add(_condition);
            current.Entries.Add(_conditionCode);
            current.Entries.Add(_iconUrl);
            current.Entries.Add(_isDay);
            current.Entries.Add(_humidity);
            current.Entries.Add(_cloudCover);
            current.Entries.Add(_dewPoint);

            // Wind & Pressure
            var wind = new PluginContainer("weather-wind", "Weather · Wind & Pressure");
            wind.Entries.Add(_windSpeed);
            wind.Entries.Add(_windDirection);
            wind.Entries.Add(_windDirectionText);
            wind.Entries.Add(_windGust);
            wind.Entries.Add(_pressure);
            wind.Entries.Add(_precipitation);
            wind.Entries.Add(_visibility);
            wind.Entries.Add(_uvIndex);

            // Air Quality
            var airQuality = new PluginContainer("weather-air", "Weather · Air Quality");
            airQuality.Entries.Add(_aqiUS);
            airQuality.Entries.Add(_aqiUK);
            airQuality.Entries.Add(_pm2_5);
            airQuality.Entries.Add(_pm10);
            airQuality.Entries.Add(_co);
            airQuality.Entries.Add(_no2);
            airQuality.Entries.Add(_o3);
            airQuality.Entries.Add(_so2);

            // Astronomy
            var astronomy = new PluginContainer("weather-astro", "Weather · Astronomy");
            astronomy.Entries.Add(_sunrise);
            astronomy.Entries.Add(_sunset);
            astronomy.Entries.Add(_moonrise);
            astronomy.Entries.Add(_moonset);
            astronomy.Entries.Add(_moonPhase);
            astronomy.Entries.Add(_moonIllumination);

            // Today's Forecast
            var today = new PluginContainer("weather-today", "Weather · Today's Forecast");
            today.Entries.Add(_maxTemp);
            today.Entries.Add(_minTemp);
            today.Entries.Add(_avgTemp);
            today.Entries.Add(_maxWind);
            today.Entries.Add(_totalPrecipitation);
            today.Entries.Add(_totalSnow);
            today.Entries.Add(_avgVisibility);
            today.Entries.Add(_avgHumidity);
            today.Entries.Add(_chanceOfRain);
            today.Entries.Add(_chanceOfSnow);
            today.Entries.Add(_uvIndexForecast);
            today.Entries.Add(_todayIconUrl);

            // Tomorrow's Forecast
            var tomorrow = new PluginContainer("weather-tomorrow", "Weather · Tomorrow");
            tomorrow.Entries.Add(_tomorrowDate);
            tomorrow.Entries.Add(_tomorrowMaxTemp);
            tomorrow.Entries.Add(_tomorrowMinTemp);
            tomorrow.Entries.Add(_tomorrowAvgTemp);
            tomorrow.Entries.Add(_tomorrowCondition);
            tomorrow.Entries.Add(_tomorrowIconUrl);
            tomorrow.Entries.Add(_tomorrowMaxWind);
            tomorrow.Entries.Add(_tomorrowPrecipitation);
            tomorrow.Entries.Add(_tomorrowChanceOfRain);
            tomorrow.Entries.Add(_tomorrowChanceOfSnow);

            // HOURLY;
            var hourly = new PluginContainer("weather-hourly", "Weather · Hourly Forecast");
            hourly.Entries.Add(_hour1Time);
            hourly.Entries.Add(_hour1IconUrl);
            hourly.Entries.Add(_hour2Time);
            hourly.Entries.Add(_hour2IconUrl);
            hourly.Entries.Add(_hour3Time);
            hourly.Entries.Add(_hour3IconUrl);
            hourly.Entries.Add(_hour4Time);
            hourly.Entries.Add(_hour4IconUrl);
            hourly.Entries.Add(_hour5Time);
            hourly.Entries.Add(_hour5IconUrl);
            hourly.Entries.Add(_hour6Time);
            hourly.Entries.Add(_hour6IconUrl);

            //Hourly Extended
            hourly.Entries.Add(_hour1Time);
            hourly.Entries.Add(_hour1IconUrl);
            hourly.Entries.Add(_hour1Temp);
            hourly.Entries.Add(_hour1ChanceOfRain);

            hourly.Entries.Add(_hour2Time);
            hourly.Entries.Add(_hour2IconUrl);
            hourly.Entries.Add(_hour2Temp);
            hourly.Entries.Add(_hour2ChanceOfRain);

            hourly.Entries.Add(_hour3Time);
            hourly.Entries.Add(_hour3IconUrl);
            hourly.Entries.Add(_hour3Temp);
            hourly.Entries.Add(_hour3ChanceOfRain);

            hourly.Entries.Add(_hour4Time);
            hourly.Entries.Add(_hour4IconUrl);
            hourly.Entries.Add(_hour4Temp);
            hourly.Entries.Add(_hour4ChanceOfRain);

            hourly.Entries.Add(_hour5Time);
            hourly.Entries.Add(_hour5IconUrl);
            hourly.Entries.Add(_hour5Temp);
            hourly.Entries.Add(_hour5ChanceOfRain);

            hourly.Entries.Add(_hour6Time);
            hourly.Entries.Add(_hour6IconUrl);
            hourly.Entries.Add(_hour6Temp);
            hourly.Entries.Add(_hour6ChanceOfRain);


            var extended = new PluginContainer("weather-extended", "Weather · Extended Forecast");

            extended.Entries.Add(_day3Date);
            extended.Entries.Add(_day3Condition);
            extended.Entries.Add(_day3IconUrl);
            extended.Entries.Add(_day3AvgTemp);
            extended.Entries.Add(_day3ChanceOfRain);

            extended.Entries.Add(_day4Date);
            extended.Entries.Add(_day4Condition);
            extended.Entries.Add(_day4IconUrl);
            extended.Entries.Add(_day4AvgTemp);
            extended.Entries.Add(_day4ChanceOfRain);

            extended.Entries.Add(_day5Date);
            extended.Entries.Add(_day5Condition);
            extended.Entries.Add(_day5IconUrl);
            extended.Entries.Add(_day5AvgTemp);
            extended.Entries.Add(_day5ChanceOfRain);

            containers.Add(location);
            containers.Add(current);
            containers.Add(wind);
            containers.Add(airQuality);
            containers.Add(astronomy);
            containers.Add(today);
            containers.Add(tomorrow);
            containers.Add(hourly);
            containers.Add(extended);
        }

        [PluginAction("Edit Configuration")]
        public void OpenConfigFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(ConfigFilePath))
                {
                    if (!File.Exists(ConfigFilePath))
                    {
                        CreateDefaultConfig();
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ConfigFilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Failed to open config file: {ex.Message}";
            }
        }

        [PluginAction("Reload Configuration")]
        public void ReloadConfig()
        {
            // Reset the cached location resolution so editing Location in the ini
            // takes effect on the next update instead of requiring an app restart.
            _locationResolved = false;
            _resolvedLatitude = null;
            _resolvedLongitude = null;

            LoadConfiguration();
        }

        public override void Update()
        {
            // Synchronous update not used - all updates are async
        }

        public override async Task UpdateAsync(CancellationToken cancellationToken)
        {
            try
            {
                await FetchWeatherDataAsync(cancellationToken);
                _updateStatus.Value = "OK";
                _lastSuccessfulUpdate = DateTime.Now;
                _lastUpdate.Value = _lastSuccessfulUpdate.ToString("HH:mm:ss");
                _lastError = string.Empty;
            }
            catch (OperationCanceledException)
            {
                // Shutdown/cancellation requested - not a failure, so don't clobber status.
            }
            catch (Exception ex)
            {
                _updateStatus.Value = "Error";
                _lastError = ex.Message;
            }
        }

        /// <summary>
        /// Resolves the configured location to a latitude/longitude pair and populates
        /// the location-related sensors. The result is cached after the first success
        /// so subsequent updates skip the geocoding/IP-lookup round trip.
        /// </summary>
        private async Task ResolveLocationAsync(CancellationToken cancellationToken)
        {
            if (_locationResolved)
                return;

            var loc = _location?.Trim();
            bool useIp = string.IsNullOrEmpty(loc)
                || loc.Equals("auto", StringComparison.OrdinalIgnoreCase)
                || loc.Equals("auto:ip", StringComparison.OrdinalIgnoreCase);

            if (useIp)
            {
                var response = await _httpClient.GetAsync("http://ip-api.com/json", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var data = JsonDocument.Parse(json);
                var root = data.RootElement;

                var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
                if (status != "success")
                {
                    throw new Exception("IP geolocation lookup failed");
                }

                _resolvedLatitude = root.GetProperty("lat").GetDouble();
                _resolvedLongitude = root.GetProperty("lon").GetDouble();

                _cityName.Value = root.TryGetProperty("city", out var city) ? city.GetString() ?? "-" : "-";
                _region.Value = root.TryGetProperty("regionName", out var region) ? region.GetString() ?? "-" : "-";
                _country.Value = root.TryGetProperty("country", out var country) ? country.GetString() ?? "-" : "-";
                _timezone.Value = root.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "-" : "-";
            }
            else
            {
                string url = "https://geocoding-api.open-meteo.com/v1/search" +
                    $"?name={Uri.EscapeDataString(loc!)}&count=1&language={Uri.EscapeDataString(_language)}&format=json";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var data = JsonDocument.Parse(json);
                var root = data.RootElement;

                if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                {
                    throw new Exception($"Location '{loc}' not found");
                }

                var first = results[0];
                _resolvedLatitude = first.GetProperty("latitude").GetDouble();
                _resolvedLongitude = first.GetProperty("longitude").GetDouble();

                _cityName.Value = first.TryGetProperty("name", out var name) ? name.GetString() ?? "-" : "-";
                _region.Value = first.TryGetProperty("admin1", out var admin1) ? admin1.GetString() ?? "-" : "-";
                _country.Value = first.TryGetProperty("country", out var country) ? country.GetString() ?? "-" : "-";
                _timezone.Value = first.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "-" : "-";
            }

            _latitude.Value = (float)_resolvedLatitude!.Value;
            _longitude.Value = (float)_resolvedLongitude!.Value;
            _locationResolved = true;
        }

        private async Task FetchWeatherDataAsync(CancellationToken cancellationToken)
        {
            await ResolveLocationAsync(cancellationToken);

            if (_resolvedLatitude is null || _resolvedLongitude is null)
                throw new Exception("Location could not be resolved");

            string lat = _resolvedLatitude.Value.ToString(CultureInfo.InvariantCulture);
            string lon = _resolvedLongitude.Value.ToString(CultureInfo.InvariantCulture);

            string url = "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}&longitude={lon}" +
                "&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,cloud_cover,surface_pressure,wind_speed_10m,wind_direction_10m,wind_gusts_10m,dew_point_2m,uv_index,visibility" +
                "&daily=temperature_2m_max,temperature_2m_min,weather_code,precipitation_sum,precipitation_probability_max,sunrise,sunset,uv_index_max,wind_speed_10m_max,snowfall_sum" +
                "&hourly=temperature_2m,weather_code,precipitation_probability,relative_humidity_2m,visibility" +
                "&timezone=auto&forecast_days=7";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var data = JsonDocument.Parse(json);
            var root = data.RootElement;

            // Open-Meteo returns the resolved IANA timezone for the coordinates; prefer it
            // over the one from geocoding/IP-lookup since it's authoritative for this location.
            if (root.TryGetProperty("timezone", out var tzProp))
            {
                var tzValue = tzProp.GetString();
                if (!string.IsNullOrEmpty(tzValue))
                    _timezone.Value = tzValue;
            }

            DateTime nowLocal = DateTime.Now;

            if (root.TryGetProperty("current", out var current))
            {
                ParseCurrentWeather(current);

                var currentTimeStr = TryGetString(current, "time");
                if (!string.IsNullOrEmpty(currentTimeStr)
                    && DateTime.TryParse(currentTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedNow))
                {
                    nowLocal = parsedNow;
                }
            }

            if (root.TryGetProperty("daily", out var daily))
            {
                ParseDailyForecast(daily);
            }

            if (root.TryGetProperty("hourly", out var hourly))
            {
                ParseHourlyForecast(hourly, nowLocal);
            }

            UpdateMoonPhase();

            // Air quality is fetched separately; a failure here must not break the
            // main weather update (temperature/condition/etc. already succeeded above).
            try
            {
                await FetchAirQualityAsync(lat, lon, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Best-effort: leave air-quality sensors at their previous/default values.
            }
        }

        private async Task FetchAirQualityAsync(string lat, string lon, CancellationToken cancellationToken)
        {
            string url = "https://air-quality-api.open-meteo.com/v1/air-quality" +
                $"?latitude={lat}&longitude={lon}" +
                "&current=pm10,pm2_5,carbon_monoxide,nitrogen_dioxide,sulphur_dioxide,ozone,us_aqi,european_aqi";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var data = JsonDocument.Parse(json);
            var root = data.RootElement;

            if (!root.TryGetProperty("current", out var current))
                return;

            var pm25 = TryGetDouble(current, "pm2_5");
            if (pm25.HasValue) _pm2_5.Value = (float)pm25.Value;

            var pm10 = TryGetDouble(current, "pm10");
            if (pm10.HasValue) _pm10.Value = (float)pm10.Value;

            var co = TryGetDouble(current, "carbon_monoxide");
            if (co.HasValue) _co.Value = (float)co.Value;

            var no2 = TryGetDouble(current, "nitrogen_dioxide");
            if (no2.HasValue) _no2.Value = (float)no2.Value;

            var o3 = TryGetDouble(current, "ozone");
            if (o3.HasValue) _o3.Value = (float)o3.Value;

            var so2 = TryGetDouble(current, "sulphur_dioxide");
            if (so2.HasValue) _so2.Value = (float)so2.Value;

            var usAqi = TryGetDouble(current, "us_aqi");
            if (usAqi.HasValue) _aqiUS.Value = (float)usAqi.Value;

            // Open-Meteo does not expose a UK DEFRA index; the European AQI is the
            // closest available equivalent and is used here as a stand-in.
            var euAqi = TryGetDouble(current, "european_aqi");
            if (euAqi.HasValue) _aqiUK.Value = (float)euAqi.Value;
        }

        private double ConvertTemp(double celsius) => _useFahrenheit ? (celsius * 9.0 / 5.0) + 32.0 : celsius;

        private void ParseCurrentWeather(JsonElement current)
        {
            // Temperature
            var tempC = TryGetDouble(current, "temperature_2m");
            if (tempC.HasValue) _temperature.Value = (float)ConvertTemp(tempC.Value);

            var feelsC = TryGetDouble(current, "apparent_temperature");
            if (feelsC.HasValue) _feelsLike.Value = (float)ConvertTemp(feelsC.Value);

            var dewC = TryGetDouble(current, "dew_point_2m");
            if (dewC.HasValue) _dewPoint.Value = (float)ConvertTemp(dewC.Value);

            // Condition (WMO weather code -> localized text)
            var weatherCode = TryGetInt(current, "weather_code");
            if (weatherCode.HasValue)
            {
                _condition.Value = GetConditionText(weatherCode.Value, _language);
                _conditionCode.Value = weatherCode.Value;
            }

            // Basic measurements
            var isDay = TryGetInt(current, "is_day");
            if (isDay.HasValue) _isDay.Value = isDay.Value;

            var humidity = TryGetDouble(current, "relative_humidity_2m");
            if (humidity.HasValue) _humidity.Value = (float)humidity.Value;

            var cloud = TryGetDouble(current, "cloud_cover");
            if (cloud.HasValue) _cloudCover.Value = (float)cloud.Value;

            // Wind
            var windSpeed = TryGetDouble(current, "wind_speed_10m");
            if (windSpeed.HasValue) _windSpeed.Value = (float)windSpeed.Value;

            var windDegree = TryGetDouble(current, "wind_direction_10m");
            if (windDegree.HasValue)
            {
                _windDirection.Value = (float)windDegree.Value;
                _windDirectionText.Value = DegreesToCompass(windDegree.Value);
            }

            var windGust = TryGetDouble(current, "wind_gusts_10m");
            if (windGust.HasValue) _windGust.Value = (float)windGust.Value;

            // Pressure & Precipitation
            var pressure = TryGetDouble(current, "surface_pressure");
            if (pressure.HasValue) _pressure.Value = (float)pressure.Value;

            var precip = TryGetDouble(current, "precipitation");
            if (precip.HasValue) _precipitation.Value = (float)precip.Value;

            // UV index & visibility (current conditions)
            var uv = TryGetDouble(current, "uv_index");
            if (uv.HasValue) _uvIndex.Value = (float)uv.Value;

            var visibilityMeters = TryGetDouble(current, "visibility");
            if (visibilityMeters.HasValue) _visibility.Value = (float)(visibilityMeters.Value / 1000.0);

            // Local time (Open-Meteo returns it in the "current.time" field, ISO 8601, local to the timezone)
            var timeStr = TryGetString(current, "time");
            if (!string.IsNullOrEmpty(timeStr)
                && DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                _localTime.Value = dt.ToString("yyyy-MM-dd HH:mm");
            }
        }

        private void ParseDailyForecast(JsonElement daily)
        {
            // --- Today (index 0) ---
            var maxC0 = TryGetArrayDouble(daily, "temperature_2m_max", 0);
            var minC0 = TryGetArrayDouble(daily, "temperature_2m_min", 0);

            if (maxC0.HasValue)
                _maxTemp.Value = (float)ConvertTemp(maxC0.Value);

            if (minC0.HasValue)
                _minTemp.Value = (float)ConvertTemp(minC0.Value);

            if (maxC0.HasValue && minC0.HasValue)
                _avgTemp.Value = (float)ConvertTemp((maxC0.Value + minC0.Value) / 2.0);

            var uvMax0 = TryGetArrayDouble(daily, "uv_index_max", 0);
            if (uvMax0.HasValue)
                _uvIndexForecast.Value = (float)uvMax0.Value;

            var maxWind0 = TryGetArrayDouble(daily, "wind_speed_10m_max", 0);
            if (maxWind0.HasValue)
                _maxWind.Value = (float)maxWind0.Value;

            var precipSum0 = TryGetArrayDouble(daily, "precipitation_sum", 0);
            if (precipSum0.HasValue)
                _totalPrecipitation.Value = (float)precipSum0.Value;

            var snowSum0 = TryGetArrayDouble(daily, "snowfall_sum", 0);
            if (snowSum0.HasValue)
                _totalSnow.Value = (float)snowSum0.Value;

            var pop0 = TryGetArrayDouble(daily, "precipitation_probability_max", 0);
            if (pop0.HasValue)
            {
                _chanceOfRain.Value = (float)pop0.Value;
                _chanceOfSnow.Value = (snowSum0.HasValue && snowSum0.Value > 0) ? (float)pop0.Value : 0f;
            }

            var sunrise = TryGetArrayString(daily, "sunrise", 0);
            if (sunrise != null && DateTime.TryParse(sunrise, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sunriseDt))
                _sunrise.Value = sunriseDt.ToString("HH:mm");

            var sunset = TryGetArrayString(daily, "sunset", 0);
            if (sunset != null && DateTime.TryParse(sunset, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sunsetDt))
                _sunset.Value = sunsetDt.ToString("HH:mm");

            // --- Tomorrow (index 1) ---
            var tomorrowDateStr = TryGetArrayString(daily, "time", 1);
            if (tomorrowDateStr != null && DateTime.TryParse(tomorrowDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDt))
                _tomorrowDate.Value = tDt.ToString("yyyy-MM-dd");

            var maxC1 = TryGetArrayDouble(daily, "temperature_2m_max", 1);
            var minC1 = TryGetArrayDouble(daily, "temperature_2m_min", 1);

            if (maxC1.HasValue)
                _tomorrowMaxTemp.Value = (float)ConvertTemp(maxC1.Value);

            if (minC1.HasValue)
                _tomorrowMinTemp.Value = (float)ConvertTemp(minC1.Value);

            if (maxC1.HasValue && minC1.HasValue)
                _tomorrowAvgTemp.Value = (float)ConvertTemp((maxC1.Value + minC1.Value) / 2.0);

            var code1 = TryGetArrayInt(daily, "weather_code", 1);
            if (code1.HasValue)
                _tomorrowCondition.Value = GetConditionText(code1.Value, _language);

            var maxWind1 = TryGetArrayDouble(daily, "wind_speed_10m_max", 1);
            if (maxWind1.HasValue)
                _tomorrowMaxWind.Value = (float)maxWind1.Value;

            var precipSum1 = TryGetArrayDouble(daily, "precipitation_sum", 1);
            if (precipSum1.HasValue)
                _tomorrowPrecipitation.Value = (float)precipSum1.Value;

            var snowSum1 = TryGetArrayDouble(daily, "snowfall_sum", 1);
            var pop1 = TryGetArrayDouble(daily, "precipitation_probability_max", 1);
            if (pop1.HasValue)
            {
                _tomorrowChanceOfRain.Value = (float)pop1.Value;
                _tomorrowChanceOfSnow.Value = (snowSum1.HasValue && snowSum1.Value > 0) ? (float)pop1.Value : 0f;
            }

            // --- Extended forecast (days 3-5, indices 2-4) ---
            ApplyExtendedDay(daily, 2, _day3Date, _day3Condition, _day3AvgTemp, _day3ChanceOfRain);
            ApplyExtendedDay(daily, 3, _day4Date, _day4Condition, _day4AvgTemp, _day4ChanceOfRain);
            ApplyExtendedDay(daily, 4, _day5Date, _day5Condition, _day5AvgTemp, _day5ChanceOfRain);
        }

        private void ApplyExtendedDay(JsonElement daily, int index, PluginText dateText, PluginText conditionText, PluginSensor avgTemp, PluginSensor chanceOfRain)
        {
            var dateStr = TryGetArrayString(daily, "time", index);
            if (dateStr != null && DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                dateText.Value = dt.ToString("yyyy-MM-dd");

            var max = TryGetArrayDouble(daily, "temperature_2m_max", index);
            var min = TryGetArrayDouble(daily, "temperature_2m_min", index);
            if (max.HasValue && min.HasValue)
                avgTemp.Value = (float)ConvertTemp((max.Value + min.Value) / 2.0);

            var code = TryGetArrayInt(daily, "weather_code", index);
            if (code.HasValue)
                conditionText.Value = GetConditionText(code.Value, _language);

            var pop = TryGetArrayDouble(daily, "precipitation_probability_max", index);
            if (pop.HasValue)
                chanceOfRain.Value = (float)pop.Value;
        }

        private void ParseHourlyForecast(JsonElement hourly, DateTime nowLocal)
        {
            if (!hourly.TryGetProperty("time", out var timeArr) || timeArr.ValueKind != JsonValueKind.Array)
                return;

            int len = timeArr.GetArrayLength();

            // The hourly series starts at local midnight of the current day (Open-Meteo,
            // timezone=auto), so the first 24 entries correspond to "today" - use them to
            // compute today's forecast averages that Open-Meteo doesn't provide directly.
            int todayCount = Math.Min(24, len);
            if (todayCount > 0)
            {
                double humSum = 0; int humCount = 0;
                double visSum = 0; int visCount = 0;

                for (int i = 0; i < todayCount; i++)
                {
                    var h = TryGetArrayDouble(hourly, "relative_humidity_2m", i);
                    if (h.HasValue) { humSum += h.Value; humCount++; }

                    var v = TryGetArrayDouble(hourly, "visibility", i);
                    if (v.HasValue) { visSum += v.Value; visCount++; }
                }

                if (humCount > 0)
                    _avgHumidity.Value = (float)(humSum / humCount);

                if (visCount > 0)
                    _avgVisibility.Value = (float)((visSum / visCount) / 1000.0);
            }

            // Find the first hourly slot at/after "now" (in the location's local time).
            int startIndex = -1;
            for (int i = 0; i < len; i++)
            {
                var tStr = TryGetArrayString(hourly, "time", i);
                if (tStr != null
                    && DateTime.TryParse(tStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
                    && t >= nowLocal)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex < 0)
                return;

            var timeSlots = new[] { _hour1Time, _hour2Time, _hour3Time, _hour4Time, _hour5Time, _hour6Time };
            var tempSlots = new[] { _hour1Temp, _hour2Temp, _hour3Temp, _hour4Temp, _hour5Temp, _hour6Temp };
            var rainSlots = new[] { _hour1ChanceOfRain, _hour2ChanceOfRain, _hour3ChanceOfRain, _hour4ChanceOfRain, _hour5ChanceOfRain, _hour6ChanceOfRain };

            for (int s = 0; s < timeSlots.Length; s++)
            {
                int idx = startIndex + s;
                if (idx >= len)
                    break;

                var tStr = TryGetArrayString(hourly, "time", idx);
                if (tStr != null && DateTime.TryParse(tStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                    timeSlots[s].Value = t.ToString("HH:mm");

                var temp = TryGetArrayDouble(hourly, "temperature_2m", idx);
                if (temp.HasValue)
                    tempSlots[s].Value = (float)ConvertTemp(temp.Value);

                var pop = TryGetArrayDouble(hourly, "precipitation_probability", idx);
                if (pop.HasValue)
                    rainSlots[s].Value = (float)pop.Value;

                // Note: hourN_icon_url sensors stay unpopulated - Open-Meteo provides no
                // icon/CDN URLs (same as the current/tomorrow/day3-5 icon_url sensors),
                // and nothing else in the codebase consumes these keys.
            }
        }

        /// <summary>
        /// Open-Meteo does not provide moon-phase data, so it is computed locally from
        /// the synodic month length relative to a known new moon reference date.
        /// Moonrise/moonset are left unpopulated (would require a full astronomical
        /// rise/set calculation, out of scope here).
        /// </summary>
        private void UpdateMoonPhase()
        {
            var knownNewMoonUtc = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
            const double synodicMonthDays = 29.53058867;

            double daysSinceNewMoon = (DateTime.UtcNow - knownNewMoonUtc).TotalDays;
            double phase = daysSinceNewMoon / synodicMonthDays;
            phase -= Math.Floor(phase); // normalize to [0, 1)

            double illuminationPercent = (1.0 - Math.Cos(2.0 * Math.PI * phase)) / 2.0 * 100.0;

            _moonIllumination.Value = (float)illuminationPercent;
            _moonPhase.Value = GetMoonPhaseName(phase, _language);
        }

        private static string GetMoonPhaseName(double phase, string language)
        {
            bool pt = string.Equals(language, "pt", StringComparison.OrdinalIgnoreCase);

            if (phase < 0.03 || phase >= 0.97) return pt ? "Lua Nova" : "New Moon";
            if (phase < 0.22) return pt ? "Lua Crescente" : "Waxing Crescent";
            if (phase < 0.28) return pt ? "Quarto Crescente" : "First Quarter";
            if (phase < 0.47) return pt ? "Lua Crescente Gibosa" : "Waxing Gibbous";
            if (phase < 0.53) return pt ? "Lua Cheia" : "Full Moon";
            if (phase < 0.72) return pt ? "Lua Minguante Gibosa" : "Waning Gibbous";
            if (phase < 0.78) return pt ? "Quarto Minguante" : "Last Quarter";
            return pt ? "Lua Minguante" : "Waning Crescent";
        }

        private static double? TryGetDouble(JsonElement parent, string propertyName)
        {
            if (parent.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();
            return null;
        }

        private static int? TryGetInt(JsonElement parent, string propertyName)
        {
            if (parent.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();
            return null;
        }

        private static string? TryGetString(JsonElement parent, string propertyName)
        {
            if (parent.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private static double? TryGetArrayDouble(JsonElement parent, string propertyName, int index)
        {
            if (parent.TryGetProperty(propertyName, out var arr)
                && arr.ValueKind == JsonValueKind.Array
                && arr.GetArrayLength() > index
                && arr[index].ValueKind == JsonValueKind.Number)
            {
                return arr[index].GetDouble();
            }
            return null;
        }

        private static int? TryGetArrayInt(JsonElement parent, string propertyName, int index)
        {
            if (parent.TryGetProperty(propertyName, out var arr)
                && arr.ValueKind == JsonValueKind.Array
                && arr.GetArrayLength() > index
                && arr[index].ValueKind == JsonValueKind.Number)
            {
                return arr[index].GetInt32();
            }
            return null;
        }

        private static string? TryGetArrayString(JsonElement parent, string propertyName, int index)
        {
            if (parent.TryGetProperty(propertyName, out var arr)
                && arr.ValueKind == JsonValueKind.Array
                && arr.GetArrayLength() > index
                && arr[index].ValueKind == JsonValueKind.String)
            {
                return arr[index].GetString();
            }
            return null;
        }

        /// <summary>Maps WMO weather codes (used by Open-Meteo) to localized condition text.</summary>
        private static string GetConditionText(int code, string language)
        {
            bool pt = string.Equals(language, "pt", StringComparison.OrdinalIgnoreCase);

            if (pt)
            {
                return code switch
                {
                    0 => "Céu limpo",
                    1 => "Predominantemente limpo",
                    2 => "Parcialmente nublado",
                    3 => "Nublado",
                    45 or 48 => "Nevoeiro",
                    51 or 53 or 55 => "Garoa",
                    56 or 57 => "Garoa congelante",
                    61 => "Chuva fraca",
                    63 => "Chuva moderada",
                    65 => "Chuva forte",
                    66 or 67 => "Chuva congelante",
                    71 or 73 or 75 => "Neve",
                    77 => "Grãos de neve",
                    80 or 81 or 82 => "Pancadas de chuva",
                    85 or 86 => "Pancadas de neve",
                    95 => "Trovoada",
                    96 or 99 => "Trovoada com granizo",
                    _ => $"Código {code}"
                };
            }

            return code switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 or 48 => "Fog",
                51 or 53 or 55 => "Drizzle",
                56 or 57 => "Freezing drizzle",
                61 => "Light rain",
                63 => "Moderate rain",
                65 => "Heavy rain",
                66 or 67 => "Freezing rain",
                71 or 73 or 75 => "Snow",
                77 => "Snow grains",
                80 or 81 or 82 => "Rain showers",
                85 or 86 => "Snow showers",
                95 => "Thunderstorm",
                96 or 99 => "Thunderstorm with hail",
                _ => $"Code {code}"
            };
        }

        private static string DegreesToCompass(double degrees)
        {
            string[] directions = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            int index = (int)Math.Round(degrees / 22.5, MidpointRounding.AwayFromZero) % 16;
            if (index < 0) index += 16;
            return directions[index];
        }

        private void LoadConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(ConfigFilePath) || !File.Exists(ConfigFilePath))
                {
                    CreateDefaultConfig();
                    return;
                }

                var lines = File.ReadAllLines(ConfigFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                        continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key.ToLower())
                    {
                        case "apikey":
                            // No longer required by Open-Meteo; parsed for backward compatibility only.
                            _apiKey = value;
                            break;
                        case "location":
                            _location = value;
                            break;
                        case "usefahrenheit":
                            _useFahrenheit = value.ToLower() == "true" || value == "1";
                            break;
                        case "language":
                            _language = string.IsNullOrWhiteSpace(value) ? "pt" : value.Trim().ToLowerInvariant();
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(_location))
                    _location = "auto";

                if (string.IsNullOrWhiteSpace(_language))
                    _language = "pt";
            }
            catch (Exception ex)
            {
                _lastError = $"Config load error: {ex.Message}";
            }
        }

        private void CreateDefaultConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(ConfigFilePath))
                    return;

                var defaultConfig = @"# SynQPanel Weather Plugin Configuration
# Powered by Open-Meteo (https://open-meteo.com) - free, no API key required.

# Location: a city name, or 'auto'/'auto:ip' to geolocate via IP address.
# Examples: London, Sao Paulo, auto, auto:ip
Location=auto

# Temperature Unit (true for Fahrenheit, false for Celsius)
UseFahrenheit=false

# Language for condition text, moon phase names, and geocoding results.
# Supported: pt (Portuguese, default), en (English)
Language=pt
";

                File.WriteAllText(ConfigFilePath, defaultConfig);
                _location = "auto";
                _language = "pt";
            }
            catch (Exception ex)
            {
                _lastError = $"Config creation error: {ex.Message}";
            }
        }

        public override void Close()
        {
            // Cleanup if needed
        }

    }
}
