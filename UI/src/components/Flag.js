import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Tooltip from 'react-bootstrap/Tooltip';
import Button from 'react-bootstrap/Button';
import React, { Component } from 'react';

export class Flag extends Component {

    static displayName = Flag.name;

    constructor(props) {
        super(props);
        this.state = {
            value: this.props.default ?? false
        };
    }

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    onToggle(event) {
        event.stopPropagation();
        event.persist();
        this.setState({
            value: !this.state.value,
        }, () => {
            if (this.props.onChange) {
                this.props.onChange(this.state.value, event);
            }
        })
    }

    render() {
        let icon = this.state.value ? this.props.set : this.props.unset;
        return (
            <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                (this.props.tooltip) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
            }>
                {
                    (this.props.button) ?
                    <Button className={"border-0 shadow-none" + (this.props.className ? ` ${this.props.className}` : "")} variant="outline-secondary" onClick={this.onToggle.bind(this)}>
                        <i className={["flag", "bi", icon, this.props.name, (this.state.value ? "set" : "")].join(" ")}></i>
                    </Button> :
                    <i className={this.props.className ? ` ${this.props.className}` : null} onClick={this.onToggle.bind(this)} className={["flag", "bi", icon, this.props.name, (this.state.value ? "set" : "")].join(" ")}></i>
                }
            </OverlayTrigger>
        );
    }
}
