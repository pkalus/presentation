package cz.ccv.instantlogis.instantclient.dataaccesslayer.api.OperationCaller;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operationItem.BasicOperationItem;
import cz.ccv.instantlogis.instantclient.dataaccesslayer.IOperationCaller;
import cz.ccv.instantlogis.instantclient.utilities.DataAccessResponseHandler;

/**
 * Created by Petr on 02.10.2016.
 */

public class FakeApiOperationCaller implements IOperationCaller {
    @Override
    public void getWarehouseOperationById(int warehouseOperationId, DataAccessResponseHandler handler) {

    }

    @Override
    public void createNewWarehouseOperation(String documentNumber, String operationType, DataAccessResponseHandler handler) {
        handler.onResponseOK(fillWarehouseOperationFromJsonFake());
    }

    @Override
    public void createNewWareMove(int warehouseOperationId, BasicOperationItem item, DataAccessResponseHandler handler) {

    }

    @Override
    public void updateWarehouseOperationState(int warehouseOperationId, String state, String documentNumber, String note, DataAccessResponseHandler handler) {

    }

    @Override
    public void getWarehouseOperations(Integer start, Integer limit, List<String> states, List<String> types, DataAccessResponseHandler handler) {
        handler.onResponseOK(getFakeWarehouseOperations(start, limit, states, types));
    }

    @Override
    public void getWarehouseOperationCount(List<String> states, List<String> types, DataAccessResponseHandler handler) {

    }


    @Override
    public void searchWarehouseOperationByCode(final String phrase, final Integer start, final Integer limit, final List<String> states, List<String> types) {

    }

    @Override
    public void finishMove(BasicOperationItem operationItem, String operationId, DataAccessResponseHandler handler) {

    }


    /////----------private method ----------

    private WarehouseOperation fillWarehouseOperationFromJsonFake() {
        WarehouseOperation wo = new WarehouseOperation();
        wo.setDocumentNumber("PRI123456");
        wo.setType("Income");
        wo.setCreationDate(new Date());
        return wo;
    }


    private List<WarehouseOperation> getFakeWarehouseOperations(Integer start, Integer limit,
                                                                List<String> states, List<String> types) {
        List<WarehouseOperation> listOperation = new ArrayList<>();
        WarehouseOperation wo = new WarehouseOperation();
        if (start != null) {

        }
        if (limit != null) {

        }

        return listOperation;
    }

}
