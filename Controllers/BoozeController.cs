using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using api.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace api.Controllers
{
    [Route("api")]
    [ApiController]
    public class BoozeController : ControllerBase
    {
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
            var listMeta = new ListMeta();
            // TODO - Search the CocktailDB for cocktails with the ingredient given, and return the cocktails
            // https://www.thecocktaildb.com/api/json/v1/1/filter.php?i=Gin
            // You will need to populate the cocktail details from
            // https://www.thecocktaildb.com/api/json/v1/1/lookup.php?i=11007
            // The calculate and fill in the meta object


            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/filter.php?i=" + ingredient))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    dynamic jsonResult = JValue.Parse(apiResponse);
                    //returnedCockTailList = JsonConvert.DeserializeObject<List<ReturnCocktail>>(apiResponse);
                    listMeta.firstId = Int32.MaxValue;
                    listMeta.lastId = 0;
                    var totalIngredientcount = 0;
                    List<Cocktail> cocktails = new List<Cocktail>();
                    foreach (dynamic rc in jsonResult.drinks)
                    {
                        using (var response1 = await httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/lookup.php?i=" + rc.idDrink))
                        {
                            string apiResponse1 = await response1.Content.ReadAsStringAsync();
                            dynamic jsonResult1 = JValue.Parse(apiResponse1);
                            var drink = jsonResult1.drinks[0];
                           
                            Cocktail cocktailCreated = new Cocktail();
                            cocktailCreated.Id = drink.idDrink;
                            cocktailCreated.Name = drink.strDrink;
                            cocktailCreated.Instructions = drink.strInstructions;
                            var ingredientList = new List<string>();
                            var x = TypeDescriptor.GetProperties(drink);
                            //foreach (JProperty prop in drink.Properties)
                            //{
                            //    if (prop.Name.Contains("Ingredient"))
                            //    {
                            //        ingredientList.Add(drink.prop);
                            //    }
                            //}



                            //dynamic d = drink;
                            //object o = d;
                            //string[] propertyNames = o.GetType().GetProperties().Select(p => p.Name).ToArray();
                            //foreach (var prop in propertyNames)
                            //{
                            //    object propValue = o.GetType().GetProperty(prop).GetValue(o, null);
                            //}


                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(drink))
                            {
                                var propertyName = prop.Name;
                                if (propertyName.Contains("Ingredient"))
                                {

                                    ingredientList.Add(drink.propertyName); //add value here
                                    //Console.WriteLine(drink);
                                    //var propertyInfo = drink.GetType().GetProperty(propertyName);
                                    //var value = propertyInfo.GetValue(drink, null);
                                }
                                totalIngredientcount++;
                            }




                            cocktailCreated.Ingredients = ingredientList;
                            cocktailCreated.ImageURL = drink.strDrinkThumb;
                            if (listMeta.firstId < cocktailCreated.Id)
                            {
                                listMeta.firstId = cocktailCreated.Id;
                            }
                            if (listMeta.lastId > cocktailCreated.Id)
                            {
                                listMeta.lastId = cocktailCreated.Id;
                            }
                            cocktails.Add(cocktailCreated);

                        }
                    }



                    listMeta.count = cocktails.Count;
                    listMeta.medianIngredientCount = (int)totalIngredientcount / listMeta.count;
                    cocktailList.meta = listMeta;
                    cocktailList.Cocktails = cocktails;


                }
            }



            return Ok(cocktailList);
        }

        [HttpGet]
        [Route("random")]
        public async Task<IActionResult> GetRandom()
        {
            var cocktail = new Cocktail();
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://www.thecocktaildb.com/api/json/v1/1/random.php"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    dynamic dynamicObject = JValue.Parse(apiResponse);
                    var drink = dynamicObject.drinks[0];

            
                    cocktail.Id = drink.idDrink;
                    cocktail.Name = drink.strDrink;
                    cocktail.Instructions = drink.strInstructions;
                    cocktail.ImageURL = drink.strDrinkThumb;
                    var ingredientList = new List<string>();

                    var x = TypeDescriptor.GetProperties(drink);


                    foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(drink))
                    {
                        var propertyName = prop.Name;
                        if (propertyName.Contains("Ingredient"))
                        {

                            ingredientList.Add(drink.propertyName); //add value here
                                                                    //Console.WriteLine(drink);
                                                                    //var propertyInfo = drink.GetType().GetProperty(propertyName);
                                                                    //var value = propertyInfo.GetValue(drink, null);
                        }
                    }

                    cocktail.Ingredients = ingredientList;
                    // TODO - Go and get a random cocktail
                    // https://www.thecocktaildb.com/api/json/v1/1/random.php
                    return Ok(cocktail);
                }
            }
        }
    }
}
public class MemberHelper<T>
{
    public string GetName<U>(Expression<Func<T, U>> expression)
    {
        MemberExpression memberExpression = expression.Body as MemberExpression;
        if (memberExpression != null)
            return memberExpression.Member.Name;

        throw new InvalidOperationException("Member expression expected");
    }
}