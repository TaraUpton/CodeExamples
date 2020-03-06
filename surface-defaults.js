import React, { Component } from "react";
import PropTypes from "prop-types";
import { connect } from "react-redux";
import { injectIntl, intlShape } from "react-intl";
import { service, actions, model } from "./data";
import { withMasked, withApiResult, withCrud } from "~/hocs";
import { preventBubbleUp } from "~/admin/utils";
import { defaultRequestFilters, REQUEST_PAYLOAD_FILTER } from "./data/service";
import { messages } from "../i18n-messages";

// Components
import AddEditPanel from "./add-edit/add-edit-container";
import ImportExportHeader from "../../agBytes/components/import-export-header";
import { DataTable, DialogBox, Button } from "~/core";
import SlidingPanel from "~/sliding-panel/sliding-panel";

import { SurfaceDefaultsLog } from "./surface-defaults-log";

export class SurfaceDefaults_ extends Component {
    static propTypes = {
        actions: PropTypes.object,
        addEditPanel: PropTypes.object,
        closeSidePanel: PropTypes.func,
        deleteSelected: PropTypes.func,
        fetchRecords: PropTypes.func,
        hierarchy: PropTypes.object,
        intl: intlShape.isRequired,
        needs: PropTypes.func,
        onFilterClear: PropTypes.func,
        onSubmit: PropTypes.func,
        records: PropTypes.array,
        userRole: PropTypes.object.isRequired
    }

    constructor(props) {
        super(props);
        this.state = {
            isModalOpen: false,
            records: [],
        };
        this.tableFooterOptions = [{
            label: "Delete Selected",
            action: this.props.deleteSelected
        }];
    }

    onToggleModalClick = () => {
        this.setState({ isModalOpen: !this.state.isModalOpen });
    }

    onFilterChange = (data) => {
        const { needs, actions } = this.props;
        const requestOptions = {
            ...defaultRequestFilters,
            [REQUEST_PAYLOAD_FILTER]: {
                ...defaultRequestFilters[REQUEST_PAYLOAD_FILTER],
                [model.PROPS_ORG_LEVEL_GUID]: data ? data.orgLevelGuid : null
            }
        };
        needs([
            actions.fetch(requestOptions)
        ]);
    }

    deleteSelected = (options) => {
        let { selectedItems } = options;
        selectedItems = selectedItems[0];
        options = { ...options, selectedItems };
        this.props.deleteSelected(options);
    }

    componentWillReceiveProps(nextProps) {
        if (nextProps.records !== this.props.records) {
            this.setState({
                records: nextProps.records
            });
        }
    }

    render() {
        const { showAddEditPanel } = this.props.addEditPanel;
        const { closeSidePanel, needs, userRole } = this.props;
        const { formatMessage } = this.props.intl;
        let slidingPanelProps = { ...this.props };
        if (showAddEditPanel) {
            slidingPanelProps = {
                ...slidingPanelProps,
                ...this.props.addEditPanel
            };
        }
        return (
            <div className="content-table-container">
                <ImportExportHeader
                    {...this.props}
                    service={service}
                    hideActiveRecordLink
                    onToggleModalClick={this.onToggleModalClick}
                />

                <DataTable
                    {...this.props}
                    isEditable
                    isCheckbox={userRole[model.PROPS_PERSON_IMPORT_EXPORT]}
                    service={service}
                    messages={messages}
                    records={this.state.records}
                    footerOptions={this.tableFooterOptions}
                />
                {!showAddEditPanel ? null :
                    <form onSubmit={(event) => preventBubbleUp(event)}>
                        <SlidingPanel
                            {...slidingPanelProps}
                            close={closeSidePanel}
                            component={AddEditPanel}
                            navigateTo={{ parentNameCode: "101", childNameCode: "255" }}
                        >
                            <Button type="save" forceSubmit onClick={() => this.props.onSubmit()} />
                            <Button type="cancel" onClick={() => this.props.closeSidePanel()} />
                        </SlidingPanel>
                    </form>
                }
                <DialogBox className="view-log-dialog-box" isOpen={this.state.isModalOpen} draggable unrestricted
                    onClose={() => this.onToggleModalClick()}
                    title={formatMessage(messages.surfaceDefaultsLogHistory)}>
                    <SurfaceDefaultsLog needs={needs} />
                </DialogBox>
            </div>
        );
    }
}


export const SurfaceDefaults = injectIntl(withMasked(withApiResult(withCrud(connect()(SurfaceDefaults_), service, actions), actions.importData)));

