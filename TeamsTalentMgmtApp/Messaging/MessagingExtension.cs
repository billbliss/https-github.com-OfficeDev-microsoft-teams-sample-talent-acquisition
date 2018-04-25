using Bogus;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using TeamsTalentMgmtApp.Utils;
using TeamsTalentMgmtApp.DataModel;

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
        /// Helper method to generate a compose extension
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

                OpenPositionsDataController controller = new OpenPositionsDataController();
                List<OpenPosition> positions = controller.ListOpenPositions();

                // Generate cards for the response.
                foreach (OpenPosition pos in positions)
                {
                    var card = CardHelper.CreateCardForPosition(pos, true);
                    var previewCard = CardHelper.CreateCardForPosition(pos);

                    var composeExtensionAttachment = card.ToAttachment().ToComposeExtensionAttachment(previewCard.ToAttachment());
                    results.Attachments.Add(composeExtensionAttachment);
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