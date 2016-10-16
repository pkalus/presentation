package cz.ccv.instantlogis.instantclient.uilayer.fragments;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.support.v4.app.Fragment;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.view.inputmethod.InputMethodManager;
import android.widget.BaseAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ListView;
import android.widget.PopupWindow;
import android.widget.RelativeLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.List;

import cz.ccv.instantlogis.instantclient.R;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.Compartment;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.StorageUnit;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.Supply;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operationItem.BasicOperationItem;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.parameter.TrackingParameter;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.parameter.TrackingParameterPrescription;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.IOperationService;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.IStorageUnitService;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.IWarehouseService;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.Services.OperationService;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.Services.StorageUnitService;
import cz.ccv.instantlogis.instantclient.businesslayer.domainService.Services.WarehouseService;
import cz.ccv.instantlogis.instantclient.businesslayer.entities.StorageUnitListInfo;
import cz.ccv.instantlogis.instantclient.uilayer.activities.MainMenuActivity;
import cz.ccv.instantlogis.instantclient.utilities.BusinessResponseHandler;
import cz.ccv.instantlogis.instantclient.utilities.ConfigurationSingleton;

import static android.view.View.generateViewId;

/**
 * Created by kalus on 19. 9. 2016.
 */

public class ioMainBodyFragment extends Fragment {
    MainBodyFragmentListener listener;
    String operationType;
    final int itemsCount = 0;

    //-----services----------
    IOperationService _operationService;
    IWarehouseService _warehouseService;
    IStorageUnitService _storageUnitService;

    //-----Elements------
    Button btnDeleteComapartment;
    Button btnDeleteWare;
    EditText etCompartment;
    EditText etStorageUnit;
    EditText etQuantity;

    ListView modalListView;
    View modalLayout;

