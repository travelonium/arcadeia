import React, { Component } from 'react';
import Navbar from 'react-bootstrap/Navbar';
import Form from 'react-bootstrap/Form';
import Nav from 'react-bootstrap/Nav';
import { Flag } from './Flag';
import './NavMenu.css';

export class NavMenu extends Component {
    static displayName = NavMenu.name;

    constructor(props) {
        super(props);
        this.searchTimeout = null;
        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.state = {
            query: "",
            collapsed: true,
            favorite: false,
            recursive: true,
        };
    }

    componentDidMount() {
        this.resetSearchParams();
    }

    resetSearchParams(callback = () => {}) {
        let state = Object.assign(this.state, {
            query: "",
            favorite: false,
            recursive: true,
        });
        let params = new URLSearchParams(window.location.search);
        let query = params.get("query");
        let flags = parseInt(params.get("flags"));
        let values = parseInt(params.get("values"));
        let recursive = params.get("recursive");
        if (query) {
            state.query = query;
        }
        if (flags && values) {
            if (flags & (1 << 1)) {
                state.favorite = ((values & (1 << 1)) !== 0);
            }
        }
        if (recursive) {
            state.recursive = (recursive === "true");
        }
        this.setState(state, callback);
    }

    onChange(event) {
        let value = event.target.value;
        if (this.searchTimeout != null) {
            clearTimeout(this.searchTimeout);
        }
        this.setState({
            query: value
        }, () => {
            this.searchTimeout = setTimeout(() => this.onTimeout(), 700);
        })
    }

    onTimeout() {
        if (!this.state.query) return;
        clearTimeout(this.searchTimeout);
        let library = this.props.library.current;
        library.search();
    }

    onKeyDown(event) {
    }

    onToggleFavorite(value) {
        this.setState({
            favorite: value
        }, () => {
            let library = this.props.library.current;
            if (this.state.query) {
                clearTimeout(this.searchTimeout);
                library.search();
            } else {
                library.list(library.state.path, true);
            }
        });
    }

    onToggleRecursive(value) {
        this.setState({
            recursive: value
        }, () => {
            if (!this.state.query) return;
            clearTimeout(this.searchTimeout);
            let library = this.props.library.current;
            library.search();
        });
    }

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
    }

    clearSearch() {
        this.setState({
            query: "",
        })
    }

    render() {
        return (
            <header>
                <Navbar collapseOnSelect expand="sm" bg={this.props.darkMode ? "dark" : "primary"} variant="dark" className="mb-3 py-2">
                    <Navbar.Brand href="/">Media Curator</Navbar.Brand>
                    <Navbar.Toggle onClick={this.toggleNavbar} className="mr-2" label="responsive-navbar-nav" />
                    <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                        <Nav className={ "flex-row" + (this.state.collapsed ? "" : " mt-2") }>
                            <Nav.Item>
                                <div className="toolbar d-flex align-items-center px-2">
                                    <Flag className="mr-1" button name="favorite" tooltip="Favorite" default={this.state.favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                                    <Flag className="mr-1" button name="recursive" tooltip="Recursive" default={this.state.recursive} set="bi-bootstrap-reboot" unset="bi-bootstrap-reboot" onChange={this.onToggleRecursive.bind(this)} />
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