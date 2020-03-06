import { createAction } from "redux-act";

const BASE_STR = "SURFACE_DEFAULTS";

export const FETCH_COMPANY_LIST_DATA = `${BASE_STR}_FETCH_COMPANY_LIST_DATA`;
export const FETCH_COMPANY_LIST_SUCCESS = `${BASE_STR}_FETCH_COMPANY_LIST_SUCCESS`;
export const FETCH_COMPANY_LIST_FAILED = `${BASE_STR}_FETCH_COMPANY_LIST_FAILED`;

export const fetchCompanyList = createAction(FETCH_COMPANY_LIST_DATA);
export const fetchCompanyListSuccess = createAction(FETCH_COMPANY_LIST_SUCCESS);
export const fetchCompanyListFailed = createAction(FETCH_COMPANY_LIST_FAILED);
