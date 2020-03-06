import { connect } from "react-redux";
import AddEditPanelContainer from "./../../../containers/add-edit-panel";
import AddEditPanel from "./add-edit-panel";
import {
    dropdowns,
    service
} from "./../data";
import * as selectors from "~/action-panel/components/layer-module/components/layer-list/selectors";
import { fetchCompanyList } from "~/admin/setup/surface-defaults/data/actions";
import { getCompanyList } from "~/admin/setup/surface-defaults/data/selectors";
const mapStateToProps = (state) => ({
    colorRamps: selectors.getColorRamps(state),
    numberOfClassesOptions: selectors.getNumberOfClasses(state),
    companyList: getCompanyList(state),
});

const mapDispatchToProps = () => ({
    fetchCompanyList: () => fetchCompanyList()
});

export default connect(mapStateToProps, mapDispatchToProps)(AddEditPanelContainer(AddEditPanel, { dropdowns, service }));
