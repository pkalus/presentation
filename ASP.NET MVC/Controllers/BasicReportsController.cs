using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Mvc;
using CCV.IILSC.Core;
using CCV.IILSC.Core.Infrastructure;
using CCV.IILSC.Web.Helpers;
using CCV.IILSC.Web.MaintenanceProcessors.Concrete;
using CCV.IILSC.Web.Processors.Concrete;
using CCV.IILSC.Web.ViewModels.BasicReports;
using CCV.IILSC.Web.ViewModels.Generic;
using PagedList;

using System.Globalization;
using CCV.IILSC.Web.Content.Resources;

namespace CCV.IILSC.Web.Controllers
{
    public class BasicReportsController : BaseController
    {
        /// <summary>
        /// The number of records displayed per page
        /// </summary>
        private int _itemsToPage;

        public BasicReportsController(IUnitOfWork unitOfWork) : base(unitOfWork)
        {

        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            // Set default itemsToPage constant
            _itemsToPage = CurrentUser.Zakaznik.CustomerConfiguration.ReportsConfiguration.ItemsToPage;
        }

        public async Task<ActionResult> WareWithoutInventories(string phrase, int pageIndex = 1, string orderBy = "wareName", bool isDescending = false)
        {
            int totalCount;
            var wares = (string.IsNullOrEmpty(phrase))
                    ? UnitOfWork.InventoryRepository.GetWaresWithoutInventories(CurrentUser.Zakaznik, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable()
                    : UnitOfWork.InventoryRepository.SearchWaresWithoutInventories(phrase, CurrentUser.Zakaznik, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var wareWithDates = wares.Select(ware => new WareBasicWithDates
            {
                Name = ware.Nazev,
                CatalogNumber = ware.KatalogoveCislo,
                CreatedDate = ware.DatumZalozeni,
                ModifiedDate = ware.DatumPosledniModifikace,
                Category = ware.Kategorie != null ? ware.Kategorie.Nazev : "Bez kategorie"
            }).ToList();

            var waresAsIPagedList = new StaticPagedList<WareBasicWithDates>(wareWithDates, pageIndex, _itemsToPage, totalCount);

            var model = new WareWithoutInventoriesPage
            {
                Phrase = phrase,
                Items = waresAsIPagedList,
                Order = new OrderResult { OrderBy = orderBy, IsDescending = isDescending }
            };

            return View(model);
        }

        public async Task<ActionResult> Incomes(DateTime? to, DateTime? from, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
            //CultureInfo culture = CultureInfo.InvariantCulture;
            //if (Request.QueryString != null) {

            //    string rawValue = Request.QueryString.GetValues("from").ToString();
            //}

            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperationsMoves =
                UnitOfWork.WarehouseOperationRepository.GetIncomesMovesFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var items = warehouseOperationsMoves.Select(y => new WarehouseOperationMove
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumPohybu,
                CreatedUser = y.Uzivatel.UserName,
                DocumentNumber = y.SkladovaOperace.CisloDokladu,
                Unit = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                //Warehouse = y.SkladovaOperace.SkladovaciObjekt.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev,
                warehouseOperationId = y.SkladovaOperaceID
            });



            var model = new BasicReportPage
            {
                Items = new StaticPagedList<WarehouseOperationMove>(items.ToList(), pageIndex, _itemsToPage, totalCount),
                Filter = new BasicReportFilter
                {
                    From = from.Value,
                    To = to.Value,
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList()
            };

            return View(model);
        }

        public async Task<ActionResult> Outcomes(DateTime? from, DateTime? to, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
           
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }


            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperationsMoves =
                UnitOfWork.WarehouseOperationRepository.GetOutcomesMovesFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var items = warehouseOperationsMoves.Select(y => new WarehouseOperationMove
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumPohybu,
                CreatedUser = y.Uzivatel.UserName,
                DocumentNumber = y.SkladovaOperace.CisloDokladu,
                Unit = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                //Warehouse = y.SkladovaOperace.SkladovaciObjekt.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev,
                warehouseOperationId = y.SkladovaOperaceID
            });



            var model = new BasicReportPage
            {
                Items = new StaticPagedList<WarehouseOperationMove>(items.ToList(), pageIndex, _itemsToPage, totalCount),
                //Filter = new BasicReportFilter { From = from.Value, To = to.Value, SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray() },
                Filter = new BasicReportFilter
                {
                    From = from.Value,
                    To = to.Value,
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList()
            };

            return View(model);
        }

        public async Task<ActionResult> WareMoves(DateTime? from, DateTime? to, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }


            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperationsMoves =
                UnitOfWork.WarehouseOperationRepository.GetWareMovesMovesFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var items = warehouseOperationsMoves.Select(y => new WarehouseOperationMove
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumPohybu,
                CreatedUser = y.Uzivatel.UserName,
                DocumentNumber = y.SkladovaOperace.CisloDokladu,
                Unit = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                //Warehouse = y.SkladovaOperace.SkladovaciObjekt.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev,
                warehouseOperationId = y.SkladovaOperaceID
            });



