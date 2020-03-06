import {
    all,
    call,
    fork,
    put,
    select,
    takeEvery
} from "redux-saga/effects";
import {
    fetchCompanyList,
    fetchCompanyListSuccess,
} from "./actions";
import { Request } from "~/utils/request";
import { SurfaceDefaultsAPI } from "~/utils/api";
// selectors
import { getTheUserGuid } from "~/login/selectors";
import { actions as notificationActions } from "~/notifications";
import { messages } from "../../i18n-messages";

function* processFetchCompanyList(action) {
    const UserGuid = yield select(getTheUserGuid);
    const requestOptions = { UserGuid };
    try {
        const companyList = yield call(Request.post, SurfaceDefaultsAPI.GET_COMPANY_LIST, requestOptions);
        yield put(fetchCompanyListSuccess({
            companyList
        }));
    } catch (error) {
        yield put(notificationActions.apiCallError(error, action, messages.fetchCompanyListError));
    }
}

export function* watchFetchCompanyList() {
    yield takeEvery(fetchCompanyList, processFetchCompanyList);
}

export default function *() {
    yield all([
        fork(watchFetchCompanyList)
    ]);
}
