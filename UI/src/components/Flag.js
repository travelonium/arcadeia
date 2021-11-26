import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Tooltip from 'react-bootstrap/Tooltip';
import Button from 'react-bootstrap/Button';
import React, { Component } from 'react';
import cx from 'classnames';

export class Flag extends Component {

    static displayName = Flag.name;

    constructor(props) {
        super(props);
        this.state = {};
    }

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    onToggle(event) {
        event.preventDefault();
        event.stopPropagation();
        event.persist();
        if (this.props.onChange) {
            this.props.onChange(!this.props.value, event);
        }
    }

    render() {
        let icon = this.props.value ? this.props.set : this.props.unset;
        return (
            <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                (this.props.tooltip) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
            }>
                {
                    (this.props.button) ?
                    <Button className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary" onClick={this.onToggle.bind(this)}>
                        <i className={cx("flag bi", icon, this.props.name, (this.props.value ? "set" : ""))}></i>
                    </Button> :
                    <i className={cx(this.props.className, "flag bi", icon, this.props.name, (this.props.value ? "set" : ""))} onClick={this.onToggle.bind(this)}></i>
                }
            </OverlayTrigger>
        );
    }
}
