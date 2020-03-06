import { createReducer } from "redux-act";
import {
    fetchCompanyListSuccess
} from "./actions";

export const SURFACE_DEFAULTS_DATA_KEY = "SURFACE_DEFAULTS_DATA";
const initialState = {
    companyList: null,
};

export const surfaceDefaultsReducer = createReducer({
    [fetchCompanyListSuccess]: (state, { companyList }) => ({
        ...state,
        companyList
    })
}, initialState);
