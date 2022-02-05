import Nav from 'react-bootstrap/Nav';
import Form from 'react-bootstrap/Form';
import React, { Component } from 'react';
import Navbar from 'react-bootstrap/Navbar';
import { setQuery, setFavorite, setRecursive } from '../features/search/slice';
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

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
    }

    render() {
        return (
            <header>
                <Navbar collapseOnSelect expand="sm" bg={(this.props.ui.theme === "dark") ? "dark" : "primary"} variant="dark" className="mb-3 py-2">
                    <Navbar.Brand className="py-0" href="/">
                        <svg width="35" height="35" className="animate__animated animate__rotateIn">
                            <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-emblem"></use>
                        </svg>
                        <svg width="225" height="40" className="ml-2 animate__animated animate__fast animate__slideInLeft">
                            <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-text"></use>
                        </svg>
                    </Navbar.Brand>
                    <Navbar.Toggle onClick={this.toggleNavbar} className="mr-2" label="responsive-navbar-nav" />
                    <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                        <Nav className={ "flex-row" + (this.state.collapsed ? "" : " mt-2") }>
                            <Nav.Item>
                                <div className="toolbar d-flex align-items-center px-2">
                                    <Flag className="mr-1" button name="favorite" tooltip="Favorite" value={this.props.search.favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                                    <Flag className="mr-1" button name="recursive" tooltip="Recursive" value={this.props.search.recursive} set="bi-bootstrap-reboot" unset="bi-bootstrap-reboot" onChange={this.onToggleRecursive.bind(this)} />
                                </div>
                            </Nav.Item>
                            <Nav.Item style={{flexShrink: 1, flexGrow: 1}}>
                                <Form.Control value={this.state.query} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className=" mr-sm-2" />
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
        theme: state.ui.theme,
    },
    search: {
        path: state.search.path,
        query: state.search.query,
        favorite: state.search.favorite,
        recursive: state.search.recursive,
    }
});

export default connect(mapStateToProps, null, null)(NavMenu);
