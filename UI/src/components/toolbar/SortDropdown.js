import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import { ReactSortable } from "react-sortablejs";
import Dropdown from 'react-bootstrap/Dropdown';
import Tooltip from 'react-bootstrap/Tooltip';
import Button from 'react-bootstrap/Button';
import React, { Component } from 'react';
import { clone } from './../../utils';
import { isEqual } from 'lodash';
import cx from 'classnames';


export class SortDropdown extends Component {

    static displayName = SortDropdown.name;

    constructor(props) {
        super(props);
        this.state = {
            open: false,
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
        let value = clone(this.props.value);
        const index = value.fields.findIndex(field => field.id === eventKey);
        if (index !== -1) {
            let field = value.fields[index];
            field.active = (field.active !== undefined) ? !field.active : true;
        }
        if (this.state.directions[eventKey] !== undefined) {
            value.direction = eventKey;
        }
        if (this.props.onChange && !isEqual(this.props.value, value)) {
            this.props.onChange(value, event);
        }
    }

    onSetList(newState) {
        if (this.props.onChange && !isEqual(this.props.value.fields, newState)) {
            let value = clone({
                ...this.props.value,
                fields: newState
            });
            this.props.onChange(value);
        }
    }

    onToggle(show) {
        this.setState({ open: show });
        return show;
    }

    onReset() {
        if (this.props.onReset) this.props.onReset();
    }

    render() {
        const disabled = this.props.disabled ?? false;
        const active = (this.props.value.fields.filter(field => field.active).length > 0) && this.props.value.direction;
        const set = active ? "set" : "";
        const icon = (this.props.value.direction === "desc") ? "bi-sort-down" : "bi-sort-down-alt";
        return (
            <Dropdown className={cx("sort-dropdown d-inline", this.props.className, this.props.overridden ? "overridden" : "")} autoClose="outside" onSelect={this.onSelect.bind(this)} onToggle={this.onToggle.bind(this)} >
                <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                    (this.props.tooltip && !this.state.open) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
                }>
                    <Dropdown.Toggle id="dropdown-autoclose-outside" className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary" disabled={disabled}>
                        <i className={cx("icon bi", icon, this.props.name, set)}></i>
                    </Dropdown.Toggle>
                </OverlayTrigger>
                <Dropdown.Menu className="position-absolute">
                    <ReactSortable list={clone(this.props.value.fields)} setList={this.onSetList.bind(this)}>
                    {
                        this.props.value.fields.map((field) => {
                            return (
                                <Dropdown.Item key={field.id} eventKey={field.id} href="#" active={field.active}>
                                    <span className="d-flex align-items-center">
                                    <i className="bi bi-grip-horizontal pe-3"></i>{field.name}{field.active ? <i className={cx("icon bi bi-check ms-auto set")}></i> : <></>}
                                    </span>
                                </Dropdown.Item>
                            )
                        })
                    }
                    </ReactSortable>
                    <Dropdown.Divider />
                {
                    Object.keys(this.state.directions).map((direction) => {
                        return (
                            <Dropdown.Item key={direction} eventKey={direction} href="#" disabled={!active} active={this.props.value.direction === direction}>
                                <span className="d-flex align-items-center">
                                    {this.state.directions[direction]}{(this.props.value.direction === direction) ? <i className={cx("icon bi bi-check ms-auto set")}></i> : <></>}
                                </span>
                            </Dropdown.Item>
                        )
                    })
                }
                {
                    (this.props.overridden) ? <>
                    <Dropdown.Divider />
                    <Dropdown.Item active={false}>
                        <div className="d-flex flex-column justify-content-center">
                            <Button onClick={this.onReset.bind(this)} variant="secondary">Reset</Button>
                        </div>
                    </Dropdown.Item>
                    </> : <></>
                }
                </Dropdown.Menu>
            </Dropdown>
        );
    }
}
