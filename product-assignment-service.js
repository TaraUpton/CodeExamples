import { createService } from "~/hocs/crud";
import * as model from "./model";
import defaultProductAssignmentRecord from "./default-record";
import { adminData } from "~/admin/data";
import {
    CropAPI,
    GrowthStageOrderAPI,
    HierarchyAPI,
    ProductAssignmentAPI } from "~/utils/api";

import {
    PICKLIST_CROP_PURPOSE,
    PICKLIST_SAMPLE_TYPE,
    getPickListCode
} from "~/core/picklist/picklist-names";

// Module Name
export const MODEL_NAME = "productAssignment";
export const PRODUCT_DROPDOWN = "product";

// Request payload
export const REQUEST_PAYLOAD_ACTIVE_ONLY = "activeOnly";
export const REQUEST_PAYLOAD_FILTER = "productAssignmentFilter";
export const REQUEST_PAYLOAD_PAGE_OPTIONS = "productAssignmentPageOptions";
export const REQUEST_PAYLOAD_SORT_LIST = "productAssignmentSort";

// URLs
export const AUTO_SEARCH_URL = ProductAssignmentAPI.AUTO_SEARCH_PRODUCT_ASSIGNMENT;
export const CREATE = ProductAssignmentAPI.ADD_PRODUCT_ASSIGNMENT;
export const DELETE = ProductAssignmentAPI.DELETE_PRODUCT_ASSIGNMENT;
export const EXPORT_FILE_NAME = "ProductAssignmentExport";
export const EXPORT_URL = ProductAssignmentAPI.EXPORT_PRODUCT_ASSIGNMENT_LIST;
export const GETRECORD = ProductAssignmentAPI.GET_PRODUCT_ASSIGNMENT;
export const HIERARCHY_URL = HierarchyAPI.GET_HIERARCHY_FILTER_LIST_WITH_SEARCH;
export const IMPORT_URL = ProductAssignmentAPI.IMPORT_PRODUCT_ASSIGNMENT_LIST;
export const IMPORT_VALID_URL = ProductAssignmentAPI.IMPORT_PRODUCT_ASSIGNMENT_VALID_URL;
export const GET_AUTO_CREATE_REPORTS_LIST = ProductAssignmentAPI.GET_AUTO_CREATE_REPORTS_LIST;
export const REQUEST_ORG_LEVEL = HierarchyAPI.REQUEST_ORG_LEVEL_WITH_PARENTS_GUID;
export const SELECT_ALL = ProductAssignmentAPI.SELECT_ALL_PRODUCT_ASSIGNMENT;
export const UPDATE = ProductAssignmentAPI.UPDATE_PRODUCT_ASSIGNMENT;
export const URL = ProductAssignmentAPI.GET_PRODUCT_ASSIGNMENTS;

// Dropdowns
export const REQUEST_CROP = CropAPI.REQUEST_CROP;
export const REQUEST_CROP_CLASS = CropAPI.REQUEST_CROP_CLASS;
export const REQUEST_GROWTH_STAGE_ORDER = GrowthStageOrderAPI.REQUEST_TISSUE_GROWTH_STAGE_ORDER;
export const REQUEST_NUTRIENT = ProductAssignmentAPI.REQUEST_NUTRIENT;
export const REQUEST_PRODUCT = ProductAssignmentAPI.REQUEST_PRODUCT;

// Default filter object
export const defaultRequestFilters = {
    [REQUEST_PAYLOAD_FILTER]: {
        CropClassName: "",
        CropName: "",
        LocationLevel: "",
        SampleTypeName: "",
        NutrientName: "",
        IsActive: ""
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
    [REQUEST_PAYLOAD_ACTIVE_ONLY]: true,
    userGuid: ""
};

export const defaultSort = {
    ...defaultRequestFilters[REQUEST_PAYLOAD_SORT_LIST][0],
    FieldName: "",
};

export const dropdowns = {
    [model.PROPS_CROP_NAME]: REQUEST_CROP,
    [model.PROPS_CROP_CLASS_NAME]: { url: REQUEST_CROP_CLASS, model: "00000000-0000-0000-0000-000000000000" },
    [model.PROPS_NUTRIENT_NAME]: REQUEST_NUTRIENT,
    [model.PROPS_ORG_LEVEL_LIST]: { url: REQUEST_ORG_LEVEL, model: "_" },
    [PRODUCT_DROPDOWN]: REQUEST_PRODUCT
};

export const pickLists = {
    [PICKLIST_CROP_PURPOSE]: getPickListCode(PICKLIST_CROP_PURPOSE),
    [PICKLIST_SAMPLE_TYPE]: getPickListCode(PICKLIST_SAMPLE_TYPE)
};

// Service
export const service = createService({
    guid: model.PROPS_PRODUCT_ASSIGNMENT_GUID,
    name: model.PROPS_SAMPLE_TYPE_NAME,
    modelName: MODEL_NAME,
    defaultRequestFilters,
    REQUEST_PAYLOAD_FILTER,
    REQUEST_PAYLOAD_SORT_LIST,
    REQUEST_PAYLOAD_PAGE_OPTIONS,
    EXPORT_FILE_NAME,
    dropdowns,
    pickLists,
    activeColumnName: adminData.PROPS_IS_ACTIVE,
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
        [model.PROPS_SAMPLE_TYPE_NAME]: { label: "sampleTypeName", gridCol: 15 },
        [model.PROPS_ORG_LEVEL_NAME]: { label: "orgLevelName", gridCol: 10, sortNameOverRide: "locationLevel" },
        [model.PROPS_NUTRIENT_NAME]: { label: "nutrientName", gridCol: 15 },
        [model.PROPS_PRODUCT_NAME]: { label: "productName", gridCol: 10 },
        [model.PROPS_CROP_NAME]: { label: "cropName", gridCol: 10 },
        [model.PROPS_CROP_PURPOSE_NAME]: { label: "cropPurposeName", gridCol: 10 },
        [model.PROPS_GROWTH_STAGE_ORDER_NAME]: { label: "growthStageOrderName", gridCol: 10 },
        [model.PROPS_IS_ACTIVE]: { label: "isActive", gridCol: 5, visible: false, className: "col-shift-15" },
        [model.PROPS_CAN_DELETE]: { label: "canDelete", gridCol: 5, className: "col-shift-15" }
    },
    getDefaultRecord: () => ({ ...defaultProductAssignmentRecord() }),
    defaultSort,
});