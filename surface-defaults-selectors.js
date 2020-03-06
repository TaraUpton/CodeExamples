import { ADMIN_STATE_KEY } from "~/admin";
import { SURFACE_DEFAULTS_DATA_KEY } from "./reducer";

const _getModuleState = (state) => state[ADMIN_STATE_KEY][SURFACE_DEFAULTS_DATA_KEY];

export const getCompanyList = (state) => _getModuleState(state).companyList;
