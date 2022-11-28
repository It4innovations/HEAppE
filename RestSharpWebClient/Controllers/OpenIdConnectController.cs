using Microsoft.AspNetCore.Mvc;
using RestSharpWebClient.Model;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RestSharpWebClient.Controllers
{
    public class OpenIdConnectController : Controller
    {
        internal static string KeyCloakBaseUrl;

        public IActionResult Index()
        {
            return View(new OpenIdConnectModel());
        }

        private IActionResult Index(OpenIdConnectModel model)
        {
            return View("Index", model);
        }

        private string UE(string input) => Uri.EscapeDataString(input);

        [HttpPost]
        public IActionResult OpenIdAuthenticate(OpenIdConnectModel model)
        {
            throw new NotImplementedException();
        }


        [HttpPost]
        public IActionResult OpenIdTokenIntrospection(OpenIdConnectModel model)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public IActionResult OpenStackAuthenticate(OpenIdConnectModel model)
        {
            // OpenStack openStack = new OpenStack();
            // try
            // {
            //     var applicationCredentials = openStack.CreateApplicationCredentials("test_name" + (DateTime.UtcNow.Ticks / 4), 600);
            //     model.OpenStackResult = JsonConvert.SerializeObject(applicationCredentials, Formatting.Indented);
            // }
            // catch (OpenStackAPIException e)
            // {
            //     model.RequestWasSuccessful = false;
            //     model.OpenStackResult = e.Message;
            // }

            model.RequestWasSuccessful = true;
            model.OpenStackResult = "Use RestApi for testing.";
            return Index(model);
        }

        class Test
        {
            // [JsonProperty("realm_access")]
            // public RoleList EffectiveRoles { get; set; }

            [JsonProperty("realm_access")]
            public Dictionary<string, List<string>> EffectiveRoles { get; set; }
        }


        [HttpPost]
        public IActionResult TestMethod()
        {
            return Index();
        }
    }
}