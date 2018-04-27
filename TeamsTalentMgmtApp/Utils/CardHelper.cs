using AdaptiveCards;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web;
using TeamsTalentMgmtApp.DataModel;

namespace TeamsTalentMgmtApp.Utils
{
    /// <summary>
    /// Helper class to generate cards based off the existing data models.
    /// </summary>
    public class CardHelper
    {
        /// <summary>
        /// JSON template.
        /// </summary>
        private static string cardJson = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath("~/cardtemplate.json"));

        #region Card Helpers

        public static ThumbnailCard CreateCardForCandidate(Candidate c)
        {
            var random = new Random();

            ThumbnailCard card = new ThumbnailCard()
            {
                Title = c.Name,
                Subtitle = $"Job ID: {c.ReqId}",
                Text = $"Current role: {c.CurrentRole}<br/> <b>Stage:</b> {c.Stage}<br/> <b>Hire:</b> {c.Hires} <b>No hire:</b> {c.NoHires}",
                Images = new List<CardImage>()
            };

            card.Images.Add(new CardImage(c.ProfilePicture));

            JObject ctx = new JObject();
            ctx["reqId"] = c.ReqId;
            ctx["name"] = c.Name;

            card.Buttons = new List<CardAction>()
                {
                    new CardAction("openUrl", "See details", null, "https://www.microsoft.com"),
                    new CardAction("messageBack", "Schedule interview", null, ctx, "schedule interview", $"Schedule interview with {c.Name}"),
                    new CardAction("openUrl", "Read feedback", null, "https://www.microsoft.com"),
                };

            return card;
        }

        public static AdaptiveCard CreateAdaptiveCardForInterviewRequest(InterviewRequest request, Candidate c)
        {
            AdaptiveCard card = AdaptiveCard.FromJson(cardJson).Card;

            return card;
        }

        // Helps create an O365 actionable message for a particular task.
        public static O365ConnectorCard CreateCardForInterviewRequest(InterviewRequest request, Candidate c)
        {
            var random = new Random();

            O365ConnectorCard actionableCard = new O365ConnectorCard()
            {
                Sections = new List<O365ConnectorCardSection>()
            };

            O365ConnectorCardSection section = new O365ConnectorCardSection()
            {
                ActivityTitle = request.CandidateName,
                ActivitySubtitle = $"For position: {request.PositionTitle}",
                ActivityText = $"Req ID: {request.ReqId}",
                ActivityImage = c.ProfilePicture,
                PotentialAction = new List<O365ConnectorCardActionBase>()
            };

            // Add a more complex form action
            O365ConnectorCardActionCard updateDateAction = new O365ConnectorCardActionCard(type: "ActionCard")
            {
                Id = "updateInterviewDate",
                Name = "Set interview date",
                Actions = new List<O365ConnectorCardActionBase>(),
                Inputs = new List<O365ConnectorCardInputBase>()
            };
            updateDateAction.Actions.Add(new O365ConnectorCardHttpPOST("HttpPOST", "Schedule", "scheduleInterview", request.ReqId));
            updateDateAction.Inputs.Add(new O365ConnectorCardDateInput("DateInput", "interviewDate", false, "Interview date", new DateTime().ToString("MMM d, yyyy"), false));
            section.PotentialAction.Add(updateDateAction);

            actionableCard.Sections.Add(section);

            return actionableCard;
        }

        // Helps create a simple thumbnail card for a task
        public static ThumbnailCard CreateCardForPosition(OpenPosition position, bool includeButtons = false)
        {
            var random = new Random();

            ThumbnailCard card = new ThumbnailCard()
            {
                Title = position.Title,
                Subtitle = $"Applicants: {position.Applicants}  Days open: {position.DaysOpen} Hiring manager: {position.HiringManager}",
                Text = $"Req ID: {position.ReqId}",
            };

            if (includeButtons)
            {
                card.Buttons = new List<CardAction>()
                {
                    new CardAction("openUrl", "See details", null, "https://hr.contoso.com"),
                    new CardAction("messageBack", "Update status", null, position.ReqId, "update position", $"Update position status for {position.ReqId}"),
                };
            }

            return card;
        }

        #endregion
    }
}