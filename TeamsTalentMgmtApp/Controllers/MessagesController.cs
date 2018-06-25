using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using System.Configuration;
using TeamsTalentMgmtApp.Utils;
using Newtonsoft.Json.Linq;

namespace TeamsTalentMgmtApp
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        /// <summary>
        /// POST: api/messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
          
            if (activity.Type == ActivityTypes.Message) 
            {
                //Handle basic message types, e.g. user initiated
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }
            else if (activity.Type == ActivityTypes.Invoke) 
            {
                //Compose extensions come in as Invokes. Leverage the Teams SDK helper functions
                if (activity.IsComposeExtensionQuery())
                {
                    // Determine the response object to reply with
                    var invokeResponse = new MessagingExtension(activity).CreateResponse();

                    // Return the response
                    return Request.CreateResponse(HttpStatusCode.OK, invokeResponse);
                } else if (activity.Name == "fileConsent/invoke")
                {
                    // Try to replace with File uploaded card.
                    return Request.CreateResponse(HttpStatusCode.OK);
                } else if (activity.Name == "signin/verifyState")
                {
                    await HandleLoginVerification(activity);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task HandleLoginVerification(Activity invoke)
        {
            string connectionName = ConfigurationManager.AppSettings["ConnectionName"];
            JObject ctx = invoke.Value as JObject;

            if (ctx != null)
            {
                string code = ctx["code"].ToString();
                var oauthClient = invoke.GetOAuthClient();
                var token = await oauthClient.OAuthApi.GetUserTokenAsync(invoke.From.Id, connectionName, magicCode: invoke.Text).ConfigureAwait(false);
                if (token != null)
                {
                    Microsoft.Graph.User current = await new GraphUtil(token.Token).GetMe();
                    
                    await context.PostAsync($"Success! You are now signed in as {current.DisplayName} with {current.Mail}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task HandleSystemMessage(Activity message)
        {

            if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info

                // This event is called when the Bot is added too, so we can trigger a welcome message if the member added is the bot:
                TeamEventBase eventData = message.GetConversationUpdateData();
                if (eventData.EventType == TeamEventType.MembersAdded)
                {
                    for (int i = 0; i < message.MembersAdded.Count; i++)
                    {
                        //Check to see if the member added was the bot itself.  We're leveraging the fact that the inbound payload's Recipient is the bot.
                        if (message.MembersAdded[i].Id == message.Recipient.Id)
                        {
                            // We'll use normal message parsing to display the welcome message.
                            message.Text = "welcome";
                            await Conversation.SendAsync(message, () => new Dialogs.RootDialog());

                            break;
                        }
                    }
                }
            }
            else if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {

            }
        }
    }
}