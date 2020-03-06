import React, { Component } from "react";
import PropTypes from "prop-types";
import { service, actions } from "./log-data";
import { injectIntl, intlShape } from "react-intl";
import { withCrud } from "~/hocs";
import { DataTable } from "~/core";
import { FormattingHelpers } from "~/utils/helpers";
import { messages } from "../i18n-messages";

export class ProductAssignmentLog_ extends Component {
    static propTypes = {
        records: PropTypes.array,
        intl: intlShape.isRequired
    }
    UNSAFE_componentWillReceiveProps(nextProps) {
        if (nextProps.records) {
            for (let item of nextProps.records) {
                FormattingHelpers.formatLogDetailText(item);
            }
        }
    }
    render() {
        return (
            <DataTable
                service={service}
                isCheckbox={false}
                showFilterInput={false}
                messages={messages}
                {...this.props}
            />
        );
    }
}
export const ProductAssignmentLog = injectIntl(withCrud(ProductAssignmentLog_, service, actions));
