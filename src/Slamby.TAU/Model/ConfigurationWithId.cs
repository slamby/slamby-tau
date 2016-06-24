using System;
using Slamby.SDK.Net;

namespace Slamby.TAU.Model
{
    public class ConfigurationWithId : Configuration
    {
        public ConfigurationWithId()
        {
            Id = Guid.NewGuid();
        }

        public ConfigurationWithId(Configuration configuration)
        {
            ApiBaseEndpoint = configuration.ApiBaseEndpoint;
            ApiSecret = configuration.ApiSecret;
            ParallelLimit = configuration.ParallelLimit;
            Timeout = configuration.Timeout;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        private int _bulkSize = 1000;
        public int BulkSize
        {
            get { return _bulkSize; }
            set {
                _bulkSize = value < 1 ? 1 : value;
            }
        }

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