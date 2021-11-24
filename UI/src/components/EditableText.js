import React, { Component } from 'react';
import { extract } from '../utils';
import cx from 'classnames';

export class EditableText extends Component {

    static displayName = EditableText.name;

    constructor(props) {
        super(props);
        this.textArea = React.createRef();
        this.state = {
            editing: false,
            current: this.props.value,
            previous: this.props.value,
        };
    }

    componentDidUpdate(prevProps) {
        if (this.props.value !== prevProps.value) {
            this.setState({
                editing: false,
                current: this.props.value,
                previous: this.props.value,
            });
        }
    }

    onClick(event) {
        event.preventDefault();
        event.stopPropagation();
        if (this.state.editing) return;
        this.setState({
            editing: true
        }, () => {
            let start = 0;
            let end = this.state.current.length;
            this.textArea.current.focus();
            const pattern = /(.*)\.(.*)/g;
            let match = pattern.exec(this.state.current);
            if (match !== null) {
                const name = extract(null, match, 1);
                const extension = extract(null, match, 2);
                if (name) {
                    start = match.index;
                    end = name.length;
                }
            }
            this.textArea.current.setSelectionRange(start, end);
        });
    }

    onFocus(event) {
    }

    onBlur(event) {
        event.preventDefault();
        event.stopPropagation();
        this.setState({
            editing: false
        }, () => {
            if (this.props.onChange && (this.state.current !== this.state.previous)) {
                this.props.onChange(this.state.current, event);
            }
        });
    }

    onChange(event) {
        const value = event.target.value;
        this.setState({
            current: value,
        });
    }

    onKeyDown(event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            event.stopPropagation();
            this.setState({
                editing: false
            }, () => {
                if (this.props.onChange && (this.state.current !== this.state.previous)) {
                    this.props.onChange(this.state.current, event);
                }
            });
        } else if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            this.setState({
                editing: false,
                current: this.state.previous,
            });
        }
    }

    render() {
        return (
            <div className={cx("editable-text", this.props.className, this.state.editing ? "editing" : null)} onBlur={this.onBlur.bind(this)} onClick={this.onClick.bind(this)}>
            {
                this.state.editing ? <textarea ref={this.textArea} rows={this.props.row ?? "2"} defaultValue={this.state.current} onBlur={this.onBlur.bind(this)} onFocus={this.onFocus.bind(this)} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} /> : this.state.current
            }
            </div>
        );
    }
}
