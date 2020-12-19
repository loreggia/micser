using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Audio;
using Micser.Common.Controllers;

namespace Micser.Controllers
{
    public class DevicesController : ApiController
    {
        private readonly DeviceService _deviceService;

        public DevicesController(DeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [Route("{type}")]
        public IEnumerable<DeviceDescription> GetByType([FromRoute] DeviceType type)
        {
            return _deviceService.GetDevices(type);
        }
    }
}