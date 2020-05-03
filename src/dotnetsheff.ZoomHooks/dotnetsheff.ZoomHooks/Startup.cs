using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

[assembly: FunctionsStartup(typeof(dotnetsheff.ZoomHooks.Startup))]

namespace dotnetsheff.ZoomHooks
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(s => {
                var connectionString = Environment.GetEnvironmentVariable("ZOOMHOOKS_MONGODB_CONNECTIONSTRING");
                var client = new MongoClient(connectionString);

                return client.GetDatabase("zoom-hooks");
            });

            builder.Services.AddSingleton<ActiveMeetingAttendees.Handler>();
        }
    }
}
