using Bogus;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using TeamsTalentMgmtApp.Utils;
using TeamsTalentMgmtApp.DataModel;
using System.Globalization;
using Newtonsoft.Json.Linq;
using AdaptiveCards;

namespace TeamsTalentMgmtApp
{
    /// <summary>
    /// Simple class that processes an activity and responds with with set of messaging extension results.
    /// </summary>
    public class MessagingExtension
    {
        private Activity activity;

        /// <summary>
        /// Used to generate image index.
        /// </summary>
        private Random random;

        private const string TaskModuleCommandType = "startTask";

        private const string CreatePostingCommand = "CreatePostingExtended";

        public MessagingExtension(Activity activity)
        {
            this.activity = activity;
            random = new Random();
        }

        /// <summary>
        /// Helper method to generate a the messaging extension response.
        /// 
        /// Note that for this sample, we are returning generated positions for illustration purposes only.
        /// </summary>
        /// <returns></returns>
        public ComposeExtensionResponse CreateResponse()
        {
            ComposeExtensionResponse response = null;

            var query = activity.GetComposeExtensionQueryData();
            JObject data = activity.Value as JObject;

            // Check if this is a task module invocation.
            if (data != null && data["type"] != null)
            {
                // Handle other types of Invoke activities here, e.g. CardActions
                if (data["type"].ToString() == TaskModuleCommandType && data["command"].ToString() == CreatePostingCommand)
                {
                    response = CreateTaskModuleResponse();
                }
            }
            else
            {
                //Check to make sure a query was actually made:
                if (query.CommandId == null || query.Parameters == null)
                {
                    return null;
                }
                else if (query.Parameters.Count > 0)
                {
                    // query.Parameters has the parameters sent by client
                    var results = new ComposeExtensionResult()
                    {
                        AttachmentLayout = "list",
                        Type = "result",
                        Attachments = new List<ComposeExtensionAttachment>(),
                    };

                    if (query.CommandId == "searchPositions")
                    {
                        OpenPositionsDataController controller = new OpenPositionsDataController();
                        IEnumerable<OpenPosition> positions;

                        if (query.Parameters[0].Name == "initialRun")
                        {
                            // Default query => list all
                            positions = controller.ListOpenPositions(10);
                        }
                        else
                        {
                            // Basic search.
                            string title = query.Parameters[0].Value.ToString().ToLower();
                            positions = controller.ListOpenPositions(10).Where(x => x.Title.ToLower().Contains(title));
                        }

                        // Generate cards for the response.
                        foreach (OpenPosition pos in positions)
                        {
                            var card = CardHelper.CreateCardForPosition(pos, true);

                            var composeExtensionAttachment = card.ToAttachment().ToComposeExtensionAttachment();
                            results.Attachments.Add(composeExtensionAttachment);
                        }
                    }
                    else if (query.CommandId == "searchCandidates")
                    {
                        string name = query.Parameters[0].Value.ToString();
                        CandidatesDataController controller = new CandidatesDataController();

                        foreach (Candidate c in controller.GetTopCandidates("ABCD1234"))
                        {
                            c.Name = c.Name.Split(' ')[0] + " " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
                            var card = CardHelper.CreateSummaryCardForCandidate(c);

                            var composeExtensionAttachment = card.ToAttachment().ToComposeExtensionAttachment(CardHelper.CreatePreviewCardForCandidate(c).ToAttachment());
                            results.Attachments.Add(composeExtensionAttachment);
                        }
                    }

                    response = new ComposeExtensionResponse()
                    {
                        ComposeExtension = results
                    };
                }
            }
            return response;
        }

        /// <summary>
        /// Helper method to create a task module response using an adaptive card.
        /// </summary>
        /// <returns></returns>
        private ComposeExtensionResponse CreateTaskModuleResponse()
        {
            ComposeExtensionResponse response = new ComposeExtensionResponse()
            {
                ComposeExtension = new ComposeExtensionResult("medium", "adaptivecard", new List<ComposeExtensionAttachment>())
            };
            var card = CardHelper.CreateExtendedCardForNewJobPosting();
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            var composeExtensionAttachment = attachment.ToComposeExtensionAttachment();
            response.ComposeExtension.Attachments.Add(composeExtensionAttachment);
            System.Diagnostics.Debug.WriteLine(response.ToString());

            return response;
        }
    }
}