using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace dotnetsheff.ZoomHooks
{
    public class ActiveMeetingAttendees
    {
        public class Handler
        {
            private readonly IMongoCollection<BsonDocument> _events;

            public Handler(IMongoDatabase database)
            {
                _events = database.GetCollection<BsonDocument>("events");
            }

            public async Task<string[]> Handle(string accountId, string meetingId)
            {
                var filter = Builders<BsonDocument>.Filter.In(x => x["event"],
                        new[]
                        {
                            new BsonString("meeting.participant_joined"),
                            new BsonString("meeting.participant_left")
                        }) & Builders<BsonDocument>.Filter.Eq(x => x["payload"]["account_id"], accountId)
                           & Builders<BsonDocument>.Filter.Eq(x => x["payload"]["object"]["id"], meetingId);

                var result = await _events.Aggregate()
                    .Match(filter)
                    .Group(@"{ _id: ""$payload.object.participant.user_id"", count: { $sum: 1 }, user_name: { $first : ""$payload.object.participant.user_name"" } }")
                    .Match(x => x["count"] == 1)
                    .Project(x => new { UserName = x["user_name"] })
                    .ToListAsync();

                return result.Select(x => x.UserName.AsString).ToArray();
            }
        }
    }
}
