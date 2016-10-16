package cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operationItem.BasicOperationItem;
import cz.ccv.instantlogis.instantclient.dataaccesslayer.IOperationCaller;
import cz.ccv.instantlogis.instantclient.utilities.BusinessResponseHandler;
import cz.ccv.instantlogis.instantclient.utilities.ConfigurationSingleton;
import cz.ccv.instantlogis.instantclient.utilities.DataAccessResponseHandler;

/**
 * Created by kalus on 7. 9. 2016.
 */
public class BasicOperation {

    IOperationCaller _operationCaller;
    Date creationDate;
    String documentNumber;
    String note;
    String type;
    String state;  // Operation State
    List<BasicOperationItem> items;
    int basicOperationId;

    public BasicOperation() {
        _operationCaller = ConfigurationSingleton.getInstance().getOperationCaller();
        items = new ArrayList<>();
    }

    public Date getCreationDate() {
        return creationDate;
    }

    public void setCreationDate(Date creationDate) {
        this.creationDate = creationDate;
    }

    public String getDocumentNumber() {
        return documentNumber;
    }

    public void setDocumentNumber(String documentNumber) {
        this.documentNumber = documentNumber;
    }

    public String getNote() {
        return note;
    }

    public void setNote(String note) {
        this.note = note;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public List<BasicOperationItem> getItems() {
        return items;
    }

    public String getState() {
        return state;
    }

    public void setState(String state) {
        this.state = state;
    }

    public int getBasicOperationId() {
        return basicOperationId;
    }

    public void setBasicOperationId(int basicOperationId) {
        this.basicOperationId = basicOperationId;
    }

    //------------------

    public void addItemMove(final BusinessResponseHandler handler) {
       BasicOperationItem item = items.get(items.size()-1);
        _operationCaller.createNewWareMove(basicOperationId, item, new DataAccessResponseHandler() {
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

    public void addItem(BasicOperationItem item) {
        items.add(item);
    }

    public void finishOperation(final BusinessResponseHandler handler) {
        state = "Finished";
        _operationCaller.updateWarehouseOperationState(basicOperationId, state, documentNumber, note, new DataAccessResponseHandler() {
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
