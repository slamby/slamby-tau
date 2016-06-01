using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Design
{
    public class DesignDataSetManager : IDataSetManager
    {
        public Task<ClientResponse> CreateDataSetAsync(DataSet dataSet)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> CreateDataSetSchemaAsync(DataSet dataSet)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> DeleteDataSetAsync(string dataSetName)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<DataSet>> GetDataSetAsync(string dataSetName)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<IEnumerable<DataSet>>> GetDataSetsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
