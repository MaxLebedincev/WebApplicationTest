using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace WebApplicationTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using Activity? activity = Activity.Current?.Source.StartActivity("GetWeatherForecast");

            activity?.AddTag("Weather", 265);

            Console.WriteLine($"{activity.TraceId} : {activity.SpanId}");

            using (var activity2 = activity.Source.StartActivity("GetWeatherForecast"))
            {
                Thread.Sleep(2000);
                Console.WriteLine($"{activity2.TraceId} : {activity2.SpanId}");
            }


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                //Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost]
        public async Task<IActionResult> SendToTheOtherApi([FromBody] WeatherForecast weatherForecast)
        {
            _logger.LogInformation("Traceparent: {0}", Activity.Current.Id);
            _logger.LogInformation("Tracestate: {0}", Activity.Current.TraceStateString);
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(weatherForecast), Encoding.UTF8, "application/json");
            await client.PostAsync(_configuration["ClientUrl"], content);

            return Ok();
        }

        [HttpPost]
        [Route("PublishInQueue")]
        public IActionResult PublishInQueue([FromBody] WeatherForecast weatherForecast)
        {
            var message = JsonConvert.SerializeObject(weatherForecast);
            var body = Encoding.UTF8.GetBytes(message);
            var traceparent = Activity.Current.Id;
            var tracestate = Activity.Current.TraceStateString;
            _logger.LogInformation("Traceparent: {0}", traceparent);
            _logger.LogInformation("Tracestate: {0}", tracestate);

            // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
            var contextToInject = Activity.Current.Context;
            _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), Request.Headers, (headers, key, value) => headers.Append(key, value));

            return Ok();
        }
    }
}
