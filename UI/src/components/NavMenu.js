import Nav from 'react-bootstrap/Nav';
import Form from 'react-bootstrap/Form';
import React, { Component } from 'react';
import Navbar from 'react-bootstrap/Navbar';
import { ViewDropdown } from './ViewDropdown';
import { SortDropdown } from './SortDropdown';
import { setQuery, setFavorite, setRecursive, setSort } from '../features/search/slice';
import { setTheme, setView } from '../features/ui/slice';
import { connect } from "react-redux";
import { Flag } from './Flag';
import _ from 'lodash';
import './NavMenu.css';

class NavMenu extends Component {
    static displayName = NavMenu.name;

    constructor(props) {
        super(props);
        this.searchTimeout = null;
        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.state = {
            timeout: 0,
            collapsed: true,
            query: this.props.search.query,
        };
    }

    componentDidMount() {
    }

    componentDidUpdate(prevProps) {
        if (!_.isEqual(this.props.search, prevProps.search)) {
            this.setState({
                query: this.props.search.query,
            });
        }
    }

    onChange(event) {
        let value = event.target.value;
        if (this.searchTimeout != null) {
            clearTimeout(this.searchTimeout);
        }
        this.setState({
            query: value
        }, () => {
            this.searchTimeout = setTimeout(() => this.onTimeout(), this.state.timeout);
        });
    }

    onTimeout() {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(setQuery(this.state.query));
    }

    onKeyDown(event) {
    }

    onToggleFavorite(value) {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(setFavorite(value));
    }

    onToggleRecursive(value) {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(setRecursive(value));
    }

    onToggleTheme(value) {
        this.props.dispatch(setTheme(value));
    }

    onViewChange(value) {
        this.props.dispatch(setView(value));
    }

    onSortChange(value) {
        clearTimeout(this.searchTimeout);
        this.props.dispatch(setSort(value));
    }

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
    }

    render() {
        return (
            <header>
                <Navbar collapseOnSelect expand="sm" bg={(this.props.ui.theme === "dark") ? "dark" : "light"} className="mb-3 p-2">
                    <Navbar.Brand className="py-0" href="/">
                        <svg width="35" height="35" className="animate__animated animate__rotateIn">
                            <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-emblem"></use>
                        </svg>
                        <svg width="225" height="40" className="ms-2 animate__animated animate__fast animate__slideInLeft">
                            <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-text"></use>
                        </svg>
                    </Navbar.Brand>
                    <Navbar.Toggle onClick={this.toggleNavbar} className="me-2" label="responsive-navbar-nav" />
                    <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                        <Nav className={ "flex-row" + (this.state.collapsed ? "" : " mt-2") }>
                            <Nav.Item>
                                <div className="toolbar d-flex align-items-center px-2">
                                    <SortDropdown className="me-1" name="sort" tooltip="Sort" value={this.props.search.sort} onChange={this.onSortChange.bind(this)} />
                                    <ViewDropdown className="me-1" name="view" tooltip="View" value={this.props.ui.view} onChange={this.onViewChange.bind(this)} />
                                    <Flag className="me-1" button name="favorite" tooltip="Favorite" value={this.props.search.favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                                    <Flag className="me-1" button name="recursive" tooltip="Recursive" value={this.props.search.recursive} set="bi-bootstrap-reboot" unset="bi-bootstrap-reboot" onChange={this.onToggleRecursive.bind(this)} />
                                    <Flag className="me-1" button name="theme" true={"dark"} false={"light"} tooltip="Theme" value={this.props.ui.theme} set="bi-sun-fill" unset="bi-sun" onChange={this.onToggleTheme.bind(this)} />
                                </div>
                            </Nav.Item>
                            <Nav.Item style={{flexShrink: 1, flexGrow: 1}}>
                                <Form.Control value={this.state.query} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className=" me-sm-2" />
                            </Nav.Item>
                        </Nav>
                    </Navbar.Collapse>
                </Navbar>
            </header>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        view: state.ui.view,
        theme: state.ui.theme,
    },
    search: {
        sort: state.search.sort,
        path: state.search.path,
        query: state.search.query,
        favorite: state.search.favorite,
        recursive: state.search.recursive,
    }
});

export default connect(mapStateToProps, null, null)(NavMenu);
