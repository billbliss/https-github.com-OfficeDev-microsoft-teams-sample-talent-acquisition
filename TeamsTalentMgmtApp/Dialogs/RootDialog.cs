﻿using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams; //Teams bot extension SDK
using Microsoft.Bot.Connector.Teams.Models;
using TeamsTalentMgmtApp.Utils;
using System.Linq;
using System.Collections.Generic;
using TeamsTalentMgmtApp.DataModel;
using Newtonsoft.Json.Linq;
using AdaptiveCards;
using System.Configuration;
using System.Net.Http;

namespace TeamsTalentMgmtApp.Dialogs
{
    /// <summary>
    /// Basic dialog implemention showing how to create an interactive chat bot.
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        /// <summary>
        /// Managing connection
        /// </summary>
        private static string ConnectionName = ConfigurationManager.AppSettings["ConnectionName"];

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        /// <summary>
        /// This is where you can process the incoming user message and decide what to do.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // Strip out all mentions.  As all channel messages to a bot must @mention the bot itself, you must strip out the bot name at minimum.
            // This uses the extension SDK function GetTextWithoutMentions() to strip out ALL mentions
            var text = activity.GetTextWithoutMentions();

            // Supports 5 commands:  Help, Welcome (sent from HandleSystemMessage when bot is added), top candidates, schedule interview, and open positions.
            // This simple text parsing assumes the command is the first two tokens, and an optional parameter is the second.
            var split = text.Split(' ');

            // The user is asking for onen of the supported commands.
            if (split.Length >= 2)
            {
                var cmd = split[0].ToLower();
                var keywords = split.Skip(2).ToArray();

                // Parse the command and go do the right thing
                if (cmd.Contains("top") && keywords.Length > 0)
                {
                    await SendTopCandidatesMessage(context, keywords[0]);
                }
                else if (cmd.Contains("schedule"))
                {
                    // Supports either structured query or via user input.
                    JObject ctx = activity.Value as JObject;

                    // Check if this is a button press or a text command.
                    if (ctx != null)
                    {
                        Candidate c = ctx.ToObject<Candidate>();
                        await SendScheduleInterviewMessage(context, c, c.ReqId);
                    }
                    else if (keywords.Length == 3)
                    {
                        string name = string.Join(" ", keywords.Take(2).ToArray());
                        string reqId = keywords[2];

                        // Takes 3 parameters: first name, last name, and then req ID
                        await SendScheduleInterviewMessage(context, name, reqId);
                    }
                    else
                    {
                        await SendHelpMessage(context, "I'm sorry, I did not understand you :(");
                    }
                }
                else if (cmd.Contains("open"))
                {
                    await SendOpenPositionsMessage(context);
                }
                else if (cmd.Contains("candidate"))
                {
                    // Supports either structured query or via user input.
                    JObject ctx = activity.Value as JObject;
                    Candidate c = null;

                    if (ctx != null)
                    {
                        c = ctx.ToObject<Candidate>();
                        await SendCandidateDetailsMessage(context, c);
                    }
                    else if (keywords.Length > 0)
                    {
                        string name = string.Join(" ", keywords);
                        c = new CandidatesDataController().GetCandidateByName(name);
                        await SendCandidateDetailsMessage(context, c);
                    }
                }
                else if (cmd.Contains("assign"))
                {
                    string guid = split[1];
                    await UpdateMessage(context, guid);
                }
                else
                {
                    await SendHelpMessage(context, "I'm sorry, I did not understand you :(");
                }
            }
            else
            {
                // Respond with standard help message.
                if (text.Contains("help"))
                {
                    await SendHelpMessage(context, "Sure, I can provide help info about me.");
                }
                else if (text.Contains("login"))
                {
                    //await SendOAuthCardAsync(context, activity);
                }
                else if (text.Contains("welcome") || text.Contains("hello") || text.Contains("hi"))
                {
                    await SendHelpMessage(context, "## Welcome to the Contoso Talent Management app");
                }
                else
                // Don't know what to say so this is the generic handling here.
                {
                    await SendHelpMessage(context, "I'm sorry, I did not understand you :(");
                }
            }

