using System;
using Slamby.SDK.Net;

namespace Slamby.TAU.Model
{
    public class ConfigurationWithId : Configuration
    {
        public ConfigurationWithId()
        { }

        public ConfigurationWithId(Configuration configuration)
        {
            ApiBaseEndpoint = configuration.ApiBaseEndpoint;
            ApiSecret = configuration.ApiSecret;
            ParallelLimit = configuration.ParallelLimit;
            Timeout = configuration.Timeout;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is ConfigurationWithId))
                return false;
            return Id == ((ConfigurationWithId)obj).Id;
        }

        protected bool Equals(ConfigurationWithId other)
        {
            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}