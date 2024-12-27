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
            this.textArea.current.focus();
            if (this.state.current) {
                let start = 0;
                let end = this.state.current?.length;
                const pattern = /(.*)\.(.*)/g;
                let match = pattern.exec(this.state.current);
                if (match !== null) {
                    const name = extract(null, match, 1);
                    // const extension = extract(null, match, 2);
                    if (name) {
                        start = match.index;
                        end = name.length;
                    }
                }
                this.textArea.current.setSelectionRange(start, end);
            }
            if (this.props.onEditing) {
                this.props.onEditing(true);
            }
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
            if (this.props.onEditing) {
                this.props.onEditing(false);
            }
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
        const row = this.props.row ?? 1;
        const current = extract("", this.state, 'current') ?? "";
        return (
            <div className={cx("editable-text d-flex flex-column", this.props.className, this.state.editing ? "editing" : null)} onBlur={this.onBlur.bind(this)} onClick={this.onClick.bind(this)}>
            {
                this.state.editing ?
                <textarea className="textbox" ref={this.textArea} rows={row} defaultValue={current} onBlur={this.onBlur.bind(this)} onFocus={this.onFocus.bind(this)} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} /> :
                <div className={cx("label", (current.length === 0) ? "empty" : null)}>{(current.length > 0) ? current : this.props.name}</div>
            }
                <div className="underline"></div>
            </div>
        );
    }
}
