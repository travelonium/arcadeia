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

import './NavMenu.css';
import cx from 'classnames';
import { Flag } from './toolbar/Flag';
import { connect } from "react-redux";
import Nav from 'react-bootstrap/Nav';
import Form from 'react-bootstrap/Form';
import React, { Component } from 'react';
import Badge from 'react-bootstrap/Badge';
import { Button } from './toolbar/Button';
import Navbar from 'react-bootstrap/Navbar';
import InputGroup from 'react-bootstrap/InputGroup';
import { ViewDropdown } from './toolbar/ViewDropdown';
import { SortDropdown } from './toolbar/SortDropdown';
import { Container, Row, Col } from 'react-bootstrap';
import { HistoryDropdown } from './toolbar/HistoryDropdown';
import { setSort, resetSort } from '../features/search/slice';
import { extract, getFlag, setFlag, withRouter } from '../utils';
import { setTheme, setView, resetView } from '../features/ui/slice';
import { selectActive, selectQueued, selectFailed } from '../features/ui/selectors';

class NavMenu extends Component {
    static displayName = NavMenu.name;

    constructor(props) {
        super(props);
        this.searchTimeout = null;
        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.state = {
            timeout: 0,
            collapsed: true,
            query: this.query,
            favorite: this.favorite ?? false,
            recursive: this.recursive ?? false,
            duplicates: this.duplicates ?? false,
        };
    }

    get path() {
        try {
            const pathname = this.props?.location?.pathname || '';
            const decodedPath = decodeURI(pathname);
            const components = decodedPath.match(/(.*\/)(.*)?/);
            if (components) return components[1];
            return null;
        } catch (error) {
            console.error('Error decoding the URI:', error);
            return null;
        }
    }

    get query() {
        return this.props.searchParams.get('query') ?? "";
    }

    get favorite() {
        const flags = parseInt(this.props.searchParams.get("flags") ?? 0);
        const values = parseInt(this.props.searchParams.get("values") ?? 0);
        return getFlag(flags, values, 1);
    }

    get recursive() {
        const recursive = this.props.searchParams.get("recursive");
        return (recursive != null) ? recursive === "true" : null;
    }

    get duplicates() {
        const duplicates = this.props.searchParams.get("duplicates");
        return (duplicates != null) ? duplicates === "true" : null;
    }

    componentDidMount() {
    }

    componentDidUpdate(prevProps) {
        // update the internal state with the latest query parameter values if anything has changed
        if (prevProps.location.search !== this.props.location.search) this.setState({
            query: this.query,
            favorite: this.favorite ?? false,
            recursive: this.recursive ?? false,
        });
    }

    updateSearchParams(replace = false) {
        this.props.setSearchParams((params) => {
            const [flags, values] = setFlag(params.get("flags") ?? 0, params.get("values") ?? 0, 1, this.state.favorite);
            if (this.state.query) {
                params.set('query', this.state.query);
            } else {
                params.delete('query');
            }
            if (flags) {
                params.set("flags", flags);
            } else {
                params.delete('flags');
            }
            if (values) {
                params.set("values", values);
            } else {
                params.delete('values');
            }
            if (this.state.recursive) {
                params.set("recursive", this.state.recursive);
            } else {
                params.delete('recursive');
            }
            if (this.state.duplicates) {
                params.set("duplicates", this.state.duplicates);
            } else {
                params.delete('duplicates');
            }
            return params;
        }, { replace: replace });
    }

    onKeyDown(event) {
    }

    onSearchChange(event) {
        let value = event.target.value;
        if (this.searchTimeout != null) {
            clearTimeout(this.searchTimeout);
        }
        this.setState({
            query: value
        }, () => {
            this.searchTimeout = setTimeout(() => this.onSearchChangeTimeout(), this.state.timeout);
        });
    }

    onSearchChangeTimeout() {
        clearTimeout(this.searchTimeout);
        const replace = (this.props.searchParams.get('query') && this.state.query) ? true : false;
        this.updateSearchParams(replace);
    }

    onToggleFavorite(value) {
        clearTimeout(this.searchTimeout);
        this.setState({
            favorite: value
        }, () => {
            this.updateSearchParams();
        });
    }

    onToggleRecursive(value) {
        clearTimeout(this.searchTimeout);
        this.setState({
            recursive: value
        }, () => {
            this.updateSearchParams();
        });
    }

    onToggleDuplicates(value) {
        clearTimeout(this.searchTimeout);
        this.setState({
            duplicates: value
        }, () => {
            this.updateSearchParams();
        });
    }

    onToggleTheme(value) {
        this.props.dispatch(setTheme(value));
    }

    onViewChange(value) {
        this.props.dispatch(setView({
            path: this.state.query ? "search" : this.path,
            value: value
        }));
    }

    onViewReset() {
        this.props.dispatch(resetView({
            path: this.state.query ? "search" : this.path,
        }));
    }

    onHistorySelect(source) {
        let fullPath = extract(null, source, "fullPath");
        if (fullPath) {
            this.props.library.current?.view(source);
        }
    }

    onSortChange(value) {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(setSort({
            path: this.state.query ? "search" : this.path,
            ...value
        }));
    }

