using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CCV.IILSC.Core;
using CCV.IILSC.Core.Infrastructure;
using CCV.IILSC.Core.Models.WarehouseOperationRepository;
using CCV.IILSC.Core.Repositories;
using CCV.IILSC.Core.Validators.DeleteValidators;
using CCV.IILSC.DataModel.Abstraktni;
using CCV.IILSC.DataModel.Entity.Administrace;
using CCV.IILSC.DataModel.Entity.PohybZbozi;
using CCV.IILSC.Web.Content.Resources;
using CCV.IILSC.Web.Mappers;
using CCV.IILSC.Web.ViewModels.WarehouseOperations;
using TrackingParameter = CCV.IILSC.Web.ViewModels.WarehouseOperations.TrackingParameter;

namespace CCV.IILSC.Web.Services.WarehouseOperations
{
    public class WarehouseOperationsService : IWarehouseOperationsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseOperationsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region INCOME
        public SkladovaOperace CreateIncome(WarehouseOperationInitialization model, Uzivatel currentUser)
        {
            if (model.GenerateDocumentNumber)
            {
                var specifiedDateTime = new DateTime(model.Date.Year, model.Date.Month, model.Date.Day, SystemTime.Now.Hour, SystemTime.Now.Minute, SystemTime.Now.Second);
                model.Date = specifiedDateTime;

                // Get number of warehouse operations created today
                var numberOfWarehouseOperationsToday = _unitOfWork.WarehouseOperationRepository.GetNumberOfWarehouseOperationsToday(
                        currentUser.Zakaznik, WarehouseOperationKind.Income, specifiedDateTime);

                // Generate new document number
                model.DocumentNumber =
                    _unitOfWork.WarehouseOperationRepository.GenerateDocumentNumber(currentUser.Zakaznik,
                        WarehouseOperationKind.Income, numberOfWarehouseOperationsToday);
            }

            // Create new income and return created warehouse operation
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.CreateWarehouseOperation(currentUser,
                     model.DocumentNumber,
                    model.Date, WarehouseOperationKind.Income, WarehouseOperationType.General,
                    WarehouseOperationState.Created, WarehouseOperationOrigin.Internal, model.GenerateDocumentNumber);

            return warehouseOperation;
        }

