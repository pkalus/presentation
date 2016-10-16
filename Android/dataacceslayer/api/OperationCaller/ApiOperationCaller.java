package cz.ccv.instantlogis.instantclient.dataaccesslayer.api.OperationCaller;

import android.content.Context;
import android.util.Base64;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.JsonObjectRequest;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operation.WarehouseOperation;
import cz.ccv.instantlogis.instantclient.businesslayer.domainModel.operationItem.BasicOperationItem;
import cz.ccv.instantlogis.instantclient.dataaccesslayer.IOperationCaller;
import cz.ccv.instantlogis.instantclient.utilities.ConfigurationSingleton;
import cz.ccv.instantlogis.instantclient.utilities.DataAccessResponseHandler;

/**
 * Created by kalus on 8. 9. 2016.
 */
public class ApiOperationCaller implements IOperationCaller {

    //GET
    @Override
    public void getWarehouseOperationById(int warehouseOperationId, final DataAccessResponseHandler handler) {

    }

    //POST
    @Override
    public void createNewWarehouseOperation(String documentNumber, String operationType, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations";

        JSONObject json = createNewWarehouseOperationJSON(documentNumber, operationType);

        RequestQueue request = Volley.newRequestQueue(ctx);
        JsonObjectRequest jreq = new JsonObjectRequest(Request.Method.POST, url, json, new Response.Listener<JSONObject>() {
            @Override
            public void onResponse(JSONObject response) {
                WarehouseOperation warehouseOperation = fillWarehouseOperationFromJson(response);
                handler.onResponseOK(warehouseOperation);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                String e = error.getMessage();
                if (error.networkResponse.statusCode == 409) {
                    //TODO zadana operace jiz existuje
                    handler.onResponseFailed(error);
                }
            }
        }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }
        };
        request.add(jreq);
    }

    //PUT
    @Override
    public void updateWarehouseOperationState(int warehouseOperationId, String state, String documentNumber, String note, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations/" + Integer.toString(warehouseOperationId);
        JSONObject json = updateWarehouseOperationJSON(state);

        RequestQueue request = Volley.newRequestQueue(ctx);
        JsonObjectRequest jreq = new JsonObjectRequest(Request.Method.PUT, url, json, new Response.Listener<JSONObject>() {
            @Override
            public void onResponse(JSONObject response) {
                handler.onResponseOK(response);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.onResponseFailed(error);
                String e = error.getMessage();
            }
        }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }

            @Override
            public Map<String, String> getParams() {
                return createParams();
            }
        };
        request.add(jreq);
    }

    @Override
    public void getWarehouseOperations(final Integer start, final Integer limit, final List<String> states, final List<String> types, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations/";
        RequestQueue request = Volley.newRequestQueue(ctx);
        JsonObjectRequest jreq = new JsonObjectRequest(Request.Method.GET, url, new Response.Listener<JSONObject>() {
            @Override
            public void onResponse(JSONObject response) {

                handler.onResponseOK(response);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.onResponseFailed(error);
                String e = error.getMessage();
            }
        }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }

            @Override
            public Map<String, String> getParams() {
                return createParams();
            }
        };
        request.add(jreq);
    }

    @Override
    public void getWarehouseOperationCount(final List<String> states, final List<String> types, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        String params = createParamsCount(states, types);
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations/count" + params;
        RequestQueue request = Volley.newRequestQueue(ctx);
        StringRequest jreq = new StringRequest(Request.Method.GET, url, new Response.Listener<String>() {
            @Override
            public void onResponse(String response) {
                handler.onResponseOK(response);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.onResponseFailed(error);
                String e = error.getMessage();
            }
        }) {

            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }


        };


        request.add(jreq);
    }

    //GET
    @Override
    public void searchWarehouseOperationByCode(final String phrase, final Integer start, final Integer limit,
                                               final List<String> states, final List<String> types) {


    }


    //zatim nemazat, ale neni vyuzita
    @Override
    public void finishMove(BasicOperationItem operationItem, String warehouseOperationId, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations/" + warehouseOperationId + "/items";

        RequestQueue request = Volley.newRequestQueue(ctx);
        JsonObjectRequest jreq = new JsonObjectRequest(Request.Method.POST, url, createNewWareMoveJson(operationItem), new Response.Listener<JSONObject>() {
            @Override
            public void onResponse(JSONObject response) {
                handler.onResponseOK(response);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.onResponseFailed(error);
                String e = error.getMessage();
            }
        }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }
        };
        request.add(jreq);
    }

    @Override
    public void createNewWareMove(int warehouseOperationId, BasicOperationItem item, final DataAccessResponseHandler handler) {
        final Context ctx = ConfigurationSingleton.getInstance().getContext();
        final String apiUrl = ConfigurationSingleton.getInstance().getApiUrl();
        final String url = apiUrl + ConfigurationSingleton.getInstance().getVersionApi() + "/warehouseOperations/" + warehouseOperationId + "/items";

        RequestQueue request = Volley.newRequestQueue(ctx);
        JsonObjectRequest jreq = new JsonObjectRequest(Request.Method.POST, url, createNewWareMoveJson(item), new Response.Listener<JSONObject>() {
            @Override
            public void onResponse(JSONObject response) {
                handler.onResponseOK(response);
            }
        }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
                handler.onResponseFailed(error);
                String e = error.getMessage();
            }
        }) {
            @Override
            public Map<String, String> getHeaders() throws AuthFailureError {
                return createBasicAuthHeader(ConfigurationSingleton.getInstance().getUser().getUserName(), ConfigurationSingleton.getInstance().getUser().getPassword());
            }
        };
        request.add(jreq);
    }


    //------private method-------
    private Map<String, String> createBasicAuthHeader(String username, String password) {
        Map<String, String> headerMap = new HashMap<String, String>();
        String credentials = username + ":" + password;
        String encodedCredentials = Base64.encodeToString(credentials.getBytes(), Base64.DEFAULT);
        headerMap.put("Authorization", "Basic " + encodedCredentials);
        String s = headerMap.toString();
        return headerMap;
    }

    private WarehouseOperation fillWarehouseOperationFromJson(JSONObject obj) {
        WarehouseOperation warehouseOperation = new WarehouseOperation();
        warehouseOperation.setDocumentNumber(obj.optString("DocumentNumber"));
        warehouseOperation.setNote(obj.optString("Note"));
        warehouseOperation.setType(obj.optString("Type"));
        warehouseOperation.setState(obj.optString("State"));
        warehouseOperation.setBasicOperationId(obj.optInt("Id"));
        //TODO deserializovat JSON WarehouseOperation
        return warehouseOperation;
    }

    private JSONObject createNewWareMoveJson(BasicOperationItem operationItem) {
        JSONObject finalJson = new JSONObject();
        JSONArray trackingParameters = new JSONArray();
        try {
            finalJson.put("Quantity", operationItem.getQuantity());
            finalJson.put("StorageUnitId", operationItem.getStorageUnit().getStorageUnitId());
            finalJson.put("CompartmentToId", operationItem.getCompartmentTo().getComartmentId());
            finalJson.put("CompartmentFromId", operationItem.getCompartmentFrom().getComartmentId());
            for (int i = 0; i < operationItem.getStorageUnit().getTrackingParameters().size(); i++) {
                JSONObject parameter = new JSONObject();
                parameter.put("SystemName", operationItem.getStorageUnit().getTrackingParameters().get(i).getSystemName());
                parameter.put("Value", operationItem.getStorageUnit().getTrackingParameters().get(i).getValue());
                trackingParameters.put(parameter);
            }
            finalJson.put("TrackingParameter", trackingParameters);
        } catch (Exception e) {

        }
        return finalJson;
    }

    private JSONObject createNewWarehouseOperationJSON(String documentNumber, String operationType) {
        JSONObject finalJson = new JSONObject();
        try {
            finalJson.put("Type", operationType);
            if (!documentNumber.equals("")) {
                finalJson.put("DocumentNumber", documentNumber);
            }
        } catch (Exception e) {
        }
        return finalJson;
    }

    private JSONObject updateWarehouseOperationJSON(String operationState) {
        JSONObject json = new JSONObject();
        try {
            json.put("State", operationState);
        } catch (Exception e) {
        }
        return json;
    }

    private Map<String, String> createParams() {
        Map<String, String> params = new HashMap<>();
        if (ConfigurationSingleton.getInstance().getPhrase().equals("")) {
            params.put("phrase", ConfigurationSingleton.getInstance().getPhrase());
        }

        if (ConfigurationSingleton.getInstance().getStart() != -1) {
            params.put("start", Integer.toString(ConfigurationSingleton.getInstance().getStart()));
        }
        if (ConfigurationSingleton.getInstance().getLimit() != -1) {
            params.put("limit", Integer.toString(ConfigurationSingleton.getInstance().getLimit()));
        }
        for (int i = 0; i < ConfigurationSingleton.getInstance().getStates().size(); i++) {
            params.put("states", ConfigurationSingleton.getInstance().getStates().get(i));
        }
        for (int i = 0; i < ConfigurationSingleton.getInstance().getTypes().size(); i++) {
            params.put("types", ConfigurationSingleton.getInstance().getTypes().get(i));
        }

        ConfigurationSingleton.getInstance().setDefaultParams();

        return params;
    }

    private String createParamsCount(List<String> states, List<String> types) {
        String params = "?";
        for (int i = 0; i < states.size(); i++) {
            params = params + "&states=" + states.get(i);
        }
        for (int i = 0; i < types.size(); i++) {
            params = params + "&types=" + types.get(i);
        }
        return params;
    }
}
