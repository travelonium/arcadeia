import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Tooltip from 'react-bootstrap/Tooltip';
import Button from 'react-bootstrap/Button';
import React, { Component } from 'react';
import cx from 'classnames';

export class Flag extends Component {

    static displayName = Flag.name;
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

    onToggle(event) {
        event.preventDefault();
        event.stopPropagation();
        event.persist();
        if (this.props.onChange) {
            this.props.onChange((this.props.value === this.props.true) ? this.props.false : this.props.true, event);
        }
    }

    render() {
        const disabled = this.props.disabled ?? false;
        const icon = (this.props.value === this.props.true) ? this.props.set : this.props.unset;
        return (
            <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                (this.props.tooltip) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
            }>
                {
                    (this.props.button) ?
                    <Button className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary" onClick={this.onToggle.bind(this)} disabled={disabled}>
                        <i className={cx("flag bi", icon, this.props.name, ((this.props.value === this.props.true) ? "set" : ""))}></i>
                    </Button> :
                    <i className={cx(this.props.className, "flag bi", icon, this.props.name, ((this.props.value === this.props.true) ? "set" : ""), disabled ? "disabled" : "")} onClick={!disabled ? this.onToggle.bind(this) : undefined}></i>
                }
            </OverlayTrigger>
        );
    }
}