        /// <summary>
        /// Get view model for create income step2 (add ware move)
        /// </summary>
        /// <param name="warehouseOperationId">Warehouse operation id</param>
        /// <param name="generateDocumentNumber">Was document number generated?</param>
        /// <param name="user"></param>
        /// <returns></returns>
        public WarehouseOperationWareMoves CreateIncome_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user)
        {
            var warehouseOperation =
                  _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId);

            // Warehouse operation with given id dows not exist
            if (warehouseOperation == null) return null;

            var model = new WarehouseOperationWareMoves
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Date = warehouseOperation.DatumZalozeni,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,
                State = (WarehouseOperationState)warehouseOperation.AktualniStav.KodStavuOperace,
                WareMoves = warehouseOperation.PohybyZbozi?.Select(x => new WareMoveBase
                {
                    Compartment = x.Prihradka.NazevPrihradky,
                    Quantity = x.MnozstviSkladovaciJednotky,
                    WareMoveId = x.PohybZboziID,
                    WareName = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky
                }).ToList(),
                CreateWareMove = new WareMoveCreate
                {
                    CreatedDate = SystemTime.Now,
                    Quantity = null
                },
              
                GenerateDocumentNumber = generateDocumentNumber
            };
            return model;
        }
        /// <summary>
        /// Create new Ware move with tracking Parameters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public WareMoveCreate_TrackingParameters CreateIncome_AddWareMove(WareMoveCreate model, Uzivatel user)
        {
            var storageUnit = _unitOfWork.WareRepository.GetStorageUnitByName(model.StorageUnit, user.Zakaznik);

            if (storageUnit == null)
            {
                throw new Exception(ViewResources.WareWasNotFound);
            }

            model.CreatedDate = new DateTime(model.CreatedDate.Year, model.CreatedDate.Month, model.CreatedDate.Day, SystemTime.Now.Hour, SystemTime.Now.Minute, SystemTime.Now.Second);


            var modelWithTrackingParameters = new WareMoveCreate_TrackingParameters
            {
                Quantity = model.Quantity.Value,
                Compartment = model.Compartment,
                CreatedDate = model.CreatedDate,
                CompartmentId = model.CompartmentId.Value,
                StorageUnit = model.StorageUnit,
                CatalogNumber = storageUnit.Zbozi.KatalogoveCislo,
                StorageUnitId = model.StorageUnitId.Value,
                TrackingParameters = new List<ICollection<TrackingParameter>>(),
                WarehouseOperationId = model.WarehouseOperationId
            };

            // Create tracking paramters 2-dimensional array
            if (storageUnit.Zbozi.SledujeParametr(KodParametruSledovaniZbozi.SerioveCislo))
            {
                // If ware tracking serial number, we need so many lines as quantity
                for (var i = 0; i < model.Quantity; i++)
                {
                    modelWithTrackingParameters.TrackingParameters.Add(
                        storageUnit.Zbozi.ParametryZbozi.Select(x => new TrackingParameter
                        {
                            Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID),
                            Key = x.ParametrSledovaniZboziID,
                            Value = ""
                        }).ToList()
                        );
                }
            }
            else
            {
                // We need only one line of parameters
                if (storageUnit.Zbozi.ParametryZbozi.Count > 0)
                {
                    modelWithTrackingParameters.TrackingParameters.Add(
                        storageUnit.Zbozi.ParametryZbozi.Select(x => new TrackingParameter
                        {
                            Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID),
                            Key = x.ParametrSledovaniZboziID,
                            Value = ""
                        }).ToList());
                }
            }
            return modelWithTrackingParameters;
        }

        public WareMoveCreate_TrackingParameters CreateIncome_AddWareMoveWithTrackingParameters(
            WareMoveCreate_TrackingParameters model, Uzivatel user)
        {
            var warehouseOperation =
                _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(model.WarehouseOperationId);

            model.CreatedDate = new DateTime(model.CreatedDate.Year, model.CreatedDate.Month, model.CreatedDate.Day, SystemTime.Now.Hour, SystemTime.Now.Minute, SystemTime.Now.Second);

            var warehouseMoveData = new WarehouseMoveData
            {
                Compartment = _unitOfWork.WarehouseRepository.GetCompartmentById(model.CompartmentId),
                Date = model.CreatedDate,
                StorageUnitId = model.StorageUnitId,
                StorageUnitQuantity = model.Quantity
            };
            if (model.TrackingParameters != null && model.TrackingParameters.Any())
            {
                foreach (var trackingParametersSet in model.TrackingParameters)
                {
                    warehouseMoveData.TrackingParameters =
                        trackingParametersSet.Select(x => new KeyValuePair<int, string>(x.Key, x.Value))
                            .AsEnumerable()
                            .ToDictionary(y => y.Key, y => y.Value);

                }
            }
            // Add warehouse move in context
            _unitOfWork.WarehouseOperationRepository.CreateWareMoves(user.UzivatelID, warehouseOperation,
                        warehouseMoveData, WarehouseOperationKind.Income, warehouseMoveData.Date, false);

            // Saves all moves to db
            _unitOfWork.SaveChanges();

            return model;
        }
        #endregion

        #region OUTCOME
        public SkladovaOperace CreateOutcome(WarehouseOperationInitialization model, Uzivatel currentUser)
        {
            if (model.GenerateDocumentNumber)
            {
                // Get number of warehouse operations created today
                var numberOfWarehouseOperationsToday = _unitOfWork.WarehouseOperationRepository.GetNumberOfWarehouseOperationsToday(
                        currentUser.Zakaznik, WarehouseOperationKind.Outcome, SystemTime.Now);

                // Generate new document number
                model.DocumentNumber =
                    _unitOfWork.WarehouseOperationRepository.GenerateDocumentNumber(currentUser.Zakaznik,
                        WarehouseOperationKind.Outcome, numberOfWarehouseOperationsToday);
            }

            model.Date = new DateTime(model.Date.Year, model.Date.Month, model.Date.Day, SystemTime.Now.Hour, SystemTime.Now.Minute, SystemTime.Now.Second);

            // Create new outcome and return created warehouse operation
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.CreateWarehouseOperation(currentUser,
                     model.DocumentNumber,
                    model.Date, WarehouseOperationKind.Outcome, WarehouseOperationType.General,
                    WarehouseOperationState.Created, WarehouseOperationOrigin.Internal, model.GenerateDocumentNumber);

            return warehouseOperation;
        }

        public OutcomeWareMoves CreateOutcome_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user)
        {
            var warehouseOperation =
                  _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId);

            // Warehouse operation with given id dows not exist
            if (warehouseOperation == null) return null;

            var model = new OutcomeWareMoves
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Date = warehouseOperation.DatumZalozeni,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,
                WareMoves = warehouseOperation.PohybyZbozi?.Select(x => new WareMoveBase
                {
                    Compartment = x.Prihradka.NazevPrihradky,
                    Quantity = x.MnozstviSkladovaciJednotky,
                    WareMoveId = x.PohybZboziID,
                    WareName = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                    TrackingParameters = x.SledovaniPohybuZbozi.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = TrackingParameterMapper.GetNameById(y.ParametrSledovaniZboziID)
                    })
                }).ToList(),
                CreateOutcomeMove = new WareMoveCreate
                {
                    CreatedDate = SystemTime.Now,
                    Quantity = null
                },
                GenerateDocumentNumber = generateDocumentNumber,
                AvailableTrackingParameters = warehouseOperation.Uzivatel.Zakaznik.CustomerConfiguration.WareConfiguration.AvailableTrackingParameters.Select(y => new TrackingParameter
                {
                    Key = y.ParametrSledovaniZboziID,
                    Label = TrackingParameterMapper.GetNameById(y.ParametrSledovaniZboziID)
                })
            };
            return model;
        }

        public OutcomeMoveCreateWareInventory CreateOutcome_AddOutcomeWareMove(OutcomeMoveCreate model, Uzivatel user)
        {
            //var ware = _unitOfWork.WareRepository.GetWareById(model.WareId, user.Zakaznik);
            var ware = _unitOfWork.WareRepository.GetStorageUnitById(model.WareId).Zbozi;
            var warehouseOperation =
                _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(model.WarehouseOperationId);
            var selectedWarehouses =
                //user.Zakaznik.SkladovaciObjekty.Where(x => x.SkladovaciObjektID == warehouseOperation.SkladovaciObjektID);
                user.Zakaznik.SkladovaciObjekty;

            var inventories = _unitOfWork.InventoryRepository.GetWareInventories(ware, selectedWarehouses);
            var result = new OutcomeMoveCreateWareInventory
            {
                CreatedDate = model.CreatedDate,
                WareId = ware.ZboziID,
                Ware = ware.Nazev,
                CatalogNumber = ware.KatalogoveCislo,
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                InventoryItems = inventories.Select(x => new InventoryItem
                {
                    CatalogNumber = x.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                    StorageUnit = x.SkladovaciJednotka.NazevSkladovaciJednotky,
                    InventoryOfStorageUnitId = x.ZasobaSkladovaciJednotkyID,
                    Compartment = x.Prihradka.NazevPrihradky,
                    Quantity = x.MnozstviSkladovaciJednotky,
                    TrackingParameters = x?.SledovaniZasoby?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = TrackingParameterMapper.GetNameById(y.ParametrSledovaniZboziID)
                    })
                }).ToList(),
                AvailableTrackingParameters = user.Zakaznik.CustomerConfiguration.WareConfiguration.AvailableTrackingParameters?.Select(x => new TrackingParameter
                {
                    Key = x.ParametrSledovaniZboziID,
                    Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID)
                }).ToList()
            };
            return result;
        }

        public OutcomeMoveCreateWareInventory CreateOutcome_AddWareMoves(OutcomeWareMoveCreate_Inventory model, Uzivatel user)
        {
            var warehouseOperation =
                _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(model.WarehouseOperationId);

            foreach (var item in model.Items.Where(x => x.Quantity.HasValue))
            {
                var inventoryOfStorageUnit =
                    _unitOfWork.InventoryRepository.GetWareInventoryById(item.InventoryOfStorageUnitId);

                if (inventoryOfStorageUnit.MnozstviSkladovaciJednotky >= item.Quantity)
                {
                    _unitOfWork.WarehouseOperationRepository.CreateOutcomeWareMove(inventoryOfStorageUnit,
                        item.Quantity.Value, warehouseOperation, DateTime.Now);
                }

                // Add warehouse move in context
                //_unitOfWork.WarehouseOperationRepository.CreateOutcomeMoves(warehouseOperation,
                //ZasobaSkladovaciJednotky tmpUserSupply, model.CreatedDate, true);



            }

            var returnModel = new OutcomeMoveCreateWareInventory
            {
                WarehouseOperationId = model.WarehouseOperationId
            };
            return returnModel;
        }
        #endregion

        #region MOVE

        public SkladovaOperace CreateMove(WarehouseOperationInitialization model, Uzivatel currentUser)
        {
            if (model.GenerateDocumentNumber)
            {
                // Get number of warehouse operations created today
                var numberOfWarehouseOperationsToday = _unitOfWork.WarehouseOperationRepository.GetNumberOfWarehouseOperationsToday(
                        currentUser.Zakaznik, WarehouseOperationKind.Move, SystemTime.Now);

                // Generate new document number
                model.DocumentNumber =
                    _unitOfWork.WarehouseOperationRepository.GenerateDocumentNumber(currentUser.Zakaznik,
                        WarehouseOperationKind.Move, numberOfWarehouseOperationsToday);
            }

            // Create new outcome and return created warehouse operation
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.CreateWarehouseOperation(currentUser,
                     model.DocumentNumber,
                    model.Date, WarehouseOperationKind.Move, WarehouseOperationType.General,
                    WarehouseOperationState.Created, WarehouseOperationOrigin.Internal,
                    autoGeneratedDocumentNumber: model.GenerateDocumentNumber);

            return warehouseOperation;
        }

        public MoveWareMoves CreateMove_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user)
        {
            var warehouseOperation =
                 _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId);

            // Warehouse operation with given id dows not exist
            if (warehouseOperation == null) return null;

            var model = new MoveWareMoves
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Date = warehouseOperation.DatumZalozeni,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,
                WareMoves = warehouseOperation.PohybyZbozi?.Select(x => new WareMoveBase
                {
                    Compartment = x.Prihradka.NazevPrihradky,
                    Quantity = x.MnozstviSkladovaciJednotky,
                    WareMoveId = x.PohybZboziID,
                    WareName = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky
                }).ToList(),
                CreateWareMove = new WareMoveCreateMove()
                {
                    CreatedDate = SystemTime.Now,
                    Quantity = null
                },
                GenerateDocumentNumber = generateDocumentNumber
            };
            return model;
        }

        #endregion

        public bool ValidateDocumentNumber(string documentNumber, int customerId)
        {
            return _unitOfWork.WarehouseOperationRepository.ValidateDocumentNumber(documentNumber, customerId);
        }
        /// <summary>
        /// Show page with Detail of Warehouse operation
        /// </summary>
        /// <param name="warehouseOperationId"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public WarehouseOperationDetailPage Detail(int warehouseOperationId, int customerId)
        {
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId, customerId);
            var validator = new DeleteWarehouseOperationBoolValidator(_unitOfWork);
            return new WarehouseOperationDetailPage
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperace
                CreatedDate = warehouseOperation.DatumZalozeni,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,//WarehouseOperationKindMapper.GetNameById((WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace),
                CurrentState = WarehouseOperationStateMapper.GetNameById((WarehouseOperationState)warehouseOperation.AktualniStav.KodStavuOperace),
                User = warehouseOperation.Uzivatel.UserName,
                WareMoves = warehouseOperation.PohybyZbozi?.GroupBy(y => new { y.ZasobaSkladovaciJednotky.SkladovaciJednotkaID, y.SledovaniString }).Select(x => new WareMoveWithTrackingParameters
                {
                    StorageUnit = x.FirstOrDefault()?.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                    CatalogNumber = x.FirstOrDefault()?.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                    Quantity = Math.Abs(x.Sum(y => y.MnozstviSkladovaciJednotky)),
                    BasicUnit = x.FirstOrDefault()?.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                    CreatedDate = x.FirstOrDefault()?.DatumPohybu ?? new DateTime(),
                    CreatedUser = x.FirstOrDefault().Uzivatel.UserName,
                    Compartment = x.FirstOrDefault()?.Prihradka.NazevPrihradky,
                    
                    TrackingParameters = x.FirstOrDefault()?.SledovaniPohybuZbozi?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = y.ParametrSledovaniZbozi.Nazev
                    })
                }),
                AvailableTrackingParameters = warehouseOperation.Uzivatel.Zakaznik.CustomerConfiguration.WareConfiguration.AvailableTrackingParameters.Select(x => new TrackingParameter
                {
                    Key = x.ParametrSledovaniZboziID,
                    Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID)
                }).ToList(),
                DeleteAllowed = validator.Validate(warehouseOperation.SkladovaOperaceID)
            };
        }

        public WarehouseOperationEditPage DetailForEdit(int warehouseOperationId, int customerId)
        {
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId, customerId);
            var validator = new DeleteWarehouseOperationBoolValidator(_unitOfWork);
            var returnModel = new WarehouseOperationEditPage
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                //WarehouseName = warehouseOperation.SkladovaciObjekt.Nazev,
                CreatedDate = warehouseOperation.DatumZalozeni,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,//WarehouseOperationKindMapper.GetNameById((WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace),
                CurrentState = WarehouseOperationStateMapper.GetNameById((WarehouseOperationState)warehouseOperation.AktualniStav.KodStavuOperace),
                CurrentStateId = warehouseOperation.AktualniStavID,
                User = warehouseOperation.Uzivatel.UserName,
                AvailableStates = _unitOfWork.WarehouseOperationRepository.AvailableWarehouseOperationStates.Select(y => new ViewModels.WarehouseOperationChange.WarehouseOperationState
                {
                    Key = y.StavOperaceID,
                    Value = WarehouseOperationStateMapper.GetNameById((WarehouseOperationState)y.KodStavuOperace),

                }).ToList(),
                WareMoves = warehouseOperation.PohybyZbozi?.Select(x => new WareMoveWithTrackingParameters
                {
                    WareMoveId = x.PohybZboziID,
                    StorageUnit = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                    CatalogNumber = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                    Quantity = x.MnozstviSkladovaciJednotky,
                    BasicUnit = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                    CreatedUser = x.Uzivatel.UserName,
                    CreatedDate = (DateTime)x?.DatumPohybu,
                    Compartment = x.Prihradka.NazevPrihradky,
                    TrackingParameters = x.SledovaniPohybuZbozi?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = y.ParametrSledovaniZbozi.Nazev
                    }),
                    EditAllowed = !(x.ZasobaSkladovaciJednotky.PohybZbozi.Any(y => y.SkladovaOperace.DruhOperace.KodDruhuOperace == (int)WarehouseOperationKind.Outcome || y.SkladovaOperace.DruhOperace.KodDruhuOperace == (int)WarehouseOperationKind.Move)
                        && (int)x.SkladovaOperace.KodDruhu == (int)WarehouseOperationKind.Income),
                    WareAvailableTrackingParameters = x.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.ParametryZbozi?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Label = TrackingParameterMapper.GetNameById(y.ParametrSledovaniZboziID)
                    }).ToList(),
                }),
                AvailableTrackingParameters = warehouseOperation.Uzivatel.Zakaznik.CustomerConfiguration.WareConfiguration.AvailableTrackingParameters.Select(x => new TrackingParameter
                {
                    Key = x.ParametrSledovaniZboziID,
                    Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID)
                }).ToList(),
                DeleteAllowed = validator.Validate(warehouseOperation.SkladovaOperaceID)
            };
            return returnModel;
        }

        public WareTransferDetailPage WareTransferDetail(int warehouseOperationId, int customerId)
        {
            var warehouseOperation = _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationById(warehouseOperationId, customerId);
            var validator = new DeleteWarehouseOperationBoolValidator(_unitOfWork);
            return new WareTransferDetailPage
            {
                WarehouseOperationId = warehouseOperation.SkladovaOperaceID,
                //WarehouseName = warehouseOperation.SkladovaciObjekt.Nazev,
                CreatedDate = warehouseOperation.DatumZalozeni,
                DocumentNumber = warehouseOperation.CisloDokladu,
                Kind = (WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace,//WarehouseOperationKindMapper.GetNameById((WarehouseOperationKind)warehouseOperation.DruhOperace.KodDruhuOperace),
                CurrentState = WarehouseOperationStateMapper.GetNameById((WarehouseOperationState)warehouseOperation.AktualniStav.KodStavuOperace),
                User = warehouseOperation.Uzivatel.UserName,
                IncomedWareMoves = warehouseOperation.PohybyZbozi?.Where(x => x.MnozstviSkladovaciJednotky > 0)?.Select(x => new WareMoveWithTrackingParameters
                {
                    StorageUnit = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                    CatalogNumber = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                    Quantity = Math.Abs(x.MnozstviSkladovaciJednotky),
                    BasicUnit = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                    CreatedDate = x?.DatumPohybu ?? new DateTime(),
                    Compartment = x?.Prihradka.NazevPrihradky,
                    TrackingParameters = x?.SledovaniPohybuZbozi?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = y.ParametrSledovaniZbozi.Nazev
                    })
                }),
                OutcomedWareMoves = warehouseOperation.PohybyZbozi?.Where(x => x.MnozstviSkladovaciJednotky < 0).Select(x => new WareMoveWithTrackingParameters
                {
                    StorageUnit = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.NazevSkladovaciJednotky,
                    CatalogNumber = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.Zbozi.KatalogoveCislo,
                    Quantity = Math.Abs(x.MnozstviSkladovaciJednotky),
                    BasicUnit = x?.ZasobaSkladovaciJednotky.SkladovaciJednotka.ZakladniJednotka.Nazev,
                    CreatedDate = x?.DatumPohybu ?? new DateTime(),
                    Compartment = x?.Prihradka.NazevPrihradky,
                    TrackingParameters = x?.SledovaniPohybuZbozi?.Select(y => new TrackingParameter
                    {
                        Key = y.ParametrSledovaniZboziID,
                        Value = y.HodnotaParametru,
                        Label = y.ParametrSledovaniZbozi.Nazev
                    })
                }),
                AvailableTrackingParameters = warehouseOperation.Uzivatel.Zakaznik.CustomerConfiguration.WareConfiguration.AvailableTrackingParameters.Select(x => new TrackingParameter
                {
                    Key = x.ParametrSledovaniZboziID,
                    Label = TrackingParameterMapper.GetNameById(x.ParametrSledovaniZboziID)
                }).ToList(),
                DeleteAllowed = validator.Validate(warehouseOperation.SkladovaOperaceID)
            };
        }


        public IEnumerable<WarehouseOperationBase> GetLatestWarehouseOperations(int customerId, int operationsCount)
        {
            return _unitOfWork.WarehouseOperationRepository.GetLastWarehouseOperations(customerId, operationsCount)
                   .Select(x => new WarehouseOperationBase
                   {
                       WarehouseOperationKind = (WarehouseOperationKind)x.DruhOperace.KodDruhuOperace,
                       DocumentNumber = x.CisloDokladu,
                       WarehouseOperationId = x.SkladovaOperaceID
                   }).ToList();
        }

        public IEnumerable<WarehouseOperationListItem> WarehouseOperationsInProgress(WarehouseOperationKind warehouseOperationKind, int customerId,
            out int totalCount, int startIndex, int pageSize)
        {
            var warehouseOperations = _unitOfWork.WarehouseOperationRepository.GetWarehouseOperationsInProgress(warehouseOperationKind, customerId, out totalCount, startIndex, pageSize);
            return warehouseOperations.Select(x => new WarehouseOperationListItem
            {
                CreatedDate = x.DatumZalozeni,
                WarehouseOperationId = x.SkladovaOperaceID,
                DocumentNumber = x.CisloDokladu,
                //Warehouse = x.SkladovaciObjekt.Nazev,
                Kind = warehouseOperationKind,
                CreatedUser = x.Uzivatel.UserName
            });
        }
    }
}