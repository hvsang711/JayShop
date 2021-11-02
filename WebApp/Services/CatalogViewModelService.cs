using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Specifications;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Interfaces;
using WebApp.ViewModels;

namespace WebApp.Services
{
    public class CatalogViewModelService : ICatalogViewModelService
    {
        private readonly ILogger<CatalogViewModelService> _logger;
        private readonly IAsyncRepository<CatalogItem> _itemRepo;
        private readonly IAsyncRepository<CatalogBrand> _brandRepo;
        private readonly IAsyncRepository<CatalogType> _typeRepo;
        private readonly IUriComposer _uriComposer;
        public CatalogViewModelService(IAsyncRepository<CatalogItem> itemRepo,
            IAsyncRepository<CatalogBrand> brandRepo,
            IAsyncRepository<CatalogType> typeRepo,
            IUriComposer uriComposer,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CatalogViewModelService>();
            _itemRepo = itemRepo;
            _brandRepo = brandRepo;
            _typeRepo = typeRepo;
            _uriComposer = uriComposer;
        }

        public async Task<CatalogIndexViewModel> GetCatalogItems(int pageIndex, int itemsPage, int? brandId, int? typeId)
        {
            _logger.LogInformation("GetCatalogItems called.");

            var filterSpec = new CatalogFilterSpecification(brandId, typeId); 
            var filterPaginatedSpecification =
                 new CatalogFilterPaginatedSpecification(itemsPage * pageIndex, itemsPage, brandId, typeId);
            var itemsOnPage = await _itemRepo.ListAsync(filterPaginatedSpecification);
            var totalItems = await _itemRepo.CountAsync(filterSpec);

            var vm = new CatalogIndexViewModel()
            {
                CatalogItems = itemsOnPage.Select(i => new CatalogItemViewModel()
                {
                    Id=i.Id,
                    Name=i.Name,
                    PictureUri= _uriComposer.ComposePicUri(i.PictureUri),
                    Price = i.Price
                }).ToList(),
                Brands = (await GetBrands()).ToList(),
                Types = (await GetTypes()).ToList(),
                BrandFilterApplied = brandId ?? 0,
                TypesFilterApplied = typeId ?? 0,
                PaginationInfo = new PaginationInfoViewModel()
                {
                    ActualPage = pageIndex,
                    ItemsPerPage = itemsOnPage.Count,
                    TotalItems = totalItems,
                    TotalPages = int.Parse(Math.Ceiling(((decimal)totalItems / itemsPage)).ToString())
                }
            };

            vm.PaginationInfo.Next = (vm.PaginationInfo.ActualPage == vm.PaginationInfo.TotalPages - 1) ? "is-disabled" : "";
            vm.PaginationInfo.Previous = (vm.PaginationInfo.ActualPage == 0) ? "is-disabled" : "";

            return vm;
        }

        public async Task<IEnumerable<SelectListItem>> GetBrands()
        {
            _logger.LogInformation("GetBrands called.");
            var brands = await _brandRepo.ListAllAsync();

            var items = brands
                .Select(br => new SelectListItem() { Value = br.Id.ToString(), Text = br.Brand })
                .OrderBy(b => b.Text)
                .ToList();

            var allItem = new SelectListItem() { Value = null, Text = "All", Selected = true };
            items.Insert(0, allItem);
            return items;
        }
        public async Task<IEnumerable<SelectListItem>> GetTypes()
        {
            _logger.LogInformation("GetTypes called.");
            var types = await _typeRepo.ListAllAsync();

            var items = types
                .Select(type => new SelectListItem() { Value = type.Id.ToString(), Text = type.Type })
                .OrderBy(t => t.Text)
                .ToList();

            var allItem = new SelectListItem() { Value = null, Text = "All", Selected = true };
            items.Insert(0, allItem);

            return items;
        }
    }
}
