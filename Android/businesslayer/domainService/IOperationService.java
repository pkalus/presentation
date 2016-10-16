package cz.ccv.instantlogis.instantclient.businesslayer.domainService;

import java.util.List;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.Entities.WarehouseOperationListInfo;
import cz.ccv.instantlogis.instantclient.utilities.BusinessResponseHandler;

/**
 * Created by kalus on 7. 9. 2016.
 */
public interface IOperationService {

    enum OperationType{
        Zaskladneni, Vyskladneni, Presun
    }

    List<WarehouseOperationListInfo> findWarehouseOperations(String searchText, int lastOperationId, int pageSize);

    List<WarehouseOperationListInfo> getWarehouseOperations(List<OperationType> types, int lastOperationId, int pageSize);

    WarehouseOperationListInfo getWarehouseOperation(int operationId);

    void saveOperation(WarehouseOperation WarehouseOperation );

    void createNewWarehouseOperation(String documentNumber, String operationType, BusinessResponseHandler handler);

    void getUnfinishedOperationCountByUser(BusinessResponseHandler handler);

}
