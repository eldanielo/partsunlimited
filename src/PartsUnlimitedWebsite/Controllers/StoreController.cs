// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PartsUnlimited.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PartsUnlimited.Controllers
{
    public class StoreController : Controller
    {
        private readonly IPartsUnlimitedContext _db;
        private readonly IMemoryCache _cache;

        public StoreController(IPartsUnlimitedContext context, IMemoryCache memoryCache)
        {
            _db = context;
            _cache = memoryCache;
        }

        //
        // GET: /Store/

        public IActionResult Index()
        {
            var category = _db.Categories.ToList();

            return View(category);
        }

        //
        // GET: /Store/Browse?category=Brakes

        public async Task<IActionResult> Browse(int categoryId)
        {
            //@TODO Products
            // Retrieve Category category and its Associated associated Products products from database

            // TODO [EF] Swap to native support for loading related data when available
            var categoryModel = _db.Categories.Single(g => g.CategoryId == categoryId);
            var ProductList = _db.Products.Where(a => a.CategoryId == categoryModel.CategoryId).ToList();


            var tasks = ProductList.Select(i => MakeProductRequest(i.Title));
            var results = await Task.WhenAll(tasks);
            for (int i = 0; i < ProductList.Count; i++) {
                ProductList[i].IsCertified = results[i].Item1;

                ProductList[i].TestScore = results[i].Item2;
            }
            return View(categoryModel);
        }



        static async Task<Tuple<bool, String>> MakeProductRequest(string queryString)
        {
            var client = new HttpClient();
            

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "d6ccd6589e1540d98b5b7423e46eedf9");

            var uri = "https://sgsapimgmt.azure-api.net/getProductCertification/manual/paths/invoke/products/" + queryString;

            var response = await client.GetAsync(uri);

            dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

           return new Tuple<bool, String>(Convert.ToBoolean(json.certifiedbysgs.ToString().Replace("null", "false")), json.testscore.ToString().Replace("null", ""));


        }




        public async Task<IActionResult> Details(int id)
        {


            Product productData;

            if (!_cache.TryGetValue(string.Format("product_{0}", id), out productData))
            {
                productData = _db.Products.Single(a => a.ProductId == id);
                productData.Category = _db.Categories.Single(g => g.CategoryId == productData.CategoryId);

                if (productData != null)
                {
                    _cache.Set(string.Format("product_{0}", id), productData, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                }                
            }
            Task<Tuple<bool,String>> porductCertified =  MakeProductRequest(productData.Title);


            Merchant merchant;

            if (!_cache.TryGetValue(string.Format("merchant_{0}", productData.MerchantId), out merchant))
            {
                merchant = _db.Merchants.Single(a => a.MerchantId == productData.MerchantId);

                if (merchant != null)
                {
                    _cache.Set(string.Format("product_{0}", productData.MerchantId), merchant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                }
            }


            Task<Tuple<bool, String>> merchantCertified =  MakeMerchantRequest(merchant.Name);

            string testscore = (await porductCertified).Item2;
            if (testscore != "") { 
                productData.TestScore = (Double.Parse(testscore) * 100).ToString() + "%"; ;

            }

            productData.IsCertified = (await porductCertified).Item1;
            merchant.CertLevel = (await merchantCertified).Item2;
            merchant.IsCertified = (await merchantCertified).Item1;

            return View(productData);
        }

         public async Task<Tuple<bool, String>> MakeMerchantRequest(string queryString)
        {
            var client = new HttpClient();


            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "d6ccd6589e1540d98b5b7423e46eedf9");

            var uri = "https://sgsapimgmt.azure-api.net/getMerchantCertification/manual/paths/invoke/accounts/" + queryString;



            var response = await client.GetAsync(uri);
            
            dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            
           
            return new Tuple<bool, String>(Convert.ToBoolean(json.certifiedbysgs.ToString().Replace("null", "false")), json.merchantlevel.ToString().Replace("null",""));

        }

        public async Task<IActionResult> Merchant(int id)
        {
            Merchant merchant;

            if (!_cache.TryGetValue(string.Format("merchant_{0}", id), out merchant))
            {
                merchant = _db.Merchants.Single(a => a.MerchantId == id);

                if (merchant != null)
                {
                    _cache.Set(string.Format("product_{0}", id), merchant, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
                }
            }
            var response = await MakeMerchantRequest(merchant.Name);
            merchant.CertLevel = response.Item2;
            merchant.IsCertified = response.Item1;
            return View(merchant);
        }
    }
}