using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using log4net;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;

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
            Logger.Debug("Static" + title + " - " + dt);
            _logger.Debug("Dynamic" + title + " - " + dt);
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            if(id> 501)
            {
               var randoms = Enumerable.Range(1, 1000).Select(x => new SomeRandomClass { id = x }).ToArray();
                _logger.Debug(JsonConvert.SerializeObject(randoms));
            }
            else if(id == 422)
            {
                var exc = new Exception("There was an exception created by the app");
                _logger.Error("ErrorMessage Generated intentionally by app", exc);
            }
            else if(id > 201 && id < 300)
            {
                var rando = new SomeRandomClass { id = id };
                _logger.Info($"Writing some info messages {JsonConvert.SerializeObject(rando)}");
            }
            else if(id == 400)
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
            var dt = DateTime.Now.ToString("G");
            var title = "ValueGet-" + id;
            Logger.Debug("Static"+title + " - " + dt);
            _logger.Debug("Dynamic"+title + " - " + dt);
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string title)
        {
            var dt = DateTime.Now.ToString("G");
            Logger.Debug("Static" + title + " - " + dt);
            _logger.Debug("Dynamic" + title + " - " + dt);

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    public class SomeRandomClass
    {
        public string Name { get; set; } = "The Name";
        public int id { get; set; }
    }
}
