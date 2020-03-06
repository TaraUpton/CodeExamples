import React, { Component } from "react";
import PropTypes from "prop-types";
import CustomPropTypes from "~/utils/proptypes";
import { injectIntl, intlShape } from "react-intl";
import {
    model,
    service
} from "./../data";
import {
    getAgBytesErrorClassNames,
    handlePicklistChange,
    mapToPicklistValue,
    onTextChange,
    prepareSelectableOptions
} from "~/admin/utils";
import { messages } from "../../i18n-messages";
import {
    adminData,
    GUID,
    NAME,
} from "~/admin/data";
// Components
import {
    Section,
    SelectInput,
    SubSection
} from "~/core";
import {
    OrgLevelList,
    PROPS_STATE_ABBREVIATION
} from "~/admin/agBytes/components/org-level-list";
// Style
import "../../add-edit-shared-responsive.css";
import {
    colorOptionRenderer,
    getBackgroundGradientStyle
} from "~/action-panel/components/layer-module/utils";
export class AddEditPanel extends Component {

    static propTypes = {
        addEditPanel: PropTypes.object.isRequired,
        apiErrors: PropTypes.array,
        apiErrorModel: PropTypes.object,
        colorRamps: PropTypes.array.isRequired,
        companyList: PropTypes.array,
        fetchData: PropTypes.bool,
        fetchDropdownData: PropTypes.func,
        fetchRecord: PropTypes.func,
        fetchUnitData: PropTypes.func,
        fetchCompanyList: PropTypes.func,
        getNextId: PropTypes.func,
        importExportHeaderTitle: PropTypes.func,
        intl: intlShape.isRequired,
        liftRecordData: PropTypes.func,
        needs: PropTypes.func,
        nextId: PropTypes.number,
        numberOfClassesOptions: PropTypes.array.isRequired,
        record: CustomPropTypes.systemDefaults,
        recordGuid: PropTypes.string,
        setBreadcrumbs: PropTypes.func,
        userRole: PropTypes.object.isRequired
    }

    constructor(props) {
        super(props);
        this.surfaceDefaults = {};
        this.state = {
            [model.PROPS_GROWTH_STAGE_ORDER]: [],
            [model.PROPS_ORG_LEVEL_LIST]: [],
            colorRampGuid: "",
            numberOfClasses: "",
            selectedCompany: {},
            selectedCompanyList: null
        };
    }

