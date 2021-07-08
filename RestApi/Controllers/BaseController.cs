using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.Controllers
{
    public class BaseController<T> : Controller
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger<T> _logger;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public BaseController(ILogger<T> logger)
        {
            _logger = logger;
        }
        #endregion
    }
}
