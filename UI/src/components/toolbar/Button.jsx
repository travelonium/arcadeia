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
import ReactBootstrapButton from 'react-bootstrap/Button';
import Tooltip from 'react-bootstrap/Tooltip';
import React, { Component } from 'react';
import cx from 'classnames';

export class Button extends Component {

    static displayName = Button.name;
    static defaultProps = {
        true: true,
        false: false
    }

    constructor(props) {
        super(props);
        this.state = {};
    }

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    onClick(event) {
        event.preventDefault();
        event.stopPropagation();
        event.persist();
        if (this.props.onClick) this.props.onClick();
    }

    render() {
        return (
            <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                (this.props.tooltip) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
            }>
                <ReactBootstrapButton className={cx(this.props.className, "button position-relative border-0 shadow-none")} variant="outline-secondary" onClick={this.onClick.bind(this)}>
                    <i className={cx("bi", this.props.icon, this.props.name)} />
                    {this.props.children}
                </ReactBootstrapButton>
            </OverlayTrigger>
        );
    }
}
