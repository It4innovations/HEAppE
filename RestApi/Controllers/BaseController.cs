using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        protected IMemoryCache _cacheProvider;
        #endregion
        #region Properties
        protected long MemoryCacheInterval = 1;
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="cacheProvider">Memory Cache provider instance</param>
        public BaseController(ILogger<T> logger, IMemoryCache cacheProvider)
        {
            _logger = logger;
            _cacheProvider = cacheProvider;
        }
        #endregion
    }
}
