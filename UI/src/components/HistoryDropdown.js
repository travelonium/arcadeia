import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Dropdown from 'react-bootstrap/Dropdown';
import Tooltip from 'react-bootstrap/Tooltip';
import React, { Component } from 'react';
import { Thumbnail } from './Thumbnail';
import { clone, extract, querify } from '../utils';
import cx from 'classnames';
import _ from 'lodash';

export class HistoryDropdown extends Component {

    static displayName = HistoryDropdown.name;

    constructor(props) {
        super(props);
        this.items = [];    // temporary storage for the state.items while the full results are being retrieved
        this.controller = new AbortController();
        this.state = {
            open: false,
            loading: false,
            items: [],
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
        let source = this.state.items.find(item => extract(null, item, "id") === eventKey);
        if (this.props.onSelect) {
            this.props.onSelect(source, event);
        }
    }

    onToggle(show) {
        if (show) this.search(this.props.limit ?? 0);
        this.setState({ open: show });
    }

    search(limit = 0, start = 0, callback = undefined) {
        const rows = limit ? Math.min(limit, 1000) : 1000;
        let query = "dateLastViewed:*";
        let solr = "/search";
        if (process.env.NODE_ENV !== "production") {
            solr = "http://localhost:8983/solr/Library/select";
        }
        const input = {
            q: query,
            fq: [
                "-dateLastViewed:\"0001-01-01T00:00:00Z\""
            ],
            rows: rows,
            start: start,
            defType: "edismax",
            sort: "dateLastViewed desc",
            wt: "json",
        };
        this.controller.abort();
        this.controller = new AbortController();
        if (!start) {
            this.items = [];
        }
        this.setState({
            items: [],
            loading: true,
            status: "Requesting",
        }, () => {
            fetch(solr + "?" + querify(input).toString(), {
                signal: this.controller.signal,
                credentials: 'include',
                headers: {
                    'Accept': 'application/json',
                },
            })
            .then((response) => {
                if (!response.ok) {
                    let message = "Error querying the Solr index!";
                    return response.text().then((data) => {
                        try {
                            let json = JSON.parse(data);
                            let exception = extract(null, json, "error", "msg");
                            if (exception) message = exception;
                        } catch (error) {}
                        throw Error(message);
                    });
                } else {
                    this.setState({
                        status: "Loading",
                    });
                }
                return response.json();
            })
            .then((result) => {
                const numFound = extract(0, result, "response", "numFound");
                const docs = extract([], result, "response", "docs");
                const more = (numFound > (rows + start)) && ((limit === 0) || ((rows + start) < limit));
                this.items = this.items.concat(docs);
                this.setState({
                    loading: more,
                    status: docs.length ? "" : "No Results",
                    items: more ? this.state.items : this.items
                }, () => {
                    if (more) {
                        this.search(limit, start + rows);
                    } else {
                        this.items = [];
                        if (callback !== undefined) {
                            callback(true);
                        }
                    }
                });
            })
            .catch(error => {
                if (error.name === 'AbortError') return;
                this.mediaContainers = {};
                this.items = [];
                this.setState({
                    loading: false,
                    status: error.message,
                    items: []
                }, () => {
                    if (callback !== undefined) {
                        callback(false);
                    }
                });
            });
        });
    }

    convertUtcDateToLocalDate(utc) {
        let date = new Date(utc);
        return date.toLocaleString();
    }

    pluralize(count, noun) {
        return `${count} ${noun}${count !== 1 ? 's' : ''}`;
    }

    timeAgo(utc) {
        const date = new Date(utc);
        const now = new Date();
        const seconds = Math.round((now - date) / 1000);
        const minutes = Math.round(seconds / 60);
        const hours = Math.round(minutes / 60);
        const days = Math.round(hours / 24);
        const weeks = Math.round(days / 7);
        const months = Math.round(days / 30);
        const years = Math.round(days / 365);

        if (seconds < 60) {
            return this.pluralize(seconds, 'second') + ' ago';
        } else if (minutes < 60) {
            return this.pluralize(minutes, 'minute') + ' ago';
        } else if (hours < 24) {
            return this.pluralize(hours, 'hour') + ' ago';
        } else if (days < 7) {
            return this.pluralize(days, 'day') + ' ago';
        } else if (weeks < 5) {
            return this.pluralize(weeks, 'week') + ' ago';
        } else if (months < 12) {
            return this.pluralize(months, 'month') + ' ago';
        } else {
            return this.pluralize(years, 'year') + ' ago';
        }
    }

    render() {
        return (
            <Dropdown className={cx("history-dropdown d-inline", this.props.className)} autoClose="true" onSelect={this.onSelect.bind(this)} onToggle={this.onToggle.bind(this)} align={{ sm: "end", md: "start"}}>
                <OverlayTrigger key={this.props.name} placement="bottom" overlay={
                    (this.props.tooltip && !this.state.open) ? <Tooltip id={"tooltip-" + this.props.name}>{this.props.tooltip}</Tooltip> : <></>
                }>
                    <Dropdown.Toggle id="dropdown-autoclose-outside" className={cx(this.props.className, "border-0 shadow-none")} variant="outline-secondary">
                        <i className={cx("icon bi set bi-clock-history", this.props.name)}></i>
                    </Dropdown.Toggle>
                </OverlayTrigger>
                <Dropdown.Menu className="position-absolute">
                {
                    this.state.items.map((item, index) => {
                        const id = extract(null, item, "id");
                        const name = extract(null, item, "name");
                        const description = extract(null, item, "description");
                        const dateLastViewed = extract(null, item, "dateLastViewed");
                        return (
                            <Dropdown.Item key={index} eventKey={id} active={false}>
                                <div className="item-container d-flex flex-row">
                                    <Thumbnail className="flex-shrink-0 pe-2" source={item} animated={false} />
                                    <div className="content d-flex flex-column justify-content-center flex-fill">
                                        <span className="name flex-shrink-1">{name}</span>
                                        <span className="date-last-viewed flex-shrink-1" title={this.convertUtcDateToLocalDate(dateLastViewed)}>
                                            <i className="bi bi-clock pe-1"></i>{this.timeAgo(dateLastViewed)}
                                        </span>
                                    </div>
                                </div>
                            </Dropdown.Item>
                        )
                    })
                }
                </Dropdown.Menu>
            </Dropdown>
        );
    }
}
