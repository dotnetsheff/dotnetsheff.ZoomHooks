using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace dotnetsheff.ZoomHooks
{
    public class Functions
    {
        private readonly ActiveMeetingAttendees.Handler _handler;
        private readonly IMongoCollection<BsonDocument> _events;

        public Functions(IMongoDatabase database, ActiveMeetingAttendees.Handler handler)
        {
            _handler = handler;
            _events = database.GetCollection<BsonDocument>("events");
        }

        [FunctionName("HandleZoomEventFunction")]
        public async Task<IActionResult> RunHandleZoomEventFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")] HttpRequest req,
            ILogger log)
        {
            var body = await req.ReadAsStringAsync();
            var bsonDocument = BsonDocument.Parse(body);
            var obj = bsonDocument["payload"]["object"].AsBsonDocument;
            
            if (obj.TryGetValue("start_time", out var startTime))
            {
                obj["start_time"] = DateTime.Parse(startTime.AsString, null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
            }

            var participant = obj["participant"].AsBsonDocument;
            if (participant.TryGetValue("leave_time", out var leaveTime))
            {
                participant["leave_time"] = DateTime.Parse(leaveTime.AsString, null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
            }
            if (participant.TryGetValue("join_time", out var joinTime))
            {
                participant["join_time"] = DateTime.Parse(joinTime.AsString, null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
            }
            await _events.InsertOneAsync(bsonDocument);

            return new OkResult();
        }


        [FunctionName("QueryMeetingAttendeesActiveFunction")]
        public async Task<IActionResult> RunQueryMeetingAttendeesActiveFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/{accountId}/meetings/{meetingId}/attendees/active")] HttpRequest req,
            string accountId,
            string meetingId,
            ILogger log)
        {
            var strings = await _handler.Handle(accountId, meetingId);

            var contentResult = new ContentResult
            {
                Content = string.Join(Environment.NewLine, strings),
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };

            return contentResult;

        }


    }
}
