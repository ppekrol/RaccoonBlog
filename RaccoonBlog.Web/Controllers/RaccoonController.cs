using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using DevTrends.MvcDonutCaching;
using HibernatingRhinos.Loci.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RaccoonBlog.Web.Helpers.Results;
using RaccoonBlog.Web.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace RaccoonBlog.Web.Controllers
{
    public abstract partial class RaccoonController : Controller
    {
        public static IDocumentStore DocumentStore { get; set; }

        public IDocumentSession RavenSession { get; set; }

        protected HttpStatusCodeResult HttpNotModified()
        {
            return new HttpStatusCodeResult(304);
        }

        protected ActionResult Xml(XDocument xml, string etag)
        {
            return new XmlResult(xml, etag);
        }

        public const int DefaultPage = 1;

        private BlogConfig blogConfig;
        public BlogConfig BlogConfig
        {
            get
            {
                if (blogConfig == null)
                {
                    using (RavenSession.Advanced.DocumentStore.AggressivelyCacheFor(TimeSpan.FromMinutes(5)))
                    {
                        blogConfig = RavenSession.Load<BlogConfig>(BlogConfig.Key);
                    }

                    if (blogConfig == null && "welcome".Equals((string)RouteData.Values["controller"], StringComparison.OrdinalIgnoreCase) == false) // first launch
                    {
                        HttpContext.Response.Redirect("~/welcome", true);
                    }
                }
                return blogConfig;
            }
        }

        private List<Section> sections;
        public List<Section> Sections
        {
            get
            {
                if (sections == null)
                {
                    using (RavenSession.Advanced.DocumentStore.AggressivelyCacheFor(TimeSpan.FromMinutes(5)))
                    {
                        sections = RavenSession
                            .Query<Section>()
                            .ToList();
                    }
                }
                return sections;
            }
        }

        private OutputCacheManager outputCacheManager;
        protected OutputCacheManager OutputCacheManager => outputCacheManager ?? (outputCacheManager = new OutputCacheManager());

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.IsHomePage = false;
            RavenSession = (IDocumentSession)HttpContext.Items["CurrentRequestRavenSession"];
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            ViewBag.BlogConfig = BlogConfig;
            ViewBag.Sections = Sections;
        }

        public int PageSize
        {
            get { return BlogConfig.PostsOnPage; }
        }

        protected int CurrentPage
        {
            get
            {
                var s = Request.QueryString["page"];
                int result;
                if (int.TryParse(s, out result))
                    return Math.Max(DefaultPage, result);
                return DefaultPage;
            }
        }

        protected new JsonNetResult Json(object data)
        {
            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            return new JsonNetResult(data, settings);
        }
    }
}
