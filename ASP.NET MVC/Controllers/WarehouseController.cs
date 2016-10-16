using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using CCV.IILSC.Core;
using CCV.IILSC.Web.Helpers;
using CCV.IILSC.Web.Services;
using CCV.IILSC.Web.ViewModels.Home;
using CCV.IILSC.Web.ViewModels.Warehouse;
using WebGrease.Css.Extensions;

namespace CCV.IILSC.Web.Controllers
{
    public class WarehouseController : BaseController
    {
        // Service for all the work with warehouse operations
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IUnitOfWork unitOfWork,
            IWarehouseService warehouseOperationsService) : base(unitOfWork)
        {
            _warehouseService = warehouseOperationsService;
        }

        public async Task<ActionResult> Index(int? warehouseId)
        {
            var customerWarehouses =
                CurrentUser.Zakaznik.SkladovaciObjekty.Select(
                    x => new Warehouse { Name = x.Nazev, WarehouseId = x.SkladovaciObjektID });

            IEnumerable<Warehouse> availableWarehouses = customerWarehouses as Warehouse[]
                ?? customerWarehouses.ToArray();
            var selectedWarehouse = availableWarehouses.FirstOrDefault(x => x.WarehouseId == warehouseId)
                ?? availableWarehouses.First();

            var model = new IndexViewModel
            {
                SelectedWarehouse = selectedWarehouse,
                AvailableWarehouses = availableWarehouses
            };
            return View(model);
        }

        public async Task<PartialViewResult> UpdateFloorplan(int? warehouseId)
        {
            if (!warehouseId.HasValue) return PartialView("_UpdateFloorplan", null);

            var warehouse = _warehouseService.GetWarehouseFloorplan(warehouseId.Value);

            return warehouse != null
                ? PartialView("_UpdateFloorplan", warehouse)
                : PartialView("_UpdateFloorplan", null);
        }

        [HttpPost]
        public async Task<PartialViewResult> UpdateFloorplan(UpdateFloorplanViewModel model)
        {
            // Validate image
            var validImageTypes = new string[]
                {
                    "image/gif",
                    "image/jpeg",
                    "image/pjpeg",
                    "image/png"
                };
            if (model.Image == null || model.Image.ContentLength == 0)
            {
                ModelState.AddModelError("Image", "Vložte obrázek");
            }
            else if (!validImageTypes.Contains(model.Image.ContentType))
            {
                ModelState.AddModelError("Image", "Požadovaný typ obrázku je GIF, JPG or PNG.");
            }
            else if (model.Image != null && !validImageTypes.Contains(model.Image.ContentType))
            {
                ModelState.AddModelError("Image", "Požadovaný typ obrázku je GIF, JPG or PNG.");
            }
            if (!ModelState.IsValid)
            {
                return PartialView("_UpdateFloorplan", model);
            }

            var warehouse = _warehouseService.UpdateFloorplan(model);
            if (warehouse != null)
            {
                TempData["partialSuccess"] = "Pozadí bylo zmìnìno.";
            }

            return PartialView("_UpdateFloorplan", model);

        }

        public ActionResult GetWarehouseModal(int warehouseId, string targetElementId)
        {
            var warehouse = UnitOfWork.WarehouseRepository.GetWarehouseById(warehouseId);
            var model = new WarehouseWithRacks
            {
                Racks = warehouse.Regal,
                Warehouse = warehouse.Nazev,
                SelectedWarehouseId = warehouseId,
                TargetElementId = targetElementId,
                ShowCompartments = CurrentUser.Zakaznik.CustomerConfiguration.WarehousesConfiguration?.FirstOrDefault(x => x.WarehouseId == warehouseId)?.ShowCompartmentsInFloorplan ?? false
            };

            return View("_GetWarehouseModal", model);
        }


        public ActionResult GetWarehouseModalAsString(int warehouseId, string targetElementId)
        {
            var warehouse = UnitOfWork.WarehouseRepository.GetWarehouseById(warehouseId);
            var model = new WarehouseWithRacks
            {
                Racks = warehouse.Regal,
                Warehouse = warehouse.Nazev,
                SelectedWarehouseId = warehouseId,
                TargetElementId = targetElementId,
                ShowCompartments = CurrentUser.Zakaznik.CustomerConfiguration.WarehousesConfiguration?.FirstOrDefault(x => x.WarehouseId == warehouseId)?.ShowCompartmentsInFloorplan ?? false
            };

            return View("_GetWarehouseModalAsString", model);
        }


