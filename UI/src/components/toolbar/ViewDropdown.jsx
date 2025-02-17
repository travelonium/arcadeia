/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Dropdown from 'react-bootstrap/Dropdown';
import Tooltip from 'react-bootstrap/Tooltip';
import Button from 'react-bootstrap/Button';
import React, { Component } from 'react';
import { isEqual } from 'lodash';
import cx from 'classnames';

export class ViewDropdown extends Component {

    static displayName = ViewDropdown.name;

    constructor(props) {
        super(props);
        this.state = {
            open: false,
            views: {
                "card": "Cards",
                "thumbnail": "Thumbnails",
            }
        };
    }

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    onSelect(eventKey, event) {
        let value = this.props.value;
        if (this.state.views[eventKey] !== undefined) {
            value = eventKey;
        }
        if (this.props.onChange && !isEqual(this.props.value, value)) {
            this.props.onChange(value, event);
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
        const icon = (this.props.value === "card") ? "bi-person-vcard" : "bi-card-image";
        return (
            <Dropdown className={cx("view-dropdown d-inline", this.props.className, this.props.overridden ? "overridden" : "")} autoClose="outside" onSelect={this.onSelect.bind(this)} onToggle={this.onToggle.bind(this)}>
                <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                    (this.props.tooltip && !this.state.open) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
                }>
                    <Dropdown.Toggle id="dropdown-autoclose-outside" className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary" disabled={disabled}>
                        <i className={cx("icon bi set", icon, this.props.name)}></i>
                    </Dropdown.Toggle>
                </OverlayTrigger>
                <Dropdown.Menu className="position-absolute">
                {
                    Object.keys(this.state.views).map((view) => {
                        return (
                            <Dropdown.Item key={view} eventKey={view} href="#" active={this.props.value === view}>
                                <span className="d-flex align-items-center">
                                    <i className={cx("bi pe-3", (view === "card") ? "bi-person-vcard" : "bi-card-image")}></i>{this.state.views[view]}{(this.props.value === view) ? <i className={cx("icon bi bi-check ms-auto set")}></i> : <></>}
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