    onSortReset() {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(resetSort({
            path: this.state.query ? "search" : this.path,
        }));
    }

    onBrandClick(event) {
        event.preventDefault();
        event.stopPropagation();
        const url = encodeURI("/" + this.props.location.search);
        this.props.navigate(url);
    }

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
    }

    render() {
        const active = this.props.ui.uploads.active.length;
        const queued = this.props.ui.uploads.queued.length;
        const failed = this.props.ui.uploads.failed.length;
        const path = this.state.query ? "search" : this.path;
        const viewOverridden = (path in this.props.ui.view);
        const sortOverridden = (path in this.props.search.sort);
        const view = this.props.ui.view[path] ?? this.props.ui.view.default;
        const sort = this.props.search.sort[path] ?? this.props.search.sort;
        const settings = encodeURI("/settings" + this.props.location.search);
        const disabled = this.props.location.pathname?.startsWith('/settings') ?? false;
        return (
            <Navbar className="d-flex mb-3 p-2" collapseOnSelect expand="sm" bg={(this.props.ui.theme === "dark") ? "dark" : "light"}>
                <Navbar.Brand className="py-0" href="/" onClick={this.onBrandClick.bind(this)}>
                <Container className="d-flex px-0" fluid>
                    <Row className="align-items-center gx-0">
                        <Col className="d-flex" xs="auto">
                            <svg width="30" height="30" className="animate__animated animate__rotateIn">
                                <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-emblem"></use>
                            </svg>
                        </Col>
                        <Col className="d-flex ps-2">
                            <Row className="">
                                <svg height="18" width="140" className="animate__animated animate__fast animate__slideInLeft">
                                    <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-text"></use>
                                </svg>
                            </Row>
                        </Col>
                    </Row>
                </Container>
                </Navbar.Brand>
                <Navbar.Toggle onClick={this.toggleNavbar} className="me-2" label="responsive-navbar-nav" />
                <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                    <Nav className={cx("toolbar flex-md-row flex-sm-column")}>
                        <Nav.Item className="px-sm-1 pb-2 pb-md-0 pt-2 pt-sm-0">
                            <div className="align-items-center">
                                <SortDropdown className="me-1" name="sort" tooltip="Sort" value={sort} overridden={sortOverridden} onChange={this.onSortChange.bind(this)} onReset={this.onSortReset.bind(this)} disabled={disabled} />
                                <ViewDropdown className="me-1" name="view" tooltip="View" value={view} overridden={viewOverridden} onChange={this.onViewChange.bind(this)} onReset={this.onViewReset.bind(this)} disabled={disabled} />
                                <HistoryDropdown className="me-1" name="history" tooltip="History" limit={this.props.ui.history.items} solr={this.props.settings?.Solr?.URL} onSelect={this.onHistorySelect.bind(this)} disabled={disabled} />
                                <Button className="me-1" name="uploads" icon="bi-upload" tooltip="Uploads" onClick={() => this.props.library.current?.showUploads()}>
                                {
                                    (active > 0 || queued > 0) ?
                                    <Badge className="position-absolute bottom-right" bg="info" title="Queued" pill>
                                        {active + queued}
                                    </Badge>
                                    : <></>
                                }
                                {
                                    (failed > 0 && active === 0 && queued === 0) ?
                                    <Badge className="position-absolute bottom-left" bg="danger" title="Failed" pill>
                                        {failed}
                                    </Badge>
                                    : <></>
                                }
                                </Button>
                                <Flag className="me-1" button name="duplicates" tooltip="Duplicates" value={this.state.duplicates} set="bi-files" unset="bi-files" onChange={this.onToggleDuplicates.bind(this)} disabled={disabled} />
                                <Flag className="me-1" button name="favorite" tooltip="Favorite" value={this.state.favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} disabled={disabled} />
                                <Flag className="me-1" button name="recursive" tooltip="Recursive" value={this.state.recursive} set="bi-bootstrap-reboot" unset="bi-bootstrap-reboot" onChange={this.onToggleRecursive.bind(this)} disabled={disabled} />
                                <Flag className="me-1" button name="theme" true={"dark"} false={"light"} tooltip="Theme" value={this.props.ui.theme} set="bi-sun-fill" unset="bi-sun" onChange={this.onToggleTheme.bind(this)} />
                                <Button className="me-1" name="settings" icon="bi-gear" tooltip="Settings" onClick={() => this.props.navigate(settings)} />
                            </div>
                        </Nav.Item>
                        <Nav.Item className="flex-grow-1">
                            <InputGroup>
                                <InputGroup.Text>
                                    <i className="bi bi-search"></i>
                                </InputGroup.Text>
                                <Form.Control value={this.state.query} onChange={this.onSearchChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className="me-sm-2" disabled={disabled} />
                            </InputGroup>
                        </Nav.Item>
                    </Nav>
                </Navbar.Collapse>
            </Navbar>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        view: state.ui.view,
        theme: state.ui.theme,
        uploads: {
            ...state.ui.uploads,
            queued: selectQueued(state),
            active: selectActive(state),
            failed: selectFailed(state)
        },
        history: {
            items: state.ui.history.items
        }
    },
    search: {
        sort: state.search.sort
    },
    settings: state.settings.current
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(NavMenu)
);
