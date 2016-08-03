using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slamby.SDK.Net.Models.Services;

namespace Slamby.TAU.Model
{
    public class ExtendedService : Service
    {
        public bool IsIndexed { get; set; }

        public ExtendedService(Service service)
        {
            this.ActualProcessId = service.ActualProcessId;
            this.Alias = service.Alias;
            this.Description = service.Description;
            this.Id = service.Id;
            this.Name = service.Name;
            this.ProcessIdList = service.ProcessIdList;
            this.Status = service.Status;
            this.Type = service.Type;
        }
    }
}
