namespace NServiceBus.Connect.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    internal class ConventionBasedChannelManager : IManageReceiveChannels
    {
        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            yield return new ReceiveChannel
            {
                Address = string.Format("http://localhost/{0}/", Configure.EndpointName),
                Type = "Http",
                NumberOfWorkerThreads = 1
            };
        }

        public Channel GetDefaultChannel(IEnumerable<string> types)
        {
            return GetReceiveChannels().First();
        }
    }
}