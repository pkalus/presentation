package cz.ccv.instantlogis.instantclient.businesslayer.domainService.Services;

import java.util.ArrayList;
import java.util.List;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.Entities.WarehouseOperationListInfo;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.IOperationService;
import cz.ccv.instantlogis.instantclient.dataaccesslayer.IOperationCaller;
import cz.ccv.instantlogis.instantclient.utilities.BusinessResponseHandler;
import cz.ccv.instantlogis.instantclient.utilities.ConfigurationSingleton;
import cz.ccv.instantlogis.instantclient.utilities.DataAccessResponseHandler;

/**
 * Created by kalus on 7. 9. 2016.
 */
public class OperationService implements IOperationService {

    IOperationCaller _operationCaller;

    public OperationService() {
        _operationCaller = ConfigurationSingleton.getInstance().getOperationCaller();
    }
        /**
         * *
         * @param searchText
         * @param lastOperationId
         * @param pageSize
         * @return
         */
        public List<WarehouseOperationListInfo> findWarehouseOperations(String searchText, int lastOperationId, int pageSize){
            return null;
        }

        /***
         *
         * @param types
         * @param lastOperationId
         * @param pageSize
         * @return
         */
        public List<WarehouseOperationListInfo> getWarehouseOperations(List<OperationType> types, int lastOperationId, int pageSize){
            return null;
        }

        /**
         *
         * @param operationId
         * @return
         */
        public WarehouseOperationListInfo getWarehouseOperation(int operationId){
            return null;
        }

        /**
         *
         * @param WarehouseOperation
         */
        public void saveOperation(WarehouseOperation WarehouseOperation ){

        }

    public void createNewWarehouseOperation(String documentNumber, String operationType, final BusinessResponseHandler handler) {
        _operationCaller.createNewWarehouseOperation(documentNumber, operationType, new DataAccessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
                handler.onResponseOK(o);
            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
                handler.onResponseFailed(o);
            }
        });


        }

    @Override
    public void getUnfinishedOperationCountByUser(final BusinessResponseHandler handler) {
        List<String> types = new ArrayList<>();
        types.add("Income");
        types.add("Outcome");
        types.add("Transfer");
        List<String> states = new ArrayList<>();
        states.add("InProgress");
        _operationCaller.getWarehouseOperationCount(states, types, new DataAccessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
                handler.onResponseOK(o);
            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
                handler.onResponseFailed(o);
            }
        });
    }
}