            context.Wait(MessageReceivedAsync);
        }

        #region MessagingHelpers

        /**
        private async Task SendOAuthCardAsync(IDialogContext context, Activity activity)
        {
            await context.PostAsync($"To do this, you'll first need to sign in.");

            var reply = activity.CreateReply();

            var client = GetOAuthClient(activity, new MicrosoftAppCredentials(null, null));
            var link = await client.OAuthApi.GetSignInLinkAsync(activity, ConnectionName);
            reply.Attachments = new List<Attachment>() {
                    new Attachment()
                    {
                        ContentType = SigninCard.ContentType,
                        Content = new SigninCard()
                        {
                            Text = "You need to sign in to do this",
                            Buttons = new CardAction[]
                            {
                                new CardAction() { Title = "Sign in", Value = link, Type = ActionTypes.Signin }
                            },
                        }
                    }
                };

            await context.PostAsync(reply);

            //context.Wait(WaitForToken);
        }
        */

        /// <summary>
        /// Helper method to send a simple help message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="firstLine"></param>
        /// <returns></returns>
        private async Task SendHelpMessage(IDialogContext context, string firstLine)
        {
            var helpMessage = $"{firstLine} \n\n Here's what I can help you do: \n\n"
                + "* Show details about a candidate, for example: candidate details John Smith 0F812D01 \n"
                + "* Show top recent candidates for a Req ID, for example: top candidates 0F812D01 \n"
                + "* Schedule interview for name and Req ID, for example: schedule interview John Smith 0F812D01 \n"
                + "* Create a new job posting \n"
                + "* List all your open positions";

            await context.PostAsync(helpMessage);
        }

        private async Task SendScheduleInterviewMessage(IDialogContext context, Candidate c, string reqId)
        {
            InterviewRequest request = new InterviewRequest
            {
                Candidate = c,
                Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day),
                PositionTitle = new OpenPositionsDataController().GetPositionForReqId(reqId).Title,
                Remote = false,
                ReqId = reqId
            };

            await SendScheduleInterviewMessage(context, request);
        }

        private async Task SendScheduleInterviewMessage(IDialogContext context, string name, string reqId)
        {
            InterviewRequest request = new InterviewRequest
            {
                Candidate = new CandidatesDataController().GetCandidateByName(name),
                Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day),
                PositionTitle = new OpenPositionsDataController().GetPositionForReqId(reqId).Title,
                Remote = false,
                ReqId = reqId
            };

            await SendScheduleInterviewMessage(context, request);
        }

        private async Task SendCandidateDetailsMessage(IDialogContext context, Candidate c)
        {
            IMessageActivity reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();

            AdaptiveCard card = CardHelper.CreateAdaptiveCardForCandidateInfo(c);
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            reply.Attachments.Add(attachment);

            ConnectorClient client = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            ResourceResponse resp = await client.Conversations.ReplyToActivityAsync((Activity)reply);
        }

        // Send a message with a list of found tasks.
        private async Task SendScheduleInterviewMessage(IDialogContext context, InterviewRequest request)
        {
            IMessageActivity reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();
            reply.Text = $"Here's your request to schedule an interview:";

            O365ConnectorCard card = CardHelper.CreateCardForInterviewRequest(request);
            reply.Attachments.Add(card.ToAttachment());

            ConnectorClient client = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            ResourceResponse resp = await client.Conversations.ReplyToActivityAsync((Activity)reply);

            // Cache the response activity ID and previous interview card so that we can refresh it later.
            //string activityId = resp.Id.ToString();
            //context.ConversationData.SetValue(request.ReqId, new Tuple<string, O365ConnectorCard>(activityId, card));
        }

        /// <summary>
        /// Helper method to create a simple task card and send it back as a message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reqId"></param>
        /// <returns></returns>
        private async Task SendTopCandidatesMessage(IDialogContext context, string reqId)
        {
            // Create a message object.
            IMessageActivity reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();
            reply.Text = $"Okay, here are top candidates who have recently applied to your position";

            // Create the task using the data controller.
            var candidates = new CandidatesDataController().GetTopCandidates(reqId);
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            foreach (Candidate c in candidates)
            {
                var card = CardHelper.CreateCardForCandidate(c);
                reply.Attachments.Add(card.ToAttachment());
            }

            // Send the message back to the user.
            ConnectorClient client = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            ResourceResponse resp = await client.Conversations.ReplyToActivityAsync((Activity)reply);
        }

        private async Task SendOpenPositionsMessage(IDialogContext context)
        {
            var openPositions = new OpenPositionsDataController().ListOpenPositions(5);

            IMessageActivity reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>();
            reply.Text = $"Hi {context.Activity.From.Name}! You have {openPositions.Count} active postings right now:";

            foreach (OpenPosition position in openPositions)
            {
                ThumbnailCard card = CardHelper.CreateCardForPosition(position);
                reply.Attachments.Add(card.ToAttachment());
            }

            ThumbnailCard buttonsCard = new ThumbnailCard();

            buttonsCard.Buttons = new List<CardAction>()
            {
                new CardAction("openUrl", "View details", null, "https://www.microsoft.com"),
                new CardAction("messageBack", "Add new job posting", null, null, $"new job posting", "New job posting")
            };
            reply.Attachments.Add(buttonsCard.ToAttachment());

            ConnectorClient client = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
            ResourceResponse resp = await client.Conversations.ReplyToActivityAsync((Activity)reply);
        }

        /// <summary>
        /// Helper method to update an existing message for the given task item GUID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="taskItemGuid"></param>
        /// <returns></returns>
        private async Task UpdateMessage(IDialogContext context, string taskItemGuid)
        {
            Tuple<string, ThumbnailCard> cachedMessage;

            //Retrieve passed task guid from the ConversationData cache
            if (context.ConversationData.TryGetValue("task " + taskItemGuid, out cachedMessage))
            {
                IMessageActivity reply = context.MakeMessage();
                reply.Attachments = new List<Attachment>();

                string activityId = cachedMessage.Item1;
                ThumbnailCard card = cachedMessage.Item2;

                card.Subtitle = $"Assigned to: {context.Activity.From.Name}";

                card.Buttons = new List<CardAction>()
                {
                    new CardAction("openUrl", "View task", null, "https://www.microsoft.com"),
                    new CardAction("openUrl", "Update details", null, "https://www.microsoft.com")
                };

                reply.Attachments.Add(card.ToAttachment());

                ConnectorClient client = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
                ResourceResponse resp = await client.Conversations.UpdateActivityAsync(context.Activity.Conversation.Id, activityId, (Activity)reply);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Could not update task {taskItemGuid}");
            }
        }

        #endregion
    }
}