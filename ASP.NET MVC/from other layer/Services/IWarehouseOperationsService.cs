using System.Collections.Generic;
using CCV.IILSC.Core.Repositories;
using CCV.IILSC.DataModel.Entity.Administrace;
using CCV.IILSC.DataModel.Entity.PohybZbozi;
using CCV.IILSC.DataModel.Entity.RizeniSkladu;
using CCV.IILSC.Web.ViewModels.WarehouseOperations;

namespace CCV.IILSC.Web.Services
{
    public interface IWarehouseOperationsService
    {
        #region INCOME
        SkladovaOperace CreateIncome(WarehouseOperationInitialization model, Uzivatel currentUser);
        WarehouseOperationWareMoves CreateIncome_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user);
        WareMoveCreate_TrackingParameters CreateIncome_AddWareMove(WareMoveCreate model, Uzivatel user);
        WareMoveCreate_TrackingParameters CreateIncome_AddWareMoveWithTrackingParameters(WareMoveCreate_TrackingParameters model, Uzivatel user);
        #endregion
      
        #region OUTCOME
        SkladovaOperace CreateOutcome(WarehouseOperationInitialization model, Uzivatel currentUser);
        OutcomeWareMoves CreateOutcome_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user);
        OutcomeMoveCreateWareInventory CreateOutcome_AddOutcomeWareMove(OutcomeMoveCreate model, Uzivatel user);
        OutcomeMoveCreateWareInventory CreateOutcome_AddWareMoves(OutcomeWareMoveCreate_Inventory model, Uzivatel user);
        #endregion

        #region MOVE
        SkladovaOperace CreateMove(WarehouseOperationInitialization model, Uzivatel currentUser);
        MoveWareMoves CreateMove_WareMoves(int warehouseOperationId, bool generateDocumentNumber, Uzivatel user);
        #endregion

        bool ValidateDocumentNumber(string documentNumber, int customerId);

        WarehouseOperationDetailPage Detail(int warehouseOperationId, int customerId);
        WarehouseOperationEditPage DetailForEdit(int warehouseOperationId, int customerId);
        WareTransferDetailPage WareTransferDetail(int warehouseOperationId, int customerId);

        IEnumerable<WarehouseOperationBase> GetLatestWarehouseOperations(int customerId, int operationsCount);
        IEnumerable<WarehouseOperationListItem> WarehouseOperationsInProgress(WarehouseOperationKind warehouseOperationKind, int zakaznikId, out int totalCount, int startIndex, int pageSize);       
    }
}