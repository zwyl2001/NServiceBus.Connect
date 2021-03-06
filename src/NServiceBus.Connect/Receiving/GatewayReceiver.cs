namespace NServiceBus.Connect.Receiving
{
    using System.Collections.Generic;
    using Logging;
    using Notifications;
    using NServiceBus.Features;
    using ObjectBuilder;
    using Routing;
    using Satellites;
    using Settings;
    using Transports;

    internal class GatewayReceiver : ISatellite
    {
        public GatewayReceiver()
        {
            activeReceivers = new List<IReceiveMessagesFromSites>();
        }

        public ISendMessages MessageSender { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IRouteMessagesToEndpoints EndpointRouter { get; set; }
        public IBuilder builder { get; set; }

        public void Stop()
        {
            Logger.InfoFormat("Receiver is shutting down");

            foreach (var channelReceiver in activeReceivers)
            {
                Logger.InfoFormat("Stopping channel - {0}", channelReceiver.GetType());

                channelReceiver.MessageReceived -= MessageReceivedOnChannel;

                channelReceiver.Dispose();
            }

            activeReceivers.Clear();

            Logger.InfoFormat("Receiver shutdown complete");
        }

        public bool Handle(TransportMessage message)
        {
            return true;
        }

        public Address InputAddress
        {
            get { return null; }
        }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<NServiceBus.Connect.Features.Gateway>(); }
        }

        public void Start()
        {
            replyToAddress = SettingsHolder.Get<Address>("Gateway.InputAddress");

            foreach (var receiveChannel in ChannelManager.GetReceiveChannels())
            {
                var receiver = builder.Build<IReceiveMessagesFromSites>();

                receiver.MessageReceived += MessageReceivedOnChannel;
                receiver.Start(receiveChannel, receiveChannel.NumberOfWorkerThreads);
                activeReceivers.Add(receiver);

                Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
            }
        }

        void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            var messageToSend = e.Message;

            messageToSend.ReplyToAddress = replyToAddress;

            var destination = EndpointRouter.GetDestinationFor(messageToSend);

            Logger.Info("Sending message to " + destination);

            MessageSender.Send(messageToSend, destination);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(GatewayReceiver));
        readonly ICollection<IReceiveMessagesFromSites> activeReceivers;

        Address replyToAddress;
    }
}