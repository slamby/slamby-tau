using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Design
{
    class DesignTagManager : ITagManager
    {
        public Task<ClientResponseWithObject<IEnumerable<Tag>>> GetTagsAsync(bool withDetails = false)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<Tag>> GetTagAsync(string tagId, bool withDetails = false)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<Tag>> CreateTagAsync(Tag tag)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> UpdateTagAsync(string tagId, Tag tag)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> DeleteTagAsync(string tagId, bool force, bool cleanDocuments)
        {
            throw new NotImplementedException();
        }
    }
}
