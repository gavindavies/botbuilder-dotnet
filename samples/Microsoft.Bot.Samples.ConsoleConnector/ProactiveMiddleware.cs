﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace Microsoft.Bot.Samples
{
    public class ProactiveMiddleware : IReceiveActivity, IContextCreated
    {
        private Microsoft.Bot.Builder.Bot _bot;
        private ConversationReference _conversationReference;

        public Task ContextCreated(BotContext context)
        {
            // Keep the Bot around, so it can be used later 
            // for sending messages to the user
            _bot = context.Bot;
            return Task.CompletedTask;
        }

        public async Task<ReceiveResponse> ReceiveActivity(BotContext context)
        {
            if (context.IfIntent("delayIntent"))
            {
                int delay = context.TopIntent.Entities[0].ValueAs<int>();

                context.Reply($"Scheduling callback in {delay} milliseconds.");

                // used for rehydrating the conversation in the proactive message path
                _conversationReference = context.ConversationReference;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Delay(delay)
                 .ContinueWith((t) => SendOutOfBandMessage($"--> Delayed {delay} milliseconds. <--"))
                 .ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return new ReceiveResponse(true);
            }

            return new ReceiveResponse(false);
        }

        private async Task SendOutOfBandMessage(string message)
        {
            // recreate context from conversationReference
            await _bot
                .CreateContext(_conversationReference, async (context) => context.Reply(message))
                .ConfigureAwait(false);
        }
    }
}