        public ActionResult SelectCompartmentModal(string targetElementId)
        {
            var warehouses = UnitOfWork.WarehouseRepository.GetWarehousesByCustomer(CurrentUser.Zakaznik);
            var model = warehouses.Select(x => new WarehouseWithRacks
            {
                Racks = x.Regal,
                Warehouse = x.Nazev,
                TargetElementId = targetElementId,
                SelectedWarehouseId = x.SkladovaciObjektID,
                ShowCompartments =
                    CurrentUser.Zakaznik.CustomerConfiguration.WarehousesConfiguration?.FirstOrDefault(
                        y => y.WarehouseId == x.SkladovaciObjektID)?
                        .ShowCompartmentsInFloorplan ?? false
            });

            return View("_SelectCompartmentModal", model);
        }

        public ActionResult GetRackModal(int rackId)
        {
            var rack = UnitOfWork.WarehouseRepository.GetRackById(rackId);

            var canvasWidth = 1000;
            var canvasHeight = 400;
            var compartmentPadding = 4;

            var compartmentWidth = ((canvasWidth - ((rack.PocetSloupcu - 1) * compartmentPadding)) / (rack.PocetSloupcu));
            var compartmentHeight = ((canvasHeight - ((rack.PocetPater - 1) * compartmentPadding)) / (rack.PocetPater));
            var compartments = rack.Prihradka.Select(compartment => new CompartmentBasicWithCoordinates
            {
                X = compartment.CompartmentCoordinates.X*compartmentWidth + compartment.CompartmentCoordinates.X*compartmentPadding,
                //Y = compartment.CompartmentCoordinates.Y*compartmentHeight + compartment.CompartmentCoordinates.Y*compartmentPadding,
                Y = (rack.PocetPater - 1 - compartment.CompartmentCoordinates.Y) * compartmentHeight + (rack.PocetPater - 1 - compartment.CompartmentCoordinates.Y) * compartmentPadding,
                Width = compartmentWidth,
                Height = compartmentHeight,
                CompartmentId = compartment.PrihradkaID,
                Name = compartment.NazevPrihradky
            }).ToList();
            var model = new RackWithComparments
            {
                Name = rack.NazevRegalu,
                RackId = rackId,
                Compartments = compartments,
                IsInitiallyStock = rack.RegalNaplnen
            };
            return View("_GetRackModal", model);
        }
        [AcceptVerbs(HttpVerbs.Get)]
        public JsonResult GetRackModalAsString(int rackId, string targetElementId)
        {
            var rack = UnitOfWork.WarehouseRepository.GetRackById(rackId);

            var canvasWidth = 1000;
            var canvasHeight = 400;
            var compartmentPadding = 4;

            //var compartmentWidth = (canvasWidth / (rack.PocetSloupcu));
            //var compartmentHeight = (canvasHeight / (rack.PocetPater));
            var compartmentWidth = ((canvasWidth - ((rack.PocetSloupcu - 1) * compartmentPadding)) / (rack.PocetSloupcu));
            var compartmentHeight = ((canvasHeight - ((rack.PocetPater - 1) * compartmentPadding)) / (rack.PocetPater));
            var compartments = rack.Prihradka.Select(compartment => new CompartmentBasicWithCoordinates
            {
                X = compartment.CompartmentCoordinates.X * compartmentWidth + compartment.CompartmentCoordinates.X * compartmentPadding,
                //Y = compartment.CompartmentCoordinates.Y * compartmentHeight + compartment.CompartmentCoordinates.Y * compartmentPadding,
                Y = (rack.PocetPater - 1 - compartment.CompartmentCoordinates.Y) * compartmentHeight + (rack.PocetPater - 1 - compartment.CompartmentCoordinates.Y) * compartmentPadding,
                Width = compartmentWidth,
                Height = compartmentHeight,
                CompartmentId = compartment.PrihradkaID,
                Name = compartment.NazevPrihradky
            }).ToList();
            
            var model = new RackWithComparments
            {
                Name = rack.NazevRegalu,
                RackId = rackId,
                Compartments = compartments,
                TargetElementId = targetElementId,
                IsInitiallyStock = rack.RegalNaplnen
            };
            return Json(new
            {
                Html = this.RenderViewToString("_GetRackModalAsString", model)
            }, JsonRequestBehavior.AllowGet);
        }
    }
}