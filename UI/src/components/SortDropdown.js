import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Dropdown from 'react-bootstrap/Dropdown';
import Tooltip from 'react-bootstrap/Tooltip';
import React, { Component } from 'react';
import cx from 'classnames';
import _ from 'lodash';

export class SortDropdown extends Component {

    static displayName = SortDropdown.name;

    constructor(props) {
        super(props);
        this.state = {
            fields: {
                "name": "Name",
                "type extension": "Type",
                "size": "Size",
                "duration": "Duration",
                "dateAdded": "Date Added",
                "dateCreated": "Date Created",
                "dateModified": "Date Modified",
                "dateTaken": "Date Taken",
            },
            directions: {
                "asc": "Ascending",
                "desc": "Descending",
            }
        };
    }

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    onSelect(eventKey, event) {
        let value = {
            ...this.props.value,
        };
        if (this.state.directions[eventKey] !== undefined) {
            value['direction'] = eventKey;
        }
        if (this.state.fields[eventKey] !== undefined) {
            value['field'] = (this.props.value.field === eventKey) ? "" : eventKey;
        }
        if (this.props.onChange && !_.isEqual(this.props.value, value)) {
            this.props.onChange(value, event);
        }
    }

    render() {
        let set = (this.props.value.field && this.props.value.direction) ? "set" : "";
        let icon = (this.props.value.direction === "desc") ? "bi-sort-down" : "bi-sort-down-alt";
        return (
            <OverlayTrigger key={this.props.name} placement="left" overlay={
                (this.props.tooltip) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
            }>
                <Dropdown className={cx("sort-dropdown d-inline", this.props.className)} autoClose="outside" onSelect={this.onSelect.bind(this)}>
                    <Dropdown.Toggle id="dropdown-autoclose-outside" className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary">
                        <i className={cx("icon bi", icon, this.props.name, set)}></i>
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                    {
                        Object.keys(this.state.fields).map((field) => <Dropdown.Item key={field} eventKey={field} href="#" active={this.props.value.field === field}><span className="d-flex">{this.state.fields[field]}{(this.props.value.field === field) ? <i className={cx("icon bi bi-check ms-auto set")}></i> : <></>}</span></Dropdown.Item>)
                    }
                        <Dropdown.Divider />
                    {
                        Object.keys(this.state.directions).map((direction) => <Dropdown.Item key={direction} eventKey={direction} href="#" disabled={this.props.value.field ? false : true} active={this.props.value.direction === direction}><span className="d-flex">{this.state.directions[direction]}{(this.props.value.direction === direction) ? <i className={cx("icon bi bi-check ms-auto set")}></i> : <></>}</span></Dropdown.Item>)
                    }
                    </Dropdown.Menu>
                </Dropdown>
            </OverlayTrigger>
        );
    }
}
