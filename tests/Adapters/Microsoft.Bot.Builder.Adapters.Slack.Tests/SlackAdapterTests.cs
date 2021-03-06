﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Schema;
using Moq;
using Newtonsoft.Json;
using SlackAPI;
using Xunit;
using File = System.IO.File;

namespace Microsoft.Bot.Builder.Adapters.Slack.Tests
{
    public class SlackAdapterTests
    {
        private readonly SlackAdapterOptions _testOptions = new SlackAdapterOptions("VerificationToken", "ClientSigningSecret", "BotToken");

        [Fact]
        public void ConstructorShouldFailWithNullClient()
        {
            Assert.Throws<ArgumentNullException>(() => new SlackAdapter((SlackClientWrapper)null));
        }

        [Fact]
        public void ConstructorSucceeds()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            Assert.NotNull(new SlackAdapter(slackApi.Object));
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullActivityTimestamp()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity
            {
                Timestamp = null,
            };

            var turnContext = new TurnContext(slackAdapter, activity);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await slackAdapter.UpdateActivityAsync(turnContext, activity, default);
            });
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullContext()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await slackAdapter.UpdateActivityAsync(null, activity, default);
            });
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullActivity()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            using (var turnContext = new TurnContext(slackAdapter, new Activity()))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await slackAdapter.UpdateActivityAsync(turnContext, null, default);
                });
            }
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullActivityConversation()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity
            {
                Id = "testId",
                Conversation = null,
            };

            using (var turnContext = new TurnContext(slackAdapter, activity))
            {
                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await slackAdapter.UpdateActivityAsync(turnContext, activity, default);
                });
            }
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldSucceed()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new UpdateResponse { ok = true }));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Mock<Activity>().SetupAllProperties();
            activity.Object.Id = "MockActivityId";
            activity.Object.Conversation = new ConversationAccount
            {
                Id = "MockConversationId",
            };
            activity.Object.Text = "Hello, Bot!";

            ResourceResponse response;

            using (var turnContext = new TurnContext(slackAdapter, activity.Object))
            {
                response = await slackAdapter.UpdateActivityAsync(turnContext, activity.Object, default);
            }

            Assert.Equal(activity.Object.Id, response.Id);
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithResponseError()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new UpdateResponse { ok = false }));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Mock<Activity>().SetupAllProperties();
            activity.Object.Id = "MockActivityId";
            activity.Object.Conversation = new ConversationAccount
            {
                Id = "MockConversationId",
            };
            activity.Object.Text = "Hello, Bot!";

            using (var turnContext = new TurnContext(slackAdapter, activity.Object))
            {
                await Assert.ThrowsAsync<Exception>(async () =>
                {
                    await slackAdapter.UpdateActivityAsync(turnContext, activity.Object, default);
                });
            }
        }

        [Fact]
        public async Task DeleteActivityAsyncShouldFailWithNullReference()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            using (var context = new TurnContext(slackAdapter, new Activity()))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                {
                    await slackAdapter.DeleteActivityAsync(context, null, default);
                });
            }
        }

        [Fact]
        public async Task DeleteActivityAsyncShouldFailWithNullTurnContext()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var reference = new ConversationReference();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await slackAdapter.DeleteActivityAsync(null, reference, default);
            });
        }

        [Fact]
        public async Task DeleteActivityAsyncShouldFailWithNullChannelId()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            using (var context = new TurnContext(slackAdapter, new Activity()))
            {
                var reference = new ConversationReference
                {
                    ChannelId = null,
                };

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await slackAdapter.DeleteActivityAsync(context, reference, default);
                });
            }
        }

        [Fact]
        public async Task DeleteActivityAsyncShouldFailWithNullTimestamp()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            using (var context = new TurnContext(slackAdapter, new Activity()))
            {
                var reference = new ConversationReference
                {
                    ChannelId = "testChannelId",
                };

                await Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await slackAdapter.DeleteActivityAsync(context, reference, default);
                });
            }
        }

        [Fact]
        public async Task DeleteActivityAsyncShouldSucceed()
        {
            var deletedMessages = 0;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Callback(() => { deletedMessages++; });

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Mock<Activity>();
            activity.Object.Timestamp = new DateTimeOffset();

            using (var context = new TurnContext(slackAdapter, activity.Object))
            {
                var reference = new ConversationReference
                {
                    ChannelId = "channelId",
                };

                await slackAdapter.DeleteActivityAsync(context, reference, default);
            }

            Assert.Equal(1, deletedMessages);
        }

        [Fact]
        public async Task SendActivitiesAsyncShouldThrowExceptionWithNullContext()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity
            {
                Id = "testId",
                Type = ActivityTypes.Message,
                Text = "text",
                Conversation = new ConversationAccount()
                {
                    Id = "testConversationId",
                },
            };

            Activity[] activities = { activity };

            await Assert.ThrowsAsync<ArgumentNullException>(async () => { await slackAdapter.SendActivitiesAsync(null, activities, default); });
        }

        [Fact]
        public async Task SendActivitiesAsyncShouldThrowExceptionWithNullActivity()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity
            {
                Id = "testId",
                Type = ActivityTypes.Message,
                Text = "text",
                Conversation = new ConversationAccount()
                {
                    Id = "testConversationId",
                },
            };

            using (var turnContext = new TurnContext(slackAdapter, activity))
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () => { await slackAdapter.SendActivitiesAsync(turnContext, null, default); });
            }
        }

        [Fact]
        public async Task SendActivitiesAsyncShouldSucceed()
        {
            var slackResponse = new SlackResponse
            {
                Ok = true,
                Ts = "mockedTS",
            };

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.PostMessageAsync(It.IsAny<NewSlackMessage>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(slackResponse));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                Text = "text",
                Conversation = new ConversationAccount()
                {
                    Id = "testConversationId",
                },
            };

            Activity[] activities = { activity };

            var turnContext = new TurnContext(slackAdapter, activity);

            var responses = await slackAdapter.SendActivitiesAsync(turnContext, activities, default);

            Assert.Equal(slackResponse.Ts, responses[0].Id);
        }

        [Fact]
        public async Task ContinueConversationAsyncShouldFailWithNullReference()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            Task BotsLogic(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            await Assert.ThrowsAsync<ArgumentNullException>(async () => { await slackAdapter.ContinueConversationAsync(null, BotsLogic, default); });
        }

        [Fact]
        public async Task ContinueConversationAsyncShouldFailWithNullBot()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => { await slackAdapter.ContinueConversationAsync(new ConversationReference(), null, default); });
        }

        [Fact]
        public async Task ContinueConversationAsyncShouldSucceed()
        {
            var callbackInvoked = false;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            Task BotsLogic(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            }

            await slackAdapter.ContinueConversationAsync(new ConversationReference(), BotsLogic, default);

            Assert.True(callbackInvoked);
        }

        [Fact]
        public async void ProcessAsyncShouldFailWithNullHttpRequest()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var httpResponse = new Mock<HttpResponse>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await slackAdapter.ProcessAsync(null, httpResponse.Object, new Mock<IBot>().Object, default);
            });
        }

        [Fact]
        public async void ProcessAsyncShouldFailWithNullHttpResponse()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var httpRequest = new Mock<HttpRequest>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await slackAdapter.ProcessAsync(httpRequest.Object, null, new Mock<IBot>().Object, default);
            });
        }

        [Fact]
        public async void ProcessAsyncShouldFailWithNullBot()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var httpRequest = new Mock<HttpRequest>();
            var httpResponse = new Mock<HttpResponse>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, null, default);
            });
        }

        [Fact]
        public async Task ProcessAsyncShouldSucceedOnUrlVerification()
        {
            string actual = null;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/URLVerificationBody.json");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var deserializedPayload = JsonConvert.DeserializeObject<SlackRequestBody>(payload);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            var mockStream = new Mock<Stream>();
            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            httpResponse.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback((byte[] data, int offset, int length, CancellationToken token) =>
                {
                    if (length > 0)
                    {
                        actual = Encoding.UTF8.GetString(data);
                    }
                });

            await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, new Mock<IBot>().Object, default);

            Assert.Equal(actual, deserializedPayload.Challenge);
        }

        [Fact]
        public async Task ProcessAsyncShouldFailWithSignatureMismatch()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(false);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/MessageBody.json");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();

            var mockStream = new Mock<Stream>();
            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            httpResponse.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] data, int offset, int length, CancellationToken token) =>
            {
                if (length > 0)
                {
                    var actual = Encoding.UTF8.GetString(data);
                }
            });

            await Assert.ThrowsAsync<Exception>(async () => { await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, new Mock<IBot>().Object, default); });
        }

        [Fact]
        public async Task ProcessAsyncShouldFailOnVerificationTokenMismatch()
        {
            _testOptions.SlackVerificationToken = "testToken";

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/MessageBody.json");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();

            var mockStream = new Mock<Stream>();
            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            await Assert.ThrowsAsync<Exception>(async () => { await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, new Mock<IBot>().Object, default); });
        }

        [Fact]
        public async Task ProcessAsyncShouldFailWithUnknownEventType()
        {
            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/UnknownEvent.json");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();

            await Assert.ThrowsAsync<Exception>(async () => { await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, new Mock<IBot>().Object, default); });
        }

        [Fact]
        public async Task ProcessAsyncShouldSucceedOnEventCallback()
        {
            var callback = false;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/MessageBody.json");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();

            var mockStream = new Mock<Stream>();
            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            httpResponse.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] data, int offset, int length, CancellationToken token) =>
            {
                if (length > 0)
                {
                    var actual = Encoding.UTF8.GetString(data);
                }
            });

            var bot = new Mock<IBot>();
            bot.Setup(x => x.OnTurnAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>())).Callback(() =>
            {
                callback = true;
            });

            await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, bot.Object, default);

            Assert.True(callback);
        }

        [Fact]
        public async Task ProcessAsyncShouldSucceedOnSlashCommand()
        {
            var callback = false;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/SlashCommandBody.txt");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            var mockStream = new Mock<Stream>();
            httpResponse.SetupAllProperties();

            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            httpResponse.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] data, int offset, int length, CancellationToken token) =>
            {
                if (length > 0)
                {
                    var actual = Encoding.UTF8.GetString(data);
                }
            });

            var bot = new Mock<IBot>();
            bot.Setup(x => x.OnTurnAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>())).Callback(() =>
            {
                callback = true;
            });

            await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, bot.Object, default);

            Assert.True(callback);
        }

        [Fact]
        public async Task ProcessAsyncShouldSucceedOnInteractiveMessage()
        {
            var callback = false;

            var slackApi = new Mock<SlackClientWrapper>(_testOptions);
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.VerifySignature(It.IsAny<HttpRequest>(), It.IsAny<string>())).Returns(true);

            var slackAdapter = new SlackAdapter(slackApi.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"/Files/InteractiveMessageBody.txt");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload.ToString()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(req => req.Body).Returns(stream);

            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupAllProperties();

            var mockStream = new Mock<Stream>();
            httpResponse.SetupGet(req => req.Body).Returns(mockStream.Object);

            httpResponse.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] data, int offset, int length, CancellationToken token) =>
            {
                if (length > 0)
                {
                    var actual = Encoding.UTF8.GetString(data);
                }
            });

            var bot = new Mock<IBot>();
            bot.Setup(x => x.OnTurnAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>())).Callback(() =>
            {
                callback = true;
            });

            await slackAdapter.ProcessAsync(httpRequest.Object, httpResponse.Object, bot.Object, default);

            Assert.True(callback);
        }
    }
}