            var model = new BasicReportPage
            {
                Items = new StaticPagedList<WarehouseOperationMove>(items.ToList(), pageIndex, _itemsToPage, totalCount),
                // Filter = new BasicReportFilter { From = from.Value, To = to.Value, SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray() },
                Filter = new BasicReportFilter
                {
                    From = from.Value,
                    To = to.Value,
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList()
            };

            return View(model);
        }

        public async Task<ActionResult> Stocktakings(DateTime? from, DateTime? to, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }


            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperations =
                UnitOfWork.WarehouseOperationRepository.GetStocktakingsFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var items = warehouseOperations.Select(y => new StocktakingBasic
            {
                CreatedDate = y.DatumZalozeni,
                DocumentNumber = y.CisloDokladu,
                //Warehouse = y.SkladovaciObjekt.Nazev,
                State = y.AktualniStav.Nazev,
                WarehouseOperationId = y.SkladovaOperaceID
            });



            var model = new StocktakingReportPage
            {
                Items = new StaticPagedList<StocktakingBasic>(items.ToList(), pageIndex, _itemsToPage, totalCount),
                // Filter = new BasicReportFilter { From = from.Value, To = to.Value, SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray() },

                Filter = new BasicReportFilter
                {
                    From = from.Value,
                    To = to.Value,
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList()
            };

            return View(model);
        }

