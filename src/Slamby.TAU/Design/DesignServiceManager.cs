using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Models;
using Slamby.SDK.Net.Models.Enums;
using Slamby.SDK.Net.Models.Services;

namespace Slamby.TAU.Design
{
    public class DesignServiceManager : IServiceManager
    {
        public async Task<ClientResponseWithObject<Service>> CreateServiceAsync(Service service)
        {
            await Task.Delay(500);
            service.Id = Guid.NewGuid().ToString();
            return new ClientResponseWithObject<Service> { Errors = null, HttpStatusCode = HttpStatusCode.OK, IsSuccessFul = true, ResponseObject = service };
        }

        public async Task<ClientResponse> DeleteServiceAsync(string serviceId)
        {
            await Task.Delay(500);
            return new ClientResponse { Errors = null, HttpStatusCode = HttpStatusCode.OK, IsSuccessFul = true };
        }

        public async Task<ClientResponseWithObject<Service>> GetServiceAsync(string serviceId)
        {
            throw new NotImplementedException();
        }

        public async Task<ClientResponseWithObject<IEnumerable<Service>>> GetServicesAsync()
        {
            await Task.Delay(200);
            var rand = new Random();
            var services = new List<Service>();
            for (int i = 0; i < 10; i++)
            {
                services.Add(new ClassifierService { Description = "This is a description for Service" + i, Name = "Service" + i, Id = Guid.NewGuid().ToString(), Status = (ServiceStatusEnum)rand.Next(4), Type = (ServiceTypeEnum)rand.Next(0) });
            }
            return new ClientResponseWithObject<IEnumerable<Service>> { Errors = null, HttpStatusCode = HttpStatusCode.OK, IsSuccessFul = true, ResponseObject = services };
        }

        public async Task<ClientResponse> UpdateServiceAsync(string serviceId, Service service)
        {
            await Task.Delay(500);
            return new ClientResponseWithObject<Service> { Errors = null, HttpStatusCode = HttpStatusCode.OK, IsSuccessFul = true, ResponseObject = service };
        }
    }
}
