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
                <ReactBootstrapButton className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary" onClick={this.onClick.bind(this)}>
                    <i className={cx("button bi", this.props.icon, this.props.name)}></i>
                </ReactBootstrapButton>
            </OverlayTrigger>
        );
    }
}