    //--------Objects-------
    WarehouseOperation warehouseOperation;
    BasicOperationItem operationItem;
    Compartment compartment;
    StorageUnit storageUnit;


    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        Bundle extras = getActivity().getIntent().getExtras();
        operationType = extras.getString("operationType", null);
        switch (operationType) {
            case "Income":
                return inflater.inflate(R.layout.income_outcome_body_fragment, container, false);

            case "Outcome":
                return inflater.inflate(R.layout.income_outcome_body_fragment, container, false);

            case "Move":
                return inflater.inflate(R.layout.move_body_fragment, container, false);

            default:
                return inflater.inflate(R.layout.income_outcome_body_fragment, container, false);
        }
    }

    @Override
    public void onViewCreated(View view, Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        getActivity().getWindow().setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_PAN);
        _operationService = new OperationService();
        _warehouseService = new WarehouseService();
        _storageUnitService = new StorageUnitService();

        switch (operationType) {
            case "Income":
                incomeView();
                break;
            case "Outcome":
		//same View as Income 
                incomeView();
                break;
            case "Move":
                //TODO moveView(); -  bude reseno jinym fragmentem
                break;
            default:

                break;
        }
    }

    @Override
    public void onAttach(Activity activity) {
        super.onAttach(activity);
        try {
            listener = (MainBodyFragmentListener) activity;

        } catch (ClassCastException e) {
            throw new ClassCastException(activity.toString() + "neni implementovany listener");
        }
    }

    public void onOperationCreated(WarehouseOperation operation) {
        this.warehouseOperation = operation;
        operationItem = new BasicOperationItem();
        etCompartment.setEnabled(true);
        etCompartment.requestFocus();
      /*  InputMethodManager imm = (InputMethodManager) getActivity().getSystemService(Context.INPUT_METHOD_SERVICE);
        imm.toggleSoftInput(InputMethodManager.SHOW_IMPLICIT,0);*/
    }

    public void finishOperation(){
        warehouseOperation.finishOperation(new BusinessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
		//TODO texty do Resource!!!
                Toast.makeText(getActivity().getApplicationContext(), "--- OPERACE BYLA DOKONCENA ---- ", Toast.LENGTH_LONG).show();
                //TODO co se ma stat po dokonceni operace
                // navrat na vyber operaci???
                Intent intent = new Intent(getActivity().getApplicationContext(), MainMenuActivity.class);
                startActivity(intent);
            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
            }
        });
    }

    public void nextOperationMove() {
        //TODO Odeslat OperationItem na API a vymazat predchozi udaje krom prihradky
                etCompartment.setEnabled(true);
                etStorageUnit.setEnabled(false);
                etStorageUnit.setText("");
                etQuantity.setEnabled(false);
                etQuantity.setText("");
                BasicOperationItem newItem = new BasicOperationItem();
                operationItem = newItem;
    }

    // ---------- private method -----
    private void incomeView() {
        warehouseOperation = new WarehouseOperation();
        operationItem = new BasicOperationItem();
        //button na smazání špatně zadané přihrádky
        btnDeleteComapartment = (Button) getView().findViewById(R.id.btn_deteteComaprtment);
        btnDeleteComapartment.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                etCompartment.setText("");
                if(operationItem != null){
                    if(operationItem.getCompartmentTo() != null){
                        operationItem.getCompartmentTo().setComartmentId(-1);
                        operationItem.getCompartmentTo().setCode("");
                        etCompartment.requestFocus();
                        etCompartment.setEnabled(true);
                        etStorageUnit.setEnabled(false);
                    }
                }
                //TODO vytvorit metodu delete pro mazani
            }
        });
        btnDeleteWare = (Button) getView().findViewById(R.id.btn_deleteWare);
        btnDeleteWare.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                //TODO vytvorit metodu pro mazani skladovacich jednotek
            }
        });
        //----------Prihradka----------------
        etCompartment = (EditText) getView().findViewById(R.id.et_Compartment);
      
        etCompartment.setOnKeyListener(new View.OnKeyListener() {
            @Override
            public boolean onKey(View v, int keyCode, KeyEvent event) {
                if (event.getAction() == KeyEvent.ACTION_DOWN) {
                    switch (keyCode) {
                        case KeyEvent.KEYCODE_DPAD_CENTER:
                        case KeyEvent.KEYCODE_ENTER:
                            getCompartmentByCode();
                            return true;
                        default:
                            break;
                    }
                }
                return false;
            }
        });
        //--------------Zbozi-------------------
        etStorageUnit = (EditText) getView().findViewById(R.id.et_StorageUnit);
        etStorageUnit.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {

            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
                //TODO Podminka  kdyz je zadano najednou vice znaku
                if (start - before > 1) {
                    getStorageUnitListInfo(s.toString(), operationType);
                }
            }

            @Override
            public void afterTextChanged(Editable s) {

            }
        });
        etStorageUnit.setOnKeyListener(new View.OnKeyListener() {
            @Override
            public boolean onKey(View v, int keyCode, KeyEvent event) {
                if (event.getAction() == KeyEvent.ACTION_DOWN) {
                    switch (keyCode) {
                        case KeyEvent.KEYCODE_DPAD_CENTER:
                        case KeyEvent.KEYCODE_ENTER:
                            getStorageUnitListInfo(etStorageUnit.getText().toString(), operationType);
                            return true;
                        default:
                            break;
                    }
                }
                return false;
            }
        });
        etQuantity = (EditText) getView().findViewById(R.id.et_quantity);
        etQuantity.setOnKeyListener(new View.OnKeyListener() {
            @Override
            public boolean onKey(View v, int keyCode, KeyEvent event) {
                if (event.getAction() == KeyEvent.ACTION_DOWN) {
                    switch (keyCode) {
                        case KeyEvent.KEYCODE_DPAD_CENTER:
                        case KeyEvent.KEYCODE_ENTER:
                            final double quantity = Double.parseDouble(etQuantity.getText().toString());
                            operationItem.setQuantity(quantity);
                            //TODO pokud zbozi nesleduje parametry sledovani ukonci pohyb
                            warehouseOperation.addItem(operationItem);
                           //TODO pokud sleduje parametry zobraz tyto parametry
                            if (operationItem.getStorageUnit().getTrackingParameterPrescriptions().size() > 0) {
                                showTrackingParameters(operationItem.getStorageUnit().getTrackingParameterPrescriptions());
                            } else {
                                warehouseOperation.addItemMove(new BusinessResponseHandler() {
                                    @Override
                                    public void onResponseOK(Object o) {
                                        super.onResponseOK(o);
                                        Toast.makeText(getActivity().getApplicationContext(), "---- Pohyb dokoncen ---", Toast.LENGTH_LONG).show();
                                        etQuantity.setEnabled(false);
                                    }

                                    @Override
                                    public void onResponseFailed(Object o) {
                                        super.onResponseFailed(o);
                                    }
                                });
                            }

                            return true;
                        default:
                            break;
                    }
                }
                return false;
            }
        });

    }

    private void getCompartmentByCode() {
        String compartmentCode = etCompartment.getText().toString();
        ConfigurationSingleton.getInstance().setContext(getActivity().getApplicationContext());
        _warehouseService.getCompartmentByCode(compartmentCode, new BusinessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
                Toast.makeText(getActivity().getApplicationContext(), "OK", Toast.LENGTH_LONG).show();
                compartment = (Compartment) o;
                if (compartment.getInitiallyStocked()) {
                    if (!compartment.getLockedUp()) {
                        etCompartment.setEnabled(false);
                        etStorageUnit.setEnabled(true);
                        etStorageUnit.requestFocus();
                        if (operationType.equals("Income")) {
                            operationItem.setCompartmentTo(compartment);
                        }else{
                            operationItem.setCompartmentFrom(compartment);
                        }
                    }
                } else {
                    Toast.makeText(getActivity().getApplicationContext(), "Přihrádka nebyla prvotně zaskladněna!", Toast.LENGTH_LONG).show();
                }

            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
                Toast.makeText(getActivity().getApplicationContext(), "Chyba", Toast.LENGTH_LONG).show();

            }
        });
    }

    private void getStorageUnitListInfo(String storageUnitCode, final String operationType) {
        //Ziskat skladovaci jednotku..pokud jich ma vic stejny kod, zobrazit seznam a uzivatel at si vybere
        _storageUnitService.getStorageUnitsInfoByCode(storageUnitCode, new BusinessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
                List<StorageUnitListInfo> storageUnits = new ArrayList<>();
                storageUnits.addAll((List<StorageUnitListInfo>) o);
                if (storageUnits.size() == 0) {
                    Toast.makeText(getActivity().getApplicationContext(), "--  Zboží nenalezeno!  -- ", Toast.LENGTH_LONG);
                } else if (storageUnits.size() == 1) {
                    for (StorageUnitListInfo su : storageUnits) {
                        //TODO get concrete StorageUnit from API
                        getStorageUnitById(su.getId());
                    }
                } else {
                    showStorageUnitsList(storageUnits);
                }
            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
            }
        });
    }

    //-----------private method to API-----------
    private void showStorageUnitsList(List<StorageUnitListInfo> storageUnits) {
        //TODO use modalPopUp
        //----Schovani klavesnice----
        View view = getActivity().getCurrentFocus();
        if (view != null) {
            InputMethodManager imm = (InputMethodManager) getActivity().getSystemService(Context.INPUT_METHOD_SERVICE);
            imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
        }
        ///----------------------------

        LayoutInflater layoutInflater = (LayoutInflater) getActivity().getBaseContext().getSystemService(Context.LAYOUT_INFLATER_SERVICE);
        modalLayout = layoutInflater.inflate(R.layout.modal_list, null);
        modalListView = (ListView) modalLayout.findViewById(R.id.list_view);
        modalListView.setAdapter(new StorageUnitListAdapter(getActivity().getApplicationContext(), storageUnits));


        final PopupWindow popupWindow = new PopupWindow(modalLayout,
                350,
                ViewGroup.LayoutParams.WRAP_CONTENT);

        popupWindow.showAtLocation(this.getView(), Gravity.CENTER, 0, 0);
    }

    private void getStorageUnitById(int storageUnitId) {
        _storageUnitService.getStorageUnit(storageUnitId, new BusinessResponseHandler() {
            @Override
            public void onResponseOK(Object o) {
                super.onResponseOK(o);
                StorageUnit su = (StorageUnit) o;
                storageUnit = su;
                operationItem.setStorageUnit(su);
                etStorageUnit.setEnabled(false);
                etQuantity.setEnabled(true);
                etQuantity.requestFocus();
                operationItem.getStorageUnit().setTrackingParameterPrescriptions(su.getTrackingParameterPrescriptions());
                if (operationType.equals("Outcome")) {
                    checkSuppliesByCompartmentId();
                }
            }

            @Override
            public void onResponseFailed(Object o) {
                super.onResponseFailed(o);
            }
        });
    }

    private void showTrackingParameters(final List<TrackingParameterPrescription> parametersPresciption) {
        final RelativeLayout rl = new RelativeLayout(getActivity().getApplicationContext());
        rl.setId(generateViewId());
        ScrollView scrollView = (ScrollView) getActivity().findViewById(R.id.tracking_parameters_sv);

        RelativeLayout.LayoutParams rlparams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        rlparams.addRule(RelativeLayout.ALIGN_PARENT_TOP);

        TextView tvLast = null;
        EditText etLast = null;

        for (int i = 0; i < parametersPresciption.size(); i++) {
            final TrackingParameter trackingParameter = new TrackingParameter();
            trackingParameter.setSystemName(parametersPresciption.get(i).getSystemName());
            trackingParameter.setId(parametersPresciption.get(i).getId());
            operationItem.getStorageUnit().addTrackingParameter(trackingParameter);
            RelativeLayout.LayoutParams tvParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT);
            if (tvLast != null) {
                tvParams.addRule(RelativeLayout.BELOW, tvLast.getId());
            }
            tvParams.setMargins(5, 30, 0, 10);
            final TextView tv = new TextView(this.getActivity());
            tv.setText(parametersPresciption.get(i).getSystemName());
            tv.setId(generateViewId());
            tv.setWidth(150);
            tvLast = tv;

            rl.addView(tv, tvParams);

            RelativeLayout.LayoutParams etParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT);
            etParams.addRule(RelativeLayout.RIGHT_OF, tvLast.getId());
            etParams.addRule(RelativeLayout.ALIGN_BOTTOM, tvLast.getId());
            etParams.setMargins(0, 5, 0, 0);

            final EditText et = new EditText(this.getActivity());
            et.setHint(trackingParameter.getSystemName());
            et.setId(generateViewId());
            etLast = et;
            final int j = i;
            et.setOnKeyListener(new View.OnKeyListener() {
                @Override
                public boolean onKey(View v, int keyCode, KeyEvent event) {
                    if (event.getAction() == KeyEvent.ACTION_DOWN) {
                        switch (keyCode) {
                            case KeyEvent.KEYCODE_DPAD_CENTER:
                            case KeyEvent.KEYCODE_ENTER:
                                operationItem.getStorageUnit().getTrackingParameters().get(j).setValue(et.getText().toString());
                                if(parametersPresciption.size() == (j+1) ){
                                    warehouseOperation.addItemMove(new BusinessResponseHandler() {
                                        @Override
                                        public void onResponseOK(Object o) {
                                            super.onResponseOK(o);
                                            Toast.makeText(getActivity().getApplicationContext(), "---- Pohyb dokoncen ---", Toast.LENGTH_LONG).show();
                                            etQuantity.setEnabled(false);
                                            rl.setVisibility(View.INVISIBLE);
                                        }

                                        @Override
                                        public void onResponseFailed(Object o) {
                                            super.onResponseFailed(o);
                                        }
                                    });
                                }
                                return true;
                            default:
                                break;
                        }
                    }
                    return false;
                }
            });
            rl.addView(et, etParams);

        }
        scrollView.addView(rl, rlparams);
    }

    ///------------interface ----------------
    public interface MainBodyFragmentListener {
        void onOperationItemCreated();
    }

    private void checkSuppliesByCompartmentId() {
        switch (operationType) {
            case "Income":
                _storageUnitService.getSuppliesByCompartmentId(operationItem.getCompartmentTo().getComartmentId(), new BusinessResponseHandler() {
                    @Override
                    public void onResponseOK(Object o) {
                        super.onResponseOK(o);
                    }

                    @Override
                    public void onResponseFailed(Object o) {
                        super.onResponseFailed(o);
                    }
                });
                break;
            case "Outcome":
                _warehouseService.getSuppliesByCompartmentId(operationItem.getCompartmentFrom().getComartmentId(), new BusinessResponseHandler() {
                    @Override
                    public void onResponseOK(Object o) {
                        super.onResponseOK(o);
                        List<Supply> supplies = (List<Supply>) o;
                        Double usableQuantity = 0.00;
                        for(int i = 0; i < supplies.size(); i++) {
                         if(supplies.get(i).getStorageUnitId() == operationItem.getStorageUnit().getStorageUnitId()){
                             usableQuantity += supplies.get(i).getQuantity();
                         }
                        }
                        if(usableQuantity > 0){
                            etQuantity.setHint("Použitelné množství: " +usableQuantity.toString());
                        }
                    }

                    @Override
                    public void onResponseFailed(Object o) {
                        super.onResponseFailed(o);
                    }
                });
                break;

        }
    }

    private void existSuppliesInComartmet(List<Supply> supplies) {
        for (int i = 0; i < supplies.size(); i++) {

        }

    }


    ///-------- Adapter -------
    class StorageUnitListAdapter extends BaseAdapter {
        Context mContext;
        List<StorageUnitListInfo> rowItems;

       public StorageUnitListAdapter(Context context, List<StorageUnitListInfo> items){
          this.mContext = context;
           this.rowItems = items;
       }

        @Override
        public View getView(int position, View convertView, ViewGroup parent){
            View view;

            if (convertView == null) {
                LayoutInflater inflater = (LayoutInflater) mContext.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
                view = inflater.inflate(R.layout.storage_units_list_basic_row, null);

            } else {
                view = convertView;
            }
            TextView tv_name = (TextView) view.findViewById(R.id.tv_storageUnit_Name);
            TextView tv_code = (TextView) view.findViewById(R.id.tv_storageUnit_code);
            final String code = Integer.toString(rowItems.get(position).getId());
            final String name = rowItems.get(position).getStorageUnitName();
            tv_name.setText(rowItems.get(position).getStorageUnitName());
            tv_code.setText(code);
            final int storageUnitId = rowItems.get(position).getId();
            view.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    //TODO Ziskej z API konkretni skladovaci jednotku
                    etStorageUnit.setText(name);

                    Toast.makeText(getActivity().getApplicationContext(), "Skladovaci jednotka " + code + " vybrana", Toast.LENGTH_LONG).show();
                    modalLayout.setVisibility(View.INVISIBLE);
                    etQuantity.setEnabled(true);
                    etStorageUnit.setEnabled(false);
                }
            });


            return view;
        }



        @Override
        public int getCount() {
            return rowItems.size();
        }

        @Override
        public Object getItem(int position) {
            return rowItems.get(position);
        }

        @Override
        public long getItemId(int position) {
            return 0;
        }

    }
}
