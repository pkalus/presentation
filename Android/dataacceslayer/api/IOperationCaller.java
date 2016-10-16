package cz.ccv.instantlogis.instantclient.dataaccesslayer;

import java.util.List;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operationItem.BasicOperationItem;
import cz.ccv.instantlogis.instantclient.utilities.DataAccessResponseHandler;

/**
 * Created by kalus on 8. 9. 2016.
 */
public interface IOperationCaller {
    //response predava handler!!!

    //vrati Skladovou operaci podle ID
    void getWarehouseOperationById(int warehouseOperationId, final DataAccessResponseHandler handler);

    void createNewWarehouseOperation(String documentNumber, String operationType, DataAccessResponseHandler handler);

    void createNewWareMove(int warehouseOperationId, BasicOperationItem item, DataAccessResponseHandler handler);

    void updateWarehouseOperationState(int warehouseOperationId, String state, String documentNumber, String note, DataAccessResponseHandler handler);

    //vrati seznam skladovych operaci podle zadanych kriterii
    void getWarehouseOperations(Integer start, Integer limit, List<String> states, List<String> types, DataAccessResponseHandler handler);

    //vrati pocet skladovych operaci podle zadanych kriterii
    void getWarehouseOperationCount(List<String> states, List<String> types, DataAccessResponseHandler handler);

    void searchWarehouseOperationByCode(String phrase, Integer start, Integer limit, List<String> states, List<String> types);

    void finishMove(BasicOperationItem operationItem, String operationId, DataAccessResponseHandler handler);
}
