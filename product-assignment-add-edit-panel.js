import React, { Component } from "react";
import PropTypes from "prop-types";
import CustomPropTypes from "~/utils/proptypes";
import { injectIntl, intlShape } from "react-intl";
import {
    model,
    PRODUCT_DROPDOWN,
    REQUEST_CROP_CLASS,
    REQUEST_GROWTH_STAGE_ORDER,
    service
} from "./../data";
import {
    ProductList,
} from "~/admin/setup/equation-list/components";
import {
    getAgBytesErrorClassNames,
    handlePicklistChange,
    mapToPicklistValue,
    onTextChange,
    prepareSelectableOptions
} from "~/admin/utils";
import { messages } from "../../i18n-messages";
import {
    ADD,
    adminData,
    GUID,
    ID,
    NAME,
    PICKLIST_GUID,
    VALUE
} from "~/admin/data";
// Components
import { Checkbox, Section, SelectInput, SubSection } from "~/core";
import * as picklistService from "~/core/picklist/picklist-names";
import CropList from "~/admin/agBytes/components/crop-info-list";
import { OrgLevelList, PROPS_STATE_ABBREVIATION } from "~/admin/agBytes/components/org-level-list";
// Style
import "../../add-edit-shared-responsive.css";

const NUTRIENT_GUID = "nutrientGuid";
const NUTRIENT_NAME = "nutrientName";
const NUTRIENT_ID = "nutrientId";

export class AddEditPanel extends Component {

    static propTypes = {
        addEditPanel: PropTypes.object.isRequired,
        apiErrors: PropTypes.array,
        apiErrorModel: PropTypes.object,
        fetchData: PropTypes.bool,
        fetchDropdownData: PropTypes.func,
        fetchGrowthStage: PropTypes.func,
        fetchPicklistData: PropTypes.func,
        fetchRecord: PropTypes.func,
        fetchUnitData: PropTypes.func,
        getNextId: PropTypes.func,
        importExportHeaderTitle: PropTypes.func,
        intl: intlShape.isRequired,
        liftRecordData: PropTypes.func,
        needs: PropTypes.func,
        nextId: PropTypes.number,
        record: CustomPropTypes.productAssignment,
        recordGuid: PropTypes.string,
        setBreadcrumbs: PropTypes.func,
        userRole: PropTypes.object.isRequired
    }

    constructor(props) {
        super(props);
        this.productAssignment = {};
        this.state = {
            [model.PROPS_GROWTH_STAGE_ORDER]: [],
            [model.PROPS_ORG_LEVEL_LIST]: []
        };
    }

