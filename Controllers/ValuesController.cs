using log4net;
using LogTest3.Appenders;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogTest3.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILog _logger;
        public ValuesController()
        {
            _logger = Logger.GetLogger(typeof(ValuesController));
        }
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            //var repos = LogManager.GetAllRepositories();
            //foreach(var repo in repos)
            //{
            //    var t = repo;
            //}

            var dt = DateTime.Now.ToString("G");
            var title = "DefaultGet";
            // Logger.Debug("Static" + title + " - " + dt);
            _logger.Debug("Dynamic" + title + " - " + dt);
            return new string[] { "Try /values/Many/{types}?total=30", "Where types == IO|Error|Flat|Monster|Html|CloudWatch|Json" };
        }

        [HttpGet("Many/{type}")]
        public ActionResult<string> ManyGet(string type, int total = 20)
        {
            int cap = 50;
            var logger = _logger;
            var message = " for the FlatFile";
            if (type.Equals("IO", StringComparison.OrdinalIgnoreCase))
            {
                logger = Logger.GetLogger(typeof(IOCustomMiddleware));
                message = " for the IO MIddleware";
            }
            else if (type.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                logger = Logger.GetLogger(typeof(ExceptionInception));
                message = " for the Error ExceptionInception Domain";
            }else if(type.Equals("Monster", StringComparison.OrdinalIgnoreCase) )
            {
                logger = Logger.GetLogger(typeof(Monster));
                message = " for the Error Monster Files. This should write a bunch of files at once.";
                //All the Loggers have a cap of 50, Monster can do 500 if the right total is 
                //passed in
                if(total == 21585)
                    cap = 500;
            }
            else if(type.Equals("Html", StringComparison.OrdinalIgnoreCase))
            {
                logger = Logger.GetLogger(typeof(HtmlFilter));
                message = " for the Html Files. Generates Html Files from an appender setup purely in c#";
            }
            else if (type.Equals("CloudWatch", StringComparison.OrdinalIgnoreCase))
            {
                logger = Logger.GetLogger(typeof(CloudWatchFilter));
                message = " for the CloudWatch Logs, check Logging.Startup LogGroup";
            }
            else if (type.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                logger = Logger.GetLogger(typeof(JsonFilter));
                message = " for the Json Logs, check the content of viles";
            }
            if (total > cap)
                total = cap;
            Enumerable.Range(1, total).ToList().ForEach(x =>

            logger.Debug(JsonConvert.SerializeObject(new SomeRandomClass { id = x })));
            return $"<b>Ran {total} Debug Statements{message} - run values/Many/IO|Error|Flat|Monster|Html|CloudWatch|json?total=30</b>";
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            if (id > 501)
            {
                var randoms = Enumerable.Range(1, 1000).Select(x => new SomeRandomClass { id = x }).ToArray();
                _logger.Debug(JsonConvert.SerializeObject(randoms));
            }
            else if (id == 422)
            {
                var exc = new Exception("There was an exception created by the app");
                _logger.Error("ErrorMessage Generated intentionally by app", exc);
            }
            else if (id > 201 && id < 300)
            {
                var rando = new SomeRandomClass { id = id };
                _logger.Info($"Writing some info messages {JsonConvert.SerializeObject(rando)}");
            }
            else if (id == 400)
            {
                throw new AppException("Random Error thrown by app code");
            }
            else if (id == 401)
            {
                throw new KeyNotFoundException("Another Random Error thrown by app code");
            }
            else if (id == 402)
            {
                throw new ApplicationException("Another Random Error created and thrown by app code");
            }
            else if (id == 66)
            {
                throw new MissingFieldException("Specific Exception that will cause an exception in the Appender");
            }
            else if (id == 67)
            {
                throw new ArithmeticException("Specific Exception that will cause an exception in the Appender");
            }
            var dt = DateTime.Now.ToString("G");
            var title = "ValueGet-" + id;
           
            _logger.Debug("Dynamic" + title + " - " + dt);
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string title)
        {
            var dt = DateTime.Now.ToString("G");
            _logger.Debug("Dynamic" + title + " - " + dt);

        }

    }

    public class SomeRandomClass
    {
        public string Name { get; set; } = "The Name";
        public int id { get; set; }
    }

    public class Monster
    {

    }
}
