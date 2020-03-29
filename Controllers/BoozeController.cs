using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using api.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace api.Controllers
{
    [Route("api")]
    [ApiController]
    public class BoozeController : ControllerBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // We will use the public CocktailDB API as our backend
        // https://www.thecocktaildb.com/api.php
        //
        // Bonus points
        // - Speed improvements
        // - Unit Tests

        [HttpGet]
        [Route("search-ingredient/{ingredient}")]
        public async Task<IActionResult> GetIngredientSearch([FromRoute] string ingredient)
        {
            var cocktailList = new CocktailList();

            // TODO - Search the CocktailDB for cocktails with the ingredient given, and return the cocktails
            // https://www.thecocktaildb.com/api/json/v1/1/filter.php?i=Gin

            using (var response = await _httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/filter.php?i=" + ingredient))
            {
                var listMeta = new ListMeta();
                string apiResponse = await response.Content.ReadAsStringAsync();
                dynamic jsonResult = JsonConvert.DeserializeObject(apiResponse);
                listMeta.firstId = Int32.MaxValue;
                listMeta.lastId = 0;
                var totalIngredientcount = 0;
                List<Cocktail> cocktails = new List<Cocktail>();

                foreach (dynamic rc in jsonResult.drinks)
                {
                    // You will need to populate the cocktail details from
                    // https://www.thecocktaildb.com/api/json/v1/1/lookup.php?i=11007
                    using var response1 = await _httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/lookup.php?i=" + rc.idDrink);
                    string apiResponse1 = await response1.Content.ReadAsStringAsync();
                    dynamic jsonResult1 = JValue.Parse(apiResponse1);
                    var drink = jsonResult1.drinks[0];

                    Cocktail cocktail = new Cocktail();
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
                                    {
                                        ingredientList.Add(ingredientName); //add value here
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

                listMeta.count = cocktails.Count;
                listMeta.medianIngredientCount = (int)totalIngredientcount / listMeta.count;
                cocktailList.meta = listMeta;
                cocktailList.Cocktails = cocktails;
            }

            return Ok(cocktailList);
        }

        [HttpGet]
        [Route("random")]
        public async Task<IActionResult> GetRandom()
        {
            var cocktail = new Cocktail();
            // TODO - Go and get a random cocktail
            // https://www.thecocktaildb.com/api/json/v1/1/random.php

            using (var response = await _httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/random.php"))
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                dynamic dynamicObject = JsonConvert.DeserializeObject(apiResponse);
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
                                    ingredientList.Add(ingredientName); //add value here
                            }
                            break;
                    }

                }

                cocktail.Ingredients = ingredientList;
            }

            return Ok(cocktail);
        }
    }
}