    componentDidMount() {
        this.props.setBreadcrumbs([""]);
        this.productAssignment = service.getDefaultRecord();
        const { needs } = this.props;
        needs([
            this.props.fetchPicklistData(),
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
            this.props.liftRecordData(this.productAssignment);
        }
        if (nextProps[model.PROPS_GROWTH_STAGE_ORDER] === this.state[model.PROPS_GROWTH_STAGE_ORDER]) {
            this.setState({
                [model.PROPS_GROWTH_STAGE_ORDER]: nextProps[model.PROPS_GROWTH_STAGE_ORDER]
            });
        }
        if (nextProps.addEditPanel.mode === "ADD") {
            if (nextProps.nextId) {
                this.setState({
                    nextId: nextProps.nextId
                });
            }
        } else {
            if (nextProps.record && (nextProps.record !== this.props.record)) {
                this.productAssignment = { ...this.productAssignment, ...nextProps.record };
            }
        }

        this.initializeDropdowns(nextProps);
    }

    fetchCropClassData = (cropGuid) => {
        this.props.needs([
            this.props.fetchDropdownData({
                [model.PROPS_CROP_CLASS_NAME]: { url: REQUEST_CROP_CLASS, model: cropGuid },
            })
        ]);
    };

    fetchGrowthStageData = (cropGuid) => {
        this.props.needs([
            this.props.fetchGrowthStage({
                [model.PROPS_GROWTH_STAGE_ORDER]: { url: REQUEST_GROWTH_STAGE_ORDER, model: cropGuid },
            })
        ]);
    };

    getUpdatedLists = (cropGuid) => {
        if (cropGuid) {
            this.props.needs([
                this.props.fetchDropdownData({
                    [model.PROPS_CROP_CLASS_NAME]: {
                        url: REQUEST_CROP_CLASS,
                        model: cropGuid
                    }
                }),
                this.props.fetchGrowthStage({
                    [model.PROPS_GROWTH_STAGE_ORDER]: {
                        url: REQUEST_GROWTH_STAGE_ORDER,
                        model: cropGuid
                    }
                })
            ]);
        } else {
            this.setState({
                [model.PROPS_GROWTH_STAGE_ORDER]: [],
                [model.PROPS_CROP_CLASS_NAME]: []
            });
            this.productAssignment[model.PROPS_GROWTH_STAGE_ORDER] = "";
            this.productAssignment[model.PROPS_CROP_CLASS_NAME] = "";
        }
    };

    initializeCropClassName = (nextProps) => {
        if ((this.productAssignment[model.PROPS_CROP_LIST] != null) &&
            nextProps[model.PROPS_CROP_CLASS_NAME]) {
            this.setState({
                [model.PROPS_CROP_CLASS_NAME]: nextProps[model.PROPS_CROP_CLASS_NAME]
            });
        }
    };

    initializeCropPurpose = (nextProps) => {
        if ((this.productAssignment[model.PROPS_CROP_LIST] != null) &&
            nextProps[picklistService.PICKLIST_CROP_PURPOSE]) {
            this.setState({
                [picklistService.PICKLIST_CROP_PURPOSE]: prepareSelectableOptions(
                    nextProps[picklistService.PICKLIST_CROP_PURPOSE],
                    { guid: PICKLIST_GUID, label: VALUE, id: ID }
                )
            });
        }
    };

    initializeCrops = (nextProps) => {
        if ((this.productAssignment[model.PROPS_CROP_LIST] != null) &&
            nextProps[model.PROPS_CROP_NAME]) {
            this.setState({
                [model.PROPS_CROP_NAME]: nextProps[model.PROPS_CROP_NAME],
                [model.PROPS_PREVIOUS_CROP]: prepareSelectableOptions(
                    nextProps[model.PROPS_CROP_NAME],
                    { guid: GUID, label: NAME, id: ID }
                ),
                [model.PROPS_NEXT_CROP]: prepareSelectableOptions(
                    nextProps[model.PROPS_CROP_NAME],
                    { guid: GUID, label: NAME, id: ID }
                )
            });
        }
    };

    initializeDropdowns = (nextProps) => {
        this.initializeCrops(nextProps);
        this.initializeCropPurpose(nextProps);
        this.initializeCropClassName(nextProps);
        this.initializeGrowthStageOrder(nextProps);
        this.initializeNutrients(nextProps);
        this.initializeProducts(nextProps);
        this.initializeSampleType(nextProps);
        this.initializeOrgLevel(nextProps);
    }

    initializeGrowthStageOrder = (nextProps) => {
        if ((this.productAssignment[model.PROPS_CROP_LIST] != null) &&
            nextProps[model.PROPS_GROWTH_STAGE_ORDER]) {
            this.setState({
                [model.PROPS_GROWTH_STAGE_ORDER]: nextProps[model.PROPS_GROWTH_STAGE_ORDER]
            });
        }
    };

    initializeNutrients = (nextProps) => {
        if ((this.productAssignment[model.PROPS_NUTRIENT_GUID] != null) &&
            nextProps[model.PROPS_NUTRIENT_NAME]) {
            this.setState({
                [model.PROPS_NUTRIENT_NAME]: prepareSelectableOptions(
                    nextProps[model.PROPS_NUTRIENT_NAME],
                    { guid: NUTRIENT_GUID, label: NUTRIENT_NAME, id: NUTRIENT_ID, appendIdToLabel: true },
                    this.productAssignment[model.PROPS_NUTRIENT_GUID]
                )
            });
        }
    };

    initializeOrgLevel = (nextProps) => {
        if (this.productAssignment[model.PROPS_ORG_LEVEL_LIST] != null && nextProps[model.PROPS_ORG_LEVEL_LIST]) {
            this.setState({
                [model.PROPS_ORG_LEVEL_LIST]: nextProps[model.PROPS_ORG_LEVEL_LIST] || []
            });
        }
    };

    initializeProducts = (nextProps) => {
        if ((this.productAssignment[model.PROPS_PRODUCT_LIST] != null) &&
            nextProps[PRODUCT_DROPDOWN]) {
            this.setState({
                [PRODUCT_DROPDOWN]: nextProps[PRODUCT_DROPDOWN]
            });
        }
    };

    initializeSampleType = (nextProps) => {
        if ((this.productAssignment[model.PROPS_SAMPLE_TYPE_GUID] != null) &&
            nextProps[picklistService.PICKLIST_SAMPLE_TYPE]) {
            this.setState({
                [picklistService.PICKLIST_SAMPLE_TYPE]: prepareSelectableOptions(
                    nextProps[picklistService.PICKLIST_SAMPLE_TYPE],
                    { guid: PICKLIST_GUID, label: VALUE, id: ID },
                    this.productAssignment[model.PROPS_SAMPLE_TYPE_GUID]
                )
            });
        }
    };

    onPicklistChange = ({ type, guid }, value, callback) => {
        this.productAssignment = handlePicklistChange(this.productAssignment, { type, guid, value }, callback);
    }

    onTextChange = (formKey, value, callback) => {
        this.productAssignment = onTextChange(this.productAssignment, { formKey: [formKey], value }, callback);
    }

    renderProductAssignmentInfo = () => {
        const { formatMessage } = this.props.intl;
        const { productAssignment } = this;
        const { apiErrors, userRole } = this.props;
        return (
            <div className="section-container">
                <Section>
                    <SubSection>
                        <SelectInput
                            required
                            clearable={false}
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            containerClassNames={getAgBytesErrorClassNames(84, apiErrors)}
                            options={this.state[picklistService.PICKLIST_SAMPLE_TYPE]}
                            placeholderText={formatMessage(messages.sampleTypeName)}
                            value={mapToPicklistValue({
                                options: this.state[picklistService.PICKLIST_SAMPLE_TYPE],
                                selectedGuid: productAssignment[model.PROPS_SAMPLE_TYPE_GUID]
                            })}
                            onChange={(value) => {
                                this.onPicklistChange({
                                    type: model.PROPS_SAMPLE_TYPE_NAME,
                                    guid: model.PROPS_SAMPLE_TYPE_GUID
                                }, value);
                            }}
                        />
                        <SelectInput
                            optionIsHiddenKey={adminData.PROPS_ACTIVE_YN}
                            options={this.state[model.PROPS_NUTRIENT_NAME]}
                            autofocus
                            value={mapToPicklistValue({
                                options: this.state[model.PROPS_NUTRIENT_NAME],
                                selectedGuid: productAssignment[model.PROPS_NUTRIENT_GUID]
                            })}
                            onChange={(value) => {
                                this.onPicklistChange({
                                    type: model.PROPS_NUTRIENT_NAME,
                                    groupId: model.PROPS_NUTRIENT_ID,
                                    guid: model.PROPS_NUTRIENT_GUID
                                }, value);
                            }}
                            placeholderText={formatMessage(messages.nutrientIdName)}
                            containerClassNames={[getAgBytesErrorClassNames(85, apiErrors)]}
                            clearable={false}
                            required
                        />
                        </SubSection>
                    </Section>
                    <span className="no-bar section-fence"></span>
                        {
                            !userRole[model.PROPS_ACTIVE_INACTIVE] || this.props.addEditPanel.mode === ADD ? null
                                : <Section>
                                    <SubSection>
                                        <Checkbox
                                            onChange={(e, value) => {
                                                if (!value && this.productAssignment[model.PROPS_USER_ACTIVEYN]) {
                                                    this.showIsActiveDialog(true);
                                                    return;
                                                }
                                                this.onTextChange(model.PROPS_ACTIVE_YN, value);
                                            }}
                                            value={productAssignment[model.PROPS_ACTIVE_YN] != null ? productAssignment[model.PROPS_ACTIVE_YN] : true}
                                            label={formatMessage(messages.active)}
                                        />
                                    </SubSection>
                                </Section>
                        }
            </div>
        );
    }

    renderDetailInfo1 = () => {
        const { onTextChange, productAssignment, state } = this;
        const { formatMessage } = this.props.intl;
        return (
            <Section required className="grid-section" headerText={formatMessage(messages.productsHeader)}>
                <SubSection>
                    <ProductList
                        formatMessage={formatMessage}
                        options={state[PRODUCT_DROPDOWN]}
                        record={productAssignment[model.PROPS_PRODUCT_LIST]}
                        itemListAlias={model.PROPS_PRODUCT_LIST}
                        onTextChange={(e, value) => onTextChange(model.PROPS_PRODUCT_LIST, value)}
                    />
                </SubSection>
            </Section>
        );
    }

    renderDetailInfo2 = () => {
        const { productAssignment } = this;
        const { formatMessage } = this.props.intl;
        return (
            <Section required className="grid-section" headerText={formatMessage(messages.orgLevelList)}>
                {!this.state[model.PROPS_ORG_LEVEL_LIST] ? null
                    : <OrgLevelList
                        apiErrors={this.props.apiErrors}
                        itemList={this.state[model.PROPS_ORG_LEVEL_LIST]}
                        record={productAssignment[model.PROPS_ORG_LEVEL_LIST]}
                        onSelectionChange={(value) => {
                            this.onTextChange(model.PROPS_ORG_LEVEL_LIST, value, () => this.forceUpdate());
                        }}
                        statePropName={PROPS_STATE_ABBREVIATION}
                    />
                }
            </Section>
        );
    }

    renderDetailInfo3 = () => {
        const { formatMessage } = this.props.intl;
        const {
            fetchCropClassData,
            fetchGrowthStageData,
            getUpdatedLists,
            onTextChange,
            productAssignment,
            state
        } = this;
        const { apiErrors } = this.props;
        return (
            <Section className="grid-section" headerText={formatMessage(messages.cropPurposeList)}>
                <SubSection>
                    <CropList
                        apiErrors={apiErrors}
                        formatMessage={formatMessage}
                        getUpdatedLists={getUpdatedLists}
                        picklistOptions={{
                            [model.PROPS_CROP_NAME]: state[model.PROPS_CROP_NAME],
                            [model.PROPS_CROP_CLASS_NAME]: state[model.PROPS_CROP_CLASS_NAME],
                            [picklistService.PICKLIST_CROP_PURPOSE]: state[picklistService.PICKLIST_CROP_PURPOSE],
                            [model.PROPS_GROWTH_STAGE_ORDER]: state[model.PROPS_GROWTH_STAGE_ORDER]
                        }}
                        record={productAssignment[model.PROPS_CROP_LIST]}
                        itemListAlias={model.PROPS_CROP_LIST}
                        onTextChange={(e, value) => onTextChange(model.PROPS_CROP_LIST, value)}
                        fetchGrowthStageData={fetchGrowthStageData}
                        fetchCropClassData={fetchCropClassData}
                    />
                </SubSection>
            </Section>
        );
    }

    render() {
        return (
            <div className="add-edit-panel">
                {this.renderProductAssignmentInfo()}
                <div className="section-container">
                    {this.renderDetailInfo1()}
                    <span className="bar section-fence"/>
                    {this.renderDetailInfo2()}
                    <span className="bar section-fence"/>
                    {this.renderDetailInfo3()}
                </div>
            </div>
        );
    }
}

export default injectIntl(AddEditPanel);
