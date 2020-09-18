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
        this.searchInput = React.createRef();
        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.state = {
            favorite: false,
            collapsed: true,
            recursive: false,
        };
    }

    onChange(event) {
        let value = event.target.value;
        if (this.searchTimeout != null) {
            clearTimeout(this.searchTimeout);
        }
        this.searchTimeout = setTimeout(() => this.onTimeout(value), 700);
    }

    onTimeout(value) {
        clearTimeout(this.searchTimeout);
        let library = this.props.library.current;
        library.search(value, this.state.favorite, this.state.recursive);
    }

    getSearchInput() {
        return this.searchInput.current.value;
    }

    setSearchInput(value) {
        this.searchInput.current.value = value;
    }

    onKeyDown(event) {
    }

    onToggleFavorite() {
        let favorite = !this.state.favorite;
        this.setState({
            favorite: favorite
        });
        clearTimeout(this.searchTimeout);
        let library = this.props.library.current;
        library.search(this.getSearchInput(), favorite, this.state.recursive);
    }

    onToggleRecursive() {
        let recursive = !this.state.recursive;
        this.setState({
            recursive: recursive
        });
        clearTimeout(this.searchTimeout);
        let library = this.props.library.current;
        library.search(this.getSearchInput(), this.state.favorite, recursive);
    }

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
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
                                    <Form.Control ref={this.searchInput} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className=" mr-sm-2" />
                                </Nav.Item>
                            </Nav>
                        </Navbar.Collapse>
                    </Container>
                </Navbar>
            </header>
        );
    }
}