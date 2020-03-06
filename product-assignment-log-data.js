import { createController, createService, prepActions } from "~/hocs/crud";
import { apiUrl } from "~/utils/api";

// Model keys
export const PROPS_AGBYTES_LOG_GUID = "agBytesLogGuid";
export const PROP_DATE = "activityDate";
export const PROPS_ACTIVITY = "activity";
export const PROPS_DETAILS = "detailsMessage";
export const PROPS_DETAILS_SORT_NAME = "details";

export const MODEL_NAME = "productAssignmentLog";
export const FEATURE_NAME = "productAssignment";
export const URL = apiUrl("AgBytes/GetAgBytesLog");


// Request payload
export const REQUEST_FEATURE = "feature";
export const REQUEST_PAYLOAD_SORT_LIST = "agBytesLogSortList";
export const REQUEST_PAYLOAD_PAGE_OPTIONS = "agBytesLogPageOptions";

export const defaultRequestFilters = {
    [REQUEST_FEATURE]: FEATURE_NAME,
    [REQUEST_PAYLOAD_SORT_LIST]: [{
        "FieldName": "ActivityDate",
        "Sort": {
            "Direction": "DESC",
            "Order": 0
        }
    }],
    [REQUEST_PAYLOAD_PAGE_OPTIONS]: {
        "pageSize": 20,
        "skip": 0
    }
};
export const service = createService({
    id: PROPS_AGBYTES_LOG_GUID,
    guid: PROPS_AGBYTES_LOG_GUID,
    modelName: MODEL_NAME,
    defaultRequestFilters,
    REQUEST_PAYLOAD_SORT_LIST,
    REQUEST_PAYLOAD_PAGE_OPTIONS,
    isModalWindow: true,
    urls: {
        URL,
    },
    _defaultLabels: {
        [PROP_DATE]: { label: "date", gridCol: 40 },
        [PROPS_ACTIVITY]: { label: "activity", gridCol: 20 },
        [PROPS_DETAILS]: { label: "details", gridCol: 40, sortNameOverRide: PROPS_DETAILS_SORT_NAME }
    },
    numOfRecords: [
        { value: "5", label: "5", selected: false },
        { value: "10", label: "10", selected: false },
        { value: "15", label: "15", selected: false },
        { value: "20", label: "20", selected: true }
    ],
    defaultSort: defaultRequestFilters[REQUEST_PAYLOAD_SORT_LIST][0],
});

export const actions = prepActions(service);
export const productAssignmentLogSagas = createController(service, actions);
