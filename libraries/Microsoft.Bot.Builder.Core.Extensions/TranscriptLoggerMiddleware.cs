﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Core.Extensions
{
    /// <summary>
    /// When added, this middleware will log incoming and outgoing activitites to a ITranscriptStore 
    /// </summary>
    public class TranscriptLoggerMiddleware : IMiddleware
    {
        private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        private ITranscriptLogger logger;

        private Queue<IActivity> transcript = new Queue<IActivity>();

        /// <summary>
        /// Middleware for logging incoming and outgoing activities to a transcript store 
        /// </summary>
        /// <param name="transcriptLogger"></param>
        public TranscriptLoggerMiddleware(ITranscriptLogger transcriptLogger)
        {
            logger = transcriptLogger ?? throw new ArgumentNullException("TranscriptLoggerMiddleware requires a ITranscriptLogger implementation.  ");
        }

        /// <summary>
        /// initialization for middleware turn
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nextTurn"></param>
        /// <returns></returns>
        public async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate nextTurn)
        {
            // log incoming activity at beginning of turn
            if (context.Activity != null)
            {
                if (String.IsNullOrEmpty((String)context.Activity.From.Properties["role"]))
                    context.Activity.From.Properties["role"] = "user";

                LogActivity(CloneActivity(context.Activity));
            }

            // hook up onSend pipeline
            context.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                // run full pipeline
                var responses = await nextSend();

                foreach (var activity in activities)
                {
                    LogActivity(CloneActivity(activity));
                }
                return responses;
            });

            // hook up update activity pipeline
            context.OnUpdateActivity(async (ctx, activity, nextUpdate) =>
            {
                // run full pipeline
                var response = await nextUpdate();

                // add Message Update activity
                var updateActivity = CloneActivity(activity);
                updateActivity.Type = ActivityTypes.MessageUpdate;
                LogActivity(updateActivity);
                return response;
            });

            // hook up delete activity pipeline
            context.OnDeleteActivity(async (ctx, reference, nextDelete) =>
            {
                // run full pipeline
                await nextDelete();

                // add MessageDelete activity
                var deleteActivity = new Activity() { Type = ActivityTypes.MessageDelete }.AsMessageDeleteActivity();
                deleteActivity.Id = reference.ActivityId;
                deleteActivity.From = reference.Bot;
                deleteActivity.Conversation = reference.Conversation;
                deleteActivity.Recipient = reference.User;
                deleteActivity.ChannelId = reference.ChannelId;
                deleteActivity.Timestamp = DateTimeOffset.UtcNow;

                LogActivity(deleteActivity);
            });

            // process bot logic
            await nextTurn().ConfigureAwait(false);

            // flush transcript at end of turn
            while (transcript.Count > 0)
            {
                try
                {
                    var activity = transcript.Dequeue();
                    await logger.LogActivity(activity);
                }
                catch (Exception err)
                {
                    System.Diagnostics.Trace.TraceError($"Transcript logActivity failed with {err}");
                }
            }
        }

        private void LogActivity(IActivity activity)
        {
            lock (transcript)
            {
                if (activity.Timestamp == null)
                    activity.Timestamp = DateTime.UtcNow;

                transcript.Enqueue(activity);
            }
        }

        private static IActivity CloneActivity(IActivity activity)
        {
            activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity, JsonSettings));
            return activity;
        }
    }
}
