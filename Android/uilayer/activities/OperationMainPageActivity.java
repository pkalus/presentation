package cz.ccv.instantlogis.instantclient.uilayer.activities;

import android.os.Bundle;
import android.support.v4.app.FragmentActivity;

import cz.ccv.instantlogis.instantclient.R;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.BasicOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.uilayer.fragments.ioMainBodyFragment;
import cz.ccv.instantlogis.instantclient.uilayer.fragments.ioMainFooterFragment;
import cz.ccv.instantlogis.instantclient.uilayer.fragments.ioMainHeaderFragment;

/**
 * Created by kalus on 19. 9. 2016.
 */

public class OperationMainPage extends FragmentActivity implements ioMainHeaderFragment.MainHeaderFragmentListener, ioMainBodyFragment.MainBodyFragmentListener, ioMainFooterFragment.MainFooterInterface {

    WarehouseOperation warehouseOperation;
    String operationType;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.manual_operation_main_layout);
        if (warehouseOperation == null) {
            warehouseOperation = new WarehouseOperation();
        }
        Bundle extras = getIntent().getExtras();
        operationType = extras.getString("operationType", null);
    }

    //
    @Override
    public void operationCreated(BasicOperation operation) {
        warehouseOperation = (WarehouseOperation) operation;
        ioMainBodyFragment mainBodyFragment = (ioMainBodyFragment) getSupportFragmentManager().findFragmentById(R.id.main_body);
        mainBodyFragment.onOperationCreated(warehouseOperation);
    }


    @Override
    public void onOperationItemCreated() {

    }


    @Override
    public void finishOperation() {
        ioMainBodyFragment mainBodyFragment = (ioMainBodyFragment) getSupportFragmentManager().findFragmentById(R.id.main_body);
        mainBodyFragment.finishOperation();
    }

    @Override
    public void nextOperationMove() {
        ioMainBodyFragment mainBodyFragment = (ioMainBodyFragment) getSupportFragmentManager().findFragmentById(R.id.main_body);
        mainBodyFragment.nextOperationMove();
    }
}
