import React, { Component } from 'react';
import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import Container from 'react-bootstrap/Container';
import Tooltip from 'react-bootstrap/Tooltip';
import Navbar from 'react-bootstrap/Navbar';
import Form from 'react-bootstrap/Form';
import Nav from 'react-bootstrap/Nav';
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
        };
    }

    componentDidMount() {
        this.resetSearchParams();
    }

    resetSearchParams(callback = () => {}) {
        let state = {
            query: "",
            favorite: false,
            recursive: true,
        };
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

    onToggleFavorite() {
        this.setState({
            favorite: !this.state.favorite
        }, () => {
            if (!this.state.query) return;
            clearTimeout(this.searchTimeout);
            let library = this.props.library.current;
            library.search();
        });
    }

    onToggleRecursive() {
        this.setState({
            recursive: !this.state.recursive
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
                <Navbar collapseOnSelect expand="sm" bg="light" variant="light" className="mb-3 ng-white">
                    <Container>
                        <Navbar.Brand href="/">Media Curator</Navbar.Brand>
                        <Navbar.Toggle onClick={this.toggleNavbar} className="mr-2" label="responsive-navbar-nav" />
                        <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                            <Nav className={ "flex-row" + (this.state.collapsed ? "" : " mt-2") }>
                                <Nav.Item>
                                    <div className="filters d-flex align-items-center">
                                        <OverlayTrigger key="favorite" placement="bottom" overlay={ <Tooltip id="tooltip-favorite">{ "" } Favorite</Tooltip> }>
                                            <span onClick={this.onToggleFavorite.bind(this)} className={"flag favorite" + (this.state.favorite ? " set" : "")}></span>
                                        </OverlayTrigger>
                                        <OverlayTrigger key="recursive" placement="bottom" overlay={ <Tooltip id="tooltip-recursive">{ "" } Recursive</Tooltip> }>
                                            <span onClick={this.onToggleRecursive.bind(this)} className={"flag recursive" + (this.state.recursive ? " set" : "")}></span>
                                        </OverlayTrigger>
                                    </div>
                                </Nav.Item>
                                <Nav.Item style={{flexShrink: 1, flexGrow: 1}}>
                                    <Form.Control value={this.state.query} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className=" mr-sm-2" />
                                </Nav.Item>
                            </Nav>
                        </Navbar.Collapse>
                    </Container>
                </Navbar>
            </header>
        );
    }
}