        public async Task<ActionResult> WareInStockQuantity(string phrase, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
            var totalCount = 0;

            var selectedWarehouses = warehouses == null
              ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
              : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();


            var waresInStockWithQuantity = (string.IsNullOrEmpty(phrase))
                ? UnitOfWork.InventoryRepository.GetWaresInStockWithQuantity(selectedWarehouses, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable()
                : UnitOfWork.InventoryRepository.SearchWaresInStockWithQuantity(phrase, selectedWarehouses, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var waresWithQuantity = waresInStockWithQuantity.Select(x => new WareInStockWithQuantity
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse
            }).ToList();

            var model = new WareInStockWithQuantityReportPage
            {
                Items = new StaticPagedList<WareInStockWithQuantity>(waresWithQuantity, pageIndex, _itemsToPage, totalCount),
                // Filter = new WareInStockReportFilter { SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray() },
                Filter = new WareInStockReportFilter
                {
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList(),
                Phrase = phrase
            };

            return View(model);
        }

        public async Task<ActionResult> WareInStockLocation(string phrase, int[] warehouses, int pageIndex = 1, string orderBy = "documentNumber", bool isDescending = false)
        {
            var totalCount = 0;

            var selectedWarehouses = warehouses == null
              ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
              : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var waresInStockWithLocation = (string.IsNullOrEmpty(phrase))
                                            ? UnitOfWork.InventoryRepository.GetWaresInStockWithLocation(selectedWarehouses, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable()
                                            : UnitOfWork.InventoryRepository.SearchWareWithLocation(phrase, selectedWarehouses, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending);
            var waresWithLocation = waresInStockWithLocation.Select(x => new WareInStockWithLocation
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse,
                Compartment = x.Compartment
            }).ToList();

            var model = new WareInStockWithLocationReportPage
            {
                Items = new StaticPagedList<WareInStockWithLocation>(waresWithLocation, pageIndex, _itemsToPage, totalCount),
                //Filter = new WareInStockReportFilter { SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray() },
                Filter = new WareInStockReportFilter
                {
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList(),
                Phrase = phrase
            };

            return View(model);
        }

        public async Task<ActionResult> Expiration(DateTime? from, DateTime? to, int[] warehouses, int pageIndex = 1, string orderBy = "expiration", bool isDescending = false) {

            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            //var inventories =
            //    UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
            //       selectedWarehouses, from.Value, to.Value, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();
            var inventories =
                UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
                   selectedWarehouses, from, to, out totalCount, (pageIndex - 1) * _itemsToPage, _itemsToPage, orderBy, isDescending).AsQueryable();

            var items = inventories.Select(y => new InventoriesExpiration
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumZalozeni,
                Expiration = y.Exspirace,
                Unit = y.SkladovaciJednotka.ZakladniJednotka.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev                
            });

            var model = new ExpirationReportPage
            {
                Items = new StaticPagedList<InventoriesExpiration>(items.ToList(), pageIndex, _itemsToPage, totalCount),
                Filter = new ReportFilterWithNulableDates
                {
                    From = from,
                    To = to,
                    SelectedWarehouses = selectedWarehouses.Select(x => x.SkladovaciObjektID).ToArray(),
                    Order = new OrderResult
                    {
                        OrderBy = orderBy,
                        IsDescending = isDescending
                    }
                },
                Warehouses = CurrentUser.Zakaznik.SkladovaciObjekty.Select(x => new WarehouseCheckbox { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID }).ToList()
            };

            return View(model);
        }


        #region EXPORT TO PDF
        public async Task<ActionResult> ExportIncomesToPdf(DateTime? from, DateTime? to, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetIncomesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount, -1 , -1, orderBy, isDescending);
            
            var items = warehouseOperationsMoves.Select(y => new WarehouseOperationMove
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumPohybu,
                CreatedUser = y.Uzivatel.UserName,
                DocumentNumber = y.SkladovaOperace.CisloDokladu,
                Unit = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateIncomesFile(items, selectedWarehouses, from.Value, to.Value);
            return File(file, MediaTypeNames.Application.Pdf, "Zaskladnìní (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").pdf");
        }
        public async Task<ActionResult> ExportOutcomesToPdf(DateTime? from, DateTime? to, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetOutcomesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount, -1, -1, orderBy, isDescending).ToList();
            
            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateOutcomesFile(warehouseOperationsMoves, selectedWarehouses, from.Value, to.Value);
            return File(file, MediaTypeNames.Application.Pdf, "Vyskladnìní (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").pdf");
        }
        public async Task<ActionResult> ExportWareMovesToPdf(DateTime? from, DateTime? to, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetWareMovesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount, -1, -1, orderBy, isDescending).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateWareMovesFile(warehouseOperationsMoves, selectedWarehouses, from.Value, to.Value);
            return File(file, MediaTypeNames.Application.Pdf, "Pøesuny (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").pdf");
        }
        public async Task<ActionResult> ExportWareInStockQuantityToPdf(string phrase, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var waresInStockWithQuantity = (string.IsNullOrEmpty(phrase))
                ? UnitOfWork.InventoryRepository.GetWaresInStockWithQuantity(selectedWarehouses, out totalCount, -1 , -1, orderBy, isDescending).AsQueryable()
                : UnitOfWork.InventoryRepository.SearchWaresInStockWithQuantity(phrase, selectedWarehouses, out totalCount, -1, -1, orderBy, isDescending).AsQueryable();

            var waresWithQuantity = waresInStockWithQuantity.Select(x => new WareInStockWithQuantity
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse,
                Phrase = phrase
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateWareInStockQuantityFile(waresWithQuantity, selectedWarehouses);
            return File(file, MediaTypeNames.Application.Pdf, "Zboží na skladì (množství).pdf");
        }
        public async Task<ActionResult> ExportWareInStockLocationToPdf(string phrase, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var waresInStockWithLocation = (string.IsNullOrEmpty(phrase))
                                            ? UnitOfWork.InventoryRepository.GetWaresInStockWithLocation(selectedWarehouses, out totalCount, -1, -1, orderBy, isDescending).AsQueryable()
                                            : UnitOfWork.InventoryRepository.SearchWareWithLocation(phrase, selectedWarehouses, out totalCount, -1, -1, orderBy, isDescending).AsQueryable();

            var waresWithLocation = waresInStockWithLocation.Select(x => new WareInStockWithLocation
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse,
                Compartment = x.Compartment
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateWareInStockLocationFile(waresWithLocation, selectedWarehouses);
            return File(file, MediaTypeNames.Application.Pdf, "Zboží na skladì (pozice).pdf");
        }
        public async Task<ActionResult> ExportStockTakingsToPdf(DateTime? @from, DateTime? to, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperations =
                UnitOfWork.WarehouseOperationRepository.GetStocktakingsFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount, -1, -1, orderBy, isDescending).AsQueryable();

            var items = warehouseOperations.Select(y => new StocktakingBasic
            {
                CreatedDate = y.DatumZalozeni,
                DocumentNumber = y.CisloDokladu,
                //Warehouse = y.SkladovaciObjekt.Nazev,
                State = y.AktualniStav.Nazev,
                WarehouseOperationId = y.SkladovaOperaceID
            });

            var exportGenerator = new PdfExportGenerator();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var file = exportGenerator.GenerateStockTakingsFile(items, from.Value, to.Value, selectedWarehouses);
            return File(file, MediaTypeNames.Application.Pdf, $"Inventrury ({from.Value.ToShortDateString()}-{to.Value.ToShortDateString()}).pdf");
        }

        public async Task<ActionResult> ExportExpirationToPdf(DateTime? from, DateTime? to, int[] warehouses, string orderBy = "documentNumber", bool isDescending = false) {

            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            //var inventories =
            //    UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
            //       selectedWarehouses, from.Value, to.Value, out totalCount, - 1, -1, orderBy, isDescending).AsQueryable();
            var inventories =
                UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
                    selectedWarehouses, from, to, out totalCount, -1, -1, orderBy, isDescending);

            var items = inventories.Select(y => new InventoriesExpiration
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumZalozeni,
                Expiration = y.Exspirace,
                Unit = y.SkladovaciJednotka.ZakladniJednotka.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev
            }).ToList();


            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var fromStr = from.HasValue ? from.Value.ToShortDateString() : BasicReportsResources.UnknownDateFileName;
            var toStr = to.HasValue ? to.Value.ToShortDateString() : BasicReportsResources.UnknownDateFileName;
            var fileName = $"Exspirace ({fromStr}-{toStr}).pdf";
            var exportGenerator = new PdfExportGenerator();
            var file = exportGenerator.GenerateExpirationFile(items, selectedWarehouses, from, to);
            //return File(file, MediaTypeNames.Application.Pdf, "Exspirace (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").pdf");
            return File(file, MediaTypeNames.Application.Pdf, fileName);
        }
        
        #endregion

        #region EXPORT TO CSV
        public async Task<ActionResult> ExportIncomesToCsv(DateTime? from, DateTime? to, int[] warehouses)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetIncomesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount);

            var items = warehouseOperationsMoves.Select(y => new WarehouseOperationMove
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumPohybu,
                CreatedUser = y.Uzivatel.UserName,
                DocumentNumber = y.SkladovaOperace.CisloDokladu,
                Unit = y.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev
            });

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateIncomesFile(items, selectedWarehouses, from.Value, to.Value);
            return File(file, "text/csv", "Zaskladnìní (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").csv");
        }
        public async Task<ActionResult> ExportOutcomesToCsv(DateTime? from, DateTime? to, int[] warehouses)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetOutcomesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateOutcomesFile(warehouseOperationsMoves, selectedWarehouses, from.Value, to.Value);
            return File(file, "text/csv", "Vyskladnìní (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").csv");
        }
        public async Task<ActionResult> ExportWareMovesToCsv(DateTime? from, DateTime? to, int[] warehouses)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var warehouseOperationsMoves =
               UnitOfWork.WarehouseOperationRepository.GetWareMovesMovesFromTo(
                  selectedWarehouses, from.Value, to.Value, out totalCount).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateOutcomesFile(warehouseOperationsMoves, selectedWarehouses, from.Value, to.Value);
            return File(file, "text/csv", "PøesunyZboží (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").csv");
        }
        public async Task<ActionResult> ExportExpirationToCsv(DateTime? from, DateTime? to, int[] warehouses)
        {
            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            //var inventories =
            //    UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
            //       selectedWarehouses, from.Value, to.Value, out totalCount, -1, -1).AsQueryable();
            var inventories =
                UnitOfWork.WarehouseOperationRepository.GetInventoriesExpirationFromTo(
                    selectedWarehouses, from, to, out totalCount, -1, -1).AsQueryable();

            var items = inventories.Select(y => new InventoriesExpiration
            {
                Compartment = y.Prihradka.NazevPrihradky,
                Name = y.SkladovaciJednotka.NazevSkladovaciJednotky,
                CatalogNumber = y.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                Quantity = y.MnozstviSkladovaciJednotky,
                CreatedDate = y.DatumZalozeni,
                Expiration = y.Exspirace,
                Unit = y.SkladovaciJednotka.ZakladniJednotka.Nazev,
                Warehouse = y.Prihradka.Regal.SkladovaciObjekt.Nazev
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var fromStr = from.HasValue ? from.Value.ToShortDateString() : BasicReportsResources.UnknownDateFileName;
            var toStr = to.HasValue ? to.Value.ToShortDateString() : BasicReportsResources.UnknownDateFileName;
            var fileName = $"Exspirace ({fromStr}-{toStr}).csv";
            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateExpirationFile(items, selectedWarehouses);
            //return File(file, "text/csv", "Exspirace (" + from.Value.ToShortDateString() + "-" + to.Value.ToShortDateString() + ").csv");
            return File(file, "text/csv", fileName);
        }

        public async Task<ActionResult> ExportWareInStockQuantityToCsv(string phrase, int[] warehouses)
        {
            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            var totalCount = 0;

            var waresInStockWithQuantity = (string.IsNullOrEmpty(phrase))
                 ? UnitOfWork.InventoryRepository.GetWaresInStockWithQuantity(selectedWarehouses, out totalCount, -1, -1, null, false).AsQueryable()
                 : UnitOfWork.InventoryRepository.SearchWaresInStockWithQuantity(phrase, selectedWarehouses, out totalCount, -1, -1, null, false).AsQueryable();

            var waresWithQuantity = waresInStockWithQuantity.Select(x => new WareInStockWithQuantity
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateWareInStockQuantityFile(waresWithQuantity, selectedWarehouses);
            return File(file, "text/csv", "Zboží na skladì (množství).csv");
        }
        public async Task<ActionResult> ExportWareInStockLocationToCsv(string phrase, int[] warehouses)
        {
            var selectedWarehouses = warehouses == null
               ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
               : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();
            int totalCount;

            var waresInStockWithLocation = (string.IsNullOrEmpty(phrase))
                                             ? UnitOfWork.InventoryRepository.GetWaresInStockWithLocation(selectedWarehouses, out totalCount, -1, -1, null, false).AsQueryable()
                                             : UnitOfWork.InventoryRepository.SearchWareWithLocation(phrase, selectedWarehouses, out totalCount, -1, -1, null, false).AsQueryable();

            var waresWithLocation = waresInStockWithLocation.Select(x => new WareInStockWithLocation
            {
                WareName = x.WareName,
                CatalogNumber = x.CatalogNumber,
                Category = x.Category ?? "Bez kategorie",
                Quantity = x.Quantity,
                Unit = x.Unit,
                Warehouse = x.Warehouse,
                Compartment = x.Compartment
            }).ToList();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var exportGenerator = new CsvExportGenerator();
            var file = exportGenerator.GenerateWareInStockLocationFile(waresWithLocation, selectedWarehouses);
            return File(file, "text/csv", "Zboží na skladì (pozice).csv");
        }
        public async Task<ActionResult> ExportStockTakingsToCsv(DateTime? @from, DateTime? to, int[] warehouses)
        {
            if (from == null)
            {
                from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (to == null)
            {
                to = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            var selectedWarehouses = warehouses == null
                ? CurrentUser.Zakaznik.SkladovaciObjekty.ToList()
                : CurrentUser.Zakaznik.SkladovaciObjekty.Where(x => warehouses.Contains(x.SkladovaciObjektID)).ToList();

            var totalCount = 0;
            var warehouseOperations =
                UnitOfWork.WarehouseOperationRepository.GetStocktakingsFromTo(
                   selectedWarehouses, from.Value, to.Value, out totalCount).AsQueryable();

            var items = warehouseOperations.Select(y => new StocktakingBasic
            {
                CreatedDate = y.DatumZalozeni,
                DocumentNumber = y.CisloDokladu,
                //Warehouse = y.SkladovaciObjekt.Nazev,
                State = y.AktualniStav.Nazev,
                WarehouseOperationId = y.SkladovaOperaceID
            });

            var exportGenerator = new CsvExportGenerator();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var file = exportGenerator.GenerateStockTakingsFile(items, selectedWarehouses);
            return File(file, "text/csv", $"Inventrury ({from.Value.ToShortDateString()}-{to.Value.ToShortDateString()}).csv");
        }
        #endregion

        public ActionResult ExportWareWithoutInventoriesToCsv(string phrase)
        {
            int totalCount;
            var wares = (string.IsNullOrEmpty(phrase))
                    ? UnitOfWork.InventoryRepository.GetWaresWithoutInventories(CurrentUser.Zakaznik, out totalCount).AsQueryable()
                    : UnitOfWork.InventoryRepository.SearchWaresWithoutInventories(phrase, CurrentUser.Zakaznik, out totalCount).AsQueryable();

            var wareWithDates = wares.Select(ware => new WareBasicWithDates
            {
                Name = ware.Nazev,
                CatalogNumber = ware.KatalogoveCislo,
                CreatedDate = ware.DatumZalozeni,
                ModifiedDate = ware.DatumPosledniModifikace,
                Category = ware.Kategorie != null ? ware.Kategorie.Nazev : "Bez kategorie"
            }).ToList();
            
            var exportGenerator = new CsvExportGenerator();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var file = exportGenerator.GenerateWareWithoutInventoriesFile(wareWithDates, phrase);
            //return File(result, "application/csv", "foo.csv");
            return File(file, "application/csv", $"Zboží bez zásob ({SystemTime.Now.ToShortDateString()}).csv");
        }

        public ActionResult ExportWareWithoutInventoriesToPdf(string phrase, string orderBy = "documentNumber", bool isDescending = false)
        {
            int totalCount;
            var wares = (string.IsNullOrEmpty(phrase))
                    ? UnitOfWork.InventoryRepository.GetWaresWithoutInventories(CurrentUser.Zakaznik, out totalCount, -1, -1, orderBy, isDescending ).AsQueryable()
                    : UnitOfWork.InventoryRepository.SearchWaresWithoutInventories(phrase, CurrentUser.Zakaznik, out totalCount).AsQueryable();

            var wareWithDates = wares.Select(ware => new WareBasicWithDates
            {
                Name = ware.Nazev,
                CatalogNumber = ware.KatalogoveCislo,
                CreatedDate = ware.DatumZalozeni,
                ModifiedDate = ware.DatumPosledniModifikace,
                Category = ware.Kategorie != null ? ware.Kategorie.Nazev : "Bez kategorie"
            }).ToList();

            var exportGenerator = new PdfExportGenerator();

            // Invert file download token for jQuery
            this.InvertFileDownloadToken();

            var file = exportGenerator.GenerateWareWithoutInventoriesFile(wareWithDates, phrase);
            return File(file, MediaTypeNames.Application.Pdf, $"Zboží bez zásob ({SystemTime.Now.ToShortDateString()}).pdf");
        }
    }
}