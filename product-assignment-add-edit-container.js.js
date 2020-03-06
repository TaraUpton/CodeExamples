import { connect } from "react-redux";
import AddEditPanelContainer from "./../../../containers/add-edit-panel";
import AddEditPanel from "./add-edit-panel";
import { dropdowns, model, pickLists, service } from "./../data";
import { fetchDropdownData } from "~/core/dropdowns/actions";
import { getDropdown } from "~/admin/selectors";

const mapStateToProps = (state) => ({
    [model.PROPS_GROWTH_STAGE_ORDER]: getDropdown(model.PROPS_GROWTH_STAGE_ORDER, state),
});

const mapDispatchToProps = () => ({
    fetchGrowthStage: (v) => fetchDropdownData(v)
});

export default connect(mapStateToProps, mapDispatchToProps)(AddEditPanelContainer(AddEditPanel, { dropdowns, pickLists, service }));
