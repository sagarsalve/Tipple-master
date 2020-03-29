using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using api.Models.Response;
using api.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace api.Controllers
{
    [Route("api")]
    [ApiController]
    public class BoozeController : ControllerBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();  //static HTTPClient object for requests
        private AppSettings _appSettings;                                   //AppSettings object to access app properties like filter and lookup urls
        public BoozeController(IOptions<AppSettings> appSettings)           // Constructor for booze controller
        {
            _appSettings = appSettings.Value;                               //initialising appsettings property 
        }

        [HttpGet]
        [Route("search-ingredient/{ingredient}")]
        public async Task<IActionResult> GetIngredientSearch([FromRoute] string ingredient)
        {
            var cocktailList = new CocktailList();


            using (var filterResponse = await _httpClient.GetAsync(_appSettings.filterUrl + ingredient))  //finding cocktails with the given ingredient 
            {
                var listMeta = new ListMeta();   
                string apiResponseFilter = await filterResponse.Content.ReadAsStringAsync(); 

                if (IsValidJson(apiResponseFilter))
                {
                    dynamic jsonResultFilter = JsonConvert.DeserializeObject(apiResponseFilter); // Converting Json string to dynamic object 
                    listMeta.firstId = Int32.MaxValue;                                                      
                    listMeta.lastId = 0;
                    var totalIngredientcount = 0;
                    List<Cocktail> cocktails = new List<Cocktail>();
                    if (jsonResultFilter.drinks.Count > 0)
                    {
                        foreach (dynamic rc in jsonResultFilter.drinks)       //Itrating through all cocktails found
                        {

                            using var lookupResponse = await _httpClient.GetAsync(_appSettings.lookupUrl + rc.idDrink);  // finding drink with requested id using lookup api
                            string apiResponseLookup = await lookupResponse.Content.ReadAsStringAsync();
                            if (IsValidJson(apiResponseLookup))
                            {
                                dynamic jsonResultLookup = JsonConvert.DeserializeObject(apiResponseLookup);    //Converting Json string returned from lookup API to dynamic object
                                var drink = jsonResultLookup.drinks[0];
                                if (drink != null)
                                {
                                    Cocktail cocktail = new Cocktail();
                                    var ingredientList = new List<string>();

                                    foreach (var property in drink)   //Iterating through properties of returned object
                                    {
                                        var name = property.Name;
                                        switch (name)                   
                                        {
                                            case "idDrink":
                                                cocktail.Id = property.Value;                   // cocktail id
                                                break;

                                            case "strDrink":                                    //cocktail name
                                                cocktail.Name = property.Value;
                                                break;
                                            case "strInstructions":                             // cocktail instructions   
                                                cocktail.Instructions = property.Value;
                                                break;
                                            case "strDrinkThumb":                               // cocktail thumbanil 
                                                cocktail.ImageURL = property.Value;
                                                break;
                                            default:
                                                if (name.Contains("Ingredient"))                // all cocktail ingredients will be found with this case
                                                {
                                                    var ingredientName = property.Value.ToString();
                                                    if (!String.IsNullOrEmpty(ingredientName))
                                                    {
                                                        ingredientList.Add(ingredientName);
                                                        totalIngredientcount++;
                                                    }

                                                }
                                                break;

                                        }
                                    }

                                    cocktail.Ingredients = ingredientList;
                                    if (listMeta.firstId > cocktail.Id)
                                    {
                                        listMeta.firstId = cocktail.Id;
                                    }
                                    if (listMeta.lastId < cocktail.Id)
                                    {
                                        listMeta.lastId = cocktail.Id;
                                    }
                                    cocktails.Add(cocktail);
                                }
                                else
                                {
                                    return Ok("Lookup API couldnot find a drink with that Id ="+ rc.idDrink);
                                }
                                
                            }
                            else
                            {
                                return Ok("Lookup API returned data with invalid Json Format");
                            }
                        }
                        listMeta.count = cocktails.Count;
                        listMeta.medianIngredientCount = (int)totalIngredientcount / listMeta.count;    // calculating median of number of ingredients
                        cocktailList.meta = listMeta;
                        cocktailList.Cocktails = cocktails;
                    }
                    else
                    {
                        return Ok("Sorry!! No cocktails containing the request ingredient were found");
                    }

                        
                }
                else
                {
                    return Ok("Filter API returned data with invalid Json Format");
                }
               




            }

            return Ok(cocktailList);
        }

        [HttpGet]
        [Route("random")]
        public async Task<IActionResult> GetRandom()
        {
            var cocktail = new Cocktail();

            using (var response = await _httpClient.GetAsync(_appSettings.randomUrl))   //getting a random cocktail from random cocktail API
            {
                string apiResponse = await response.Content.ReadAsStringAsync();     
                dynamic dynamicObject = JsonConvert.DeserializeObject(apiResponse);  //Converting Json string returned from lookup API to dynamic object
                var drink = dynamicObject.drinks[0];
                var ingredientList = new List<string>();
                foreach (var property in drink)
                {
                    var name = property.Name;
                    switch (name)
                    {
                        case "idDrink":
                            cocktail.Id = property.Value;
                            break;

                        case "strDrink":
                            cocktail.Name = property.Value;
                            break;
                        case "strInstructions":
                            cocktail.Instructions = property.Value;
                            break;
                        case "strDrinkThumb":
                            cocktail.ImageURL = property.Value;
                            break;
                        default:
                            if (name.Contains("Ingredient"))
                            {
                                var ingredientName = property.Value.ToString();
                                if (!String.IsNullOrEmpty(ingredientName))
                                    ingredientList.Add(ingredientName);
                            }
                            break;
                    }

                }

                cocktail.Ingredients = ingredientList;
            }

            return Ok(cocktail);
        }

        private static bool IsValidJson(string resultString)  //This method validates the passed JSON String
        {
            resultString = resultString.Trim();


            if ((resultString.StartsWith("{") && resultString.EndsWith("}")) ||(resultString.StartsWith("[") && resultString.EndsWith("]"))) // Check if Json string is encapsulated correctly
            {
                try
                {
                    var obj = JToken.Parse(resultString);  //parsing the result string
                    return true;
                }
                catch (JsonReaderException jException)   //catch block for Json reader exception
                {
                    Console.WriteLine(jException.Message);
                    return false;
                }
                catch (Exception e)                  //catch block for all other exceptions
                {
                    Console.WriteLine(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

}