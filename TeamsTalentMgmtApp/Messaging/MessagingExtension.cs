﻿using Bogus;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using TeamsTalentMgmtApp.Utils;
using TeamsTalentMgmtApp.DataModel;
using System.Globalization;

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
                        positions = controller.ListOpenPositions(10);
                    }
                    else
                    {
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
                } else if (query.CommandId == "searchCandidates")
                {
                    string name = query.Parameters[0].Value.ToString();
                    CandidatesDataController controller = new CandidatesDataController();

                    foreach(Candidate c in controller.GetTopCandidates("ABCD1234"))
                    {
                        c.Name = c.Name.Split(' ')[0] + " " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
                        var card = CardHelper.CreateCardForCandidate(c);

                        var composeExtensionAttachment = card.ToAttachment().ToComposeExtensionAttachment(CardHelper.CreatePreviewCardForCandidate(c).ToAttachment());
                        results.Attachments.Add(composeExtensionAttachment);
                    }
                }

                response = new ComposeExtensionResponse()
                {
                    ComposeExtension = results
                };
            }
            return response;
        }
    }
}