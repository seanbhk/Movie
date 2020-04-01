using Microsoft.AspNetCore.Mvc;
using Movies.Web.Models.Movies;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Schema.NET;

namespace Movies.Web.Controllers
{
    public class MoviesController : Controller
    {
        private IConfiguration configuration;
       
        public MoviesController(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        public async Task<IActionResult> Index(string s, string y)
        {
            string apiUrl = configuration.GetValue<string>("api:url");
            string apiKey = configuration.GetValue<string>("api:key");

            UriBuilder baseUri = new UriBuilder(apiUrl + "?apiKey="+ apiKey);

            var model = new SearchResults();

            if (s != null && s.Length > 0)
            {
                string queryToAppend = "s="+s;
                if (baseUri.Query != null && baseUri.Query.Length > 1)
                    baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
                else
                    baseUri.Query = queryToAppend;
            }

            bool checkYear = int.TryParse(y, out int year);
            if (y != null && y.Length == 4 && checkYear)
            {
                string queryToAppend = "y=" + y;
                if (baseUri.Query != null && baseUri.Query.Length > 1)
                    baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
                else
                    baseUri.Query = queryToAppend;
            }

            if(s != null && s.Length > 0 || (y != null && y.Length == 4 && checkYear))
            {
                string url = baseUri.ToString();

                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        JObject o = JObject.Parse(apiResponse);
                        if ((string)o["Response"] == "True")
                        {
                            JArray arrMovies = (JArray)o["Search"];
                            IList<SearchResult> list = arrMovies.ToObject<IList<SearchResult>>();
                            model.Results = list;

                            int position = 1;
                            List<IListItem> itemElements = new List<IListItem>();
                            foreach (SearchResult sResult in list)
                            {
                                var i = new Schema.NET.ListItem()
                                {
                                    Position = position,
                                    Item = new Schema.NET.Movie()
                                    {
                                        Name = sResult.Title,
                                        Image = new Uri(sResult.Poster),
                                        Url = new Uri(sResult.Poster)
                                    }
                                };
                                itemElements.Add(i);
                                position++;
                            }

                            var itemListSchema = new Schema.NET.ItemList()
                            {
                                ItemListElement = itemElements
                            };
                            ViewBag.JsonLd = itemListSchema.ToString();
                        }
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Details(string i)
        {
            string apiUrl = configuration.GetValue<string>("api:url");
            string apiKey = configuration.GetValue<string>("api:key");

            UriBuilder baseUri = new UriBuilder(apiUrl + "?apiKey=" + apiKey);

            Details details = null;

            if (i != null && i.Length > 0)
            {
                string queryToAppend = "i=" + i;
                if (baseUri.Query != null && baseUri.Query.Length > 1)
                    baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
                else
                    baseUri.Query = queryToAppend;

                string url = baseUri.ToString();

                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(url))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        JObject o = JObject.Parse(apiResponse);
                        if ((string)o["Response"] == "True")
                        {
                            details = JsonConvert.DeserializeObject<Details>(apiResponse);
                        }
                    }
                }
            }

            Person actor = new Person()
            {
                Name = new List<String>()
                {
                    details.Actors
                }
            };

            Person director = new Person()
            {
                Name = details.Director
            };

            Country c = new Country()
            {
                Name = details.Country
            };

            int minDuration = 0;
            if(details.Runtime.Length > 0 && details.Runtime.Contains(' '))
            {
                string[] sDuration = details.Runtime.Split(' ');
                if(sDuration.Length > 0)
                {
                    minDuration = int.Parse(sDuration[0]);
                }
            }
            int hours = 0,
                minutes = 0;
            if(minDuration > 0)
            {
                hours = minDuration / 60;
                minutes = minDuration % 60;
            }
            OneOrMany<TimeSpan?> duration = new OneOrMany<TimeSpan?>(new TimeSpan(hours, minutes, 0));

            var organization = new Organization()
            {
                Name = details.Production
            };

            var movieSchema = new Schema.NET.Movie()
            {
                Actor = actor,
                CountryOfOrigin = c,
                Director = director,
                Duration = duration,
                SubtitleLanguage = details.Language,
                ProductionCompany = organization
            };
            ViewBag.JsonLd = movieSchema.ToString();

            return View(details);
        }
    }
}