    componentDidMount() {
        this.props.setBreadcrumbs([""]);
        this.surfaceDefaults = service.getDefaultRecord();
        const { needs } = this.props;
        needs([
            this.props.fetchCompanyList(),
            this.props.fetchDropdownData()
        ]);

        if (this.props.recordGuid) {
            needs([
                this.props.fetchRecord(this.props.recordGuid)
            ]);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (nextProps.fetchData) {
            this.props.liftRecordData(this.surfaceDefaults);
        }
        if (nextProps.addEditPanel.mode === "ADD") {
            if (nextProps.nextId) {
                this.setState({
                    nextId: nextProps.nextId
                });
            }
        } else {
            if (nextProps.record && (nextProps.record !== this.props.record)) {
                this.surfaceDefaults = { ...this.surfaceDefaults, ...nextProps.record };
                this.setState({
                    colorRampGuid: nextProps.record.colorRampGuid || "",
                    numberOfClasses: nextProps.record.numberOfClasses || ""
                });
            }
        }
        if (nextProps.companyList !== this.props.companyList) {
            this.setState({
                companyList: nextProps.companyList.map(({ name, guid }) => ({
                    label: name,
                    value: guid
                }))
            });

        }

        this._initializeDropdowns(nextProps);
    }

    _initializeDropdowns = (nextProps) => {
        this._initializeClassificationMethods(nextProps);
        this._initializeSystemAttributes(nextProps);
        this._initializeOrgLevel(nextProps);
    }

    _initializeSystemAttributes = (nextProps) => {
        if ((this.surfaceDefaults[model.PROPS_SYSTEM_ATTRIBUTE_GUID] != null) &&
            nextProps[model.PROPS_SYSTEM_ATTRIBUTE_NAME]) {
            this.setState({
                [model.PROPS_SYSTEM_ATTRIBUTE_NAME]: prepareSelectableOptions(
                    nextProps[model.PROPS_SYSTEM_ATTRIBUTE_NAME],
                    { guid: GUID, label: NAME },
                    this.surfaceDefaults[model.PROPS_SYSTEM_ATTRIBUTE_GUID]
                )
            });
        }
    };

    _initializeClassificationMethods = (nextProps) => {
        if ((this.surfaceDefaults[model.PROPS_CLASSIFICATION_METHOD_GUID] != null) &&
            nextProps[model.PROPS_CLASSIFICATION_METHOD_NAME]) {
            this.setState({
                [model.PROPS_CLASSIFICATION_METHOD_NAME]: prepareSelectableOptions(
                    nextProps[model.PROPS_CLASSIFICATION_METHOD_NAME],
                    { guid: GUID, label: NAME },
                    this.surfaceDefaults[model.PROPS_CLASSIFICATION_METHOD_GUID]
                )
            });
        }
    };

    _initializeOrgLevel = (nextProps) => {
        if (this.surfaceDefaults[model.PROPS_ORG_LEVEL_LIST] != null && nextProps[model.PROPS_ORG_LEVEL_LIST]) {
            this.setState({
                [model.PROPS_ORG_LEVEL_LIST]: nextProps[model.PROPS_ORG_LEVEL_LIST] || []
            });
        }
    };

    _onPicklistChange = ({ type, guid }, value, callback) => {
        this.surfaceDefaults = handlePicklistChange(this.surfaceDefaults, { type, guid, value }, callback);
    }

    _onTextChange = (formKey, value, callback) => {
        this.surfaceDefaults = onTextChange(this.surfaceDefaults, { formKey: [formKey], value }, callback);
    }

    _updateColorRamp(colorRampGuid) {
        this.setState({ colorRampGuid }, () => this._updateColorRampSelect());
    }

    _updateColorRampSelect(colorRampGuid = this.state.colorRampGuid) {
        const colorRamp = this.props.colorRamps.find(cs => cs.colorRampGuid === colorRampGuid);
        if (this.colorRampSelect && colorRamp) {
            this.colorRampSelect.getInstance().textInput.input.style.backgroundImage =
                getBackgroundGradientStyle(colorRamp.layerFiles.slice(-1)[0]);
        }
    }

    _updateColorSelects() {
        this._updateColorRampSelect();
    }

    _updateNumClasses(numberOfClasses) {
        this.setState({ numberOfClasses });
    }

    _renderSurfaceDefaultsInfo = () => {
        const { formatMessage } = this.props.intl;
        const { surfaceDefaults } = this;
        const { apiErrors,
            colorRamps,
            numberOfClassesOptions
            } = this.props;
        const {
            colorRampGuid,
            numberOfClasses
        } = this.state;


        const colorRampSelectOptions = colorRamps.map(cr => ({
            background: getBackgroundGradientStyle(cr.layerFiles.slice(-1)[0]),
            label: " ",
            value: cr.colorRampGuid
        }));

        const numClassesOptions = numberOfClassesOptions.map(noc => ({
            label: noc,
            value: noc
        }));
        numClassesOptions.shift(); // Remove first item in the list
        const colorRamp = this.props.colorRamps.find(cs => cs.colorRampGuid === colorRampGuid);
        return (
            <div className="section-container">
                <Section>
                    <SubSection>
                        <SelectInput
                            filterable={false}
                            onChange={e => this._updateColorRamp(e)}
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            options={colorRampSelectOptions}
                            optionRenderer={colorOptionRenderer}
                            placeholderText={formatMessage(messages.colorRampName)}
                            ref={ref => this.colorRampSelect = ref}
                            value={colorRampGuid}
                            clearable={false}
                            required
                        />
                        <SelectInput
                            filterable={false}
                            onChange={e => this._updateNumClasses(e)}
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            options={numClassesOptions}
                            placeholderText={formatMessage(messages.numberOfClasses)}
                            value={numberOfClasses}
                            clearable={false}
                            required
                        />
                    </SubSection>
                    <SubSection>
                        <SelectInput
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            options={this.state[model.PROPS_SYSTEM_ATTRIBUTE_NAME]}
                            autofocus
                            value={mapToPicklistValue({
                                options: this.state[model.PROPS_SYSTEM_ATTRIBUTE_NAME],
                                selectedGuid: surfaceDefaults[model.PROPS_SYSTEM_ATTRIBUTE_GUID]
                            })}
                            onChange={(value) => {
                                this._onPicklistChange({
                                    type: model.PROPS_SYSTEM_ATTRIBUTE_NAME,
                                    guid: model.PROPS_SYSTEM_ATTRIBUTE_GUID
                                }, value);
                            }}
                            placeholderText={formatMessage(messages.systemAttributeName)}
                            containerClassNames={[getAgBytesErrorClassNames(905, apiErrors)]}
                            clearable={false}
                            required
                        />
                        <SelectInput
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            options={this.state[model.PROPS_CLASSIFICATION_METHOD_NAME]}
                            autofocus
                            value={mapToPicklistValue({
                                options: this.state[model.PROPS_CLASSIFICATION_METHOD_NAME],
                                selectedGuid: surfaceDefaults[model.PROPS_CLASSIFICATION_METHOD_GUID]
                            })}
                            onChange={(value) => {
                                this._onPicklistChange({
                                    type: model.PROPS_CLASSIFICATION_METHOD_NAME,
                                    guid: model.PROPS_CLASSIFICATION_METHOD_GUID
                                }, value);
                            }}
                            placeholderText={formatMessage(messages.classificationMethodName)}
                            containerClassNames={[getAgBytesErrorClassNames(906, apiErrors)]}
                            clearable={false}
                            required
                        />
                        </SubSection>
                    </Section>
            </div>
        );
    }

    _renderDetailInfo2 = () => {
        const { surfaceDefaults } = this;
        const { formatMessage } = this.props.intl;
        console.warn("render this.state.companyList", this.state.userCompanyList);
        return (
            <Section required className="grid-section" headerText={formatMessage(messages.orgLevelList)}>
                {this.state.companyList && this.state.companyList.length === 1 ? null
                    : <SubSection>
                        <div className="select-company-container">
                            <span>{formatMessage(messages.selectACompanyMessage)}</span>
                            <SelectInput
                                required
                                clearable={false}
                                optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                                placeholderText={formatMessage(messages.companyName)}
                                value={this.state.selectedCompany}
                                options={this.state.companyList}
                                onChange={this.onCompanyNameChange}
                            />
                        </div>
                    </SubSection>
                }
                {!this.state[model.PROPS_ORG_LEVEL_LIST] ? null
                    : <OrgLevelList
                        apiErrors={this.props.apiErrors}
                        itemList={this.state[model.PROPS_ORG_LEVEL_LIST]}
                        record={surfaceDefaults[model.PROPS_ORG_LEVEL_LIST]}
                        onSelectionChange={(value) => {
                            this._onTextChange(model.PROPS_ORG_LEVEL_LIST, value, () => this.forceUpdate());
                        }}
                        statePropName={PROPS_STATE_ABBREVIATION}
                    />
                }
            </Section>
        );
    }


    render() {
        return (
            <div className="add-edit-panel">
                {this._renderSurfaceDefaultsInfo()}
                <div className="section-container">
                    <span className="bar section-fence"/>
                    {this._renderDetailInfo2()}
                    <span className="bar section-fence"/>
                </div>
            </div>
        );
    }
}

export default injectIntl(AddEditPanel);
