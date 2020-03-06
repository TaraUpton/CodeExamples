import { createService } from "~/hocs/crud";
import * as model from "./model";
import defaultSurfaceDefaultsRecord from "./default-record";
import { adminData } from "~/admin/data";
import {
    HierarchyAPI,
    SurfaceDefaultsAPI } from "~/utils/api";

// Module Name
export const MODEL_NAME = "surfaceDefaults";

// Request payload
export const REQUEST_PAYLOAD_FILTER = "surfaceDefaultsFilter";
export const REQUEST_PAYLOAD_PAGE_OPTIONS = "surfaceDefaultsPageOptions";
export const REQUEST_PAYLOAD_SORT_LIST = "surfaceDefaultsSort";

// URLs
export const AUTO_SEARCH_URL = SurfaceDefaultsAPI.AUTO_SEARCH_SURFACE_DEFAULTS;
export const CREATE = SurfaceDefaultsAPI.ADD_SURFACE_DEFAULTS;
export const DELETE = SurfaceDefaultsAPI.DELETE_SURFACE_DEFAULTS;
export const EXPORT_FILE_NAME = "SurfaceDefaultsExport";
export const EXPORT_URL = SurfaceDefaultsAPI.EXPORT_SURFACE_DEFAULTS_LIST;
export const GETRECORD = SurfaceDefaultsAPI.GET_SURFACE_DEFAULTS;
export const HIERARCHY_URL = HierarchyAPI.GET_HIERARCHY_FILTER_LIST_WITH_SEARCH;
export const IMPORT_URL = SurfaceDefaultsAPI.IMPORT_SURFACE_DEFAULTS_LIST;
export const IMPORT_VALID_URL = SurfaceDefaultsAPI.IMPORT_SURFACE_DEFAULTS_VALID_URL;
export const GET_AUTO_CREATE_REPORTS_LIST = SurfaceDefaultsAPI.GET_AUTO_CREATE_REPORTS_LIST;
export const REQUEST_ORG_LEVEL = HierarchyAPI.REQUEST_ORG_LEVEL_WITH_PARENTS_GUID;
export const SELECT_ALL = SurfaceDefaultsAPI.SELECT_ALL_SURFACE_DEFAULTS;
export const UPDATE = SurfaceDefaultsAPI.UPDATE_SURFACE_DEFAULTS;
export const URL = SurfaceDefaultsAPI.GET_SURFACE_DEFAULTS_LIST;
export const REQUEST_CLASSIFICATION_METHOD = SurfaceDefaultsAPI.REQUEST_CLASSIFICATION_METHOD_LIST;
export const REQUEST_SYSTEM_ATTRIBUTE = SurfaceDefaultsAPI.REQUEST_SYSTEM_ATTRIBUTE_LIST;

// Default filter object
export const defaultRequestFilters = {
    [REQUEST_PAYLOAD_FILTER]: {
        SystemAttributeName: "",
        ColorRampName: "",
        LocationLevel: "",
        ClassificationMethodName: "",
        NumberOfClasses: ""
    },
    [REQUEST_PAYLOAD_SORT_LIST]: [{
        FieldName: "",
        Sort: {
            Direction: "ASC",
            Order: 0
        }
    }],
    [REQUEST_PAYLOAD_PAGE_OPTIONS]: {
        pageSize: 50,
        skip: 0
    },
    userGuid: ""
};

export const defaultSort = {
    ...defaultRequestFilters[REQUEST_PAYLOAD_SORT_LIST][0],
    FieldName: "",
};

export const dropdowns = {
    [model.PROPS_SYSTEM_ATTRIBUTE_NAME]: REQUEST_SYSTEM_ATTRIBUTE,
    [model.PROPS_CLASSIFICATION_METHOD_NAME]: REQUEST_CLASSIFICATION_METHOD,
    [model.PROPS_ORG_LEVEL_LIST]: { url: REQUEST_ORG_LEVEL, model: "_" },
};

// Service
export const service = createService({
    guid: model.PROPS_SURFACE_DEFAULTS_GUID,
    name: model.PROPS_SURFACE_DEFAULTS_NAME,
    modelName: MODEL_NAME,
    defaultRequestFilters,
    REQUEST_PAYLOAD_FILTER,
    REQUEST_PAYLOAD_SORT_LIST,
    REQUEST_PAYLOAD_PAGE_OPTIONS,
    EXPORT_FILE_NAME,
    dropdowns,
    urls: {
        AUTO_SEARCH_URL,
        CREATE,
        DELETE,
        EXPORT_URL,
        GETRECORD,
        HIERARCHY_URL,
        IMPORT_VALID_URL,
        IMPORT_URL,
        SELECT_ALL,
        UPDATE,
        URL
    },
    _defaultLabels: {
        [model.PROPS_SYSTEM_ATTRIBUTE_NAME]: { label: "systemAttributeName", gridCol: 15 },
        [model.PROPS_ORG_LEVEL_NAME]: { label: "location", gridCol: 10, sortNameOverRide: "locationLevel" },
        [model.PROPS_COLOR_RAMP_NAME]: { label: "colorRampName", gridCol: 15 },
        [model.PROPS_CLASSIFICATION_METHOD_NAME]: { label: "classificationMethodName", gridCol: 10 },
        [model.PROPS_NUMBER_OF_CLASSES]: { label: "numberOfClasses", gridCol: 10 },
        [model.PROPS_CAN_DELETE]: { label: "canDelete", gridCol: 5, className: "col-shift-15" }
    },
    getDefaultRecord: () => ({ ...defaultSurfaceDefaultsRecord() }),
    defaultSort,
});