using System;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace dotnetsheff.ZoomHooks.Tests
{
    public class ActiveMeetingAttendeesTests : IAsyncLifetime
    {
        private readonly IMongoDatabase _mongoDatabase;

        public ActiveMeetingAttendeesTests()
        {
            var name = Guid.NewGuid().ToString();
            var mongoClient = new MongoClient();
            _mongoDatabase = mongoClient.GetDatabase(name);
        }

        [Fact]
        public async Task ShouldReturnActiveAttendeeForCorrectAccountIdAndMeetingId()
        {
            var accountId = "accountId-1";
            var meetingid = "meetingId-1";
            await _mongoDatabase.GetCollection<BsonDocument>("events")
                .InsertManyAsync(new[]
                {
                    CreateMeetingParticipantJoinedEventNotification(accountId, meetingid, "userId-1", "username1"),
                    CreateMeetingParticipantJoinedEventNotification("accountId-1", "meetingId-2", "userId-2", "username2"),
                    CreateMeetingParticipantJoinedEventNotification("accountId-2", "meetingId-1", "userId-3", "username2")
                });

            var handler = new ActiveMeetingAttendees.Handler(_mongoDatabase);

            var result = await handler.Handle(accountId, meetingid);

            result.Should().BeEquivalentTo("username1");
        }

        [Fact]
        public async Task ShouldReturnActiveAttendee()
        {
            var accountId = "accountId-1";
            var meetingid = "meetingId-1";
            await _mongoDatabase.GetCollection<BsonDocument>("events")
                .InsertManyAsync(new[]
                {
                    CreateMeetingParticipantJoinedEventNotification(accountId, meetingid, "userId-1", "username1"),
                    CreateMeetingParticipantJoinedEventNotification(accountId, meetingid, "userId-2", "username2"),

                    CreateMeetingParticipantLeftEventNotification(accountId, meetingid, "userId-2", "username2"),

                });

            var handler = new ActiveMeetingAttendees.Handler(_mongoDatabase);

            var result = await handler.Handle(accountId, meetingid);

            result.Should().BeEquivalentTo("username1");
        }

        private BsonDocument CreateMeetingParticipantLeftEventNotification(string accountId, string meetingid, string userid, string username)
        {
            return BsonDocument.Parse($@"{{
  ""event"": ""meeting.participant_left"",
  ""payload"": {{
    ""account_id"": ""{accountId}"",
    ""object"": {{
      ""uuid"": ""{Guid.NewGuid()}"",
      ""id"": ""{meetingid}"",
      ""host_id"": ""{Guid.NewGuid()}"",
      ""topic"": ""My Meeting"",
      ""type"": 2,
      ""start_time"": ISODate(""2019-07-09T17:00:00Z""),
      ""duration"": 60,
      ""timezone"": ""America/Los_Angeles"",
      ""participant"": {{
        ""user_id"": ""{userid}"",
        ""user_name"": ""{username}"",
        ""id"": ""{Guid.NewGuid()}"",
        ""leave_time"": ISODate(""2019-07-16T17:13:13Z"")
      }}
    }}
  }}
}}");
        }

        private BsonDocument CreateMeetingParticipantJoinedEventNotification(string accountId, string meetingid, string userid, string userName)
        {
            return BsonDocument.Parse($@"{{
  ""event"": ""meeting.participant_joined"",
  ""payload"": {{
    ""account_id"": ""{accountId}"",
    ""object"": {{
      ""uuid"": ""{Guid.NewGuid()}"",
      ""id"": ""{meetingid}"",
      ""host_id"": ""{Guid.NewGuid()}"",
      ""topic"": ""My Meeting"",
      ""type"": 2,
      ""start_time"": ISODate(""2019-07-09T17:00:00Z""),
      ""duration"": 60,
      ""timezone"": ""America/Los_Angeles"",
      ""participant"": {{
        ""user_id"": ""{userid}"",
        ""user_name"": ""{userName}"",
        ""id"": ""{Guid.NewGuid()}"",
        ""join_time"": ISODate(""2019-07-16T17:13:13Z"")
      }}
    }}
  }}
}}");
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _mongoDatabase.Client.DropDatabaseAsync(_mongoDatabase.DatabaseNamespace.DatabaseName);
        }
    }
}
